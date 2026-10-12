using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;


namespace GameRecord.Gamess.VPLUS
{
    public class VPLUSAPI : IDisposable
    {
        #region Declaration
        public string APIUrl { get; set; }
        public string MerchantSecret { get; set; }
        public string MerchantID { get; set; }

        public string GameLaunchUrl { get; set; }


        public string finalUrl { get; set; }
        public string Request { get; set; }
        public string Parameters { get; set; }
        public string Response { get; set; }
        public string ErrorMessage { get; set; }


        public ErrorResponse ErrorRes { get; set; }

        public GameResponse GameRes { get; set; }
        public GameResponse2 Game2Res { get; set; }

        public createAccountResponse createAccountRes { get; set; }

        public PlayerListResponse PlayerListRes { get; set; }

        public LoginResponse LoginRes { get; set; }

        public CheckTokenResponse CheckTokenRes { get; set; }

        public KickPlayerResponse KickPlayerRes { get; set; }

        public BlockPlayerResponse BlockPlayerRes { get; set; }

        public onlineStatusResponse onlineStatusRes { get; set; }

        public WalletInitResponse WalletInitRes { get; set; }

        public CheckWalletBalanceResponse CheckWalletBalanceRes { get; set; }

        public DepositWalletResponse DepositWalletRes { get; set; }

        public WithdrawWalletResponse WithdrawWalletRes { get; set; }

        public GetExtendGameBetReportResponse GetExtendGameBetReportRes { get; set; }

        public QueryOrderResponse QueryOrderRes { get; set; }

        public ActivePlayerReportResponse ActivePlayerReportRes { get; set; }

        public GetFiveMinutesReportResponse FiveMinutesReportRes { get; set; }
        public HourlyReportResponse HourlyReportRes { get; set; }
        public DailyReportResponse DailyReportRes { get; set; }



        #endregion

        #region Constructor
        public VPLUSAPI(string APIUrl, string MerchantSecret, string MerchantID, string GameLaunchUrl)
        {
            this.APIUrl = APIUrl;
            this.MerchantSecret = MerchantSecret;
            this.MerchantID = MerchantID;
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
                url = url.Replace("\u200B", "");
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
                using (HttpClient client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(100) })
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



