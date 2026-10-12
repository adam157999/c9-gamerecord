using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using static GameRecord.Gamess.SAGAPI.SAGAMINGAPI;

namespace GameRecord.Gamess.JiLi
{
    public class JiLi : IDisposable
    {
        #region Declaration
        private string P_apiUrl { get; set; }
        private string P_agentId { get; set; }
        private string P_agentKey { get; set; }
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

        public CreateMemberResponse CreateMemberResponse { get { return p_CreateMemberResponse; } }
        public ExchangeTransferByAgentIdResponse ExchangeTransferByAgentIdResponse { get { return p_ExchangeTransferByAgentIdResponse; } }
        public GetMemberInfoResponse GetMemberInfoResponse { get { return p_GetMemberInfoResponse; } }
        public KickMemberResponse KickMemberResponse { get { return p_KickMemberResponse; } }
        public KickMemberAllResponse KickAllMemberResponse { get { return p_KickMemberAllResponse; } }
        public LoginResponse LoginResponse { get { return p_LoginResponse; } }
        public LoginWithoutRedirectResponse LoginWithoutRedirectResponse { get { return p_LoginWithoutRedirectResponse; } }
        public GetBetRecordByTimeResponse GetBetRecordByTimeResponse { get { return p_GetBetRecordByTimeResponse; } }
        public GetGameListResponse GetGameListResponse { get { return p_GetGameListResponse; } }
        public GetGameListDtoResponse GetGameListDtoResponse { get { return p_GetGameListDtoResponse; } }

        private CreateMemberResponse p_CreateMemberResponse;
        private ExchangeTransferByAgentIdResponse p_ExchangeTransferByAgentIdResponse;
        private GetMemberInfoResponse p_GetMemberInfoResponse;
        private KickMemberResponse p_KickMemberResponse;
        private KickMemberAllResponse p_KickMemberAllResponse;
        private LoginResponse p_LoginResponse;
        private LoginWithoutRedirectResponse p_LoginWithoutRedirectResponse;
        private GetBetRecordByTimeResponse p_GetBetRecordByTimeResponse;
        private GetGameListResponse p_GetGameListResponse;
        private GetGameListDtoResponse p_GetGameListDtoResponse;
        public JiLi(string apiUrl, string agentId, string agentKey, int timeOutSec = 30)
        {
            P_apiUrl = apiUrl + ((apiUrl.Substring(apiUrl.Length - 1) != "/") ? "/" : "");
            P_agentId = agentId;
            P_agentKey = agentKey;
            P_timeOutSec = timeOutSec;
        }

        private readonly Encoding Charset = Encoding.UTF8;
        #endregion

        #region General Functions

        private string RandomStringGenerator(int length, bool lowerCase = false)
        {
            var builder = new StringBuilder(length);

            Random _random = new Random();
            char offset = lowerCase ? 'a' : 'A';
            const int lettersOffset = 26;

            for (var i = 0; i < length; i++)
            {
                var @char = (char)_random.Next(offset, offset + lettersOffset);
                builder.Append(@char);
            }

            return lowerCase ? builder.ToString().ToLower() : builder.ToString();
        }

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

        //private string MD5Hashing(string input)
        //{
        //	using (MD5 md5 = MD5.Create())
        //	{
        //		byte[] inputBytes = Encoding.ASCII.GetBytes(input);
        //		byte[] hashBytes = md5.ComputeHash(inputBytes);
        //		StringBuilder sb = new StringBuilder();
        //		for (int i = 0; i < hashBytes.Length; i++)
        //		{
        //			sb.Append(hashBytes[i].ToString("X2"));
        //		}
        //		return sb.ToString();
        //	}
        //}

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

