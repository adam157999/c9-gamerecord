using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GameRecord.Gamess.Joker
{

    public class CreateUserRequest
    {
        public string Username { get; set; }
    }

    public class ExtCreateUserRequest
    {
        public string Method { get; set; } = "CU";
        public string Username { get; set; }
        public long Timestamp { get; set; }

        public IDictionary<string, string> GetValues()
        {
            return new Dictionary<string, string>
            {
                { "Method", Method },
                { "Username", Username },
                { "Timestamp", Timestamp.ToString() }
            };
        }
    }

    public class GetListGameResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public GetListGameData[] Games { get; set; }
    }

    public class GetListGameData
    {
        public string GameType { get; set; }
        public string GameCode { get; set; }
        public string GameName { get; set; }
        public int DefaultWidth { get; set; }
        public int DefaultHeight { get; set; }
        public string Special { get; set; }
        public int Order { get; set; }
        public string Image1 { get; set; }
        public string Image2 { get; set; }
    }

    public class CreateUserResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public class ExtCreateUserResponse
    {
        public string Status { get; set; }
    }

    public class GetTransactionRequest
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }


    public class GetTransactionResponse
    {
        public string Message { get; set; }
        public List<GameTransaction> GameTransactions { get; set; } = new List<GameTransaction> { };
    }

    public class GameTransaction
    {
        public string TransactionType { get; set; }
        public string OCode { get; set; }
        public string Username { get; set; }
        public string GameCode { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public decimal Amount { get; set; }
        public decimal Result { get; set; }
        public DateTime Time { get; set; }
        public string TransactionOCode { get; set; }
        public string Key { get { return $"{this.Username}_{this.OCode}"; } }
    }

    public class GameTransactionComparer : IEqualityComparer<GameTransaction>
    {
        public bool Equals(GameTransaction x, GameTransaction y)
        {
            return x.Key == y.Key;
        }

        public int GetHashCode(GameTransaction obj)
        {
            return obj.GetHashCode();
        }
    }

    public class GetUserCreditRequest
    {
        public string Username { get; set; }
    }

    public class GetUserCreditResponse
    {
        public bool Success { get; set; }
        public string Username { get; set; }
        public decimal Credit { get; set; }
        public decimal OutstandingCredit { get; set; }
        public decimal FreeCredit { get; set; }
        public decimal OutstandingFreeCredit { get; set; }
        public string Message { get; set; }
    }

    public class PlayGameRequest
    {
        public string Username { get; set; }
    }

    public class PlayGameResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; }
        public string Message { get; set; }
    }

    public class PlayGameWithAmountRequest
    {
        public string Username { get; set; }
        public decimal Amount { get; set; }
        public string RequestID { get; set; }
    }

    public class PlayGameWithAmountResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; }
        public string Message { get; set; }
    }

    public class SignOutRequest
    {
        public string Username { get; set; }
    }

    public class ExtSignOutRequest
    {
        public string Method { get; set; } = "SO";
        public string Username { get; set; }
        public long Timestamp { get; set; }

        public IDictionary<string, string> GetValues()
        {
            return new Dictionary<string, string>
            {
                { "Method", Method },
                { "Username", Username },
                { "Timestamp", Timestamp.ToString() }
            };
        }
    }

    public class SignOutResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public class ExtSignOutResponse
    {
        public string Status { get; set; }
    }

    public class TransferCreditRequest
    {
        public string Username { get; set; }
        public decimal Amount { get; set; }
        public string RequestID { get; set; }
    }

    public class TransferCreditResponse
    {
        public TransferCreditStatus Status { get; set; }
        public string RequestID { get; set; }
        public decimal Credit { get; set; }
        public decimal BeforeCredit { get; set; }
        public decimal OutstandingCredit { get; set; }
        public DateTime Time { get; set; }
        public string Message { get; set; }
    }

    public class VerifyTransferRequest
    {
        public string RequestID { get; set; }
    }

    public class VerifyTransferResponse
    {
        public VerifyTransferCreditStatus Status { get; set; }
        public string Message { get; set; }
        public string Username { get; set; }
        public string RequestID { get; set; }
        public decimal Amount { get; set; }
        public DateTime Time { get; set; }
    }

    public class WithdrawAllCreditRequest
    {
        public string Username { get; set; }
        public string RequestID { get; set; }
    }

    public class ExtWithdrawAllCreditRequest
    {
        public string Method { get; set; } = "WAC";
        public string Username { get; set; }
        public long Timestamp { get; set; }
        public string RequestID { get; set; }

        public IDictionary<string, string> GetValues()
        {
            return new Dictionary<string, string>
            {
                { "Method", Method },
                { "Username", Username },
                { "Timestamp", Timestamp.ToString() },
                { "RequestID", RequestID }
            };
        }
    }

    public class WithdrawAllCreditResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Username { get; set; }
        public string RequestID { get; set; }
        public decimal Amount { get; set; }
        public DateTime Time { get; set; }
    }

    public class ExtWithdrawAllCreditResponse
    {
        public string Username { get; set; }
        public string RequestID { get; set; }
        public decimal Amount { get; set; }
        public string Time { get; set; }
    }



    public enum TransferCreditStatus
    {
        Success,
        Failed,
        Unknown
    }

    public enum VerifyTransferCreditStatus
    {
        Success,
        Failed,
        Unknown
    }




    public interface IJokerIntegration
    {
        Task<GetListGameResponse> GetListGames();
        Task<bool> GetListGamesV2();
        Task<PlayGameWithAmountResponse> PlayGame(PlayGameWithAmountRequest request);
        Task<PlayGameResponse> PlayGame(PlayGameRequest request);
        Task<VerifyTransferResponse> VerifyTransfer(VerifyTransferRequest request);
        Task<GetUserCreditResponse> GetUserCredit(GetUserCreditRequest request);
        Task<TransferCreditResponse> TransferCredit(TransferCreditRequest request);
        Task<GetTransactionResponse> GetTransactions(GetTransactionRequest request);
        Task<CreateUserResponse> CreateUser(CreateUserRequest request);
        Task<WithdrawAllCreditResponse> WithdrawAllCredit(WithdrawAllCreditRequest request);
        Task<SignOutResponse> SignOut(SignOutRequest request);
    }
    public class JokerIntegration : IJokerIntegration
    {
        private readonly HttpClient _jokerHttpClient;
        private readonly string _appId;
        private readonly string _secretKey;

        public JokerIntegration(string baseUrl, string appId, string secretKey)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new ArgumentNullException(nameof(baseUrl));
            if (string.IsNullOrWhiteSpace(appId))
                throw new ArgumentNullException(nameof(appId));
            if (string.IsNullOrWhiteSpace(secretKey))
                throw new ArgumentNullException(nameof(secretKey));

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            _jokerHttpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
            _appId = appId;
            _secretKey = secretKey;
        }

        public class ExtGetListGameRequest
        {
            public string Method { get; } = "ListGames";
            public int Timestamp { get; set; }

            public IDictionary<string, string> GetValues()
            {
                return new Dictionary<string, string>
                {
                    ["Method"] = this.Method,
                    ["Timestamp"] = this.Timestamp.ToString(),
                };
            }
        }

        public class ExtGetListGameResponse
        {
            public ExtGetListGameData[] ListGames { get; set; }
        }

        public class ExtGetListGameData
        {
            public string GameType { get; set; }
            public string GameCode { get; set; }
            public string GameName { get; set; }
            public int DefaultWidth { get; set; }
            public int DefaultHeight { get; set; }
            public string Special { get; set; }
            public int Order { get; set; }
            public string Image1 { get; set; }
            public string Image2 { get; set; }
        }

        public class ExtGetTransactionRequest
        {
            public string Method { get; } = "TSM";
            public string StartDate { get; set; }
            public string EndDate { get; set; }
            public string NextId { get; set; }
            public int Timestamp { get; set; }
            public int Delay { get; set; }

            public IDictionary<string, string> GetValues()
            {
                return new Dictionary<string, string>
                {
                    ["Method"] = this.Method,
                    ["StartDate"] = this.StartDate,
                    ["EndDate"] = this.EndDate,
                    ["NextId"] = this.NextId,
                    ["Timestamp"] = this.Timestamp.ToString(),
                    ["Delay"] = this.Delay.ToString(),
                };
            }
        }

        public class ExtGetTransactionResponse
        {
            public string Message { get; set; }
            public Dictionary<string, ExtGetTransactionData[]> Data { get; set; }
            public string NextId { get; set; }
        }

        public class ExtGetTransactionData
        {
            public string OCode { get; set; }
            public string Username { get; set; }
            public string GameCode { get; set; }
            public string Description { get; set; }
            public string Type { get; set; }
            public decimal Amount { get; set; }
            public decimal Result { get; set; }
            public DateTime Time { get; set; }
            public string TransactionOCode { get; set; }
            public string Key { get { return $"{this.Username}_{this.OCode}"; } }
        }

        public class ExtGetUserCreditRequest
        {
            public string Method { get; } = "GC";
            public string Username { get; set; }
            public int Timestamp { get; set; }

            public IDictionary<string, string> GetValues()
            {
                return new Dictionary<string, string>
                {
                    ["Method"] = this.Method,
                    ["Username"] = this.Username,
                    ["Timestamp"] = this.Timestamp.ToString(),
                };
            }
        }

        public class ExtGetUserCreditResponse
        {
            public string Username { get; set; }
            public decimal Credit { get; set; }
            public decimal OutstandingCredit { get; set; }
            public decimal FreeCredit { get; set; }
            public decimal OutstandingFreeCredit { get; set; }
            public string Message { get; set; }
        }

        public class ExtPlayGameRequest
        {
            public string Method { get; } = "PLAY";
            public string Username { get; set; }
            public int Timestamp { get; set; }

            public IDictionary<string, string> GetValues()
            {
                return new Dictionary<string, string>
                {
                    ["Method"] = this.Method,
                    ["Username"] = this.Username,
                    ["Timestamp"] = this.Timestamp.ToString(),
                };
            }
        }

        public class ExtPlayGameResponse
        {
            public string Token { get; set; }
            public string Message { get; set; }
        }

        public class ExtPlayGameWithAmountRequest
        {
            public string Method { get; } = "PLAY";
            public string Username { get; set; }
            public string Amount { get; set; }
            public string RequestID { get; set; }
            public int Timestamp { get; set; }

            public IDictionary<string, string> GetValues()
            {
                return new Dictionary<string, string>
                {
                    ["Method"] = this.Method,
                    ["Username"] = this.Username,
                    ["Amount"] = this.Amount,
                    ["RequestID"] = this.RequestID,
                    ["Timestamp"] = this.Timestamp.ToString(),
                };
            }
        }

        public class ExtPlayGameWithAmountResponse
        {
            public string Token { get; set; }
            public string Message { get; set; }
        }

        public class ExtTransferCreditRequest
        {
            public string Method { get; } = "TC";
            public string Username { get; set; }
            public string Amount { get; set; }
            public string RequestID { get; set; }
            public int Timestamp { get; set; }

            public IDictionary<string, string> GetValues()
            {
                return new Dictionary<string, string>
                {
                    ["Method"] = this.Method,
                    ["Username"] = this.Username,
                    ["Amount"] = this.Amount,
                    ["RequestID"] = this.RequestID,
                    ["Timestamp"] = this.Timestamp.ToString(),
                };
            }
        }

        public class ExtTransferCreditResponse
        {
            public string RequestID { get; set; }
            public decimal Credit { get; set; }
            public decimal BeforeCredit { get; set; }
            public decimal OutstandingCredit { get; set; }
            public DateTime Time { get; set; }
            public string Message { get; set; }
        }

        public class ExtVerifyTransferRequest
        {
            public string Method { get; } = "TCH";
            public string RequestID { get; set; }
            public int Timestamp { get; set; }

            public IDictionary<string, string> GetValues()
            {
                return new Dictionary<string, string>
                {
                    ["Method"] = this.Method,
                    ["RequestID"] = this.RequestID,
                    ["Timestamp"] = this.Timestamp.ToString(),
                };
            }
        }

        public class ExtVerifyTransferResponse
        {
            public string Username { get; set; }
            public string RequestID { get; set; }
            public decimal Amount { get; set; }
            public DateTime Time { get; set; }
            public string Message { get; set; }
        }

        public static class Helper
        {
            public static int GetTimestamp()
            {
                var unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                return ((int)DateTime.UtcNow.Subtract(unixEpoch).TotalSeconds);
            }
        }

        public static class SignatureHelper
        {
            public static string GetSignature(IDictionary<string, string> data, string secretKey)
            {
                var rawData = String.Join("&", data.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}={pair.Value}"));
                using (var sha = new HMACSHA1(Encoding.UTF8.GetBytes(secretKey)))
                {
                    var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                    return Convert.ToBase64String(hash);
                }
            }
        }



        public class ListGameV2
        {
            public string GameType { get; set; }
            public string GameTypeName { get; set; }
            public string Code { get; set; }
            public string GameOCode { get; set; }
            public string GameCode { get; set; }
            public string GameName { get; set; }
            public string Specials { get; set; }
            public string Technology { get; set; }
            public string SupportedPlatForms { get; set; }
            public int Order { get; set; }
            public int DefaultWidth { get; set; }
            public int DefaultHeight { get; set; }
            public string Image1 { get; set; }
            public string Image2 { get; set; }
            public bool FreeSpin { get; set; }
            public List<Localization> Localizations { get; set; }
            public string OriginalImage1 { get; set; }
            public string OriginalImage2 { get; set; }
        }

        public class Localization
        {
            public string Language { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
        }

        public class ExtGetListGameV2Response
        {
            public List<ListGameV2> ListGames { get; set; }
        }


        public string finalUrl { get; set; }
        public string Request { get; set; }
        public string Parameters { get; set; }
        public string Response { get; set; }
        public string ErrorMessage { get; set; }


        public ExtGetListGameV2Response ExtGetListGameV2Res { get; set; }
        public ErrorResponse ErrorRes { get; set; }


        public class ErrorResponse
        {
            public string Message { get; set; }
        }

        public JokerIntegration(HttpClient httpClient, string appId, string secretKey)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            _jokerHttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _appId = appId ?? throw new ArgumentNullException(nameof(appId));
            _secretKey = secretKey ?? throw new ArgumentNullException(nameof(secretKey));
        }

        public async Task<PlayGameWithAmountResponse> PlayGame(PlayGameWithAmountRequest request)
        {
            var extRequest = new ExtPlayGameWithAmountRequest
            {
                Amount = request.Amount.ToString(),
                RequestID = request.RequestID,
                Timestamp = Helper.GetTimestamp(),
                Username = request.Username
            };

            try
            {
                var signature = Uri.EscapeDataString(SignatureHelper.GetSignature(extRequest.GetValues(), _secretKey));
                var content = new StringContent(
                    JsonConvert.SerializeObject(extRequest),
                    Encoding.UTF8,
                    "application/json");
                finalUrl = _jokerHttpClient.BaseAddress?.ToString();
                Parameters = JsonConvert.SerializeObject(extRequest);
               
                var httpResponse = _jokerHttpClient.PostAsync($"?appid={_appId}&signature={signature}", content).GetAwaiter().GetResult();
                var resultJson = await httpResponse.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ExtPlayGameWithAmountResponse>(resultJson);
                return new PlayGameWithAmountResponse
                {
                    Success = !string.IsNullOrWhiteSpace(result.Token),
                    Token = result.Token,
                    Message = result.Message
                };
            }
            catch (Exception ex)
            {
                return new PlayGameWithAmountResponse
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<PlayGameResponse> PlayGame(PlayGameRequest request)
        {
            var extRequest = new ExtPlayGameRequest
            {
                Timestamp = Helper.GetTimestamp(),
                Username = request.Username
            };

            try
            {
                var signature = Uri.EscapeDataString(SignatureHelper.GetSignature(extRequest.GetValues(), _secretKey));
                var content = new StringContent(
                    JsonConvert.SerializeObject(extRequest),
                    Encoding.UTF8,
                    "application/json");
                finalUrl = _jokerHttpClient.BaseAddress?.ToString();
                Parameters = JsonConvert.SerializeObject(extRequest);

                var httpResponse = _jokerHttpClient.PostAsync($"?appid={_appId}&signature={signature}", content).GetAwaiter().GetResult();
                var resultJson = await httpResponse.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ExtPlayGameResponse>(resultJson);
                return new PlayGameResponse
                {
                    Success = !string.IsNullOrWhiteSpace(result.Token),
                    Token = result.Token,
                    Message = result.Message
                };
            }
            catch (Exception ex)
            {
                return new PlayGameResponse
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<VerifyTransferResponse> VerifyTransfer(VerifyTransferRequest request)
        {
            try
            {
                var extRequest = new ExtVerifyTransferRequest
                {
                    Timestamp = Helper.GetTimestamp(),
                    RequestID = request.RequestID
                };

                var signature = Uri.EscapeDataString(SignatureHelper.GetSignature(extRequest.GetValues(), _secretKey));
                var content = new StringContent(
                    JsonConvert.SerializeObject(extRequest),
                    Encoding.UTF8,
                    "application/json");
                finalUrl = _jokerHttpClient.BaseAddress?.ToString();
                Parameters = JsonConvert.SerializeObject(extRequest);

                var httpResponse = _jokerHttpClient.PostAsync($"?appid={_appId}&signature={signature}", content).GetAwaiter().GetResult();
                var resultJson = await httpResponse.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ExtVerifyTransferResponse>(resultJson);

                if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new VerifyTransferResponse
                    {
                        Status = VerifyTransferCreditStatus.Failed
                    };
                }

                return new VerifyTransferResponse
                {
                    Status = VerifyTransferCreditStatus.Success,
                    Message = result.Message,
                    Amount = result.Amount,
                    RequestID = result.RequestID,
                    Time = result.Time,
                    Username = result.Username
                };
            }
            catch (Exception ex)
            {
                return new VerifyTransferResponse
                {
                    Status = VerifyTransferCreditStatus.Unknown,
                    Message = ex.Message
                };
            }
        }

        public async Task<GetListGameResponse> GetListGames()
        {
            var extRequest = new ExtGetListGameRequest
            {
                Timestamp = Helper.GetTimestamp(),
            };

            try
            {
                var signature = Uri.EscapeDataString(SignatureHelper.GetSignature(extRequest.GetValues(), _secretKey));
                var content = new StringContent(
                    JsonConvert.SerializeObject(extRequest),
                    Encoding.UTF8,
                    "application/json");
                finalUrl = _jokerHttpClient.BaseAddress?.ToString();
                Parameters = JsonConvert.SerializeObject(extRequest);
                var httpResponse = _jokerHttpClient.PostAsync($"?appid={_appId}&signature={signature}", content).GetAwaiter().GetResult();
                var resultJson = await httpResponse.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ExtGetListGameResponse>(resultJson);
                return new GetListGameResponse
                {
                    Success = true,
                    Games = result.ListGames.Select(p => new GetListGameData
                    {
                        GameCode = p.GameCode,
                        GameName = p.GameName,
                        GameType = p.GameType,
                        DefaultHeight = p.DefaultHeight,
                        DefaultWidth = p.DefaultWidth,
                        Special = p.Special,
                        Order = p.Order,
                        Image1 = p.Image1,
                        Image2 = p.Image2
                    }).ToArray()
                };
            }
            catch (Exception ex)
            {
                return new GetListGameResponse
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<bool> GetListGamesV2()
        {
            var extRequest = new ExtGetListGameRequest
            {
                Timestamp = Helper.GetTimestamp(),
            };

            try
            {
                var signature = Uri.EscapeDataString(SignatureHelper.GetSignature(extRequest.GetValues(), _secretKey));
                var content = new StringContent(
                    JsonConvert.SerializeObject(extRequest),
                    Encoding.UTF8,
                    "application/json");
                finalUrl = _jokerHttpClient.BaseAddress?.ToString();
                Parameters = JsonConvert.SerializeObject(extRequest);
                var httpResponse = _jokerHttpClient.PostAsync($"?appid={_appId}&signature={signature}", content).GetAwaiter().GetResult();


                if (httpResponse.StatusCode == HttpStatusCode.OK)
                {
                    var resultJson = await httpResponse.Content.ReadAsStringAsync();
                    ExtGetListGameV2Res = JsonConvert.DeserializeObject<ExtGetListGameV2Response>(resultJson);
                    return true;
                }
                else
                {

                    var resultJson = "";
                    var resultBytes = await httpResponse.Content.ReadAsByteArrayAsync();

                    using (var inputStream = new MemoryStream(resultBytes))
                    using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
                    using (var reader = new StreamReader(gzipStream, Encoding.UTF8))
                    {
                        resultJson = reader.ReadToEnd();
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(resultJson);
                        ErrorMessage = httpResponse.ReasonPhrase + "," + string.Join(" | ", ErrorRes.Message, "");
                    }


                    return false;
                }




            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<GetUserCreditResponse> GetUserCredit(GetUserCreditRequest request)
        {
            var extRequest = new ExtGetUserCreditRequest
            {
                Timestamp = Helper.GetTimestamp(),
                Username = request.Username
            };

            try
            {
                var signature = Uri.EscapeDataString(SignatureHelper.GetSignature(extRequest.GetValues(), _secretKey));
                var content = new StringContent(
                    JsonConvert.SerializeObject(extRequest),
                    Encoding.UTF8,
                    "application/json");
                finalUrl = _jokerHttpClient.BaseAddress?.ToString();
                Parameters = JsonConvert.SerializeObject(extRequest);
                var httpResponse = _jokerHttpClient.PostAsync($"?appid={_appId}&signature={signature}", content).GetAwaiter().GetResult();
                var resultJson = await httpResponse.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ExtGetUserCreditResponse>(resultJson);
                return new GetUserCreditResponse
                {
                    Success = true,
                    Credit = result.Credit,
                    OutstandingCredit = result.OutstandingCredit,
                    FreeCredit = result.FreeCredit,
                    OutstandingFreeCredit = result.OutstandingFreeCredit,
                    Username = result.Username,
                    Message = result.Message
                };
            }
            catch (Exception ex)
            {
                return new GetUserCreditResponse
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<TransferCreditResponse> TransferCredit(TransferCreditRequest request)
        {
            var extRequest = new ExtTransferCreditRequest
            {
                Amount = request.Amount.ToString(),
                RequestID = request.RequestID,
                Timestamp = Helper.GetTimestamp(),
                Username = request.Username
            };

            try
            {
                var signature = Uri.EscapeDataString(SignatureHelper.GetSignature(extRequest.GetValues(), _secretKey));
                var content = new StringContent(
                    JsonConvert.SerializeObject(extRequest),
                    Encoding.UTF8,
                    "application/json");
                finalUrl = _jokerHttpClient.BaseAddress?.ToString();
                Parameters = JsonConvert.SerializeObject(extRequest);
                var httpResponse = _jokerHttpClient.PostAsync($"?appid={_appId}&signature={signature}", content).GetAwaiter().GetResult();
                var resultJson = await httpResponse.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ExtTransferCreditResponse>(resultJson);
                if ((int)httpResponse.StatusCode >= 400 && (int)httpResponse.StatusCode < 500)
                {
                    return new TransferCreditResponse
                    {
                        Status = TransferCreditStatus.Failed,
                        Message = result.Message
                    };
                }

                if ((int)httpResponse.StatusCode >= 500)
                {
                    return new TransferCreditResponse
                    {
                        Status = TransferCreditStatus.Unknown,
                        Message = result.Message
                    };
                }

                return new TransferCreditResponse
                {
                    Status = TransferCreditStatus.Success,
                    Credit = result.Credit,
                    BeforeCredit = result.BeforeCredit,
                    OutstandingCredit = result.OutstandingCredit,
                    RequestID = result.RequestID,
                    Time = result.Time,
                    Message = result.Message
                };
            }
            catch (Exception ex)
            {
                return new TransferCreditResponse
                {
                    Status = TransferCreditStatus.Unknown,
                    Message = ex.Message
                };
            }
        }

        public async Task<GetTransactionResponse> GetTransactions(GetTransactionRequest request)
        {
            try
            {
                var result = new GetTransactionResponse { };
                var nextId = string.Empty;
                do
                {
                    var extRequest = new ExtGetTransactionRequest
                    {
                        StartDate = request.Start.ToString("yyyy-MM-dd HH:00", CultureInfo.InvariantCulture),
                        EndDate = request.End.ToString("yyyy-MM-dd HH:00", CultureInfo.InvariantCulture),
                        NextId = nextId,
                        Timestamp = Helper.GetTimestamp()
                    };

                    var signature = Uri.EscapeDataString(SignatureHelper.GetSignature(extRequest.GetValues(), _secretKey));
                    var content = new StringContent(
                        JsonConvert.SerializeObject(extRequest),
                        Encoding.UTF8,
                        "application/json");
                    finalUrl = _jokerHttpClient.BaseAddress?.ToString();
                    Parameters = JsonConvert.SerializeObject(extRequest);
                    var httpResponse = _jokerHttpClient.PostAsync($"?appid={_appId}&signature={signature}", content).GetAwaiter().GetResult();
                    var resultJson = await httpResponse.Content.ReadAsStringAsync();
                    var response = JsonConvert.DeserializeObject<ExtGetTransactionResponse>(resultJson);

                    if (httpResponse.IsSuccessStatusCode == false)
                    {
                        return new GetTransactionResponse
                        {
                            Message = response.Message
                        };
                    }

                    foreach (var item in response.Data)
                    {
                        result.GameTransactions.AddRange(item.Value.Select(p => new GameTransaction
                        {
                            TransactionType = item.Key,
                            OCode = p.OCode,
                            Username = p.Username,
                            GameCode = p.GameCode,
                            Description = p.Description,
                            Amount = p.Amount,
                            Result = p.Result,
                            Time = p.Time,
                            Type = p.Type,
                            TransactionOCode = p.TransactionOCode
                        }));
                    }

                    nextId = response.NextId;

                } while (string.IsNullOrWhiteSpace(nextId) == false);

                return result;
            }
            catch (Exception ex)
            {
                return new GetTransactionResponse
                {
                    Message = ex.Message,
                };
            }
        }

        public async Task<CreateUserResponse> CreateUser(CreateUserRequest request)
        {
            var extRequest = new ExtCreateUserRequest
            {
                Username = request.Username,
                Timestamp = Helper.GetTimestamp()
            };

            try
            {
                var signature = Uri.EscapeDataString(SignatureHelper.GetSignature(extRequest.GetValues(), _secretKey));
                var content = new StringContent(
                    JsonConvert.SerializeObject(extRequest),
                    Encoding.UTF8,
                    "application/json");
                finalUrl = _jokerHttpClient.BaseAddress?.ToString();
                Parameters = JsonConvert.SerializeObject(extRequest);
                var httpResponse = _jokerHttpClient.PostAsync($"?appid={_appId}&signature={signature}", content).GetAwaiter().GetResult();
                var resultJson = await httpResponse.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ExtCreateUserResponse>(resultJson);

                return new CreateUserResponse
                {
                    Success = result.Status == "OK" || result.Status == "Created",
                    Message = result.Status
                };
            }
            catch (Exception ex)
            {
                return new CreateUserResponse
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<WithdrawAllCreditResponse> WithdrawAllCredit(WithdrawAllCreditRequest request)
        {
            var extRequest = new ExtWithdrawAllCreditRequest
            {
                Username = request.Username,
                RequestID = request.RequestID,
                Timestamp = Helper.GetTimestamp()
            };

            try
            {
                var signature = Uri.EscapeDataString(SignatureHelper.GetSignature(extRequest.GetValues(), _secretKey));
                var content = new StringContent(
                    JsonConvert.SerializeObject(extRequest),
                    Encoding.UTF8,
                    "application/json");
                finalUrl = _jokerHttpClient.BaseAddress?.ToString();
                Parameters = JsonConvert.SerializeObject(extRequest);
                var httpResponse = _jokerHttpClient.PostAsync($"?appid={_appId}&signature={signature}", content).GetAwaiter().GetResult();
                var resultJson = await httpResponse.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ExtWithdrawAllCreditResponse>(resultJson);

                return new WithdrawAllCreditResponse
                {
                    Success = true,
                    Username = result.Username,
                    RequestID = result.RequestID,
                    Amount = result.Amount,
                    Time = DateTime.Parse(result.Time),
                    Message = "Success"
                };
            }
            catch (Exception ex)
            {
                return new WithdrawAllCreditResponse
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<SignOutResponse> SignOut(SignOutRequest request)
        {
            var extRequest = new ExtSignOutRequest
            {
                Username = request.Username,
                Timestamp = Helper.GetTimestamp()
            };

            try
            {
                var signature = Uri.EscapeDataString(SignatureHelper.GetSignature(extRequest.GetValues(), _secretKey));
                var content = new StringContent(
                    JsonConvert.SerializeObject(extRequest),
                    Encoding.UTF8,
                    "application/json");
                finalUrl = _jokerHttpClient.BaseAddress?.ToString();
                Parameters = JsonConvert.SerializeObject(extRequest);
                var httpResponse = _jokerHttpClient.PostAsync($"?appid={_appId}&signature={signature}", content).GetAwaiter().GetResult();
                var resultJson = await httpResponse.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ExtSignOutResponse>(resultJson);

                return new SignOutResponse
                {
                    Success = result.Status == "OK",
                    Message = result.Status
                };
            }
            catch (Exception ex)
            {
                return new SignOutResponse
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }
    }


    public class Test
    {
        public static async Task Main() // Can also be Main(string[] args)
        {

            JokerIntegration api = new JokerIntegration(
                         baseUrl: "https://w.apiext88.net/",
                        appId: "TK9T",
                        secretKey: "xeyusihzct954"
                      );

            Console.WriteLine("Starting API Tests...");

            //if (await api.GetListGamesV2())
            //{

            //}
            //else
            //{
            //    Console.WriteLine($"GetListGamesV2 failed: {api.ErrorMessage}");
            //}



            Console.WriteLine("\nPress any key to exit.");
            Console.ReadKey();
        }
    }
}