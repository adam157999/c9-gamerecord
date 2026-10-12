using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace GameRecord.Gamess.AFB
{
    public class AFB : IDisposable
    {
        #region Declaration
        private string API_URL { get; set; }

        private string GAMELIST_API_URL { get; set; }

        private string FREE_ROUND_BONUS_API_URL { get; set; }

        private string GAME_STATE_API_URL { get; set; }

        private string API_KEY { get; set; }
        private int SITE_ID { get; set; }

        public string Request { get; set; }
        public string Parameters { get; set; }
        public string Response { get; set; }
        public string ErrorMessage { get; set; }

        public LoginResponse LoginRes { get; set; }
        public BalanceResponse BalanceRes { get; set; }

        public DepositResponse DepositRes { get; set; }

        public WithdrawResponse WithdrawRes { get; set; }

        public CheckTransactionResponse CheckTransactionRes { get; set; }

        public BlockUserResponse BlockUserRes { get; set; }

        public UnblockUserResponse UnblockUserRes { get; set; }

        public BetRecordsResponse BetRecordsRes { get; set; }

        public FishingRecordsResponse FishingRecordsRes { get; set; }

        public FishingTransferRecordsResponse FishingTransferRecordsRes { get; set; }

        public GameListResponse GameListRes { get; set; }

        public FreeRoundsBonusResponse FreeRoundsBonusRes { get; set; }

        public CancelFreeRoundsBonusResponse CancelFreeRoundsBonusRes { get; set; }

        public FreeRoundsGameListResponse FreeRoundsGameListRes { get; set; }

        public FreeRoundsAccountResponse FreeRoundsAccountRes { get; set; }

        public UnfinishedGamesResponse UnfinishedGamesRes { get; set; }

        #endregion

        #region Constructor
        public AFB(string apiUrl, string apiKey, int siteId)
        {
            this.API_URL = apiUrl + "/transferAPI";
            this.GAMELIST_API_URL = apiUrl + "/api/gamelist";
            this.FREE_ROUND_BONUS_API_URL= apiUrl + "/freeRoundsBonusAPI";
            this.GAME_STATE_API_URL= apiUrl + "/service/gamesState";
            this.API_KEY = apiKey;
            this.SITE_ID = siteId;
        }
        #endregion

        #region Private Methods
        private HttpResponseMessage PostApiAsync(string url, Dictionary<string, dynamic> bodyContent = null)
        {
            try
            {
                var jsonContent = JsonConvert.SerializeObject(bodyContent);
                Parameters = jsonContent;

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(url),
                    Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
                };
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                Request = JsonConvert.SerializeObject(request);

                using (HttpClient client = new HttpClient())
                {
                    return Task.Run(async () => await client.SendAsync(request)).Result;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private string ReadResponseMessageAsync(HttpResponseMessage httpResponseMessage)
        {
            return Task.Run(async () => await httpResponseMessage.Content.ReadAsStringAsync()).Result;
        }
        #endregion

        #region Public API Methods
        public bool Login( string game, string nickname, string userId, string lang, string currency)
        {
            try
            {

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("jsonrpc", "2.0");
                geek.Add("method", "login");
                geek.Add("siteId", this.SITE_ID);
                geek.Add("apiKey", this.API_KEY);
                geek.Add("id", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());

                Dictionary<string, dynamic> parameters = new Dictionary<string, dynamic>();
                parameters.Add("game", game);
                parameters.Add("nickname", nickname);
                parameters.Add("userId", userId);
                parameters.Add("lang", lang); // Default language
                parameters.Add("currency", currency);

                geek.Add("params", parameters);

              
                HttpResponseMessage response = PostApiAsync(API_URL, geek);
                

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = ReadResponseMessageAsync(response);
                    Response = responseContent;
                    LoginRes = JsonConvert.DeserializeObject<LoginResponse>(responseContent);

                    if (LoginRes.Error == null)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = LoginRes.Error.Message;
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = response.ReasonPhrase;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                return false;
            }
        }

        public bool GetBalance(string userId)
        {
            try
            {
                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("jsonrpc", "2.0");
                geek.Add("method", "getBalance");
                geek.Add("siteId", this.SITE_ID);
                geek.Add("apiKey", this.API_KEY);
                geek.Add("id", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()); // Unique ID

                Dictionary<string, dynamic> parameters = new Dictionary<string, dynamic>();
                parameters.Add("userId", userId);

                geek.Add("params", parameters);

                HttpResponseMessage response = PostApiAsync(API_URL, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = ReadResponseMessageAsync(response);
                    Response = responseContent;
                    BalanceRes = JsonConvert.DeserializeObject<BalanceResponse>(responseContent);

                    if (BalanceRes.Error == null)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = BalanceRes.Error.Message;
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = response.ReasonPhrase;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                return false;
            }
        }

        public bool Deposit(string userId, int amount)
        {
            try
            {
                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("jsonrpc", "2.0");
                geek.Add("method", "deposit");
                geek.Add("siteId", this.SITE_ID);
                geek.Add("apiKey", this.API_KEY);
                geek.Add("id", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()); // Generates unique ID

                Dictionary<string, dynamic> parameters = new Dictionary<string, dynamic>();
                parameters.Add("amount", amount);
                parameters.Add("userId", userId);

                geek.Add("params", parameters);


                HttpResponseMessage response = PostApiAsync(API_URL, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = ReadResponseMessageAsync(response);
                    Response = responseContent;
                    DepositRes = JsonConvert.DeserializeObject<DepositResponse>(responseContent);

                    if (DepositRes.Error == null)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = DepositRes.Error.Message;
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = response.ReasonPhrase;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                return false;
            }
        }

        public bool Withdraw(string userId, int amount)
        {
            try
            {
                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("jsonrpc", "2.0");
                geek.Add("method", "withdraw");
                geek.Add("siteId", this.SITE_ID);
                geek.Add("apiKey", this.API_KEY);
                geek.Add("id", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()); // Generates unique ID

                Dictionary<string, dynamic> parameters = new Dictionary<string, dynamic>();
                parameters.Add("amount", amount);
                parameters.Add("userId", userId);

                geek.Add("params", parameters);

                HttpResponseMessage response = PostApiAsync(API_URL, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = ReadResponseMessageAsync(response);
                    Response = responseContent;
                    WithdrawRes = JsonConvert.DeserializeObject<WithdrawResponse>(responseContent);

                    if (WithdrawRes.Error == null)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = WithdrawRes.Error.Message;
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = response.ReasonPhrase;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                return false;
            }
        }

        public bool CheckTransaction(string transactionId)
        {
            try
            {
                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("jsonrpc", "2.0");
                geek.Add("method", "checkTransaction");
                geek.Add("siteId", this.SITE_ID);
                geek.Add("apiKey", this.API_KEY);
                geek.Add("id", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());

                Dictionary<string, dynamic> parameters = new Dictionary<string, dynamic>();
                parameters.Add("transactionId", transactionId);
                geek.Add("params", parameters);

                HttpResponseMessage response = PostApiAsync(API_URL, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = ReadResponseMessageAsync(response);
                    Response = responseContent;
                    CheckTransactionRes = JsonConvert.DeserializeObject<CheckTransactionResponse>(responseContent);

                    if (CheckTransactionRes.Error == null)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = CheckTransactionRes.Error.Message;
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = response.ReasonPhrase;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                return false;
            }
        }

        public bool BlockUser(string userId)
        {
            try
            {
                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("jsonrpc", "2.0");
                geek.Add("method", "blockUser");
                geek.Add("siteId", this.SITE_ID);
                geek.Add("apiKey", this.API_KEY);
                geek.Add("id", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());

                Dictionary<string, dynamic> parameters = new Dictionary<string, dynamic>();
                parameters.Add("userId", userId);
                geek.Add("params", parameters);

                HttpResponseMessage response = PostApiAsync(API_URL, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = ReadResponseMessageAsync(response);
                    Response = responseContent;
                    BlockUserRes = JsonConvert.DeserializeObject<BlockUserResponse>(responseContent);

                    if (BlockUserRes.Error == null)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = BlockUserRes.Error.Message;
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = response.ReasonPhrase;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                return false;
            }
        }

        public bool UnblockUser(string userId)
        {
            try
            {
                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("jsonrpc", "2.0");
                geek.Add("method", "unblockUser");
                geek.Add("siteId", this.SITE_ID);
                geek.Add("apiKey", this.API_KEY);
                geek.Add("id", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());

                Dictionary<string, dynamic> parameters = new Dictionary<string, dynamic>();
                parameters.Add("userId", userId);
                geek.Add("params", parameters);

                HttpResponseMessage response = PostApiAsync(API_URL, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = ReadResponseMessageAsync(response);
                    Response = responseContent;
                    UnblockUserRes = JsonConvert.DeserializeObject<UnblockUserResponse>(responseContent);

                    if (UnblockUserRes.Error == null)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = UnblockUserRes.Error.Message;
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = response.ReasonPhrase;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                return false;
            }
        }

        public bool GetBetRecords(string lastUpdate, string endTime = null, int count = 3000)
        {
            try
            {
                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("jsonrpc", "2.0");
                geek.Add("method", "getBetRecords");
                geek.Add("siteId", this.SITE_ID);
                geek.Add("apiKey", this.API_KEY);
                geek.Add("id", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());

                Dictionary<string, dynamic> parameters = new Dictionary<string, dynamic>();
                parameters.Add("lastUpdate", lastUpdate);
                if (!string.IsNullOrEmpty(endTime))
                {
                    parameters.Add("endTime", endTime);
                }
                parameters.Add("count", count);
                geek.Add("params", parameters);

                HttpResponseMessage response = PostApiAsync(API_URL, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = ReadResponseMessageAsync(response);
                    Response = responseContent;
                    BetRecordsRes = JsonConvert.DeserializeObject<BetRecordsResponse>(responseContent);

                    if (BetRecordsRes.Error == null)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = BetRecordsRes.Error.Message;
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = response.ReasonPhrase;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                return false;
            }
        }

        public bool GetFishingRecords(string lastUpdate, string endTime = null, int count = 3000)
        {
            try
            {
                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("jsonrpc", "2.0");
                geek.Add("method", "getFishingRecords");
                geek.Add("siteId", this.SITE_ID);
                geek.Add("apiKey", this.API_KEY);
                geek.Add("id", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());

                Dictionary<string, dynamic> parameters = new Dictionary<string, dynamic>();
                parameters.Add("lastUpdate", lastUpdate);
                if (!string.IsNullOrEmpty(endTime))
                {
                    parameters.Add("endTime", endTime);
                }
                parameters.Add("count", count);
                geek.Add("params", parameters);

                HttpResponseMessage response = PostApiAsync(API_URL, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = ReadResponseMessageAsync(response);
                    Response = responseContent;
                    FishingRecordsRes = JsonConvert.DeserializeObject<FishingRecordsResponse>(responseContent);

                    if (FishingRecordsRes.Error == null)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = FishingRecordsRes.Error.Message;
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = response.ReasonPhrase;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                return false;
            }
        }

        public bool GetFishingTransferRecords(string lastUpdate, string endTime = null, int count = 3000)
        {
            try
            {
                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("jsonrpc", "2.0");
                geek.Add("method", "getFishingTransferRecords");
                geek.Add("siteId", this.SITE_ID);
                geek.Add("apiKey", this.API_KEY);
                geek.Add("id", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());

                Dictionary<string, dynamic> parameters = new Dictionary<string, dynamic>();
                parameters.Add("lastUpdate", lastUpdate);
                if (!string.IsNullOrEmpty(endTime))
                {
                    parameters.Add("endTime", endTime);
                }
                parameters.Add("count", count);
                geek.Add("params", parameters);

                HttpResponseMessage response = PostApiAsync(API_URL, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = ReadResponseMessageAsync(response);
                    Response = responseContent;
                    FishingTransferRecordsRes = JsonConvert.DeserializeObject<FishingTransferRecordsResponse>(responseContent);

                    if (FishingTransferRecordsRes.Error == null)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = FishingTransferRecordsRes.Error.Message;
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = response.ReasonPhrase;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                return false;
            }
        }

        private HttpResponseMessage GetApiAsync(string url)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    return Task.Run(async () => await client.GetAsync(url)).Result;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public bool GetGameList(string lang = "en")
        {
            try
            {
                string requestUrl = $"{this.GAMELIST_API_URL}?sid={this.SITE_ID}&key={this.API_KEY}&lang={lang}";
                HttpResponseMessage response = GetApiAsync(requestUrl);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = ReadResponseMessageAsync(response);
                    Response = responseContent;
                    GameListRes = JsonConvert.DeserializeObject<GameListResponse>(responseContent);

                    return true;
                }
                else
                {
                    ErrorMessage = response.ReasonPhrase;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                return false;
            }
        }

        public bool SetFreeRoundsBonus(string account, string currency, int rounds, int features, List<string> gameList, string bonusCode, int expirationDate)
        {
            try
            {
                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("jsonrpc", "2.0");
                geek.Add("method", "set");
                geek.Add("siteId", this.SITE_ID);
                geek.Add("apiKey", this.API_KEY);
                geek.Add("id", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());

                Dictionary<string, dynamic> parameters = new Dictionary<string, dynamic>();
                parameters.Add("account", account);
                parameters.Add("currency", currency);
                parameters.Add("rounds", rounds);
                parameters.Add("features", features);
                parameters.Add("gameList", gameList);
                parameters.Add("bonusCode", bonusCode);
                parameters.Add("expirationDate", expirationDate);
                geek.Add("params", parameters);

                HttpResponseMessage response = PostApiAsync(this.FREE_ROUND_BONUS_API_URL , geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = ReadResponseMessageAsync(response);
                    Response = responseContent;
                    FreeRoundsBonusRes = JsonConvert.DeserializeObject<FreeRoundsBonusResponse>(responseContent);

                    return true;
                }
                else
                {
                    ErrorMessage = response.ReasonPhrase;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                return false;
            }
        }


        public bool CancelFreeRoundsBonus(string account, string bonusCode)
        {
            try
            {
                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("jsonrpc", "2.0");
                geek.Add("method", "cancel");
                geek.Add("siteId", this.SITE_ID);
                geek.Add("apiKey", this.API_KEY);
                geek.Add("id", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());

                Dictionary<string, dynamic> parameters = new Dictionary<string, dynamic>();
                parameters.Add("account", account);
                parameters.Add("bonusCode", bonusCode);
                geek.Add("params", parameters);

                HttpResponseMessage response = PostApiAsync(this.FREE_ROUND_BONUS_API_URL, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = ReadResponseMessageAsync(response);
                    Response = responseContent;
                    CancelFreeRoundsBonusRes = JsonConvert.DeserializeObject<CancelFreeRoundsBonusResponse>(responseContent);

                    return true;
                }
                else
                {
                    ErrorMessage = response.ReasonPhrase;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                return false;
            }
        }

        public bool GetFreeRoundsGameList()
        {
            try
            {
                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("jsonrpc", "2.0");
                geek.Add("method", "getGameList");
                geek.Add("siteId", this.SITE_ID);
                geek.Add("apiKey", this.API_KEY);
                geek.Add("id", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
                geek.Add("params", new Dictionary<string, dynamic>());

                HttpResponseMessage response = PostApiAsync(this.FREE_ROUND_BONUS_API_URL, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = ReadResponseMessageAsync(response);
                    Response = responseContent;
                    FreeRoundsGameListRes = JsonConvert.DeserializeObject<FreeRoundsGameListResponse>(responseContent);

                    return true;
                }
                else
                {
                    ErrorMessage = response.ReasonPhrase;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                return false;
            }
        }


        public bool GetFreeRoundsAccount(string account)
        {
            try
            {
                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("jsonrpc", "2.0");
                geek.Add("method", "getAccount");
                geek.Add("siteId", this.SITE_ID);
                geek.Add("apiKey", this.API_KEY);
                geek.Add("id", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());

                Dictionary<string, dynamic> parameters = new Dictionary<string, dynamic>();
                parameters.Add("account", account);
                geek.Add("params", parameters);

                HttpResponseMessage response = PostApiAsync(this.FREE_ROUND_BONUS_API_URL , geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = ReadResponseMessageAsync(response);
                    Response = responseContent;
                    FreeRoundsAccountRes = JsonConvert.DeserializeObject<FreeRoundsAccountResponse>(responseContent);

                    return true;
                }
                else
                {
                    ErrorMessage = response.ReasonPhrase;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                return false;
            }
        }

        

        public bool GetUnfinishedGames(string account)
        {
            try
            {
                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("jsonrpc", "2.0");
                geek.Add("method", "unfinishedGames");
                geek.Add("siteId", this.SITE_ID);
                geek.Add("apiKey", this.API_KEY);
                geek.Add("id", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());

                Dictionary<string, dynamic> parameters = new Dictionary<string, dynamic>();
                parameters.Add("account", account);
                geek.Add("params", parameters);

                HttpResponseMessage response = PostApiAsync(this.GAME_STATE_API_URL, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = ReadResponseMessageAsync(response);
                    Response = responseContent;
                    UnfinishedGamesRes = JsonConvert.DeserializeObject<UnfinishedGamesResponse>(responseContent);

                    return true;
                }
                else
                {
                    ErrorMessage = response.ReasonPhrase;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                return false;
            }
        }



        #endregion

        #region IDisposable Implementation
        public void Dispose()
        {
            // Dispose resources if needed
        }
        #endregion
    }

    #region Response Models

    public class BaseResponse
    {
        public string jsonrpc { get; set; }
        public string id { get; set; }
        public ErrorResponse Error { get; set; }
    }

    public class ErrorResponse
    {
        public int Code { get; set; }
        public string Message { get; set; }
    }

    public class LoginResponse : BaseResponse
    {
 
        public LoginResult result { get; set; }
        
    }

    public class LoginResult
    {
        public string link { get; set; }
    }
    
    public class BalanceResponse : BaseResponse
    {
        public BalanceResult result { get; set; }
    }

    public class BalanceResult
    {
        public int balance { get; set; }
    }

    public class DepositResponse : BaseResponse
    {
       
        public DepositResult result { get; set; }
        
    }

    public class DepositResult
    {
        public int balance { get; set; }
    }

    public class WithdrawResponse : BaseResponse
    {
        
        public WithdrawResult result { get; set; }
        
    }

    public class WithdrawResult
    {
        public int balance { get; set; }
    }


    public class CheckTransactionResponse : BaseResponse
    {
         
        
        public CheckTransactionResult result { get; set; }
        
    }

    public class CheckTransactionResult
    {
        public int codeId { get; set; }
        public string transactionId { get; set; }
        public string message { get; set; }
        public string userName { get; set; }
        public int amountCent { get; set; }
        public string method { get; set; }
        public string time { get; set; }
        public int beforeBalanceCent { get; set; }
        public int afterBalanceCent { get; set; }
    }

    public class BlockUserResponse : BaseResponse
    {
       
        public BlockUserResult result { get; set; }
      
    }

    public class BlockUserResult
    {
        public int codeId { get; set; }
        public string Message { get; set; }
    }

    public class UnblockUserResponse : BaseResponse
    {
      
        public UnblockUserResult result { get; set; }
    }

    public class UnblockUserResult
    {
        public int codeId { get; set; }
        public string Message { get; set; }
    }

    public class BetRecordsResponse : BaseResponse
    {
        
        public BetRecordsResult result { get; set; } 
    }

    public class BetRecordsResult
    {
        public string nextTimestamp { get; set; }
        public List<BetRecord> recordList { get; set; }
    }

    public class BetRecord
    {
        public int userId { get; set; }
        public int balance { get; set; }
        public string gamecode { get; set; }
        public string betId { get; set; }
        public int bet { get; set; }
        public int win { get; set; }
        public int lineBet { get; set; }
        public int lineCount { get; set; }
        public string lastUpdate { get; set; }
        public string mode { get; set; }
        public string currency { get; set; }
        public double jackpotCommissions { get; set; }
        public string time { get; set; }
    }
     
    public class FishingRecordsResponse : BaseResponse
    {
        
        public FishingRecordsResult result { get; set; }
        
    }

    public class FishingRecordsResult
    {
        public string nextTimestamp { get; set; }
        public List<FishingRecord> recordList { get; set; }
    }

    public class FishingRecord
    {
        public string betId { get; set; }
        public string userId { get; set; }
        public string currency { get; set; }
        public int siteId { get; set; }
        public int gameId { get; set; }
        public string gameCode { get; set; }
        public List<FishRecord> fishRecords { get; set; }
        public string uniqueNo { get; set; }
        public int fishingId { get; set; }
        public int payPoint { get; set; }
        public double payMoney { get; set; }
        public int winPoint { get; set; }
        public double winMoney { get; set; }
        public string playForm { get; set; }
        public string time { get; set; }
        public string transferId { get; set; }
    }

    public class FishRecord
    {
        public string uniqueNo { get; set; }
        public int fishingId { get; set; }
        public int payPoint { get; set; }
    }

     
    public class FishingTransferRecordsResponse : BaseResponse
    {
       
        public FishingTransferRecordsResult result { get; set; }
         
    }

    public class FishingTransferRecordsResult
    {
        public string nextTimestamp { get; set; }
        public List<FishingTransferRecord> recordList { get; set; }
    }

    public class FishingTransferRecord
    {
        public int siteId { get; set; }
        public string userId { get; set; }
        public double amount { get; set; }
        public int point { get; set; }
        public int pointRate { get; set; }
        public int betPoint { get; set; }
        public int winPoint { get; set; }
        public string transactionId { get; set; }
        public string time { get; set; }
        public string method { get; set; }
        public string currency { get; set; }
    }

     
    public class Coin
    {
        public string currency { get; set; }
        public string coins { get; set; }
        public string minBet { get; set; }
        public string maxBet { get; set; }
    }

    public class GameList
    {
        public string code { get; set; }
        public string name { get; set; }
        public int line { get; set; }
        public int realityLine { get; set; }
        public string picture { get; set; }
        public string type { get; set; }
        public List<string> modes { get; set; }
        public List<Coin> coins { get; set; }
        public string window { get; set; }
        public string rtp { get; set; }
        public string volatility { get; set; }
        public string maxMultiple { get; set; }
        public List<string> supportLang { get; set; }
        public List<string> certifications { get; set; }
        public List<string> features { get; set; }
    }

    public class GameListResponse
    {
        public List<GameList> gameList { get; set; }
    }

    public class FreeRoundsBonusResponse : BaseResponse
    {
        
        public FreeRoundsBonusResult result { get; set; }
        
    }

    public class FreeRoundsBonusResult
    {
        public string account { get; set; }
        public string currency { get; set; }
        public int rounds { get; set; }
        public List<string> gameList { get; set; }
        public string bonusCode { get; set; }
    }
    public class CancelFreeRoundsBonusResponse : BaseResponse
    {
       
        public CancelFreeRoundsBonusResult result { get; set; } 
    }

    public class CancelFreeRoundsBonusResult
    {
        public string account { get; set; }
        public string currency { get; set; }
        public string bonusCode { get; set; }
    }

    public class FreeRoundsGameListResponse : BaseResponse
    {
        
        public List<string> result { get; set; }
    }


    public class FreeRoundsAccountResponse : BaseResponse
    {
      
        public List<FreeRoundsAccountResult> result { get; set; }
    }

    public class FreeRoundsAccountResult
    {
        public string account { get; set; }
        public string currency { get; set; }
        public int rounds { get; set; }
        public List<string> gameList { get; set; }
        public string bonusCode { get; set; }
        public string createdAt { get; set; }
        public int remainRounds { get; set; }
        public string expirationDate { get; set; }
    }

    public class UnfinishedGamesResponse : BaseResponse
    {
       
        public UnfinishedGamesResult result { get; set; }
    }

    public class UnfinishedGamesResult
    {
        public List<string> unfinishedGames { get; set; }
    }
     
    #endregion


    //public class Test
    //{
    //    public static void Main()
    //    {
    //        AFBTransferAPI o = new AFBTransferAPI(
    //            "http://api.afbgaming.net/transferAPI",
    //            "http://api.afbgaming.net/api/gamelist",
    //            "http://api.afbgaming.net/freeRoundsBonusAPI",
    //            "http://api.afbgaming.net/service/gamesState",
    //            "GUK1JznPbcM7FvTkL6dDV0WXiQIp5ofR",
    //            2448
    //        );

    //        Console.WriteLine("Starting API Tests...");

    //        if (o.Login("lobby", "1001989", "1001989", "en", "MYR"))
    //        {
    //            Console.WriteLine("Login successful!");
    //        }
    //        else
    //        {
    //            Console.WriteLine($"Login failed: {o.ErrorMessage}");
    //        }

    //        if (o.GetBalance("1001989"))
    //        {
    //            Console.WriteLine($"Balance: {o.BalanceRes.result.balance}");
    //        }
    //        else
    //        {
    //            Console.WriteLine($"GetBalance failed: {o.ErrorMessage}");
    //        }

    //        if (o.Deposit("1001989", 100))
    //        {
    //            Console.WriteLine("Deposit successful!");
    //        }
    //        else
    //        {
    //            Console.WriteLine($"Deposit failed: {o.ErrorMessage}");
    //        }

    //        if (o.Withdraw("1001989", 50))
    //        {
    //            Console.WriteLine("Withdraw successful!");
    //        }
    //        else
    //        {
    //            Console.WriteLine($"Withdraw failed: {o.ErrorMessage}");
    //        }

    //        if (o.CheckTransaction("TXN123456"))
    //        {
    //            Console.WriteLine("Transaction check successful!");
    //        }
    //        else
    //        {
    //            Console.WriteLine($"Transaction check failed: {o.ErrorMessage}");
    //        }

    //        if (o.BlockUser("1001989"))
    //        {
    //            Console.WriteLine("User blocked successfully!");
    //        }
    //        else
    //        {
    //            Console.WriteLine($"Block user failed: {o.ErrorMessage}");
    //        }

    //        if (o.UnblockUser("1001989"))
    //        {
    //            Console.WriteLine("User unblocked successfully!");
    //        }
    //        else
    //        {
    //            Console.WriteLine($"Unblock user failed: {o.ErrorMessage}");
    //        }

    //        if (o.GetBetRecords("2024-01-01T00:00:00Z"))
    //        {
    //            Console.WriteLine("Bet records retrieved successfully!");
    //        }
    //        else
    //        {
    //            Console.WriteLine($"Get bet records failed: {o.ErrorMessage}");
    //        }

    //        if (o.GetFishingRecords("2024-01-01T00:00:00Z"))
    //        {
    //            Console.WriteLine("Fishing records retrieved successfully!");
    //        }
    //        else
    //        {
    //            Console.WriteLine($"Get fishing records failed: {o.ErrorMessage}");
    //        }

    //        if (o.GetGameList("en"))
    //        {
    //            Console.WriteLine("Game list retrieved successfully!");
    //        }
    //        else
    //        {
    //            Console.WriteLine($"Get game list failed: {o.ErrorMessage}");
    //        }

    //        if (o.SetFreeRoundsBonus("1001989", "MYR", 10, 0, new List<string> { "game1", "game2" }, "BONUS123", 20251231))
    //        {
    //            Console.WriteLine("Free Rounds Bonus set successfully!");
    //        }
    //        else
    //        {
    //            Console.WriteLine($"Set Free Rounds Bonus failed: {o.ErrorMessage}");
    //        }

    //        if (o.CancelFreeRoundsBonus("1001989", "BONUS123"))
    //        {
    //            Console.WriteLine("Free Rounds Bonus cancelled successfully!");
    //        }
    //        else
    //        {
    //            Console.WriteLine($"Cancel Free Rounds Bonus failed: {o.ErrorMessage}");
    //        }

    //        if (o.GetFreeRoundsGameList())
    //        {
    //            Console.WriteLine("Free Rounds Game List retrieved successfully!");
    //        }
    //        else
    //        {
    //            Console.WriteLine($"Get Free Rounds Game List failed: {o.ErrorMessage}");
    //        }

    //        if (o.GetFreeRoundsAccount("1001989"))
    //        {
    //            Console.WriteLine("Free Rounds Account details retrieved successfully!");
    //        }
    //        else
    //        {
    //            Console.WriteLine($"Get Free Rounds Account failed: {o.ErrorMessage}");
    //        }

    //        if (o.GetUnfinishedGames("1001989"))
    //        {
    //            Console.WriteLine("Unfinished games retrieved successfully!");
    //        }
    //        else
    //        {
    //            Console.WriteLine($"Get Unfinished Games failed: {o.ErrorMessage}");
    //        }

    //        Console.WriteLine("API Tests Completed.");
    //        Console.WriteLine("Press any key to exit.");
    //        Console.ReadKey();
    //    }
    //}
}
