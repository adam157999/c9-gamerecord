using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace GameRecord.Gamess.SimplePlay
{
    public class SimplePlay : IDisposable
    {
        #region Declaration
        public string BaseUrl { get; set; }
        public string LobbyCode { get; set; }
        public string SecretKey { get; set; }
        public string MD5Key { get; set; }
        public string EncryptKey { get; set; }


        public string finalUrl { get; set; }
        public string Request { get; set; }
        public string Parameters { get; set; }
        public string Response { get; set; }
        public string ErrorMessage { get; set; }

        public SimplePlay(string BaseUrl, string LobbyCode, string SecretKey, string MD5Key, string EncryptKey)
        {
            this.BaseUrl = BaseUrl;
            this.LobbyCode = LobbyCode;
            this.SecretKey = SecretKey;
            this.MD5Key = MD5Key;
            this.EncryptKey = EncryptKey;
        }
        #endregion

        #region Private Function
        public static string GetCurrentUtcTime()
        {
            return DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        }

        public string DESEncrypt(string inString)
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

        public string BuildMD5(string inString)
        {
            byte[] hashed = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(inString));
            StringBuilder sb = new StringBuilder(hashed.Length * 2);
            for (int i = 0; i < hashed.Length; i++)
            {
                sb.Append(hashed[i].ToString("x2"));
            }
            return sb.ToString();
        }
        #endregion

        #region Api function
        public RegUserInfoResponse RegUserInfoRes { get; set; }

        public VerifyUsernameResponse VerifyUsernameRes { get; set; }

        public GetUserStatusResponse GetUserStatusRes { get; set; }

        public SetUserMaxBalanceResponse SetUserMaxBalanceRes { get; set; }

        public SetUserMaxWinningResponse SetUserMaxWinningRes { get; set; }

        public GetUnfinishedGameResponse GetUnfinishedGameRes { get; set; }

        public LoginRequestResponse LoginRequestRes { get; set; }
        public LoginRequestForFunResponse LoginRequestForFunRes { get; set; }

        public KickUserResponse KickUserRes { get; set; }

        public GetAllBetDetailsResponse GetAllBetDetailsRes { get; set; }

        public GetAllBetDetailsResponse GetAllBetDetailsForTimeIntervalRes { get; set; }

        public GetUserBetAmountResponse GetUserBetAmountRes { get; set; }

        public GetUserWinLostResponse GetUserWinLostRes { get; set; }

        public GetSlotDetailsURLResponse GetSlotDetailsURLRes { get; set; }
        public GetFishermenGoldBetDetailsResponse GetFishermenGoldBetDetailsRes { get; set; }
        public GetFishingGameDetailsResponse GetFishingGameDetailsRes { get; set; }

        public GetFishingDetailsURLResponse GetFishingDetailsURLRes { get; set; }

        public GetSlotDetailsURLResponse2 GetSlotDetailsURLRes2 { get; set; }

        public GetCancelledBetDetailsResponse GetCancelledBetDetailsRes { get; set; }

        public JackpotQueryResponse JackpotQueryRes { get; set; }

        public GetActiveGameListResponse GetActiveGameListRes { get; set; }

        public MonthlyGameRankingQueryResponse MonthlyGameRankingQueryRes { get; set; }

        public DebitBalanceResponse DebitBalanceRes { get; set; }
        public DebitAllBalanceResponse DebitAllBalanceRes { get; set; }

        public CreditBalanceResponse CreditBalanceRes { get; set; }
        public CheckOrderIdResponse CheckOrderIdRes { get; set; }

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

        public async Task<bool> CreateUser(string UserName, string CurrencyType)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=RegUserInfo" +
                            "&Key=" + SecretKey +
                            "&Time=" + currTime +
                            "&Username=" + UserName +
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

        public async Task<bool> GetUserStatus(string UserName)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=GetUserStatus" +
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
                    GetUserStatusRes = DeserializeFromXml<GetUserStatusResponse>(responseContent);
                    
                    if (GetUserStatusRes.ErrorMsgId == 0)
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

        public async Task<bool> SetUserMaxBalance(string UserName, decimal MaxBalance)
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
                    SetUserMaxBalanceRes = DeserializeFromXml<SetUserMaxBalanceResponse>(responseContent);
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
                    SetUserMaxWinningRes = DeserializeFromXml<SetUserMaxWinningResponse>(responseContent);
                    
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

        public async Task<bool> GetUnfinishedGame(string UserName)
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

        public async Task<bool> LoginRequest(string Username, string CurrencyType, string GameCode = null, int? HostID = null, string Lang = null, bool? Mobile = null)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=LoginRequest" +
                            "&Key=" + SecretKey +
                            "&Time=" + currTime +
                            "&Username=" + Username;

                if (!string.IsNullOrEmpty(GameCode)) QS += "&GameCode=" + GameCode;
                if (HostID.HasValue) QS += "&HostID=" + HostID.Value;
                if (!string.IsNullOrEmpty(Lang)) QS += "&Lang=" + Lang;
                if (Mobile.HasValue) QS += "&Mobile=" + (Mobile.Value ? "1" : "0");


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
                    LoginRequestRes = DeserializeFromXml<LoginRequestResponse>(responseContent);
                  

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

        public async Task<bool> LoginRequestForFun(string Username, string CurrencyType, string GameCode = null, int? HostID = null, string Lang = null, bool? Mobile = null)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=LoginRequestForFun" +
                            "&Key=" + SecretKey +
                            "&Time=" + currTime +
                            "&Username=" + Username;

                if (!string.IsNullOrEmpty(GameCode)) QS += "&GameCode=" + GameCode;
                if (HostID.HasValue) QS += "&HostID=" + HostID.Value;
                if (!string.IsNullOrEmpty(Lang)) QS += "&Lang=" + Lang;
                if (Mobile.HasValue) QS += "&Mobile=" + (Mobile.Value ? "1" : "0");


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
                    LoginRequestForFunRes = DeserializeFromXml<LoginRequestForFunResponse>(responseContent);
                     
                    if (LoginRequestForFunRes.ErrorMsgId == 0)
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

        public async Task<bool> GetAllBetDetails(string Username = null, string Date = null)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=GetAllBetDetails" +
                            "&Key=" + SecretKey +
                            "&Time=" + currTime;

                if (!string.IsNullOrEmpty(Username)) QS += "&Username=" + Date;
                if (!string.IsNullOrEmpty(Date)) QS += "&Date=" + Date;

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
                    GetAllBetDetailsRes = DeserializeFromXml<GetAllBetDetailsResponse>(responseContent);
                  
                    if (GetAllBetDetailsRes.ErrorMsgId == 0)
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

        public async Task<bool> GetAllBetDetailsForTimeInterval(string FromTime, string ToTime, string Username = null)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=GetAllBetDetails" +
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
                    GetAllBetDetailsForTimeIntervalRes = DeserializeFromXml<GetAllBetDetailsResponse>(responseContent);
                    
                    if (GetAllBetDetailsForTimeIntervalRes.ErrorMsgId == 0)
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

        public async Task<bool> GetUserBetAmount(string Username = null, string StartDate = null, string TimeRange = null)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=GetUserBetAmount" +
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
                    GetUserBetAmountRes = DeserializeFromXml<GetUserBetAmountResponse>(responseContent);
                   
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
                string QS = "method=GetUserBetAmount" +
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

        public async Task<bool> GetSlotDetailsURL(string BetID = null, string GameID = null, string Lang = null)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=GetSlotDetailsURL" +
                            "&Key=" + SecretKey +
                            "&Time=" + currTime;

                if (!string.IsNullOrEmpty(BetID)) QS += "&BetID=" + BetID;
                if (!string.IsNullOrEmpty(GameID)) QS += "&GameID=" + GameID;
                if (!string.IsNullOrEmpty(Lang)) QS += "&Lang=" + Lang;


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
                    GetSlotDetailsURLRes = DeserializeFromXml<GetSlotDetailsURLResponse>(responseContent);
                    
                    if (GetSlotDetailsURLRes.ErrorMsgId == 0)
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

        public async Task<bool> GetFishermenGoldBetDetails(string GameId, string Page)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=GetFishermenGoldBetDetails" +
                            "&Key=" + SecretKey +
                            "&Time=" + currTime +
                            "&GameId=" + SecretKey +
                            "&Page=" + SecretKey;



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
                    GetFishermenGoldBetDetailsRes = DeserializeFromXml<GetFishermenGoldBetDetailsResponse>(responseContent);
                   
                    if (GetFishermenGoldBetDetailsRes.ErrorMsgId == 0)
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

        public async Task<bool> GetFishingGameDetails(string GameId, string Page)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=GetFishingGameDetails" +
                            "&Key=" + SecretKey +
                            "&Time=" + currTime +
                            "&GameId=" + SecretKey +
                            "&Page=" + SecretKey;



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
                    GetFishingGameDetailsRes = DeserializeFromXml<GetFishingGameDetailsResponse>(responseContent);
                   

                    if (GetFishingGameDetailsRes.ErrorMsgId == 0)
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

        public async Task<bool> GetFishingDetailsURL(string BetID, string Lang = null)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=GetFishingDetailsURL" +
                            "&Key=" + SecretKey +
                            "&Time=" + currTime +
                            "&BetID=" + BetID;

                if (!string.IsNullOrEmpty(Lang)) QS += "&Lang=" + Lang;

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
                    GetFishingDetailsURLRes = DeserializeFromXml<GetFishingDetailsURLResponse>(responseContent);
                    
                    if (GetFishingDetailsURLRes.ErrorMsgId == 0)
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

        public async Task<bool> GetGameDetailsURL(string BetID = null, string GameID = null, string Lang = null)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=GetGameDetailsURL" +
                            "&Key=" + SecretKey +
                            "&Time=" + currTime +
                            "&BetID=" + BetID;

                if (!string.IsNullOrEmpty(BetID)) QS += "&BetID=" + BetID;
                if (!string.IsNullOrEmpty(GameID)) QS += "&GameID=" + GameID;
                if (!string.IsNullOrEmpty(Lang)) QS += "&Lang=" + Lang;

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
                    GetSlotDetailsURLRes2 = DeserializeFromXml<GetSlotDetailsURLResponse2>(responseContent);
                   
                    if (GetSlotDetailsURLRes2.ErrorMsgId == 0)
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

        public async Task<bool> GetCancelledBetDetails(string Username = null, string GameID = null, string FromTime = null, string ToTime = null)
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

        public async Task<bool> JackpotQuery(string Currency)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=GetCancelledBetDetails" +
                            "&Key=" + SecretKey +
                            "&Time=" + currTime +
                             "&Currency=" + Currency;


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
                    JackpotQueryRes = DeserializeFromXml<JackpotQueryResponse>(responseContent);
                   
                    if (JackpotQueryRes.ErrorMsgId == 0)
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

        public async Task<bool> GetActiveGameList()
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=GetCancelledBetDetails" +
                            "&Key=" + SecretKey +
                            "&Time=" + currTime;


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
                    GetActiveGameListRes = DeserializeFromXml<GetActiveGameListResponse>(responseContent);
                   
                    if (GetActiveGameListRes.ErrorMsgId == 0)
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

        public async Task<bool> MonthlyGameRankingQuery(string RankingMonth = null)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=MonthlyGameRankingQuery" +
                            "&Key=" + SecretKey +
                            "&Time=" + currTime +
                            "&GameCategory=SlotGame";

                if (!string.IsNullOrEmpty(RankingMonth)) QS += "&RankingMonth=" + RankingMonth;

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
                    MonthlyGameRankingQueryRes = DeserializeFromXml<MonthlyGameRankingQueryResponse>(responseContent);
                   
                    if (MonthlyGameRankingQueryRes.ErrorMsgId == 0)
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

        public async Task<bool> DebitBalance(string Username, string OrderId, decimal DebitAmount)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=DebitBalance" +
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
                    DebitBalanceRes = DeserializeFromXml<DebitBalanceResponse>(responseContent);
                    
                    if (DebitBalanceRes.ErrorMsgId == 0)
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

        public async Task<bool> DebitAllBalance(string Username, string OrderId)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=DebitAllBalance" +
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
                    DebitAllBalanceRes = DeserializeFromXml<DebitAllBalanceResponse>(responseContent);
                    
                    if (DebitAllBalanceRes.ErrorMsgId == 0)
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

        public async Task<bool> CreditBalance(string Username, string OrderId, decimal CreditAmount, string CurrencyType)
        {
            try
            {
                string currTime = GetCurrentUtcTime();
                string QS = "method=CreditBalance" +
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
                    CreditBalanceRes = DeserializeFromXml<CreditBalanceResponse>(responseContent);
                    
                    if (CreditBalanceRes.ErrorMsgId == 0)
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

        public async Task<bool> CheckOrderId(string OrderId)
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
                    CheckOrderIdRes = DeserializeFromXml<CheckOrderIdResponse>(responseContent);
                  
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
        #endregion

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        #region Models
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



        [XmlRoot(ElementName = "GetUserStatusResponse", Namespace = "")]
        public class GetUserStatusResponse
        {

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

            [XmlElement(ElementName = "BetAmount")]
            public decimal BetAmount { get; set; }

            [XmlElement(ElementName = "MaxBalance")]
            public decimal MaxBalance { get; set; }

            [XmlElement(ElementName = "MaxWinning")]
            public decimal MaxWinning { get; set; }

            [XmlElement(ElementName = "WithholdAmount")]
            public decimal WithholdAmount { get; set; }

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

        [XmlRoot(ElementName = "LoginRequestResponse", Namespace = "")]
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

            [XmlElement(ElementName = "GameURL")]
            public string GameURL { get; set; }

            [XmlAttribute(AttributeName = "xsd")]
            public string Xsd { get; set; }

            [XmlAttribute(AttributeName = "xsi")]
            public string Xsi { get; set; }

            [XmlText]
            public string Text { get; set; }
        }


        [XmlRoot(ElementName = "LoginRequestForFunResponse", Namespace = "")]
        public class LoginRequestForFunResponse
        {

            [XmlElement(ElementName = "ErrorMsgId")]
            public int ErrorMsgId { get; set; }

            [XmlElement(ElementName = "ErrorMsg")]
            public string ErrorMsg { get; set; }

            [XmlElement(ElementName = "Token")]
            public string Token { get; set; }

            [XmlElement(ElementName = "DisplayName")]
            public object DisplayName { get; set; }

            [XmlElement(ElementName = "GameURL")]
            public string GameURL { get; set; }

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
        public class GetAllBetDetailsResponse
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




        [XmlRoot(ElementName = "BetAmountDetails")]
        public class BetAmountDetails
        {

            [XmlElement(ElementName = "Username")]
            public string Username { get; set; }

            [XmlElement(ElementName = "StakeAmount")]
            public double StakeAmount { get; set; }
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
            public double Winlost { get; set; }

            [XmlAttribute(AttributeName = "xsd")]
            public string Xsd { get; set; }

            [XmlAttribute(AttributeName = "xsi")]
            public string Xsi { get; set; }

            [XmlText]
            public double Text { get; set; }
        }


        [XmlRoot(ElementName = "GetSlotDetailsURLResponse", Namespace = "")]
        public class GetSlotDetailsURLResponse
        {

            [XmlElement(ElementName = "ErrorMsgId")]
            public int ErrorMsgId { get; set; }

            [XmlElement(ElementName = "ErrorMsg")]
            public string ErrorMsg { get; set; }

            [XmlElement(ElementName = "Url")]
            public string Url { get; set; }

            [XmlAttribute(AttributeName = "xsd")]
            public string Xsd { get; set; }

            [XmlAttribute(AttributeName = "xsi")]
            public string Xsi { get; set; }

            [XmlText]
            public string Text { get; set; }
        }



        [XmlRoot(ElementName = "BulletDetails")]
        public class BulletDetails
        {

            [XmlElement(ElementName = "Denom")]
            public int Denom { get; set; }

            [XmlElement(ElementName = "ID")]
            public int ID { get; set; }

            [XmlElement(ElementName = "Multiplier")]
            public int Multiplier { get; set; }

            [XmlElement(ElementName = "Cost")]
            public int Cost { get; set; }

            [XmlElement(ElementName = "Reward")]
            public int Reward { get; set; }

            [XmlElement(ElementName = "Species")]
            public int Species { get; set; }

            [XmlElement(ElementName = "FreeBulletTotal")]
            public int FreeBulletTotal { get; set; }

            [XmlElement(ElementName = "FreeBulletRemain")]
            public int FreeBulletRemain { get; set; }

            [XmlElement(ElementName = "FreeBulletReward")]
            public string FreeBulletReward { get; set; }
        }

        [XmlRoot(ElementName = "BulletDetailsList")]
        public class BulletDetailsList
        {

            [XmlElement(ElementName = "BulletDetails")]
            public BulletDetails BulletDetails { get; set; }
        }

        [XmlRoot(ElementName = "GetFishermenGoldBetDetailsResponse", Namespace = "")]
        public class GetFishermenGoldBetDetailsResponse
        {

            [XmlElement(ElementName = "BetAmount")]
            public decimal BetAmount { get; set; }

            [XmlElement(ElementName = "WinAmount")]
            public decimal WinAmount { get; set; }

            [XmlElement(ElementName = "Currency")]
            public string Currency { get; set; }

            [XmlElement(ElementName = "ExchangeRate")]
            public int ExchangeRate { get; set; }

            [XmlElement(ElementName = "Count")]
            public int Count { get; set; }

            [XmlElement(ElementName = "ItemCount")]
            public int ItemCount { get; set; }

            [XmlElement(ElementName = "Limit")]
            public int Limit { get; set; }

            [XmlElement(ElementName = "BulletDetailsList")]
            public BulletDetailsList BulletDetailsList { get; set; }

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

        [XmlRoot(ElementName = "BulletDetails")]
        public class BulletDetails2
        {

            [XmlElement(ElementName = "Denom")]
            public double Denom { get; set; }

            [XmlElement(ElementName = "ID")]
            public int ID { get; set; }

            [XmlElement(ElementName = "Multiplier")]
            public int Multiplier { get; set; }

            [XmlElement(ElementName = "Cost")]
            public int Cost { get; set; }

            [XmlElement(ElementName = "Reward")]
            public DateTime Reward { get; set; }

            [XmlElement(ElementName = "Species")]
            public int Species { get; set; }

            [XmlElement(ElementName = "FreeBulletTotal")]
            public int FreeBulletTotal { get; set; }

            [XmlElement(ElementName = "FreeBulletRemain")]
            public int FreeBulletRemain { get; set; }

            [XmlElement(ElementName = "CreatedAt")]
            public DateTime CreatedAt { get; set; }

            [XmlElement(ElementName = "FreeBulletReward")]
            public int FreeBulletReward { get; set; }

            [XmlElement(ElementName = "FreeBulletMultiplier")]
            public int FreeBulletMultiplier { get; set; }
        }

        [XmlRoot(ElementName = "BulletDetailsList")]
        public class BulletDetailsList2
        {

            [XmlElement(ElementName = "BulletDetails")]
            public BulletDetails2 BulletDetails { get; set; }
        }

        [XmlRoot(ElementName = "GetFishingGameDetailsResponse", Namespace = "")]
        public class GetFishingGameDetailsResponse
        {

            [XmlElement(ElementName = "ErrorMsgId")]
            public int ErrorMsgId { get; set; }

            [XmlElement(ElementName = "ErrorMsg")]
            public string ErrorMsg { get; set; }

            [XmlElement(ElementName = "BetAmount")]
            public decimal BetAmount { get; set; }

            [XmlElement(ElementName = "WinAmount")]
            public DateTime WinAmount { get; set; }

            [XmlElement(ElementName = "Currency")]
            public string Currency { get; set; }

            [XmlElement(ElementName = "ExchangeRate")]
            public int ExchangeRate { get; set; }

            [XmlElement(ElementName = "Count")]
            public int Count { get; set; }

            [XmlElement(ElementName = "ItemCount")]
            public int ItemCount { get; set; }

            [XmlElement(ElementName = "GameName")]
            public string GameName { get; set; }

            [XmlElement(ElementName = "Limit")]
            public int Limit { get; set; }

            [XmlElement(ElementName = "BulletDetailsList")]
            public BulletDetailsList2 BulletDetailsList { get; set; }

            [XmlAttribute(AttributeName = "xsd")]
            public string Xsd { get; set; }

            [XmlAttribute(AttributeName = "xsi")]
            public string Xsi { get; set; }

            [XmlText]
            public string Text { get; set; }
        }



        [XmlRoot(ElementName = "GetFishingDetailsURLResponse", Namespace = "")]
        public class GetFishingDetailsURLResponse
        {

            [XmlElement(ElementName = "ErrorMsgId")]
            public int ErrorMsgId { get; set; }

            [XmlElement(ElementName = "ErrorMsg")]
            public string ErrorMsg { get; set; }

            [XmlElement(ElementName = "Url")]
            public string Url { get; set; }

            [XmlAttribute(AttributeName = "xsd")]
            public string Xsd { get; set; }

            [XmlAttribute(AttributeName = "xsi")]
            public string Xsi { get; set; }

            [XmlText]
            public string Text { get; set; }
        }

        [XmlRoot(ElementName = "GetSlotDetailsURLResponse", Namespace = "")]
        public class GetSlotDetailsURLResponse2
        {

            [XmlElement(ElementName = "ErrorMsgId")]
            public int ErrorMsgId { get; set; }

            [XmlElement(ElementName = "ErrorMsg")]
            public string ErrorMsg { get; set; }

            [XmlElement(ElementName = "Url")]
            public string Url { get; set; }

            [XmlAttribute(AttributeName = "xsd")]
            public string Xsd { get; set; }

            [XmlAttribute(AttributeName = "xsi")]
            public string Xsi { get; set; }

            [XmlText]
            public string Text { get; set; }
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

            [XmlElement(ElementName = "GameType")]
            public string GameType { get; set; }

            [XmlElement(ElementName = "GameCode")]
            public string GameCode { get; set; }

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
        }

        [XmlRoot(ElementName = "CancelledBetDetailList")]
        public class CancelledBetDetailList
        {

            [XmlElement(ElementName = "CancelledBetDetail")]
            public List<CancelledBetDetail> CancelledBetDetail { get; set; }
        }

        [XmlRoot(ElementName = "GetCancelledBetDetailsResponse", Namespace = "")]
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



        [XmlRoot(ElementName = "JackpotQueryResponse", Namespace = "")]
        public class JackpotQueryResponse
        {

            [XmlElement(ElementName = "ErrorMsgId")]
            public int ErrorMsgId { get; set; }

            [XmlElement(ElementName = "ErrorMsg")]
            public string ErrorMsg { get; set; }

            [XmlElement(ElementName = "Major")]
            public int Major { get; set; }

            [XmlElement(ElementName = "Minor")]
            public int Minor { get; set; }

            [XmlElement(ElementName = "Mini")]
            public int Mini { get; set; }

            [XmlAttribute(AttributeName = "xsd")]
            public string Xsd { get; set; }

            [XmlAttribute(AttributeName = "xsi")]
            public string Xsi { get; set; }

            [XmlText]
            public string Text { get; set; }
        }



        [XmlRoot(ElementName = "GameLogo")]
        public class GameLogo
        {

            [XmlElement(ElementName = "original")]
            public string Original { get; set; }

            [XmlElement(ElementName = "square")]
            public string Square { get; set; }
        }

        [XmlRoot(ElementName = "Game")]
        public class Game
        {

            [XmlElement(ElementName = "GameCode")]
            public string GameCode { get; set; }

            [XmlElement(ElementName = "GameName")]
            public string GameName { get; set; }

            [XmlElement(ElementName = "GameType")]
            public string GameType { get; set; }

            [XmlElement(ElementName = "GameLogo")]
            public GameLogo GameLogo { get; set; }

            [XmlElement(ElementName = "Enabled")]
            public bool Enabled { get; set; }
        }

        [XmlRoot(ElementName = "GameList")]
        public class GameList
        {

            [XmlElement(ElementName = "Game")]
            public List<Game> Game { get; set; }
        }

        [XmlRoot(ElementName = "GetActiveGameListResponse", Namespace = "")]
        public class GetActiveGameListResponse
        {

            [XmlElement(ElementName = "ErrorMsgId")]
            public int ErrorMsgId { get; set; }

            [XmlElement(ElementName = "ErrorMsg")]
            public string ErrorMsg { get; set; }

            [XmlElement(ElementName = "GameList")]
            public GameList GameList { get; set; }

            [XmlAttribute(AttributeName = "xsd")]
            public string Xsd { get; set; }

            [XmlAttribute(AttributeName = "xsi")]
            public string Xsi { get; set; }

            [XmlText]
            public string Text { get; set; }
        }



        [XmlRoot(ElementName = "RankingItem")]
        public class RankingItem
        {

            [XmlElement(ElementName = "Ranking")]
            public int Ranking { get; set; }

            [XmlElement(ElementName = "GameCode")]
            public string GameCode { get; set; }

            [XmlElement(ElementName = "GameName")]
            public string GameName { get; set; }
        }

        [XmlRoot(ElementName = "RankingList")]
        public class RankingList
        {

            [XmlElement(ElementName = "RankingItem")]
            public List<RankingItem> RankingItem { get; set; }
        }

        [XmlRoot(ElementName = "MonthlyGameRankingQueryResponse", Namespace = "")]
        public class MonthlyGameRankingQueryResponse
        {

            [XmlElement(ElementName = "ErrorMsgId")]
            public int ErrorMsgId { get; set; }

            [XmlElement(ElementName = "ErrorMsg")]
            public string ErrorMsg { get; set; }

            [XmlElement(ElementName = "RankingMonth")]
            public int RankingMonth { get; set; }

            [XmlElement(ElementName = "GameCategory")]
            public string GameCategory { get; set; }

            [XmlElement(ElementName = "RankingList")]
            public RankingList RankingList { get; set; }

            [XmlAttribute(AttributeName = "xsd")]
            public string Xsd { get; set; }

            [XmlAttribute(AttributeName = "xsi")]
            public string Xsi { get; set; }

            [XmlText]
            public string Text { get; set; }
        }

        [XmlRoot(ElementName = "DebitBalanceResponse", Namespace = "")]
        public class DebitBalanceResponse
        {

            [XmlElement(ElementName = "ErrorMsgId")]
            public int ErrorMsgId { get; set; }

            [XmlElement(ElementName = "ErrorMsg")]
            public string ErrorMsg { get; set; }

            [XmlElement(ElementName = "Username")]
            public string Username { get; set; }

            [XmlElement(ElementName = "Balance")]
            public decimal Balance { get; set; }

            [XmlElement(ElementName = "BetAmount")]
            public decimal BetAmount { get; set; }

            [XmlElement(ElementName = "WithholdAmount")]
            public decimal WithholdAmount { get; set; }

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



        [XmlRoot(ElementName = "DebitAllBalanceResponse", Namespace = "")]
        public class DebitAllBalanceResponse
        {

            [XmlElement(ElementName = "ErrorMsgId")]
            public int ErrorMsgId { get; set; }

            [XmlElement(ElementName = "ErrorMsg")]
            public string ErrorMsg { get; set; }

            [XmlElement(ElementName = "Username")]
            public string Username { get; set; }

            [XmlElement(ElementName = "Balance")]
            public decimal Balance { get; set; }

            [XmlElement(ElementName = "BetAmount")]
            public decimal BetAmount { get; set; }

            [XmlElement(ElementName = "WithholdAmount")]
            public decimal WithholdAmount { get; set; }

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



        [XmlRoot(ElementName = "CreditBalanceResponse", Namespace = "")]
        public class CreditBalanceResponse
        {

            [XmlElement(ElementName = "ErrorMsgId")]
            public int ErrorMsgId { get; set; }

            [XmlElement(ElementName = "ErrorMsg")]
            public string ErrorMsg { get; set; }

            [XmlElement(ElementName = "Username")]
            public string Username { get; set; }

            [XmlElement(ElementName = "Balance")]
            public decimal Balance { get; set; }

            [XmlElement(ElementName = "BetAmount")]
            public decimal BetAmount { get; set; }

            [XmlElement(ElementName = "WithholdAmount")]
            public decimal WithholdAmount { get; set; }

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


        [XmlRoot(ElementName = "CheckOrderIdResponse", Namespace = "")]
        public class CheckOrderIdResponse
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
        #endregion

    }

    //public class Test
    //{
    //    public static async Task Main(string[] args)
    //    {
    //        SimplePlay api = new SimplePlay(
    //           "https://api.sp-portal.net/api/api.aspx",
    //           "S5518",
    //           "5B1299AD830F4AD8A5F7B4B31A18B806",
    //           "GgaIMaiNNtg",
    //           "g9G16nTs"
    //       );

    //        Console.WriteLine("Starting API Tests...");


    //        // Call API methods below: 
    //        //if (await api.CreateUser("mitjenny123","MYR"))
    //        //{
    //        //    if (api.RegUserInfoRes.ErrorMsgId == 0)
    //        //    {
    //        //        Console.WriteLine("CreateUser successful!");
    //        //    }
    //        //    else
    //        //    {
    //        //        Console.WriteLine($"CreateUser failed: {api.RegUserInfoRes.ErrorMsg}");
    //        //    }

    //        //}
    //        //else
    //        //{
    //        //    Console.WriteLine($"CreateUser failed: {api.ErrorMessage}");
    //        //}


    //        //if (await api.VerifyUsername("mitjenny123"))
    //        //{
    //        //    if (api.VerifyUsernameRes.ErrorMsgId == 0)
    //        //    {
    //        //        Console.WriteLine("VerifyUsername successful!");
    //        //    }
    //        //    else
    //        //    {
    //        //        Console.WriteLine($"VerifyUsername failed: {api.VerifyUsernameRes.ErrorMsg}");
    //        //    }

    //        //}
    //        //else
    //        //{
    //        //    Console.WriteLine($"VerifyUsername failed: {api.ErrorMessage}");
    //        //}


    //        //if (await api.GetUserStatus("mitjenny123"))
    //        //{
    //        //    if (api.GetUserStatusRes.ErrorMsgId == 0)
    //        //    {
    //        //        Console.WriteLine("GetUserStatus successful!");
    //        //    }
    //        //    else
    //        //    {
    //        //        Console.WriteLine($"GetUserStatus failed: {api.GetUserStatusRes.ErrorMsg}");
    //        //    }

    //        //}
    //        //else
    //        //{
    //        //    Console.WriteLine($"GetUserStatus failed: {api.ErrorMessage}");
    //        //}

    //        //if (await api.SetUserMaxBalance("mitjenny123",9999))
    //        //{
    //        //    if (api.GetUserStatusRes.ErrorMsgId == 0)
    //        //    {
    //        //        Console.WriteLine("SetUserMaxBalance successful!");
    //        //    }
    //        //    else
    //        //    {
    //        //        Console.WriteLine($"SetUserMaxBalance failed: {api.SetUserMaxBalanceRes.ErrorMsg}");
    //        //    }

    //        //}
    //        //else
    //        //{
    //        //    Console.WriteLine($"SetUserMaxBalance failed: {api.ErrorMessage}");
    //        //}

    //        //if (await api.SetUserMaxWinning("mitjenny123", 9999))
    //        //{
    //        //    if (api.SetUserMaxWinningRes.ErrorMsgId == 0)
    //        //    {
    //        //        Console.WriteLine("SetUserMaxWinning successful!");
    //        //    }
    //        //    else
    //        //    {
    //        //        Console.WriteLine($"SetUserMaxWinning failed: {api.SetUserMaxWinningRes.ErrorMsg}");
    //        //    }

    //        //}
    //        //else
    //        //{
    //        //    Console.WriteLine($"SetUserMaxWinning failed: {api.ErrorMessage}");
    //        //}


    //        //if (await api.GetUnfinishedGame("mitjenny123"))
    //        //{
    //        //    if (api.GetUnfinishedGameRes.ErrorMsgId == 0)
    //        //    {
    //        //        Console.WriteLine("GetUnfinishedGame successful!");
    //        //    }
    //        //    else
    //        //    {
    //        //        Console.WriteLine($"GetUnfinishedGame failed: {api.GetUnfinishedGameRes.ErrorMsg}");
    //        //    }

    //        //}
    //        //else
    //        //{
    //        //    Console.WriteLine($"GetUnfinishedGame failed: {api.ErrorMessage}");
    //        //}


    //        //if (await api.LoginRequest("mitjenny123", "MYR"))
    //        //{
    //        //    if (api.LoginRequestRes.ErrorMsgId == 0)
    //        //    {
    //        //        Console.WriteLine("LoginRequest successful!");
    //        //    }
    //        //    else
    //        //    {
    //        //        Console.WriteLine($"LoginRequest failed: {api.LoginRequestRes.ErrorMsg}");
    //        //    }

    //        //}
    //        //else
    //        //{
    //        //    Console.WriteLine($"LoginRequest failed: {api.ErrorMessage}");
    //        //}


    //        //if (await api.LoginRequestForFun("mitjenny123", "MYR"))
    //        //{
    //        //    if (api.LoginRequestForFunRes.ErrorMsgId == 0)
    //        //    {
    //        //        Console.WriteLine("LoginRequestForFun successful!");
    //        //    }
    //        //    else
    //        //    {
    //        //        Console.WriteLine($"LoginRequestForFun failed: {api.LoginRequestForFunRes.ErrorMsg}");
    //        //    }

    //        //}
    //        //else
    //        //{
    //        //    Console.WriteLine($"LoginRequestForFun failed: {api.ErrorMessage}");
    //        //}



    //        //if (await api.KickUser("mitjenny123"))
    //        //{
    //        //    if (api.KickUserRes.ErrorMsgId == 0)
    //        //    {
    //        //        Console.WriteLine("KickUser successful!");
    //        //    }
    //        //    else
    //        //    {
    //        //        Console.WriteLine($"KickUser failed: {api.KickUserRes.ErrorMsg}");
    //        //    }

    //        //}
    //        //else
    //        //{
    //        //    Console.WriteLine($"KickUser failed: {api.ErrorMessage}");
    //        //}


    //        //if (await api.GetAllBetDetails(Date: "2025-03-19"))
    //        //{
    //        //    if (api.GetAllBetDetailsRes.ErrorMsgId == 0)
    //        //    {
    //        //        Console.WriteLine("GetAllBetDetails successful!");
    //        //    }
    //        //    else
    //        //    {
    //        //        Console.WriteLine($"GetAllBetDetails failed: {api.GetAllBetDetailsRes.ErrorMsg}");
    //        //    }

    //        //}
    //        //else
    //        //{
    //        //    Console.WriteLine($"GetAllBetDetails failed: {api.ErrorMessage}");
    //        //}


    //        //if (await api.GetAllBetDetailsForTimeInterval("2025-03-19 00:00:00", "2025-03-19 23:59:59"))
    //        //{
    //        //    if (api.GetAllBetDetailsForTimeIntervalRes.ErrorMsgId == 0)
    //        //    {
    //        //        Console.WriteLine("GetAllBetDetailsForTimeInterval successful!");
    //        //    }
    //        //    else
    //        //    {
    //        //        Console.WriteLine($"GetAllBetDetailsForTimeInterval failed: {api.GetAllBetDetailsForTimeIntervalRes.ErrorMsg}");
    //        //    }

    //        //}
    //        //else
    //        //{
    //        //    Console.WriteLine($"GetAllBetDetailsForTimeInterval failed: {api.ErrorMessage}");
    //        //}


    //        //if (await api.GetUserBetAmount())
    //        //{
    //        //    if (api.GetUserBetAmountRes.ErrorMsgId == 0)
    //        //    {
    //        //        Console.WriteLine("GetUserBetAmount successful!");
    //        //    }
    //        //    else
    //        //    {
    //        //        Console.WriteLine($"GetUserBetAmount failed: {api.GetUserBetAmountRes.ErrorMsg}");
    //        //    }

    //        //}
    //        //else
    //        //{
    //        //    Console.WriteLine($"GetUserBetAmount failed: {api.ErrorMessage}");
    //        //}

    //        //string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
    //        //Console.WriteLine(timestamp);

    //        //if (await api.CreditBalance("mitjenny123", "IN"+ timestamp  + "mitjenny123", 100,"MYR"))
    //        //{
    //        //    if (api.CreditBalanceRes.ErrorMsgId == 0)
    //        //    {
    //        //        Console.WriteLine("CreditBalance successful!");
    //        //    }
    //        //    else
    //        //    {
    //        //        Console.WriteLine($"CreditBalance failed: {api.CreditBalanceRes.ErrorMsg}");
    //        //    }

    //        //}
    //        //else
    //        //{
    //        //    Console.WriteLine($"CreditBalance failed: {api.ErrorMessage}");
    //        //}



    //        Console.WriteLine("API Tests Completed.");
    //        Console.WriteLine("Press any key to exit.");
    //        Console.ReadKey();
    //    }
    //}
}
