using GameRecord.Gamess.WINFINITY.Models;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net;

namespace GameRecord.Gamess.WINFINITY
{
    /// <summary>
    /// Winfinity (agg.bet) B2B API — transfer wallet. Same style as MT:
    /// each call returns bool, sets the *Response property on success, ErrorMessage on failure.
    ///
    /// IsUnknown = true means we did NOT get a clear answer (timeout / network / HTTP 5xx / "unknown error").
    /// For Deposit/Withdraw that means money MAY have moved -> retry with the SAME transaction_id
    /// (Winfinity is idempotent on transaction_id) or compare balances.
    /// </summary>
    public class WINFINITY : IDisposable
    {
        private readonly string apiUrl;
        private readonly string b2bId;
        private readonly string apiKey;
        private readonly string vendorId;
        private readonly int timeOut; // seconds

        // Token is shared across connector instances (connector is created per request).
        private static readonly ConcurrentDictionary<string, CachedToken> TokenCache = new ConcurrentDictionary<string, CachedToken>();
        private static readonly object TokenLock = new object();

        private class CachedToken
        {
            public string Token;
            public DateTime ExpireUtc;
        }

        public string ErrorMessage { get; private set; }
        public bool IsUnknown { get; private set; }
        public HttpStatusCode? LastHttpStatus { get; private set; }
        public string LastRequest { get; private set; }
        public string LastResponse { get; private set; }

        public WfGameListResponse GameListResponse { get; private set; }
        public WfStartGameResponse StartGameResponse { get; private set; }
        public WfTransferResponse DepositResponse { get; private set; }
        public WfTransferResponse WithdrawResponse { get; private set; }
        public WfBalanceResponse GetBalanceResponse { get; private set; }
        public WfReportResponse ReportResponse { get; private set; }

        public WINFINITY(string apiUrl, string b2bId, string apiKey, string vendorId, int timeOut)
        {
            this.apiUrl = (apiUrl ?? "").TrimEnd('/');
            this.b2bId = b2bId;
            this.apiKey = apiKey;
            this.vendorId = vendorId;
            this.timeOut = timeOut <= 0 ? 30 : timeOut;
        }

        // =====================================================================
        // Public API
        // =====================================================================

        public bool GetGameList(string filterVendorId = null)
        {
            var path = "/api/v1/b2b/game/list";
            if (!string.IsNullOrEmpty(filterVendorId)) path += "?vendor_id=" + Uri.EscapeDataString(filterVendorId);
            WfGameListResponse r;
            var ok = Execute(Method.GET, path, null, false, null, out r);
            GameListResponse = r;
            return ok;
        }

        /// <param name="requestId">UUID idempotency key; reuse for a retry of the same launch.</param>
        public bool StartGame(WfStartGameRequest request, string requestId = null)
        {
            WfStartGameResponse r;
            var ok = Execute(Method.POST, "/api/v1/b2b/start/game", request, false, requestId ?? Guid.NewGuid().ToString(), out r);
            StartGameResponse = r;
            if (ok && string.IsNullOrEmpty(r == null ? null : r.url) && string.IsNullOrEmpty(r == null ? null : r.html))
            {
                ErrorMessage = "Start game returned no url/html";
                return false;
            }
            return ok;
        }

        public bool StartDemoGame(WfStartDemoGameRequest request)
        {
            WfStartGameResponse r;
            var ok = Execute(Method.POST, "/api/v1/b2b/start/game/demo", request, false, Guid.NewGuid().ToString(), out r);
            StartGameResponse = r;
            return ok;
        }

        public bool Deposit(WfTransferRequest request)
        {
            WfTransferResponse r;
            var ok = Execute(Method.POST, "/api/v1/b2b/deposit", request, true, null, out r);
            DepositResponse = r;
            return ok && ValidTransfer(r);
        }

        public bool Withdraw(WfTransferRequest request)
        {
            WfTransferResponse r;
            var ok = Execute(Method.POST, "/api/v1/b2b/withdraw", request, true, null, out r);
            WithdrawResponse = r;
            return ok && ValidTransfer(r);
        }

        public bool GetBalance(WfBalanceRequest request)
        {
            WfBalanceResponse r;
            var ok = Execute(Method.POST, "/api/v1/b2b/transfer-balance", request, true, null, out r);
            GetBalanceResponse = r;
            if (ok && (r == null || r.balance == null || string.IsNullOrEmpty(r.balance.amount)))
            {
                ErrorMessage = "Balance response missing amount";
                IsUnknown = true;
                return false;
            }
            return ok;
        }

        /// <summary>Hour-aligned UTC window, max 24h, end at least 1h before now.</summary>
        public bool GetReport(DateTime startUtc, DateTime endUtc, int? page = null)
        {
            var req = new WfReportRequest
            {
                start_time = ToHourUtc(startUtc),
                end_time = ToHourUtc(endUtc),
                vendor_id = vendorId,
                page = page
            };
            WfReportResponse r;
            var ok = Execute(Method.POST, "/api/v1/b2b/reports/transaction", req, false, null, out r);
            ReportResponse = r;
            return ok;
        }

        // =====================================================================
        // Helpers (public static so connector can use them)
        // =====================================================================

        public static string FormatAmount(decimal amount)
        {
            return amount.ToString("0.################", CultureInfo.InvariantCulture);
        }

        public static decimal ParseAmount(string amount)
        {
            if (string.IsNullOrWhiteSpace(amount)) return 0m;
            return decimal.Parse(amount, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture);
        }

        // =====================================================================
        // Token
        // =====================================================================

        private string CacheKey { get { return apiUrl + "|" + b2bId; } }

