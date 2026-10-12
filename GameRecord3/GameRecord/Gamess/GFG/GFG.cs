using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace GameRecord.Gamess.GFG
{
    public class GFG : IDisposable
    {


        #region Declaration
        private string P_apiUrl { get; set; }
        private string P_agent { get; set; }
        private string P_secretKey { get; set; }
        private string P_companyKey { get; set; }
        private string P_theme { get; set; }
        private int P_timeOutSec { get; set; }

        public string Parameters
        {
            get
            {
                return P_Parameters;
            }
        }
        private string P_Parameters;

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

        public DoTransferDepositTaskResponse DoTransferDepositTaskResponse { get { return p_DoTransferDepositTaskResponse; } }
        public DoTransferWithdrawTaskResponse DoTransferWithdrawTaskResponse { get { return p_DoTransferWithdrawTaskResponse; } }
        public GetGameListResponse GetGameListResponse { get { return p_GetGameListResponse; } }
        public KickUserResponse KickUserResponse { get { return p_KickUserResponse; } }
        public LoginResponse LoginResponse { get { return p_LoginResponse; } }
        public QueryTransferOrderResponse QueryTransferOrderResponse { get { return p_QueryTransferOrderResponse; } }
        public QueryUserScoreResponse QueryUserScoreResponse { get { return p_QueryUserScoreResponse; } }
        public TakeBetLogsResponse TakeBetLogsResponse { get { return p_TakeBetLogsResponse; } }

        private DoTransferDepositTaskResponse p_DoTransferDepositTaskResponse;
        private DoTransferWithdrawTaskResponse p_DoTransferWithdrawTaskResponse;
        private GetGameListResponse p_GetGameListResponse;
        private KickUserResponse p_KickUserResponse;
        private LoginResponse p_LoginResponse;
        private QueryTransferOrderResponse p_QueryTransferOrderResponse;
        private QueryUserScoreResponse p_QueryUserScoreResponse;
        private TakeBetLogsResponse p_TakeBetLogsResponse;

        public GFG(string apiUrl, string companyKey, string secretKey, string agentKey, string theme, int timeOutSec = 30)
        {
            P_apiUrl = apiUrl + ((apiUrl.Substring(apiUrl.Length - 1) != "/") ? "/" : "");
            P_companyKey = companyKey;
            P_secretKey = secretKey;
            P_agent = agentKey;
            P_timeOutSec = timeOutSec;
            P_theme = theme;
        }

        private readonly Encoding Charset = Encoding.UTF8;
        #endregion

        #region General Functions
        public string GetMD5Signature(string parameter)
        {
            using (MD5 md5Provider = MD5.Create())
            {
                var hash = md5Provider.ComputeHash(Encoding.UTF8.GetBytes(parameter));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hash)
                {
                    // 将字节转换成16进制表示的字符串，
                    sb.Append(b.ToString("x2"));
                }
                // 返回加密的字符串
                return sb.ToString();
            }
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

        private string ConvertClassObjectToJson(object Object)
        {
            try
            {
                string Json = JsonConvert.SerializeObject(Object);
                return Json;
            }
            catch (Exception ex)
            {
                p_ErrorMessage = ex.Message;
                throw;
            }
        }

        private bool PostJSON<T>(BaseRequest req, ref T resp, GetGameListRequest req2 = null)
        {
            try
            {
                if (req2 != null)
                {
                    p_TargetApiUrl = P_apiUrl + req2.method;
                }
                else
                {
                    p_TargetApiUrl = P_apiUrl + req.method;
                }
                p_Request = JsonConvert.SerializeObject(req);
                byte[] body = Charset.GetBytes(p_Request);
                //Dim hash As String = ComputeHash(body)
                string stringJson = ConvertClassObjectToJson(req);
                string signature = GetMD5Signature(stringJson + P_secretKey);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(p_TargetApiUrl);
                request.Method = "POST";
                request.Headers.Add("Authorization", signature);
                request.ContentType = "application/json";
                request.ContentLength = body.Length;
                request.Timeout = P_timeOutSec * 1000;


                using (Stream os = request.GetRequestStream())
                {
                    os.Write(body, 0, body.Length);
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
                StreamReader StreamReader = null;
                StreamReader = new StreamReader(errorResponse.GetResponseStream(), Charset);
                P_Response = StreamReader.ReadToEnd().Trim();
                if (ConvertJsonToClassObject<T>(P_Response, ref resp))
                {

                    var responseObj = resp as dynamic;
                    if (responseObj != null && responseObj.msg != null)
                    {
                        p_ErrorMessage = responseObj.msg.ToString();
                    }
                    else
                    {
                        p_ErrorMessage = "Unknown error response format";
                    }
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                p_ErrorMessage = ex.Message;
                return false;
            }
        }
        #endregion

        #region Api Functions
        public bool DoTransferDepositTask(string account, string orderId, float money, string timestamp)
        {
            DoTransferDepositTaskRequest req = new DoTransferDepositTaskRequest()
            {
                account = account,
                agent = P_agent,
                orderId = orderId,
                money = money,
                timestamp = timestamp
            };

            try
            {
                if (PostJSON<DoTransferDepositTaskResponse>(req, ref p_DoTransferDepositTaskResponse))
                {
                    if (p_DoTransferDepositTaskResponse != null && p_DoTransferDepositTaskResponse.code == 0)
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

        public bool DoTransferWithdrawTask(string account, string orderId, float money, string timestamp)
        {
            DoTransferWithdrawTaskRequest req = new DoTransferWithdrawTaskRequest()
            {
                account = account,
                agent = P_agent,
                orderId = orderId,
                money = money,
                timestamp = timestamp
            };

            try
            {
                if (PostJSON<DoTransferWithdrawTaskResponse>(req, ref p_DoTransferWithdrawTaskResponse))
                {
                    if (p_DoTransferWithdrawTaskResponse != null && p_DoTransferWithdrawTaskResponse.code == 0)
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

        public bool GetGameList(string timestamp)
        {
            GetGameListRequest req = new GetGameListRequest()
            {
                agent = P_agent,
                timestamp = timestamp
            };

            try
            {
                if (PostJSON<GetGameListResponse>(req, ref p_GetGameListResponse, req))
                {
                    if (p_GetGameListResponse != null && p_GetGameListResponse.code == 0)
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

        public bool KickUser(string account)
        {
            KickUserRequest req = new KickUserRequest()
            {
                account = account,
                agentId = P_agent
            };

            try
            {
                if (PostJSON<KickUserResponse>(req, ref p_KickUserResponse))
                {
                    if (p_KickUserResponse != null && p_KickUserResponse.code == 0)
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

        public bool Login(string account, string ip, string token, string nickName, string timestamp, string languageType, int gameId = 2635)
        {
            LoginRequest req = new LoginRequest()
            {
                account = account,
                gameId = gameId,
                //ip = ip,
                ip = "1.1.1.1",
                agent = P_agent,
                platform = 2,
                theme = P_theme,
                token = token,
                nickName = nickName,
                timestamp = timestamp,
                languageType = languageType
            };

            try
            {
                if (PostJSON<LoginResponse>(req, ref p_LoginResponse))
                {
                    if (p_LoginResponse != null && p_LoginResponse.code == 0)
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

        public bool QueryTransferOrder(string account, string orderId, string timestamp)
        {
            QueryTransferOrderRequest req = new QueryTransferOrderRequest()
            {
                account = account,
                agent = P_agent,
                orderId = orderId,
                timestamp = timestamp
            };

            try
            {
                if (PostJSON<QueryTransferOrderResponse>(req, ref p_QueryTransferOrderResponse))
                {
                    if (p_QueryTransferOrderResponse != null && p_QueryTransferOrderResponse.code == 0)
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

        public bool QueryUserScore(string account, string timestamp)
        {
            QueryUserScoreRequest req = new QueryUserScoreRequest()
            {
                account = account,
                agent = P_agent,
                timestamp = timestamp

            };

            try
            {
                if (PostJSON<QueryUserScoreResponse>(req, ref p_QueryUserScoreResponse))
                {
                    if (p_QueryUserScoreResponse != null && p_QueryUserScoreResponse.code == 0)
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

        public bool TakeBetLogs(string startTime, string endTime, int size, int page, string timestamp)
        {
            TakeBetLogsRequest req = new TakeBetLogsRequest()
            {
                agent = P_agent,
                startTime = startTime,
                endTime = endTime,
                size = size,
                page = page,
                timestamp = timestamp
            };

            try
            {
                if (PostJSON<TakeBetLogsResponse>(req, ref p_TakeBetLogsResponse))
                {
                    if (p_TakeBetLogsResponse != null && p_TakeBetLogsResponse.code == 0)
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
            throw new NotImplementedException();
        }
    }

    #region models
    public class BaseRequest
    {


        [JsonIgnore]
        internal string method { get; set; }
    }

    public class BaseResponse
    {
        public int code { get; set; }
        public string timestamp { get; set; }
        public string msg { get; set; }
    }

    public class DoTransferDepositTaskRequest : BaseRequest
    {
        public string account { get; set; }
        public DoTransferDepositTaskRequest()
        {
            method = "doTransferDepositTask";
        }
        public string orderId { get; set; }
        public float money { get; set; }
        public string timestamp { get; set; }
        public string agent { get; set; }
    }

    public class DoTransferDepositTaskResponse : BaseResponse
    {
        public DoTransferDepositTaskResult data { get; set; }
    }

    public class DoTransferDepositTaskResult
    {
        public float money { get; set; }
        public string orderId { get; set; }
    }

    public class DoTransferWithdrawTaskRequest : BaseRequest
    {
        public string account { get; set; }
        public DoTransferWithdrawTaskRequest()
        {
            method = "doTransferWithdrawTask";
        }
        public string orderId { get; set; }
        public float money { get; set; }
        public string timestamp { get; set; }
        public string agent { get; set; }
    }

    public class DoTransferWithdrawTaskResponse : BaseResponse
    {
        public DoTransferWithdrawTaskResult data { get; set; }
    }

    public class DoTransferWithdrawTaskResult
    {
        public float money { get; set; }
        public string orderId { get; set; }
    }

    public class GetGameListRequest : BaseRequest
    {
        [JsonIgnore]
        public string method { get; set; }
        public GetGameListRequest()
        {
            method = "getGameList";
        }
        public string agent { get; set; }
        public string timestamp { get; set; }

    }

    public class GetGameListResponse : BaseResponse
    {
        public GetGameListResult data { get; set; }
    }

    public class GetGameListResult
    {
        public long agent { get; set; }
        public List<Games> games { get; set; }
    }

    public class Games
    {
        public long gameId { get; set; }
        public string gameName { get; set; }
        public int state { get; set; }
        public string gameType { get; set; }
    }

    public class KickUserRequest : BaseRequest
    {
        public string account { get; set; }
        public string agentId { get; set; }

        public KickUserRequest()
        {
            method = "kickUser";
        }

    }

    public class KickUserResponse : BaseResponse
    {

    }

    public class LoginRequest : BaseRequest
    {
        public LoginRequest()
        {
            method = "login";
        }
        public int gameId { get; set; }
        public string orderId { get; set; }
        public float money { get; set; }
        public string ip { get; set; }
        public string agent { get; set; }
        public int platform { get; set; }
        public string appUrl { get; set; }
        public string exitUrl { get; set; }
        public string theme { get; set; }
        public string p1 { get; set; }
        public string p2 { get; set; }
        public string token { get; set; }
        public string nickName { get; set; }
        public string timestamp { get; set; }
        public string languageType { get; set; }
        public string account { get; set; }

    }

    public class LoginResponse : BaseResponse
    {
        public LoginResult data { get; set; }
    }

    public class LoginResult
    {
        public Uri url { get; set; }
    }

    public class QueryTransferOrderRequest : BaseRequest
    {
        public QueryTransferOrderRequest()
        {
            method = "queryTransfeOrder";
        }
        public string orderId { get; set; }
        public string timestamp { get; set; }
        public string agent { get; set; }
        public string account { get; set; }
    }

    public class QueryTransferOrderResponse : BaseResponse
    {
        public QueryTransferOrderResult data { get; set; }
    }

    public class QueryTransferOrderResult
    {
        public float money { get; set; }
        public string orderId { get; set; }
    }

    public class QueryUserScoreRequest : BaseRequest
    {
        public QueryUserScoreRequest()
        {
            method = "queryUserScore";
        }
        public string agent { get; set; }
        public string timestamp { get; set; }
        public string account { get; set; }
    }

    public class QueryUserScoreResponse : BaseResponse
    {
        public QueryUserScoreResult data { get; set; }
    }

    public class QueryUserScoreResult
    {
        public float money { get; set; }
    }

    public class TakeBetLogsRequest : BaseRequest
    {
        public TakeBetLogsRequest()
        {
            method = "takeBetLogs";
        }
        public string startTime { get; set; }
        public string endTime { get; set; }
        public int size { get; set; }
        public int page { get; set; }
        public string timestamp { get; set; }
        public string agent { get; set; }
    }

    public class TakeBetLogsResponse : BaseResponse
    {
        public TakeBetLogsResult data { get; set; }
    }

    public class TakeBetLogsResult
    {
        public int total { get; set; }
        public List<Bets> bets { get; set; }
    }

    public class Bets
    {
        public long gameId { get; set; }
        public string account { get; set; }
        public string accountId { get; set; }
        public string platform { get; set; }
        public string roundId { get; set; }
        public int gameResult { get; set; }
        public string mainRoundId { get; set; }
        public decimal bet { get; set; }
        public decimal validBet { get; set; }
        public decimal win { get; set; }
        public decimal lose { get; set; }
        public decimal fee { get; set; }
        public decimal enterMoney { get; set; }
        public DateTime createTime { get; set; }
        public DateTime roundBeginTime { get; set; }
        public DateTime roundEndTime { get; set; }
        public string ip { get; set; }

    }

    #endregion

}