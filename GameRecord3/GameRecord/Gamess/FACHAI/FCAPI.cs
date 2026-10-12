using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using static GameRecord.Gamess.FACHAI.FCAPI;
using System.Linq;


namespace GameRecord.Gamess.FACHAI
{
    public class FCAPI : IDisposable
    {
        #region Declaration
        public string APIUrl { get; set; }
        public string AgentCode { get; set; }
        public string Currency { get; set; }
        public string AgentKey { get; set; }

        public string finalUrl { get; set; }
        public string Request { get; set; }
        public string Parameters { get; set; }
        public string Response { get; set; }
        public string ErrorMessage { get; set; }


        public ErrorResponse ErrorRes { get; set; }

        public CreateUserResponse CreateUserRes { get; set; }

        public SearchMemberInfoResponse SearchMemberInfoRes { get; set; }

        public GameLoginResponse GameLoginRes { get; set; }

        public KickoutMemberResponse KickoutMemberRes { get; set; }

        public KickoutAllMemberResponse KickoutAllMemberRes { get; set; }

        public QueryPlayerReportResponse QueryPlayerReportRes { get; set; }

        public DailyCurrencyReportResponse DailyCurrencyReportRes { get; set; }


        public GetOnlineMemberResponse GetOnlineMemberRes { get; set; }

        public GetRecordListResponse GetRecordListRes { get; set; }

        public MemberGameReportResponse MemberGameReportRes { get; set; }

        public GetGameListandIconResponse GetGameListandIconRes { get; set; }

        public GetJackpotPoolAmountResponse GetJackpotPoolAmountRes { get; set; }

        public GetLotteryPrizeReportResponse GetLotteryPrizeReportRes { get; set; }


        public GetHistoryRecordListResponse GetHistoryRecordListRes { get; set; }

        public GetEventLinkResponse GetEventLinkRes { get; set; }

        public SetPointsResponse SetPointsRes { get; set; }


        public GetSingleBillResponse GetSingleBillRes { get; set; }

        public GetMemberPointsResponse GetMemberPointsRes { get; set; }

        public GetBillListResponse GetBillListRes { get; set; }


        public CreateFreeSpinResponse CreateFreeSpinRes { get; set; }

        public CancelFreeSpinResponse CancelFreeSpinRes { get; set; }

        public BindingMemberFreeSpinResponse BindingMemberFreeSpinRes { get; set; }
        public FreezeMemberFreeSpinResponse FreezeMemberFreeSpinRes { get; set; }

        public SearchMemberFreeSpinResponse SearchMemberFreeSpinRes { get; set; }

        public GetActivityRecordListResponse GetActivityRecordListRes { get; set; }



        public enum ApiErrorCode
        {
            Success = 0,
            WithdrawPointsIsZero = 200,
            AbnormalPoints = 201,
            WrongCodeForDepositAndWithdrawal = 202,
            NotEnoughBalance = 203,
            TransactionIdTooLong = 204,
            DuplicateTransactionId = 205,
            InvalidStartTime = 206,
            InvalidEndTime = 207,
            StartTimeAheadOfEndTime = 208,
            PageNumbersNotNumeric = 209,
            TransactionIdEmpty = 210,
            InvalidBettingRecordParameter = 211,
            TransactionIdNotNumeric = 212,
            IllegalTransactionId = 213,
            GameIdNoBonusMode = 214,
            LanguageNotExist = 215,
            ParameterNotJsonEncrypted = 217,
            DataNotNumber = 218,
            PointsExceedMaximumLimit = 219,
            SearchTimeExceedsMaxRange = 220,
            TransactionIdNotExist = 221,
            WrongGameType = 222,
            PageExceedsTotal = 223,
            PageError = 224,
            RowError = 225,
            OrderError = 226,
            SearchTimeBelowMinRange = 227,
            EventTypeError = 228,
            UnableToGetApiKey = 301,
            DataDecryptionFailed = 303,
            SignVerificationFailed = 304,
            SignDisabled = 401,
            MethodDisabled = 402,
            MethodUnderMaintenance = 403,
            GameNotExist = 405,
            GameClosed = 406,
            AccountLocked = 407,
            GameUnderMaintenance = 408,
            NoAuthorization = 409,
            IpNotAllowed = 410,
            MethodMaintenance = 411,
            EventIdNotFound = 416,
            BindingLimitReached = 417,
            PlayerNotInEvent = 418,
            DuplicateEventId = 419,
            AccountNotExist = 500,
            AccountTooLong = 501,
            DuplicateAccount = 502,
            UserOnline = 503,
            UserOffline = 504,
            AccountTooShort = 505,
            UrlIsBlank = 601,
            ResponseNull = 602,
            ReturnDataNotJson = 603,
            VerificationFailed = 604,
            NoVerificationResult = 605,
            PointsNotNumber = 606,
            NoPointsReturned = 607,
            FailedToObtainPoints = 701,
            WriteTransactionFailed = 702,
            DepositWithdrawalError = 703,
            PlayerInfoQueryBlank = 704,
            TotalGameRecordQueryBlank = 705,
            GameRecordQueryBlank = 706,
            TotalTransactionRecordQueryEmpty = 708,
            NoTransactionRecordFound = 709,
            RevertRecordId = 799,
            SendCancelRequest = 899,
            UrlExpired = 901,
            CannotFindGameUrl = 902,
            LanguageUpdateFailed = 903,
            DataTransmissionError = 910,
            UnableToFetchData = 911,
            IpTooLong = 912,
            UnknownError = 999,
            AgentCodeMissing = 1011,
            CurrencyCodeMissing = 1012,
            ParameterContainsNoData = 1013,
            AgentSignMissing = 1014,
            StartTimeMissing = 1015,
            EndTimeMissing = 1016,
            PageNumberMissing = 1017,
            DateInputMissing = 1018,
            GameTypeMissing = 1019,
            GameRecordNumberMissing = 1020,
            ParameterContainsError = 1098,
            ParameterContainsErrorDuplicate = 1099
        }



        #endregion

        #region Constructor
        public FCAPI(string APIUrl, string AgentCode, string Currency, string AgentKey)
        {
            this.APIUrl = APIUrl;
            this.AgentCode = AgentCode;
            this.Currency = Currency;
            this.AgentKey = AgentKey;

        }
        #endregion

        #region Public API Methods


