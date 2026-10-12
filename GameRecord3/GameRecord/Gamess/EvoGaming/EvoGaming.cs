#region  Imports 
using System.Xml;
using System.Xml.Serialization;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.XPath;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System;
using System.Linq;
using System.Web;
#endregion

namespace GameRecord.Gamess.EvoGaming
{
	public class EVOGM : IDisposable
	{
		Common _common = new Common();

		#region Properties Declaration
		private string p_hostName = "evogaming";
		private string p_merchantId = "";
		private string p_casinoKey = "qufrezcbnhsqypbx";
		private string p_apiToken = "5affb20d5b2e738d56ac503f22f6ed5c";
		private readonly string p_currency = "";
		private int p_timeOutSec;
		private string p_gameListHost = "";

		public EVOGM(string HostName, string MerchantId, string CasinoKey, string ApiToken, int TimeOutSec = 30, string Currency = "", string GameListHost = "")
		{
			this.p_hostName = HostName;
			this.p_merchantId = MerchantId;
			this.p_casinoKey = CasinoKey;
			this.p_apiToken = ApiToken;
			this.p_currency = Currency;
			this.p_timeOutSec = TimeOutSec;
			this.p_gameListHost = GameListHost;
		}

		public string request
		{
			get
			{
				return p_request;
			}
		}
		private string p_request;

		public string response
		{
			get
			{
				return p_response;
			}
		}
		private string p_response;


		public string errorMessage
		{
			get
			{
				return p_errorMessage;
			}
		}
		private string p_errorMessage;

		public string targetApiUrl
		{
			get
			{
				return p_targetApiUrl;
			}
		}
		private string p_targetApiUrl;

		public bool ParseError
		{
			get
			{
				return p_parseError;
			}
		}
		private bool p_parseError = false;

		public string parameter
		{
			get
			{
				return p_parameter;
			}
		}
		private string p_parameter;

		private readonly Encoding Charset = Encoding.UTF8;

		private UserAuthSuccessResponse p_UserAuthSuccessResponse;
		private UserAuthFailedResponse p_UserAuthFailedResponse;
		private GetBalanceSuccessResponse p_GetBalanceSuccessResponse;
		private ErrorResponse p_GetBalanceFailedResponse;
		private TransferSuccessResponse p_TransferSuccessResponse;
		private TransferFailedResponse p_TransferFailedResponse;
		private GetUserDetailSuccessResponse p_GetUserDetailSuccessResponse;
		private SingleErrorResponse p_GetUserDetailFailedResponse;
		private GetTransactionSuccessResponse p_GetTransactionSuccessResponse;
		private ErrorResponse p_GetTransactionFailedResponse;
		private CasinoDailyReport.CasinoDailyReportResponse p_CasinoDailyReportResponse;
		private CasinoGamesDetail.CasinoGamesDetailResponse p_CasinoGamesDetailResponse;
		private CasinoStreamGamesDetail.CasinoStreamGamesDetailResponse p_CasinoStreamGamesDetailResponse;
		private PlayerGameDetail.PlayerGameDetailResponse p_PlayerGameDetailResponse;
		private GameListResponse p_GameListResponse;

		public UserAuthSuccessResponse UserAuthSuccessResponse
		{
			get
			{
				return p_UserAuthSuccessResponse;
			}
		}

		public UserAuthFailedResponse UserAuthFailedResponse
		{
			get
			{
				return p_UserAuthFailedResponse;
			}
		}

		public GetBalanceSuccessResponse GetBalanceSuccessResponse
		{
			get
			{
				return p_GetBalanceSuccessResponse;
			}
		}

		public ErrorResponse GetBalanceFailedResponse
		{
			get
			{
				return p_GetBalanceFailedResponse;
			}
		}

		public TransferSuccessResponse TransferSuccessResponse
		{
			get
			{
				return p_TransferSuccessResponse;
			}
		}

		public TransferFailedResponse TransferFailedResponse
		{
			get
			{
				return p_TransferFailedResponse;
			}
		}

		public GetUserDetailSuccessResponse GetUserDetailSuccessResponse
		{
			get
			{
				return p_GetUserDetailSuccessResponse;
			}
		}

