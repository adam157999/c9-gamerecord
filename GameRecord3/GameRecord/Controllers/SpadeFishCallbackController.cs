using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http;

namespace GameRecord.Controllers
{
	public class SpadeFishCallbackController : ApiController
	{
		public static readonly object MyLock = new object();
		public string BingoConn = System.Configuration.ConfigurationManager.ConnectionStrings["BingoConn1"].ToString();
		Common _common = new Common();
	V2.SpadeGamingFishConnector api = new V2.SpadeGamingFishConnector(environment: Controllers.BaseConnector.GameEnvironment.PROD);

		public class AuthorizeResquest
		{
			public string acctId { get; set; }
			public string token { get; set; }
			public string language { get; set; }
			public string gameCode { get; set; }
			public bool? forFun { get; set; }
			public string merchantCode { get; set; }
			public string serialNo { get; set; }
		}

		public class AuthorizeResponse
		{
			public acctInfo acctInfo { get; set; }
			public int code { get; set; }
			public string msg { get; set; }
			public string merchantCode { get; set; }
			public string serialNo { get; set; }
		}

		public class acctInfo
		{
			public string acctId { get; set; }
			public string userName { get; set; }
			public string currency { get; set; }
			public decimal? balance { get; set; }
			public string siteId { get; set; }
		}

		//This is for spadefish side to call for verification before open game
		[HttpPost]
		public HttpResponseMessage AuthorizeUser(AuthorizeResquest req)
		{
			lock (MyLock)
			{
				try
				{
					_common.InsertLog("SpadeFishCallback", "AuthorizeUser", "AuthorizeUser_Request", JsonConvert.SerializeObject(req), "acctId", req.acctId, "token", req.token, "serialNo", req.serialNo, "merchantCode", req.merchantCode, "language", req.language);

					AuthorizeResponse result = new AuthorizeResponse();
					if (api.ValidateCallbackMerchantCode(req.merchantCode.Trim().ToUpper()) == false)
					{
						result.code = 1;
						result.msg = "Invalid Merchant Code";
						result.serialNo = req.serialNo;
						result.merchantCode = req.merchantCode;

						return new HttpResponseMessage()
						{
							StatusCode = HttpStatusCode.OK,
							Content = new StringContent(JsonConvert.SerializeObject(result), System.Text.Encoding.UTF8, "application/json")
						};
					}

					DataTable dt = GetSpadeFishGameTokenDt(req.acctId, req.token);

					if (dt.Rows.Count > 0)
					{
						if (req.token != dt.Rows[0]["Token"].ToString())
						{
							result.code = 1;
							result.msg = "Invalid Token";
							result.serialNo = req.serialNo;
							result.merchantCode = req.merchantCode;
							_common.InsertLog("SpadeFishCallback", "AuthorizeUser", "AuthorizeUser_Response", JsonConvert.SerializeObject(req), "code", result.code.ToString(), "msg", result.msg, "result", JsonConvert.SerializeObject(result));
							return new HttpResponseMessage()
							{
								StatusCode = HttpStatusCode.OK,
								Content = new StringContent(JsonConvert.SerializeObject(result), System.Text.Encoding.UTF8, "application/json")
							};
						}
					}
					else
					{
						result.code = 1;
						result.msg = "Token Not Found";
						result.serialNo = req.serialNo;
						result.merchantCode = req.merchantCode;
						_common.InsertLog("SpadeFishCallback", "AuthorizeUser", "AuthorizeUser_Response", JsonConvert.SerializeObject(req), "code", result.code.ToString(), "msg", result.msg, "result", JsonConvert.SerializeObject(result));
						return new HttpResponseMessage()
						{
							StatusCode = HttpStatusCode.OK,
							Content = new StringContent(JsonConvert.SerializeObject(result), System.Text.Encoding.UTF8, "application/json")
						};
					}

					result.code = 0;
					result.msg = "success";
					result.serialNo = req.serialNo;
					result.merchantCode = req.merchantCode;
					result.acctInfo = new acctInfo
					{
						acctId = req.acctId,
						userName = req.acctId,
						currency = api.GetCurrency(),
						balance = api.GetGameBalance()
					};

					_common.InsertLog("SpadeFishCallback", "AuthorizeUser", "AuthorizeUser_Response", JsonConvert.SerializeObject(req), "code", result.code.ToString(), "msg", result.msg, "result", JsonConvert.SerializeObject(result));

					return new HttpResponseMessage()
					{
						StatusCode = HttpStatusCode.OK,
						Content = new StringContent(JsonConvert.SerializeObject(result), System.Text.Encoding.UTF8, "application/json")
					};
				}
				catch (Exception ex)
				{
					_common.LogError("AuthorizeUser " + DateTime.Now.ToString() + " " + JsonConvert.SerializeObject(req) + " " + ex.Message.ToString(), "SpadeFish");
					_common.InsertLog("SpadeFishCallback", "AuthorizeUser", "Exception", ex.Message.ToString(), "acctId", req.acctId, "token", req.token, "serialNo", req.serialNo, "merchantCode", req.merchantCode, "language", req.language);
					return new HttpResponseMessage(HttpStatusCode.InternalServerError);
				}
			}
		}

		public DataTable GetSpadeFishGameTokenDt(string gamePlayerID, string token)
		{
			try
			{
				using (SqlConnection con = new SqlConnection(BingoConn))
				{
					con.Open();
					SqlCommand cmd = new SqlCommand("sp_SpadeFishGameToken_Get", con);
					cmd.CommandType = CommandType.StoredProcedure;
					cmd.Parameters.Add(new SqlParameter("@GamePlayerID", gamePlayerID));
					DataTable table = new DataTable();
					SqlDataAdapter da = new SqlDataAdapter(cmd);
					da.Fill(table);

					return table;
				}
			}
			catch (Exception ex)
			{
				return new DataTable();
			}
		}
	}
}