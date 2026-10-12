using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;


namespace GameRecord.Gamess.VPOWER
{
    public class VPOWERAPI : IDisposable
    {
        #region Declaration
        public string APIUrl { get; set; }
        public string APPID { get; set; }
        public string AppSecret { get; set; }
        public string GameLaunchUrl { get; set; }
        public string finalUrl { get; set; }
        public string Request { get; set; }
        public string Parameters { get; set; }
        public string Response { get; set; }
        public string ErrorMessage { get; set; }

        public ErrorResponse ErrorRes { get; set; }
        public LoginResponse LoginRes { get; set; }
        public UpdateUserPasswordResponse UpdateUserPasswordRes { get; set; }
        public GetUserTokenResponse GetUserTokenRes { get; set; }
        public CheckUserTokenResponse CheckUserTokenRes { get; set; }
        public GetUserBalanceResponse GetUserBalanceRes { get; set; }
        public TransferUserCreditResponse TransferUserCreditRes { get; set; }
        public CheckUserCreditTransferResponse CheckUserCreditTransferRes { get; set; }
        public GetPlayedUsersResponse GetPlayedUsersRes { get; set; }
        public GetDailySummaryResponse GetDailySummaryRes { get; set; }
        public GetHourSummaryResponse GetHourSummaryRes { get; set; }
        public GetGameLogGroupResponse GetGameLogGroupRes { get; set; }
        public GetGameLogResponse GetGameLogRes { get; set; }
        public GetGameListResponse GetGameListRes { get; set; }
        public int ErrorCode1 { get; set; }

        public enum ErrorCode
        {
            Success = 0,
            NewUserCreated = 1,
            UserDoesNotExist = 2,
            ParameterError = 3,
            InvalidSignature = 4,
            TokenExpired = 5,
            RequestIdAlreadyUsed = 6,
            RequestIdFormatError = 7,
            ExceededMaximumLimit = 8,
            SigningOutFailed = 9,
            InvalidRequest = 10,
            UnknownDatabaseError = 11,
            UserAlreadyExist = 12,
            TopupFail = 13,
            InsufficientCredit = 14,
            NoDataFound = 15,
            RateLimitOrThrottled = 16,
            SystemUnderMaintenance = 17,
            AppIdDoesNotExist = 18,
            ErrorRetrievingBonusFigure = 19,
            ErrorRetrievingPlayerBalance = 20,
            PlayerInActiveGamingSession = 21,
            PlayerAccountDeactivated = 25
        }
        #endregion

        #region Constructor
        public VPOWERAPI(string APIUrl, string APPID, string AppSecret, string GameLaunchUrl)
        {
            this.APIUrl = APIUrl;
            this.APPID = APPID;
            this.AppSecret = AppSecret;
            this.GameLaunchUrl = GameLaunchUrl;
        }
        #endregion

