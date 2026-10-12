using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace GameRecord.Gamess.Relax
{
	public class Relax : IDisposable
	{
		#region Declaration
		private string P_apiUrl { get; set; }
		private string P_reportUrl { get; set; }
		private string P_apiKey { get; set; }
		private string P_brandId { get; set; }
		private string P_provider { get; set; }
		private int P_timeOutSec { get; set; }
		private string successCode = "1000";
		private string reportMethod = "dct/getBetData";
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

		public LoginResponse LoginResponse { get { return p_LoginResponse; } }
		public ReportResponse ReportResponse { get { return p_ReportResponse; } }
		public GetBalanceResponse GetBalanceResponse { get { return p_GetBalanceResponse; } }
		public CreditResponse CreditResponse { get { return p_CreditResponse; } }
		public GetGameListResponse GetGameListResponse { get { return p_GetGameListResponse; } }
		public KickPlayerResponose KickPlayerResponose { get { return p_KickPlayerResponose; } }
		public WithdrawResponse WithdrawResponse { get { return p_WithdrawResponse; } }


		private LoginResponse p_LoginResponse;
		private GetBalanceResponse p_GetBalanceResponse;
		private ReportResponse p_ReportResponse;
		private CreditResponse p_CreditResponse;
		private GetGameListResponse p_GetGameListResponse;
		private KickPlayerResponose p_KickPlayerResponose;
		private WithdrawResponse p_WithdrawResponse;

		public Relax(string apiUrl, string apiKey, string brandId, string reportUrl, string provider, int timeOutSec = 30)
		{
			P_apiUrl = apiUrl + ((apiUrl.Substring(apiUrl.Length - 1) != "/") ? "/" : "");
			P_apiKey = apiKey;
			P_brandId = brandId;
			P_reportUrl = reportUrl;
			P_timeOutSec = timeOutSec;
			P_provider = provider;
		}

		private readonly Encoding Charset = Encoding.UTF8;
		#endregion

		#region General Functions
		public string GetMD5Signature(string parameter)
		{
			StringBuilder hash = new StringBuilder();
			MD5CryptoServiceProvider md5provider = new MD5CryptoServiceProvider();
			byte[] bytes = md5provider.ComputeHash(new UTF8Encoding().GetBytes(parameter));

			for (int i = 0; i < bytes.Length; i++)
			{
				hash.Append(bytes[i].ToString("X2"));
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
				//Attached brand id and api key for each call
				string random = Guid.NewGuid().ToString();
				req.brand_id = P_brandId;
				req.api_key = P_apiKey;
				req.sign = GetMD5Signature(req.getParameter());

				byte[] body = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(req));
				//Dim hash As String = ComputeHash(body)
				if (req.method == reportMethod)
					p_TargetApiUrl = P_reportUrl + req.method;
				else
					p_TargetApiUrl = P_apiUrl + req.method;

				ServicePointManager.Expect100Continue = true;
				ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 |
								   SecurityProtocolType.Tls11 |
								   SecurityProtocolType.Tls |
								   SecurityProtocolType.Ssl3 |
								   SecurityProtocolType.Tls13;

				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(p_TargetApiUrl);
				request.Method = "POST";
				request.ContentType = "application/json";
				request.ContentLength = body.Length;
				request.Timeout = P_timeOutSec * 1000;
				request.Headers.Add("Request-Id", "");

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
		public bool LoginGame(string brand_uid, string game_id, string currency, string language, string channel, string country_code)
		{
			LoginRequest req = new LoginRequest()
			{
				brand_uid = brand_uid,
				game_id = game_id,
				currency = currency,
				language = language,
				channel = channel,
				country_code = country_code
			};

			try
			{
				if (PostJSON<LoginResponse>(req, ref p_LoginResponse))
				{
					if (p_LoginResponse != null && p_LoginResponse.code == successCode && string.IsNullOrEmpty(p_ErrorMessage))
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

		public bool Credit(string brand_uid, decimal amount, string bill_no, string currency, string country_code)
		{

			CreditRequest req = new CreditRequest()
			{
				brand_uid = brand_uid,
				amount = amount,
				bill_no = bill_no,
				currency = currency,
				country_code = country_code,


			};

			try
			{
				if (PostJSON<CreditResponse>(req, ref p_CreditResponse))
				{
					if (p_CreditResponse != null && p_CreditResponse.code == successCode && string.IsNullOrEmpty(p_ErrorMessage))
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

		public bool GetBalance(string brand_uid, string currency)
		{

			GetBalanceRequest req = new GetBalanceRequest()
			{
				brand_uid = brand_uid,
				currency = currency
			};

			try
			{
				if (PostJSON<GetBalanceResponse>(req, ref p_GetBalanceResponse))
				{
					if (p_GetBalanceResponse != null && p_GetBalanceResponse.code == successCode && string.IsNullOrEmpty(p_ErrorMessage))
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

		public bool Withdraw(string brand_uid, decimal amount, string bill_no, string currency, string country_code)
		{

			WithdrawRequest req = new WithdrawRequest()
			{
				brand_uid = brand_uid,
				amount = amount,
				bill_no = bill_no,
				currency = currency,
				country_code = country_code
			};

			try
			{
				if (PostJSON<WithdrawResponse>(req, ref p_WithdrawResponse))
				{
					if (p_WithdrawResponse != null && p_WithdrawResponse.code == successCode && string.IsNullOrEmpty(p_ErrorMessage))
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

		public bool KickPlayer(string brand_uid, string currency)
		{

			KickPlayerRequest req = new KickPlayerRequest()
			{
				brand_uid = brand_uid,
				currency = currency
			};


			try
			{
				if (PostJSON<KickPlayerResponose>(req, ref p_KickPlayerResponose))
				{
					if (p_KickPlayerResponose != null && p_KickPlayerResponose.code == successCode && string.IsNullOrEmpty(p_ErrorMessage))
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

			GetGameListRequest req = new GetGameListRequest()
			{
				provider = P_provider
			};


			try
			{
				if (PostJSON<GetGameListResponse>(req, ref p_GetGameListResponse))
				{
					if (p_GetGameListResponse != null && p_GetGameListResponse.code == successCode && string.IsNullOrEmpty(p_ErrorMessage))
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

		public bool Report(int page, string start_time, string end_time, string currency, string provider)
		{
			ReportRequest req = new ReportRequest()
			{
				page = page,
				start_time = start_time,
				end_time = end_time,
				currency = currency,
				provider = provider
			};

			try
			{
				if (PostJSON<ReportResponse>(req, ref p_ReportResponse))
				{
					if (p_ReportResponse != null && p_ReportResponse.code == successCode && string.IsNullOrEmpty(p_ErrorMessage))
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
		public string brand_id { get; set; }
		public string sign { get; set; }

		[JsonIgnore]
		public string api_key { get; set; }
		[JsonIgnore]
		public string method { get; set; }
		public abstract string getParameter();
	}

	public class BaseResponse
	{
		public string code { get; set; }
		public string msg { get; set; }
	}

	public class CreditRequest : BaseRequest
	{
		public CreditRequest()
		{
			method = "dct/credit";
		}
		public string brand_uid { get; set; }
		public decimal amount { get; set; }
		public string bill_no { get; set; }
		public string currency { get; set; }
		public string country_code { get; set; }
		public override string getParameter()
		{
			return base.brand_id + brand_uid + base.api_key;
		}
	}

	public class CreditResponse : BaseResponse
	{
		public CreditInnerResponse data { get; set; }
	}

	public class CreditInnerResponse
	{
		public decimal balance { get; set; }
	}

	public class GetBalanceRequest : BaseRequest
	{
		public GetBalanceRequest()
		{
			method = "dct/getBalance";
		}
		public string brand_uid { get; set; }
		public string currency { get; set; }
		public override string getParameter()
		{
			return base.brand_id + brand_uid + base.api_key;
		}

	}

	public class GetBalanceResponse : BaseResponse
	{
		public GetBalanceInnerResponse data { get; set; }
	}

	public class GetBalanceInnerResponse
	{
		public decimal balance { get; set; }
	}

	public class GetGameListRequest : BaseRequest
	{
		public GetGameListRequest()
		{
			method = "dct/getGameList";
		}
		public string provider { get; set; }
		public override string getParameter()
		{
			return base.brand_id + base.api_key;
		}
	}

	public class GetGameListResponse : BaseResponse
	{
		public List<GetGameListInnerResponse> data { get; set; }
	}

	public class GetGameListInnerResponse
	{
		public string provider { get; set; }
		public int game_id { get; set; }
		public string game_name { get; set; }
		public string game_name_cn { get; set; }
		public string release_date { get; set; }
		public string rtp { get; set; }
		public string game_icon { get; set; }
		public string content_type { get; set; }
		public string game_type { get; set; }
	}

	public class KickPlayerRequest : BaseRequest
	{
		public KickPlayerRequest()
		{
			method = "dct/kickPlayer";
		}
		public string brand_uid { get; set; }
		public string currency { get; set; }
		public override string getParameter()
		{
			return base.brand_id + brand_uid + base.api_key;
		}
	}

	public class KickPlayerResponose : BaseResponse
	{

	}

	public class LoginRequest : BaseRequest
	{
		public LoginRequest()
		{
			method = "dct/loginGame";
		}
		public string brand_uid { get; set; }
		public string game_id { get; set; }
		public string currency { get; set; }
		public string language { get; set; }
		public string channel { get; set; }
		public string country_code { get; set; }

		public override string getParameter()
		{
			return base.brand_id + brand_uid + base.api_key;
		}
	}

	public class LoginResponse : BaseResponse
	{
		public LoginInnerResponse data { get; set; }
	}

	public class LoginInnerResponse
	{
		public string game_url { get; set; }
	}

	public class ReportRequest : BaseRequest
	{
		public ReportRequest()
		{
			method = "dct/getBetData";
		}
		public int page { get; set; }
		public string start_time { get; set; }
		public string end_time { get; set; }
		public string currency { get; set; }
		public string provider { get; set; }
		public override string getParameter()
		{
			return base.brand_id + start_time + end_time + base.api_key;
		}
	}

	public class ReportResponse : BaseResponse
	{
		public List<ReportInnerResponse> data { get; set; }
		public PageClass page { get; set; }
	}

	public class ReportInnerResponse
	{
		public string brand_uid { get; set; }
		public string currency { get; set; }
		public string wager_type { get; set; }
		public decimal amount { get; set; }
		public decimal before_amount { get; set; }
		public decimal after_amount { get; set; }
		public string game_id { get; set; }
		public string game_name { get; set; }
		public string round_id { get; set; }
		public string wager_id { get; set; }
		public decimal jackpot_contribution { get; set; }
		public decimal jackpot_win { get; set; }
		public string description { get; set; }
		public dynamic game_result { get; set; }
		public string create_time { get; set; }
		public decimal tip { get; set; }
	}

	public class PageClass
	{
		public int current_page { get; set; }
		public int total_count { get; set; }
	}

	public class WithdrawRequest : BaseRequest
	{
		public WithdrawRequest()
		{
			method = "dct/withdraw";
		}
		public string brand_uid { get; set; }
		public decimal amount { get; set; }
		public string bill_no { get; set; }
		public string currency { get; set; }
		public string country_code { get; set; }

		public override string getParameter()
		{
			return base.brand_id + brand_uid + base.api_key;
		}
	}

	public class WithdrawResponse : BaseResponse
	{
		public WithdrawInnerResponse data { get; set; }
	}

	public class WithdrawInnerResponse
	{
		public decimal balance { get; set; }
	}
	#endregion
}