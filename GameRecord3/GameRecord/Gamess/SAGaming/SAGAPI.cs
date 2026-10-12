using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace GameRecord.Gamess.SAGAPI
{
    public class SAGAMINGAPI : IDisposable
    {
     

        public string BaseUrl { get; set; }

        public string BetDetailAPIURL { get; set; }

        public string GameClientLaunchingURL { get; set; }

        public string LobbyCode { get; set; }
        public string SecretKey { get; set; }
        public string MD5Key { get; set; }
        public string EncryptKey { get; set; }


        public string finalUrl { get; set; }
        public string Request { get; set; }
        public string Parameters { get; set; }
        public string Response { get; set; }
        public string ErrorMessage { get; set; }


        public SAGAMINGAPI(string BaseUrl, string BetDetailAPIURL, string GameClientLaunchingURL, string LobbyCode, string SecretKey, string MD5Key, string EncryptKey)
        {
            this.BaseUrl = BaseUrl;
            this.BetDetailAPIURL = BetDetailAPIURL;
            this.GameClientLaunchingURL = GameClientLaunchingURL;
            this.LobbyCode = LobbyCode;
            this.SecretKey = SecretKey;
            this.MD5Key = MD5Key;
            this.EncryptKey = EncryptKey;
        }

        public static string GetCurrentUtcTime()
        {
            return DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        }

      
        public  string DESEncrypt(string inString)
        {
            byte[] EncryptKey = ASCIIEncoding.ASCII.GetBytes(this.EncryptKey);
            MemoryStream ms = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms, new DESCryptoServiceProvider().CreateEncryptor(EncryptKey, EncryptKey),
                CryptoStreamMode.Write);
            StreamWriter sw = new StreamWriter(cs);
            sw.Write(inString);
            sw.Flush();
            cs.FlushFinalBlock();
            sw.Flush();
            return Convert.ToBase64String(ms.GetBuffer(), 0, (int)ms.Length);
        }

        public  string BuildMD5(string inString)
        {
            byte[] hashed = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(inString));
            StringBuilder sb = new StringBuilder(hashed.Length * 2);
            for (int i = 0; i < hashed.Length; i++)
            {
                sb.Append(hashed[i].ToString("x2"));
            }
            return sb.ToString();
        }


        public RegUserInfoResponse RegUserInfoRes { get; set; }

        public VerifyUsernameResponse VerifyUsernameRes { get; set; }

        public GetUserStatusDVResponse GetUserStatusDVRes { get; set; }

        public QueryBetLimitResponse QueryBetLimitRes { get; set; }

        public SetAllBetLimitResponse SetAllBetLimitRes { get; set; }

        public GetUserMaxBalanceResponse GetUserMaxBalanceRes { get; set; }

        public SetUserMaxBalanceResponse SetUserMaxBalanceRes { get; set; }
         
        public SetUserMaxWinningResponse SetUserMaxWinningRes { get; set; }

        public QueryUserBetLimitResponse QueryUserBetLimitRes { get; set; }

        public GetUnfinishedGameResponse GetUnfinishedGameRes { get; set; }

        public LoginRequestResponse LoginRequestRes { get; set; }
        public LoginRequestTryToPlayResponse LoginRequestTryToPlayRes { get; set; }

        public KickUserResponse KickUserRes { get; set; }


        public GetAllBetDetailsDVResponse GetAllBetDetailsDVRes { get; set; }

        public GetAllBetDetailsDVResponse GetAllBetDetailsForTimeIntervalDVRes { get; set; }


        public GetAllBetDetailsForTransactionIDResponse GetAllBetDetailsForTransactionIDRes { get; set; }

        public GetUserBetItemDVResponse GetUserBetItemDVRes { get; set; }

        public GetUserBetAmountResponse GetUserBetAmountRes { get; set; }

        public GetUserWinLostResponse GetUserWinLostRes { get; set; }

        public GetTransactionDetailsResponse GetTransactionDetailsRes { get; set; }

        public GetActiveHostListResponse GetActiveHostListRes { get; set; }

        public GetCancelledBetDetailsResponse GetCancelledBetDetailsRes { get; set; }

        public GetGameErrorDetailResponse GetGameErrorDetailRes { get; set; }

        public GetUnsuccessfulBetDetailsResponse GetUnsuccessfulBetDetailsRes { get; set; }
 
        public DebitBalanceDVResponse DebitBalanceDVRes  { get; set; }
        public DebitAllBalanceDVResponse DebitAllBalanceDVRes  { get; set; }

        public CreditBalanceDVResponse CreditBalanceDVRes  { get; set; }
        public CheckOrderIdResponse CheckOrderIdRes { get; set; }

        public CheckOrderDetailsDVResponse CheckOrderDetailsDVRes { get; set; }




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


        private async Task<string> ReadResponseMessageAsync(HttpResponseMessage httpResponseMessage)
        {
            return await httpResponseMessage.Content.ReadAsStringAsync();
        }

        public static T DeserializeFromXml<T>(string xml)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using (StringReader reader = new StringReader(xml))
            {
                return (T)serializer.Deserialize(reader);
            }
        }



        public string GetGameLink(string username, string token, string lobby, string lang = null, string returnurl = null, bool? mobile = null, string options = null)
        {
            try
            {
                 
                Dictionary<string, string> geek = new Dictionary<string, string>
                {
                    { "username", username },
                    { "token", token },
                    { "lobby", lobby }
                };

                if (!string.IsNullOrEmpty(lang)) geek.Add("lang", lang);
                if (!string.IsNullOrEmpty(returnurl)) geek.Add("returnurl", returnurl);
                if (mobile.HasValue) geek.Add("mobile", (mobile.Value ? "true" : "false"));
                if (!string.IsNullOrEmpty(options)) geek.Add("options", options);

                
                var query = string.Join("&", geek.Select(kvp =>
                    $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

                 
                string finalUrl = $"{this.GameClientLaunchingURL}?{query}";

                return finalUrl;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return null;
            }
        }


        public async Task<bool> CreateUser(string UserName, string CurrencyType)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=RegUserInfo" +
                            "&Key=" + SecretKey +
                            "&Time=" + currTime +
                            "&Username="+ UserName +
                            "&CurrencyType=" + CurrencyType;

                string q = DESEncrypt(QS);
                string s = BuildMD5(QS + MD5Key + currTime + SecretKey);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                    geek.Add("q", q);
                    geek.Add("s", s);

                    
                    HttpResponseMessage response = await PostApiAsync(this.BaseUrl, geek);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        Response = responseContent;
                        RegUserInfoRes = DeserializeFromXml<RegUserInfoResponse>(responseContent);
                        if (RegUserInfoRes.ErrorMsgId == 0)
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
                        //ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = responseContent;
                        return false;
                    }
                
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> VerifyUsername(string UserName)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=VerifyUsername" +
                            "&Key=" + SecretKey +
                            "&Time=" + currTime +
                            "&Username=" + UserName;

                string q = DESEncrypt(QS);
                string s = BuildMD5(QS + MD5Key + currTime + SecretKey);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("q", q);
                geek.Add("s", s);


                HttpResponseMessage response = await PostApiAsync(this.BaseUrl, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    VerifyUsernameRes = DeserializeFromXml<VerifyUsernameResponse>(responseContent);
                    
                    if (VerifyUsernameRes.ErrorMsgId == 0)
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
                    //ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = responseContent;
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> GetUserStatusDV(string UserName)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=GetUserStatusDV" +
                            "&Key=" + SecretKey +
                            "&Time=" + currTime +
                            "&Username=" + UserName;

                string q = DESEncrypt(QS);
                string s = BuildMD5(QS + MD5Key + currTime + SecretKey);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("q", q);
                geek.Add("s", s);


                HttpResponseMessage response = await PostApiAsync(this.BaseUrl, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    GetUserStatusDVRes = DeserializeFromXml<GetUserStatusDVResponse>(responseContent);
                     
                    if (GetUserStatusDVRes.ErrorMsgId == 0)
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
                    //ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = responseContent;
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> QueryBetLimit(string CurrencyType,string Gametype)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=QueryBetLimit" +
                            "&Key=" + SecretKey +
                            "&Time=" + currTime +
                            "&Currency=" + CurrencyType +
                            "&Gametype=" + Gametype;

                string q = DESEncrypt(QS);
                string s = BuildMD5(QS + MD5Key + currTime + SecretKey);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("q", q);
                geek.Add("s", s);


                HttpResponseMessage response = await PostApiAsync(this.BaseUrl, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    QueryBetLimitRes = DeserializeFromXml<QueryBetLimitResponse>(responseContent);
                    
                    if (QueryBetLimitRes != null )
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
                    //ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = responseContent;
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> SetAllBetLimit(string Username, string Currency, string ListSet)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=SetAllBetLimit" +
                            "&Key=" + SecretKey +
                            "&Time=" + currTime +
                            "&Username=" + Username +
                            "&Currency=" + Currency +
                            "&ListSet=" + ListSet;

                string q = DESEncrypt(QS);
                string s = BuildMD5(QS + MD5Key + currTime + SecretKey);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("q", q);
                geek.Add("s", s);


                HttpResponseMessage response = await PostApiAsync(this.BaseUrl, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    SetAllBetLimitRes = DeserializeFromXml<SetAllBetLimitResponse>(responseContent);
                    
                    if (SetAllBetLimitRes.ErrorMsgId == 0)
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
                    //ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = responseContent;
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> GetUserMaxBalance(string Username)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=GetUserMaxBalance" +
                            "&Key=" + SecretKey +
                            "&Time=" + currTime +
                            "&Username=" + Username;

                string q = DESEncrypt(QS);
                string s = BuildMD5(QS + MD5Key + currTime + SecretKey);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("q", q);
                geek.Add("s", s);


                HttpResponseMessage response = await PostApiAsync(this.BaseUrl, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    GetUserMaxBalanceRes = DeserializeFromXml<GetUserMaxBalanceResponse>(responseContent);
                    
                    if (GetUserMaxBalanceRes.ErrorMsgId == 0)
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
                    //ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = responseContent;
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }


        public async Task<bool> SetUserMaxBalance(string UserName,decimal MaxBalance)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=SetUserMaxBalance" +
                            "&Key=" + SecretKey +
                            "&Time=" + currTime +
                            "&Username=" + UserName +
                            "&MaxBalance=" + MaxBalance;

                string q = DESEncrypt(QS);
                string s = BuildMD5(QS + MD5Key + currTime + SecretKey);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("q", q);
                geek.Add("s", s);


                HttpResponseMessage response = await PostApiAsync(this.BaseUrl, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    SetUserMaxBalanceRes  = DeserializeFromXml<SetUserMaxBalanceResponse>(responseContent);
                    
                    if (SetUserMaxBalanceRes.ErrorMsgId == 0)
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
                    //ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = responseContent;
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        


        public async Task<bool> SetUserMaxWinning(string UserName, decimal MaxWinning)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=SetUserMaxWinning" +
                            "&Key=" + SecretKey +
                            "&Time=" + currTime +
                            "&Username=" + UserName +
                            "&MaxWinning=" + MaxWinning;

                string q = DESEncrypt(QS);
                string s = BuildMD5(QS + MD5Key + currTime + SecretKey);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("q", q);
                geek.Add("s", s);


                HttpResponseMessage response = await PostApiAsync(this.BaseUrl, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    SetUserMaxWinningRes  = DeserializeFromXml<SetUserMaxWinningResponse>(responseContent);
                    
                    if (SetUserMaxWinningRes.ErrorMsgId == 0)
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
                    //ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = responseContent;
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> QueryUserBetLimit(string UserName, string Gametype)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=SetUserMaxBalance" +
                            "&Key=" + SecretKey +
                            "&Time=" + currTime +
                            "&Username=" + UserName +
                            "&Gametype=" + Gametype;

                string q = DESEncrypt(QS);
                string s = BuildMD5(QS + MD5Key + currTime + SecretKey);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("q", q);
                geek.Add("s", s);


                HttpResponseMessage response = await PostApiAsync(this.BaseUrl, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    QueryUserBetLimitRes = DeserializeFromXml<QueryUserBetLimitResponse>(responseContent);

                    if (QueryUserBetLimitRes != null)
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
                    //ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = responseContent;
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> GetUnfinishedGame(string UserName )
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=GetUnfinishedGame" +
                            "&Key=" + SecretKey +
                            "&Time=" + currTime +
                            "&Username=" + UserName;

                string q = DESEncrypt(QS);
                string s = BuildMD5(QS + MD5Key + currTime + SecretKey);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("q", q);
                geek.Add("s", s);


                HttpResponseMessage response = await PostApiAsync(this.BaseUrl, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    GetUnfinishedGameRes = DeserializeFromXml<GetUnfinishedGameResponse>(responseContent);
                     
                    if (GetUnfinishedGameRes.ErrorMsgId == 0)
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
                    //ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = responseContent;
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }


        public async Task<bool> LoginRequest(string Username,string CurrencyType)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=LoginRequest" +
                            "&Key=" + SecretKey +
                            "&Time=" + currTime +
                            "&Username=" + Username;

                
                string q = DESEncrypt(QS);
                string s = BuildMD5(QS + MD5Key + currTime + SecretKey);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("q", q);
                geek.Add("s", s);


                HttpResponseMessage response = await PostApiAsync(this.BaseUrl, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    LoginRequestRes  = DeserializeFromXml<LoginRequestResponse>(responseContent);
                     
                    if (LoginRequestRes.ErrorMsgId == 0)
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
                    //ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = responseContent;
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> LoginRequestForFun(string Username, string CurrencyType)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=LoginRequestForFun" +
                            "&Key=" + SecretKey +
                            "&Time=" + currTime +
                            "&Username=" + Username;


                string q = DESEncrypt(QS);
                string s = BuildMD5(QS + MD5Key + currTime + SecretKey);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("q", q);
                geek.Add("s", s);


                HttpResponseMessage response = await PostApiAsync(this.BaseUrl, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    LoginRequestTryToPlayRes = DeserializeFromXml<LoginRequestTryToPlayResponse>(responseContent);
                     
                    if (LoginRequestTryToPlayRes.ErrorMsgId == 0)
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
                    //ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = responseContent;
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }


        public async Task<bool> KickUser(string UserName)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=KickUser" +
                            "&Key=" + SecretKey +
                            "&Time=" + currTime +
                            "&Username=" + UserName;

                string q = DESEncrypt(QS);
                string s = BuildMD5(QS + MD5Key + currTime + SecretKey);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("q", q);
                geek.Add("s", s);


                HttpResponseMessage response = await PostApiAsync(this.BaseUrl, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    KickUserRes = DeserializeFromXml<KickUserResponse>(responseContent);
                     
                    if (KickUserRes.ErrorMsgId == 0)
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
                    //ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = responseContent;
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> GetAllBetDetailsDV(string Username = null, string Date = null)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=GetAllBetDetailsDV" +
                            "&Key=" + SecretKey +
                            "&Time=" + currTime;

                if (!string.IsNullOrEmpty(Username)) QS += "&Username=" + Date;
                if (!string.IsNullOrEmpty(Date)) QS += "&Date=" + Date;

                string q = DESEncrypt(QS);
                string s = BuildMD5(QS + MD5Key + currTime + SecretKey);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("q", q);
                geek.Add("s", s);


                HttpResponseMessage response = await PostApiAsync(this.BetDetailAPIURL, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    GetAllBetDetailsDVRes = DeserializeFromXml<GetAllBetDetailsDVResponse>(responseContent);
                  
                    if (GetAllBetDetailsDVRes.ErrorMsgId == 0)
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
                    //ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = responseContent;
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }


        public async Task<bool> GetAllBetDetailsForTimeIntervalDV(string FromTime, string ToTime,string Username = null)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=GetAllBetDetailsForTimeIntervalDV" +
                            "&Key=" + SecretKey +
                            "&Time=" + currTime +
                            "&FromTime=" + FromTime +
                            "&ToTime=" + ToTime;

                if (!string.IsNullOrEmpty(Username)) QS += "&Username=" + Username; 

                string q = DESEncrypt(QS);
                string s = BuildMD5(QS + MD5Key + currTime + SecretKey);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("q", q);
                geek.Add("s", s);


                HttpResponseMessage response = await PostApiAsync(this.BaseUrl, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    GetAllBetDetailsForTimeIntervalDVRes = DeserializeFromXml<GetAllBetDetailsDVResponse>(responseContent);
                    
                    if (GetAllBetDetailsForTimeIntervalDVRes.ErrorMsgId == 0)
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
                    //ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = responseContent;
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> GetAllBetDetailsForTransactionID(string TransactionID)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=GetAllBetDetailsForTransactionID" +
                            "&Key=" + SecretKey +
                            "&Time=" + currTime +
                            "&TransactionID=" + TransactionID;

                 

                string q = DESEncrypt(QS);
                string s = BuildMD5(QS + MD5Key + currTime + SecretKey);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("q", q);
                geek.Add("s", s);


                HttpResponseMessage response = await PostApiAsync(this.BaseUrl, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    GetAllBetDetailsForTransactionIDRes = DeserializeFromXml<GetAllBetDetailsForTransactionIDResponse>(responseContent);
                     

                    if (GetAllBetDetailsForTransactionIDRes.ErrorMsgId == 0)
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
                    //ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = responseContent;
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }


        public async Task<bool> GetUserBetItemDV(string Username,string FromTime,string ToTime,string Offset)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=GetUserBetItemDV" +
                            "&Key=" + SecretKey +
                            "&Time=" + currTime +
                            "&Username=" + Username +
                            "&FromTime=" + FromTime +
                            "&ToTime=" + ToTime +
                            "&Offset=" + Offset;



                string q = DESEncrypt(QS);
                string s = BuildMD5(QS + MD5Key + currTime + SecretKey);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("q", q);
                geek.Add("s", s);


                HttpResponseMessage response = await PostApiAsync(this.BaseUrl, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    GetUserBetItemDVRes = DeserializeFromXml<GetUserBetItemDVResponse>(responseContent);
                    
                    if (GetUserBetItemDVRes.ErrorMsgId == 0)
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
                    //ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = responseContent;
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }


        public async Task<bool> GetUserBetAmountDV(string Username = null, string StartDate = null, string TimeRange = null)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=GetUserBetAmountDV " +
                            "&Key=" + SecretKey +
                            "&Time=" + currTime;

                if (!string.IsNullOrEmpty(Username)) QS += "&Username=" + Username;
                if (!string.IsNullOrEmpty(StartDate)) QS += "&StartDate=" + StartDate;
                if (!string.IsNullOrEmpty(TimeRange)) QS += "&TimeRange=" + TimeRange;


                string q = DESEncrypt(QS);
                string s = BuildMD5(QS + MD5Key + currTime + SecretKey);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("q", q);
                geek.Add("s", s);


                HttpResponseMessage response = await PostApiAsync(this.BaseUrl, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    GetUserBetAmountRes  = DeserializeFromXml<GetUserBetAmountResponse>(responseContent);
                    
                    if (GetUserBetAmountRes.ErrorMsgId == 0)
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
                    //ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = responseContent;
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }


        public async Task<bool> GetUserWinLost(string Username, string FromTime, string ToTime, int? Type = null)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=GetUserWinLost" +
                            "&Key=" + SecretKey +
                            "&Time=" + currTime +
                            "&Username=" + Username +
                            "&FromTime=" + FromTime +
                            "&ToTime=" + ToTime;

                if (Type.HasValue) QS += "&Type=" + Type.Value;


                string q = DESEncrypt(QS);
                string s = BuildMD5(QS + MD5Key + currTime + SecretKey);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("q", q);
                geek.Add("s", s);


                HttpResponseMessage response = await PostApiAsync(this.BaseUrl, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    GetUserWinLostRes = DeserializeFromXml<GetUserWinLostResponse>(responseContent);

                    if (GetUserWinLostRes.ErrorMsgId == 0)
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
                    //ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = responseContent;
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }


        public async Task<bool> GetTransactionDetails(string FromTime, string ToTime,string Username = null)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=GetTransactionDetails" +
                            "&Key=" + SecretKey +
                            "&Time=" + currTime +
                            "&FromTime=" + FromTime +
                            "&ToTime=" + ToTime;

                if (!string.IsNullOrEmpty(Username)) QS += "&Username=" + Username;

                string q = DESEncrypt(QS);
                string s = BuildMD5(QS + MD5Key + currTime + SecretKey);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("q", q);
                geek.Add("s", s);


                HttpResponseMessage response = await PostApiAsync(this.BaseUrl, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    GetTransactionDetailsRes = DeserializeFromXml<GetTransactionDetailsResponse>(responseContent);
                    
                    if (GetTransactionDetailsRes.ErrorMsgId == 0)
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
                    //ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = responseContent;
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }


        public async Task<bool> GetActiveHostList(string FromTime, string ToTime, string Username = null)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=GetActiveHostList" +
                            "&Time=" + currTime+
                            "&Key=" + SecretKey;

                if (!string.IsNullOrEmpty(Username)) QS += "&Username=" + Username;

                string q = DESEncrypt(QS);
                string s = BuildMD5(QS + MD5Key + currTime + SecretKey);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("q", q);
                geek.Add("s", s);


                HttpResponseMessage response = await PostApiAsync(this.BaseUrl, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    GetActiveHostListRes = DeserializeFromXml<GetActiveHostListResponse>(responseContent);
                     
                    if (GetActiveHostListRes != null)
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
                    //ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = responseContent;
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> GetCancelledBetDetails(string Username=null,string GameID = null, string FromTime = null, string ToTime = null)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=GetCancelledBetDetails" +
                            "&Key=" + SecretKey +
                            "&Time=" + currTime;

                if (!string.IsNullOrEmpty(Username)) QS += "&Username=" + Username;
                if (!string.IsNullOrEmpty(GameID)) QS += "&GameID=" + GameID;
                if (!string.IsNullOrEmpty(FromTime)) QS += "&FromTime=" + FromTime;
                if (!string.IsNullOrEmpty(ToTime)) QS += "&ToTime=" + ToTime;

                string q = DESEncrypt(QS);
                string s = BuildMD5(QS + MD5Key + currTime + SecretKey);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("q", q);
                geek.Add("s", s);


                HttpResponseMessage response = await PostApiAsync(this.BaseUrl, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    GetCancelledBetDetailsRes = DeserializeFromXml<GetCancelledBetDetailsResponse>(responseContent);
                     
                    if (GetCancelledBetDetailsRes.ErrorMsgId == 0)
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
                    //ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = responseContent;
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }


        public async Task<bool> GetGameErrorDetail(string Username = null, string GameID = null, string HostList = null, string FromTime = null, string ToTime = null)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=GetGameErrorDetail" +
                            "&Key=" + SecretKey +
                            "&Time=" + currTime;

                if (!string.IsNullOrEmpty(Username)) QS += "&Username=" + Username;
                if (!string.IsNullOrEmpty(GameID)) QS += "&GameID=" + GameID;
                if (!string.IsNullOrEmpty(HostList)) QS += "&HostList=" + HostList;
                if (!string.IsNullOrEmpty(FromTime)) QS += "&FromTime=" + FromTime;
                if (!string.IsNullOrEmpty(ToTime)) QS += "&ToTime=" + ToTime;

                string q = DESEncrypt(QS);
                string s = BuildMD5(QS + MD5Key + currTime + SecretKey);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("q", q);
                geek.Add("s", s);


                HttpResponseMessage response = await PostApiAsync(this.BaseUrl, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    GetGameErrorDetailRes = DeserializeFromXml<GetGameErrorDetailResponse>(responseContent);
                    
                    if (GetGameErrorDetailRes != null)
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
                    //ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = responseContent;
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> GetUnsuccessfulBetDetails(string Username = null, string GameID = null, string FromTime = null, string ToTime = null,int? PageNum = 1)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=GetUnsuccessfulBetDetails" +
                            "&Key=" + SecretKey +
                            "&Time=" + currTime;

                if (!string.IsNullOrEmpty(Username)) QS += "&Username=" + Username;
                if (!string.IsNullOrEmpty(GameID)) QS += "&GameID=" + GameID;
                if (!string.IsNullOrEmpty(FromTime)) QS += "&FromTime=" + FromTime;
                if (!string.IsNullOrEmpty(ToTime)) QS += "&ToTime=" + ToTime;
                if (PageNum.HasValue) QS += "&PageNum=" + PageNum.Value;

                string q = DESEncrypt(QS);
                string s = BuildMD5(QS + MD5Key + currTime + SecretKey);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("q", q);
                geek.Add("s", s);


                HttpResponseMessage response = await PostApiAsync(this.BaseUrl, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    GetUnsuccessfulBetDetailsRes = DeserializeFromXml<GetUnsuccessfulBetDetailsResponse>(responseContent);
                    
                    if (GetUnsuccessfulBetDetailsRes.ErrorMsgId == 0)
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
                    //ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = responseContent;
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }
 


        public async Task<bool> DebitBalanceDV(string Username, string OrderId, decimal DebitAmount)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=DebitBalanceDV" +
                            "&Key=" + SecretKey +
                            "&Time=" + currTime +
                            "&Username=" + Username +
                            "&OrderId=" + OrderId +
                            "&DebitAmount=" + DebitAmount;
                 

                string q = DESEncrypt(QS);
                string s = BuildMD5(QS + MD5Key + currTime + SecretKey);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("q", q);
                geek.Add("s", s);


                HttpResponseMessage response = await PostApiAsync(this.BaseUrl, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    DebitBalanceDVRes = DeserializeFromXml<DebitBalanceDVResponse>(responseContent);
                    
                    if (DebitBalanceDVRes.ErrorMsgId == 0)
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
                    //ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = responseContent;
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> DebitAllBalanceDV(string Username, string OrderId)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=DebitAllBalanceDV" +
                            "&Key=" + SecretKey +
                            "&Time=" + currTime +
                            "&Username=" + Username +
                            "&OrderId=" + OrderId;


                string q = DESEncrypt(QS);
                string s = BuildMD5(QS + MD5Key + currTime + SecretKey);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("q", q);
                geek.Add("s", s);


                HttpResponseMessage response = await PostApiAsync(this.BaseUrl, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    DebitAllBalanceDVRes = DeserializeFromXml<DebitAllBalanceDVResponse>(responseContent);
                    
                    if (DebitAllBalanceDVRes.ErrorMsgId == 0)
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
                    //ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = responseContent;
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> CreditBalanceDV(string Username, string OrderId, decimal CreditAmount, string CurrencyType)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=CreditBalanceDV" +
                            "&Key=" + SecretKey +
                            "&Time=" + currTime +
                            "&Username=" + Username +
                            "&OrderId=" + OrderId +
                            "&CreditAmount=" + CreditAmount +
                            "&CurrencyType=" + CurrencyType;

                string q = DESEncrypt(QS);
                string s = BuildMD5(QS + MD5Key + currTime + SecretKey);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("q", q);
                geek.Add("s", s);


                HttpResponseMessage response = await PostApiAsync(this.BaseUrl, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    CreditBalanceDVRes = DeserializeFromXml<CreditBalanceDVResponse>(responseContent);
                    
                    if (CreditBalanceDVRes.ErrorMsgId == 0)
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
                    //ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = responseContent;
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }


        public async Task<bool> CheckOrderId( string OrderId)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=CheckOrderId" +
                            "&Key=" + SecretKey +
                            "&Time=" + currTime +
                            "&OrderId=" + OrderId;
                            

                string q = DESEncrypt(QS);
                string s = BuildMD5(QS + MD5Key + currTime + SecretKey);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("q", q);
                geek.Add("s", s);


                HttpResponseMessage response = await PostApiAsync(this.BaseUrl, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    CheckOrderIdRes  = DeserializeFromXml<CheckOrderIdResponse>(responseContent);
                    
                    if (CheckOrderIdRes.ErrorMsgId == 0)
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
                    //ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = responseContent;
                    return false;
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> CheckOrderDetailsDV(string OrderId)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=CheckOrderDetailsDV" +
                            "&Key=" + SecretKey +
                            "&Time=" + currTime +
                            "&OrderId=" + OrderId;


                string q = DESEncrypt(QS);
                string s = BuildMD5(QS + MD5Key + currTime + SecretKey);

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("q", q);
                geek.Add("s", s);


                HttpResponseMessage response = await PostApiAsync(this.BaseUrl, geek);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    CheckOrderDetailsDVRes = DeserializeFromXml<CheckOrderDetailsDVResponse>(responseContent);
                    
                    if (CheckOrderDetailsDVRes.ErrorMsgId == 0)
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
                    //ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    ErrorMessage = responseContent;
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


        [XmlRoot("RegUserInfoResponse", Namespace = "")]
        public class RegUserInfoResponse
        {
            public int ErrorMsgId { get; set; }
            public string ErrorMsg { get; set; }
            public string Username { get; set; }
        }




        [XmlRoot(ElementName = "VerifyUsernameResponse", Namespace = "")]
        public class VerifyUsernameResponse
        {

            [XmlElement(ElementName = "ErrorMsgId")]
            public int ErrorMsgId { get; set; }

            [XmlElement(ElementName = "ErrorMsg")]
            public string ErrorMsg { get; set; }

            [XmlElement(ElementName = "IsExist")]
            public bool IsExist { get; set; }

            [XmlElement(ElementName = "Username")]
            public string Username { get; set; }

            [XmlAttribute(AttributeName = "xsd")]
            public string Xsd { get; set; }

            [XmlAttribute(AttributeName = "xsi")]
            public string Xsi { get; set; }

            [XmlText]
            public string Text { get; set; }
        }



        [XmlRoot(ElementName = "GetUserStatusResponse")]
        public class GetUserStatusDVResponse
        {

            [XmlElement(ElementName = "ErrorMsgId")]
            public int ErrorMsgId { get; set; }

            [XmlElement(ElementName = "ErrorMsg")]
            public string ErrorMsg { get; set; }

            [XmlElement(ElementName = "IsSuccess")]
            public bool IsSuccess { get; set; }

            [XmlElement(ElementName = "Username")]
            public string Username { get; set; }

            [XmlElement(ElementName = "Balance")]
            public decimal Balance { get; set; }

            [XmlElement(ElementName = "Online")]
            public bool Online { get; set; }

            [XmlElement(ElementName = "Betted")]
            public bool Betted { get; set; }

            [XmlElement(ElementName = "BettedAmount")]
            public decimal BettedAmount { get; set; }

            [XmlElement(ElementName = "MaxBalance")]
            public decimal MaxBalance { get; set; }

            [XmlElement(ElementName = "MaxWinning")]
            public decimal MaxWinning { get; set; }

            [XmlAttribute(AttributeName = "xsd")]
            public string Xsd { get; set; }

            [XmlAttribute(AttributeName = "xsi")]
            public string Xsi { get; set; }

            [XmlText]
            public string Text { get; set; }
        }

        [XmlRoot(ElementName = "BetLimit")]
        public class BetLimit
        {

            [XmlElement(ElementName = "RuleID")]
            public int RuleID { get; set; }

            [XmlElement(ElementName = "Min")]
            public decimal Min { get; set; }

            [XmlElement(ElementName = "Max")]
            public decimal Max { get; set; }
        }

        [XmlRoot(ElementName = "BetLimitList")]
        public class QueryBetLimitResponse
        {

            [XmlElement(ElementName = "BetLimit")]
            public List<BetLimit> BetLimit { get; set; }

            [XmlAttribute(AttributeName = "GameType")]
            public string GameType { get; set; }

            [XmlText]
            public double Text { get; set; }
        }



        [XmlRoot(ElementName = "SetAllBetLimitResponse")]
        public class SetAllBetLimitResponse
        {

            [XmlElement(ElementName = "ErrorMsgId")]
            public int ErrorMsgId { get; set; }

            [XmlElement(ElementName = "ErrorMsg")]
            public string ErrorMsg { get; set; }

            [XmlAttribute(AttributeName = "xsd")]
            public string Xsd { get; set; }

            [XmlAttribute(AttributeName = "xsi")]
            public string Xsi { get; set; }

            [XmlText]
            public string Text { get; set; }
        }


        [XmlRoot(ElementName = "GetUserMaxBalanceResponse")]
        public class GetUserMaxBalanceResponse
        {

            [XmlElement(ElementName = "ErrorMsgId")]
            public int ErrorMsgId { get; set; }

            [XmlElement(ElementName = "ErrorMsg")]
            public string ErrorMsg { get; set; }

            [XmlElement(ElementName = "MaxBalance")]
            public decimal MaxBalance { get; set; }

            [XmlAttribute(AttributeName = "xsd")]
            public string Xsd { get; set; }

            [XmlAttribute(AttributeName = "xsi")]
            public string Xsi { get; set; }

            [XmlText]
            public string Text { get; set; }
        }




        [XmlRoot(ElementName = "SetUserMaxBalanceResponse", Namespace = "")]
        public class SetUserMaxBalanceResponse
        {

            [XmlElement(ElementName = "ErrorMsgId")]
            public int ErrorMsgId { get; set; }

            [XmlElement(ElementName = "ErrorMsg")]
            public string ErrorMsg { get; set; }

            [XmlAttribute(AttributeName = "xsd")]
            public string Xsd { get; set; }

            [XmlAttribute(AttributeName = "xsi")]
            public string Xsi { get; set; }

            [XmlText]
            public string Text { get; set; }
        }

        [XmlRoot(ElementName = "SetUserMaxWinningResponse", Namespace = "")]
        public class SetUserMaxWinningResponse
        {

            [XmlElement(ElementName = "ErrorMsgId")]
            public int ErrorMsgId { get; set; }

            [XmlElement(ElementName = "ErrorMsg")]
            public string ErrorMsg { get; set; }

            [XmlAttribute(AttributeName = "xsd")]
            public string Xsd { get; set; }

            [XmlAttribute(AttributeName = "xsi")]
            public string Xsi { get; set; }

            [XmlText]
            public string Text { get; set; }
        }


        [XmlRoot(ElementName = "BetLimit")]
        public class QueryUserBetLimitBetLimit
        {

            [XmlElement(ElementName = "RuleID")]
            public int RuleID { get; set; }

            [XmlElement(ElementName = "Min")]
            public decimal Min { get; set; }

            [XmlElement(ElementName = "Max")]
            public decimal Max { get; set; }
        }

        [XmlRoot(ElementName = "BetLimitList")]
        public class QueryUserBetLimitResponse
        {

            [XmlElement(ElementName = "BetLimit")]
            public List<QueryUserBetLimitBetLimit> BetLimit { get; set; }

            [XmlAttribute(AttributeName = "GameType")]
            public string GameType { get; set; }

            [XmlText]
            public double Text { get; set; }
        }



        [XmlRoot(ElementName = "UnfinishedGameDetail")]
        public class UnfinishedGameDetail
        {

            [XmlElement(ElementName = "GameCode")]
            public string GameCode { get; set; }

            [XmlElement(ElementName = "BetAmount")]
            public decimal BetAmount { get; set; }

            [XmlElement(ElementName = "HostID")]
            public int HostID { get; set; }
        }

        [XmlRoot(ElementName = "UnfinishedGameDetailList")]
        public class UnfinishedGameDetailList
        {

            [XmlElement(ElementName = "UnfinishedGameDetail")]
            public List<UnfinishedGameDetail> UnfinishedGameDetail { get; set; }
        }

        [XmlRoot(ElementName = "GetUnfinishedGameResponse", Namespace = "")]
        public class GetUnfinishedGameResponse
        {

            [XmlElement(ElementName = "Username")]
            public string Username { get; set; }

            [XmlElement(ElementName = "UnfinishedGameDetailList")]
            public UnfinishedGameDetailList UnfinishedGameDetailList { get; set; }

            [XmlElement(ElementName = "ErrorMsgId")]
            public int ErrorMsgId { get; set; }

            [XmlElement(ElementName = "ErrorMsg")]
            public string ErrorMsg { get; set; }

            [XmlAttribute(AttributeName = "xsd")]
            public string Xsd { get; set; }

            [XmlAttribute(AttributeName = "xsi")]
            public string Xsi { get; set; }

            [XmlText]
            public string Text { get; set; }
        }

        

        [XmlRoot(ElementName = "LoginRequestResponse")]
        public class LoginRequestResponse
        {

            [XmlElement(ElementName = "ErrorMsgId")]
            public int ErrorMsgId { get; set; }

            [XmlElement(ElementName = "ErrorMsg")]
            public string ErrorMsg { get; set; }

            [XmlElement(ElementName = "Token")]
            public string Token { get; set; }

            [XmlElement(ElementName = "DisplayName")]
            public string DisplayName { get; set; }

            [XmlAttribute(AttributeName = "xsd")]
            public string Xsd { get; set; }

            [XmlAttribute(AttributeName = "xsi")]
            public string Xsi { get; set; }

            [XmlText]
            public string Text { get; set; }
        }




        [XmlRoot(ElementName = "LoginRequestTryToPlayResponse")]
        public class LoginRequestTryToPlayResponse
        {

            [XmlElement(ElementName = "ErrorMsgId")]
            public int ErrorMsgId { get; set; }

            [XmlElement(ElementName = "ErrorMsg")]
            public string ErrorMsg { get; set; }

            [XmlElement(ElementName = "Token")]
            public string Token { get; set; }

            [XmlElement(ElementName = "DisplayName")]
            public string DisplayName { get; set; }

            [XmlAttribute(AttributeName = "xsd")]
            public string Xsd { get; set; }

            [XmlAttribute(AttributeName = "xsi")]
            public string Xsi { get; set; }

            [XmlText]
            public string Text { get; set; }
        }



        [XmlRoot(ElementName = "KickUserResponse", Namespace = "")]
        public class KickUserResponse
        {

            [XmlElement(ElementName = "ErrorMsgId")]
            public int ErrorMsgId { get; set; }

            [XmlElement(ElementName = "ErrorMsg")]
            public string ErrorMsg { get; set; }

            [XmlAttribute(AttributeName = "xsd")]
            public string Xsd { get; set; }

            [XmlAttribute(AttributeName = "xsi")]
            public string Xsi { get; set; }

            [XmlText]
            public string Text { get; set; }
        }


        [XmlRoot(ElementName = "GetAllBetDetailsResponse", Namespace = "")]
        public class GetAllBetDetailsDVResponse
        {
            public int NumOfRecord { get; set; }
            public List<BetDetail> BetDetailList { get; set; }
            public int ErrorMsgId { get; set; }
            public string ErrorMsg { get; set; }
        }

        public class BetDetail
        {
            public DateTime BetTime { get; set; }
            public DateTime PayoutTime { get; set; }
            public string Username { get; set; }
            public long GameID { get; set; }
            public long BetID { get; set; }
            public string Currency { get; set; }
            public bool BuyFreeGame { get; set; }
            public decimal BetAmount { get; set; }
            public decimal Rolling { get; set; }
            public decimal ResultAmount { get; set; }
            public decimal Balance { get; set; }
            public string GameType { get; set; }
            public int BetSource { get; set; }
            public string Detail { get; set; }
            public int? TransactionID { get; set; }
            public decimal JackpotContribution { get; set; }
            public int? PrizeID { get; set; }
            public GameResult GameResult { get; set; }
        }

        public class GameResult
        {
            public BaccaratResult BaccaratResult { get; set; }
            public ColorSicboResult ColorSicboResult { get; set; }
            public ShakeShakeResult ShakeShakeResult { get; set; }
            public PulaPutiResult PulaPutiResult { get; set; }
            public ThaiHiLoResult ThaiHiLoResult { get; set; }
            public Rolling7Result Rolling7Result { get; set; }
            public RouletteResult RouletteResult { get; set; }
        }

        public class BaccaratResult
        {
            public Card PlayerCard1 { get; set; }
            public Card PlayerCard2 { get; set; }
            public Card PlayerCard3 { get; set; }
            public Card BankerCard1 { get; set; }
            public Card BankerCard2 { get; set; }
            public Card BankerCard3 { get; set; }
            public BaccaratResultDetail ResultDetail { get; set; }
        }

        public class Card
        {
            public int Suit { get; set; }
            public int Rank { get; set; }
        }

        public class BaccaratResultDetail
        {
            public bool BRTie { get; set; }
            public bool BRPlayerWin { get; set; }
            public bool BRBankerWin { get; set; }
            public bool BRPlayerPair { get; set; }
            public bool BRBankerPair { get; set; }
        }

        public class ColorSicboResult
        {
            public int Dice1 { get; set; }
            public int Dice2 { get; set; }
            public int Dice3 { get; set; }
        }

        public class ShakeShakeResult
        {
            public int Dice1 { get; set; }
            public int Dice2 { get; set; }
            public int TotalPoint { get; set; }
        }

        public class PulaPutiResult
        {
            public int Ball1 { get; set; }
            public int Ball2 { get; set; }
            public int Ball3 { get; set; }
        }

        public class ThaiHiLoResult
        {
            public int Dice1 { get; set; }
            public int Dice2 { get; set; }
            public int Dice3 { get; set; }
            public int TotalPoint { get; set; }
        }

        public class Rolling7Result
        {
            public int Dice1 { get; set; }
            public int Dice2 { get; set; }
            public int TotalPoint { get; set; }
        }

        public class RouletteResult
        {
            public int Point { get; set; }
        }




        [XmlRoot(ElementName = "PlayerCard1")]
        public class GetAllBetDetailsForTransactionIDPlayerCard1
        {

            [XmlElement(ElementName = "Suit")]
            public int Suit { get; set; }

            [XmlElement(ElementName = "Rank")]
            public int Rank { get; set; }
        }

        [XmlRoot(ElementName = "PlayerCard2")]
        public class GetAllBetDetailsForTransactionIDPlayerCard2
        {

            [XmlElement(ElementName = "Suit")]
            public int Suit { get; set; }

            [XmlElement(ElementName = "Rank")]
            public int Rank { get; set; }
        }

        [XmlRoot(ElementName = "PlayerCard3")]
        public class GetAllBetDetailsForTransactionIDPlayerCard3
        {

            [XmlElement(ElementName = "Suit")]
            public int Suit { get; set; }

            [XmlElement(ElementName = "Rank")]
            public int Rank { get; set; }
        }

        [XmlRoot(ElementName = "BankerCard1")]
        public class GetAllBetDetailsForTransactionIDBankerCard1
        {

            [XmlElement(ElementName = "Suit")]
            public int Suit { get; set; }

            [XmlElement(ElementName = "Rank")]
            public int Rank { get; set; }
        }

        [XmlRoot(ElementName = "BankerCard2")]
        public class GetAllBetDetailsForTransactionIDBankerCard2
        {

            [XmlElement(ElementName = "Suit")]
            public int Suit { get; set; }

            [XmlElement(ElementName = "Rank")]
            public int Rank { get; set; }
        }

        [XmlRoot(ElementName = "ResultDetail")]
        public class GetAllBetDetailsForTransactionIDResultDetail
        {

            [XmlElement(ElementName = "BRTie")]
            public bool BRTie { get; set; }

            [XmlElement(ElementName = "BRPlayerWin")]
            public bool BRPlayerWin { get; set; }

            [XmlElement(ElementName = "BRBankerWin")]
            public bool BRBankerWin { get; set; }

            [XmlElement(ElementName = "BRPlayerPair")]
            public bool BRPlayerPair { get; set; }

            [XmlElement(ElementName = "BRBankerPair")]
            public bool BRBankerPair { get; set; }

            [XmlElement(ElementName = "BRS2CardsLuckySix")]
            public bool BRS2CardsLuckySix { get; set; }

            [XmlElement(ElementName = "BRS3CardsLuckySix")]
            public bool BRS3CardsLuckySix { get; set; }

            [XmlElement(ElementName = "BRSSS2CardsLuckySix")]
            public bool BRSSS2CardsLuckySix { get; set; }

            [XmlElement(ElementName = "BRSSS3CardsLuckySix")]
            public bool BRSSS3CardsLuckySix { get; set; }

            [XmlElement(ElementName = "BRSPlayerBonus")]
            public bool BRSPlayerBonus { get; set; }

            [XmlElement(ElementName = "BRSBankerBonus")]
            public bool BRSBankerBonus { get; set; }

            [XmlElement(ElementName = "BRSSTie")]
            public bool BRSSTie { get; set; }

            [XmlElement(ElementName = "BRSSPlayerWin")]
            public bool BRSSPlayerWin { get; set; }

            [XmlElement(ElementName = "BRSSBankerWin")]
            public bool BRSSBankerWin { get; set; }

            [XmlElement(ElementName = "BRSSPlayerPair")]
            public bool BRSSPlayerPair { get; set; }

            [XmlElement(ElementName = "BRSSBankerPair")]
            public bool BRSSBankerPair { get; set; }

            [XmlElement(ElementName = "BRPlayerNatural")]
            public bool BRPlayerNatural { get; set; }

            [XmlElement(ElementName = "BRBankerNatural")]
            public bool BRBankerNatural { get; set; }

            [XmlElement(ElementName = "BRSSPlayerNatural")]
            public bool BRSSPlayerNatural { get; set; }

            [XmlElement(ElementName = "BRSSBankerNatural")]
            public bool BRSSBankerNatural { get; set; }

            [XmlElement(ElementName = "BRSAnyPair")]
            public bool BRSAnyPair { get; set; }

            [XmlElement(ElementName = "BRSSSAnyPair")]
            public bool BRSSSAnyPair { get; set; }

            [XmlElement(ElementName = "BRSPerfectPair")]
            public bool BRSPerfectPair { get; set; }

            [XmlElement(ElementName = "BRSSSPerfectPair")]
            public bool BRSSSPerfectPair { get; set; }

            [XmlElement(ElementName = "BRSSSPlayerBonus")]
            public bool BRSSSPlayerBonus { get; set; }

            [XmlElement(ElementName = "BRSSSBankerBonus")]
            public bool BRSSSBankerBonus { get; set; }

            [XmlElement(ElementName = "BRSSSLuckySix")]
            public bool BRSSSLuckySix { get; set; }

            [XmlElement(ElementName = "BRSLuckySix")]
            public bool BRSLuckySix { get; set; }
        }

        [XmlRoot(ElementName = "BaccaratResult")]
        public class GetAllBetDetailsForTransactionIDBaccaratResult
        {

            [XmlElement(ElementName = "PlayerCard1")]
            public GetAllBetDetailsForTransactionIDPlayerCard1 PlayerCard1 { get; set; }

            [XmlElement(ElementName = "PlayerCard2")]
            public GetAllBetDetailsForTransactionIDPlayerCard2 PlayerCard2 { get; set; }

            [XmlElement(ElementName = "PlayerCard3")]
            public GetAllBetDetailsForTransactionIDPlayerCard3 PlayerCard3 { get; set; }

            [XmlElement(ElementName = "BankerCard1")]
            public GetAllBetDetailsForTransactionIDBankerCard1 BankerCard1 { get; set; }

            [XmlElement(ElementName = "BankerCard2")]
            public GetAllBetDetailsForTransactionIDBankerCard2 BankerCard2 { get; set; }

            [XmlElement(ElementName = "ResultDetail")]
            public GetAllBetDetailsForTransactionIDResultDetail ResultDetail { get; set; }
        }

        [XmlRoot(ElementName = "Result")]
        public class GetAllBetDetailsForTransactionIDResult
        {

            [XmlElement(ElementName = "BaccaratResult")]
            public GetAllBetDetailsForTransactionIDBaccaratResult BaccaratResult { get; set; }
        }

        [XmlRoot(ElementName = "BetDetail")]
        public class GetAllBetDetailsForTransactionIDBetDetail
        {

            [XmlElement(ElementName = "BetTime")]
            public DateTime BetTime { get; set; }

            [XmlElement(ElementName = "PayoutTime")]
            public DateTime PayoutTime { get; set; }

            [XmlElement(ElementName = "Username")]
            public string Username { get; set; }

            [XmlElement(ElementName = "HostID")]
            public int HostID { get; set; }

            [XmlElement(ElementName = "GameID")]
            public double GameID { get; set; }

            [XmlElement(ElementName = "Round")]
            public int Round { get; set; }

            [XmlElement(ElementName = "Set")]
            public int Set { get; set; }

            [XmlElement(ElementName = "BetID")]
            public double BetID { get; set; }

            [XmlElement(ElementName = "Currency")]
            public string Currency { get; set; }

            [XmlElement(ElementName = "BetAmount")]
            public int BetAmount { get; set; }

            [XmlElement(ElementName = "Rolling")]
            public int Rolling { get; set; }

            [XmlElement(ElementName = "ResultAmount")]
            public int ResultAmount { get; set; }

            [XmlElement(ElementName = "Balance")]
            public int Balance { get; set; }

            [XmlElement(ElementName = "GameType")]
            public string GameType { get; set; }

            [XmlElement(ElementName = "BetType")]
            public int BetType { get; set; }

            [XmlElement(ElementName = "BetSource")]
            public int BetSource { get; set; }

            [XmlElement(ElementName = "Detail")]
            public object Detail { get; set; }

            [XmlElement(ElementName = "TransactionID")]
            public double TransactionID { get; set; }

            [XmlElement(ElementName = "BetConfirmation")]
            public int BetConfirmation { get; set; }
        }

        [XmlRoot(ElementName = "BetDetailList")]
        public class GetAllBetDetailsForTransactionIDBetDetailList
        {

            [XmlElement(ElementName = "BetDetail")]
            public List<GetAllBetDetailsForTransactionIDBetDetail> BetDetail { get; set; }
        }

        [XmlRoot(ElementName = "GetAllBetDetailsForTransactionIDResponse")]
        public class GetAllBetDetailsForTransactionIDResponse
        {

            [XmlElement(ElementName = "ErrorMsgId")]
            public int ErrorMsgId { get; set; }

            [XmlElement(ElementName = "ErrorMsg")]
            public string ErrorMsg { get; set; }

            [XmlElement(ElementName = "NumOfRecord")]
            public int NumOfRecord { get; set; }

            [XmlElement(ElementName = "Result")]
            public GetAllBetDetailsForTransactionIDResult Result { get; set; }

            [XmlElement(ElementName = "BetDetailList")]
            public GetAllBetDetailsForTransactionIDBetDetailList BetDetailList { get; set; }

            [XmlAttribute(AttributeName = "xsd")]
            public string Xsd { get; set; }

            [XmlAttribute(AttributeName = "xsi")]
            public string Xsi { get; set; }

            [XmlText]
            public string Text { get; set; }
        }





        [XmlRoot(ElementName = "PlayerCard1")]
        public class GetUserBetItemPlayerCard1
        {

            [XmlElement(ElementName = "Suit")]
            public int Suit { get; set; }

            [XmlElement(ElementName = "Rank")]
            public int Rank { get; set; }
        }

        [XmlRoot(ElementName = "PlayerCard2")]
        public class GetUserBetItemPlayerCard2
        {

            [XmlElement(ElementName = "Suit")]
            public int Suit { get; set; }

            [XmlElement(ElementName = "Rank")]
            public int Rank { get; set; }
        }

        [XmlRoot(ElementName = "PlayerCard3")]
        public class GetUserBetItemPlayerCard3
        {

            [XmlElement(ElementName = "Suit")]
            public int Suit { get; set; }

            [XmlElement(ElementName = "Rank")]
            public int Rank { get; set; }
        }

        [XmlRoot(ElementName = "BankerCard1")]
        public class GetUserBetItemBankerCard1
        {

            [XmlElement(ElementName = "Suit")]
            public int Suit { get; set; }

            [XmlElement(ElementName = "Rank")]
            public int Rank { get; set; }
        }

        [XmlRoot(ElementName = "BankerCard2")]
        public class GetUserBetItemBankerCard2
        {

            [XmlElement(ElementName = "Suit")]
            public int Suit { get; set; }

            [XmlElement(ElementName = "Rank")]
            public int Rank { get; set; }
        }

        [XmlRoot(ElementName = "ResultDetail")]
        public class GetUserBetItemResultDetail
        {

            [XmlElement(ElementName = "BRTie")]
            public bool BRTie { get; set; }

            [XmlElement(ElementName = "BRPlayerWin")]
            public bool BRPlayerWin { get; set; }

            [XmlElement(ElementName = "BRBankerWin")]
            public bool BRBankerWin { get; set; }

            [XmlElement(ElementName = "BRPlayerPair")]
            public bool BRPlayerPair { get; set; }

            [XmlElement(ElementName = "BRBankerPair")]
            public bool BRBankerPair { get; set; }

            [XmlElement(ElementName = "BRS2CardsLuckySix")]
            public bool BRS2CardsLuckySix { get; set; }

            [XmlElement(ElementName = "BRS3CardsLuckySix")]
            public bool BRS3CardsLuckySix { get; set; }

            [XmlElement(ElementName = "BRSSS2CardsLuckySix")]
            public bool BRSSS2CardsLuckySix { get; set; }

            [XmlElement(ElementName = "BRSSS3CardsLuckySix")]
            public bool BRSSS3CardsLuckySix { get; set; }

            [XmlElement(ElementName = "BRSPlayerBonus")]
            public bool BRSPlayerBonus { get; set; }

            [XmlElement(ElementName = "BRSBankerBonus")]
            public bool BRSBankerBonus { get; set; }

            [XmlElement(ElementName = "BRSSTie")]
            public bool BRSSTie { get; set; }

            [XmlElement(ElementName = "BRSSPlayerWin")]
            public bool BRSSPlayerWin { get; set; }

            [XmlElement(ElementName = "BRSSBankerWin")]
            public bool BRSSBankerWin { get; set; }

            [XmlElement(ElementName = "BRSSPlayerPair")]
            public bool BRSSPlayerPair { get; set; }

            [XmlElement(ElementName = "BRSSBankerPair")]
            public bool BRSSBankerPair { get; set; }

            [XmlElement(ElementName = "BRPlayerNatural")]
            public bool BRPlayerNatural { get; set; }

            [XmlElement(ElementName = "BRBankerNatural")]
            public bool BRBankerNatural { get; set; }

            [XmlElement(ElementName = "BRSSPlayerNatural")]
            public bool BRSSPlayerNatural { get; set; }

            [XmlElement(ElementName = "BRSSBankerNatural")]
            public bool BRSSBankerNatural { get; set; }

            [XmlElement(ElementName = "BRSAnyPair")]
            public bool BRSAnyPair { get; set; }

            [XmlElement(ElementName = "BRSSSAnyPair")]
            public bool BRSSSAnyPair { get; set; }

            [XmlElement(ElementName = "BRSPerfectPair")]
            public bool BRSPerfectPair { get; set; }

            [XmlElement(ElementName = "BRSSSPerfectPair")]
            public bool BRSSSPerfectPair { get; set; }

            [XmlElement(ElementName = "BRSSSPlayerBonus")]
            public bool BRSSSPlayerBonus { get; set; }

            [XmlElement(ElementName = "BRSSSBankerBonus")]
            public bool BRSSSBankerBonus { get; set; }

            [XmlElement(ElementName = "BRSSSLuckySix")]
            public bool BRSSSLuckySix { get; set; }

            [XmlElement(ElementName = "BRSLuckySix")]
            public bool BRSLuckySix { get; set; }
        }

        [XmlRoot(ElementName = "BaccaratResult")]
        public class GetUserBetItemBaccaratResult
        {

            [XmlElement(ElementName = "PlayerCard1")]
            public GetUserBetItemPlayerCard1 PlayerCard1 { get; set; }

            [XmlElement(ElementName = "PlayerCard2")]
            public GetUserBetItemPlayerCard2 PlayerCard2 { get; set; }

            [XmlElement(ElementName = "PlayerCard3")]
            public GetUserBetItemPlayerCard3 PlayerCard3 { get; set; }

            [XmlElement(ElementName = "BankerCard1")]
            public GetUserBetItemBankerCard1 BankerCard1 { get; set; }

            [XmlElement(ElementName = "BankerCard2")]
            public GetUserBetItemBankerCard2 BankerCard2 { get; set; }

            [XmlElement(ElementName = "ResultDetail")]
            public GetUserBetItemResultDetail ResultDetail { get; set; }
        }

        [XmlRoot(ElementName = "GameResult")]
        public class GetUserBetItemGameResult
        {

            [XmlElement(ElementName = "BaccaratResult")]
            public GetUserBetItemBaccaratResult BaccaratResult { get; set; }
        }

        [XmlRoot(ElementName = "Detail")]
        public class GetUserBetItemDetail
        {

            [XmlElement(ElementName = "GameResult")]
            public GetUserBetItemGameResult GameResult { get; set; }

            [XmlElement(ElementName = "ResultAmount")]
            public decimal ResultAmount { get; set; }

            [XmlElement(ElementName = "Balance")]
            public decimal Balance { get; set; }
        }

        [XmlRoot(ElementName = "UserBetItem")]
        public class GetUserBetItemUserBetItem
        {

            [XmlElement(ElementName = "BetID")]
            public int BetID { get; set; }

            [XmlElement(ElementName = "BetTime")]
            public DateTime BetTime { get; set; }

            [XmlElement(ElementName = "PayoutTime")]
            public DateTime PayoutTime { get; set; }

            [XmlElement(ElementName = "GameID")]
            public int GameID { get; set; }

            [XmlElement(ElementName = "HostID")]
            public int HostID { get; set; }

            [XmlElement(ElementName = "HostName")]
            public string HostName { get; set; }

            [XmlElement(ElementName = "GameType")]
            public string GameType { get; set; }

            [XmlElement(ElementName = "Set")]
            public int Set { get; set; }

            [XmlElement(ElementName = "Round")]
            public int Round { get; set; }

            [XmlElement(ElementName = "BetType")]
            public int BetType { get; set; }

            [XmlElement(ElementName = "BetAmount")]
            public decimal BetAmount { get; set; }

            [XmlElement(ElementName = "Rolling")]
            public decimal Rolling { get; set; }

            [XmlElement(ElementName = "Detail")]
            public GetUserBetItemDetail Detail { get; set; }
        }

        [XmlRoot(ElementName = "UserBetItemList")]
        public class GetUserBetItemUserBetItemList
        {

            [XmlElement(ElementName = "UserBetItem")]
            public List<GetUserBetItemUserBetItem> UserBetItem { get; set; }
        }

        [XmlRoot(ElementName = "GetUserBetItemResponse")]
        public class GetUserBetItemDVResponse
        {

            [XmlElement(ElementName = "Username")]
            public string Username { get; set; }

            [XmlElement(ElementName = "FromTime")]
            public DateTime FromTime { get; set; }

            [XmlElement(ElementName = "ToTime")]
            public DateTime ToTime { get; set; }

            [XmlElement(ElementName = "Offset")]
            public int Offset { get; set; }

            [XmlElement(ElementName = "More")]
            public bool More { get; set; }

            [XmlElement(ElementName = "ItemCount")]
            public int ItemCount { get; set; }

            [XmlElement(ElementName = "UserBetItemList")]
            public GetUserBetItemUserBetItemList UserBetItemList { get; set; }

            [XmlElement(ElementName = "ErrorMsgId")]
            public int ErrorMsgId { get; set; }

            [XmlElement(ElementName = "ErrorMsg")]
            public string ErrorMsg { get; set; }

            [XmlAttribute(AttributeName = "xsd")]
            public string Xsd { get; set; }

            [XmlAttribute(AttributeName = "xsi")]
            public string Xsi { get; set; }

            [XmlText]
            public string Text { get; set; }
        }




        [XmlRoot(ElementName = "BetAmountDetails")]
        public class BetAmountDetails
        {

            [XmlElement(ElementName = "Username")]
            public string Username { get; set; }

            [XmlElement(ElementName = "StakeAmount")]
            public decimal StakeAmount { get; set; }
        }

        [XmlRoot(ElementName = "BetAmountDetailList")]
        public class BetAmountDetailList
        {

            [XmlElement(ElementName = "BetAmountDetails")]
            public BetAmountDetails BetAmountDetails { get; set; }
        }

        [XmlRoot(ElementName = "GetUserBetAmountResponse", Namespace = "")]
        public class GetUserBetAmountResponse
        {

            [XmlElement(ElementName = "BetAmountDetailList")]
            public BetAmountDetailList BetAmountDetailList { get; set; }

            [XmlElement(ElementName = "ErrorMsgId")]
            public int ErrorMsgId { get; set; }

            [XmlElement(ElementName = "ErrorMsg")]
            public string ErrorMsg { get; set; }

            [XmlAttribute(AttributeName = "xsd")]
            public string Xsd { get; set; }

            [XmlAttribute(AttributeName = "xsi")]
            public string Xsi { get; set; }

            [XmlText]
            public string Text { get; set; }
        }


        [XmlRoot(ElementName = "GetUserWinLostResponse", Namespace = "")]
        public class GetUserWinLostResponse
        {

            [XmlElement(ElementName = "ErrorMsgId")]
            public int ErrorMsgId { get; set; }

            [XmlElement(ElementName = "Winlost")]
            public decimal Winlost { get; set; }

            [XmlAttribute(AttributeName = "xsd")]
            public string Xsd { get; set; }

            [XmlAttribute(AttributeName = "xsi")]
            public string Xsi { get; set; }

            [XmlText]
            public double Text { get; set; }
        }


        [XmlRoot(ElementName = "TransactionDetails")]
        public class GetTransactionDetailsTransactionDetails
        {

            [XmlElement(ElementName = "CurrencyName")]
            public string CurrencyName { get; set; }

            [XmlElement(ElementName = "Count")]
            public decimal Count { get; set; }

            [XmlElement(ElementName = "Winlose")]
            public decimal Winlose { get; set; }

            [XmlElement(ElementName = "TotalBet")]
            public decimal TotalBet { get; set; }

            [XmlElement(ElementName = "TotalRolling")]
            public decimal TotalRolling { get; set; }
        }

        [XmlRoot(ElementName = "Details")]
        public class GetTransactionDetailsDetails
        {

            [XmlElement(ElementName = "TransactionDetails")]
            public List<GetTransactionDetailsTransactionDetails> TransactionDetails { get; set; }
        }

        [XmlRoot(ElementName = "GetTransactionDetailsResponse")]
        public class GetTransactionDetailsResponse
        {

            [XmlElement(ElementName = "ErrorMsgId")]
            public int ErrorMsgId { get; set; }

            [XmlElement(ElementName = "ErrorMsg")]
            public string ErrorMsg { get; set; }

            [XmlElement(ElementName = "Details")]
            public GetTransactionDetailsDetails Details { get; set; }

            [XmlAttribute(AttributeName = "xsd")]
            public string Xsd { get; set; }

            [XmlAttribute(AttributeName = "xsi")]
            public string Xsi { get; set; }

            [XmlText]
            public string Text { get; set; }
        }



        [XmlRoot(ElementName = "GameIcon")]
        public class GetActiveHostListGameIcon
        {

            [XmlElement(ElementName = "s208x147")]
            public string S208x147 { get; set; }

            [XmlElement(ElementName = "s279x400")]
            public string S279x400 { get; set; }

            [XmlElement(ElementName = "s300x200")]
            public string S300x200 { get; set; }

            [XmlElement(ElementName = "s512x512")]
            public string S512x512 { get; set; }

            [XmlElement(ElementName = "s660x380")]
            public string S660x380 { get; set; }

            [XmlElement(ElementName = "s760x539_1")]
            public string S760x5391 { get; set; }

            [XmlElement(ElementName = "s760x539_2")]
            public string S760x5392 { get; set; }
        }

        [XmlRoot(ElementName = "Host")]
        public class GetActiveHostListHost
        {

            [XmlElement(ElementName = "HostID")]
            public int HostID { get; set; }

            [XmlElement(ElementName = "GameType")]
            public string GameType { get; set; }

            [XmlElement(ElementName = "HostName")]
            public string HostName { get; set; }

            [XmlElement(ElementName = "Enabled")]
            public bool Enabled { get; set; }

            [XmlElement(ElementName = "GameStatus")]
            public int GameStatus { get; set; }

            [XmlElement(ElementName = "GameIcon")]
            public GetActiveHostListGameIcon GameIcon { get; set; }
        }

        [XmlRoot(ElementName = "HostList")]
        public class GetActiveHostListResponse
        {

            [XmlElement(ElementName = "Host")]
            public GetActiveHostListHost Host { get; set; }
        }




        [XmlRoot(ElementName = "CancelledBetDetail")]
        public class CancelledBetDetail
        {

            [XmlElement(ElementName = "StartTime")]
            public DateTime StartTime { get; set; }

            [XmlElement(ElementName = "CancelledTime")]
            public DateTime CancelledTime { get; set; }

            [XmlElement(ElementName = "GameID")]
            public double GameID { get; set; }

            [XmlElement(ElementName = "HostID")]
            public int HostID { get; set; }

            [XmlElement(ElementName = "GameType")]
            public string GameType { get; set; }

            [XmlElement(ElementName = "Round")]
            public int Round { get; set; }

            [XmlElement(ElementName = "Set")]
            public int Set { get; set; }

            [XmlElement(ElementName = "BetTime")]
            public DateTime BetTime { get; set; }

            [XmlElement(ElementName = "Username")]
            public string Username { get; set; }

            [XmlElement(ElementName = "TransactionID")]
            public int TransactionID { get; set; }

            [XmlElement(ElementName = "BetID")]
            public int BetID { get; set; }

            [XmlElement(ElementName = "Currency")]
            public string Currency { get; set; }

            [XmlElement(ElementName = "BetAmount")]
            public decimal BetAmount { get; set; }

            [XmlElement(ElementName = "BetType")]
            public int BetType { get; set; }

            [XmlElement(ElementName = "BetSource")]
            public int BetSource { get; set; }

            [XmlElement(ElementName = "BetConfirmation")]
            public int BetConfirmation { get; set; }
        }

        [XmlRoot(ElementName = "CancelledBetDetailList")]
        public class CancelledBetDetailList
        {

            [XmlElement(ElementName = "CancelledBetDetail")]
            public List<CancelledBetDetail> CancelledBetDetail { get; set; }
        }

        [XmlRoot(ElementName = "GetCancelledBetDetailsResponse")]
        public class GetCancelledBetDetailsResponse
        {

            [XmlElement(ElementName = "ErrorMsgId")]
            public int ErrorMsgId { get; set; }

            [XmlElement(ElementName = "ErrorMsg")]
            public string ErrorMsg { get; set; }

            [XmlElement(ElementName = "GameID")]
            public object GameID { get; set; }

            [XmlElement(ElementName = "Username")]
            public object Username { get; set; }

            [XmlElement(ElementName = "FromTime")]
            public DateTime FromTime { get; set; }

            [XmlElement(ElementName = "ToTime")]
            public DateTime ToTime { get; set; }

            [XmlElement(ElementName = "NumOfRecord")]
            public int NumOfRecord { get; set; }

            [XmlElement(ElementName = "CancelledBetDetailList")]
            public CancelledBetDetailList CancelledBetDetailList { get; set; }

            [XmlAttribute(AttributeName = "xsd")]
            public string Xsd { get; set; }

            [XmlAttribute(AttributeName = "xsi")]
            public string Xsi { get; set; }

            [XmlText]
            public string Text { get; set; }
        }



        [XmlRoot(ElementName = "xocdiaresult")]
        public class Xocdiaresult
        {

            [XmlElement(ElementName = "noofred")]
            public int Noofred { get; set; }
        }

        [XmlRoot(ElementName = "incorrectresult")]
        public class Incorrectresult
        {

            [XmlElement(ElementName = "xocdiaresult")]
            public Xocdiaresult Xocdiaresult { get; set; }
        }

        [XmlRoot(ElementName = "correctresult")]
        public class Correctresult
        {

            [XmlElement(ElementName = "xocdiaresult")]
            public Xocdiaresult Xocdiaresult { get; set; }
        }

        [XmlRoot(ElementName = "getransactiondetail")]
        public class Getransactiondetail
        {

            [XmlElement(ElementName = "bettime")]
            public DateTime Bettime { get; set; }

            [XmlElement(ElementName = "payouttime")]
            public DateTime Payouttime { get; set; }

            [XmlElement(ElementName = "adjustmenttime")]
            public DateTime Adjustmenttime { get; set; }

            [XmlElement(ElementName = "username")]
            public string Username { get; set; }

            [XmlElement(ElementName = "betid")]
            public int Betid { get; set; }

            [XmlElement(ElementName = "currency")]
            public string Currency { get; set; }

            [XmlElement(ElementName = "bettype")]
            public int Bettype { get; set; }

            [XmlElement(ElementName = "betamount")]
            public decimal Betamount { get; set; }

            [XmlElement(ElementName = "originalwinlose")]
            public decimal Originalwinlose { get; set; }

            [XmlElement(ElementName = "adjustedwinlose")]
            public decimal Adjustedwinlose { get; set; }
        }

        [XmlRoot(ElementName = "getransactiondetaillist")]
        public class Getransactiondetaillist
        {

            [XmlElement(ElementName = "getransactiondetail")]
            public Getransactiondetail Getransactiondetail { get; set; }
        }

        [XmlRoot(ElementName = "impactedtransactionlist")]
        public class Impactedtransactionlist
        {

            [XmlElement(ElementName = "count")]
            public int Count { get; set; }

            [XmlElement(ElementName = "getransactiondetaillist")]
            public Getransactiondetaillist Getransactiondetaillist { get; set; }
        }

        [XmlRoot(ElementName = "gameerrorresult")]
        public class Gameerrorresult
        {

            [XmlElement(ElementName = "gameid")]
            public double Gameid { get; set; }

            [XmlElement(ElementName = "set")]
            public int Set { get; set; }

            [XmlElement(ElementName = "round")]
            public int Round { get; set; }

            [XmlElement(ElementName = "payouttime")]
            public DateTime Payouttime { get; set; }

            [XmlElement(ElementName = "hostid")]
            public int Hostid { get; set; }

            [XmlElement(ElementName = "gametype")]
            public string Gametype { get; set; }

            [XmlElement(ElementName = "incorrectresult")]
            public Incorrectresult Incorrectresult { get; set; }

            [XmlElement(ElementName = "correctresult")]
            public Correctresult Correctresult { get; set; }

            [XmlElement(ElementName = "impactedtransactionlist")]
            public Impactedtransactionlist Impactedtransactionlist { get; set; }
        }

        [XmlRoot(ElementName = "gameerrorresultlist")]
        public class Gameerrorresultlist
        {

            [XmlElement(ElementName = "gameerrorresult")]
            public Gameerrorresult Gameerrorresult { get; set; }
        }

        [XmlRoot(ElementName = "hostlist")]
        public class Hostlist
        {

            [XmlElement(ElementName = "fromtime")]
            public DateTime Fromtime { get; set; }

            [XmlElement(ElementName = "totime")]
            public DateTime Totime { get; set; }

            [XmlElement(ElementName = "numofgame")]
            public int Numofgame { get; set; }

            [XmlElement(ElementName = "gameerrorresultlist")]
            public Gameerrorresultlist Gameerrorresultlist { get; set; }
        }

        [XmlRoot(ElementName = "gameid")]
        public class Gameid
        {

            [XmlElement(ElementName = "hostlist")]
            public Hostlist Hostlist { get; set; }
        }

        [XmlRoot(ElementName = "username")]
        public class Username
        {

            [XmlElement(ElementName = "gameid")]
            public Gameid Gameid { get; set; }
        }

        [XmlRoot(ElementName = "getgameerrordetailresponse")]
        public class Getgameerrordetailresponse
        {

            [XmlElement(ElementName = "errormsgid")]
            public int Errormsgid { get; set; }

            [XmlElement(ElementName = "errormsg")]
            public string Errormsg { get; set; }

            [XmlElement(ElementName = "username")]
            public Username Username { get; set; }

            [XmlAttribute(AttributeName = "xsd")]
            public string Xsd { get; set; }

            [XmlAttribute(AttributeName = "xsi")]
            public string Xsi { get; set; }

            [XmlText]
            public string Text { get; set; }
        }

       
        public class GetGameErrorDetailResponse
        {

            [XmlElement(ElementName = "getgameerrordetailresponse")]
            public Getgameerrordetailresponse Getgameerrordetailresponse { get; set; }

            [XmlAttribute(AttributeName = "Zmlns")]
            public string Zmlns { get; set; }

            [XmlText]
            public string Text { get; set; }
        }


        [XmlRoot(ElementName = "UnsuccessfulBetDetail")]
        public class UnsuccessfulBetDetail
        {

            [XmlElement(ElementName = "BetTime")]
            public DateTime BetTime { get; set; }

            [XmlElement(ElementName = "UTID")]
            public int UTID { get; set; }

            [XmlElement(ElementName = "GameID")]
            public double GameID { get; set; }

            [XmlElement(ElementName = "HostID")]
            public int HostID { get; set; }

            [XmlElement(ElementName = "GameType")]
            public string GameType { get; set; }

            [XmlElement(ElementName = "Round")]
            public int Round { get; set; }

            [XmlElement(ElementName = "Set")]
            public int Set { get; set; }

            [XmlElement(ElementName = "Username")]
            public string Username { get; set; }

            [XmlElement(ElementName = "TransactionID")]
            public int TransactionID { get; set; }

            [XmlElement(ElementName = "BetID")]
            public object BetID { get; set; }

            [XmlElement(ElementName = "Currency")]
            public string Currency { get; set; }

            [XmlElement(ElementName = "BetAmount")]
            public decimal BetAmount { get; set; }

            [XmlElement(ElementName = "BetType")]
            public int BetType { get; set; }

            [XmlElement(ElementName = "BetSource")]
            public int BetSource { get; set; }

            [XmlElement(ElementName = "UnsuccessfulReason")]
            public int UnsuccessfulReason { get; set; }

            [XmlElement(ElementName = "BetConfirmation")]
            public int BetConfirmation { get; set; }
        }

        [XmlRoot(ElementName = "UnsuccessfulBetDetailList")]
        public class UnsuccessfulBetDetailList
        {

            [XmlElement(ElementName = "UnsuccessfulBetDetail")]
            public List<UnsuccessfulBetDetail> UnsuccessfulBetDetail { get; set; }
        }

        [XmlRoot(ElementName = "GetUnsuccessfulBetDetailsResponse")]
        public class GetUnsuccessfulBetDetailsResponse
        {

            [XmlElement(ElementName = "ErrorMsgId")]
            public int ErrorMsgId { get; set; }

            [XmlElement(ElementName = "ErrorMsg")]
            public string ErrorMsg { get; set; }

            [XmlElement(ElementName = "GameID")]
            public double GameID { get; set; }

            [XmlElement(ElementName = "Username")]
            public string Username { get; set; }

            [XmlElement(ElementName = "FromTime")]
            public DateTime FromTime { get; set; }

            [XmlElement(ElementName = "ToTime")]
            public DateTime ToTime { get; set; }

            [XmlElement(ElementName = "NumOfRecord")]
            public int NumOfRecord { get; set; }

            [XmlElement(ElementName = "NumOfPage")]
            public int NumOfPage { get; set; }

            [XmlElement(ElementName = "PageNum")]
            public int PageNum { get; set; }

            [XmlElement(ElementName = "UnsuccessfulBetDetailList")]
            public UnsuccessfulBetDetailList UnsuccessfulBetDetailList { get; set; }

            [XmlAttribute(AttributeName = "xsd")]
            public string Xsd { get; set; }

            [XmlAttribute(AttributeName = "xsi")]
            public string Xsi { get; set; }

            [XmlText]
            public string Text { get; set; }
        }




        [XmlRoot(ElementName = "DebitBalanceResponse")]
        public class DebitBalanceDVResponse
        {

            [XmlElement(ElementName = "ErrorMsgId")]
            public int ErrorMsgId { get; set; }

            [XmlElement(ElementName = "ErrorMsg")]
            public string ErrorMsg { get; set; }

            [XmlElement(ElementName = "Username")]
            public string Username { get; set; }

            [XmlElement(ElementName = "Balance")]
            public decimal Balance { get; set; }

            [XmlElement(ElementName = "DebitAmount")]
            public decimal DebitAmount { get; set; }

            [XmlElement(ElementName = "OrderId")]
            public string OrderId { get; set; }

            [XmlAttribute(AttributeName = "xsd")]
            public string Xsd { get; set; }

            [XmlAttribute(AttributeName = "xsi")]
            public string Xsi { get; set; }

            [XmlText]
            public string Text { get; set; }
        }



        [XmlRoot(ElementName = "DebitAllBalanceResponse")]
        public class DebitAllBalanceDVResponse
        {

            [XmlElement(ElementName = "ErrorMsgId")]
            public int ErrorMsgId { get; set; }

            [XmlElement(ElementName = "ErrorMsg")]
            public string ErrorMsg { get; set; }

            [XmlElement(ElementName = "Username")]
            public string Username { get; set; }

            [XmlElement(ElementName = "DebitAmount")]
            public int DebitAmount { get; set; }

            [XmlElement(ElementName = "OrderId")]
            public string OrderId { get; set; }

            [XmlAttribute(AttributeName = "xsd")]
            public string Xsd { get; set; }

            [XmlAttribute(AttributeName = "xsi")]
            public string Xsi { get; set; }

            [XmlText]
            public string Text { get; set; }
        }






        [XmlRoot(ElementName = "CreditBalanceResponse")]
        public class CreditBalanceDVResponse
        {

            [XmlElement(ElementName = "ErrorMsgId")]
            public int ErrorMsgId { get; set; }

            [XmlElement(ElementName = "ErrorMsg")]
            public string ErrorMsg { get; set; }

            [XmlElement(ElementName = "Username")]
            public string Username { get; set; }

            [XmlElement(ElementName = "Balance")]
            public decimal Balance { get; set; }

            [XmlElement(ElementName = "CreditAmount")]
            public decimal CreditAmount { get; set; }

            [XmlElement(ElementName = "OrderId")]
            public string OrderId { get; set; }

            [XmlAttribute(AttributeName = "xsd")]
            public string Xsd { get; set; }

            [XmlAttribute(AttributeName = "xsi")]
            public string Xsi { get; set; }

            [XmlText]
            public string Text { get; set; }
        }





        [XmlRoot(ElementName = "CheckOrderIdResponse")]
        public class CheckOrderIdResponse
        {

            [XmlElement(ElementName = "ErrorMsgId")]
            public int ErrorMsgId { get; set; }

            [XmlElement(ElementName = "ErrorMsg")]
            public string ErrorMsg { get; set; }

            [XmlElement(ElementName = "isExist")]
            public bool IsExist { get; set; }

            [XmlAttribute(AttributeName = "xsd")]
            public string Xsd { get; set; }

            [XmlAttribute(AttributeName = "xsi")]
            public string Xsi { get; set; }

            [XmlText]
            public string Text { get; set; }
        }




        [XmlRoot(ElementName = "CheckOrderDetailsResponse")]
        public class CheckOrderDetailsDVResponse
        {

            [XmlElement(ElementName = "ErrorMsgId")]
            public int ErrorMsgId { get; set; }

            [XmlElement(ElementName = "ErrorMsg")]
            public string ErrorMsg { get; set; }

            [XmlElement(ElementName = "isExist")]
            public bool IsExist { get; set; }

            [XmlElement(ElementName = "Date")]
            public DateTime Date { get; set; }

            [XmlElement(ElementName = "Type")]
            public int Type { get; set; }

            [XmlElement(ElementName = "Currency")]
            public string Currency { get; set; }

            [XmlElement(ElementName = "Amount")]
            public decimal Amount { get; set; }

            [XmlElement(ElementName = "PreviousBalance")]
            public decimal PreviousBalance { get; set; }

            [XmlElement(ElementName = "Balance")]
            public decimal Balance { get; set; }

            [XmlAttribute(AttributeName = "xsd")]
            public string Xsd { get; set; }

            [XmlAttribute(AttributeName = "xsi")]
            public string Xsi { get; set; }

            [XmlText]
            public string Text { get; set; }
        }



    }

    public class Test
    {
        public static async Task Main(string[] args)
        {
            SAGAMINGAPI api = new SAGAMINGAPI(
               "https://sai-api.sa-apisvr.com/api/api.aspx",
               "https://sai-api.sa-rpt.com/api/api.aspx",
               "https://www.sai.slgaming.net/app.aspx",
               "A12219",
               "8AF343907DD94D9D9DC779D56DB83FC4",
               "GgaIMaiNNtg",
               "g9G16nTs" 
           );

            Console.WriteLine("Starting API Tests...");


            // Call API methods below: 
            //if (await api.CreateUser("mitjenny123", "MYR"))
            //{
            //    if (api.RegUserInfoRes.ErrorMsgId == 0)
            //    {
            //        Console.WriteLine("CreateUser successful!");
            //    }
            //    else
            //    {
            //        Console.WriteLine($"CreateUser failed: {api.RegUserInfoRes.ErrorMsg}");
            //    }

            //}
            //else
            //{
            //    Console.WriteLine($"CreateUser failed: {api.ErrorMessage}");
            //}


            //if (await api.VerifyUsername("mitjenny123"))
            //{
            //    if (api.VerifyUsernameRes.ErrorMsgId == 0)
            //    {
            //        Console.WriteLine("VerifyUsername successful!");
            //    }
            //    else
            //    {
            //        Console.WriteLine($"VerifyUsername failed: {api.VerifyUsernameRes.ErrorMsg}");
            //    }

            //}
            //else
            //{
            //    Console.WriteLine($"VerifyUsername failed: {api.ErrorMessage}");
            //}


            //if (await api.GetUserStatusDV("mitjenny123"))
            //{
            //    if (api.GetUserStatusDVRes.ErrorMsgId == 0)
            //    {
            //        Console.WriteLine("GetUserStatus successful!");
            //    }
            //    else
            //    {
            //        Console.WriteLine($"GetUserStatus failed: {api.GetUserStatusDVRes.ErrorMsg}");
            //    }

            //}
            //else
            //{
            //    Console.WriteLine($"GetUserStatus failed: {api.ErrorMessage}");
            //}

            //if (await api.SetUserMaxBalance("mitjenny123",9999))
            //{
            //    if (api.GetUserStatusRes.ErrorMsgId == 0)
            //    {
            //        Console.WriteLine("SetUserMaxBalance successful!");
            //    }
            //    else
            //    {
            //        Console.WriteLine($"SetUserMaxBalance failed: {api.SetUserMaxBalanceRes.ErrorMsg}");
            //    }

            //}
            //else
            //{
            //    Console.WriteLine($"SetUserMaxBalance failed: {api.ErrorMessage}");
            //}

            //if (await api.SetUserMaxWinning("mitjenny123", 9999))
            //{
            //    if (api.SetUserMaxWinningRes.ErrorMsgId == 0)
            //    {
            //        Console.WriteLine("SetUserMaxWinning successful!");
            //    }
            //    else
            //    {
            //        Console.WriteLine($"SetUserMaxWinning failed: {api.SetUserMaxWinningRes.ErrorMsg}");
            //    }

            //}
            //else
            //{
            //    Console.WriteLine($"SetUserMaxWinning failed: {api.ErrorMessage}");
            //}


            //if (await api.GetUnfinishedGame("mitjenny123"))
            //{
            //    if (api.GetUnfinishedGameRes.ErrorMsgId == 0)
            //    {
            //        Console.WriteLine("GetUnfinishedGame successful!");
            //    }
            //    else
            //    {
            //        Console.WriteLine($"GetUnfinishedGame failed: {api.GetUnfinishedGameRes.ErrorMsg}");
            //    }

            //}
            //else
            //{
            //    Console.WriteLine($"GetUnfinishedGame failed: {api.ErrorMessage}");
            //}


            //if (await api.LoginRequest("mitjenny123", "MYR"))
            //{
            //    if (api.LoginRequestRes.ErrorMsgId == 0)
            //    {

            //        string gamelink = api.GetGameLink("mitjenny123", api.LoginRequestRes.Token, "A12219");

            //        Console.WriteLine("LoginRequest successful!");
            //    }
            //    else
            //    {
            //        Console.WriteLine($"LoginRequest failed: {api.LoginRequestRes.ErrorMsg}");
            //    }

            //}
            //else
            //{
            //    Console.WriteLine($"LoginRequest failed: {api.ErrorMessage}");
            //}






            //if (await api.LoginRequestForFun("mitjenny123", "MYR"))
            //{
            //    if (api.LoginRequestForFunRes.ErrorMsgId == 0)
            //    {
            //        Console.WriteLine("LoginRequestForFun successful!");
            //    }
            //    else
            //    {
            //        Console.WriteLine($"LoginRequestForFun failed: {api.LoginRequestForFunRes.ErrorMsg}");
            //    }

            //}
            //else
            //{
            //    Console.WriteLine($"LoginRequestForFun failed: {api.ErrorMessage}");
            //}



            //if (await api.KickUser("mitjenny123"))
            //{
            //    if (api.KickUserRes.ErrorMsgId == 0)
            //    {
            //        Console.WriteLine("KickUser successful!");
            //    }
            //    else
            //    {
            //        Console.WriteLine($"KickUser failed: {api.KickUserRes.ErrorMsg}");
            //    }

            //}
            //else
            //{
            //    Console.WriteLine($"KickUser failed: {api.ErrorMessage}");
            //}


            //if (await api.GetAllBetDetailsDV(Date: "2025-04-07"))
            //{
            //    if (api.GetAllBetDetailsDVRes.ErrorMsgId == 0)
            //    {
            //        Console.WriteLine("GetAllBetDetails successful!");
            //    }
            //    else
            //    {
            //        Console.WriteLine($"GetAllBetDetails failed: {api.GetAllBetDetailsDVRes.ErrorMsg}");
            //    }

            //}
            //else
            //{
            //    Console.WriteLine($"GetAllBetDetails failed: {api.ErrorMessage}");
            //}


            //if (await api.GetAllBetDetailsForTimeInterval("2025-03-19 00:00:00", "2025-03-19 23:59:59"))
            //{
            //    if (api.GetAllBetDetailsForTimeIntervalRes.ErrorMsgId == 0)
            //    {
            //        Console.WriteLine("GetAllBetDetailsForTimeInterval successful!");
            //    }
            //    else
            //    {
            //        Console.WriteLine($"GetAllBetDetailsForTimeInterval failed: {api.GetAllBetDetailsForTimeIntervalRes.ErrorMsg}");
            //    }

            //}
            //else
            //{
            //    Console.WriteLine($"GetAllBetDetailsForTimeInterval failed: {api.ErrorMessage}");
            //}


            //if (await api.GetUserBetAmount())
            //{
            //    if (api.GetUserBetAmountRes.ErrorMsgId == 0)
            //    {
            //        Console.WriteLine("GetUserBetAmount successful!");
            //    }
            //    else
            //    {
            //        Console.WriteLine($"GetUserBetAmount failed: {api.GetUserBetAmountRes.ErrorMsg}");
            //    }

            //}
            //else
            //{
            //    Console.WriteLine($"GetUserBetAmount failed: {api.ErrorMessage}");
            //}

            //string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            //Console.WriteLine(timestamp);

            //if (await api.CreditBalanceDV("mitjenny123", "IN" + timestamp + "mitjenny123", 100, "MYR"))
            //{
            //    if (api.CreditBalanceDVRes.ErrorMsgId == 0)
            //    {
            //        Console.WriteLine("CreditBalance successful!");
            //    }
            //    else
            //    {
            //        Console.WriteLine($"CreditBalance failed: {api.CreditBalanceDVRes.ErrorMsg}");
            //    }

            //}
            //else
            //{
            //    Console.WriteLine($"CreditBalance failed: {api.ErrorMessage}");
            //}

            //string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            //Console.WriteLine(timestamp);

            //if (await api.DebitBalanceDV("mitjenny123", "OUT" + timestamp + "mitjenny123", 1.50m))
            //{
            //    if (api.DebitBalanceDVRes.ErrorMsgId == 0)
            //    {
            //        Console.WriteLine("DebitBalance successful!");
            //    }
            //    else
            //    {
            //        Console.WriteLine($"DebitBalance failed: {api.DebitBalanceDVRes.ErrorMsg}");
            //    }

            //}
            //else
            //{
            //    Console.WriteLine($"DebitBalance failed: {api.ErrorMessage}");
            //}

            Console.WriteLine("API Tests Completed.");
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}