        private bool GetJSON<T>(BaseRequest req, ref T resp, string speicalGenerateParameterKey = null)
        {
            try
            {
                req.AgentId = P_agentId;
                if (string.IsNullOrEmpty(speicalGenerateParameterKey))
                {
                    P_Parameters = req.GetParam();
                }
                else
                {
                    P_Parameters = speicalGenerateParameterKey;
                }
                P_Functions = req.Function();

                string KeyG = MD5Hashing(DateTime.UtcNow.AddHours(-4).ToString("yyMMd") + P_agentId + P_agentKey);
                string signature = RandomStringGenerator(6) + MD5Hashing(P_Parameters + KeyG) + RandomStringGenerator(6);
                p_TargetApiUrl = P_apiUrl + P_Functions + "?" + req.GetParam() + "&Key=" + signature;

                p_Request = p_TargetApiUrl;

                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(p_TargetApiUrl);
                request.Method = "GET";
                request.Timeout = P_timeOutSec * 1000;

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
                    p_ErrorMessage = "";
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
        public bool Login(string account, int gameId, string lang)
        {
            Login req = new Login() { Account = account, GameId = gameId, Lang = lang };

            try
            {
                if (GetJSON<LoginResponse>(req, ref p_LoginResponse))
                {
                    if (p_LoginResponse != null && p_LoginResponse.ErrorCode == 0)
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

        public bool LoginWithoutRedirect(string account, int gameId, string lang)
        {
            LoginWithoutRedirect req = new LoginWithoutRedirect() { Account = account, GameId = gameId, Lang = lang };

            try
            {
                if (GetJSON<LoginWithoutRedirectResponse>(req, ref p_LoginWithoutRedirectResponse))
                {
                    if (p_LoginWithoutRedirectResponse != null && p_LoginWithoutRedirectResponse.ErrorCode == 0)
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

        public bool CreateMember(string account)
        {
            CreateMember req = new CreateMember() { Account = account };

            try
            {
                if (GetJSON<CreateMemberResponse>(req, ref p_CreateMemberResponse))
                {
                    if (p_CreateMemberResponse != null && p_CreateMemberResponse.ErrorCode == 0)
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

        public bool ExchangeTransferByAgentId(string account, string transactionId, decimal amount, byte transferType)
        {
            ExchangeTransferByAgentId req = new ExchangeTransferByAgentId()
            {
                Account = account,
                TransactionId = transactionId,
                Amount = amount,
                TransferType = transferType
            };

            try
            {
                if (GetJSON<ExchangeTransferByAgentIdResponse>(req, ref p_ExchangeTransferByAgentIdResponse))
                {
                    if (p_ExchangeTransferByAgentIdResponse != null && p_ExchangeTransferByAgentIdResponse.ErrorCode == 0)
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

        public bool GetMemberInfo(string account)
        {
            GetMemberInfo req = new GetMemberInfo()
            {
                Account = account
            };

            try
            {
                if (GetJSON<GetMemberInfoResponse>(req, ref p_GetMemberInfoResponse))
                {
                    if (p_GetMemberInfoResponse != null && p_GetMemberInfoResponse.ErrorCode == 0)
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

        public bool KickMember(string account)
        {
            KickMember req = new KickMember()
            {
                Account = account
            };

            try
            {
                if (GetJSON<KickMemberResponse>(req, ref p_KickMemberResponse))
                {
                    if (p_KickMemberResponse != null && p_KickMemberResponse.ErrorCode == 0)
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

        public bool KickMemberAll(int gameId)
        {
            KickMemberAll req = new KickMemberAll()
            {
                GameId = gameId
            };

            try
            {
                if (GetJSON<KickMemberAllResponse>(req, ref p_KickMemberAllResponse))
                {
                    if (p_KickMemberAllResponse != null && p_KickMemberAllResponse.ErrorCode == 0)
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

        public bool GetBetRecordByTime(DateTime startTime, DateTime endTime, int page, int pageLimit, bool filterAgent, int? gameId = null)
        {
            GetBetRecordByTime req = new GetBetRecordByTime();
            if (gameId == null)
            {
                req = new GetBetRecordByTime()
                {
                    AgentId = P_agentId,
                    StartTime = startTime,
                    EndTime = endTime,
                    Page = page,
                    PageLimit = pageLimit,
                    FilterAgent = filterAgent

                };
            }
            else
            {
                req = new GetBetRecordByTime()
                {
                    AgentId = P_agentId,
                    StartTime = startTime,
                    EndTime = endTime,
                    Page = page,
                    GameId = gameId,
                    PageLimit = pageLimit,
                    FilterAgent = filterAgent

                };
            }

            try
            {
                if (GetJSON<GetBetRecordByTimeResponse>(req, ref p_GetBetRecordByTimeResponse, req.GenerateKeyParam()))
                {
                    if (p_GetBetRecordByTimeResponse != null && p_GetBetRecordByTimeResponse.ErrorCode == 0)
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

        public bool GetGameList()
        {
            GetGameRequest req = new GetGameRequest()
            {
                AgentId = P_agentId
            };

            try
            {
                if (GetJSON<GetGameListResponse>(req, ref p_GetGameListResponse))
                {
                    if (p_GetGameListResponse != null && p_GetGameListResponse.ErrorCode == 0)
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
    public abstract class BaseRequest
    {

        public string AgentId { get; set; }
        public string Key { get; set; }
        public abstract string Function();
        public abstract string GetParam();
    }

    public class BaseResponse
    {
        public int ErrorCode { get; set; }
        public string Message { get; set; }
    }

    public class CreateMember : BaseRequest
    {

        public string Account { get; set; }
        public override string Function()
        {
            string functionName = "CreateMember";
            return functionName;
        }
        public override string GetParam()
        {
            string rawData = $"Account={Account}&AgentId={AgentId}";
            return rawData;
        }
    }

    public class GetGameRequest : BaseRequest
    {
        public override string Function()
        {
            string functionName = "GetGameList";
            return functionName;
        }
        public override string GetParam()
        {
            string rawData = $"AgentId={AgentId}";
            return rawData;
        }
    }

    public class CreateMemberResponse : BaseResponse
    {
        public object Data;
    }

    public class ExchangeTransferByAgentId : BaseRequest
    {
        public string Account { get; set; }
        public string TransactionId { get; set; }
        public decimal Amount { get; set; }
        public byte TransferType { get; set; }
        public override string Function()
        {
            string functionName = "ExchangeTransferByAgentId";
            return functionName;
        }
        public override string GetParam()
        {
            string rawData = $"Account={Account}&TransactionId={TransactionId}&Amount={Amount}&TransferType={TransferType}&AgentId={AgentId}";
            return rawData;
        }
    }

    public class ExchangeTransferByAgentIdResponse : BaseResponse
    {
        public string TransactionId { get; set; }
        public float CoinBefore { get; set; }
        public float CoinAfter { get; set; }
        public float CurrencyBefore { get; set; }
        public float CurrencyAfter { get; set; }
        public int Status { get; set; }
        public object Data { get; set; }
    }

    public class GetBetRecordByTime : BaseRequest
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int Page { get; set; }
        public int PageLimit { get; set; }
        public int? GameId { get; set; }
        public bool FilterAgent { get; set; }
        public override string Function()
        {
            string functionName = "GetBetRecordByTime";
            return functionName;
        }

        //This case GetParam Will Be Ignored.
        public override string GetParam()
        {
            string rawData = $"StartTime={StartTime.ToString("yyyy-MM-ddTHH:mm:ss")}&EndTime={EndTime.ToString("yyyy-MM-ddTHH:mm:ss")}&Page={Page}&PageLimit={PageLimit}&FilterAgent={FilterAgent}&AgentId={AgentId}";
            return rawData;
        }

        public string GenerateKeyParam()
        {
            string rawData = $"StartTime={StartTime.ToString("yyyy-MM-ddTHH:mm:ss")}&EndTime={EndTime.ToString("yyyy-MM-ddTHH:mm:ss")}&Page={Page}&PageLimit={PageLimit}&AgentId={AgentId}";
            return rawData;
        }
    }


    public class GetBetRecordByTimeResponse : BaseResponse
    {
        public ResponseData Data { get; set; }

    }

    public class ResponseData
    {
        public List<Result> Result { get; set; }
        public Pagination Pagination { get; set; }
    }

    public class Result
    {
        public string Account { get; set; }
        public long WagersId { get; set; }
        public int GameId { get; set; }
        public DateTime WagersTime { get; set; }
        public decimal BetAmount { get; set; }
        public DateTime PayoffTime { get; set; }
        public decimal PayoffAmount { get; set; }
        public int Status { get; set; }
        public DateTime SettlementTime { get; set; }
        public int GameCategoryId { get; set; }
        public int Type { get; set; }
        public string AgentId { get; set; }
        public long RoundIndex { get; set; }

    }

    public class Pagination
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageLimit { get; set; }
        public int TotalNumber { get; set; }
    }

    public class GetMemberInfo : BaseRequest
    {
        public string Account { get; set; }
        public override string Function()
        {
            string functionName = "GetMemberInfo";
            return functionName;
        }
        public override string GetParam()
        {
            string rawData = $"Accounts={Account}&AgentId={AgentId}";
            return rawData;
        }
    }

    public class GetMemberInfoResponse : BaseResponse
    {
        public List<InfoResponseData> Data { get; set; }
    }

    public class InfoResponseData
    {
        public string Account { get; set; }
        public decimal Balance { get; set; }
        public int Status { get; set; }
    }

    public class KickMember : BaseRequest
    {
        public string Account { get; set; }
        public override string Function()
        {
            string functionName = "KickMember";
            return functionName;
        }
        public override string GetParam()
        {
            string rawData = $"Account={Account}&AgentId={AgentId}";
            return rawData;
        }
    }

    public class KickMemberResponse : BaseResponse
    {
        public object Data { get; set; }
    }

    public class KickMemberAll : BaseRequest
    {
        public int GameId { get; set; }
        public override string Function()
        {
            string functionName = "KickMemberAll";
            return functionName;
        }
        public override string GetParam()
        {
            string rawData = $"GameId={GameId}&AgentId={AgentId}";
            return rawData;
        }
    }

    public class KickMemberAllResponse : BaseResponse
    {
        public object Data { get; set; }
    }

    public class Login : BaseRequest
    {
        public int GameId { get; set; }
        public string Account { get; set; }
        public string Lang { get; set; }
        public int LineCode { get; set; }
        public string HomeUrl { get; set; }
        public string platform { get; set; }
        public override string Function()
        {
            string functionName = "Login";
            return functionName;
        }
        public override string GetParam()
        {
            string rawData = $"AgentId={AgentId}&GameId={GameId}&Account={Account}&Lang={Lang}";
            return rawData;
        }
    }

    public class LoginResponse : BaseResponse
    {
        public object Data { get; set; }
    }

    public class LoginWithoutRedirect : BaseRequest
    {
        public int GameId { get; set; }
        public string Account { get; set; }
        public string Lang { get; set; }
        public override string Function()
        {
            string functionName = "LoginWithoutRedirect";
            return functionName;
        }
        public override string GetParam()
        {
            //string rawData = $"AgentId={AgentId}&GameId=80&Account={Account}&Lang={Lang}";
            string rawData = $"Account={Account}&GameId={GameId}&Lang={Lang}&AgentId={AgentId}";
            return rawData;
        }
    }

    public class LoginWithoutRedirectResponse : BaseResponse
    {
        public string Data { get; set; }
    }

    public class GetGameListResponse : BaseResponse
    {
        public List<GetGameListItem> Data { get; set; }
    }

    public class GetGameListDtoResponse : BaseResponse
    {
        public List<GetGameListDto> Data { get; set; }
    }

    public class GetGameListItem
    {
        public string GameId { get; set; }
        public GameName name { get; set; }
        public int GameCategoryId { get; set; }
        public bool JP { get; set; }
        public bool Freespin { get; set; }
    }

    public class GetGameListDto
    {
        public string GameId { get; set; }
        public string GameName { get; set; }
        public string GameNameCn { get; set; }
        public int GameCategoryId { get; set; }
        public bool JP { get; set; }

        public bool Freespin { get; set; }
    }

    public class GameName
    {
        [JsonProperty("zh-CN")]
        public string zhCN { get; set; }
        [JsonProperty("zh-TW")]
        public string zhTW { get; set; }
        [JsonProperty("en-US")]
        public string enUS { get; set; }
    }
    #endregion
}