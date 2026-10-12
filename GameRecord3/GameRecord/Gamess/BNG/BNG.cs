using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace GameRecord.Gamess.BNG
{
	public class BNG : IDisposable
	{
		#region Properties Declaration
		private string p_serviceUrl;
		private string p_reportUrl;
		private string p_projectName;
		public string p_wl;
		private string p_apiToken;
		private int p_timeOutSec { get; set; }
		private string p_targetUrl;
		private string p_parameter;

		private string p_response;
		private bool p_parseError = false;

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

		public create_player_response Create_Player_Response
		{
			get
			{
				return p_create_Player_Response;
			}
		}
		private create_player_response p_create_Player_Response;

		public get_player_response Get_Player_Response
		{
			get
			{
				return p_Get_Player_Response;
			}
		}
		private get_player_response p_Get_Player_Response;

		public get_player_token_response Get_Player_Token_Response
		{
			get
			{
				return p_Get_Player_Token_Response;
			}
		}
		private get_player_token_response p_Get_Player_Token_Response;

		public logout_player_response Logout_Player_Response
		{
			get
			{
				return p_Logout_Player_Response;
			}
		}
		private logout_player_response p_Logout_Player_Response;

		public transactionListResponse TransactionListResponse
		{
			get
			{
				return p_TransactionListResponse;
			}
		}
		private transactionListResponse p_TransactionListResponse;

		public transfer_balance_response Transfer_Balance_Response
		{
			get
			{
				return p_Transfer_Balance_Response;
			}
		}
		private transfer_balance_response p_Transfer_Balance_Response;

		public BNG(string _serviceUrl, string _reportUrl, string _apiToken, string _wl, string _projectName, int _timeOutSec = 30)
		{
			p_serviceUrl = _serviceUrl;
			p_reportUrl = _reportUrl;
			p_apiToken = _apiToken;
			p_wl = _wl;
			p_projectName = _projectName;
			p_timeOutSec = _timeOutSec;
		}


		private readonly Encoding Charset = Encoding.UTF8;
		#endregion

		#region General Functions
		//public string GetMD5Signature(string parameter)
		//{
		//	StringBuilder hash = new StringBuilder();
		//	MD5CryptoServiceProvider md5provider = new MD5CryptoServiceProvider();
		//	byte[] bytes = md5provider.ComputeHash(new UTF8Encoding().GetBytes(parameter));

		//	for (int i = 0; i < bytes.Length; i++)
		//	{
		//		hash.Append(bytes[i].ToString("x2"));
		//	}
		//	return hash.ToString();
		//}

		//private static string GetSha1Base64(string parameter)
		//{
		//	UTF8Encoding encoder = new UTF8Encoding();
		//	SHA1CryptoServiceProvider sha1Hasher = new SHA1CryptoServiceProvider();
		//	byte[] hashedDataBytes = sha1Hasher.ComputeHash(encoder.GetBytes(parameter));
		//	return Convert.ToBase64String(hashedDataBytes);
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

		private bool PostJSON<T>(BaseRequest req, ref T resp)
		{
			try
			{
				//Attached brand id and api key for each call
				req.api_token = p_apiToken;

				byte[] body = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(req));
				//Dim hash As String = ComputeHash(body)
				//{PLATFORM_SERVER_URL}/{PROJECT_NAME}/wallet/{WL}/create_player/

				if (req.method == "transaction/list")
					p_TargetApiUrl = string.Format(p_reportUrl, p_projectName, req.method);
				else
					p_TargetApiUrl = string.Format(p_serviceUrl, p_projectName, p_wl, req.method);

				ServicePointManager.Expect100Continue = true;
				ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(p_TargetApiUrl);
				request.Method = "POST";
				request.ContentType = "application/json";
				request.ContentLength = body.Length;
				request.Timeout = p_timeOutSec * 1000;

				p_Request = p_TargetApiUrl;
				P_Parameters = JsonConvert.SerializeObject(req);

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
		public bool UserCreate(string loginId, string currency)
		{

			create_player_request req = new create_player_request()
			{
				player_id = loginId,
				currency = currency

			};

			try
			{
				if (PostJSON<create_player_response>(req, ref p_create_Player_Response))
				{
					if (p_create_Player_Response != null && string.IsNullOrEmpty(p_ErrorMessage))
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

		public bool UserGet(string loginId, string currency)
		{

			get_player_request req = new get_player_request()
			{
				player_id = loginId,
				currency = currency
			};

			try
			{
				if (PostJSON<get_player_response>(req, ref p_Get_Player_Response))
				{
					if (p_Get_Player_Response != null && string.IsNullOrEmpty(p_ErrorMessage))
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

		public bool UserLogout(string loginId, string currency)
		{

			logout_player_request req = new logout_player_request()
			{
				player_id = loginId,
				currency = currency
			};

			try
			{
				if (PostJSON<logout_player_response>(req, ref p_Logout_Player_Response))
				{
					if (p_Logout_Player_Response != null && string.IsNullOrEmpty(p_ErrorMessage))
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

		public bool BalanceTransfer(string loginId, string amount, string currency, string type, string uid)
		{

			transfer_balance_request req = new transfer_balance_request()
			{
				player_id = loginId,
				currency = currency,
				amount = amount,
				type = type,
				uid = uid
			};

			try
			{
				if (PostJSON<transfer_balance_response>(req, ref p_Transfer_Balance_Response))
				{
					if (p_Transfer_Balance_Response != null && string.IsNullOrEmpty(p_ErrorMessage))
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

		public bool GetPlayerToken(string loginId, string currency)
		{

			get_player_token req = new get_player_token()
			{
				player_id = loginId,
				currency = currency,

			};


			try
			{
				if (PostJSON<get_player_token_response>(req, ref p_Get_Player_Token_Response))
				{
					if (p_Get_Player_Token_Response != null && string.IsNullOrEmpty(p_ErrorMessage))
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

		public bool GetPlayer(string loginId, string currency)
		{

			get_player_request req = new get_player_request()
			{
				player_id = loginId,
				currency = currency,

			};


			try
			{
				if (PostJSON<get_player_response>(req, ref p_Get_Player_Response))
				{
					if (p_Get_Player_Response != null && string.IsNullOrEmpty(p_ErrorMessage))
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

		public bool GetReport(string startTime, string endTime, int fetchSize = 10000)
		{

			transactionListRequest req = new transactionListRequest()
			{
				start_date = startTime,
				end_date = endTime,
				fetch_size = fetchSize
			};


			try
			{
				if (PostJSON<transactionListResponse>(req, ref p_TransactionListResponse))
				{
					if (p_TransactionListResponse != null && string.IsNullOrEmpty(p_ErrorMessage))
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

	//models
	public class BaseRequest
	{
		public string api_token { get; set; }
		public string player_id { get; set; }
		[JsonIgnore]
		public string method { get; set; }
	}

	public class BaseResponse
	{
	}

	public class create_player_request : BaseRequest
	{
		public create_player_request()
		{
			method = "create_player";
		}
		public string currency { get; set; }

	}

	public class create_player_response : BaseResponse
	{
		public string player_id { get; set; } // string, [-_*@.0-9a-zA-Z]+, 与请求的player_id相同
		public string currency { get; set; } // string, ISO-4217的货币代码
		public string mode { get; set; } // string, 游戏模式 ("REAL", "FUN")
		public string balance { get; set; } // decimal string
		public bool is_online { get; set; } // boolean
		public bool is_test { get; set; } // boolean, 预设为false，在此指玩家是否为测试或正式玩家，请注意，所有 'is_test: false' 的玩家都会纳入正式环境的对帐数据
		public string brand { get; set; } // string, [-_*@.0-9a-zA-Z]+, 玩家的站点, 与请求的brand相同或 "*" 为预设站点
	}

	public class get_player_request : BaseRequest
	{
		public get_player_request()
		{
			method = "get_player";
		}
		public string currency { get; set; }
	}

	public class get_player_response : BaseResponse
	{
		public string player_id { get; set; } // string, [-_*@.0-9a-zA-Z]+, 与请求的player_id相同
		public string brand { get; set; } // string, [-_*@.0-9a-zA-Z]+, 玩家的站点
		public string currency { get; set; } // string, ISO-4217的货币代码
		public string mode { get; set; } // string, "FUN" 或 "REAL"，预设为 "REAL"。玩家的模式。"FUN" 娱乐模式玩家使用虚拟的货币进行游戏，"REAL" 真钱模式玩家使用真实的货币进行游戏
		public string balance { get; set; } // decimal string
		public bool is_online { get; set; } // boolean
		public bool is_test { get; set; } // boolean
	}

	public class get_player_token : BaseRequest
	{
		public get_player_token()
		{
			method = "get_player_token";
		}
		public string currency { get; set; }
	}

	public class get_player_token_response : BaseResponse
	{
		public string player_token { get; set; }
	}

	public class logout_player_request : BaseRequest
	{
		public logout_player_request()
		{
			method = "logout_player";
		}
		public string currency { get; set; }
	}

	public class logout_player_response : BaseResponse
	{
		public string player_id { get; set; }     // string, [-_*@.0-9a-zA-Z]+, 与请求的player_id相同
		public string brand { get; set; }         // string, [-_*@.0-9a-zA-Z]+, 玩家的站点
		public bool is_test { get; set; }         // boolean
		public string currency { get; set; }      // string, ISO-4217的货币代码
		public string mode { get; set; }          // string, 游戏模式, 其中之一: "REAL", "FUN"
		public decimal balance { get; set; }       // decimal string, >=0
		public bool is_online { get; set; }       // boolean
	}

	public class transactionListRequest : BaseRequest
	{
		public transactionListRequest()
		{
			method = "transaction/list";
		}
		public string start_date { get; set; }  // [必填] string, UTC, ISO-8601
		public string end_date { get; set; }    // [必填] string, UTC, ISO-8601
		public int fetch_size { get; set; }
	}

	public class transactionListResponse : BaseResponse
	{
		public string fetch_state { get; set; }
		public List<TransactionItem> items { get; set; }
	}

	public class TransactionItem
	{
		public string transaction_id { get; set; }
		public string original_transaction_id { get; set; }
		public string type { get; set; }
		public string player_id { get; set; }
		public string brand { get; set; }
		public string currency { get; set; }
		public string mode { get; set; }
		public bool is_test { get; set; }
		public string platform { get; set; }
		public string tag { get; set; }
		public long round_id { get; set; }
		public bool round_started { get; set; }
		public bool round_finished { get; set; }
		public string bet { get; set; }
		public string win { get; set; }
		public string balance_after { get; set; }
		public bool auto_paid { get; set; }
		public string provider_id { get; set; }
		public string provider_name { get; set; }
		public string game_id { get; set; }
		public string game_name { get; set; }
		public string status { get; set; }
		public bool manual_confirmed { get; set; }
		public string c_at { get; set; }
		public long? bonus_id { get; set; }
		public string campaign { get; set; }
		public string bonus_source { get; set; }
		public string ext_bonus_id { get; set; }
		public string bonus_event { get; set; }
		public string bonus_type { get; set; }
	}

	public class transfer_balance_request : BaseRequest
	{
		public transfer_balance_request()
		{
			method = "transfer_balance";
		}
		public string currency { get; set; } // [必填] string, ISO-4217的货币代码
		public string uid { get; set; } // [必填] string, [-_0-9a-zA-Z]+
		public string amount { get; set; } // [必填 为credit和debit类型] decimal string.
		public string type { get; set; } // [必填] string, 其中之一: "DEBIT", "CREDIT", "DEBIT_ALL"
	}

	public class transfer_balance_response : BaseResponse
	{
		public string uid { get; set; }
		public string player_id { get; set; }
		public string brand { get; set; }
		public string currency { get; set; }
		public string mode { get; set; }
		public string amount { get; set; }
		public string type { get; set; }
		public decimal balance_before { get; set; }
		public decimal balance_after { get; set; }
	}
}