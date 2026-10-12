
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace GameRecord.Gamess.DCT
{
    public class DCT : IDisposable
    {
        //Common _common = new Common();

        #region Declaration
        private string API_URL { get; set; }
        private string BET_DATA_URL { get; set; }
        private string PREFIX { get; set; }
        private string PROVIDER { get; set; }
        private string BRAND_ID { get; set; }
        private string API_KEY { get; set; }
        private string CURRENCY { get; set; }
        private string COUNTRY_CODE { get; set; }
        private int DEFAULT_GAME_ID { get; set; }
        private int TIME_OUT_SEC { get; set; }

        //public string Url { get; set; }
        public string Parameters { get; set; }
        public string Request { get; set; }
        public string Response { get; set; }
        public string ErrorMessage { get; set; }

        public KickPlayerRes KickPlayerRes;
        public GetBalanceRes GetBalanceRes;
        public CreditRes CreditRes;
        public WithdrawRes WithdrawRes;
        public LoginGameRes LoginGameRes;
        public GetGameListRes GetGameListRes;
        public GetBetDataRes GetBetDataRes;

        public DCT(
            string apiUrl, string betDataUrl, string prefix, string provider, string brandId,
            string apiKey, string currency, string countryCode = "MY", int defaultGameId = 0, int timeOutSec = 30000
            )
        {
            this.API_URL = apiUrl;
            this.BET_DATA_URL = betDataUrl;
            this.PREFIX = prefix;
            this.PROVIDER = provider;
            this.BRAND_ID = brandId;
            this.API_KEY = apiKey;
            this.CURRENCY = currency;
            this.COUNTRY_CODE = countryCode;
            this.DEFAULT_GAME_ID = defaultGameId;
            this.TIME_OUT_SEC = timeOutSec;
        }
        #endregion

        #region General Functions
        public string GetSignature(string brand_id, string apiKey, string[] paramKeys = null)
        {
            string strParams = paramKeys != null ? string.Join("", paramKeys) : "";
            string s = $"{brand_id}{strParams}{apiKey}";
            using (MD5 md5 = MD5.Create())
            {
                byte[] rawBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(s));
                return BitConverter.ToString(rawBytes).Replace("-", "").ToUpper();
            }
        }

        private readonly Encoding Charset = Encoding.UTF8;
        private string P_Response;

        //private HttpResponseMessage PostApiAsync(string url, Dictionary<string, dynamic> bodyContent = null)
        //{
        //    try
        //    {
        //        var jsonContent = JsonConvert.SerializeObject(bodyContent);

        //        var request = new HttpRequestMessage
        //        {
        //            Method = HttpMethod.Post,
        //            RequestUri = new Uri(url),
        //            Content = new StringContent(jsonContent, null, "application/json")
        //            //Content = new StringContent(jsonContent)
        //        };

        //        // Adding Content-Type to the request headers (Not Standard Practice)
        //        //request.Headers.TryAddWithoutValidation("Content-Type", "application/json");
        //        //request.Headers.Add("X-Content-Type", "application/json");

        //        //request.Headers.Add("Content-Type", "application/json");
        //        //request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        //        //request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        //        //request.Content.Headers.Add("token", API_SERVICE_TOKEN);
        //        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        //        Request = JsonConvert.SerializeObject(request);

        //        // Create the HttpClient with a custom handler
        //        //var handler = new CustomHttp.CustomHttpClientHandler();
        //        using (HttpClient client = new HttpClient())
        //        {
        //            client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");
        //            return Task.Run(async () =>
        //            {
        //                return await client.SendAsync(request);
        //            }).Result;
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }
        //}

        private HttpResponseMessage PostApiAsync(string url, Dictionary<string, dynamic> bodyContent = null)
        {
            try
            {
                var jsonContent = JsonConvert.SerializeObject(bodyContent);

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(url),
                    //Content = new StringContent(jsonContent, null, "application/json")
                    Content = new StringContent(jsonContent)
                };

                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                // request.Content.Headers.Add("token", API_SERVICE_TOKEN);

                Request = JsonConvert.SerializeObject(request);
                Parameters = jsonContent;

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

                //request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Headers.Add("Content-Type", "application/json");
                //request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                //request.Headers.Add("token", API_SERVICE_TOKEN);
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
        public bool LoginGame(string playerId, int gameId, string language, string channel = "mobile", string return_url = "", bool full_screen = false)
        {
            try
            {
                var signature = GetSignature(BRAND_ID, API_KEY, new string[] { playerId });

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("brand_id", BRAND_ID);
                geek.Add("sign", signature);
                geek.Add("brand_uid", playerId);
                geek.Add("game_id", gameId);
                geek.Add("currency", CURRENCY);
                geek.Add("language", language);
                geek.Add("channel", channel);
                geek.Add("country_code", COUNTRY_CODE);
                geek.Add("return_url", return_url);
                geek.Add("full_screen", full_screen);

                var requestUrl = API_URL + "/" + PREFIX + "/loginGame";

                Request = requestUrl;
                Parameters = JsonConvert.SerializeObject(geek);

                HttpResponseMessage response = PostApiAsync(requestUrl, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = ReadResponseMessageAsync(response);
                    Response = responseContent;

                    LoginGameRes = JsonConvert.DeserializeObject<LoginGameRes>(responseContent);
                    if (LoginGameRes.code == 1000)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = LoginGameRes.code + ": " + LoginGameRes.msg;
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

        public bool GetBalance(string playerId)
        {
            try
            {
                var signature = GetSignature(BRAND_ID, API_KEY, new string[] { playerId });

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("brand_id", BRAND_ID);
                geek.Add("sign", signature);
                geek.Add("brand_uid", playerId);
                geek.Add("currency", CURRENCY);

                var requestUrl = API_URL + "/" + PREFIX + "/getBalance";
                var client = new RestClient(requestUrl);
                var r = new RestRequest(Method.POST);
                r.AddHeader("Content-Type", "application/json");
                r.AddJsonBody(geek);

                Request = requestUrl;
                Parameters = JsonConvert.SerializeObject(geek);
                IRestResponse response = client.Execute(r);
                Response = response.Content;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    GetBalanceRes = JsonConvert.DeserializeObject<GetBalanceRes>(response.Content);
                    if (GetBalanceRes.code == 1000)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = GetBalanceRes.code + ": " + GetBalanceRes.msg;
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = response.StatusCode + ": " + response.StatusDescription;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                return false;
            }
        }

        public bool Credit(string playerId, decimal amount, string bill_no)
        {
            try
            {
                var signature = GetSignature(BRAND_ID, API_KEY, new string[] { playerId });

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("brand_id", BRAND_ID);
                geek.Add("sign", signature);
                geek.Add("brand_uid", playerId);
                geek.Add("amount", amount);
                geek.Add("bill_no", bill_no);
                geek.Add("currency", CURRENCY);
                geek.Add("country_code", COUNTRY_CODE);

                var requestUrl = API_URL + "/" + PREFIX + "/credit";
                var client = new RestClient(requestUrl);
                var r = new RestRequest(Method.POST);
                r.AddHeader("Content-Type", "application/json");
                r.AddJsonBody(geek);

                Request = requestUrl;
                Parameters = JsonConvert.SerializeObject(geek);
                IRestResponse response = client.Execute(r);
                Response = response.Content;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    CreditRes = JsonConvert.DeserializeObject<CreditRes>(response.Content);
                    if (CreditRes.code == 1000)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = CreditRes.code + ": " + CreditRes.msg;
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = response.StatusCode + ": " + response.StatusDescription;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                return false;
            }
        }

        public bool Withdraw(string playerId, decimal amount, string bill_no)
        {
            try
            {
                var signature = GetSignature(BRAND_ID, API_KEY, new string[] { playerId });

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("brand_id", BRAND_ID);
                geek.Add("sign", signature);
                geek.Add("brand_uid", playerId);
                geek.Add("amount", amount);
                geek.Add("bill_no", bill_no);
                geek.Add("currency", CURRENCY);
                geek.Add("country_code", COUNTRY_CODE);

                var requestUrl = API_URL + "/" + PREFIX + "/withdraw";
                var client = new RestClient(requestUrl);
                var r = new RestRequest(Method.POST);
                r.AddHeader("Content-Type", "application/json");
                r.AddJsonBody(geek);

                Request = requestUrl;
                Parameters = JsonConvert.SerializeObject(geek);
                IRestResponse response = client.Execute(r);
                Response = response.Content;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    WithdrawRes = JsonConvert.DeserializeObject<WithdrawRes>(response.Content);
                    if (WithdrawRes.code == 1000)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = WithdrawRes.code + ": " + WithdrawRes.msg;
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = response.StatusCode + ": " + response.StatusDescription;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                return false;
            }
        }

        public bool GetBetData(DateTime timeFrom, DateTime timeTo, int page = 1, string playerId = "")
        {
            try
            {
                string startTime = timeFrom.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss");
                string endTime = timeTo.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss");

                var signature = GetSignature(BRAND_ID, API_KEY, new string[] { startTime, endTime });

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("brand_id", BRAND_ID);
                geek.Add("sign", signature);
                geek.Add("page", page);
                geek.Add("currency", CURRENCY);
                geek.Add("start_time", startTime);
                geek.Add("end_time", endTime);
                geek.Add("provider", PROVIDER);

                if (!string.IsNullOrWhiteSpace(playerId))
                {
                    geek.Add("brand_uid", playerId);
                }

                var requestUrl = BET_DATA_URL + "/" + PREFIX + "/getBetData";
                var client = new RestClient(requestUrl);
                var r = new RestRequest(Method.POST);
                r.AddHeader("Content-Type", "application/json");
                r.AddJsonBody(geek);

                Request = requestUrl;
                Parameters = JsonConvert.SerializeObject(geek);
                IRestResponse response = client.Execute(r);
                Response = response.Content;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    GetBetDataRes = JsonConvert.DeserializeObject<GetBetDataRes>(response.Content);
                    if (GetBetDataRes.code == 1000)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = GetBetDataRes.code + ": " + GetBetDataRes.msg;
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = response.StatusCode + ": " + response.StatusDescription;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                return false;
            }
        }

        public bool KickPlayer(string playerId)
        {
            try
            {
                var signature = GetSignature(BRAND_ID, API_KEY, new string[] { playerId });

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("brand_id", BRAND_ID);
                geek.Add("sign", signature);
                geek.Add("brand_uid", playerId);
                geek.Add("currency", CURRENCY);

                var requestUrl = API_URL + "/" + PREFIX + "/kickPlayer";
                var client = new RestClient(requestUrl);
                var r = new RestRequest(Method.POST);
                r.AddHeader("Content-Type", "application/json");
                r.AddJsonBody(geek);

                Request = requestUrl;
                Parameters = JsonConvert.SerializeObject(geek);
                IRestResponse response = client.Execute(r);
                Response = response.Content;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    KickPlayerRes = JsonConvert.DeserializeObject<KickPlayerRes>(response.Content);
                    if (KickPlayerRes.code == 1000)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = KickPlayerRes.code + ": " + KickPlayerRes.msg;
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = response.StatusCode + ": " + response.StatusDescription;
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
                var signature = GetSignature(BRAND_ID, API_KEY);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("brand_id", BRAND_ID);
                geek.Add("sign", signature);
                geek.Add("provider", PROVIDER);

                //var requestUrl = API_URL + "/" + PREFIX + "/getGameList";
                //var client = new RestClient(requestUrl);
                //var r = new RestRequest(Method.POST);
                //r.AddHeader("Content-Type", "application/json");
                //r.AddJsonBody(geek);

                //Request = requestUrl;
                //Parameters = JsonConvert.SerializeObject(geek);
                //IRestResponse response = client.Execute(r);
                //Response = response.Content;

                HttpResponseMessage response = PostApiAsync(API_URL + "/" + PREFIX + "/getGameList", geek);

                //HttpResponseMessage response = null;
                //Task.Run(async () => response = await PostApiAsync(requestUrl, geek)).Wait();
                //Response = response?.Content.ReadAsStringAsync().Result;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = ReadResponseMessageAsync(response);
                    Response = responseContent;

                    GetGameListRes = JsonConvert.DeserializeObject<GetGameListRes>(responseContent);
                    if (GetGameListRes.code == 1000)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = GetGameListRes.code + ": " + GetGameListRes.msg;
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
        #endregion

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }

    #region models
    public class LoginGameRes
    {
        public int code { get; set; }
        public LoginGameData data { get; set; }
        public string msg { get; set; }
    }
    public class GetBalanceRes
    {
        public int code { get; set; }
        public BalanceData data { get; set; }
        public string msg { get; set; }
    }
    public class CreditRes
    {
        public int code { get; set; }
        public BalanceData data { get; set; }
        public string msg { get; set; }
    }
    public class WithdrawRes
    {
        public int code { get; set; }
        public BalanceData data { get; set; }
        public string msg { get; set; }
    }
    public class KickPlayerRes
    {
        public int code { get; set; }
        public string msg { get; set; }
    }
    public class GetGameListRes
    {
        public int code { get; set; }
        public string msg { get; set; }
        public List<GameListData> data { get; set; }
    }
    public class GetBetDataRes
    {
        public int code { get; set; }
        public string msg { get; set; }
        public List<BetData> data { get; set; }
        public Paging page { get; set; }
    }
    public class LoginGameData
    {
        public string game_url { get; set; }
    }
    public class BalanceData
    {
        public decimal balance { get; set; }
    }
    public class GameListData
    {
        public string provider { get; set; }
        public int game_id { get; set; }
        public string game_name { get; set; }
        public string game_name_cn { get; set; }
        public DateTime release_date { get; set; }
        public string rtp { get; set; }
        public string game_icon { get; set; }
        public string content_type { get; set; }
        public string game_type { get; set; }
        public string content { get; set; }
    }
    public class Paging
    {
        public int current_page { get; set; }
        public int total_count { get; set; }
    }
    public class BetData
    {
        public string brand_uid { get; set; }
        public string currency { get; set; }
        public string wager_type { get; set; }
        public decimal? amount { get; set; }
        public decimal? before_amount { get; set; }
        public decimal? after_amount { get; set; }
        public int game_id { get; set; }
        public string game_name { get; set; }
        public long round_id { get; set; }
        public long? wager_id { get; set; }
        public decimal? jackpot_contribution { get; set; }
        public decimal? jackpot_win { get; set; }
        public string description { get; set; }
        public DateTime? create_time { get; set; }
        public int? game_result { get; set; }
        public decimal? tip { get; set; }
        public bool? is_endround { get; set; }
    }
    #endregion
}