		public SingleErrorResponse GetUserDetailFailedResponse
		{
			get
			{
				return p_GetUserDetailFailedResponse;
			}
		}

		public GetTransactionSuccessResponse GetTransactionSuccessResponse
		{
			get
			{
				return p_GetTransactionSuccessResponse;
			}
		}

		public ErrorResponse GetTransactionFailedResponse
		{
			get
			{
				return p_GetTransactionFailedResponse;
			}
		}

		public CasinoDailyReport.CasinoDailyReportResponse CasinoDailyReportResponse
		{
			get
			{
				return p_CasinoDailyReportResponse;
			}
		}

		public CasinoGamesDetail.CasinoGamesDetailResponse CasinoGamesDetailResponse
		{
			get
			{
				return p_CasinoGamesDetailResponse;
			}
		}

		public CasinoStreamGamesDetail.CasinoStreamGamesDetailResponse CasinoStreamGamesDetailResponse
		{
			get
			{
				return p_CasinoStreamGamesDetailResponse;
			}
		}

		public PlayerGameDetail.PlayerGameDetailResponse PlayerGameDetailResponse
		{
			get
			{
				return p_PlayerGameDetailResponse;
			}
		}

		public GameListResponse GameListResponse
		{
			get { return p_GameListResponse; }
		}
		#endregion

		#region General Functions

		public string GenerateSerialNo()
		{
			int number = 0;
			string otp = DateTime.Now.ToString("yyyyMMddHHmmss");

			var rand = new Random();
			// The program will generate a number from 0 to 99999
			number = (int)Math.Floor(rand.NextDouble() * 99999) + 1;

			return otp + number.ToString();
		}

		private string GetAuthHeader()
		{
			string txt = p_casinoKey + ":" + p_apiToken;
			byte[] byt = System.Text.Encoding.UTF8.GetBytes(txt);
			return System.Convert.ToBase64String(byt);
		}

