using GameRecord.Gamess.BTG.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace GameRecord.Gamess.BTG
{
    public class BTG : IDisposable
    {
        #region Declaration
        private string P_apiUrl { get; set; }
        private string P_accountId { get; set; }
        private string P_securityCode { get; set; }
        private int P_timeOutSec { get; set; }

        public string Parameters
        {
            get
            {
                return P_Parameters;
            }
        }
        private string P_Parameters;

        public string Functions
        {
            get
            {
                return P_Functions;
            }
        }
        private string P_Functions;

        public string Response
        {
            get
            {
                return P_Response;
            }
        }
        private string P_Response;

        public string ErrorMessage
        {
            get
            {
                return p_ErrorMessage;
            }
        }
        private string p_ErrorMessage;

        public string Request
        {
            get
            {
                return p_Request;
            }
        }
        private string p_Request;

        public string TargetApiUrl
        {
            get
            {
                return p_TargetApiUrl;
            }
        }
        private string p_TargetApiUrl;

        public bool ParseError
        {
            get
            {
                return p_ParseError;
            }
        }
        private bool p_ParseError = false;

        public GetBalanceResponse GetBalanceResponse { get { return p_GetBalanceResponse; } }
        public DepositAmountResponse DepositAmountResponse { get { return p_DepositAmountResponse; } }
        public WithdrawAmountResponse WithdrawAmountResponse { get { return p_WithdrawAmountResponse; } }
        public UserLoginResponse UserLoginResponse { get { return p_UserLoginResponse; } }
        public LaunchGameResponse LaunchGameResponse { get { return p_LaunchGameResponse; } }
        public EnterLobbyResponse EnterLobbyResponse { get { return p_EnterLobbyResponse; } }
        public UserLogoutResponse UserLogoutResponse { get { return p_UserLogoutResponse; } }
        public GetGameListResponse GetGameListResponse { get { return p_GetGameListResponse; } }
        public QueryGameHistoriesResponse QueryGameHistoriesResponse { get { return p_QueryGameHistoriesResponse; } }
        public QueryPlayerHistoriesResponse QueryPlayerHistoriesResponse { get { return p_QueryPlayerHistoriesResponse; } }
        public QueryPlayerFinancesResponse QueryPlayerFinancesResponse { get { return p_QueryPlayerFinancesResponse; } }
        public QueryFinancesResponse QueryFinancesResponse { get { return p_QueryFinancesResponse; } }
        public QueryBettingRecordResponse QueryBettingRecordResponse { get { return p_QueryBettingRecordResponse; } }

        private GetBalanceResponse p_GetBalanceResponse;
        private DepositAmountResponse p_DepositAmountResponse;
        private WithdrawAmountResponse p_WithdrawAmountResponse;
        private UserLoginResponse p_UserLoginResponse;
        private LaunchGameResponse p_LaunchGameResponse;
        private EnterLobbyResponse p_EnterLobbyResponse;
        private UserLogoutResponse p_UserLogoutResponse;
        private GetGameListResponse p_GetGameListResponse;
        private QueryGameHistoriesResponse p_QueryGameHistoriesResponse;
        private QueryPlayerHistoriesResponse p_QueryPlayerHistoriesResponse;
        private QueryPlayerFinancesResponse p_QueryPlayerFinancesResponse;
        private QueryFinancesResponse p_QueryFinancesResponse;
        private QueryBettingRecordResponse p_QueryBettingRecordResponse;

        public BTG(string apiUrl, string accountId, string securityCode, int timeOutSec = 30)
        {
            P_apiUrl = apiUrl + ((apiUrl.Substring(apiUrl.Length - 1) != "/") ? "/" : "");
            P_accountId = accountId;
            P_securityCode = securityCode;
            P_timeOutSec = timeOutSec;
        }

        private readonly Encoding Charset = Encoding.UTF8;
        #endregion

        #region General Functions

        private string MD5Hashing(string input)
        {
            StringBuilder hash = new StringBuilder();
            MD5CryptoServiceProvider md5provider = new MD5CryptoServiceProvider();
            byte[] bytes = md5provider.ComputeHash(new UTF8Encoding().GetBytes(input));

            for (int i = 0; i < bytes.Length; i++)
            {
                hash.Append(bytes[i].ToString("x2"));
            }
            return hash.ToString();
        }

        private bool ConvertJsonToClassObject<T>(string JsonObj, ref T TargetClass)
        {
            try
            {
                TargetClass = JsonConvert.DeserializeObject<T>(JsonObj);
                return true;
            }
            catch (Exception ex)
            {
                p_ErrorMessage = ex.Message;
                return false;
            }
        }

        private bool PostJSON<T>(BaseRequest req, ref T resp)
        {
            try
            {
                req.AccountId = P_accountId;
                req.CheckCode = GenerateCheckCode(req);
                
                P_Functions = req.Function();
                p_TargetApiUrl = P_apiUrl + P_Functions;

                string jsonData = JsonConvert.SerializeObject(req, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });

                P_Parameters = jsonData;
                p_Request = p_TargetApiUrl + " | " + jsonData;

                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(p_TargetApiUrl);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Timeout = P_timeOutSec * 1000;

                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    streamWriter.Write(jsonData);
                    streamWriter.Flush();
                }

                HttpWebResponse objResponse = (HttpWebResponse)request.GetResponse();

                resp = default(T);

                if ((int)objResponse.StatusCode == 200)
                {
                    StreamReader StreamReader = null;
                    StreamReader = new StreamReader(objResponse.GetResponseStream(), Charset);
                    P_Response = StreamReader.ReadToEnd().Trim();
                    if (ConvertJsonToClassObject<T>(P_Response, ref resp))
                    {
                        return true;
                    }
                    return false;
                }
                else
                {
                    p_ErrorMessage = objResponse.StatusDescription;
                    return false;
                }
            }
            catch (WebException ex1)
            {
                HttpWebResponse errorResponse = (HttpWebResponse)ex1.Response;
                if (errorResponse != null)
                {
                    StreamReader StreamReader = null;
                    StreamReader = new StreamReader(errorResponse.GetResponseStream(), Charset);
                    P_Response = StreamReader.ReadToEnd().Trim();
                    if (ConvertJsonToClassObject<T>(P_Response, ref resp))
                    {
                        p_ErrorMessage = "";
                        return true;
                    }
                }
                else
                {
                    p_ErrorMessage = ex1.Message;
                }
                return false;
            }
            catch (Exception ex)
            {
                p_ErrorMessage = ex.Message;
                return false;
            }
        }

        private string GenerateCheckCode(BaseRequest req)
        {
            var properties = req.GetType().GetProperties();
            var sortedParams = new SortedDictionary<string, string>();

            foreach (var prop in properties)
            {
                var propName = prop.Name;
                var jsonProperty = prop.GetCustomAttributes(typeof(JsonPropertyAttribute), false);
                
                if (jsonProperty.Length > 0)
                {
                    propName = ((JsonPropertyAttribute)jsonProperty[0]).PropertyName;
                }

                if (propName != "check_code" && propName != "CheckCode" && propName != "plat_username")
                {
                    var value = prop.GetValue(req);
                    if (value != null)
                    {
                        sortedParams[propName] = value.ToString();
                        if (value is bool)
                        {
                            sortedParams[propName] = (bool)value ? "true" : "false";
                        }
                    }
                }
            }

            StringBuilder paramString = new StringBuilder();
            paramString.Append($"security_code={P_securityCode}");

            foreach (var param in sortedParams)
            {
                paramString.Append($"&{param.Key}={param.Value}");
            }

            return MD5Hashing(paramString.ToString());
        }
        #endregion

        #region Api Functions

        public bool GetBalance(string username)
        {
            GetBalanceRequest req = new GetBalanceRequest() { Username = username };

            try
            {
                if (PostJSON<GetBalanceResponse>(req, ref p_GetBalanceResponse))
                {
                    if (p_GetBalanceResponse != null && p_GetBalanceResponse.Status.Code == 1000)
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
                    return false;
                }
            }
            catch (Exception ex)
            {
                p_ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool DepositAmount(string username, string depositAmount, string externalOrderId)
        {
            DepositAmountRequest req = new DepositAmountRequest()
            {
                Username = username,
                DepositAmount = depositAmount,
                ExternalOrderId = externalOrderId
            };

            try
            {
                if (PostJSON<DepositAmountResponse>(req, ref p_DepositAmountResponse))
                {
                    if (p_DepositAmountResponse != null && p_DepositAmountResponse.Status.Code == 1000)
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
                    return false;
                }
            }
            catch (Exception ex)
            {
                p_ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool WithdrawAmount(string username, string withdrawAmount, string externalOrderId, bool? takeAll = null)
        {
            WithdrawAmountRequest req = new WithdrawAmountRequest()
            {
                Username = username,
                //WithdrawAmount = withdrawAmount,
                ExternalOrderId = externalOrderId,
                TakeAll = takeAll
            };

            try
            {
                if (PostJSON<WithdrawAmountResponse>(req, ref p_WithdrawAmountResponse))
                {
                    if (p_WithdrawAmountResponse != null && p_WithdrawAmountResponse.Status.Code == 1000)
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
                    return false;
                }
            }
            catch (Exception ex)
            {
                p_ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool UserLogin(string username, string gameCode, string lang = "en", string platUsername = null, 
            string backUrl = null, string routing = "world", int? curType = null, bool? demo = false, string device = "desktop")
        {
            UserLoginRequest req = new UserLoginRequest()
            {
                username = username,
                game_code = gameCode,
                lang = lang,
                plat_username = platUsername,
                back_url = backUrl,
                routing = routing,
                cur_type = curType,
                demo = demo,
                device = device
            };

            try
            {
                if (PostJSON<UserLoginResponse>(req, ref p_UserLoginResponse))
                {
                    if (p_UserLoginResponse != null && p_UserLoginResponse.Status.Code == 1000)
                    {
                        return true;
                    }
                    else
                    {
                        p_ErrorMessage = p_UserLoginResponse?.Status?.Message ?? "Unknown error";
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                p_ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool LaunchGame(string username, string gameCode, string lang, string curType = null, int? demo = null, string homeUrl = null)
        {
            LaunchGameRequest req = new LaunchGameRequest()
            {
                Username = username,
                GameCode = gameCode,
                Lang = lang,
                CurType = curType,
                Demo = demo,
                HomeUrl = homeUrl
            };

            try
            {
                if (PostJSON<LaunchGameResponse>(req, ref p_LaunchGameResponse))
                {
                    if (p_LaunchGameResponse != null && p_LaunchGameResponse.Status.Code == 1000)
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
                    return false;
                }
            }
            catch (Exception ex)
            {
                p_ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool EnterLobby(string username, string lang, string gameType = null, string curType = null, int? cnRouting = null, 
            string priorGametype = null, string homeUrl = null)
        {
            EnterLobbyRequest req = new EnterLobbyRequest()
            {
                Username = username,
                Lang = lang,
                GameType = gameType,
                CurType = curType,
                CnRouting = cnRouting,
                PriorGametype = priorGametype,
                HomeUrl = homeUrl
            };

            try
            {
                if (PostJSON<EnterLobbyResponse>(req, ref p_EnterLobbyResponse))
                {
                    if (p_EnterLobbyResponse != null && p_EnterLobbyResponse.Status.Code == 1000)
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
                    return false;
                }
            }
            catch (Exception ex)
            {
                p_ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool UserLogout(string username)
        {
            UserLogoutRequest req = new UserLogoutRequest() { Username = username };

            try
            {
                if (PostJSON<UserLogoutResponse>(req, ref p_UserLogoutResponse))
                {
                    if (p_UserLogoutResponse != null && p_UserLogoutResponse.Status.Code == 1000)
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
                    return false;
                }
            }
            catch (Exception ex)
            {
                p_ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool GetGameList(string lang, string gameType = null)
        {
            GetGameListRequest req = new GetGameListRequest()
            {
                Lang = lang,
                GameType = gameType
            };

            try
            {
                if (PostJSON<GetGameListResponse>(req, ref p_GetGameListResponse))
                {
                    if (p_GetGameListResponse != null && p_GetGameListResponse.Status.Code == 1000)
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
                    return false;
                }
            }
            catch (Exception ex)
            {
                p_ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool QueryGameHistories(DateTime startTime, DateTime endTime, int page, int pageSize, string gameType = null)
        {
            QueryGameHistoriesRequest req = new QueryGameHistoriesRequest()
            {
                StartTime = startTime.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                EndTime = endTime.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                Page = page,
                PageSize = pageSize,
                GameType = gameType
            };

            try
            {
                if (PostJSON<QueryGameHistoriesResponse>(req, ref p_QueryGameHistoriesResponse))
                {
                    if (p_QueryGameHistoriesResponse != null && p_QueryGameHistoriesResponse.Status.Code == 1000)
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
                    return false;
                }
            }
            catch (Exception ex)
            {
                p_ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool QueryPlayerHistories(string username, DateTime startTime, DateTime endTime, int page, int pageSize, string gameType = null)
        {
            QueryPlayerHistoriesRequest req = new QueryPlayerHistoriesRequest()
            {
                Username = username,
                StartTime = startTime.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                EndTime = endTime.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                Page = page,
                PageSize = pageSize,
                GameType = gameType
            };

            try
            {
                if (PostJSON<QueryPlayerHistoriesResponse>(req, ref p_QueryPlayerHistoriesResponse))
                {
                    if (p_QueryPlayerHistoriesResponse != null && p_QueryPlayerHistoriesResponse.Status.Code == 1000)
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
                    return false;
                }
            }
            catch (Exception ex)
            {
                p_ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool QueryPlayerFinances(string username, DateTime startTime, DateTime endTime)
        {
            QueryPlayerFinancesRequest req = new QueryPlayerFinancesRequest()
            {
                Username = username,
                StartTime = startTime.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                EndTime = endTime.ToString("yyyy-MM-ddTHH:mm:sszzz")
            };

            try
            {
                if (PostJSON<QueryPlayerFinancesResponse>(req, ref p_QueryPlayerFinancesResponse))
                {
                    if (p_QueryPlayerFinancesResponse != null && p_QueryPlayerFinancesResponse.Status.Code == 1000)
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
                    return false;
                }
            }
            catch (Exception ex)
            {
                p_ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool QueryFinances(DateTime startTime, DateTime endTime, int page, int pageSize)
        {
            QueryFinancesRequest req = new QueryFinancesRequest()
            {
                StartTime = startTime.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                EndTime = endTime.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                Page = page,
                PageSize = pageSize
            };

            try
            {
                if (PostJSON<QueryFinancesResponse>(req, ref p_QueryFinancesResponse))
                {
                    if (p_QueryFinancesResponse != null && p_QueryFinancesResponse.Status.Code == 1000)
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
                    return false;
                }
            }
            catch (Exception ex)
            {
                p_ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool QueryBettingRecord(string orderId, string lang)
        {
            QueryBettingRecordRequest req = new QueryBettingRecordRequest()
            {
                OrderId = orderId,
                Lang = lang
            };

            try
            {
                if (PostJSON<QueryBettingRecordResponse>(req, ref p_QueryBettingRecordResponse))
                {
                    if (p_QueryBettingRecordResponse != null && p_QueryBettingRecordResponse.Status.Code == 1000)
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
                    return false;
                }
            }
            catch (Exception ex)
            {
                p_ErrorMessage = ex.Message;
                return false;
            }
        }

        #endregion

        public void Dispose()
        {
            // Cleanup if needed
        }
    }
}
