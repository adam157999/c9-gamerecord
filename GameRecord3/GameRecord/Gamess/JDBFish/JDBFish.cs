using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace GameRecord.Gamess.JDBFish
{
    public class JDBFish : IDisposable
    {
        #region Declaration
        private string P_apiUrl { get; set; }
        private string P_iv { get; set; }
        private string P_key { get; set; }
        private string P_manager { get; set; }
        private string P_masterAgent { get; set; }
        private string P_agent { get; set; }
        private int P_timeOutSec { get; set; }
        private string P_dc { get; set; }
        private int[] P_gType;
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

        public Action12CreatePlayerResponse Action12CreatePlayerResponse { get { return p_Action12CreatePlayerResponse; } }
        public GetDetailReportResponse GetDetailReportResponse { get { return p_GetDetailReportResponse; } }
        public Action17KickPlayerResposne Action17KickPlayerResposne { get { return p_Action17KickPlayerResposne; } }
        public Action15QueryPlayerResponse Action15QueryPlayerResponse { get { return p_Action15QueryPlayerResponse; } }
        public TokenResponse TokenResponse { get { return p_TokenResponse; } }
        public Action19WithdrawDepositeResponse Action19WithdrawDepositeResponse { get { return p_Action19WithdrawDepositeResponse; } }


        private Action12CreatePlayerResponse p_Action12CreatePlayerResponse;
        private GetDetailReportResponse p_GetDetailReportResponse;
        private Action17KickPlayerResposne p_Action17KickPlayerResposne;
        private Action15QueryPlayerResponse p_Action15QueryPlayerResponse;
        private TokenResponse p_TokenResponse;
        private Action19WithdrawDepositeResponse p_Action19WithdrawDepositeResponse;


        public JDBFish(string apiUrl, string IV, string key, string manager, string masterAgent, string agent, string dc, int timeOutSec = 30)
        {
            P_apiUrl = apiUrl + ((apiUrl.Substring(apiUrl.Length - 1) != "/") ? "/" : "");
            P_iv = IV;
            P_key = key;
            P_manager = manager;
            P_masterAgent = masterAgent;
            P_agent = agent;
            P_dc = dc;
            P_timeOutSec = timeOutSec;
        }

        private readonly Encoding Charset = Encoding.UTF8;
        #endregion

        #region General Functions
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

        private static string encrypt(string data, string key, string iv)
        {
            byte[] encrypted;
            using (SymmetricAlgorithm des = Rijndael.Create())
            {
                byte[] inputByteArray = Encoding.UTF8.GetBytes(data);
                des.Key = Encoding.UTF8.GetBytes(key);
                des.IV = Encoding.UTF8.GetBytes(iv);
                var encrypter = des.CreateEncryptor();
                encrypted = encrypter.TransformFinalBlock(inputByteArray, 0, inputByteArray.Length);
            }
            var URL_Safe_AESEncrypt_String = Convert.ToBase64String(encrypted)
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');
            return URL_Safe_AESEncrypt_String;
        }
        //private bool AssignReportJsonToClassObject<T>(int gType, dynamic obj, ref GetDetailReportResponse TargetClass)
        //{
        //	try
        //	{
        //		string jsonObj = Convert.ToString(obj);
        //		switch (gType)
        //		{
        //			case 0:
        //			case 66:
        //				TargetClass.data = JsonConvert.DeserializeObject<List<TigetSlot>>(jsonObj);
        //				break;
        //			case 7:
        //			case 67:
        //				TargetClass.data = JsonConvert.DeserializeObject<List<FishSlot>>(jsonObj);
        //				break;
        //			case 9:
        //				TargetClass.data = JsonConvert.DeserializeObject<List<StreetSlot>>(jsonObj);
        //				break;
        //			case 12:
        //				TargetClass.data = JsonConvert.DeserializeObject<List<ElectronicTicketSlot>>(jsonObj);
        //				break;
        //			case 18:
        //				TargetClass.data = JsonConvert.DeserializeObject<List<ChessCardSlot>>(jsonObj);
        //				break;
        //			case 22:
        //				TargetClass.data = JsonConvert.DeserializeObject<List<SpribeStreetSlot>>(jsonObj);
        //				break;
        //			case 26:
        //			case 27:
        //			case 30:
        //			case 31:
        //			case 32:
        //				TargetClass.data = JsonConvert.DeserializeObject<List<TpFcBet>>(jsonObj);
        //				break;
        //			default:
        //				TargetClass.data = null;
        //				break;
        //		}
        //		return true;
        //	}
        //	catch (Exception ex)
        //	{
        //		p_ErrorMessage = ex.Message;
        //		return false;
        //	}
        //}

        private bool PostJSON<T>(BaseRequest req, ref T resp)
        {
            try
            {
                //Attached brand id and api key for each call
                p_Request = JsonConvert.SerializeObject(req);
                string x = encrypt(p_Request, P_key, P_iv);
                byte[] body = Encoding.ASCII.GetBytes(p_Request);
                //Dim hash As String = ComputeHash(body)

                p_TargetApiUrl = P_apiUrl + $"?dc={P_dc}&x={x}";

                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(p_TargetApiUrl);
                request.Method = "POST";
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
                    StreamReader StreamReader;
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
                StreamReader StreamReader;
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
        public bool WithdrawDeposite(string uid, string serialNo, string allCashOutFlag, double amount, string remark)
        {

            Action19WithdrawDepositeRequest req = new Action19WithdrawDepositeRequest()
            {
                uid = uid,
                parent = P_agent,
                serialNo = serialNo.Replace("-", ""),
                allCashOutFlag = allCashOutFlag,
                amount = amount,
                remark = remark
            };

            try
            {
                if (PostJSON<Action19WithdrawDepositeResponse>(req, ref p_Action19WithdrawDepositeResponse))
                {
                    if (p_Action19WithdrawDepositeResponse != null && string.IsNullOrEmpty(p_ErrorMessage) && p_Action19WithdrawDepositeResponse.status == "0000")
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

        public bool QueryPlayer(string uid)
        {
            Action15QueryPlayerRequest req = new Action15QueryPlayerRequest()
            {
                uid = uid,
                parent = P_agent
            };

            try
            {
                if (PostJSON<Action15QueryPlayerResponse>(req, ref p_Action15QueryPlayerResponse))
                {
                    if (p_Action15QueryPlayerResponse != null && string.IsNullOrEmpty(p_ErrorMessage) && p_Action15QueryPlayerResponse.status == "0000")
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

        public bool KickPlayer(string uid)
        {
            Action17KickPlayerRequest req = new Action17KickPlayerRequest()
            {
                uid = uid,
                parent = P_agent
            };

            try
            {
                if (PostJSON<Action17KickPlayerResposne>(req, ref p_Action17KickPlayerResposne))
                {
                    if (p_Action17KickPlayerResposne != null && string.IsNullOrEmpty(p_ErrorMessage) && p_Action17KickPlayerResposne.status == "0000")
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

        public bool CreatePlayer(string uid, string name, int creditAllocated)
        {
            Action12CreatePlayerRequest req = new Action12CreatePlayerRequest()
            {
                uid = uid,
                name = name,
                parent = P_agent,
                credit_allocated = creditAllocated
            };

            try
            {
                if (PostJSON<Action12CreatePlayerResponse>(req, ref p_Action12CreatePlayerResponse))
                {
                    if (Action12CreatePlayerResponse != null && string.IsNullOrEmpty(p_ErrorMessage) && p_Action12CreatePlayerResponse.status == "0000")
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

        public bool Token(string uid, string lang, string gtype = "", string mType = "")
        {
            Action11TokenRequest req = new Action11TokenRequest() { };

            if (!string.IsNullOrEmpty(gtype))
            {
                req = new Action11TokenRequest()
                {
                    uid = uid,
                    lang = lang,
                    gType = gtype,
                    mType = mType,
                    windowMode = "1",
                    isAPP = false,
                    mute = 0,
                    isShowDollarSign = false
                };

            }
            else
            {
                req = new Action11TokenRequest()
                {
                    uid = uid,
                    lang = lang,
                    //gType = "0",
                    //mType = mType,
                    windowMode = "1",
                    isAPP = false,
                    mute = 0,
                    isShowDollarSign = false
                };
            }

            try
            {
                if (PostJSON<TokenResponse>(req, ref p_TokenResponse))
                {
                    if (TokenResponse != null && string.IsNullOrEmpty(p_ErrorMessage) && TokenResponse.status == "0000")
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

        public bool QueryGameList (string lang)
        {
            Action49TokenRequest req = new Action49TokenRequest() {
                parent = P_agent,
                lang = lang
            };
            try
            {
                if (PostJSON<TokenResponse>(req, ref p_TokenResponse))
                {
                    if (TokenResponse != null && string.IsNullOrEmpty(p_ErrorMessage) && TokenResponse.status == "0000")
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

        public bool TrialToken(string lang, string gType, string mType, string windowMode, bool isApp, string lobbyURL,
                int mute, bool isShowDollarSign)
        {
            Action47TrialTokenRequest req = new Action47TrialTokenRequest()
            {
                lang = lang,
                gType = gType,
                mType = mType,
                windowMode = windowMode,
                isAPP = isApp,
                lobbyURL = lobbyURL,
                mute = mute,
                isShowDollarSign = isShowDollarSign
            };

            try
            {
                if (PostJSON<TokenResponse>(req, ref p_TokenResponse))
                {
                    if (TokenResponse != null && string.IsNullOrEmpty(p_ErrorMessage) && p_TokenResponse.status == "0000")
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

        public bool GetDetailReport(string startTime, string endTime)
        {
            Action29GetDetailReportRequest req = new Action29GetDetailReportRequest()
            {
                //gTypes = gTypes,
                parent = P_agent,
                starttime = startTime,
                endtime = endTime
            };

            try
            {
                //bool flag = false;
                //P_gType = gTypes;
                if (PostJSON<GetDetailReportResponse>(req, ref p_GetDetailReportResponse))
                {
                    if (GetDetailReportResponse != null && string.IsNullOrEmpty(p_ErrorMessage) && p_GetDetailReportResponse.status == "0000")
                    {
                        return true;
                        //for (int i = 0; i < P_gType.Length; i++)
                        //{
                        //	if (AssignReportJsonToClassObject<GetDetailReportResponse>(P_gType[i], p_GetDetailReportResponse.data, ref p_GetDetailReportResponse))
                        //	{
                        //		flag = true;
                        //	}
                        //	else
                        //	{
                        //		flag = false;
                        //	}
                        //}
                        //return flag;
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

        public bool GetHistoryDetailReport(string startTime, string endTime)
        {
            Action64GetDetailReportRequest req = new Action64GetDetailReportRequest()
            {
                //gTypes = gTypes,
                parent = P_agent,
                starttime = startTime,
                endtime = endTime
            };

            try
            {
                //bool flag = false;
                //P_gType = gTypes;
                if (PostJSON<GetDetailReportResponse>(req, ref p_GetDetailReportResponse))
                {
                    if (GetDetailReportResponse != null && string.IsNullOrEmpty(p_ErrorMessage) && p_GetDetailReportResponse.status == "0000")
                    {
                        return true;
                        //for (int i = 0; i < P_gType.Length; i++)
                        //{
                        //	if (AssignReportJsonToClassObject<GetDetailReportResponse>(P_gType[i], p_GetDetailReportResponse.data, ref p_GetDetailReportResponse))
                        //	{
                        //		flag = true;
                        //	}
                        //	else
                        //	{
                        //		flag = false;
                        //	}
                        //}
                        //return flag;
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
        public int action { get; set; }
        public long ts { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        public string uid { get; set; }
    }

    public class BaseResponse
    {
        public string status { get; set; }
        public string err_text { get; set; }
    }

    public class BaseReportResponse
    {
        public long seqNo { get; set; }
        public string playerId { get; set; }
        public int gType { get; set; }
        public int mType { get; set; }
        public string gameDate { get; set; }
        public double bet { get; set; }
        public double win { get; set; }
        public double total { get; set; }
        public string currency { get; set; }
        public string lastModifyTime { get; set; }
    }

    public class Action12CreatePlayerRequest : BaseRequest
    {
        public Action12CreatePlayerRequest()
        {
            action = 12;
        }
        public string parent { get; set; }
        public string name { get; set; }
        public int credit_allocated { get; set; }
    }

    public class Action12CreatePlayerResponse : BaseResponse
    {

    }

    public class GetDetailReportRequest : BaseRequest
    {
        public string parent { get; set; }
        public int[] gTypes { get; set; }
        public string starttime { get; set; }
        public string endtime { get; set; }
    }
    public class Action29GetDetailReportRequest : GetDetailReportRequest
    {
        public Action29GetDetailReportRequest()
        {
            action = 29;
        }
    }

    public class Action64GetDetailReportRequest : GetDetailReportRequest
    {
        public Action64GetDetailReportRequest()
        {
            action = 64;
        }
    }

    public class GetDetailReportResponse : BaseResponse
    {
        //public dynamic data { get; set; }
        public List<ReportResponse> data { get; set; }
    }

    public class ReportResponse
    {
        //public long seqNo { get; set; }
        //public long historyId { get; set; }
        public string historyId { get; set; }

        public string playerId { get; set; }
        public int gType { get; set; }
        public int mtype { get; set; }
        public string gameDate { get; set; }
        public decimal bet { get; set; }
        public decimal win { get; set; }
        public decimal total { get; set; }
        public string currency { get; set; }
        public string lastModifyTime { get; set; }

        public decimal? gambleBet { get; set; }
        public decimal? jackpot { get; set; }
        public decimal? jackpotContribute { get; set; }
        public decimal? denom { get; set; }
        public string playerIp { get; set; }
        public string clientType { get; set; }
        public int? hasFreegame { get; set; }
        public int? hasGamble { get; set; }
        public int? systemTakeWin { get; set; }

        public int? roomType { get; set; }
        public decimal? beforeBalance { get; set; }
        public decimal? afterBalance { get; set; }

        public int? hasBonusGame { get; set; }

        public long? roundSeqNo { get; set; }
        public decimal? tax { get; set; }
        public decimal? validBet { get; set; }
    }

    public class TigetSlot : BaseReportResponse
    {
        public double gambleBet { get; set; }
        public double jackpot { get; set; }
        public double jackpotContribute { get; set; }
        public double denom { get; set; }
        public string playerIp { get; set; }
        public string clientType { get; set; }
        public int hasFreegame { get; set; }
        public int hasGamble { get; set; }
        public int systemTakeWin { get; set; }
    }

    public class FishSlot : BaseReportResponse
    {
        public int roomType { get; set; }
        public double denom { get; set; }
        public double beforeBalance { get; set; }
        public double afterBalance { get; set; }
        public string playerIp { get; set; }
        public string clientType { get; set; }
    }

    public class StreetSlot : BaseReportResponse
    {
        public double denom { get; set; }
        public string playerIp { get; set; }
        public string clientType { get; set; }
        public int hasBonusGame { get; set; }
        public int hasGamble { get; set; }
    }

    public class ElectronicTicketSlot : BaseReportResponse
    {
        public double denom { get; set; }
        public string playerIp { get; set; }
        public string clientType { get; set; }
        public int hasBonusGame { get; set; }
    }

    public class ChessCardSlot : BaseReportResponse
    {
        public long roundSeqNo { get; set; }
        public double tax { get; set; }
        public string playerIp { get; set; }
        public string clientType { get; set; }
        public double validBet { get; set; }
    }

    public class SpribeStreetSlot : BaseReportResponse
    {
        public string playerIp { get; set; }
        public string clientType { get; set; }
    }

    public class TpFcBet : BaseReportResponse
    {
    }

    public class Action17KickPlayerRequest : BaseRequest
    {
        public Action17KickPlayerRequest()
        {
            action = 17;
        }
        public string parent { get; set; }
    }

    public class Action17KickPlayerResposne : BaseResponse
    {

    }

    public class Action15QueryPlayerRequest : BaseRequest
    {
        public Action15QueryPlayerRequest()
        {
            action = 15;
        }
        public string parent { get; set; }
    }

    public class Action15QueryPlayerResponse : BaseResponse
    {
        public List<PlayerData> data { get; set; }
    }

    public class PlayerData
    {
        public string uid { get; set; }
        public decimal balance { get; set; }
        public string parent { get; set; }
        public string username { get; set; }
        public string currency { get; set; }
        public int lvl { get; set; }
        public int locked { get; set; }
        public int closed { get; set; }
        public int jackpotFlag { get; set; }

    }

    public class Action11TokenRequest : BaseRequest
    {
        public Action11TokenRequest()
        {
            action = 11;
        }
        public string lang { get; set; }
        public string gType { get; set; }
        public string mType { get; set; }
        public string remark { get; set; }
        public string windowMode { get; set; }
        public bool isAPP { get; set; }
        public string lobbyURL { get; set; }
        public int mute { get; set; }
        public string cardGameGroup { get; set; }
        public bool isShowDollarSign { get; set; }
    }

    public class Action49TokenRequest : BaseRequest
    {
        public Action49TokenRequest()
        {
            action = 49;
        }
        public string parent { get; set; }
        public string lang { get; set; }
    }


    public class Action47TrialTokenRequest : BaseRequest
    {
        public Action47TrialTokenRequest()
        {
            action = 47;
        }
        public string lang { get; set; }
        public string gType { get; set; }
        public string mType { get; set; }
        public string windowMode { get; set; }
        public bool isAPP { get; set; }
        public string lobbyURL { get; set; }
        public int mute { get; set; }
        public bool isShowDollarSign { get; set; }
    }

    public class TokenResponse : BaseResponse
    {
        public string path { get; set; }
    }

    public class Action19WithdrawDepositeRequest : BaseRequest
    {
        public Action19WithdrawDepositeRequest()
        {
            action = 19;
            ts = DateTime.Now.ToFileTime();
        }
        public string parent { get; set; }
        public string serialNo { get; set; }
        public string allCashOutFlag { get; set; }
        public double amount { get; set; }
        public string remark { get; set; }
    }

    public class Action19WithdrawDepositeResponse : BaseResponse
    {
        public double userBalance { get; set; }
        public double agentCashBalance { get; set; }
        public double amount { get; set; }
        public string serialNo { get; set; }
        public long pid { get; set; }
        public string payDate { get; set; }
    }
    #endregion
}