        private async Task<HttpResponseMessage> PostApiAsync(string url, Dictionary<string, dynamic> bodyContent = null)
        {
            try
            {
                finalUrl = url;


                Dictionary<string, dynamic> tempBodyContent;
                if (bodyContent != null)
                {
                    tempBodyContent = bodyContent;
                }
                else
                {
                    tempBodyContent = new Dictionary<string, dynamic>();
                }


                Dictionary<string, string> stringBodyContent = new Dictionary<string, string>();
                foreach (KeyValuePair<string, dynamic> kvp in tempBodyContent)
                {
                    string value;
                    if (kvp.Value != null)
                    {
                        value = kvp.Value.ToString();
                    }
                    else
                    {
                        value = string.Empty;
                    }
                    stringBodyContent.Add(kvp.Key, value);
                }



                var formContent = new FormUrlEncodedContent(stringBodyContent);
                Parameters = await formContent.ReadAsStringAsync(); // Convert bodyContent to a string for logging/debugging


                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(url),
                    Content = formContent
                };


                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
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

        public async Task<bool> CreateMember(string MemberAccount)
        {
            try
            {
                Dictionary<string, dynamic> paramobj = new Dictionary<string, dynamic>();
                paramobj.Add("MemberAccount", MemberAccount);
                string paramobjstr = JsonConvert.SerializeObject(paramobj, Formatting.None);
                this.Request = paramobjstr;
                string encryptstr = AesEncrypt(paramobjstr, this.AgentKey);
                string signstr = GetMD5Hash(paramobjstr);
                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Params", encryptstr);
                geek.Add("Sign", signstr);
                geek.Add("AgentCode", this.AgentCode);
                geek.Add("Currency", this.Currency);



                string endpoint = $"{this.APIUrl}​/AddMember";
                HttpResponseMessage response = await PostApiAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    CreateUserRes = JsonConvert.DeserializeObject<CreateUserResponse>(responseContent);
                    if (CreateUserRes.Result == 0)
                    {
                        return true;
                    }
                    else
                    { 
                        return false;                    
                    }
                    
                }
                else
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = ((ApiErrorCode)ErrorRes.Result).ToString();
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> GetDemoUrl(int GameID, int? LanguageID = 0, string HomeUrl = null, bool? IsLandscapeGame = false)
        {
            try
            {
                Dictionary<string, dynamic> paramobj = new Dictionary<string, dynamic>();
                paramobj.Add("GameID", GameID);
                if (LanguageID > 0) paramobj["LanguageID"] = LanguageID;
                if (!string.IsNullOrWhiteSpace(HomeUrl)) paramobj["HomeUrl"] = HomeUrl;
                if (IsLandscapeGame.HasValue) paramobj["IsLandscapeGame"] = IsLandscapeGame.Value;


                string paramobjstr = JsonConvert.SerializeObject(paramobj, Formatting.None);
                string encryptstr = AesEncrypt(paramobjstr, this.AgentKey);
                string signstr = GetMD5Hash(paramobjstr);
                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Params", encryptstr);
                geek.Add("Sign", signstr);
                geek.Add("AgentCode", this.AgentCode);
                geek.Add("Currency", this.Currency);



                string endpoint = $"{this.APIUrl}​/GetDemoUrl";
                HttpResponseMessage response = await PostApiAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    CreateUserRes = JsonConvert.DeserializeObject<CreateUserResponse>(responseContent);
                    
                    if (CreateUserRes.Result == 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = ((ApiErrorCode)ErrorRes.Result).ToString();
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> GameLogin(string MemberAccount, int GameID, bool LoginGameHall, int? LanguageID = 0, string HomeUrl = null,
                                   bool? JackpotStatus = null, List<int> GameHallGameType = null, List<string> Event = null,
                                   bool? CloseFeatureBuy = null, bool? IsLandscapeGame = null)
        {
            try
            {
                Dictionary<string, dynamic> paramobj = new Dictionary<string, dynamic>();
                paramobj.Add("MemberAccount", MemberAccount);
                paramobj.Add("GameID", GameID);
                paramobj.Add("LoginGameHall", LoginGameHall);

                if (LanguageID > 0) paramobj["LanguageID"] = LanguageID;
                if (!string.IsNullOrWhiteSpace(HomeUrl)) paramobj["HomeUrl"] = HomeUrl;
                if (JackpotStatus.HasValue) paramobj["JackpotStatus"] = JackpotStatus.Value;
                if (GameHallGameType != null && GameHallGameType.Any()) paramobj["GameHallGameType"] = GameHallGameType;
                if (Event != null && Event.Any()) paramobj["Event"] = Event;
                if (CloseFeatureBuy.HasValue) paramobj["CloseFeatureBuy"] = CloseFeatureBuy.Value;
                if (IsLandscapeGame.HasValue) paramobj["IsLandscapeGame"] = IsLandscapeGame.Value;

                string paramobjstr = JsonConvert.SerializeObject(paramobj, Formatting.None);
                this.Request = paramobjstr;
                string encryptstr = AesEncrypt(paramobjstr, this.AgentKey);
                string signstr = GetMD5Hash(paramobjstr);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Params", encryptstr);
                geek.Add("Sign", signstr);
                geek.Add("AgentCode", this.AgentCode);
                geek.Add("Currency", this.Currency);

                string endpoint = $"{this.APIUrl}/Login";
                HttpResponseMessage response = await PostApiAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    GameLoginRes = JsonConvert.DeserializeObject<GameLoginResponse>(responseContent);
                    
                    if (GameLoginRes.Result == 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = ((ApiErrorCode)ErrorRes.Result).ToString();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> KickoutMember(string MemberAccount)
        {
            try
            {
                Dictionary<string, dynamic> paramobj = new Dictionary<string, dynamic>();
                paramobj.Add("MemberAccount", MemberAccount);

                string paramobjstr = JsonConvert.SerializeObject(paramobj, Formatting.None);
                this.Request = paramobjstr;
                string encryptstr = AesEncrypt(paramobjstr, this.AgentKey);
                string signstr = GetMD5Hash(paramobjstr);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Params", encryptstr);
                geek.Add("Sign", signstr);
                geek.Add("AgentCode", this.AgentCode);
                geek.Add("Currency", this.Currency);

                string endpoint = $"{this.APIUrl}/KickOut";
                HttpResponseMessage response = await PostApiAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    KickoutMemberRes = JsonConvert.DeserializeObject<KickoutMemberResponse>(responseContent);
                   
                    if (KickoutMemberRes.Result == 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = ((ApiErrorCode)ErrorRes.Result).ToString();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }


        public async Task<bool> KickoutAllMember()
        {
            try
            {
                Dictionary<string, dynamic> paramobj = new Dictionary<string, dynamic>();

                string paramobjstr = JsonConvert.SerializeObject(paramobj, Formatting.None);
                string encryptstr = AesEncrypt(paramobjstr, this.AgentKey);
                string signstr = GetMD5Hash(paramobjstr);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Params", encryptstr);
                geek.Add("Sign", signstr);
                geek.Add("AgentCode", this.AgentCode);
                geek.Add("Currency", this.Currency);

                string endpoint = $"{this.APIUrl}/KickoutAll";
                HttpResponseMessage response = await PostApiAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    KickoutAllMemberRes = JsonConvert.DeserializeObject<KickoutAllMemberResponse>(responseContent);
                  
                    if (KickoutAllMemberRes.Result == 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = ((ApiErrorCode)ErrorRes.Result).ToString();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }


        public async Task<bool> QueryPlayerReport(string MemberAccount, int? LanguageID = null, int? ShowAccount = null, int? ShowPoints = null, int? GameType = null, string RecordID = null)
        {
            try
            {
                Dictionary<string, dynamic> paramobj = new Dictionary<string, dynamic>();
                paramobj.Add("MemberAccount", MemberAccount);
                if (LanguageID.HasValue) paramobj["LanguageID"] = LanguageID.Value;
                if (ShowAccount.HasValue) paramobj["ShowAccount"] = ShowAccount.Value;
                if (ShowPoints.HasValue) paramobj["ShowPoints"] = ShowPoints.Value;
                if (GameType.HasValue) paramobj["GameType"] = GameType.Value;
                if (!string.IsNullOrWhiteSpace(RecordID)) paramobj["RecordID"] = RecordID;

                string paramobjstr = JsonConvert.SerializeObject(paramobj, Formatting.None);
                string encryptstr = AesEncrypt(paramobjstr, this.AgentKey);
                string signstr = GetMD5Hash(paramobjstr);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Params", encryptstr);
                geek.Add("Sign", signstr);
                geek.Add("AgentCode", this.AgentCode);
                geek.Add("Currency", this.Currency);

                string endpoint = $"{this.APIUrl}/GetPlayerReport";
                HttpResponseMessage response = await PostApiAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    QueryPlayerReportRes = JsonConvert.DeserializeObject<QueryPlayerReportResponse>(responseContent);
                    
                    if (QueryPlayerReportRes.Result == 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = ((ApiErrorCode)ErrorRes.Result).ToString();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> DailyCurrencyReport(string Date)
        {
            try
            {
                Dictionary<string, dynamic> paramobj = new Dictionary<string, dynamic>();
                paramobj.Add("Date", Date);

                string paramobjstr = JsonConvert.SerializeObject(paramobj, Formatting.None);
                string encryptstr = AesEncrypt(paramobjstr, this.AgentKey);
                string signstr = GetMD5Hash(paramobjstr);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Params", encryptstr);
                geek.Add("Sign", signstr);
                geek.Add("AgentCode", this.AgentCode);
                geek.Add("Currency", this.Currency);

                string endpoint = $"{this.APIUrl}/GetCurrencyReport";
                HttpResponseMessage response = await PostApiAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    DailyCurrencyReportRes = JsonConvert.DeserializeObject<DailyCurrencyReportResponse>(responseContent);
                   
                    if (DailyCurrencyReportRes.Result == 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = ((ApiErrorCode)ErrorRes.Result).ToString();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }


        public async Task<bool> GetOnlineMember()
        {
            try
            {
                Dictionary<string, dynamic> paramobj = new Dictionary<string, dynamic>();

                string paramobjstr = JsonConvert.SerializeObject(paramobj, Formatting.None);
                string encryptstr = AesEncrypt(paramobjstr, this.AgentKey);
                string signstr = GetMD5Hash(paramobjstr);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Params", encryptstr);
                geek.Add("Sign", signstr);
                geek.Add("AgentCode", this.AgentCode);
                geek.Add("Currency", this.Currency);

                string endpoint = $"{this.APIUrl}/GetOnlineMember";
                HttpResponseMessage response = await PostApiAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    GetOnlineMemberRes = JsonConvert.DeserializeObject<GetOnlineMemberResponse>(responseContent);
                  
                    if (GetOnlineMemberRes.Result == 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = ((ApiErrorCode)ErrorRes.Result).ToString();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> GetRecordList(string StartDate, string EndDate)
        {
            try
            {
                Dictionary<string, dynamic> paramobj = new Dictionary<string, dynamic>();
                paramobj.Add("StartDate", StartDate);
                paramobj.Add("EndDate", EndDate);

                string paramobjstr = JsonConvert.SerializeObject(paramobj, Formatting.None);
                string encryptstr = AesEncrypt(paramobjstr, this.AgentKey);
                string signstr = GetMD5Hash(paramobjstr);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Params", encryptstr);
                geek.Add("Sign", signstr);
                geek.Add("AgentCode", this.AgentCode);
                geek.Add("Currency", this.Currency);

                string endpoint = $"{this.APIUrl}/GetRecordList";
                HttpResponseMessage response = await PostApiAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    GetRecordListRes = JsonConvert.DeserializeObject<GetRecordListResponse>(responseContent);
                    
                    if (GetRecordListRes.Result == 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = ((ApiErrorCode)ErrorRes.Result).ToString();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }


        public async Task<bool> MemberGameReport(string Date)
        {
            try
            {
                Dictionary<string, dynamic> paramobj = new Dictionary<string, dynamic>();
                paramobj.Add("Date", Date);

                string paramobjstr = JsonConvert.SerializeObject(paramobj, Formatting.None);
                string encryptstr = AesEncrypt(paramobjstr, this.AgentKey);
                string signstr = GetMD5Hash(paramobjstr);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Params", encryptstr);
                geek.Add("Sign", signstr);
                geek.Add("AgentCode", this.AgentCode);
                geek.Add("Currency", this.Currency);

                string endpoint = $"{this.APIUrl}/GetMemberGameReport";
                HttpResponseMessage response = await PostApiAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    MemberGameReportRes = JsonConvert.DeserializeObject<MemberGameReportResponse>(responseContent);
                   
                    if (MemberGameReportRes.Result == 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = ((ApiErrorCode)ErrorRes.Result).ToString();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }


        public async Task<bool> GetGameListandIcon()
        {
            try
            {
                Dictionary<string, dynamic> paramobj = new Dictionary<string, dynamic>();


                string paramobjstr = JsonConvert.SerializeObject(paramobj, Formatting.None);
                this.Request = paramobjstr;
                string encryptstr = AesEncrypt(paramobjstr, this.AgentKey);
                string signstr = GetMD5Hash(paramobjstr);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Params", encryptstr);
                geek.Add("Sign", signstr);
                geek.Add("AgentCode", this.AgentCode);
                geek.Add("Currency", this.Currency);

                string endpoint = $"{this.APIUrl}/GetGameIconList";
                HttpResponseMessage response = await PostApiAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    GetGameListandIconRes = JsonConvert.DeserializeObject<GetGameListandIconResponse>(responseContent);
                    
                    if (GetGameListandIconRes.Result == 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = ((ApiErrorCode)ErrorRes.Result).ToString();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> GetJackpotPoolAmount()
        {
            try
            {
                Dictionary<string, dynamic> paramobj = new Dictionary<string, dynamic>();


                string paramobjstr = JsonConvert.SerializeObject(paramobj, Formatting.None);
                string encryptstr = AesEncrypt(paramobjstr, this.AgentKey);
                string signstr = GetMD5Hash(paramobjstr);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Params", encryptstr);
                geek.Add("Sign", signstr);
                geek.Add("AgentCode", this.AgentCode);
                geek.Add("Currency", this.Currency);

                string endpoint = $"{this.APIUrl}/GetJackpotPool";
                HttpResponseMessage response = await PostApiAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    GetJackpotPoolAmountRes = JsonConvert.DeserializeObject<GetJackpotPoolAmountResponse>(responseContent);
                    
                    if (GetJackpotPoolAmountRes.Result == 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = ((ApiErrorCode)ErrorRes.Result).ToString();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }


        public async Task<bool> GetLotteryPrizeReport(string Date, string pageIndex = null, string pageSize = null)
        {
            try
            {
                Dictionary<string, dynamic> paramobj = new Dictionary<string, dynamic>();
                paramobj["Date"] = Date;
                if (!string.IsNullOrWhiteSpace(pageIndex)) paramobj["pageIndex"] = pageIndex;
                if (!string.IsNullOrWhiteSpace(pageSize)) paramobj["pageSize"] = pageSize;

                string paramobjstr = JsonConvert.SerializeObject(paramobj, Formatting.None);
                string encryptstr = AesEncrypt(paramobjstr, this.AgentKey);
                string signstr = GetMD5Hash(paramobjstr);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Params", encryptstr);
                geek.Add("Sign", signstr);
                geek.Add("AgentCode", this.AgentCode);
                geek.Add("Currency", this.Currency);

                string endpoint = $"{this.APIUrl}/GetLotteryPrizeReport";
                HttpResponseMessage response = await PostApiAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    GetLotteryPrizeReportRes = JsonConvert.DeserializeObject<GetLotteryPrizeReportResponse>(responseContent);
                    if (GetLotteryPrizeReportRes.Result == 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = ((ApiErrorCode)ErrorRes.Result).ToString();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> GetHistoryRecordList(string StartDate, string EndDate)
        {
            try
            {
                Dictionary<string, dynamic> paramobj = new Dictionary<string, dynamic>();
                paramobj["StartDate"] = StartDate;
                paramobj["EndDate"] = EndDate;

                string paramobjstr = JsonConvert.SerializeObject(paramobj, Formatting.None);
                string encryptstr = AesEncrypt(paramobjstr, this.AgentKey);
                string signstr = GetMD5Hash(paramobjstr);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Params", encryptstr);
                geek.Add("Sign", signstr);
                geek.Add("AgentCode", this.AgentCode);
                geek.Add("Currency", this.Currency);

                string endpoint = $"{this.APIUrl}/GetHistoryRecordList";
                HttpResponseMessage response = await PostApiAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    GetHistoryRecordListRes = JsonConvert.DeserializeObject<GetHistoryRecordListResponse>(responseContent);
                    
                    if (GetHistoryRecordListRes.Result == 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = ((ApiErrorCode)ErrorRes.Result).ToString();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> GetEventLink(string MemberAccount, string EventID, string EventType, int? LanguageID = 0, string HomeUrl = null, bool? JackpotStatus = null)
        {
            try
            {
                Dictionary<string, dynamic> paramobj = new Dictionary<string, dynamic>();
                paramobj["MemberAccount"] = MemberAccount;
                paramobj["EventID"] = EventID;
                paramobj["EventType"] = EventType;
                if (LanguageID > 0) paramobj["LanguageID"] = LanguageID;
                if (!string.IsNullOrWhiteSpace(HomeUrl)) paramobj["HomeUrl"] = HomeUrl;
                if (JackpotStatus.HasValue) paramobj["JackpotStatus"] = JackpotStatus.Value;

                string paramobjstr = JsonConvert.SerializeObject(paramobj, Formatting.None);
                string encryptstr = AesEncrypt(paramobjstr, this.AgentKey);
                string signstr = GetMD5Hash(paramobjstr);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Params", encryptstr);
                geek.Add("Sign", signstr);
                geek.Add("AgentCode", this.AgentCode);
                geek.Add("Currency", this.Currency);

                string endpoint = $"{this.APIUrl}/GetEventLink";
                HttpResponseMessage response = await PostApiAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    GetEventLinkRes = JsonConvert.DeserializeObject<GetEventLinkResponse>(responseContent);
                 

                    if (GetEventLinkRes.Result == 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = ((ApiErrorCode)ErrorRes.Result).ToString();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }



        public async Task<bool> SearchMemberInfo(string MemberAccount)
        {
            try
            {
                Dictionary<string, dynamic> paramobj = new Dictionary<string, dynamic>();
                paramobj.Add("MemberAccount", MemberAccount);
                string paramobjstr = JsonConvert.SerializeObject(paramobj, Formatting.None);
                this.Request = paramobjstr;
                string encryptstr = AesEncrypt(paramobjstr, this.AgentKey);
                string signstr = GetMD5Hash(paramobjstr);
                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Params", encryptstr);
                geek.Add("Sign", signstr);
                geek.Add("AgentCode", this.AgentCode);
                geek.Add("Currency", this.Currency);



                string endpoint = $"{this.APIUrl}​/SearchMember";
                HttpResponseMessage response = await PostApiAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    SearchMemberInfoRes = JsonConvert.DeserializeObject<SearchMemberInfoResponse>(responseContent);
                    
                    if (SearchMemberInfoRes.Result == 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = ((ApiErrorCode)ErrorRes.Result).ToString();
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> DepositAndWithdrawal(string MemberAccount, string TrsID = null, decimal? Points = null, int? AllOut = null)
        {
            try
            {
                Dictionary<string, dynamic> paramobj = new Dictionary<string, dynamic>();
                paramobj["MemberAccount"] = MemberAccount;
                if (!string.IsNullOrWhiteSpace(TrsID)) paramobj["TrsID"] = TrsID;
                if (Points.HasValue) paramobj["Points"] = Points.Value;
                if (AllOut.HasValue) paramobj["AllOut"] = AllOut.Value;

                string paramobjstr = JsonConvert.SerializeObject(paramobj, Formatting.None);
                this.Request = paramobjstr;
                string encryptstr = AesEncrypt(paramobjstr, this.AgentKey);
                string signstr = GetMD5Hash(paramobjstr);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Params", encryptstr);
                geek.Add("Sign", signstr);
                geek.Add("AgentCode", this.AgentCode);
                geek.Add("Currency", this.Currency);

                string endpoint = $"{this.APIUrl}/SetPoints";
                HttpResponseMessage response = await PostApiAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    SetPointsRes = JsonConvert.DeserializeObject<SetPointsResponse>(responseContent);
                  
                    if (SetPointsRes.Result == 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = ((ApiErrorCode)ErrorRes.Result).ToString();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }




        public async Task<bool> GetSingleBill(int? BankID = null, string TrsID = null)
        {
            try
            {
                Dictionary<string, dynamic> paramobj = new Dictionary<string, dynamic>();
                if (BankID.HasValue) paramobj["BankID"] = BankID.Value;
                if (!string.IsNullOrWhiteSpace(TrsID)) paramobj["TrsID"] = TrsID;

                string paramobjstr = JsonConvert.SerializeObject(paramobj, Formatting.None);
                string encryptstr = AesEncrypt(paramobjstr, this.AgentKey);
                string signstr = GetMD5Hash(paramobjstr);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Params", encryptstr);
                geek.Add("Sign", signstr);
                geek.Add("AgentCode", this.AgentCode);
                geek.Add("Currency", this.Currency);

                string endpoint = $"{this.APIUrl}/GetSingleBill";
                HttpResponseMessage response = await PostApiAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    GetSingleBillRes = JsonConvert.DeserializeObject<GetSingleBillResponse>(responseContent);
                  
                    if (GetSingleBillRes.Result == 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = ((ApiErrorCode)ErrorRes.Result).ToString();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }


        public async Task<bool> GetMemberPoints(List<string> MemberList)
        {
            try
            {
                Dictionary<string, dynamic> paramobj = new Dictionary<string, dynamic>();
                if (MemberList != null && MemberList.Count > 0) paramobj["MemberList"] = MemberList;

                string paramobjstr = JsonConvert.SerializeObject(paramobj, Formatting.None);
                string encryptstr = AesEncrypt(paramobjstr, this.AgentKey);
                string signstr = GetMD5Hash(paramobjstr);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Params", encryptstr);
                geek.Add("Sign", signstr);
                geek.Add("AgentCode", this.AgentCode);
                geek.Add("Currency", this.Currency);

                string endpoint = $"{this.APIUrl}/GetMemberPoints";
                HttpResponseMessage response = await PostApiAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    GetMemberPointsRes = JsonConvert.DeserializeObject<GetMemberPointsResponse>(responseContent);
                    
                    if (GetMemberPointsRes.Result == 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = ((ApiErrorCode)ErrorRes.Result).ToString();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }



        public async Task<bool> GetBillList(string StartDateTime, string EndDateTime, string MemberAccount = null)
        {
            try
            {
                Dictionary<string, dynamic> paramobj = new Dictionary<string, dynamic>();
                paramobj.Add("StartDateTime", StartDateTime);
                paramobj.Add("EndDateTime", EndDateTime);
                if (!string.IsNullOrWhiteSpace(MemberAccount)) paramobj["MemberAccount"] = MemberAccount;

                string paramobjstr = JsonConvert.SerializeObject(paramobj, Formatting.None);
                string encryptstr = AesEncrypt(paramobjstr, this.AgentKey);
                string signstr = GetMD5Hash(paramobjstr);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Params", encryptstr);
                geek.Add("Sign", signstr);
                geek.Add("AgentCode", this.AgentCode);
                geek.Add("Currency", this.Currency);

                string endpoint = $"{this.APIUrl}/GetBillList";
                HttpResponseMessage response = await PostApiAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    GetBillListRes = JsonConvert.DeserializeObject<GetBillListResponse>(responseContent);
                   
                    if (GetBillListRes.Result == 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = ((ApiErrorCode)ErrorRes.Result).ToString();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }


        public async Task<bool> CreateFreeSpin(
            string StartDate, string EndDate, string EventName, List<int> GameIDList,
            decimal MinWithdraw, decimal MaxWithdraw, int FreeSpinTimes, string JoinOption,
            int MaxMemberCount, string ZoneID = null, List<string> MemberList = null,
            string EventID = null, string bet = null, List<GiftCode> giftCodes = null)
        {
            try
            {
                Dictionary<string, dynamic> paramobj = new Dictionary<string, dynamic>();
                paramobj.Add("StartDate", StartDate);
                paramobj.Add("EndDate", EndDate);
                paramobj.Add("EventName", EventName);
                paramobj.Add("GameIDList", GameIDList);
                paramobj.Add("MinWithdraw", MinWithdraw);
                paramobj.Add("MaxWithdraw", MaxWithdraw);
                paramobj.Add("FreeSpinTimes", FreeSpinTimes);
                paramobj.Add("JoinOption", JoinOption);
                paramobj.Add("MaxMemberCount", MaxMemberCount);
                if (!string.IsNullOrWhiteSpace(ZoneID)) paramobj.Add("ZoneID", ZoneID);
                if (MemberList != null && MemberList.Any()) paramobj.Add("MemberList", MemberList);
                if (!string.IsNullOrWhiteSpace(EventID)) paramobj.Add("EventID", EventID);
                if (!string.IsNullOrWhiteSpace(bet)) paramobj.Add("bet", bet);
                if (giftCodes != null && giftCodes.Any()) paramobj.Add("giftCodes", giftCodes);

                string paramobjstr = JsonConvert.SerializeObject(paramobj, Formatting.None);
                string encryptstr = AesEncrypt(paramobjstr, this.AgentKey);
                string signstr = GetMD5Hash(paramobjstr);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Params", encryptstr);
                geek.Add("Sign", signstr);
                geek.Add("AgentCode", this.AgentCode);
                geek.Add("Currency", this.Currency);

                string endpoint = $"{this.APIUrl}/CreateFreeSpin";
                HttpResponseMessage response = await PostApiAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    CreateFreeSpinRes = JsonConvert.DeserializeObject<CreateFreeSpinResponse>(responseContent);
                   
                    if (CreateFreeSpinRes.Result == 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = ((ApiErrorCode)ErrorRes.Result).ToString();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }



        public async Task<bool> CancelFreeSpin(string EventID)
        {
            try
            {
                Dictionary<string, dynamic> paramobj = new Dictionary<string, dynamic>();
                paramobj.Add("EventID", EventID);

                string paramobjstr = JsonConvert.SerializeObject(paramobj, Formatting.None);
                string encryptstr = AesEncrypt(paramobjstr, this.AgentKey);
                string signstr = GetMD5Hash(paramobjstr);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Params", encryptstr);
                geek.Add("Sign", signstr);
                geek.Add("AgentCode", this.AgentCode);
                geek.Add("Currency", this.Currency);

                string endpoint = $"{this.APIUrl}/CancelFreeSpin";
                HttpResponseMessage response = await PostApiAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    CancelFreeSpinRes = JsonConvert.DeserializeObject<CancelFreeSpinResponse>(responseContent);
                  
                    if (CancelFreeSpinRes.Result == 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = ((ApiErrorCode)ErrorRes.Result).ToString();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }


        public async Task<bool> BindingMemberFreeSpin(string EventID, List<string> MemberList)
        {
            try
            {
                Dictionary<string, dynamic> paramobj = new Dictionary<string, dynamic>();
                paramobj.Add("EventID", EventID);
                paramobj.Add("MemberList", MemberList);

                string paramobjstr = JsonConvert.SerializeObject(paramobj, Formatting.None);
                string encryptstr = AesEncrypt(paramobjstr, this.AgentKey);
                string signstr = GetMD5Hash(paramobjstr);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Params", encryptstr);
                geek.Add("Sign", signstr);
                geek.Add("AgentCode", this.AgentCode);
                geek.Add("Currency", this.Currency);

                string endpoint = $"{this.APIUrl}/BindingMemberFreeSpin";
                HttpResponseMessage response = await PostApiAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    BindingMemberFreeSpinRes = JsonConvert.DeserializeObject<BindingMemberFreeSpinResponse>(responseContent);
                    
                    if (BindingMemberFreeSpinRes.Result == 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = ((ApiErrorCode)ErrorRes.Result).ToString();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }


        public async Task<bool> FreezeMemberFreeSpin(string EventID, string MemberAccount)
        {
            try
            {
                Dictionary<string, dynamic> paramobj = new Dictionary<string, dynamic>();
                paramobj.Add("EventID", EventID);
                paramobj.Add("MemberAccount", MemberAccount);

                string paramobjstr = JsonConvert.SerializeObject(paramobj, Formatting.None);
                string encryptstr = AesEncrypt(paramobjstr, this.AgentKey);
                string signstr = GetMD5Hash(paramobjstr);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Params", encryptstr);
                geek.Add("Sign", signstr);
                geek.Add("AgentCode", this.AgentCode);
                geek.Add("Currency", this.Currency);

                string endpoint = $"{this.APIUrl}/FreezeMemberFreeSpin";
                HttpResponseMessage response = await PostApiAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    FreezeMemberFreeSpinRes = JsonConvert.DeserializeObject<FreezeMemberFreeSpinResponse>(responseContent);
                    
                    if (FreezeMemberFreeSpinRes.Result == 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = ((ApiErrorCode)ErrorRes.Result).ToString();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> SearchMemberFreeSpin(string MemberAccount, string EventID)
        {
            try
            {
                Dictionary<string, dynamic> paramobj = new Dictionary<string, dynamic>();
                paramobj.Add("MemberAccount", MemberAccount);
                paramobj.Add("EventID", EventID);

                string paramobjstr = JsonConvert.SerializeObject(paramobj, Formatting.None);
                string encryptstr = AesEncrypt(paramobjstr, this.AgentKey);
                string signstr = GetMD5Hash(paramobjstr);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Params", encryptstr);
                geek.Add("Sign", signstr);
                geek.Add("AgentCode", this.AgentCode);
                geek.Add("Currency", this.Currency);

                string endpoint = $"{this.APIUrl}/SearchMemberFreeSpin";
                HttpResponseMessage response = await PostApiAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    SearchMemberFreeSpinRes = JsonConvert.DeserializeObject<SearchMemberFreeSpinResponse>(responseContent);
                  
                    if (SearchMemberFreeSpinRes.Result == 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = ((ApiErrorCode)ErrorRes.Result).ToString();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }



        public async Task<bool> GetActivityRecordList(string StartDate, string EndDate)
        {
            try
            {
                Dictionary<string, dynamic> paramobj = new Dictionary<string, dynamic>();
                paramobj.Add("StartDate", StartDate);
                paramobj.Add("EndDate", EndDate);

                string paramobjstr = JsonConvert.SerializeObject(paramobj, Formatting.None);
                string encryptstr = AesEncrypt(paramobjstr, this.AgentKey);
                string signstr = GetMD5Hash(paramobjstr);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Params", encryptstr);
                geek.Add("Sign", signstr);
                geek.Add("AgentCode", this.AgentCode);
                geek.Add("Currency", this.Currency);

                string endpoint = $"{this.APIUrl}/GetActivityRecordList";
                HttpResponseMessage response = await PostApiAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    GetActivityRecordListRes = JsonConvert.DeserializeObject<GetActivityRecordListResponse>(responseContent);
                    
                    if (GetActivityRecordListRes.Result == 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = ((ApiErrorCode)ErrorRes.Result).ToString();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }


        public static string AesEncrypt(string str, string key)
        {
            if (string.IsNullOrEmpty(str)) return null;
            Byte[] toEncryptArray = Encoding.UTF8.GetBytes(str);

            System.Security.Cryptography.RijndaelManaged rm = new System.Security.Cryptography.RijndaelManaged
            {
                Key = Encoding.UTF8.GetBytes(key),
                Mode = System.Security.Cryptography.CipherMode.ECB,
                Padding = System.Security.Cryptography.PaddingMode.PKCS7,
            };

            System.Security.Cryptography.ICryptoTransform cTransform = rm.CreateEncryptor();
            Byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }

        public static string AesDecrypt(string str, string key)
        {
            if (string.IsNullOrEmpty(str)) return null;
            Byte[] toEncryptArray = Convert.FromBase64String(str);

            System.Security.Cryptography.RijndaelManaged rm = new System.Security.Cryptography.RijndaelManaged
            {
                Key = Encoding.UTF8.GetBytes(key),
                Mode = System.Security.Cryptography.CipherMode.ECB,
                Padding = System.Security.Cryptography.PaddingMode.PKCS7
            };

            System.Security.Cryptography.ICryptoTransform cTransform = rm.CreateDecryptor();
            Byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

            return Encoding.UTF8.GetString(resultArray);
        }

        public static string GetMD5Hash(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                    sb.Append(b.ToString("x2"));

                return sb.ToString();
            }
        }


        public void Dispose()
        {
            throw new NotImplementedException();
        }


        #endregion

        #region Response Models
        public class ErrorResponse
        {

            public int Result { get; set; }
        }

        public class CreateUserResponse : ErrorResponse
        {

        }


        public class SearchMemberInfoResponse : ErrorResponse
        {

            public int OnlineType { get; set; }
            public decimal Points { get; set; }
        }


        public class GameLoginResponse : ErrorResponse
        {

            public string Url { get; set; }
        }

        public class KickoutMemberResponse : ErrorResponse
        {

        }

        public class KickoutAllMemberResponse : ErrorResponse
        {

        }

        public class QueryPlayerReportResponse : ErrorResponse
        {

            public string Url { get; set; }
        }



        public class DailyCurrencyReportResponse : ErrorResponse
        {

            public decimal Bet { get; set; }
            public int jptax { get; set; }
            public decimal inGameJptax { get; set; }
            public decimal Win { get; set; }
            public int jppoints { get; set; }
            public int inGameJppoints { get; set; }
            public decimal Winlose { get; set; }
            public int Round { get; set; }
            public decimal Refund { get; set; }
            public decimal ValidBet { get; set; }
            public int Commission { get; set; }
        }





        public class MemberList
        {
        }

        public class GetOnlineMemberResponse : ErrorResponse
        {

            public int count { get; set; }
            public List<MemberList> MemberList { get; set; }
        }


        public class Record
        {
            public decimal bet { get; set; }
            public decimal validBet { get; set; }
            public decimal prize { get; set; }
            public decimal refund { get; set; }
            public decimal winlose { get; set; }
            public int commission { get; set; }
            public decimal before { get; set; }
            public decimal after { get; set; }
            public int jptax { get; set; }
            public int jppoints { get; set; }
            public decimal inGameJptax { get; set; }
            public int inGameJppoints { get; set; }
            public string recordID { get; set; }
            public string account { get; set; }
            public int gameID { get; set; }
            public int gametype { get; set; }
            public int jpmode { get; set; }
            public int inGameJpmode { get; set; }
            public string bdate { get; set; }
            public bool isBuyFeature { get; set; }
            public int gameMode { get; set; }
        }

        public class GetRecordListResponse : ErrorResponse
        {

            public List<Record> Records { get; set; }
        }



        public class MemberGameReportResponseReport
        {
            public string account { get; set; }
            public int gameID { get; set; }
            public int gameType { get; set; }
            public decimal bet { get; set; }
            public int jptax { get; set; }
            public int inGameJptax { get; set; }
            public decimal win { get; set; }
            public int jppoints { get; set; }
            public int inGameJppoints { get; set; }
            public decimal netWin { get; set; }
            public int round { get; set; }
            public int betCount { get; set; }
            public decimal refund { get; set; }
            public decimal validBet { get; set; }
            public int commission { get; set; }
        }

        public class MemberGameReportResponse : ErrorResponse
        {

            public List<MemberGameReportResponseReport> Report { get; set; }
        }

        public class GameInfo
        {
            public string status { get; set; }
            public string gameNameOfChinese { get; set; }
            public string gameNameOfEnglish { get; set; }
            public string cnUrl { get; set; }
            public string enUrl { get; set; }
        }

        public class GetGameIconList
        {

            public Dictionary<string, GameInfo> fishing { get; set; }
            public Dictionary<string, GameInfo> arcade { get; set; }
            public Dictionary<string, GameInfo> slot { get; set; }
            public Dictionary<string, GameInfo> table { get; set; }
        }

        public class GetGameListandIconResponse : ErrorResponse
        {

            public GetGameIconList GetGameIconList { get; set; }
        }



        public class DATum
        {
            public int GRAND { get; set; }
            public decimal MAJOR { get; set; }
            public int MINI { get; set; }
            public string currency { get; set; }
            public List<int> gameIDList { get; set; }
        }

        public class GetJackpotPoolAmountResponse : ErrorResponse
        {

            public List<DATum> DATA { get; set; }
        }



        public class Datum
        {
            public string memberAccount { get; set; }
            public string eventID { get; set; }
            public string currency { get; set; }
            public int totalPrize { get; set; }
            public int count { get; set; }
        }

        public class GetLotteryPrizeReportResponse : ErrorResponse
        {

            public List<Datum> Data { get; set; }
            public int Total { get; set; }
        }



        public class GetHistoryRecordListResponseRecord
        {
            public decimal bet { get; set; }
            public decimal validBet { get; set; }
            public decimal prize { get; set; }
            public decimal refund { get; set; }
            public decimal winlose { get; set; }
            public int commission { get; set; }
            public decimal before { get; set; }
            public decimal after { get; set; }
            public int jptax { get; set; }
            public int jppoints { get; set; }
            public decimal inGameJptax { get; set; }
            public int inGameJppoints { get; set; }
            public string recordID { get; set; }
            public string account { get; set; }
            public int gameID { get; set; }
            public int gametype { get; set; }
            public int jpmode { get; set; }
            public int inGameJpmode { get; set; }
            public string bdate { get; set; }
            public bool isBuyFeature { get; set; }
            public int gameMode { get; set; }
        }

        public class GetHistoryRecordListResponse : ErrorResponse
        {

            public List<GetHistoryRecordListResponseRecord> Records { get; set; }
        }



        public class GetEventLinkResponse : ErrorResponse
        {

            public string EventUrl { get; set; }
        }


        public class SetPointsResponse : ErrorResponse
        {

            public int BankID { get; set; }
            public string TrsID { get; set; }
            public decimal AfterPoint { get; set; }
            public decimal Points { get; set; }
        }

        public class GetSingleBillResponse : ErrorResponse
        {

            public int BankID { get; set; }
            public string TrsID { get; set; }
            public string Action { get; set; }
            public decimal Points { get; set; }
            public string Account { get; set; }
            public int Status { get; set; }
            public decimal BeforePoints { get; set; }
            public decimal AfterPoints { get; set; }
            public string CDate { get; set; }
            public string BDate { get; set; }
            public string EventID { get; set; }
            public string Currency { get; set; }
        }

        public class GetMemberPointsResponse : ErrorResponse
        {

            public List<Dictionary<string, decimal>> MemberList { get; set; }
        }

        public class GetBillListResponse : ErrorResponse
        {

            public List<BankEntry> Bank { get; set; }
        }

        public class BankEntry
        {
            public int bankID { get; set; }
            public string trsID { get; set; }
            public decimal points { get; set; }
            public string account { get; set; }
            public decimal before { get; set; }
            public decimal after { get; set; }
            public string createDateTime { get; set; }
            public string eventID { get; set; }
            public string currency { get; set; }
        }

        public class CreateFreeSpinResponse : ErrorResponse
        {

            public string EventID { get; set; }
        }

        public class GiftCode
        {
            public string code { get; set; }
            public string limit { get; set; }
        }


        public class CancelFreeSpinResponse : ErrorResponse
        {

        }



        public class BindingMemberFreeSpinResponse : ErrorResponse
        {

        }



        public class FreezeMemberFreeSpinResponse : ErrorResponse
        {

        }

        public class SearchMemberFreeSpinResponse : ErrorResponse
        {

            public List<SearchMemberFreeSpinData> Data { get; set; }
        }

        public class SearchMemberFreeSpinData
        {
            public string EventID { get; set; }
            public int Prize { get; set; }
            public string Status { get; set; }
            public int TotalSpinTimes { get; set; }
            public int UsedSpinTimes { get; set; }
            public int TotalWin { get; set; }
            public string CreateTime { get; set; }
            public string ExpiredTime { get; set; }
        }



        public class GetActivityRecordListResponse : ErrorResponse
        {

            public List<ActivityRecord> Records { get; set; }
        }

        public class ActivityRecord
        {
            public double Bet { get; set; }
            public double Prize { get; set; }
            public double WithdrawalAmount { get; set; }
            public double WinLose { get; set; }
            public double Before { get; set; }
            public double After { get; set; }
            public string RecordID { get; set; }
            public string Account { get; set; }
            public int GameID { get; set; }
            public int GameType { get; set; }
            public string BDate { get; set; }
            public string EventID { get; set; }
        }



        #endregion
    }

    public class Test
    {
        public static string AesEncrypt(string str, string key)
        {
            if (string.IsNullOrEmpty(str)) return null;
            Byte[] toEncryptArray = Encoding.UTF8.GetBytes(str);

            System.Security.Cryptography.RijndaelManaged rm = new System.Security.Cryptography.RijndaelManaged
            {
                Key = Encoding.UTF8.GetBytes(key),
                Mode = System.Security.Cryptography.CipherMode.ECB,
                Padding = System.Security.Cryptography.PaddingMode.PKCS7,
            };

            System.Security.Cryptography.ICryptoTransform cTransform = rm.CreateEncryptor();
            Byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }

        public static string AesDecrypt(string str, string key)
        {
            if (string.IsNullOrEmpty(str)) return null;
            Byte[] toEncryptArray = Convert.FromBase64String(str);

            System.Security.Cryptography.RijndaelManaged rm = new System.Security.Cryptography.RijndaelManaged
            {
                Key = Encoding.UTF8.GetBytes(key),
                Mode = System.Security.Cryptography.CipherMode.ECB,
                Padding = System.Security.Cryptography.PaddingMode.PKCS7
            };

            System.Security.Cryptography.ICryptoTransform cTransform = rm.CreateDecryptor();
            Byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

            return Encoding.UTF8.GetString(resultArray);
        }

        public static string GetMD5Hash(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                    sb.Append(b.ToString("x2"));

                return sb.ToString();
            }
        }


        public static async Task Main(string[] args)
        {


            FCAPI api = new FCAPI(
               "https://api.fcg666.net",
               "A133",
               "MYR",
               "K22uGiVKWYqYZTVl"
           );


            // Console.WriteLine("Starting API Tests...");


            // Call API methods below:

            //if (await api.SearchMemberInfo("mitjenny123"))
            //{
            //    if (api.SearchMemberInfoRes.Result == 0)
            //    {
            //        Console.WriteLine("SearchMemberInfoRes successful!");
            //    }
            //    else
            //    {
            //        Console.WriteLine($"SearchMemberInfoRes failed: {((ApiErrorCode)api.SearchMemberInfoRes.Result).ToString()}");
            //    }

            //}
            //else
            //{
            //    Console.WriteLine($"SearchMemberInfoRes failed: {api.ErrorMessage}");
            //}


            //if (await api.KickoutAllMember())
            //{
            //    if (api.KickoutAllMemberRes.Result == 0)
            //    {
            //        Console.WriteLine("KickoutAllMember successful!");
            //    }
            //    else
            //    {
            //        Console.WriteLine($"KickoutAllMember failed: {((ApiErrorCode)api.KickoutAllMemberRes.Result).ToString()}");
            //    }

            //}
            //else
            //{
            //    Console.WriteLine($"KickoutAllMember failed: {api.ErrorMessage}");
            //}

            //if (await api.GetOnlineMember())
            //{
            //    if (api.GetOnlineMemberRes.Result == 0)
            //    {
            //        Console.WriteLine("GetOnlineMember successful!");
            //    }
            //    else
            //    {
            //        Console.WriteLine($"GetOnlineMember failed: {((ApiErrorCode)api.GetOnlineMemberRes.Result).ToString()}");
            //    }

            //}
            //else
            //{
            //    Console.WriteLine($"GetOnlineMember failed: {api.ErrorMessage}");
            //}


            //if (await api.GetGameListandIcon())
            //{
            //    if (api.GetGameListandIconRes.Result == 0)
            //    {
            //        Console.WriteLine("GetGameListandIcon successful!");
            //    }
            //    else
            //    {
            //        Console.WriteLine($"GetGameListandIcon failed: {((ApiErrorCode)api.GetGameListandIconRes.Result).ToString()}");
            //    }

            //}
            //else
            //{
            //    Console.WriteLine($"GetGameListandIcon failed: {api.ErrorMessage}");
            //}

            //if (await api.DepositAndWithdrawal("mitjenny123",TrsID:"TEST001",Points:100))
            //{
            //    if (api.SetPointsRes.Result == 0)
            //    {
            //        Console.WriteLine("DepositAndWithdrawal successful!");
            //    }
            //    else
            //    {
            //        Console.WriteLine($"DepositAndWithdrawal failed: {((ApiErrorCode)api.SetPointsRes.Result).ToString()}");
            //    }

            //}
            //else
            //{
            //    Console.WriteLine($"DepositAndWithdrawal failed: {api.ErrorMessage}");
            //}


            //if (await api.GameLogin("mitjenny123", 22053,false))
            //{
            //    if (api.SetPointsRes.Result == 0)
            //    {
            //        Console.WriteLine("DepositAndWithdrawal successful!");
            //    }
            //    else
            //    {
            //        Console.WriteLine($"DepositAndWithdrawal failed: {((ApiErrorCode)api.SetPointsRes.Result).ToString()}");
            //    }

            //}
            //else
            //{
            //    Console.WriteLine($"DepositAndWithdrawal failed: {api.ErrorMessage}");
            //}


            //if (await api.QueryPlayerReport("mitjenny123"))
            //{
            //    if (api.QueryPlayerReportRes.Result == 0)
            //    {
            //        Console.WriteLine("QueryPlayerReport successful!");
            //    }
            //    else
            //    {
            //        Console.WriteLine($"QueryPlayerReport failed: {((ApiErrorCode)api.QueryPlayerReportRes.Result).ToString()}");
            //    }

            //}
            //else
            //{
            //    Console.WriteLine($"GetHistoryRecordList failed: {api.ErrorMessage}");
            //}


            //if (await api.GetRecordList("2025-04-23 19:00:00", "2025-04-23 19:00:00"))
            //{
            //    if (api.GetRecordListRes.Result == 0)
            //    {
            //        Console.WriteLine("GetRecordList successful!");
            //    }
            //    else
            //    {
            //        Console.WriteLine($"GetRecordList failed: {((ApiErrorCode)api.GetRecordListRes.Result).ToString()}");
            //    }

            //}
            //else
            //{
            //    Console.WriteLine($"GetHistoryRecordList failed: {api.ErrorMessage}");
            //}


            //if (await api.GetHistoryRecordList("2025-04-23 06:55:00", "2025-04-23 06:56:00"))
            //{
            //    if (api.GetHistoryRecordListRes.Result == 0)
            //    {
            //        Console.WriteLine("GetHistoryRecordList successful!");
            //    }
            //    else
            //    {
            //        Console.WriteLine($"GetHistoryRecordList failed: {((ApiErrorCode)api.GetHistoryRecordListRes.Result).ToString()}");
            //    }

            //}
            //else
            //{
            //    Console.WriteLine($"GetHistoryRecordList failed: {api.ErrorMessage}");
            //}


            Console.WriteLine("API Tests Completed.");
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}