        #region Public API Methods
        private async Task<HttpResponseMessage> GetApiAsync(string url, Dictionary<string, dynamic> bodyContent = null)
        {
            try
            {
                url = url.Replace("\u200B", "");
                finalUrl = url;


                // Convert Dictionary<string, dynamic> to URL query parameters
                if (bodyContent != null && bodyContent.Count > 0)
                {
                    List<string> queryList = new List<string>();
                    foreach (var kvp in bodyContent)
                    {
                        queryList.Add($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value.ToString())}");
                    }
                    string queryString = string.Join("&", queryList);
                    url += "?" + queryString;
                }

                Parameters = url; // Store for debugging/logging

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(url)
                };


                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var handler = new HttpClientHandler();
                using (HttpClient client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(60) })
                {
                    return await client.SendAsync(request);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                throw;
            }
        }

        private async Task<HttpResponseMessage> PostApiInJsonAsync(string url, Dictionary<string, dynamic> bodyContent = null)
        {
            try
            {
                finalUrl = url;
                var jsonContent = JsonConvert.SerializeObject(bodyContent, Formatting.None);
                Parameters = jsonContent;

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(url),
                    Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
                };

                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var handler = new HttpClientHandler();
                using (HttpClient client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) })
                {
                    return await client.SendAsync(request);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                throw;
            }
        }

        private async Task<string> ReadResponseMessageAsync(HttpResponseMessage httpResponseMessage)
        {
            return await httpResponseMessage.Content.ReadAsStringAsync();
        }

        public async Task<bool> GetUserAccount(string playerName)
        {
            try
            {


                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Act", "getacc");
                geek.Add("Timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                geek.Add("Username", playerName);
                geek.Add("AppId", this.APPID);

                string message = $"Act=getacc&Timestamp={(long)geek["Timestamp"]}&Username={playerName}&AppId={this.APPID}";
                string signature = VPowerSignatureHelper.GenerateSignature(message, this.AppSecret);
                geek.Add("Signature", signature);


                string endpoint = $"{this.APIUrl}​";
                HttpResponseMessage response = await GetApiAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    LoginRes = JsonConvert.DeserializeObject<LoginResponse>(responseContent);

                    if (LoginRes.ErrorCode == (int)ErrorCode.NewUserCreated)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", (ErrorCode)ErrorRes.ErrorCode, "");
                        return false;
                    }

                }
                else
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", (ErrorCode)ErrorRes.ErrorCode, "");
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> UpdateUserPasswordAsync(string playerName, string oldPasswordEncrypted, string newPasswordEncrypted)
        {
            try
            {
                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Act", "updatepwd");
                geek.Add("Timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                geek.Add("Username", playerName);
                string PasswordEncryptedV2 = VPowerEncryptor.auth_code(oldPasswordEncrypted, true, this.AppSecret);
                geek.Add("Password", PasswordEncryptedV2);
                string newPasswordEncryptedV2 = VPowerEncryptor.auth_code(newPasswordEncrypted, true, this.AppSecret);
                geek.Add("NewPassword", newPasswordEncryptedV2);
                geek.Add("AppId", this.APPID);

                string message = $"Act=updatepwd&Timestamp={(long)geek["Timestamp"]}&Username={playerName}&Password={oldPasswordEncrypted}&NewPassword={newPasswordEncrypted}&AppId={this.APPID}";
                string signature = VPowerSignatureHelper.GenerateSignature(message, this.AppSecret);
                geek.Add("Signature", signature);

                string endpoint = $"{this.APIUrl}";
                HttpResponseMessage response = await GetApiAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;

                    UpdateUserPasswordRes = JsonConvert.DeserializeObject<UpdateUserPasswordResponse>(responseContent);

                    if (UpdateUserPasswordRes.ErrorCode == (int)ErrorCode.Success)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = response.ReasonPhrase + ", " + ((ErrorCode)ErrorRes.ErrorCode).ToString();
                        return false;
                    }
                }
                else
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = response.ReasonPhrase + ", " + ((ErrorCode)ErrorRes.ErrorCode).ToString();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> GetUserTokenAsync(string playerName)
        {
            try
            {
                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Act", "tkreq");
                geek.Add("Timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                geek.Add("Username", playerName);
                geek.Add("AppId", this.APPID);

                string message = $"Act=tkreq&Timestamp={(long)geek["Timestamp"]}&Username={playerName}&AppId={this.APPID}";
                string signature = VPowerSignatureHelper.GenerateSignature(message, this.AppSecret);
                geek.Add("Signature", signature);

                string endpoint = $"{this.APIUrl}";
                HttpResponseMessage response = await GetApiAsync(endpoint, geek);

                string responseContent = await ReadResponseMessageAsync(response);
                Response = responseContent;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    GetUserTokenRes = JsonConvert.DeserializeObject<GetUserTokenResponse>(responseContent);

                    if (GetUserTokenRes.ErrorCode == (int)ErrorCode.Success)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = response.ReasonPhrase + ", " + ((ErrorCode)ErrorRes.ErrorCode).ToString();
                        return false;
                    }
                }
                else
                {
                    ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = response.ReasonPhrase + ", " + ((ErrorCode)ErrorRes.ErrorCode).ToString();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> CheckUserTokenAsync(string token)
        {
            try
            {
                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Act", "tkchk");
                geek.Add("Timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                geek.Add("Token", token);
                geek.Add("AppId", this.APPID);

                string message = $"Act=tkchk&Timestamp={(long)geek["Timestamp"]}&Token={token}&AppId={this.APPID}";
                string signature = VPowerSignatureHelper.GenerateSignature(message, this.AppSecret);
                geek.Add("Signature", signature);

                string endpoint = $"{this.APIUrl}";
                HttpResponseMessage response = await GetApiAsync(endpoint, geek);

                string responseContent = await ReadResponseMessageAsync(response);
                Response = responseContent;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    CheckUserTokenRes = JsonConvert.DeserializeObject<CheckUserTokenResponse>(responseContent);

                    if (CheckUserTokenRes.ErrorCode == (int)ErrorCode.Success)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = response.ReasonPhrase + ", " + ((ErrorCode)ErrorRes.ErrorCode).ToString();
                        return false;
                    }
                }
                else
                {
                    ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = response.ReasonPhrase + ", " + ((ErrorCode)ErrorRes.ErrorCode).ToString();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> GetUserBalanceAsync(string playerName)
        {
            try
            {
                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Act", "getbal");
                geek.Add("Timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                geek.Add("Username", playerName);
                geek.Add("AppId", this.APPID);

                string message = $"Act=getbal&Timestamp={(long)geek["Timestamp"]}&Username={playerName}&AppId={this.APPID}";
                string signature = VPowerSignatureHelper.GenerateSignature(message, this.AppSecret);
                geek.Add("Signature", signature);

                string endpoint = $"{this.APIUrl}";
                HttpResponseMessage response = await GetApiAsync(endpoint, geek);

                string responseContent = await ReadResponseMessageAsync(response);
                Response = responseContent;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    GetUserBalanceRes = JsonConvert.DeserializeObject<GetUserBalanceResponse>(responseContent);

                    if (GetUserBalanceRes.ErrorCode == (int)ErrorCode.Success)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = response.ReasonPhrase + ", " + ((ErrorCode)ErrorRes.ErrorCode).ToString();
                        return false;
                    }
                }
                else
                {
                    ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = response.ReasonPhrase + ", " + ((ErrorCode)ErrorRes.ErrorCode).ToString();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> TransferUserCreditAsync(string playerName, decimal amount)
        {
            try
            {
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                string requestId = $"{playerName}{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{new Random().Next(1000, 9999)}";

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Act", "creditxf");
                geek.Add("RequestID", requestId);
                geek.Add("Timestamp", timestamp);
                geek.Add("Username", playerName);
                geek.Add("AppId", this.APPID);

                //decimal truncated = Math.Floor(amount * 100) / 100;
                //string result = truncated.ToString("F2");

                geek.Add("Amount", amount.ToString());

                string message = $"Act=creditxf&RequestID={requestId}&Timestamp={timestamp}&Username={playerName}&AppId={this.APPID}&Amount={amount}";
                string signature = VPowerSignatureHelper.GenerateSignature(message, this.AppSecret);
                geek.Add("Signature", signature);

                string endpoint = $"{this.APIUrl}";
                HttpResponseMessage response = await GetApiAsync(endpoint, geek);

                string responseContent = await ReadResponseMessageAsync(response);
                Response = responseContent;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    TransferUserCreditRes = JsonConvert.DeserializeObject<TransferUserCreditResponse>(responseContent);

                    if (TransferUserCreditRes.ErrorCode == (int)ErrorCode.Success)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = response.ReasonPhrase + ", " + ((ErrorCode)ErrorRes.ErrorCode).ToString();
                        return false;
                    }
                }
                else
                {
                    ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = response.ReasonPhrase + ", " + ((ErrorCode)ErrorRes.ErrorCode).ToString();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> CheckUserCreditTransferAsync(string playerName, string requestId, string beginTime, string endTime)
        {
            try
            {
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Act", "creditcheck");
                geek.Add("RequestID", requestId);
                geek.Add("Username", playerName);
                geek.Add("BeginTime", beginTime); // Format: "yyyy-MM-dd HH:mm:ss"
                geek.Add("EndTime", endTime);     // Format: "yyyy-MM-dd HH:mm:ss"
                geek.Add("Timestamp", timestamp);
                geek.Add("AppId", this.APPID);

                string message = $"Act=creditcheck&RequestID={requestId}&Username={playerName}&BeginTime={beginTime}&EndTime={endTime}&Timestamp={timestamp}&AppId={this.APPID}";
                string signature = VPowerSignatureHelper.GenerateSignature(message, this.AppSecret);
                geek.Add("Signature", signature);

                string endpoint = $"{this.APIUrl}";
                HttpResponseMessage response = await GetApiAsync(endpoint, geek);

                string responseContent = await ReadResponseMessageAsync(response);
                Response = responseContent;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    CheckUserCreditTransferRes = JsonConvert.DeserializeObject<CheckUserCreditTransferResponse>(responseContent);

                    if (CheckUserCreditTransferRes.ErrorCode == (int)ErrorCode.Success)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = response.ReasonPhrase + ", " + ((ErrorCode)ErrorRes.ErrorCode).ToString();
                        return false;
                    }
                }
                else
                {
                    ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = response.ReasonPhrase + ", " + ((ErrorCode)ErrorRes.ErrorCode).ToString();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> GetPlayedUsersAsync(string date, string beginTime, string endTime, int pageIndex = 1, int pageSize = 100)
        {
            try
            {
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Act", "playedu");
                geek.Add("Date", date); // Format: yyyy-MM-dd
                geek.Add("BeginTime", beginTime); // Format: HH:mm:ss
                geek.Add("EndTime", endTime);     // Format: HH:mm:ss
                geek.Add("PageIndex", pageIndex);
                geek.Add("PageSize", pageSize);
                geek.Add("TimeStamp", timestamp);
                geek.Add("AppId", this.APPID);

                string message = $"Act=playedu&Date={date}&BeginTime={beginTime}&EndTime={endTime}&PageIndex={pageIndex}&PageSize={pageSize}&TimeStamp={timestamp}&AppId={this.APPID}";
                string signature = VPowerSignatureHelper.GenerateSignature(message, this.AppSecret);
                geek.Add("Signature", signature);

                string endpoint = $"{this.APIUrl}";
                HttpResponseMessage response = await GetApiAsync(endpoint, geek);

                string responseContent = await ReadResponseMessageAsync(response);
                Response = responseContent;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    GetPlayedUsersRes = JsonConvert.DeserializeObject<GetPlayedUsersResponse>(responseContent);

                    if (GetPlayedUsersRes.ErrorCode == (int)ErrorCode.Success)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = response.ReasonPhrase + ", " + ((ErrorCode)ErrorRes.ErrorCode).ToString();
                        return false;
                    }
                }
                else
                {
                    ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = response.ReasonPhrase + ", " + ((ErrorCode)ErrorRes.ErrorCode).ToString();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> GetDailySummaryAsync(string date, int pageIndex = 1, int pageSize = 100, string username = "")
        {
            try
            {
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Act", "dailysum");
                geek.Add("Date", date); // Format: yyyy-MM-dd
                if (!string.IsNullOrEmpty(username))
                    geek.Add("Username", username);
                geek.Add("PageIndex", pageIndex);
                geek.Add("PageSize", pageSize);
                geek.Add("Timestamp", timestamp);
                geek.Add("AppId", this.APPID);

                string message = $"Act=dailysum&Date={date}"
                               + (string.IsNullOrEmpty(username) ? "" : $"&Username={username}")
                               + $"&PageIndex={pageIndex}&PageSize={pageSize}&Timestamp={timestamp}&AppId={this.APPID}";

                string signature = VPowerSignatureHelper.GenerateSignature(message, this.AppSecret);
                geek.Add("Signature", signature);

                string endpoint = $"{this.APIUrl}";
                HttpResponseMessage response = await GetApiAsync(endpoint, geek);

                string responseContent = await ReadResponseMessageAsync(response);
                Response = responseContent;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    GetDailySummaryRes = JsonConvert.DeserializeObject<GetDailySummaryResponse>(responseContent);

                    if (GetDailySummaryRes.ErrorCode == (int)ErrorCode.Success)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = response.ReasonPhrase + ", " + ((ErrorCode)ErrorRes.ErrorCode).ToString();
                        return false;
                    }
                }
                else
                {
                    ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = response.ReasonPhrase + ", " + ((ErrorCode)ErrorRes.ErrorCode).ToString();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> GetHourSummaryAsync(int pageIndex = 1, int pageSize = 100, string username = "")
        {
            try
            {
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Act", "hoursum");
                if (!string.IsNullOrEmpty(username))
                    geek.Add("Username", username);
                geek.Add("PageIndex", pageIndex);
                geek.Add("PageSize", pageSize);
                geek.Add("Timestamp", timestamp);
                geek.Add("AppId", this.APPID);

                string message = $"Act=hoursum"
                               + (string.IsNullOrEmpty(username) ? "" : $"&Username={username}")
                               + $"&PageIndex={pageIndex}&PageSize={pageSize}&Timestamp={timestamp}&AppId={this.APPID}";

                string signature = VPowerSignatureHelper.GenerateSignature(message, this.AppSecret);
                geek.Add("Signature", signature);

                string endpoint = $"{this.APIUrl}";
                HttpResponseMessage response = await GetApiAsync(endpoint, geek);

                string responseContent = await ReadResponseMessageAsync(response);
                Response = responseContent;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    GetHourSummaryRes = JsonConvert.DeserializeObject<GetHourSummaryResponse>(responseContent);

                    if (GetHourSummaryRes.ErrorCode == (int)ErrorCode.Success)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = response.ReasonPhrase + ", " + ((ErrorCode)ErrorRes.ErrorCode).ToString();
                        return false;
                    }
                }
                else
                {
                    ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = response.ReasonPhrase + ", " + ((ErrorCode)ErrorRes.ErrorCode).ToString();
                    return false;

                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> GetGameLogGroupAsync(int pageIndex = 1, int pageSize = 100, string username = "")
        {
            try
            {
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Act", "gloggrp");
                if (!string.IsNullOrEmpty(username))
                    geek.Add("Username", username);
                geek.Add("PageIndex", pageIndex);
                geek.Add("PageSize", pageSize);
                geek.Add("Timestamp", timestamp);
                geek.Add("AppId", this.APPID);

                string message = $"Act=gloggrp"
                               + (string.IsNullOrEmpty(username) ? "" : $"&Username={username}")
                               + $"&PageIndex={pageIndex}&PageSize={pageSize}&Timestamp={timestamp}&AppId={this.APPID}";

                string signature = VPowerSignatureHelper.GenerateSignature(message, this.AppSecret);
                geek.Add("Signature", signature);

                string endpoint = $"{this.APIUrl}";
                HttpResponseMessage response = await GetApiAsync(endpoint, geek);

                string responseContent = await ReadResponseMessageAsync(response);
                Response = responseContent;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    GetGameLogGroupRes = JsonConvert.DeserializeObject<GetGameLogGroupResponse>(responseContent);

                    if (GetGameLogGroupRes.ErrorCode == (int)ErrorCode.Success)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = response.ReasonPhrase + ", " + ((ErrorCode)ErrorRes.ErrorCode).ToString();
                        return false;
                    }
                }
                else
                {
                    ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = response.ReasonPhrase + ", " + ((ErrorCode)ErrorRes.ErrorCode).ToString();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> GetGameLogAsync(string gameId, string username, string beginTime, string endTime,
                                                int pageIndex = 1, string lang = "en")
        {
            try
            {
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Act", "glog");
                geek.Add("GameId", gameId);
                geek.Add("Username", username);
                geek.Add("BeginTime", beginTime);
                geek.Add("EndTime", endTime);
                geek.Add("PageIndex", pageIndex);
                geek.Add("Lang", lang);
                geek.Add("Timestamp", timestamp);
                geek.Add("AppId", this.APPID);

                string message = $"Act=glog&GameId={gameId}&Username={username}&BeginTime={beginTime}&EndTime={endTime}"
                               + $"&PageIndex={pageIndex}&Lang={lang}&Timestamp={timestamp}&AppId={this.APPID}";

                string signature = VPowerSignatureHelper.GenerateSignature(message, this.AppSecret);
                geek.Add("Signature", signature);

                string endpoint = $"{this.APIUrl}";
                HttpResponseMessage response = await GetApiAsync(endpoint, geek);

                string responseContent = await ReadResponseMessageAsync(response);
                Response = responseContent;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    GetGameLogRes = JsonConvert.DeserializeObject<GetGameLogResponse>(responseContent);

                    if (GetGameLogRes.ErrorCode == (int)ErrorCode.Success)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = response.ReasonPhrase + ", " + ((ErrorCode)ErrorRes.ErrorCode).ToString();
                        return false;
                    }
                }
                else
                {
                    ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = response.ReasonPhrase + ", " + ((ErrorCode)ErrorRes.ErrorCode).ToString();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> GetGameListAsync(int? bonus = null)
        {
            try
            {
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Act", "gamelist");

                if (bonus.HasValue)
                {
                    geek.Add("Bonus", bonus.Value);
                }

                geek.Add("Timestamp", timestamp);
                geek.Add("AppId", this.APPID);

                string message = $"Act=gamelist&Timestamp={timestamp}&AppId={this.APPID}";

                if (bonus.HasValue)
                {
                    message += $"&Bonus={bonus.Value}";
                }

                string signature = VPowerSignatureHelper.GenerateSignature(message, this.AppSecret);
                geek.Add("Signature", signature);

                string endpoint = $"{this.APIUrl}";
                HttpResponseMessage response = await GetApiAsync(endpoint, geek);

                string responseContent = await ReadResponseMessageAsync(response);
                Response = responseContent;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    GetGameListRes = JsonConvert.DeserializeObject<GetGameListResponse>(responseContent);

                    if (GetGameListRes.ErrorCode == (int)ErrorCode.Success)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = response.ReasonPhrase + ", " + ((ErrorCode)ErrorRes.ErrorCode).ToString();
                        return false;
                    }
                }
                else
                {
                    ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = response.ReasonPhrase + ", " + ((ErrorCode)ErrorRes.ErrorCode).ToString();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> SignOutAsync(string username)
        {
            try
            {
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Act", "signout");
                geek.Add("Username", username);
                geek.Add("Timestamp", timestamp);
                geek.Add("AppId", this.APPID);

                string message = $"Act=signout&Username={username}&Timestamp={timestamp}&AppId={this.APPID}";
                string signature = VPowerSignatureHelper.GenerateSignature(message, this.AppSecret);
                geek.Add("Signature", signature);

                string endpoint = $"{this.APIUrl}";
                HttpResponseMessage response = await GetApiAsync(endpoint, geek);

                string responseContent = await ReadResponseMessageAsync(response);
                Response = responseContent;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var signOutResponse = JsonConvert.DeserializeObject<SignOutResponse>(responseContent);

                    if (signOutResponse.ErrorCode == (int)ErrorCode.Success)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorCode1 = signOutResponse.ErrorCode1;
                        ErrorMessage = response.ReasonPhrase + ", " + ((ErrorCode)ErrorCode1).ToString();
                        return false;
                    }
                }
                else
                {
                    var signOutResponse = JsonConvert.DeserializeObject<SignOutResponse>(responseContent);
                    ErrorCode1 = signOutResponse.ErrorCode1;
                    ErrorMessage = response.ReasonPhrase + ", " + ((ErrorCode)ErrorCode1).ToString();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public string LaunchGameAsync(string gameId, string token, string backUrl, string lang = "en")
        {
            try
            {

                var encodedBack = Convert.ToBase64String(Encoding.UTF8.GetBytes(backUrl));


                var url = $"{this.GameLaunchUrl}?id={gameId}&token={token}&back={encodedBack}&lang={lang}";
                return url;


            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return "";
            }
        }

        public class SignOutResponse : BaseResponse
        {
            [JsonProperty("ErrorCode")]
            public int ErrorCode1 { get; set; }
        }

        public class GetGameListResponse : BaseResponse
        {
            public List<GameItem> GameList { get; set; }
        }

        public class GameItem
        {
            [JsonProperty("GameType")]
            public string GameType { get; set; }

            [JsonProperty("GameID")]
            public string GameID { get; set; }

            [JsonProperty("GameName")]
            public string GameName { get; set; }

            [JsonProperty("GameName_EN")]
            public string GameNameEN { get; set; }

            [JsonProperty("isNew")]
            public string IsNew { get; set; }

            [JsonProperty("Bonus")]
            public string Bonus { get; set; }

            [JsonProperty("SupportedPlatForms")]
            public string SupportedPlatforms { get; set; }

            [JsonProperty("Special")]
            public string Special { get; set; }

            [JsonProperty("DefaultWidth")]
            public int DefaultWidth { get; set; }

            [JsonProperty("DefaultHeight")]
            public int DefaultHeight { get; set; }

            [JsonProperty("Image1")]
            public string Image1 { get; set; }
        }


        public class GetGameLogResponse : BaseResponse
        {
            public List<GameLogData> Data { get; set; }
            public int Total { get; set; }
        }

        public class GameLogData
        {
            [JsonProperty("uid")]
            public string Uid { get; set; }

            [JsonProperty("username")]
            public string Username { get; set; }

            [JsonProperty("gameid")]
            public string GameId { get; set; }

            [JsonProperty("gameocode")]
            public string GameCode { get; set; }

            [JsonProperty("gamename")]
            public string GameName { get; set; }

            [JsonProperty("tableid")]
            public string TableId { get; set; }

            [JsonProperty("pay")]
            public string Pay { get; set; }

            [JsonProperty("win")]
            public string Win { get; set; }

            [JsonProperty("uniqleid")]
            public string UniqueId { get; set; }

            [JsonProperty("startscore")]
            public string StartScore { get; set; }

            [JsonProperty("finalscore")]
            public string FinalScore { get; set; }

            [JsonProperty("time")]
            public string Time { get; set; }
        }


        public class GetGameLogGroupResponse : BaseResponse
        {
            public List<GameLogGroupData> Data { get; set; }
            public int Total { get; set; }
        }

        public class GameLogGroupData
        {
            [JsonProperty("username")]
            public string Username { get; set; }

            [JsonProperty("record_count")]
            public int RecordCount { get; set; }

            [JsonProperty("total_pay")]
            public string TotalPay { get; set; }

            [JsonProperty("total_win")]
            public string TotalWin { get; set; }

            [JsonProperty("unique_id")]
            public string UniqueId { get; set; }
        }


        public class GetHourSummaryResponse : BaseResponse
        {
            public HourSummaryData Data { get; set; }
        }

        public class HourSummaryData
        {
            [JsonProperty("record_count")]
            public int RecordCount { get; set; }

            [JsonProperty("total_pay")]
            public string TotalPay { get; set; }

            [JsonProperty("total_win")]
            public string TotalWin { get; set; }
        }


        public class GetDailySummaryResponse : BaseResponse
        {
            public int Total { get; set; }
            public List<DailySummaryData> Data { get; set; }
        }

        public class DailySummaryData
        {
            [JsonProperty("username")]
            public string Username { get; set; }

            [JsonProperty("total_pay")]
            public string TotalPay { get; set; }

            [JsonProperty("total_win")]
            public string TotalWin { get; set; }

            [JsonProperty("start_score")]
            public string StartScore { get; set; }

            [JsonProperty("end_score")]
            public string EndScore { get; set; }
        }



        public class GetPlayedUsersResponse : BaseResponse
        {
            public int Total { get; set; }
            public List<string> Data { get; set; }
        }


        public class CheckUserCreditTransferResponse : BaseResponse
        {
            public List<CreditTransferRecord> List { get; set; }
            public int Total { get; set; }
        }

        public class CreditTransferRecord
        {
            public string RequestID { get; set; }
            public string Username { get; set; }
            public string Amount { get; set; }
            public string BeforeCredit { get; set; }
            public string Credit { get; set; }
            public string Time { get; set; }
        }




        public class TransferUserCreditResponse : BaseResponse
        {
            public string Username { get; set; }
            public string Credit { get; set; }
            public string RequestID { get; set; }
            public string Time { get; set; }
            public string BeforeCredit { get; set; }
        }


        public class GetUserBalanceResponse : BaseResponse
        {
            public string Username { get; set; }
            public decimal Credit { get; set; }
        }


        public class CheckUserTokenResponse : BaseResponse
        {

        }



        public class GetUserTokenResponse : BaseResponse
        {
            public string Username { get; set; }
            public string Token { get; set; }
        }


        public class ErrorResponse : BaseResponse
        {


        }

        public class LoginResponse : BaseResponse
        {

            public string Username { get; set; }
            public string Password { get; set; }
        }

        public class UpdateUserPasswordResponse : BaseResponse
        {


        }





        public void Dispose()
        {
            throw new NotImplementedException();
        }


        #endregion

        #region Response Models

        public class BaseResponse
        {
            public int ErrorCode { get; set; }

            public string Info => Enum.IsDefined(typeof(ErrorCode), ErrorCode) ? ((ErrorCode)ErrorCode).ToString() : "Unknown Error";

        }


        #endregion
    }

    public class VPowerEncryptor
    {
        public static string auth_code(string input, bool encode = true, string key = "")
        {
            byte[] stringBytes;

            if (!encode)
            {
                try
                {
                    stringBytes = Convert.FromBase64String(input);
                }
                catch
                {
                    return "";
                }
            }
            else
            {
                stringBytes = Encoding.UTF8.GetBytes(input);
            }

            key = GetMD5(key);
            string aKey = GetMD5(key.Substring(0, 16));
            string bKey = GetMD5(key.Substring(16, 16));

            string eKey = aKey + GetMD5(aKey);
            int eKeyLength = eKey.Length;

            if (encode)
            {
                string md5Part = GetMD5(Encoding.UTF8.GetString(stringBytes) + bKey).Substring(0, 16);
                stringBytes = Encoding.UTF8.GetBytes(md5Part + Encoding.UTF8.GetString(stringBytes));
            }
            else
            {
                try
                {
                    stringBytes = Convert.FromBase64String(Encoding.UTF8.GetString(stringBytes));
                }
                catch
                {
                    return "";
                }
            }

            int stringLength = stringBytes.Length;

            int[] rKey = new int[128];
            for (int i = 0; i < 128; i++)
            {
                rKey[i] = (byte)eKey[i % eKeyLength];
            }

            int[] box = new int[128];
            for (int i = 0; i < 128; i++)
            {
                box[i] = i;
            }

            int j = 0;
            for (int i = 0; i < 128; i++)
            {
                j = (j + box[i] + rKey[i]) % 128;
                int tmp = box[i];
                box[i] = box[j];
                box[j] = tmp;
            }

            byte[] result = new byte[stringLength];
            int k = 0;
            j = 0;
            for (int i = 0; i < stringLength; i++)
            {
                k = (k + 1) % 128;
                j = (j + box[k]) % 128;
                int tmp = box[k];
                box[k] = box[j];
                box[j] = tmp;
                int xorIndex = (box[k] + box[j]) % 128;
                result[i] = (byte)(stringBytes[i] ^ box[xorIndex]);
            }

            if (encode)
            {
                string b64_1 = Convert.ToBase64String(result).Replace("=", "");
                string b64_2 = Convert.ToBase64String(Encoding.UTF8.GetBytes(b64_1)).Replace("=", "");
                return b64_2;
            }
            else
            {
                string resultStr = Encoding.UTF8.GetString(result);
                if (resultStr.Length < 22) return "";

                string checkSub = resultStr.Substring(16);
                string md5check = GetMD5(checkSub + bKey).Substring(10, 6);
                if (resultStr.Substring(10, 6) == md5check)
                {
                    return checkSub;
                }
                return "";
            }
        }

        private static string GetMD5(string input)
        {
            var md5 = MD5.Create();
            byte[] data = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            StringBuilder sb = new StringBuilder();
            foreach (byte t in data)
            {
                sb.Append(t.ToString("x2"));
            }
            return sb.ToString();
        }
    }

    public static class VPowerSignatureHelper
    {
        public static string GenerateSignature(string content, string secretKey)
        {
            string secret = secretKey;
            var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(secret));
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(content));
            return Convert.ToBase64String(hash);
        }
    }

    public class Test
    {
        public static async Task Main(string[] args)
        {
            VPOWERAPI api = new VPOWERAPI(
               "https://www.vpower34.com/open_api",
               "rf0IKtofUrmikzw4107",
               "Y37KY3opJwmkotrn1Np8vy5945f2",
               "https://www.vpower34.com/open_game"
           );


            //string act = "updatepwd";
            //long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            //string username = "mitjenny123";
            //string appId = "rf0IKtofUrmikzw4107";
            //string secretKey = "Y37KY3opJwmkotrn1Np8vy5945f2";



            //string plaintext = "123123";

            //// Encrypt
            //string encrypted = VPowerEncryptor.auth_code(plaintext, true, secretKey);
            //Console.WriteLine("Encrypted: " + encrypted);

            //// Decrypt
            //string decrypted = VPowerEncryptor.auth_code(encrypted, false, secretKey);
            //Console.WriteLine("Decrypted: " + decrypted);

            //string message = $"Act={act}&Timestamp={timestamp}&Username={username}&Password=U2xWVkpuaGdWMnc3QVYxYWQxeDhkRFVrZVNsVkV3&NewPassword={encrypted}&AppId={appId}";
            //Console.WriteLine(timestamp);
            //string signature = VPowerSignatureHelper.GenerateSignature(message, secretKey);

            //Console.WriteLine(signature);





            // Decrypt
            //string decrypted = VPowerAuthCode.AuthCode("U2xWVkpuaGdWMnc3QVYxYWQxeDhkRFVrZVNsVkV3", false, "Y37KY3opJwmkotrn1Np8vy5945f2");
            //Console.WriteLine("Decrypted: " + decrypted);



            //if (await api.GetUserAccount("mitjenny123"))
            //{
            //    if (api.LoginRes.ErrorCode == (int)ErrorCode.Success)
            //    {
            //        Console.WriteLine("GetUserAccount successful!");

            //    }
            //    else
            //    {
            //        Console.WriteLine($"GetUserAccount failed: {api.LoginRes.Info}");
            //    }
            //}
            //else
            //{
            //    Console.WriteLine($"GetUserAccount failed: {api.ErrorMessage}");
            //}


            //if (await api.GetUserBalanceAsync("mitjenny123"))
            //{
            //    if (api.GetUserBalanceRes.ErrorCode == (int)VPOWERAPI.ErrorCode.Success)
            //    {
            //        Console.WriteLine("GetUserBalance successful!");

            //    }
            //    else
            //    {
            //        Console.WriteLine($"GetUserBalance failed: {api.GetUserBalanceRes.Info}");
            //    }
            //}
            //else
            //{
            //    Console.WriteLine($"GetUserBalance failed: {api.ErrorMessage}");
            //}

            //if (await api.TransferUserCreditAsync("mitjenny123",100))
            //{
            //    if (api.TransferUserCreditRes.ErrorCode == (int)VPOWERAPI.ErrorCode.Success)
            //    {
            //        Console.WriteLine("TransferUserCredit successful!");

            //    }
            //    else
            //    {
            //        Console.WriteLine($"TransferUserCredit failed: {api.TransferUserCreditRes.Info}");
            //    }
            //}
            //else
            //{
            //    Console.WriteLine($"TransferUserCredit failed: {api.ErrorMessage}");
            //}


            //if (await api.GetGameListAsync())
            //{
            //    if (api.GetGameListRes.ErrorCode == (int)VPOWERAPI.ErrorCode.Success)
            //    {
            //        Console.WriteLine("GetGameList successful!");

            //    }
            //    else
            //    {
            //        Console.WriteLine($"GetGameList failed: {api.GetGameListRes.Info}");
            //    }
            //}
            //else
            //{
            //    Console.WriteLine($"GetGameList failed: {api.ErrorMessage}");
            //}



            //if (await api.GetUserTokenAsync("mitjenny123"))
            //{
            //    if (api.GetUserTokenRes.ErrorCode == (int)VPOWERAPI.ErrorCode.Success)
            //    {
            //        Console.WriteLine("GetUserToken successful!");

            //    }
            //    else
            //    {
            //        Console.WriteLine($"GetUserToken failed: {api.GetUserTokenRes.Info}");
            //    }
            //}
            //else
            //{
            //    Console.WriteLine($"GetUserToken failed: {api.ErrorMessage}");
            //}


            //string gameurl = api.LaunchGameAsync("CODE3136", "86afbbe9a5c2a2f0", "");



            Console.WriteLine("API Tests Completed.");
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}