        public async Task<bool> gameList()
        {
            try
            {


                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("appId", this.MerchantID);
                geek.Add("timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

                string message = "";
                // Convert Dictionary<string, dynamic> to URL query parameters
                if (geek != null && geek.Count > 0)
                {
                    List<string> queryList = new List<string>();
                    foreach (var kvp in geek)
                    {
                        queryList.Add($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value.ToString())}");
                    }
                    string queryString = string.Join("&", queryList);
                    message = queryString;
                }
                message = Sort.SortQueryString(message);
                message += this.MerchantSecret;
                string md5 = Sort.GetMD5(message);

                geek.Add("sign", md5);



                string endpoint = $"{this.APIUrl}​/gameList";
                HttpResponseMessage response = await PostApiInJsonAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    GameRes = JsonConvert.DeserializeObject<GameResponse>(responseContent);


                    if (GameRes.code == 200)
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
                    ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", "", "");
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }


        public async Task<bool> gameExtList()
        {
            try
            {


                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("appId", this.MerchantID);
                geek.Add("timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

                string message = "";
                // Convert Dictionary<string, dynamic> to URL query parameters
                if (geek != null && geek.Count > 0)
                {
                    List<string> queryList = new List<string>();
                    foreach (var kvp in geek)
                    {
                        queryList.Add($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value.ToString())}");
                    }
                    string queryString = string.Join("&", queryList);
                    message = queryString;
                }
                message = Sort.SortQueryString(message);
                message += this.MerchantSecret;
                string md5 = Sort.GetMD5(message);

                geek.Add("sign", md5);



                string endpoint = $"{this.APIUrl}​/gameExtList";
                HttpResponseMessage response = await PostApiInJsonAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    Game2Res = JsonConvert.DeserializeObject<GameResponse2>(responseContent);


                    if (Game2Res.code == 200)
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
                    ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", "", "");
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }


        public async Task<bool> playerList(int pageNo, int pageSize)
        {
            try
            {


                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("appId", this.MerchantID);
                geek.Add("timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                geek.Add("pageNo", pageNo);
                geek.Add("pageSize", pageSize);

                string message = "";
                // Convert Dictionary<string, dynamic> to URL query parameters
                if (geek != null && geek.Count > 0)
                {
                    List<string> queryList = new List<string>();
                    foreach (var kvp in geek)
                    {
                        queryList.Add($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value.ToString())}");
                    }
                    string queryString = string.Join("&", queryList);
                    message = queryString;
                }
                message = Sort.SortQueryString(message);
                message += this.MerchantSecret;

                string md5 = Sort.GetMD5(message);

                geek.Add("sign", md5);

                string endpoint = $"{this.APIUrl}​/playerList";
                HttpResponseMessage response = await PostApiInJsonAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    PlayerListRes = JsonConvert.DeserializeObject<PlayerListResponse>(responseContent);
                    if (PlayerListRes.code == 200)
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
                    ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", "", "");
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> createAccount(string username)
        {
            try
            {


                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("appId", this.MerchantID);
                geek.Add("timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                geek.Add("username", username);

                string message = "";
                // Convert Dictionary<string, dynamic> to URL query parameters
                if (geek != null && geek.Count > 0)
                {
                    List<string> queryList = new List<string>();
                    foreach (var kvp in geek)
                    {
                        queryList.Add($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value.ToString())}");
                    }
                    string queryString = string.Join("&", queryList);
                    message = queryString;
                }
                message = Sort.SortQueryString(message);
                message += this.MerchantSecret;

                string md5 = Sort.GetMD5(message);

                geek.Add("sign", md5);

                string endpoint = $"{this.APIUrl}​/createAccount";
                HttpResponseMessage response = await PostApiInJsonAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    createAccountRes = JsonConvert.DeserializeObject<createAccountResponse>(responseContent);
                    if (createAccountRes.code == 200)
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
                    ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", "", "");
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }


        public async Task<bool> login(string username)
        {
            try
            {


                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("appId", this.MerchantID);
                geek.Add("timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                geek.Add("username", username);

                string message = "";
                // Convert Dictionary<string, dynamic> to URL query parameters
                if (geek != null && geek.Count > 0)
                {
                    List<string> queryList = new List<string>();
                    foreach (var kvp in geek)
                    {
                        queryList.Add($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value.ToString())}");
                    }
                    string queryString = string.Join("&", queryList);
                    message = queryString;
                }
                message = Sort.SortQueryString(message);
                message += this.MerchantSecret;

                string md5 = Sort.GetMD5(message);

                geek.Add("sign", md5);

                string endpoint = $"{this.APIUrl}​/player/login";
                HttpResponseMessage response = await PostApiInJsonAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    LoginRes = JsonConvert.DeserializeObject<LoginResponse>(responseContent);
                    if (LoginRes.code == 200)
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
                    ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", "", "");
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> checkToken(string username, string token)
        {
            try
            {


                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("appId", this.MerchantID);
                geek.Add("timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                geek.Add("username", username);
                geek.Add("token", token);

                string message = "";
                // Convert Dictionary<string, dynamic> to URL query parameters
                if (geek != null && geek.Count > 0)
                {
                    List<string> queryList = new List<string>();
                    foreach (var kvp in geek)
                    {
                        queryList.Add($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value.ToString())}");
                    }
                    string queryString = string.Join("&", queryList);
                    message = queryString;
                }
                message = Sort.SortQueryString(message);
                message += this.MerchantSecret;

                string md5 = Sort.GetMD5(message);

                geek.Add("sign", md5);

                string endpoint = $"{this.APIUrl}​/player/checkToken";
                HttpResponseMessage response = await PostApiInJsonAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    CheckTokenRes = JsonConvert.DeserializeObject<CheckTokenResponse>(responseContent);
                    if (CheckTokenRes.code == 200)
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
                    ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", "", "");
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }


        public async Task<bool> KickPlayer(string username)
        {
            try
            {


                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("appId", this.MerchantID);
                geek.Add("timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                geek.Add("username", username);

                string message = "";
                // Convert Dictionary<string, dynamic> to URL query parameters
                if (geek != null && geek.Count > 0)
                {
                    List<string> queryList = new List<string>();
                    foreach (var kvp in geek)
                    {
                        queryList.Add($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value.ToString())}");
                    }
                    string queryString = string.Join("&", queryList);
                    message = queryString;
                }
                message = Sort.SortQueryString(message);
                message += this.MerchantSecret;

                string md5 = Sort.GetMD5(message);

                geek.Add("sign", md5);

                string endpoint = $"{this.APIUrl}​/player/kick";
                HttpResponseMessage response = await PostApiInJsonAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    KickPlayerRes = JsonConvert.DeserializeObject<KickPlayerResponse>(responseContent);
                    if (KickPlayerRes.code == 200)
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
                    ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", "", "");
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }


        public async Task<bool> BlockPlayer(string username)
        {
            try
            {


                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("appId", this.MerchantID);
                geek.Add("timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                geek.Add("username", username);

                string message = "";
                // Convert Dictionary<string, dynamic> to URL query parameters
                if (geek != null && geek.Count > 0)
                {
                    List<string> queryList = new List<string>();
                    foreach (var kvp in geek)
                    {
                        queryList.Add($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value.ToString())}");
                    }
                    string queryString = string.Join("&", queryList);
                    message = queryString;
                }
                message = Sort.SortQueryString(message);
                message += this.MerchantSecret;

                string md5 = Sort.GetMD5(message);

                geek.Add("sign", md5);

                string endpoint = $"{this.APIUrl}​/player/block";
                HttpResponseMessage response = await PostApiInJsonAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    BlockPlayerRes = JsonConvert.DeserializeObject<BlockPlayerResponse>(responseContent);
                    if (BlockPlayerRes.code == 200)
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
                    ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", "", "");
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }


        public async Task<bool> openGame(string username)
        {
            try
            {


                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("appId", this.MerchantID);
                geek.Add("timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                geek.Add("username", username);

                string message = "";
                // Convert Dictionary<string, dynamic> to URL query parameters
                if (geek != null && geek.Count > 0)
                {
                    List<string> queryList = new List<string>();
                    foreach (var kvp in geek)
                    {
                        queryList.Add($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value.ToString())}");
                    }
                    string queryString = string.Join("&", queryList);
                    message = queryString;
                }
                message = Sort.SortQueryString(message);
                message += this.MerchantSecret;

                string md5 = Sort.GetMD5(message);

                geek.Add("sign", md5);

                string endpoint = $"{this.APIUrl}​/player/openGame";
                HttpResponseMessage response = await PostApiInJsonAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    BlockPlayerRes = JsonConvert.DeserializeObject<BlockPlayerResponse>(responseContent);
                    if (BlockPlayerRes.code == 200)
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
                    ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", "", "");
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }



        public async Task<string> OpenPlayerGameAsync(string token, string id, string backUrlBase64, string closeBack,
                                            string lang = null, string layout = null, string mute = null, string currencyDisplay = null)
        {
            try
            {


                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("appId", this.MerchantID);
                geek.Add("timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                geek.Add("token", token);
                geek.Add("id", id);
                geek.Add("back", backUrlBase64); // Already Base64 encoded
                geek.Add("closeBack", closeBack);

                // Optional parameters
                if (!string.IsNullOrEmpty(lang)) geek.Add("lang", lang);
                if (!string.IsNullOrEmpty(layout)) geek.Add("layout", layout);
                if (!string.IsNullOrEmpty(mute)) geek.Add("mute", mute);
                if (!string.IsNullOrEmpty(currencyDisplay)) geek.Add("currencyDisplay", currencyDisplay);

                // Prepare message for signing
                string message = "";
                string message1 = "";
                if (geek != null && geek.Count > 0)
                {
                    List<string> queryList = new List<string>();
                    foreach (var kvp in geek)
                    {
                        queryList.Add($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value.ToString())}");
                    }
                    string queryString = string.Join("&", queryList);
                    message = queryString;
                }
                message = Sort.SortQueryString(message);
                message1 = message;
                message += this.MerchantSecret;

                string md5 = Sort.GetMD5(message);



                geek.Add("sign", md5);

                message1 = message1 + "&sign=" + md5;

                string endpoint = $"{this.APIUrl}​/player/openGame?{message1}".Replace("\u200B", "");

                return endpoint;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return "";
            }
        }


        public async Task<bool> onlineStatus(string username)
        {
            try
            {


                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("appId", this.MerchantID);
                geek.Add("timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                geek.Add("username", username);

                string message = "";
                // Convert Dictionary<string, dynamic> to URL query parameters
                if (geek != null && geek.Count > 0)
                {
                    List<string> queryList = new List<string>();
                    foreach (var kvp in geek)
                    {
                        queryList.Add($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value.ToString())}");
                    }
                    string queryString = string.Join("&", queryList);
                    message = queryString;
                }
                message = Sort.SortQueryString(message);
                message += this.MerchantSecret;

                string md5 = Sort.GetMD5(message);

                geek.Add("sign", md5);

                string endpoint = $"{this.APIUrl}​/player/onlineStatus/query";
                HttpResponseMessage response = await PostApiInJsonAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    onlineStatusRes = JsonConvert.DeserializeObject<onlineStatusResponse>(responseContent);
                    if (onlineStatusRes.code == 200)
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
                    ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", "", "");
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }


        public async Task<bool> InitWalletAsync(string username, string balance, int walletMode, string requestId)
        {
            try
            {
                Dictionary<string, dynamic> paramObj = new Dictionary<string, dynamic>();
                paramObj.Add("appId", this.MerchantID);
                paramObj.Add("timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                paramObj.Add("username", username);
                paramObj.Add("balance", balance);
                paramObj.Add("walletMode", walletMode);
                paramObj.Add("requestId", requestId);

                string message = "";
                if (paramObj != null && paramObj.Count > 0)
                {
                    List<string> queryList = new List<string>();
                    foreach (var kvp in paramObj)
                    {
                        queryList.Add($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value.ToString())}");
                    }
                    string queryString = string.Join("&", queryList);
                    message = queryString;
                }

                message = Sort.SortQueryString(message);
                message += this.MerchantSecret;
                string md5 = Sort.GetMD5(message);

                paramObj.Add("sign", md5);

                string endpoint = $"{this.APIUrl}/wallet/init";
                HttpResponseMessage response = await PostApiInJsonAsync(endpoint, paramObj);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    WalletInitRes = JsonConvert.DeserializeObject<WalletInitResponse>(responseContent);
                    if (WalletInitRes.code == 200)
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
                    ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", "", "");
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }



        public async Task<bool> CheckWalletBalance(string username)
        {
            try
            {


                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("appId", this.MerchantID);
                geek.Add("timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                geek.Add("username", username);

                string message = "";
                // Convert Dictionary<string, dynamic> to URL query parameters
                if (geek != null && geek.Count > 0)
                {
                    List<string> queryList = new List<string>();
                    foreach (var kvp in geek)
                    {
                        queryList.Add($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value.ToString())}");
                    }
                    string queryString = string.Join("&", queryList);
                    message = queryString;
                }
                message = Sort.SortQueryString(message);
                message += this.MerchantSecret;

                string md5 = Sort.GetMD5(message);

                geek.Add("sign", md5);

                string endpoint = $"{this.APIUrl}​/wallet/queryBalance";
                HttpResponseMessage response = await PostApiInJsonAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    CheckWalletBalanceRes = JsonConvert.DeserializeObject<CheckWalletBalanceResponse>(responseContent);
                    if (CheckWalletBalanceRes.code == 200)
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
                    ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", "", "");
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }



        public async Task<bool> DepositWalletAsync(string username, string balance, string requestId)
        {
            try
            {
                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("appId", this.MerchantID);
                geek.Add("timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                geek.Add("username", username);
                geek.Add("balance", balance);
                geek.Add("requestId", requestId);

                string message = "";
                if (geek != null && geek.Count > 0)
                {
                    List<string> queryList = new List<string>();
                    foreach (var kvp in geek)
                    {
                        queryList.Add($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value.ToString())}");
                    }
                    string queryString = string.Join("&", queryList);
                    message = queryString;
                }

                message = Sort.SortQueryString(message);
                message += this.MerchantSecret;
                string md5 = Sort.GetMD5(message);

                geek.Add("sign", md5);

                string endpoint = $"{this.APIUrl}/wallet/deposit";
                HttpResponseMessage response = await PostApiInJsonAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    DepositWalletRes = JsonConvert.DeserializeObject<DepositWalletResponse>(responseContent);
                    if (DepositWalletRes.code == 200)
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
                    ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", "", "");
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> WithdrawWalletAsync(string username, string balance, string requestId)
        {
            try
            {
                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("appId", this.MerchantID);
                geek.Add("timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                geek.Add("username", username);
                geek.Add("balance", balance);
                geek.Add("requestId", requestId);

                string message = "";
                if (geek != null && geek.Count > 0)
                {
                    List<string> queryList = new List<string>();
                    foreach (var kvp in geek)
                    {
                        queryList.Add($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value.ToString())}");
                    }
                    string queryString = string.Join("&", queryList);
                    message = queryString;
                }

                message = Sort.SortQueryString(message);
                message += this.MerchantSecret;
                string md5 = Sort.GetMD5(message);

                geek.Add("sign", md5);

                string endpoint = $"{this.APIUrl}/wallet/withdraw";
                HttpResponseMessage response = await PostApiInJsonAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    WithdrawWalletRes = JsonConvert.DeserializeObject<WithdrawWalletResponse>(responseContent);
                    if (WithdrawWalletRes.code == 200)
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
                    ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", "", "");
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }


        public async Task<bool> GetExtendGameBetReportAsync(
                                        string username = null,
                                        string gameId = null,
                                        long? begin = null,
                                        long? end = null,
                                        int? pageNo = null,
                                        int? pageSize = null,
                                        string requestId = null)
        {
            try
            {
                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("appId", this.MerchantID);
                geek.Add("timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

                if (!string.IsNullOrEmpty(requestId))
                {
                    geek.Add("requestId", requestId);
                }
                else
                {
                    if (!string.IsNullOrEmpty(username))
                        geek.Add("username", username);
                    if (!string.IsNullOrEmpty(gameId))
                        geek.Add("gameId", gameId);
                    if (begin.HasValue)
                        geek.Add("begin", begin.Value);
                    if (end.HasValue)
                        geek.Add("end", end.Value);
                    if (pageNo.HasValue)
                        geek.Add("pageNo", pageNo.Value);
                    if (pageSize.HasValue)
                        geek.Add("pageSize", pageSize.Value);
                }

                string message = "";
                if (geek != null && geek.Count > 0)
                {
                    List<string> queryList = new List<string>();
                    foreach (var kvp in geek)
                    {
                        queryList.Add($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value.ToString())}");
                    }
                    string queryString = string.Join("&", queryList);
                    message = queryString;
                }

                message = Sort.SortQueryString(message);
                message += this.MerchantSecret;
                string md5 = Sort.GetMD5(message);

                geek.Add("sign", md5);

                string endpoint = $"{this.APIUrl}/report/extendGameBet";
                HttpResponseMessage response = await PostApiInJsonAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    GetExtendGameBetReportRes = JsonConvert.DeserializeObject<GetExtendGameBetReportResponse>(responseContent);
                    if (GetExtendGameBetReportRes.code == 200)
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
                    ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", "", "");
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }


        public async Task<bool> QueryOrderAsync(string requestId, int pageNo, int pageSize, string username = null, long? begin = null, long? end = null, int? opType = null, bool? ascending = null)
        {
            try
            {
                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("appId", this.MerchantID);
                geek.Add("timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                geek.Add("requestId", requestId);
                geek.Add("pageNo", pageNo);
                geek.Add("pageSize", pageSize);

                if (!string.IsNullOrEmpty(requestId))
                {
                    geek.Add("requestId", requestId);
                }
                else
                {
                    if (!string.IsNullOrEmpty(username))
                        geek.Add("username", username);
                    if (begin.HasValue)
                        geek.Add("begin", begin.Value);
                    if (end.HasValue)
                        geek.Add("end", end.Value);
                    if (opType.HasValue)
                        geek.Add("opType", opType.Value);
                    if (ascending.HasValue)
                        geek.Add("ascending", ascending.Value);
                }

                string message = "";
                if (geek != null && geek.Count > 0)
                {
                    List<string> queryList = new List<string>();
                    foreach (var kvp in geek)
                    {
                        queryList.Add($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value.ToString())}");
                    }
                    string queryString = string.Join("&", queryList);
                    message = queryString;
                }

                message = Sort.SortQueryString(message);
                message += this.MerchantSecret;

                string md5 = Sort.GetMD5(message);
                geek.Add("sign", md5);

                string endpoint = $"{this.APIUrl}/report/queryOrder";
                HttpResponseMessage response = await PostApiInJsonAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    QueryOrderRes = JsonConvert.DeserializeObject<QueryOrderResponse>(responseContent);
                    if (QueryOrderRes.code == 200)
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
                    ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", "", "");
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> GetActivePlayerReportAsync(long begin, long end, int pageNo, int pageSize)
        {
            try
            {
                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("appId", this.MerchantID);
                geek.Add("timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                geek.Add("begin", begin);
                geek.Add("end", end);
                geek.Add("pageNo", pageNo);
                geek.Add("pageSize", pageSize);

                string message = "";
                if (geek != null && geek.Count > 0)
                {
                    List<string> queryList = new List<string>();
                    foreach (var kvp in geek)
                    {
                        queryList.Add($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value.ToString())}");
                    }
                    string queryString = string.Join("&", queryList);
                    message = queryString;
                }

                message = Sort.SortQueryString(message);
                message += this.MerchantSecret;

                string md5 = Sort.GetMD5(message);
                geek.Add("sign", md5);

                string endpoint = $"{this.APIUrl}/report/activePlayer";
                HttpResponseMessage response = await PostApiInJsonAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    ActivePlayerReportRes = JsonConvert.DeserializeObject<ActivePlayerReportResponse>(responseContent);
                    if (ActivePlayerReportRes.code == 200)
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
                    ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", "", "");
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }



        public async Task<bool> GetFiveMinutesReportAsync(long begin, int pageNo, int pageSize, string username = "")
        {
            try
            {
                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("appId", this.MerchantID);
                geek.Add("timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                geek.Add("begin", begin);
                geek.Add("pageNo", pageNo);
                geek.Add("pageSize", pageSize);

                if (!string.IsNullOrEmpty(username))
                    geek.Add("username", username);

                string message = "";
                if (geek != null && geek.Count > 0)
                {
                    List<string> queryList = new List<string>();
                    foreach (var kvp in geek)
                    {
                        queryList.Add($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value.ToString())}");
                    }
                    string queryString = string.Join("&", queryList);
                    message = queryString;
                }

                message = Sort.SortQueryString(message);
                message += this.MerchantSecret;

                string md5 = Sort.GetMD5(message);
                geek.Add("sign", md5);

                string endpoint = $"{this.APIUrl}/report/fiveMinutes";
                HttpResponseMessage response = await PostApiInJsonAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    FiveMinutesReportRes = JsonConvert.DeserializeObject<GetFiveMinutesReportResponse>(responseContent);
                    if (FiveMinutesReportRes.code == 200)
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
                    ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", "", "");
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }


        public async Task<bool> GetHourlyReportAsync(long begin, int pageNo, int pageSize, string zoneId = "")
        {
            try
            {
                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("appId", this.MerchantID);
                geek.Add("timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                geek.Add("begin", begin);
                geek.Add("pageNo", pageNo);
                geek.Add("pageSize", pageSize);

                if (!string.IsNullOrEmpty(zoneId))
                    geek.Add("zoneId", zoneId);

                string message = "";
                if (geek != null && geek.Count > 0)
                {
                    List<string> queryList = new List<string>();
                    foreach (var kvp in geek)
                    {
                        queryList.Add($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value.ToString())}");
                    }
                    string queryString = string.Join("&", queryList);
                    message = queryString;
                }

                message = Sort.SortQueryString(message);
                message += this.MerchantSecret;

                string md5 = Sort.GetMD5(message);
                geek.Add("sign", md5);

                string endpoint = $"{this.APIUrl}/report/hourly";
                HttpResponseMessage response = await PostApiInJsonAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    HourlyReportRes = JsonConvert.DeserializeObject<HourlyReportResponse>(responseContent);
                    if (HourlyReportRes.code == 200)
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
                    ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", "", "");
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }



        public async Task<bool> GetDailyReportAsync(long begin, int pageNo, int pageSize, string username = "", string zoneId = "")
        {
            try
            {
                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("appId", this.MerchantID);
                geek.Add("timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                geek.Add("begin", begin);
                geek.Add("pageNo", pageNo);
                geek.Add("pageSize", pageSize);

                if (!string.IsNullOrEmpty(username))
                    geek.Add("username", username);

                if (!string.IsNullOrEmpty(zoneId))
                    geek.Add("zoneId", zoneId);

                string message = "";
                if (geek != null && geek.Count > 0)
                {
                    List<string> queryList = new List<string>();
                    foreach (var kvp in geek)
                    {
                        queryList.Add($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value.ToString())}");
                    }
                    string queryString = string.Join("&", queryList);
                    message = queryString;
                }

                message = Sort.SortQueryString(message);
                message += this.MerchantSecret;

                string md5 = Sort.GetMD5(message);
                geek.Add("sign", md5);

                string endpoint = $"{this.APIUrl}/report/daily";
                HttpResponseMessage response = await PostApiInJsonAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    DailyReportRes = JsonConvert.DeserializeObject<DailyReportResponse>(responseContent);
                    if (DailyReportRes.code == 200)
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
                    ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", "", "");
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public PlayerGameRecordResponse PlayerGameRecordRes { get; set; }


        public async Task<bool> GetPlayerGameRecordAsync(string token, int gameId, int layout)
        {
            try
            {
                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                geek.Add("appId", this.MerchantID);
                geek.Add("token", token);
                geek.Add("gameId", gameId);
                geek.Add("layout", layout);

                string message = "";
                if (geek != null && geek.Count > 0)
                {
                    List<string> queryList = new List<string>();
                    foreach (var kvp in geek)
                    {
                        queryList.Add($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value.ToString())}");
                    }
                    string queryString = string.Join("&", queryList);
                    message = queryString;
                }

                message = Sort.SortQueryString(message);
                message += this.MerchantSecret;

                string md5 = Sort.GetMD5(message);
                geek.Add("sign", md5);

                string endpoint = $"{this.APIUrl}/report/playerGameRecord";
                HttpResponseMessage response = await PostApiInJsonAsync(endpoint, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    PlayerGameRecordRes = JsonConvert.DeserializeObject<PlayerGameRecordResponse>(responseContent);
                    if (PlayerGameRecordRes.code == 200)
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
                    ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", "", "");
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

        #region Response Models

        public class Datum
        {
            public int id { get; set; }
            public string name { get; set; }
            public string type { get; set; }
            public string squareIcon { get; set; }
            public string portraitIcon { get; set; }
            public string loadingPic { get; set; }
            public int gameHeight { get; set; }
            public int gameWidth { get; set; }
        }

        public class GameResponse : BaseResponse
        {

            public List<Datum> data { get; set; }
        }



        public class GameResponse2Datum
        {
            public int id { get; set; }
            public string name { get; set; }
            public string type { get; set; }
            public string squareIcon { get; set; }
            public string portraitIcon { get; set; }
            public string loadingPic { get; set; }
            public int gameHeight { get; set; }
            public int gameWidth { get; set; }
            public int isHot { get; set; }
            public int isNew { get; set; }
            public int isMaintained { get; set; }
        }

        public class GameResponse2 : BaseResponse
        {

            public List<GameResponse2Datum> data { get; set; }
        }




        public class createAccountResponseData
        {
            public string username { get; set; }
            public long uid { get; set; }
        }

        public class createAccountResponse : BaseResponse
        {

            public createAccountResponseData data { get; set; }
        }


        public class PlayerListResponseDatum
        {
            public long uid { get; set; }
            public string username { get; set; }
            public string currency { get; set; }
            public int locked { get; set; }
            public int lastLoginTime { get; set; }
        }

        public class PlayerListResponse : BaseResponse
        {

            public List<PlayerListResponseDatum> data { get; set; }
        }



        public class LoginResponseData
        {
            public string username { get; set; }
            public string token { get; set; }
        }

        public class LoginResponse : BaseResponse
        {

            public LoginResponseData data { get; set; }
        }



        public class CheckTokenResponse : BaseResponse
        {

            public object data { get; set; }
        }


        public class KickPlayerResponse : BaseResponse
        {

            public object data { get; set; }
        }

        public class BlockPlayerResponse : BaseResponse
        {

            public object data { get; set; }
        }

        public class WalletInitResponse : BaseResponse
        {

            public object data { get; set; }
        }


        public class onlineStatusResponse : BaseResponse
        {

            public object data { get; set; }
        }



        public class CheckWalletBalanceResponse : BaseResponse
        {
            public string data { get; set; }
        }

        public class DepositWalletResponse : BaseResponse
        {
            public string data { get; set; }
        }

        public class WithdrawWalletResponse : BaseResponse
        {
            public string data { get; set; }
        }

        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
        public class GetExtendGameBetReportResponseData
        {
            public int total { get; set; }
            public List<List> list { get; set; }
        }

        public class List
        {
            public string recordId { get; set; }
            public string requestId { get; set; }
            public string currency { get; set; }
            public int gameId { get; set; }
            public string bet { get; set; }
            public string win { get; set; }
            public string startScore { get; set; }
            public string endScore { get; set; }
            public long time { get; set; }
            public string username { get; set; }
            public string gameName { get; set; }
            public string gameType { get; set; }
            public string roundId { get; set; }
            public string isStop { get; set; }
            public string traceId { get; set; }
        }

        public class GetExtendGameBetReportResponse : BaseResponse
        {

            public GetExtendGameBetReportResponseData data { get; set; }
        }



        public class QueryOrderResponseData
        {
            public int total { get; set; }
            public List<QueryOrderResponseDataList> list { get; set; }
        }

        public class QueryOrderResponseDataList
        {
            public string requestId { get; set; }
            public string username { get; set; }
            public double startBalance { get; set; }
            public double balance { get; set; }
            public double endBalance { get; set; }
            public long time { get; set; }
            public string state { get; set; }
            public int opType { get; set; }
        }

        public class QueryOrderResponse : BaseResponse
        {

            public QueryOrderResponseData data { get; set; }
        }


        public class ActivePlayerReportResponseData
        {
            public int total { get; set; }
            public List<string> list { get; set; }
        }

        public class ActivePlayerReportResponse : BaseResponse
        {

            public ActivePlayerReportResponseData data { get; set; }
        }

        public class GetFiveMinutesReportResponse : BaseResponse
        {

            public FiveMinutesData data { get; set; }
        }

        public class FiveMinutesData
        {
            public int total { get; set; }
            public List<FiveMinutesReportItem> list { get; set; }
        }

        public class FiveMinutesReportItem
        {
            public string username { get; set; }
            public decimal totalPay { get; set; }
            public decimal totalWin { get; set; }
            public decimal startScore { get; set; }
            public decimal endScore { get; set; }
        }


        public class HourlyReportResponse : BaseResponse
        {

            public HourlyReportData data { get; set; }
        }

        public class HourlyReportData
        {
            public int total { get; set; }
            public List<HourlyReportItem> list { get; set; }
        }

        public class HourlyReportItem
        {
            public string username { get; set; }
            public decimal totalPay { get; set; }
            public decimal totalWin { get; set; }
            public decimal startScore { get; set; }
            public decimal endScore { get; set; }
        }


        public class DailyReportResponse : BaseResponse
        {

            public DailyReportData data { get; set; }
        }

        public class DailyReportData
        {
            public int total { get; set; }
            public List<DailyReportItem> list { get; set; }
        }

        public class DailyReportItem
        {
            public string username { get; set; }
            public decimal totalPay { get; set; }
            public decimal totalWin { get; set; }
            public decimal startScore { get; set; }
            public decimal endScore { get; set; }
        }

        public class PlayerGameRecordResponse
        {
            public int code { get; set; }
            public string msg { get; set; }
            public string data { get; set; } // This is a URL string
        }


        public class BaseResponse
        {
            public int code { get; set; }
            public string msg { get; set; }

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




        #endregion
    }

    public class Sort
    {
        public static string SortQueryString(string queryString)
        {
            if (string.IsNullOrWhiteSpace(queryString))
                return string.Empty;

            var keyValuePairs = queryString
                .Split('&', (char)StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Split('=', (char)2)) // Split only on the first '='
                .Where(kv => kv.Length == 2)
                .ToDictionary(kv => kv[0], kv => kv[1]);

            var sortedQuery = string.Join("&",
                keyValuePairs
                    .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(kv => $"{kv.Key}={kv.Value}"));

            return sortedQuery;
        }

        public static string GetMD5(string input)
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

    public class Test
    {
        public static async Task Main(string[] args)
        {
            VPLUSAPI api = new VPLUSAPI(
               "https://sg.slm53iy.com/api/v2/ext",
               "rlbjiseq5uctakrk",
               "cv9wlj8kvrphxyq4",
               "https://sg.slm53iy.com/api/v2/ext/player/openGame"
           );


            // Call API methods below:




            //if (await api.gameList())
            //{
            //    Console.WriteLine("GetGameList successful!");
            //}
            //else
            //{
            //    Console.WriteLine($"GetGameList failed: {api.ErrorMessage}");
            //}

            //if (await api.gameExtList())
            //{
            //    Console.WriteLine("GetGameList successful!");
            //}
            //else
            //{
            //    Console.WriteLine($"GetGameList failed: {api.ErrorMessage}");
            //}

            //if (await api.playerList(1, 1))
            //{
            //    Console.WriteLine("GetGameList successful!");
            //}
            //else
            //{
            //    Console.WriteLine($"GetGameList failed: {api.ErrorMessage}");
            //}

            //if (await api.createAccount("mitjenny123"))
            //{
            //    Console.WriteLine("GetGameList successful!");
            //}
            //else
            //{
            //    Console.WriteLine($"GetGameList failed: {api.ErrorMessage}");
            //}

            //if (await api.login("mitjenny123"))
            //{
            //    Console.WriteLine("GetGameList successful!");
            //}
            //else
            //{
            //    Console.WriteLine($"GetGameList failed: {api.ErrorMessage}");
            //}


            //if (await api.checkToken("mitjenny123", "1e744454f8eaebd2"))
            //{
            //    Console.WriteLine("GetGameList successful!");
            //}
            //else
            //{
            //    Console.WriteLine($"GetGameList failed: {api.ErrorMessage}");
            //}

            //if (await api.KickPlayer("mitjenny123"))
            //{
            //    Console.WriteLine("GetGameList successful!");
            //}
            //else
            //{
            //    Console.WriteLine($"GetGameList failed: {api.ErrorMessage}");
            //}

            //if (await api.login("mitjenny123"))
            //{
            //    string x = await api.OpenPlayerGameAsync(api.LoginRes.data.token, "330000023", "", "0");
            //    Console.WriteLine("GetGameList successful!");
            //}
            //else
            //{
            //    Console.WriteLine($"GetGameList failed: {api.ErrorMessage}");
            //}


            //if (await api.checkToken("mitjenny123", "1e744454f8eaebd2"))
            //{
            //    Console.WriteLine("GetGameList successful!");
            //}
            //else
            //{
            //    Console.WriteLine($"GetGameList failed: {api.ErrorMessage}");
            //}

            //if (await api.onlineStatus("mitjenny123"))
            //{
            //    Console.WriteLine("GetGameList successful!");
            //}
            //else
            //{
            //    Console.WriteLine($"GetGameList failed: {api.ErrorMessage}");
            //}

            //if (await api.InitWalletAsync("mitjenny123","100",1,"TEST001"))
            //{
            //    Console.WriteLine("GetGameList successful!");
            //}
            //else
            //{
            //    Console.WriteLine($"GetGameList failed: {api.ErrorMessage}");
            //}

            //if (await api.CheckWalletBalance("mitjenny123"))
            //{
            //    Console.WriteLine("GetGameList successful!");
            //}
            //else
            //{
            //    Console.WriteLine($"GetGameList failed: {api.ErrorMessage}");
            //}

            //if (await api.DepositWalletAsync("mitjenny123","50","TEST002"))
            //{
            //    Console.WriteLine("GetGameList successful!");
            //}
            //else
            //{
            //    Console.WriteLine($"GetGameList failed: {api.ErrorMessage}");
            //}

            //if (await api.WithdrawWalletAsync("mitjenny123", "50", "TEST003"))
            //{
            //    Console.WriteLine("GetGameList successful!");
            //}
            //else
            //{
            //    Console.WriteLine($"GetGameList failed: {api.ErrorMessage}");
            //}


            TimeZoneInfo gmt8 = TimeZoneInfo.FindSystemTimeZoneById("Singapore"); // GMT+8

            DateTimeOffset yesterdayStartGmt8 = TimeZoneInfo.ConvertTime(DateTime.Today.AddDays(-2), gmt8);
            DateTimeOffset todayEndGmt8 = TimeZoneInfo.ConvertTime(DateTime.Today.AddDays(1).AddMilliseconds(-1), gmt8);

            long unixMsStart = new DateTimeOffset(yesterdayStartGmt8.DateTime).ToUnixTimeMilliseconds();
            long unixMsEnd = new DateTimeOffset(todayEndGmt8.DateTime).ToUnixTimeMilliseconds();





            if (await api.GetExtendGameBetReportAsync(begin: unixMsStart, end: unixMsEnd, pageNo: 1, pageSize: 1000))
            {
                Console.WriteLine("GetGameList successful!");
            }
            else
            {
                Console.WriteLine($"GetGameList failed: {api.ErrorMessage}");
            }


            if (await api.QueryOrderAsync("", 1, 1, begin: unixMsStart, end: unixMsEnd))
            {
                Console.WriteLine("GetGameList successful!");
            }
            else
            {
                Console.WriteLine($"GetGameList failed: {api.ErrorMessage}");
            }


            Console.WriteLine("API Tests Completed.");
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}
