
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace GameRecord.Gamess.CQ9Fish
{

    public class CQ9Fish : IDisposable
    {
        private string p_apiUrl;

        private string p_token;

        private int p_timeOutSec;

        private string p_targetUrl;

        private string p_parameter;

        private string p_request;

        private string p_response;

        private string p_errorMessage;

        private bool p_parseError = false;

        private CreatePlayerResponse p_CreatePlayerResponse;

        private GetBalanceResponse p_GetBalanceResponse;

        private DepositResponse p_DepositResponse;

        private WithdrawResponse p_WithdrawResponse;

        private ChangePasswordResponse p_ChangePasswordResponse;

        private GetBetHistoryResponse p_GetBetHistoryResponse;

        private GetGameLinkResponse p_GetGameLinkResponse;

        private GetGameListResponse p_GetGameListResponse;

        private GetGameLobbyLinkResponse p_GetGameLobbyLinkResponse;

        private GetGameProviderListResponse p_GetGameProviderListResponse;

        private GetTotalJackpotAmountResponse p_GetTotalJackpotAmountResponse;

        private GetUserTokenResponse p_GetUserTokenResponse;

        private UserLoginResponse p_UserLoginResponse;

        private UserLogoutResponse p_UserLogoutResponse;

        private readonly Encoding Charset = Encoding.UTF8;

        public string parameter => p_parameter;

        public string response => p_response;

        public string errorMessage => p_errorMessage;

        public string request => p_request;

        public string targetUrl => p_targetUrl;

        public bool parseError => p_parseError;

        // Remove the duplicate property definition
        // Ensure only one definition of CreatePlayerResponse exists

        // Remove this line if it is a duplicate
        // public CreatePlayerResponse CreatePlayerResponse => p_CreatePlayerResponse;

        // Ensure the correct property definition is retained
        public CreatePlayerResponse CreatePlayerResponses => p_CreatePlayerResponse;

        public GetBalanceResponse GetBalanceResponses => p_GetBalanceResponse;

        public DepositResponse DepositResponses => p_DepositResponse;

        public WithdrawResponse WithdrawResponses => p_WithdrawResponse;

        public ChangePasswordResponse ChangePasswordResponses => p_ChangePasswordResponse;

        public GetBetHistoryResponse GetBetHistoryResponses => p_GetBetHistoryResponse;

        public GetGameLinkResponse GetGameLinkResponses => p_GetGameLinkResponse;

        public GetGameListResponse GetGameListResponses => p_GetGameListResponse;

        public GetGameLobbyLinkResponse GetGameLobbyLinkResponses => p_GetGameLobbyLinkResponse;

        public GetGameProviderListResponse GetGameProviderListResponses => p_GetGameProviderListResponse;

        public GetTotalJackpotAmountResponse GetTotalJackpotAmountResponses => p_GetTotalJackpotAmountResponse;

        public GetUserTokenResponse GetUserTokenResponses => p_GetUserTokenResponse;

        public UserLoginResponse UserLoginResponses => p_UserLoginResponse;

        public UserLogoutResponse UserLogoutResponses => p_UserLogoutResponse;

        public CQ9Fish(string _apiUrl, string _token, int _timeOutSec = 120)
        {
            p_apiUrl = _apiUrl + ((_apiUrl.Substring(_apiUrl.Length - 1) != "/") ? "/" : "");
            p_token = _token;
            p_timeOutSec = _timeOutSec;
        }

        public string GenerateGUID()
        {
            return Guid.NewGuid().ToString();
        }

        public bool ConvertJsonToClassObject<T>(string JsonObj, ref T TargetClass)
        {
            try
            {
                TargetClass = JsonConvert.DeserializeObject<T>(JsonObj);
                return true;
            }
            catch (Exception ex)
            {
                p_parseError = true;
                p_errorMessage = ex.Message;
                return false;
            }
        }

        public bool PostJSON<T>(BaseRequest req, ref T resp)
        {
            HttpWebResponse httpWebResponse = null;
            try
            {
                byte[] bytes = Charset.GetBytes(req.GetParam());
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                p_parameter = req.GetParam();
                p_targetUrl = p_apiUrl + req.GetUrl() + "?" + p_parameter;
                p_request = p_targetUrl;
                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(p_targetUrl);
                httpWebRequest.Method = "POST";
                httpWebRequest.Headers.Add("Authorization", p_token);
                httpWebRequest.ContentType = "application/x-www-form-urlencoded";
                httpWebRequest.ContentLength = bytes.Length;
                httpWebRequest.Timeout = p_timeOutSec * 1000;
                using (Stream stream = httpWebRequest.GetRequestStream())
                {
                    stream.Write(bytes, 0, bytes.Length);
                }

                httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                StreamReader streamReader = null;
                streamReader = new StreamReader(httpWebResponse.GetResponseStream(), Charset);
                string text = streamReader.ReadToEnd().Trim();
                p_response = text;
                T TargetClass = default(T);
                if (ConvertJsonToClassObject(p_response, ref TargetClass))
                {
                    resp = TargetClass;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                if (httpWebResponse != null)
                {
                    httpWebResponse.Close();
                    httpWebResponse = null;
                }

                p_errorMessage = ex.Message;
                return false;
            }
        }

        public bool GetJSON<T>(BaseRequest req, ref T resp)
        {
            HttpWebResponse httpWebResponse = null;
            try
            {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                p_parameter = req.GetParam();
                p_targetUrl = p_apiUrl + req.GetUrl() + "?" + p_parameter;
                p_request = p_targetUrl;
                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(p_targetUrl);
                httpWebRequest.Method = "GET";
                httpWebRequest.Headers.Add("Authorization", p_token);
                httpWebRequest.Timeout = p_timeOutSec * 1000;
                HttpWebResponse httpWebResponse2 = (HttpWebResponse)httpWebRequest.GetResponse();
                StreamReader streamReader = null;
                streamReader = new StreamReader(httpWebResponse2.GetResponseStream(), Charset);
                string text = streamReader.ReadToEnd().Trim();
                p_response = text;
                T TargetClass = default(T);
                if (ConvertJsonToClassObject(p_response, ref TargetClass))
                {
                    resp = TargetClass;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                if (httpWebResponse != null)
                {
                    httpWebResponse.Close();
                    httpWebResponse = null;
                }

                p_errorMessage = ex.Message;
                return false;
            }
        }

        private string DataSerializeJSON(ref object TargetClass)
        {
            return JsonConvert.SerializeObject(TargetClass);
        }

        public bool CreatePlayer(string account, string password, string nickname)
        {
            CreatePlayerRequest req = new CreatePlayerRequest
            {
                account = account,
                password = password,
                nickname = nickname
            };
            try
            {
                if (PostJSON(req, ref p_CreatePlayerResponse))
                {
                    if (p_CreatePlayerResponse != null && p_CreatePlayerResponse.status.code == "0")
                    {
                        return true;
                    }

                    p_errorMessage = p_CreatePlayerResponse.status.message;
                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                p_errorMessage = ex.Message;
                return false;
            }
        }

        public bool UserLogin(string account, string password)
        {
            UserLoginRequest req = new UserLoginRequest
            {
                account = account,
                password = password
            };
            try
            {
                if (PostJSON(req, ref p_UserLoginResponse))
                {
                    if (p_UserLoginResponse != null && p_UserLoginResponse.status.code == "0")
                    {
                        return true;
                    }

                    p_errorMessage = p_UserLoginResponse.status.message;
                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                p_errorMessage = ex.Message;
                return false;
            }
        }

        public bool UserLogout(string account)
        {
            UserLogoutRequest req = new UserLogoutRequest
            {
                account = account
            };
            try
            {
                if (PostJSON(req, ref p_UserLogoutResponse))
                {
                    if (p_UserLogoutResponse != null && p_UserLogoutResponse.status.code == "0")
                    {
                        return true;
                    }

                    p_errorMessage = p_UserLogoutResponse.status.message;
                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                p_errorMessage = ex.Message;
                return false;
            }
        }

        public bool GetBalance(string account)
        {
            GetBalanceRequest req = new GetBalanceRequest
            {
                account = account
            };
            try
            {
                if (GetJSON(req, ref p_GetBalanceResponse))
                {
                    if (p_GetBalanceResponse != null && p_GetBalanceResponse.status.code == "0")
                    {
                        return true;
                    }

                    p_errorMessage = p_GetBalanceResponse.status.message;
                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                p_errorMessage = ex.Message;
                return false;
            }
        }

        public bool Deposit(string account, decimal amount)
        {
            DepositRequest req = new DepositRequest
            {
                account = account,
                mtcode = GenerateGUID(),
                amount = amount
            };
            try
            {
                if (PostJSON(req, ref p_DepositResponse))
                {
                    if (p_DepositResponse != null && p_DepositResponse.status.code == "0")
                    {
                        return true;
                    }

                    p_errorMessage = p_DepositResponse.status.message;
                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                p_errorMessage = ex.Message;
                return false;
            }
        }

        public bool Withdrawal(string account, decimal amount)
        {
            WithdrawRequest req = new WithdrawRequest
            {
                account = account,
                mtcode = GenerateGUID(),
                amount = amount
            };
            try
            {
                if (PostJSON(req, ref p_WithdrawResponse))
                {
                    if (p_WithdrawResponse != null && p_WithdrawResponse.status.code == "0")
                    {
                        return true;
                    }

                    p_errorMessage = p_WithdrawResponse.status.message;
                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                p_errorMessage = ex.Message;
                return false;
            }
        }

        public bool ChangePassword(string account, string password)
        {
            ChangePasswordRequest req = new ChangePasswordRequest
            {
                account = account,
                password = password
            };
            try
            {
                if (PostJSON(req, ref p_ChangePasswordResponse))
                {
                    if (p_ChangePasswordResponse != null && p_ChangePasswordResponse.status.code == "0")
                    {
                        return true;
                    }

                    p_errorMessage = p_ChangePasswordResponse.status.message;
                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                p_errorMessage = ex.Message;
                return false;
            }
        }

        public bool GetUserToken(string account)
        {
            GetUserTokenRequest req = new GetUserTokenRequest
            {
                account = account
            };
            try
            {
                if (GetJSON(req, ref p_GetUserTokenResponse))
                {
                    if (p_GetUserTokenResponse != null && p_GetUserTokenResponse.status.code == "0")
                    {
                        return true;
                    }

                    p_errorMessage = p_GetUserTokenResponse.status.message;
                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                p_errorMessage = ex.Message;
                return false;
            }
        }

        public bool GetGameLobbyLink(string account, string password, string lang = "en")
        {
            if (UserLogin(account, password))
            {
                GetGameLobbyLinkRequest req = new GetGameLobbyLinkRequest
                {
                    account = account,
                    usertoken = p_UserLoginResponse.data.usertoken,
                    lang = lang
                };
                try
                {
                    if (PostJSON(req, ref p_GetGameLobbyLinkResponse))
                    {
                        if (p_GetGameLobbyLinkResponse != null && p_GetGameLobbyLinkResponse.status.code == "0")
                        {
                            return true;
                        }

                        p_errorMessage = p_GetGameLobbyLinkResponse.status.message;
                        return false;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    p_errorMessage = ex.Message;
                    return false;
                }
            }

            return false;
        }

        public bool GetGameLink(string account, string password, string gamehall, string gamecode, string gameplat, string lang = "en")
        {
            if (UserLogin(account, password))
            {
                GetGameLinkRequest req = new GetGameLinkRequest
                {
                    account = account,
                    gamehall = gamehall,
                    gamecode = gamecode,
                    gameplat = gameplat,
                    usertoken = p_UserLoginResponse.data.usertoken,
                    lang = lang
                };
                try
                {
                    if (PostJSON(req, ref p_GetGameLinkResponse))
                    {
                        if (p_GetGameLinkResponse != null && p_GetGameLinkResponse.status.code == "0")
                        {
                            return true;
                        }

                        p_errorMessage = p_GetGameLinkResponse.status.message;
                        return false;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    p_errorMessage = ex.Message;
                    return false;
                }
            }

            return false;
        }

        public bool GetGameProviderList()
        {
            GetGameProviderListRequest req = new GetGameProviderListRequest();
            try
            {
                if (GetJSON(req, ref p_GetGameProviderListResponse))
                {
                    if (p_GetGameProviderListResponse != null && p_GetGameProviderListResponse.status.code == "0")
                    {
                        return true;
                    }

                    p_errorMessage = p_GetGameProviderListResponse.status.message;
                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                p_errorMessage = ex.Message;
                return false;
            }
        }

        public bool GetGameList(string gamehall)
        {
            GetGameListRequest req = new GetGameListRequest
            {
                gamehall = gamehall
            };
            try
            {
                if (GetJSON(req, ref p_GetGameListResponse))
                {
                    if (p_GetGameListResponse != null && p_GetGameListResponse.status.code == "0")
                    {
                        return true;
                    }

                    p_errorMessage = p_GetGameListResponse.status.message;
                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                p_errorMessage = ex.Message;
                return false;
            }
        }

        public bool GetGameHallList(string gamehall)
        {
            GetGameListRequest req = new GetGameListRequest
            {
                gamehall = gamehall
            };
            try
            {
                if (GetJSON(req, ref p_GetGameListResponse))
                {
                    if (p_GetGameListResponse != null && p_GetGameListResponse.status.code == "0")
                    {
                        return true;
                    }

                    p_errorMessage = p_GetGameListResponse.status.message;
                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                p_errorMessage = ex.Message;
                return false;
            }
        }

        public bool GetTotalJackpotAmount()
        {
            GetTotalJackpotAmountRequest req = new GetTotalJackpotAmountRequest();
            try
            {
                if (GetJSON(req, ref p_GetTotalJackpotAmountResponse))
                {
                    if (p_GetTotalJackpotAmountResponse != null && p_GetTotalJackpotAmountResponse.status.code == "0")
                    {
                        return true;
                    }

                    p_errorMessage = p_GetTotalJackpotAmountResponse.status.message;
                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                p_errorMessage = ex.Message;
                return false;
            }
        }

        public bool GetBetHistory(DateTime starttime, DateTime endtime, int page, int pagesize = 500)
        {
            GetBetHistoryRequest req = new GetBetHistoryRequest
            {
                starttime = starttime,
                endtime = endtime,
                page = page,
                pagesize = pagesize
            };
            try
            {
                if (GetJSON(req, ref p_GetBetHistoryResponse))
                {
                    if (p_GetBetHistoryResponse != null && (p_GetBetHistoryResponse.status.code == "0" || p_GetBetHistoryResponse.status.code == "8"))
                    {
                        return true;
                    }

                    p_errorMessage = p_GetBetHistoryResponse.status.message;
                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                p_errorMessage = ex.Message;
                return false;
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        #region Model
        public class BaseResponse
        {
            public Status status { get; set; }
        }
        public class Status
        {
            public string code { get; set; }

            public string message { get; set; }

            public DateTime datetime { get; set; }

            public string traceCode { get; set; }
        }

        public class GetBalanceResponse : BaseResponse
        {
            public BalanceData data { get; set; }
        }

        public class BalanceData
        {
            public string balance { get; set; }

            public string currency { get; set; }
        }

        public class DepositResponse : BaseResponse
        {
            public DepositData data { get; set; }
        }

        public class DepositData
        {
            public string balance { get; set; }

            public string currency { get; set; }
        }

        public class WithdrawResponse : BaseResponse
        {
            public WithdrawData data { get; set; }
        }

        public class WithdrawData
        {
            public string balance { get; set; }

            public string currency { get; set; }
        }


        public class CreatePlayerResponse : BaseResponse
        {
            public CreateUserData data { get; set; }
        }

        public class CreateUserData
        {
            public string account { get; set; }

            public string password { get; set; }

            public string nickname { get; set; }
        }
        public class ChangePasswordResponse : BaseResponse
        {
            public string data { get; set; }
        }

        public class GetBetHistoryResponse : BaseResponse
        {
            public BetHistoryData data { get; set; }
        }

        public class BetHistoryData
        {
            public int TotalSize { get; set; }

            public List<Datum> Data { get; set; }
        }

        public class Datum
        {
            public string gamehall { get; set; }

            public string gametype { get; set; }

            public string gameplat { get; set; }

            public string gamecode { get; set; }

            public string account { get; set; }

            public string round { get; set; }

            public decimal? balance { get; set; }

            public decimal? win { get; set; }

            public decimal? bet { get; set; }

            public decimal? validbet { get; set; }

            public decimal? jackpot { get; set; }

            public object jackpotcontribution { get; set; }

            public string jackpottype { get; set; }

            public string status { get; set; }

            public DateTime? endroundtime { get; set; }

            public DateTime? createtime { get; set; }

            public DateTime? bettime { get; set; }

            public object detail { get; set; }

            public bool? singlerowbet { get; set; }

            public object gamerole { get; set; }

            public object bankertype { get; set; }

            public int rake { get; set; }

            public int roomfee { get; set; }

            public object bettype { get; set; }

            public Gameresult gameresult { get; set; }

            public object tabletype { get; set; }

            public object tableid { get; set; }

            public object roundnumber { get; set; }
        }

        public class Gameresult
        {
            public object points { get; set; }

            public object cards { get; set; }
        }

        public class GetGameLinkResponse : BaseResponse
        {
            public GetGameLinkData data { get; set; }
        }

        public class GetGameLinkData
        {
            public string url { get; set; }

            public string token { get; set; }
        }

        public class GetGameListResponse : BaseResponse
        {
            private List<GameListData> m_data;

            public List<GameListData> data
            {
                get
                {
                    return m_data;
                }
                set
                {
                    m_data = value;
                }
            }
        }

        public class GameListData
        {
            public string gamehall { get; set; }

            public string gametype { get; set; }

            public string gamecode { get; set; }

            public string gamename { get; set; }

            public string gameplat { get; set; }

            public object lang { get; set; }

            public string gametech { get; set; }

            public bool status { get; set; }

            public bool maintain { get; set; }
            public List<NameSet> nameset { get; set; } // Updated to handle the JSON response
        }

        public class NameSet
        {
            public string name { get; set; }
            public string lang { get; set; }
        }

        public class GetGameLobbyLinkResponse : BaseResponse
        {
            public GetGameLobbyLinkData data { get; set; }
        }

        public class GetGameLobbyLinkData
        {
            public string url { get; set; }

            public string token { get; set; }
        }

        public class GetGameProviderListResponse : BaseResponse
        {
            public List<GetGameProviderListData> data { get; set; }
        }

        public class GetGameProviderListData
        {
            public string gamehall { get; set; }

            public string fullname { get; set; }
        }

        public class GetTotalJackpotAmountResponse : BaseResponse
        {
            public List<Data> items { get; set; }
        }

        public class Data
        {
            public int amount { get; set; }
        }
        public class GetUserTokenResponse : BaseResponse
        {
            public GetUserTokenData data { get; set; }
        }

        public class GetUserTokenData
        {
            public string usertoken { get; set; }

            public string status { get; set; }

            public int count { get; set; }
        }

        public class UserLoginResponse : BaseResponse
        {
            public UserLoginData data { get; set; }
        }

        public class UserLoginData
        {
            public string usertoken { get; set; }
        }

        public class UserLogoutResponse : BaseResponse
        {
            public string data { get; set; }
        }



        public abstract class BaseRequest
        {
            public string account { get; set; }

            public abstract string GetUrl();

            public abstract string GetParam();
        }


        #endregion

        #region ModelClasses

        public class DepositRequest : BaseRequest
        {
            public string mtcode { get; set; }

            public decimal amount { get; set; }

            public override string GetParam()
            {
                List<string> list = new List<string>();
                list.Add("account=" + base.account);
                list.Add("mtcode=" + mtcode);
                list.Add("amount=" + amount);
                return string.Join("&", list);
            }

            public override string GetUrl()
            {
                return "gameboy/player/deposit";
            }
        }

        public class GetGameListRequest : BaseRequest
        {
            public string gamehall { get; set; }

            public override string GetParam()
            {
                return "";
            }

            public override string GetUrl()
            {
                return "gameboy/game/list/" + gamehall;
            }
        }

        public class GetGameProviderListRequest : BaseRequest
        {
            public override string GetParam()
            {
                return "";
            }

            public override string GetUrl()
            {
                return "gameboy/game/halls";
            }
        }

        public class WithdrawRequest : BaseRequest
        {
            public string mtcode { get; set; }

            public decimal amount { get; set; }

            public override string GetParam()
            {
                List<string> list = new List<string>();
                list.Add("account=" + base.account);
                list.Add("mtcode=" + mtcode);
                list.Add("amount=" + amount);
                return string.Join("&", list);
            }

            public override string GetUrl()
            {
                return "gameboy/player/withdraw";
            }
        }

        public class CreatePlayerRequest : BaseRequest
        {
            public string password { get; set; }

            public string nickname { get; set; }

            public override string GetParam()
            {
                List<string> list = new List<string>();
                list.Add("account=" + base.account);
                list.Add("password=" + password);
                list.Add("nickname=" + nickname);
                return string.Join("&", list);
            }

            public override string GetUrl()
            {
                return "gameboy/player";
            }
        }

        public class UserLoginRequest : BaseRequest
        {
            public string password { get; set; }

            public override string GetParam()
            {
                List<string> list = new List<string>();
                list.Add("account=" + base.account);
                list.Add("password=" + password);
                return string.Join("&", list);
            }

            public override string GetUrl()
            {
                return "gameboy/player/login";
            }
        }

        public class UserLogoutRequest : BaseRequest
        {
            public override string GetParam()
            {
                List<string> list = new List<string>();
                list.Add("account=" + base.account);
                return string.Join("&", list);
            }

            public override string GetUrl()
            {
                return "gameboy/player/logout";
            }
        }

        public class GetBalanceRequest : BaseRequest
        {
            public override string GetParam()
            {
                return "";
            }

            public override string GetUrl()
            {
                return "gameboy/player/balance/" + base.account;
            }
        }

        public class ChangePasswordRequest : BaseRequest
        {
            public string password { get; set; }

            public override string GetParam()
            {
                List<string> list = new List<string>();
                list.Add("account=" + base.account);
                list.Add("password=" + password);
                return string.Join("&", list);
            }

            public override string GetUrl()
            {
                return "gameboy/player/pwd";
            }
        }

        public class GetUserTokenRequest : BaseRequest
        {
            public override string GetParam()
            {
                return "";
            }

            public override string GetUrl()
            {
                return "gameboy/game/token/" + base.account;
            }
        }

        public class GetGameLobbyLinkRequest : BaseRequest
        {
            public string usertoken { get; set; }

            public string lang { get; set; }

            public override string GetParam()
            {
                List<string> list = new List<string>();
                list.Add("usertoken=" + usertoken);
                list.Add("lang=" + lang);
                return string.Join("&", list);
            }

            public override string GetUrl()
            {
                return "gameboy/player/lobbylink";
            }
        }

        public class GetGameLinkRequest : BaseRequest
        {
            public string usertoken { get; set; }

            public string gamehall { get; set; }

            public string gamecode { get; set; }

            public string gameplat { get; set; }

            public string lang { get; set; }

            public override string GetParam()
            {
                List<string> list = new List<string>();
                list.Add("usertoken=" + usertoken);
                list.Add("gamehall=" + gamehall);
                list.Add("gamecode=" + gamecode);
                list.Add("gameplat=" + gameplat);
                list.Add("lang=" + lang);
                return string.Join("&", list);
            }

            public override string GetUrl()
            {
                return "gameboy/player/gamelink";
            }
        }

        public class GetTotalJackpotAmountRequest : BaseRequest
        {
            public override string GetParam()
            {
                return "";
            }

            public override string GetUrl()
            {
                return "gameboy/game/jackpot";
            }
        }

        public class GetBetHistoryRequest : BaseRequest
        {
            public DateTime starttime { get; set; }

            public DateTime endtime { get; set; }

            public int page { get; set; }

            public int pagesize { get; set; }

            public override string GetParam()
            {
                List<string> list = new List<string>();
                list.Add("starttime=" + starttime.ToString("yyyy-MM-dd'T'HH:mm:ss") + "-04:00");
                list.Add("endtime=" + endtime.ToString("yyyy-MM-dd'T'HH:mm:ss") + "-04:00");
                list.Add("page=" + page);
                list.Add("pagesize=" + pagesize);
                return string.Join("&", list);
            }

            public override string GetUrl()
            {
                return "gameboy/order/view";
            }
        }
        #endregion
    }
}