		public void LogError(string sMsg)
		{
			string path = HttpContext.Current.Server.MapPath(@"~\Logs\").ToString();
			DateTime dtNow = DateTime.Now;
			string date = string.Format("{0:yyyyMMdd}", dtNow);
			string time = string.Format("{0:HH:mm:ss.fff}", dtNow);
			path = path + "Log_" + date + ".txt";
			if (!File.Exists(path))
			{
				using (StreamWriter log = new StreamWriter(path))
				{
					log.WriteLine(time);
					log.WriteLine(sMsg);
					log.WriteLine("");
				}
			}
			else
			{
				using (StreamWriter log = File.AppendText(path))
				{
					log.WriteLine(time);
					log.WriteLine(sMsg);
					log.WriteLine("");
				}
			}
		}

		#region  JSON 
		private string DataSerializeJSON(ref object TargetClass)
		{
			return JsonConvert.SerializeObject(TargetClass, Newtonsoft.Json.Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
		}

		private bool PostJSON<T, E>(ref T resp, ref E respErr)
		{
			try
			{
				byte[] body = Charset.GetBytes(p_parameter);
				//Dim hash As String = ComputeHash(body)

				ServicePointManager.Expect100Continue = true;
				ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(p_targetApiUrl);
				request.Method = "POST";
				request.ContentType = "application/json";
				request.ContentLength = body.Length;
				request.Timeout = p_timeOutSec * 1000;

				using (Stream os = request.GetRequestStream())
				{
					os.Write(body, 0, body.Length);
				}

				using (HttpWebResponse objResponse = (HttpWebResponse)request.GetResponse())
				{
					if (objResponse.StatusCode == HttpStatusCode.OK)
					{
						StreamReader StreamReader = null;
						StreamReader = new StreamReader(objResponse.GetResponseStream(), Charset);

						string strJson = StreamReader.ReadToEnd().Trim();
						p_response = strJson;

						T oResponse = default(T);
						E oErr = default(E);
						if (ConvertJsonToClassObject<T>(strJson, ref oResponse))
						{
							resp = oResponse;
							return true;
						}
						else if (ConvertJsonToClassObject<E>(strJson, ref oErr))
						{
							respErr = oErr;
							return true;
						}
						else
						{
							return false;
						}
					}
					else
					{
						p_errorMessage = objResponse.StatusDescription;
						return false;
					}
				}
			}
			catch (WebException ex)
			{
				using (var stream = ex.Response.GetResponseStream())
				using (var reader = new StreamReader(stream))
				{
					p_response = reader.ReadToEnd();

					E oErr = default(E);
					if (ConvertJsonToClassObject<E>(p_response, ref oErr))
					{
						respErr = oErr;
						return true;
					}
					else
					{
						return false;
					}
				}
			}
			catch (Exception ex)
			{
				p_errorMessage = ex.Message;
				return false;
			}
		}

		public bool GetJson<T>(ref T resp)
		{
			try
			{
				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(p_targetApiUrl);
				request.ContentType = "application/json; encoding=utf-8";
				request.Method = "GET";
				request.Accept = "application/json; encoding=utf-8";
				request.Timeout = p_timeOutSec * 1000;
				request.Headers.Add("Authorization", "Basic " + GetAuthHeader());

				HttpWebResponse objResponse = (HttpWebResponse)request.GetResponse();

				if (objResponse.StatusCode == HttpStatusCode.OK)
				{
					StreamReader StreamReader = null;
					StreamReader = new StreamReader(objResponse.GetResponseStream());

					string strJson = StreamReader.ReadToEnd().Trim();
					p_response = strJson;

					T oResponse = default(T);
					if (ConvertJsonToClassObject<T>(strJson, ref oResponse))
					{
						resp = oResponse;
						return true;
					}
					return false;
				}
				else
				{
					p_errorMessage = objResponse.StatusDescription;
					return false;
				}

			}
			catch (Exception ex)
			{
				p_errorMessage = ex.Message;
				LogError("Error at: Evo/GetJson" + "\r\nError message: " + ex.Message.ToString());
				return false;
			}

		}

		public string DebugGetJson<T>(ref T resp)
		{
			try
			{
				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(p_targetApiUrl);
				request.ContentType = "application/json; encoding=utf-8";
				request.Method = "GET";
				request.Accept = "application/json; encoding=utf-8";
				request.Timeout = p_timeOutSec * 1000;
				request.Headers.Add("Authorization", "Basic " + GetAuthHeader());

				HttpWebResponse objResponse = (HttpWebResponse)request.GetResponse();

				if (objResponse.StatusCode == HttpStatusCode.OK)
				{
					StreamReader StreamReader = null;
					StreamReader = new StreamReader(objResponse.GetResponseStream());

					string strJson = StreamReader.ReadToEnd().Trim();
					p_response = strJson;

					T oResponse = default(T);
					//if (ConvertJsonToClassObject<T>(strJson, ref oResponse))
					//{
					//	resp = oResponse;
					//	return "true" + p_response;
					//}
					//return "false1" + p_response;

					return NConvertJsonToClassObject<T>(strJson, ref oResponse);
				}
				else
				{
					p_errorMessage = objResponse.StatusDescription;
					return "false2 " + p_errorMessage;
				}

			}
			catch (Exception ex)
			{
				p_errorMessage = ex.Message;
				LogError("Error at: Evo/GetJson" + "\r\nError message: " + ex.Message.ToString());
				return "false3 " + ex.Message.ToString();
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
				p_parseError = true;
				p_errorMessage = ex.Message;
				return false;
			}
		}

		private string NConvertJsonToClassObject<T>(string JsonObj, ref T TargetClass)
		{
			try
			{
				TargetClass = JsonConvert.DeserializeObject<T>(JsonObj);
				return "true";
			}
			catch (Exception ex)
			{
				p_parseError = true;
				p_errorMessage = ex.Message;
				return "false" + p_errorMessage;
			}
		}

		#endregion

		#region  XML 

		private bool PostXML<T, E>(ref T resp, ref E respErr)
		{
			try
			{
				byte[] body = Charset.GetBytes(p_parameter);

				ServicePointManager.Expect100Continue = true;
				ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

				HttpWebRequest objRequest = (HttpWebRequest)(WebRequest.Create(p_targetApiUrl));

				objRequest.Method = "POST";
				objRequest.ContentType = "application/xml; charset=utf-8";
				objRequest.ContentLength = body.Length;
				objRequest.Timeout = p_timeOutSec * 1000;
				objRequest.AllowWriteStreamBuffering = true;

				using (Stream os = objRequest.GetRequestStream())
				{
					os.Write(body, 0, body.Length);
				}

				HttpWebResponse objResponse = (HttpWebResponse)objRequest.GetResponse();

				StreamReader StreamReader = null;
				StreamReader = new StreamReader(objResponse.GetResponseStream(), Charset);

				string strXML = StreamReader.ReadToEnd().Trim();
				p_response = strXML;

				T oResponse = default(T);
				E oErr = default(E);
				if (DeSerializeFromXMLString<T>(strXML, ref oResponse))
				{
					resp = oResponse;
					return true;
				}
				else if (DeSerializeFromXMLString<E>(strXML, ref oErr))
				{
					respErr = oErr;
					return true;
				}
				else
				{
					return false;
				}
			}
			catch (Exception ex)
			{
				p_response = ex.Message;
				return false;
			}

		}

		public bool GetXML<T, E>(ref T resp, ref E respErr, bool textReturn = false, string ElementName = "error")
		{
			try
			{
				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(p_targetApiUrl);
				request.Method = "GET";
				if (textReturn)
				{
					request.Accept = "text/plain";
				}
				else
				{
					request.Accept = "application/xml";
				}

				HttpWebResponse objResponse = (HttpWebResponse)request.GetResponse();
				StreamReader StreamReader = null;
				StreamReader = new StreamReader(objResponse.GetResponseStream(), Charset);

				string strXML = StreamReader.ReadToEnd().Trim();
				//string strXML = @"<?xml version=""1.0""?> <transfer><result>N</result><errormsg>Frequency over limit, please try again later.</errormsg></transfer>";
				//string strXML = @"<?xml version=""1.0""?> <transfer><result>Y</result><balance>96.62</balance><etransid>668b0590-b403-46bb-bcf0-debb31e820e1</etransid><transid>q2646qngdsusv3ff</transid><datetime>Tue Mar 7 20:42:02 UTC 2023</datetime><euid>EVGYQQIANG26</euid><uid/></transfer>";
				p_response = strXML;

				T oResponse = default(T);
				E oErr = default(E);

				if (DeSerializeFromXMLString<T>(strXML, ref oResponse, ElementName))
				{
					resp = oResponse;
					return true;
				}
				//else if (DeSerializeFromXMLString<E>(strXML, ref oErr, ElementName))
				//{
				//	respErr = oErr;
				//	return true;
				//}
				else
				{
					return false;
				}
			}
			catch (Exception ex)
			{
				p_response = ex.Message;
				return false;
			}

		}

		//private bool ConvertXmlToClassObject<T>(XmlNode Xml_node, ref T resp)
		//{
		//    try
		//    {
		//        resp = DeSerializeFromXMLString<T>(TargetClass.GetType(), Xml_node.OuterXml, ElementName);
		//        return true;
		//    }
		//    catch (Exception ex)
		//    {
		//        p_parseError = true;
		//        p_errorMessage = ex.Message;
		//        return false;
		//    }
		//}

		//private bool ConvertXmlToClassObject(string Xml_string, ref object TargetClass, string ElementName = "")
		//{
		//    try
		//    {
		//        TargetClass = DeSerializeFromXMLString(TargetClass.GetType(), Xml_string, ElementName);
		//        return true;
		//    }
		//    catch (Exception ex)
		//    {
		//        p_parseError = true;
		//        p_errorMessage = ex.Message;
		//        return false;
		//    }
		//}

		public bool DeSerializeFromXMLString<T>(string xmlString, ref T TargetClass, string ElementName = "error")
		{
			try
			{
				XmlRootAttribute xRoot = new XmlRootAttribute();
				xRoot.ElementName = ElementName;
				xRoot.IsNullable = true;

				XmlSerializer ser = new XmlSerializer(typeof(T), xRoot);
				XmlReader xRdr = XmlReader.Create(new StringReader(xmlString));
				TargetClass = (T)ser.Deserialize(xRdr);
				return true;
			}
			catch (Exception ex)
			{
				p_parseError = true;
				p_errorMessage = ex.Message;
				return false;
			}
		}


		#endregion

		#endregion

		#region API Functions

		public bool UserAuth(string PlayerId, string FirstName, string LastName, string Country, string Language, string Currency, string SessionId, string IpAddr, string BrandId, string SkinId, string GameCategory, string GameInterface, JObject Urls, bool isMobile = false, bool InfoUpdate = true, string vipGroupId = "", string vipGroupAction = "assign", string gameCode ="")
		{
			try
			{
				UserAuthRequest Req = new UserAuthRequest();
				Req.uuid = GenerateSerialNo();
				Req.player.id = PlayerId;
				Req.player.update = InfoUpdate;
				Req.player.firstName = FirstName;
				Req.player.lastName = LastName;
				Req.player.nickname = PlayerId;
				Req.player.country = Country;
				Req.player.language = Language;
				Req.player.currency = Currency;
				Req.player.session.id = SessionId;
				Req.player.session.ip = IpAddr;

				//Req.player.group.id = vipGroupId;
				Req.player.group.action = vipGroupAction;

				Req.config.brand.id = BrandId;
				Req.config.brand.skin = SkinId;

				Req.config.game.category = ((!(string.IsNullOrEmpty(GameCategory))) ? GameCategory : Constant.GameCategory.Roulette);
				Req.config.game._interface = ((!(string.IsNullOrEmpty(GameInterface))) ? GameInterface : Constant.GameInterface.View3D);
				//.table.id = If(Not String.IsNullOrEmpty(TableId), TableId, "ikzwgcwlysgqepar")

				Req.config.channel.mobile = false;
				Req.config.channel.wrapped = false;

				if (isMobile)
				{
					Req.config.channel.mobile = true;
					Req.config.channel.wrapped = true;
				}

				Req.config.urls.cashier = Urls["cashier"].ToString();
				Req.config.urls.gameHistory = Urls["gameHistory"].ToString();
				Req.config.urls.lobby = Urls["lobby"].ToString();
				Req.config.urls.realityCheckURL = Urls["realityCheckURL"].ToString();
				Req.config.urls.responsibleGaming = Urls["responsibleGaming"].ToString();
				Req.config.urls.sessionTimeout = Urls["sessionTimeout"].ToString();
				if(!string.IsNullOrEmpty(gameCode))
				{
					Req.config.game.table.id = gameCode;
				}

				p_targetApiUrl = string.Format("{0}ua/v1/{1}/{2}", p_hostName, p_casinoKey, p_apiToken);
				p_parameter = JsonConvert.SerializeObject(Req);
				p_request = p_targetApiUrl + " " + p_parameter;

				if (PostJSON<UserAuthSuccessResponse, UserAuthFailedResponse>(ref p_UserAuthSuccessResponse, ref p_UserAuthFailedResponse))
				{
					if (p_UserAuthFailedResponse != null && p_UserAuthFailedResponse.errors.Count > 0)
					{
						p_errorMessage = string.Join(Environment.NewLine, p_UserAuthFailedResponse.errors.Select((x) => x.message));
						return false;
					}
					return true;
				}
				else
				{
					return false;
				}
			}
			catch (Exception ex)
			{
				p_errorMessage = ex.Message;
				return false;
			}

		}

		public bool GetBalance(string PlayerId)
		{
			string strParam = "cCode=RWA&output=1&ecID={0}&euID={1}";
			p_parameter = string.Format(strParam, p_casinoKey, PlayerId);

			try
			{
				p_targetApiUrl = p_hostName + "api/ecashier?" + p_parameter;
				p_request = p_targetApiUrl;

				if (GetXML<GetBalanceSuccessResponse, ErrorResponse>(ref p_GetBalanceSuccessResponse, ref p_GetBalanceFailedResponse, ElementName: "userbalance"))
				{
					if (p_GetBalanceFailedResponse != null && p_GetBalanceFailedResponse.error.Count > 0)
					{
						p_errorMessage = string.Join(Environment.NewLine, p_GetBalanceFailedResponse.error.Select((x) => x.errormsg));
						return false;
					}
					return true;
				}
				else
				{
					return false;
				}
			}
			catch (Exception ex)
			{
				p_errorMessage = ex.Message;
				return false;
			}
		}

		public bool Deposit(string PlayerId, decimal Amount, string TxnId)
		{
			string strParam = "cCode=ECR&output=1&ecID={0}&euID={1}&amount={2}&eTransID={3}";
			p_parameter = string.Format(strParam, p_casinoKey, PlayerId, Amount, TxnId);

			try
			{
				p_targetApiUrl = p_hostName + "api/ecashier?" + p_parameter;
				p_request = p_targetApiUrl;

				if (GetXML<TransferSuccessResponse, TransferFailedResponse>(ref p_TransferSuccessResponse, ref p_TransferFailedResponse, ElementName: "transfer"))
				{
					//if (p_TransferFailedResponse != null)
					//{
					//	p_errorMessage = p_TransferFailedResponse.errormsg;
					//	return false;
					//}
					//return true;
					if (p_TransferSuccessResponse != null && p_TransferSuccessResponse.result == "Y")
					{
						return true;
					}
					else
					{
						p_errorMessage = p_TransferSuccessResponse.errormsg;
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
				p_errorMessage = ex.Message;
				return false;
			}
		}

		public bool Withdraw(string PlayerId, decimal Amount, string TxnId)
		{
			string strParam = "cCode=EDB&output=1&ecID={0}&euID={1}&amount={2}&eTransID={3}";
			p_parameter = string.Format(strParam, p_casinoKey, PlayerId, Amount, TxnId);

			try
			{
				p_targetApiUrl = p_hostName + "api/ecashier?" + p_parameter;
				p_request = p_targetApiUrl;

				if (GetXML<TransferSuccessResponse, TransferFailedResponse>(ref p_TransferSuccessResponse, ref p_TransferFailedResponse, ElementName: "transfer"))
				{
					//if (p_TransferFailedResponse != null)
					//{
					//	p_errorMessage = p_TransferFailedResponse.errormsg;
					//	return false;
					//}
					//return true;
					if (p_TransferSuccessResponse != null && p_TransferSuccessResponse.result == "Y")
					{
						return true;
					}
					else
					{
						p_errorMessage = p_TransferSuccessResponse.errormsg;
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
				p_errorMessage = ex.Message;
				return false;
			}
		}

		public bool GetUserDetail(string PlayerId)
		{
			string strParam = "cCode=GUI&ecID={0}&euID={1}&output=1";
			p_parameter = string.Format(strParam, p_casinoKey, PlayerId);

			try
			{
				p_targetApiUrl = p_hostName + "api/ecashier?" + p_parameter;
				p_request = p_targetApiUrl;

				if (GetXML<GetUserDetailSuccessResponse, SingleErrorResponse>(ref p_GetUserDetailSuccessResponse, ref p_GetUserDetailFailedResponse, ElementName: "userdetails"))
				{
					if (p_GetUserDetailFailedResponse != null)
					{
						p_errorMessage = p_GetUserDetailFailedResponse.errormsg;
						return false;
					}
					return true;
				}
				else
				{
					return false;
				}
			}
			catch (Exception ex)
			{
				p_errorMessage = ex.Message;
				return false;
			}
		}

		public bool GetTransaction(string PlayerId, string TxnId)
		{
			string strParam = "cCode=TRI&ecID={0}&euID={1}&output=1&eTransID={2}";
			p_parameter = string.Format(strParam, p_casinoKey, PlayerId, TxnId);

			try
			{
				p_targetApiUrl = p_hostName + "api/ecashier?" + p_parameter;
				p_request = p_targetApiUrl;

				if (GetXML<GetTransactionSuccessResponse, ErrorResponse>(ref p_GetTransactionSuccessResponse, ref p_GetTransactionFailedResponse, ElementName: "transaction"))
				{
					if (p_GetTransactionFailedResponse != null)
					{
						p_errorMessage = string.Join(Environment.NewLine, p_GetTransactionFailedResponse.error.Select((x) => x.errormsg));
						return false;
					}
					return true;
				}
				else
				{
					return false;
				}
			}
			catch (Exception ex)
			{
				p_errorMessage = ex.Message;
				return false;
			}
		}

		public bool CasinoDailyReport(DateTime? StartDate = null, DateTime? EndDate = null, string GameType = "", string Channel = "")
		{

			try
			{
				List<string> lstParam = new List<string>();
				lstParam.Add("playerCurrency=true");

				if (StartDate.HasValue)
				{
					lstParam.Add("startDate=" + StartDate.Value.ToString("yyyy-MM-dd"));
				}
				if (EndDate.HasValue)
				{
					lstParam.Add("endDate=" + EndDate.Value.ToString("yyyy-MM-dd"));
				}
				if (!(string.IsNullOrEmpty(GameType)))
				{
					lstParam.Add("gameType=" + GameType);
				}
				if (!(string.IsNullOrEmpty(Channel)))
				{
					lstParam.Add("channel=" + Channel);
				}

				p_targetApiUrl = p_hostName + "api/gamehistory/v1/casino/daily-report?" + string.Join("&", lstParam);

				p_request = p_targetApiUrl;

				if (GetJson<CasinoDailyReport.CasinoDailyReportResponse>(ref p_CasinoDailyReportResponse))
				{
					return true;
				}
				else
				{
					return false;
				}
			}
			catch (Exception ex)
			{
				p_errorMessage = ex.Message;
				return false;
			}

		}

		public bool CasinoGamesDetail(string StartDate = "", string EndDate = "")
		{
			try
			{
				List<string> lstParam = new List<string>();

				if (!(string.IsNullOrEmpty(StartDate)))
				{
					lstParam.Add("startDate=" + StartDate);
				}
				if (!(string.IsNullOrEmpty(EndDate)))
				{
					lstParam.Add("endDate=" + EndDate);
				}

				p_targetApiUrl = p_hostName + "api/gamehistory/v1/casino/games?" + string.Join("&", lstParam);

				p_request = p_targetApiUrl;

				if (GetJson<CasinoGamesDetail.CasinoGamesDetailResponse>(ref p_CasinoGamesDetailResponse))
				{
					return true;
				}
				else
				{
					return false;
				}
			}
			catch (Exception ex)
			{
				p_errorMessage = ex.Message;
				return false;
			}

		}

		public bool CasinoStreamGamesDetail(string StartDate = "", string EndDate = "", string GameType = "", string Channel = "", string TxnId = "")
		{
			try
			{
				List<string> lstParam = new List<string>();

				if (StartDate != null)
				{
					lstParam.Add("startDate=" + StartDate);
				}
				if (EndDate != null)
				{
					lstParam.Add("endDate=" + EndDate);
				}
				if (!(string.IsNullOrEmpty(GameType)))
				{
					lstParam.Add("gameType=" + GameType);
				}
				if (!(string.IsNullOrEmpty(TxnId)))
				{
					lstParam.Add("transactionId=" + TxnId);
				}

				p_targetApiUrl = p_hostName + "api/gamehistory/v1/casino/games?" + string.Join("&", lstParam);
				p_request = p_targetApiUrl;

                if (GetJson<CasinoStreamGamesDetail.CasinoStreamGamesDetailResponse>(ref p_CasinoStreamGamesDetailResponse))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
			catch (Exception ex)
			{
				p_errorMessage = ex.Message;
				LogError("Error at: Evo/CasinoStreamGamesDetail" + "\r\nError message: " + ex.Message.ToString());
				return false;
			}

		}
		public bool PlayerGameDetail(string PlayerId, string GameId)
		{

			try
			{
				p_targetApiUrl = string.Format(p_hostName + "api/gamehistory/v1/players/{0}/games/{1}", PlayerId, GameId);
				p_request = p_targetApiUrl;

				if (GetJson<PlayerGameDetail.PlayerGameDetailResponse>(ref p_PlayerGameDetailResponse))
				{
					return true;
				}
				else
				{
					return false;
				}
			}
			catch (Exception ex)
			{
				p_errorMessage = ex.Message;
				return false;
			}
		}

		public bool GetGameList()
		{
			try
			{
				// Construct the API endpoint
				p_targetApiUrl = string.Format("{0}api/lobby/v1/{1}/tablelist", p_gameListHost, p_casinoKey);
				p_request = p_targetApiUrl;

				// Call the API and deserialize the response
				if (GetJson<GameListResponse>(ref p_GameListResponse))
				{
					return true;
				}
				else
				{
					return false;
				}
			}
			catch (Exception ex)
			{
				p_errorMessage = ex.Message;
				LogError("Error at: Evo/GetGameList" + "\r\nError message: " + ex.Message.ToString());
				return false;
			}
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
		}
		#endregion

	}
}