        private string GetToken(bool forceRefresh)
        {
            CachedToken t;
            if (!forceRefresh && TokenCache.TryGetValue(CacheKey, out t) && DateTime.UtcNow < t.ExpireUtc)
                return t.Token;

            lock (TokenLock)
            {
                if (!forceRefresh && TokenCache.TryGetValue(CacheKey, out t) && DateTime.UtcNow < t.ExpireUtc)
                    return t.Token;

                var client = new RestClient(apiUrl + "/api/v1/b2b/token");
                var request = new RestRequest(Method.POST);
                request.AddHeader("Content-Type", "application/json");
                request.AddHeader("Accept", "application/json");
                request.AddParameter("application/json", JsonConvert.SerializeObject(new WfTokenRequest(b2bId, apiKey)), ParameterType.RequestBody);
                request.Timeout = timeOut * 1000;

                var response = client.Execute(request);
                if (response.ResponseStatus != ResponseStatus.Completed || response.StatusCode != HttpStatusCode.OK)
                    throw new Exception("Winfinity token failed: HTTP " + (int)response.StatusCode + " " + (response.ErrorMessage ?? response.Content));

                var tr = JsonConvert.DeserializeObject<WfTokenResponse>(response.Content);
                if (tr == null || string.IsNullOrEmpty(tr.token))
                    throw new Exception("Winfinity token failed: " + response.Content);

                var expire = tr.expired_at != null && tr.expired_at.seconds > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(tr.expired_at.seconds).UtcDateTime
                    : DateTime.UtcNow.AddMinutes(30);

                TokenCache[CacheKey] = new CachedToken { Token = tr.token, ExpireUtc = expire.AddSeconds(-60) };
                return tr.token;
            }
        }

        // =====================================================================
        // HTTP
        // =====================================================================

        private bool Execute<T>(Method method, string path, object body, bool transferEndpoint, string requestId, out T result) where T : class
        {
            result = null;
            ErrorMessage = null;
            IsUnknown = false;
            LastHttpStatus = null;
            LastRequest = body == null ? null : JsonConvert.SerializeObject(body, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            LastResponse = null;

            bool sent = false;
            try
            {
                var token = GetToken(false);
                sent = true;
                var response = Send(method, path, LastRequest, transferEndpoint, requestId, token);

                // Token expired / revoked early -> refresh once and replay (same transaction_id / request-id, safe).
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    sent = false; // 401 = not processed
                    var newToken = GetToken(true);
                    sent = true;
                    response = Send(method, path, LastRequest, transferEndpoint, requestId, newToken);
                }

                LastResponse = response.Content;
                LastHttpStatus = response.ResponseStatus == ResponseStatus.Completed ? (HttpStatusCode?)response.StatusCode : null;

                // No response at all (timeout / connection error)
                if (response.ResponseStatus != ResponseStatus.Completed)
                {
                    IsUnknown = true;
                    ErrorMessage = response.ResponseStatus + ": " + (response.ErrorMessage ?? "no response");
                    return false;
                }

                var status = (int)response.StatusCode;
                var providerError = ReadError(response.Content);

                if (status >= 500)
                {
                    IsUnknown = true;
                    ErrorMessage = "HTTP " + status + ": " + (providerError ?? response.Content);
                    return false;
                }

                if (status < 200 || status > 299)
                {
                    ErrorMessage = "HTTP " + status + ": " + (providerError ?? response.Content);
                    return false;
                }

                // Business errors come back as HTTP 200 + {"error":"..."}
                if (!string.IsNullOrEmpty(providerError))
                {
                    IsUnknown = string.Equals(providerError, "unknown error", StringComparison.OrdinalIgnoreCase);
                    ErrorMessage = providerError;
                    return false;
                }

                result = JsonConvert.DeserializeObject<T>(response.Content);
                if (result == null)
                {
                    IsUnknown = true;
                    ErrorMessage = "Empty response";
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                // Token failure -> nothing was sent (definite fail). Failure after sending -> unknown.
                IsUnknown = sent;
                ErrorMessage = ex.Message;
                return false;
            }
        }

        private IRestResponse Send(Method method, string path, string json, bool transferEndpoint, string requestId, string token)
        {
            var client = new RestClient(apiUrl + path);
            var request = new RestRequest(method);
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Authorization", "Bearer " + token);
            request.AddHeader("b2b-client-id", b2bId);
            if (transferEndpoint) request.AddHeader("vendor-id", vendorId);
            if (!string.IsNullOrEmpty(requestId)) request.AddHeader("request-id", requestId);
            if (json != null)
            {
                request.AddHeader("Content-Type", "application/json");
                request.AddParameter("application/json", json, ParameterType.RequestBody);
            }
            request.Timeout = timeOut * 1000;
            return client.Execute(request);
        }

        private static string ReadError(string content)
        {
            if (string.IsNullOrWhiteSpace(content) || content.TrimStart()[0] != '{') return null;
            try
            {
                var e = JsonConvert.DeserializeObject<WfErrorResponse>(content);
                return e == null || string.IsNullOrWhiteSpace(e.error) ? null : e.error.Trim();
            }
            catch
            {
                return null;
            }
        }

        private bool ValidTransfer(WfTransferResponse r)
        {
            if (r != null && !string.IsNullOrEmpty(r.transaction_id) && r.balance != null) return true;
            IsUnknown = true;
            ErrorMessage = "Transfer response missing transaction_id/balance";
            return false;
        }

        private static string ToHourUtc(DateTime dt)
        {
            var u = dt.Kind == DateTimeKind.Local ? dt.ToUniversalTime() : DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            return new DateTime(u.Year, u.Month, u.Day, u.Hour, 0, 0, DateTimeKind.Utc).ToString("yyyy-MM-dd'T'HH:00:00'Z'", CultureInfo.InvariantCulture);
        }

        public void Dispose()
        {
        }
    }
}
