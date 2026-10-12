using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace GameRecord.Gamess.PEGASUS
{
    public class PEGASUSAPI : IDisposable
    {
        #region Declaration
        public string APIUrl { get; set; }
        public string MerchantSecret { get; set; }
        public string MerchantID { get; set; }


        public string finalUrl { get; set; }
        public string Request { get; set; }
        public string Parameters { get; set; }
        public string Response { get; set; }
        public string ErrorMessage { get; set; }


        public ErrorResponse ErrorRes { get; set; }

        public CreatePlayerResponse CreatePlayerRes { get; set; }

        public CheckPlayerResponse CheckPlayerRes { get; set; }


        public GetGameLinkResponse GetGameLinkRes { get; set; }


        public PlayerLogoutResponse PlayerLogoutRes { get; set; }

        public CheckPlayerBalanceResponse CheckPlayerBalanceRes { get; set; }

        public DepositPlayerResponse DepositPlayerRes { get; set; }

        public WithdrawPlayerResponse WithdrawPlayerRes { get; set; }


        public QueryTransferRecordsResponse QueryTransferRecordsRes { get; set; }

        public QueryGameRecordsResponse QueryGameRecordsRes { get; set; }

        public GetbetRecordResponse GetbetRecordRes { get; set; }

        public GetScreenshotHistoryLinkResponse GetScreenshotHistoryLinkRes { get; set; }

        public GetGameRecordResponse GetGameRecordRes { get; set; }

        public GetPlayerWinloseRecordResponse GetPlayerWinloseRecordRes { get; set; }


        public GetGameListResponse GetGameListRes { get; set; }

        public GetPrizeRecordsResponse GetPrizeRecordsRes { get; set; }
        #endregion

        #region Constructor
        public PEGASUSAPI(string APIUrl, string MerchantSecret, string MerchantID)
        {
            this.APIUrl = APIUrl;
            this.MerchantSecret = MerchantSecret;
            this.MerchantID = MerchantID;

        }
        #endregion

        #region Public API Methods
        private async Task<HttpResponseMessage> GetApiAsync(string url, Dictionary<string, dynamic> bodyContent = null)
        {
            try
            {
                url= url.Replace("\u200B", "");
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

        public async Task<bool> GetGameLink(string playerName, int gameID, string lang = null, bool? isTrialModeV2 = null, string backUrl = null, string liveCasinoLevelID = null, int? userDevice = null)
        {
            try
            {


                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("playerName", playerName);
                geek.Add("merchantSecretKey", this.MerchantSecret);
                geek.Add("merchantID", this.MerchantID);
                geek.Add("gameID", gameID);

                if (!string.IsNullOrEmpty(lang)) geek.Add("lang", lang);
                if (isTrialModeV2.HasValue) geek.Add("isTrialModeV2", isTrialModeV2.Value);
                if (!string.IsNullOrEmpty(backUrl)) geek.Add("backUrl", backUrl);
                if (!string.IsNullOrEmpty(liveCasinoLevelID)) geek.Add("liveCasinoLevelID", liveCasinoLevelID);
                if (userDevice.HasValue) geek.Add("userDevice", userDevice.Value);

                string endpoint = $"{this.APIUrl}​/v1​/game​/link";
                HttpResponseMessage response = await GetApiAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    GetGameLinkRes = JsonConvert.DeserializeObject<GetGameLinkResponse>(responseContent);

                    if (GetGameLinkRes.code == 0)
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
                    ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.message, "");
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }



        public async Task<bool> CheckPlayerExist(string playerName)
        {
            try
            {


                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("playerName", playerName);
                geek.Add("merchantSecretKey", this.MerchantSecret);
                geek.Add("merchantID", this.MerchantID);

                string endpoint = $"{this.APIUrl}/v1/player/exist";
                HttpResponseMessage response = await GetApiAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    CheckPlayerRes = JsonConvert.DeserializeObject<CheckPlayerResponse>(responseContent);
                   
                    if (CheckPlayerRes.code == 0)
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
                    ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.message, "");
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> CreatePlayer(string playerName)
        {
            try
            {

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>
                            {
                                { "playerName", playerName },
                                { "merchantSecretKey", this.MerchantSecret },
                                { "merchantID", this.MerchantID }
                            };

                string endpoint = $"{this.APIUrl}/v1/player";
                HttpResponseMessage response = await PostApiInJsonAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    CreatePlayerRes = JsonConvert.DeserializeObject<CreatePlayerResponse>(responseContent);
                   

                    if (CreatePlayerRes.code == 0)
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
                    ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.message, "");
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> PlayerLogout(string playerName)
        {
            try
            {

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>
                            {
                                { "playerName", playerName },
                                { "merchantSecretKey", this.MerchantSecret },
                                { "merchantID", this.MerchantID }
                            };

                string endpoint = $"{this.APIUrl}/v1/player/logout";
                HttpResponseMessage response = await PostApiInJsonAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    PlayerLogoutRes = JsonConvert.DeserializeObject<PlayerLogoutResponse>(responseContent);
                    
                    if (PlayerLogoutRes.code == 0)
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
                    ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.message, "");
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }



        public async Task<bool> CheckPlayerBalance(string playerName, bool? isIncludeExtraInfo = null)
        {
            try
            {


                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("playerName", playerName);
                geek.Add("merchantSecretKey", this.MerchantSecret);
                geek.Add("merchantID", this.MerchantID);
                if (isIncludeExtraInfo.HasValue) geek.Add("isIncludeExtraInfo", isIncludeExtraInfo.Value);

                string endpoint = $"{this.APIUrl}/v1/wallet/amount";
                HttpResponseMessage response = await GetApiAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    CheckPlayerBalanceRes = JsonConvert.DeserializeObject<CheckPlayerBalanceResponse>(responseContent);
                    
                    if (CheckPlayerBalanceRes.code == 0)
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
                    ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.message, "");
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }


        public async Task<bool> DepositPlayer(decimal amount, string merchantOrderNo, string playerName)
        {
            try
            {

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>
                            {
                                { "amount", amount },
                                { "merchantOrderNo", merchantOrderNo },
                                 { "playerName", playerName },
                                { "merchantSecretKey", this.MerchantSecret },
                                { "merchantID", this.MerchantID }
                            };

                string endpoint = $"{this.APIUrl}/v1/wallet/transfer-in";
                HttpResponseMessage response = await PostApiInJsonAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    DepositPlayerRes = JsonConvert.DeserializeObject<DepositPlayerResponse>(responseContent);
                    
                    if (DepositPlayerRes.code == 0)
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
                    ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.message, "");
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }


        public async Task<bool> WithdrawPlayer(decimal amount, string merchantOrderNo, string playerName)
        {
            try
            {

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>
                            {
                                { "amount", amount },
                                { "merchantOrderNo", merchantOrderNo },
                                 { "playerName", playerName },
                                { "merchantSecretKey", this.MerchantSecret },
                                { "merchantID", this.MerchantID }
                            };

                string endpoint = $"{this.APIUrl}/v1/wallet/transfer-out";
                HttpResponseMessage response = await PostApiInJsonAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    WithdrawPlayerRes = JsonConvert.DeserializeObject<WithdrawPlayerResponse>(responseContent);
                    
                    if (WithdrawPlayerRes.code == 0)
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
                    ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.message, "");
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }


        public async Task<bool> QueryTransferRecords(string pGSTransactionNo = null, string merchantTransactionNo = null)
        {
            try
            {


                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("merchantSecretKey", this.MerchantSecret);
                geek.Add("merchantID", this.MerchantID);

                if (!string.IsNullOrEmpty(pGSTransactionNo)) geek.Add("pGSTransactionNo", pGSTransactionNo);
                if (!string.IsNullOrEmpty(merchantTransactionNo)) geek.Add("merchantTransactionNo", merchantTransactionNo);

                string endpoint = $"{this.APIUrl}/v1/transfer";
                HttpResponseMessage response = await GetApiAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    QueryTransferRecordsRes = JsonConvert.DeserializeObject<QueryTransferRecordsResponse>(responseContent);
                    
                    if (QueryTransferRecordsRes.code == 0)
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
                    ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.message, "");
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }


        public async Task<bool> QueryGameRecords(string startTime, string endTime, int pageIndex = 1, int pageSize = 10000, string playerName = null, bool? isSearchExtraDetail = null, bool? isExchangeToOriginalRate = null, int? tagID = null)
        {
            try
            {


                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>
                    {
                        { "merchantSecretKey", this.MerchantSecret },
                        { "merchantID", this.MerchantID },
                        { "startTime", startTime },
                        { "endTime", endTime },
                        { "pageIndex", pageIndex },
                        { "pageSize", pageSize }
                    };

                if (!string.IsNullOrEmpty(playerName)) geek.Add("playerName", playerName);
                if (isSearchExtraDetail.HasValue) geek.Add("isSearchExtraDetail", isSearchExtraDetail.Value);
                if (isExchangeToOriginalRate.HasValue) geek.Add("isExchangeToOriginalRate", isExchangeToOriginalRate.Value);
                if (tagID.HasValue) geek.Add("tagID", tagID.Value);



                string endpoint = $"{this.APIUrl}/v1/game-records";
                HttpResponseMessage response = await GetApiAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    QueryGameRecordsRes = JsonConvert.DeserializeObject<QueryGameRecordsResponse>(responseContent);
                    


                    if (QueryGameRecordsRes.code == 0)
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
                    ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.message, "");
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> GetbetRecord(string transactionNo)
        {
            try
            {


                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>
                    {
                        { "merchantSecretKey", this.MerchantSecret },
                        { "merchantID", this.MerchantID },
                        { "transactionNo", transactionNo }
                    };



                string endpoint = $"{this.APIUrl}/v1/game-records/transactionNo";
                HttpResponseMessage response = await GetApiAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    GetbetRecordRes = JsonConvert.DeserializeObject<GetbetRecordResponse>(responseContent);
                    
                    if (GetbetRecordRes.code == 0)
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
                    ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.message, "");
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> GetScreenshotHistoryLink(string transactionNo, string betID = null)
        {
            try
            {


                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>
                    {
                        { "merchantSecretKey", this.MerchantSecret },
                        { "merchantID", this.MerchantID },
                        { "transactionNo", transactionNo }
                    };

                if (!string.IsNullOrEmpty(betID)) geek.Add("betID", betID);





                string endpoint = $"{this.APIUrl}/v1/game-records/screenshot-history/link";
                HttpResponseMessage response = await GetApiAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    GetScreenshotHistoryLinkRes = JsonConvert.DeserializeObject<GetScreenshotHistoryLinkResponse>(responseContent);
                  
                    if (GetScreenshotHistoryLinkRes.code == 0)
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
                    ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.message, "");
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }


        public async Task<bool> GetGameRecord(string startTime, string endTime, bool? isExchangeToOriginalRate = null)
        {
            try
            {


                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>
                    {
                        { "merchantSecretKey", this.MerchantSecret },
                        { "merchantID", this.MerchantID },
                        { "startTime", startTime },
                        { "endTime", endTime }
                    };
                if (isExchangeToOriginalRate.HasValue) geek.Add("isExchangeToOriginalRate", isExchangeToOriginalRate.Value);


                string endpoint = $"{this.APIUrl}/v1/game-records/report";
                HttpResponseMessage response = await GetApiAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    GetGameRecordRes = JsonConvert.DeserializeObject<GetGameRecordResponse>(responseContent);
                   

                    if (GetGameRecordRes.code == 0)
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
                    ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.message, "");
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }


        public async Task<bool> GetPlayerWinloseRecord(string startTime, string endTime, int pageIndex = 1, int pageSize = 10000, int? gameID = null, string playerName = null, bool? isExchangeToOriginalRate = null)
        {
            try
            {


                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>
                    {
                        { "merchantSecretKey", this.MerchantSecret },
                        { "merchantID", this.MerchantID },
                        { "startTime", startTime },
                        { "endTime", endTime },
                        { "pageIndex", pageIndex },
                        { "pageSize", pageSize }
                    };

                if (gameID.HasValue) geek.Add("gameID", gameID);
                if (!string.IsNullOrEmpty(playerName)) geek.Add("playerName", playerName);
                if (isExchangeToOriginalRate.HasValue) geek.Add("isExchangeToOriginalRate", isExchangeToOriginalRate);


                string endpoint = $"{this.APIUrl}/v1/game-records/player-win-lose-report";
                HttpResponseMessage response = await GetApiAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    GetPlayerWinloseRecordRes = JsonConvert.DeserializeObject<GetPlayerWinloseRecordResponse>(responseContent);
                    
                    if (GetPlayerWinloseRecordRes.code == 0)
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
                    ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.message, "");
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }


        public async Task<bool> GetGameList(int? specialOrderSequence = null)
        {
            try
            {


                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>
                    {
                        { "merchantSecretKey", this.MerchantSecret },
                        { "merchantID", this.MerchantID }
                    };

                if (specialOrderSequence.HasValue) geek.Add("specialOrderSequence", specialOrderSequence);


                string endpoint = $"{this.APIUrl}/v1/games";
                HttpResponseMessage response = await GetApiAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    GetGameListRes = JsonConvert.DeserializeObject<GetGameListResponse>(responseContent);
                   
                    if (GetGameListRes.code == 0)
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
                    ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.message, "");
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> GetPrizeRecords(string startTime, string endTime, int pageIndex = 1, int pageSize = 10000, string playerName = null, bool? isFilterByCreateTime = null, bool? isExchangeToOriginalRate = null)
        {
            try
            {


                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>
                    {
                        { "merchantSecretKey", this.MerchantSecret },
                        { "merchantID", this.MerchantID },
                        { "startTime", startTime },
                        { "endTime", endTime },
                        { "pageIndex", pageIndex },
                        { "pageSize", pageSize }
                    };

                if (!string.IsNullOrEmpty(playerName)) geek.Add("playerName", playerName);
                if (isFilterByCreateTime.HasValue) geek.Add("isFilterByCreateTime", isFilterByCreateTime);
                if (isExchangeToOriginalRate.HasValue) geek.Add("isExchangeToOriginalRate", isExchangeToOriginalRate);


                string endpoint = $"{this.APIUrl}/v1/event/prize-records";
                HttpResponseMessage response = await GetApiAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    GetPrizeRecordsRes = JsonConvert.DeserializeObject<GetPrizeRecordsResponse>(responseContent);
                    
                    if (GetPrizeRecordsRes.code == 0)
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
                    ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.message, "");
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }


        public void Dispose()
        {
            throw new NotImplementedException();
        }


        #endregion

    }


    #region Response Models

    public class BaseResponse
    {
        public int code { get; set; }
        public string message { get; set; }

    }

    public class ErrorData
    {
        public string DebugExtraInfo { get; set; }
        public string DebugExtraInfo2 { get; set; }
    }

    public class ErrorResponse : BaseResponse
    {
        public ErrorData data { get; set; }
    }


    public class CreatePlayerData
    {
    }

    public class CreatePlayerResponse : BaseResponse
    {
        public CreatePlayerData data { get; set; }
    }


    public class CheckPlayerData
    {
        public bool isExist { get; set; }
        public object createTime { get; set; }
    }

    public class CheckPlayerResponse : BaseResponse
    {
        public CheckPlayerData data { get; set; }
    }

    public class GetGameLinkData
    {
        public string link { get; set; }
    }


    public class GetGameLinkResponse : BaseResponse
    {
        public GetGameLinkData data { get; set; }
    }


    public class PlayerLogoutData
    {
        public string playerName { get; set; }
    }

    public class PlayerLogoutResponse : BaseResponse
    {
        public PlayerLogoutData data { get; set; }
    }

    public class CheckPlayerBalanceData
    {
        public string currency { get; set; }
        public decimal amount { get; set; }
        public decimal amountWithConvert { get; set; }
        public decimal convertRate { get; set; }
        public ExtraInfo extraInfo { get; set; }
    }

    public class ExtraInfo
    {
        public bool isTransferLock { get; set; }
        public string playerName { get; set; }
    }

    public class CheckPlayerBalanceResponse : BaseResponse
    {
        public CheckPlayerBalanceData data { get; set; }
    }


    public class DepositPlayerData
    {
        public string transactionNo { get; set; }
        public decimal amount { get; set; }
        public decimal convertRate { get; set; }
        public decimal beforeAmount { get; set; }
        public decimal afterAmount { get; set; }
    }


    public class DepositPlayerResponse : BaseResponse
    {
        public DepositPlayerData data { get; set; }
    }

    public class WithdrawPlayerData
    {
        public string transactionNo { get; set; }
        public decimal amount { get; set; }
        public decimal convertRate { get; set; }
        public decimal beforeAmount { get; set; }
        public decimal afterAmount { get; set; }
    }


    public class WithdrawPlayerResponse : BaseResponse
    {
        public WithdrawPlayerData data { get; set; }
    }


    public class QueryTransferRecordsData
    {
        public string pgsTransactionNo { get; set; }
        public string merchantTransactionNo { get; set; }
        public int transferType { get; set; }
        public int playerID { get; set; }
        public string playerName { get; set; }
        public decimal amount { get; set; }
        public decimal beforeAmount { get; set; }
        public decimal afterAmount { get; set; }
        public decimal convertRate { get; set; }
        public DateTime createTime { get; set; }
    }

    public class QueryTransferRecordsResponse : BaseResponse
    {
        public QueryTransferRecordsData data { get; set; }
    }


    public class QueryGameRecordsData
    {
        public List<QueryGameRecordsDataItem> items { get; set; }
        public int pageIndex { get; set; }
        public int pageSize { get; set; }
        public int totalCount { get; set; }
        public int totalPage { get; set; }
        public string extraObject { get; set; }
        public string debugInfo { get; set; }
    }

    public class QueryGameRecordsDataExtraInfo
    {
    }

    public class QueryGameRecordsDataItem
    {
        public string playerName { get; set; }
        public int gameID { get; set; }
        public decimal winLossAmount { get; set; }
        public string roundID { get; set; }
        public string transactionNo { get; set; }
        public string betID { get; set; }
        public DateTime betTime { get; set; }
        public decimal betAmount { get; set; }
        public decimal payoutAmount { get; set; }
        public decimal taxAmount { get; set; }
        public decimal validAmount { get; set; }
        public QueryGameRecordsDataExtraInfo extraInfo { get; set; }
        public decimal beforeAmount { get; set; }
        public DateTime roundStartTime { get; set; }
        public DateTime roundEndTime { get; set; }
    }
    public class QueryGameRecordsResponse : BaseResponse
    {

        public QueryGameRecordsData data { get; set; }
    }


    public class GetbetRecordData
    {
        public int gameID { get; set; }
        public string gameName { get; set; }
        public string playerName { get; set; }
        public int status { get; set; }
        public string statusText { get; set; }
        public string transactionNo { get; set; }
        public DateTime betTime { get; set; }
        public decimal betAmount { get; set; }
        public decimal payoutAmount { get; set; }
        public decimal validAmount { get; set; }
        public decimal winLossAmount { get; set; }
        public decimal taxAmount { get; set; }
        public DateTime settlementTime { get; set; }
    }

    public class GetbetRecordResponse : BaseResponse
    {

        public GetbetRecordData data { get; set; }
    }


    public class GetScreenshotHistoryLinkData
    {
        public string url { get; set; }
    }


    public class GetScreenshotHistoryLinkResponse : BaseResponse
    {

        public GetScreenshotHistoryLinkData data { get; set; }
    }

    public class GetGameRecordData
    {
        public List<GetGameRecordDataGameSummary> gameSummary { get; set; }
        public int totalPromotionBonus { get; set; }
        public int totalRound { get; set; }
        public int totalWinLossAmount { get; set; }
        public int totalValidAmount { get; set; }
        public int totalPayoutAmount { get; set; }
        public int totalBetAmount { get; set; }
        public int totalTaxAmount { get; set; }
    }

    public class GetGameRecordDataGameSummary
    {
        public int gameID { get; set; }
        public int totalRound { get; set; }
        public int totalWinLossAmount { get; set; }
        public int totalValidAmount { get; set; }
        public int totalPayoutAmount { get; set; }
        public int totalBetAmount { get; set; }
        public int totalTaxAmount { get; set; }
    }

    public class GetGameRecordResponse : BaseResponse
    {

        public GetGameRecordData data { get; set; }
    }


    public class GetPlayerWinloseRecordData
    {
        public List<GetPlayerWinloseRecordItem> items { get; set; }
        public int pageIndex { get; set; }
        public int pageSize { get; set; }
        public int totalCount { get; set; }
        public int totalPage { get; set; }
        public string extraObject { get; set; }
        public string debugInfo { get; set; }
    }

    public class GetPlayerWinloseRecordItem
    {
        public int playerID { get; set; }
        public string playerName { get; set; }
        public int gameID { get; set; }
        public string gameName { get; set; }
        public decimal totalRound { get; set; }
        public decimal betAmount { get; set; }
        public decimal payoutAmount { get; set; }
        public decimal winLossAmount { get; set; }
        public decimal rtp { get; set; }
    }


    public class GetPlayerWinloseRecordResponse : BaseResponse
    {

        public GetPlayerWinloseRecordData data { get; set; }
    }


    public class Datum
    {
        public int gameID { get; set; }
        public string gameCode { get; set; }
        public string gameName { get; set; }
        public string imageUrl { get; set; }
        public string imageUrl_H { get; set; }
        public int gameType { get; set; }
        public string gameTypeName { get; set; }
        public bool isPrePaidMode { get; set; }
        public bool isLaunchAvailable { get; set; }
        public string supportedOrientation { get; set; }
        public List<GameNameMapping> gameNameMappings { get; set; }
    }

    public class GameNameMapping
    {
        public string languageCode { get; set; }
        public string text { get; set; }
    }


    public class GetGameListResponse : BaseResponse
    {
        public List<Datum> data { get; set; }

    }



    public class GetPrizeRecordsData
    {
        public List<GetPrizeRecordsDataItem> items { get; set; }
        public int pageIndex { get; set; }
        public int pageSize { get; set; }
        public int totalCount { get; set; }
        public int totalPage { get; set; }
        public string extraObject { get; set; }
        public string debugInfo { get; set; }
    }

    public class GetPrizeRecordsDataItem
    {
        public int eventPrizeRecordID { get; set; }
        public int playerID { get; set; }
        public string playerName { get; set; }
        public int eventID { get; set; }
        public string eventName { get; set; }
        public int eventPrizeType { get; set; }
        public string eventPrizeTypeText { get; set; }
        public int quantityOrReferenceRatio { get; set; }
        public decimal prizeCashAmount { get; set; }
        public int status { get; set; }
        public string statusText { get; set; }
        public string memo { get; set; }
        public DateTime createTime { get; set; }
        public DateTime receiveTime { get; set; }
        public string prizeRecordTransactionNo { get; set; }
        public int gameID { get; set; }
        public string gameName { get; set; }
    }

    public class GetPrizeRecordsResponse : BaseResponse
    {

        public GetPrizeRecordsData data { get; set; }
    }

    #endregion

    public class Test
    {
        public static async Task Main(string[] args)
        {
            PEGASUSAPI api = new PEGASUSAPI(
               "https://vendor-api.888star.xyz",
               "0b228abc6c70455",
               "181897"
           );

            string playerName = "testPlayerXYZ789";
            int gameId = 1001;
            double amount = 10.00;
            string pgsTransactionNo = "PGS_TXN_12345";
            string merchantTransactionNo = "MERCHANT_TXN_67890";
            string gameRecordTransactionNo = "GAME_REC_TXN_ABCDE";
            string betId = "BETID_FGHIJ";
            string merchantTxnId = "";
            string transactionNo = "";

            DateTime endTime = DateTime.UtcNow;
            DateTime startTime = endTime.AddDays(-1);
            string startTimeStr = FormatDate(startTime);
            string endTimeStr = FormatDate(endTime);

            string FormatDate(DateTime dt) => dt.ToString("yyyy-MM-dd HH:mm:ss");

            Console.WriteLine("Starting API Tests...");


            // Call API methods below:

            //if (await api.CheckPlayerExist("mitjenny123"))
            //{
            //    if (api.CheckPlayerRes.code == 0)
            //    {
            //        Console.WriteLine("CheckPlayerRes successful!");
            //    }
            //    else
            //    {
            //        Console.WriteLine($"CheckPlayerRes failed: {api.CheckPlayerRes.message}");
            //    }

            //}
            //else
            //{
            //    Console.WriteLine($"CheckPlayerRes failed: {api.ErrorMessage}");
            //}


            //if (await api.CreatePlayer("mitjenny123"))
            //{
            //    if (api.CreatePlayerRes.code == 0)
            //    {
            //        Console.WriteLine("CreatePlayer successful!");
            //    }
            //    else
            //    {
            //        Console.WriteLine($"CreatePlayer failed: {api.CreatePlayerRes.message}");
            //    }

            //}
            //else
            //{
            //    Console.WriteLine($"CreateUser failed: {api.ErrorMessage}");
            //}



            //if (await api.GetGameLink("mitjenny", 290121, lang: "en"))
            //{
            //    if (api.GetGameLinkRes.code == 0)
            //    {
            //        Console.WriteLine("GetGameLink successful!");

            //    }
            //    else
            //    {
            //        Console.WriteLine($"GetGameLink failed: {api.GetGameLinkRes.message}");
            //    }
            //}
            //else
            //{
            //    Console.WriteLine($"GetGameLink failed: {api.ErrorMessage}");
            //}



            //        if (await api.PlayerLogout(playerName))
            //        {
            //            if (api.PlayerLogoutRes.code == 0)
            //            {
            //                Console.WriteLine("PlayerLogout successful!");
            //            }
            //            else
            //            {
            //                Console.WriteLine($"PlayerLogout failed: {api.PlayerLogoutRes.message}");
            //            }
            //        }
            //        else
            //        {
            //            Console.WriteLine($"PlayerLogout failed: {api.ErrorMessage}");
            //        }



            //        if (await api.CheckPlayerBalance(playerName, isIncludeExtraInfo: true))
            //        {
            //            if (api.CheckPlayerBalanceRes.code == 0)
            //            {
            //                Console.WriteLine("CheckPlayerBalance successful!");

            //            }
            //            else
            //            {
            //                Console.WriteLine($"CheckPlayerBalance failed: {api.CheckPlayerBalanceRes.message}");
            //            }
            //        }
            //        else
            //        {
            //            Console.WriteLine($"CheckPlayerBalance failed: {api.ErrorMessage}");
            //        }

            //string timestamp = DateTime.Now.ToString("yyyyMMddHHmm");

            //if (await api.DepositPlayer(1000, "AZ" + timestamp, "mitjenny"))
            //{
            //    if (api.DepositPlayerRes.code == 0)
            //    {
            //        Console.WriteLine("DepositPlayer successful!");

            //    }
            //    else
            //    {
            //        Console.WriteLine($"DepositPlayer failed: {api.DepositPlayerRes.message}");
            //    }
            //}
            //else
            //{
            //    Console.WriteLine($"DepositPlayer failed: {api.ErrorMessage}");
            //}


            //if (await api.WithdrawPlayer(50, "ZA" + timestamp, "mitjenny"))
            //{
            //    if (api.WithdrawPlayerRes.code == 0)
            //    {
            //        Console.WriteLine("WithdrawPlayer successful!");

            //    }
            //    else
            //    {
            //        Console.WriteLine($"WithdrawPlayer failed: {api.WithdrawPlayerRes.message}");
            //    }
            //}
            //else
            //{
            //    Console.WriteLine($"WithdrawPlayer failed: {api.ErrorMessage}");
            //}




            //        if (await api.QueryTransferRecords())
            //        {
            //            if (api.QueryTransferRecordsRes.code == 0)
            //            {
            //                Console.WriteLine("QueryTransferRecordsByMerchantTxn successful!");

            //            }
            //            else
            //            {
            //                Console.WriteLine($"QueryTransferRecordsByMerchantTxn failed: {api.QueryTransferRecordsRes.message}");
            //            }
            //        }
            //        else
            //        {
            //            Console.WriteLine($"QueryTransferRecordsByMerchantTxn failed: {api.ErrorMessage}");
            //        }




            //        if (await api.QueryTransferRecords())
            //        {
            //            if (api.QueryTransferRecordsRes.code == 0)
            //            {
            //                Console.WriteLine("QueryTransferRecordsByPgsTxn successful!");

            //            }
            //            else
            //            {
            //                Console.WriteLine($"QueryTransferRecordsByPgsTxn failed: {api.QueryTransferRecordsRes.message}");
            //            }
            //        }
            //        else
            //        {
            //            Console.WriteLine($"QueryTransferRecordsByPgsTxn failed: {api.ErrorMessage}");
            //        }





            if (await api.QueryGameRecords("2025-04-03 08:00:00", "2025-04-03 17:00:00"))
            {
                if (api.QueryGameRecordsRes.code == 0)
                {
                    Console.WriteLine("QueryGameRecords successful!");

                }
                else
                {
                    Console.WriteLine($"QueryGameRecords failed: {api.QueryGameRecordsRes.message}");
                }
            }
            else
            {
                Console.WriteLine($"QueryGameRecords failed: {api.ErrorMessage}");
            }

            //        if (await api.GetbetRecord(transactionNo))
            //        {
            //            if (api.GetbetRecordRes.code == 0)
            //            {
            //                Console.WriteLine("GetBetRecord successful!");

            //            }
            //            else
            //            {
            //                Console.WriteLine($"GetBetRecord failed: {api.GetbetRecordRes.message}");
            //            }
            //        }
            //        else
            //        {
            //            Console.WriteLine($"GetBetRecord failed: {api.ErrorMessage}");
            //        }




            //        if (await api.GetScreenshotHistoryLink(transactionNo))
            //        {
            //            if (api.GetScreenshotHistoryLinkRes.code == 0)
            //            {
            //                Console.WriteLine("GetScreenshotHistoryLink successful!");

            //            }
            //            else
            //            {
            //                Console.WriteLine($"GetScreenshotHistoryLink failed: {api.GetScreenshotHistoryLinkRes.message}");
            //            }
            //        }
            //        else
            //        {
            //            Console.WriteLine($"GetScreenshotHistoryLink failed: {api.ErrorMessage}");
            //        }



            //        if (await api.GetGameRecord(startTimeStr, endTimeStr))
            //        {
            //            if (api.GetGameRecordRes.code == 0)
            //            {
            //                Console.WriteLine("GetGameRecordReport successful!");

            //            }
            //            else
            //            {
            //                Console.WriteLine($"GetGameRecordReport failed: {api.GetGameRecordRes.message}");
            //            }
            //        }
            //        else
            //        {
            //            Console.WriteLine($"GetGameRecordReport failed: {api.ErrorMessage}");
            //        }





            //        if (await api.GetPlayerWinloseRecord(startTimeStr, endTimeStr, playerName: playerName, pageSize: 10))
            //        {
            //            if (api.GetPlayerWinloseRecordRes.code == 0)
            //            {
            //                Console.WriteLine("GetPlayerWinLoseRecord successful!");

            //            }
            //            else
            //            {
            //                Console.WriteLine($"GetPlayerWinLoseRecord failed: {api.GetPlayerWinloseRecordRes.message}");
            //            }
            //        }
            //        else
            //        {
            //            Console.WriteLine($"GetPlayerWinLoseRecord failed: {api.ErrorMessage}");
            //        }



            //if (await api.GetGameList())
            //{
            //    if (api.GetGameListRes.code == 0)
            //    {
            //        Console.WriteLine("GetGameList successful!");

            //    }
            //    else
            //    {
            //        Console.WriteLine($"GetGameList failed: {api.GetGameListRes.message}");
            //    }
            //}
            //else
            //{
            //    Console.WriteLine($"GetGameList failed: {api.ErrorMessage}");
            //}



            //        if (await api.GetPrizeRecords(startTimeStr, endTimeStr, playerName: playerName, pageSize: 10))
            //        {
            //            if (api.GetPrizeRecordsRes.code == 0)
            //            {
            //                Console.WriteLine("GetPrizeRecords successful!");

            //            }
            //            else
            //            {
            //                Console.WriteLine($"GetPrizeRecords failed: {api.GetPrizeRecordsRes.message}");
            //            }
            //        }
            //        else
            //        {
            //            Console.WriteLine($"GetPrizeRecords failed: {api.ErrorMessage}");
            //        }



            Console.WriteLine("API Tests Completed.");
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}
