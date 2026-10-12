using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using RestSharp;
using System.Net.Http;
using System.Xml.Linq;
using System.Dynamic;
using System.Web;
using GameRecord.Models;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;

namespace PPSlot
{
    public class PPSlot : IDisposable
    {
		#region Declaration
		private string P_apiUrl { get; set; }
		private string P_secretKey { get; set; }
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

		public AddPlayerFRBResponse AddPlayerFRResponse { get { return p_AddPlayerFRBResponse; } }
		public RemovePlayerFRBResponse RemovePlayerFRResponse { get { return p_RemovePlayerFRBResponse; } }
		public GetPlayerFRBResponse GetPlayerFRResponse { get { return p_GetPlayerFRBResponse; } }

		private AddPlayerFRBResponse p_AddPlayerFRBResponse;
		private RemovePlayerFRBResponse p_RemovePlayerFRBResponse;
		private GetPlayerFRBResponse p_GetPlayerFRBResponse;

		public PPSlot(string apiUrl, string secretKey, int timeOutSec = 30)
		{
			P_apiUrl = apiUrl + ((apiUrl.Substring(apiUrl.Length - 1) != "/") ? "/" : "");
			P_secretKey = secretKey;
			P_timeOutSec = timeOutSec;
		}

		private readonly Encoding Charset = Encoding.UTF8;
		#endregion

		#region General Functions
		public string GetMD5Signature(string input)
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

		private bool PostJSON<T>(BaseRequest req, ref T resp, string contentType = "")
		{
			try
			{
				//Common Value For All Model
				req.secretKey = P_secretKey;
				req.hash = GetMD5Signature(req.GetParam());
				P_Parameters = req.GetParamWithHash();
				byte[] body = Charset.GetBytes(P_Parameters);
				p_TargetApiUrl = P_apiUrl + req.method;
				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(p_TargetApiUrl);
				request.Method = "POST";
				request.ContentType = "application/x-www-form-urlencoded";

				if (!string.IsNullOrEmpty(contentType))
					request.ContentType = contentType;

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
		public bool AddPlayerFRB(string secureLogin, string bonusCode, List<string> playerList)
		{
			AddPlayerFRBRequest req = new AddPlayerFRBRequest()
			{
				secureLogin = secureLogin,
				bonusCode = bonusCode,
				playerList = playerList
			};

			try
			{
				if (PostJSON<AddPlayerFRBResponse>(req, ref p_AddPlayerFRBResponse, "application/json"))
				{
					if (p_AddPlayerFRBResponse != null && p_AddPlayerFRBResponse.error == 0)
					{
						return true;
					}
					else
					{
						p_ErrorMessage = p_AddPlayerFRBResponse.description;
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

		public bool RemovePlayerFRB(string secureLogin, string bonusCode, List<string> playerList)
		{
			RemovePlayerFRBRequest req = new RemovePlayerFRBRequest()
			{
				secureLogin = secureLogin,
				bonusCode = bonusCode,
				playerList = playerList
			};

			try
			{
				if (PostJSON<RemovePlayerFRBResponse>(req, ref p_RemovePlayerFRBResponse, "application/json"))
				{
					if (p_RemovePlayerFRBResponse != null && p_RemovePlayerFRBResponse.error == 0)
					{
						return true;
					}
					else
					{
						p_ErrorMessage = p_RemovePlayerFRBResponse.description;
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

		public bool GetPlayerFRB(string secureLogin, string playerId)
		{
			GetPlayerFRBRequest req = new GetPlayerFRBRequest()
			{
				secureLogin = secureLogin,
				playerId = playerId
			};

			try
			{
				if (PostJSON<GetPlayerFRBResponse>(req, ref p_GetPlayerFRBResponse))
				{
					if (p_GetPlayerFRBResponse != null && p_GetPlayerFRBResponse.error == 0)
					{
						return true;
					}
					else
					{
						p_ErrorMessage = p_GetPlayerFRBResponse.description;
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

		#region Model
		public abstract class BaseRequest
		{
			public string secureLogin { get; set; }
			public string externalPlayerId { get; set; }
			public string hash { get; set; }
			public string method { get; set; }
			public string secretKey { get; set; }
			public abstract string GetParam();
			public abstract string GetParamWithHash();
		}

		public class BaseResponse
		{
			public int error { get; set; }
			public string description { get; set; }
		}

		public class AddPlayerFRBRequest : BaseRequest
		{
			public AddPlayerFRBRequest()
			{
				method = $"http/FreeRoundsBonusAPI/v2/players/add";
			}
			public string bonusCode { get; set; }
			public List<string> playerList { get; set; }
			public override string GetParam()
			{
				string rawData = $"bonusCode={bonusCode}&secureLogin={secureLogin}{secretKey}";
				return rawData;
			}

			public override string GetParamWithHash()
			{
				method += $"?bonusCode={bonusCode}&secureLogin={secureLogin}&hash={hash}";
				var json = new { playerList = playerList };
				string rawData = JsonConvert.SerializeObject(json, Formatting.Indented);
				return rawData;
			}
		}

		public class AddPlayerFRBResponse : BaseResponse
		{
		}

		public class GetPlayerFRBRequest : BaseRequest
		{
			public GetPlayerFRBRequest()
			{
				method = $"http/FreeRoundsBonusAPI/getPlayersFRB";
			}
			public string playerId { get; set; }
			public override string GetParam()
			{
				string rawData = $"playerId={playerId}&secureLogin={secureLogin}{secretKey}";
				return rawData;
			}

			public override string GetParamWithHash()
			{
				string rawData = $"playerId={playerId}&secureLogin={secureLogin}&hash={hash}";
				return rawData;
			}
		}

		public class GetPlayerFRBResponse : BaseResponse
		{
			public List<FRB> bonuses { get; set; }
		}

		public class FRB
		{
			public string currency { get; set; }
			public string gameIDList { get; set; }
			public int rounds { get; set; }
			public int roundsPlayed { get; set; }
			public string bonusCode { get; set; }
			public DateTime? expirationDate { get; set; }
		}

		public class RemovePlayerFRBRequest : BaseRequest
		{
			public RemovePlayerFRBRequest()
			{
				method = "http/FreeRoundsBonusAPI/v2/players/remove";
			}
			public string bonusCode { get; set; }
			public List<string> playerList { get; set; }
			public override string GetParam()
			{
				string rawData = $"bonusCode={bonusCode}&secureLogin={secureLogin}{secretKey}";
				return rawData;
			}

			public override string GetParamWithHash()
			{
				method += $"?bonusCode={bonusCode}&secureLogin={secureLogin}&hash={hash}";
				var json = new { playerList = playerList };
				string rawData = JsonConvert.SerializeObject(json, Formatting.Indented);
				return rawData;
			}
		}
		public class RemovePlayerFRBResponse : BaseResponse
		{
		}
		#endregion

	}


}