using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace GameRecord.Gamess.Ace333
{
    public class Ace333 : IDisposable
    {
        private Common _common = new Common();

        #region Declaration
        private string API_URL { get; set; }
        private string H5_LOGIN_API_URL { get; set; }
        private string H5_LOBBY_API_URL { get; set; }
        private string POST_FIX { get; set; }
        //private string TOKEN { get; set; }
        private string API_SERVICE_TOKEN { get; set; }
        private string SECRET_KEY { get; set; }
        private string MD5_KEY { get; set; }
        private string ENCRYPT_KEY { get; set; }
        private string DELIMETER { get; set; }
        private string CURRENCY { get; set; }
        private int timeOutSec { get; set; }

        public CreatePlayerRes CreatePlayerRes { get; set; }
        public GetBalanceRes GetBalanceRes { get; set; }
        public CreditRes CreditRes { get; set; }
        public WithdrawRes WithdrawRes { get; set; }
        public GameListRes GameListRes { get; set; }
        public GameLoginRes GameLoginRes { get; set; }
        public GameLogoutRes GameLogoutRes { get; set; }
        public GetLobbyUrlRes GetLobbyUrlRes { get; set; }
        public GetAccountTransactionsRes GetAccountTransactionsRes { get; set; }

        public string Request { get; set; }
        public string Parameters { get; set; }
        public string Response { get; set; }
        public string ErrorMessage { get; set; }

        public Ace333(string apiUrl, string h5LoginApiUrl, string h5LobbyApiUrl, string postFix, string apiServiceToken, string secretKey, string md5Key, string encryptKey,
            string delimeter, string currency, int timeOutSec = 30000)
        {
            this.API_URL = apiUrl;
            this.H5_LOGIN_API_URL = h5LoginApiUrl;
            this.H5_LOBBY_API_URL = h5LobbyApiUrl;
            this.POST_FIX = postFix;
            this.API_SERVICE_TOKEN = apiServiceToken;
            this.SECRET_KEY = secretKey;
            this.MD5_KEY = md5Key;
            this.ENCRYPT_KEY = encryptKey;
            this.DELIMETER = delimeter;
            this.CURRENCY = currency;
            this.timeOutSec = timeOutSec;
        }
        #endregion

        #region General Functions

        public string MD5Hash(string input)
        {
            byte[] hashed = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(input));
            StringBuilder sb = new StringBuilder(hashed.Length * 2);
            for (int i = 0; i < hashed.Length; i++)
            {
                sb.Append(hashed[i].ToString("x2"));
            }
            return sb.ToString();
        }

        public static string DESEncrypt(string inString, string key)
        {
            byte[] encryptKey = ASCIIEncoding.ASCII.GetBytes(key);
            using (MemoryStream ms = new MemoryStream())
            {
                ICryptoTransform createEncryptor = new DESCryptoServiceProvider().CreateEncryptor(encryptKey, encryptKey);
                CryptoStream cs = new CryptoStream(ms, createEncryptor, CryptoStreamMode.Write);
                StreamWriter sw = new StreamWriter(cs); sw.Write(inString); sw.Flush();
                cs.FlushFinalBlock();
                sw.Flush();

                return Convert.ToBase64String(ms.GetBuffer(), 0, (int)ms.Length);
            }
        }

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
                    Content = new StringContent(jsonContent, null, "application/json")
                };
               
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                request.Content.Headers.Add("token", API_SERVICE_TOKEN);

                Request = JsonConvert.SerializeObject(request);

                using (HttpClient client = new HttpClient())
                {
                    return Task.Run(async () =>
                    {
                        return await client.SendAsync(request);
                    }).Result;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private HttpResponseMessage GetApiAsync(string url, Dictionary<string, dynamic> parameters = null)
        {
            try
            {
                // Add parameters to the URL
                if (parameters != null && parameters.Count > 0)
                {
                    var builder = new UriBuilder(url);
                    var query = HttpUtility.ParseQueryString(builder.Query);
                    foreach (var p in parameters)
                    {
                        query[p.Key] = p.Value.ToString();
                    }
                    builder.Query = query.ToString();
                    url = builder.ToString();
                }

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(url)
                };

                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                //request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                request.Headers.Add("token", API_SERVICE_TOKEN);
                //request.Content.Headers.Add("token", API_SERVICE_TOKEN);

                Request = JsonConvert.SerializeObject(request);

                using (HttpClient client = new HttpClient())
                {
                    return Task.Run(async () =>
                    {
                        return await client.SendAsync(request);
                    }).Result;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private string ReadResponseMessageAsync(HttpResponseMessage httpResponseMessage)
        {
            return Task.Run(async () =>
            {
                return await httpResponseMessage.Content.ReadAsStringAsync();
            }).Result;
            
        }

        #endregion

        #region Api Functions
        public bool CreatePlayer(string accountId, string nickname)
        {
            try
            {
                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("accountID", accountId);
                geek.Add("nickname", nickname);
                geek.Add("currency", CURRENCY);

                HttpResponseMessage response = PostApiAsync(API_URL + "/api/createplayer", geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent =  ReadResponseMessageAsync(response);
                    Response = responseContent;
                    CreatePlayerRes = JsonConvert.DeserializeObject<CreatePlayerRes>(responseContent);

                    if (CreatePlayerRes.error == "0")
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = CreatePlayerRes.description;
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

        public bool GetBalance(int playerId, int type = 4500)
        {
            try
            {

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("currency", CURRENCY);
                geek.Add("playerID", Convert.ToInt32(playerId));
                geek.Add("type", type);

                HttpResponseMessage response = GetApiAsync(API_URL + "/api/getbalance", geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = ReadResponseMessageAsync(response);
                    Response = responseContent;
                    GetBalanceRes = JsonConvert.DeserializeObject<GetBalanceRes>(responseContent);

                    if (GetBalanceRes.error == "0")
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = GetBalanceRes.description;
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

        public bool Credit(int playerId, decimal amount, string referenceId, int type = 4500)
        {
            try
            {
                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("playerID", playerId);
                geek.Add("referenceID", referenceId);
                geek.Add("topUpAmount", amount);
                geek.Add("currency", CURRENCY);
                geek.Add("type", type);

                HttpResponseMessage response = PostApiAsync(API_URL + "/api/topup", geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = ReadResponseMessageAsync(response);
                    Response = responseContent;
                    CreditRes = JsonConvert.DeserializeObject<CreditRes>(responseContent);

                    if (CreditRes.error == "0")
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = CreditRes.description;
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

        public bool Withdraw(int playerId, decimal amount, string referenceId, int type = 4500)
        {
            try
            {
                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("playerID", playerId);
                geek.Add("referenceID", referenceId);
                geek.Add("withdrawAmount", amount);
                geek.Add("currency", CURRENCY);
                geek.Add("type", type);

                HttpResponseMessage response = PostApiAsync(API_URL + "/api/withdraw", geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = ReadResponseMessageAsync(response);
                    Response = responseContent;
                    WithdrawRes = JsonConvert.DeserializeObject<WithdrawRes>(responseContent);

                    if (WithdrawRes.error == "0")
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = WithdrawRes.description;
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

        public bool GetGameList()
        {
            try
            {
                HttpResponseMessage response = GetApiAsync(API_URL + "/api/gamelist", null);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = ReadResponseMessageAsync(response);
                    Response = responseContent;
                    GameListRes = JsonConvert.DeserializeObject<GameListRes>(responseContent);

                    if (GameListRes.error == "0")
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = GameListRes.description;
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

        public bool GetLobbyUrl(string actk)
        {
            try
            {
                //if (GameLogin(username, postFix, password, nickName))
                //{
                GetLobbyUrlRes = new GetLobbyUrlRes();
                GetLobbyUrlRes.LobbyUrl = string.Format(H5_LOBBY_API_URL, actk);
                return true;
                //}
                //else
                //{
                //    return false;
                //}
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                return false;
            }
        }

        public bool GameLogin(string username, string postfix, string password, string nickName)
        {
            try
            {

                var currTime = DateTime.Now.ToUniversalTime().ToString("yyyyMMddHHmmss"); // Time format "yyyyMMddHHmmss" of current time, UTC format
                username = $"{username}{postfix}"; // UserName of player

                var _params = $"key={SECRET_KEY}{DELIMETER}time={currTime}{DELIMETER}userName={username}{DELIMETER}password={password}{DELIMETER}currency={CURRENCY}{DELIMETER}nickName={nickName}";
                // Build "q" (Encrypted Request String) from QS with DESencrypt and UrlEncode
                var q = HttpUtility.UrlEncode(DESEncrypt(_params, ENCRYPT_KEY));

                // Build "s" (Signature)
                var s = MD5Hash(_params + MD5_KEY + currTime + SECRET_KEY);

                //var client = new RestClient(H5_LOGIN_API_URL + "/api/Acc/Login");
                //client.Timeout = timeOutSec;

                //var r = new RestRequest(Method.POST);
                //r.RequestFormat = DataFormat.Json;

                //r.AddHeader("Content-Type", "application/json");

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("q", q);
                geek.Add("s", s);
                geek.Add("accessToken", API_SERVICE_TOKEN);
                //r.AddBody(geek);

                //Request = client.BuildUri(r).ToString();
                //IRestResponse response = client.Execute(r);

                HttpResponseMessage response = PostApiAsync(H5_LOGIN_API_URL + "/api/Acc/Login", geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = ReadResponseMessageAsync(response);
                    Response = responseContent;
                    GameLoginRes = JsonConvert.DeserializeObject<GameLoginRes>(responseContent);

                    if (GameLoginRes.Status == "1")
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = GameLoginRes.Status;
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

        public bool GameLogout(string playerId)
        {
            try
            {
                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("playerId", playerId);
                geek.Add("currency", CURRENCY);

                HttpResponseMessage response = PostApiAsync(API_URL + "/api/LogOut", geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = ReadResponseMessageAsync(response);
                    Response = responseContent;
                    GameLogoutRes = JsonConvert.DeserializeObject<GameLogoutRes>(responseContent);

                    if (GameLogoutRes.error == "0")
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = GameLogoutRes.description;
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

        public bool GetAccountTransactions(DateTime timeFrom)
        {
            try
            {
                int timePoint = _common.ToUnixTime(timeFrom.ToUniversalTime());

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("timepoint", timePoint);

                HttpResponseMessage response = PostApiAsync(API_URL + "/api/accounttransactions3", geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = ReadResponseMessageAsync(response);
                    Response = responseContent;

                    GetAccountTransactionsRes.AccountTransactions = DeserializeFromTextPlain<List<AccountTransaction>>(responseContent);
                    if (GetAccountTransactionsRes.AccountTransactions.Count > 0)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = "Error Occured";
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = response.StatusCode + ": " + response.ReasonPhrase;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                return false;
            }
        }

        public static T DeserializeFromTextPlain<T>(string data)
        {
            var serializer = new DataContractSerializer(typeof(T));
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(data)))
            {
                return (T)serializer.ReadObject(stream);
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
        #endregion
    }

    #region models
    public abstract class BaseResponse
    {
        public string error { get; set; }
        public string description { get; set; }
    }
    public class CreatePlayerRes : BaseResponse
    {
        public int playerID { get; set; }
    }
    public class GetBalanceRes : BaseResponse
    {
        public decimal balance { get; set; }
    }
    public class WithdrawRes : BaseResponse
    {
        public string transactionID { get; set; }
        public decimal balance { get; set; }
    }
    public class CreditRes : BaseResponse
    {
        public string transactionID { get; set; }
        public decimal balance { get; set; }
    }
    public class GameListRes : BaseResponse
    {
        public List<Game> Games { get; set; }
    }
    public class Game
    {
        public string gameID { get; set; }
        public string gameName { get; set; }
        public int gameCreditType { get; set; }
    }
    public class GameLoginRes
    {
        public string actk { get; set; }
        public string Status { get; set; }
        public int playerID { get; set; }
        public string currencyCode { get; set; }
        public string description { get; set; }
    }
    public class GameLogoutRes : BaseResponse
    {
    }
    public class GetLobbyUrlRes
    {
        public string LobbyUrl { get; set; }
    }
    public class GetAccountTransactionsRes
    {
        public List<AccountTransaction> AccountTransactions;
    }
    public class AccountTransaction
    {
        public int playerID { get; set; }
        public int extPlayerID { get; set; }
        public string gameID { get; set; }
        public string methodType { get; set; }
        public string playSessionID { get; set; }
        public string referenceID { get; set; }
        public string status { get; set; }
        public DateTime created { get; set; }
        public DateTime updated { get; set; }
        public decimal betAmount { get; set; }
        public decimal winAmount { get; set; }
        public int jackpotModule { get; set; }
        public decimal jackpotContributionAmt { get; set; }
        public string currency { get; set; }
        public string resultUrl { get; set; }
        public string roundDetails { get; set; }
        public string platform { get; set; }

    }
    #endregion
}