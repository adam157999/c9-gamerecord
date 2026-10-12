using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Web;
using System.Data;

namespace GameRecord.Gamess.MEGA
{
	public class MegaLogin : IHttpHandler
	{

		public void ProcessRequest(HttpContext context)
		{
			string P_Method = "open.operator.user.login";
			string P_FilePrefix = "MEGA";
			Common _common = new Common();

			_common.LogError("Start" + context.ToString(), P_FilePrefix);
			DateTime reqtime = DateTime.Now; string respData = string.Empty; string request = string.Empty;
			//string P_SecretKey = "KOI9iMnTl3H72Nr1hqfza6JyC1o=";
			Controllers.MEGAConnector api = new Controllers.MEGAConnector(Environment: Controllers.BaseConnector.GameEnvironment.PROD);

			dynamic res = new ExpandoObject();
			dynamic req = new ExpandoObject();

			try
			{

				System.Collections.Specialized.NameValueCollection query;
				if (context.Request.Form.Count > 0)
				{
					query = context.Request.Form;
				}
				else
				{
					query = context.Request.QueryString;
				}

				request = query.ToString();


				StreamReader reader = new StreamReader(context.Request.InputStream);
				request = reader.ReadToEnd();
				_common.InsertLog("", "Login", "Login_Request", "", "request", request, "", "", "", "");

				if (string.IsNullOrEmpty(request))
				{
					res.id = req.id;
					res.jsonrpc = req.jsonrpc;
					res.result = new ExpandoObject();
					res.result.success = "0";
					res.result.sessionId = null;
					res.result.msg = "Invalid parameter";
					res.error = null;
					_common.InsertLog("", "Login", "Login_Response", JsonConvert.SerializeObject(req), "id", res.id.ToString(), "jsonrpc", res.jsonrpc.ToString(), "result", JsonConvert.SerializeObject(res));
					goto ending;
				}

				if (Convert.ToString(request).StartsWith("json="))
				{
					request = request.Substring(5); //remove json=
				}

				req = JObject.Parse(request);

				if (req.method != P_Method)
				{
					res.id = req.id;
					res.jsonrpc = req.jsonrpc;
					res.result = new ExpandoObject();
					res.result.success = "0";
					res.result.sessionId = null;
					res.result.msg = "Invalid method";
					res.error = null;
					_common.InsertLog("", "Login", "Login_Response", JsonConvert.SerializeObject(req), "id", res.id.ToString(), "jsonrpc", res.jsonrpc.ToString(), "result", JsonConvert.SerializeObject(res));
					goto ending;
				}

				if (api.ValidateCallbackSn(Convert.ToString(req.@params.sn)) == false)
				{
					res.id = req.id;
					res.jsonrpc = req.jsonrpc;
					res.result = new ExpandoObject();
					res.result.success = "0";
					res.result.sessionId = null;
					res.result.msg = "Invalid sn";
					res.error = null;
					_common.InsertLog("", "Login", "Login_Response", JsonConvert.SerializeObject(req), "id", res.id.ToString(), "jsonrpc", res.jsonrpc.ToString(), "result", JsonConvert.SerializeObject(res));
					goto ending;
				}

				if (string.IsNullOrWhiteSpace(Convert.ToString(req.@params.random)))
				{
					res.id = req.id;
					res.jsonrpc = req.jsonrpc;
					res.result = new ExpandoObject();
					res.result.success = "0";
					res.result.sessionId = null;
					res.result.msg = "Invalid random";
					res.error = null;
					_common.InsertLog("", "Login", "Login_Response", JsonConvert.SerializeObject(req), "id", res.id.ToString(), "jsonrpc", res.jsonrpc.ToString(), "result", JsonConvert.SerializeObject(res));
					goto ending;
				}

				if (string.IsNullOrWhiteSpace(Convert.ToString(req.@params.loginId)))
				{
					res.id = req.id;
					res.jsonrpc = req.jsonrpc;
					res.result = new ExpandoObject();
					res.result.success = "0";
					res.result.sessionId = null;
					res.result.msg = "Invalid loginId";
					res.error = null;
					_common.InsertLog("", "Login", "Login_Response", JsonConvert.SerializeObject(req), "id", res.id.ToString(), "jsonrpc", res.jsonrpc.ToString(), "result", JsonConvert.SerializeObject(res));
					goto ending;
				}

				if (string.IsNullOrWhiteSpace(Convert.ToString(req.@params.password)))
				{
					res.id = req.id;
					res.jsonrpc = req.jsonrpc;
					res.result = new ExpandoObject();
					res.result.success = "0";
					res.result.sessionId = null;
					res.result.msg = "Invalid password";
					res.error = null;
					_common.InsertLog("", "Login", "Login_Response", JsonConvert.SerializeObject(req), "id", res.id.ToString(), "jsonrpc", res.jsonrpc.ToString(), "result", JsonConvert.SerializeObject(res));
					goto ending;
				}

				string digest = api.GetDigest(Convert.ToString(req.@params.random), Convert.ToString(req.@params.sn), Convert.ToString(req.@params.loginId));
				if (Convert.ToString(req.@params.digest).ToLower() != digest.ToLower())
				{
					res.id = req.id;
					res.jsonrpc = req.jsonrpc;
					res.result = new ExpandoObject();
					res.result.success = "0";
					res.result.sessionId = null;
					res.result.msg = "Invalid digest";
					res.error = null;
					_common.InsertLog("", "Login", "Login_Response", JsonConvert.SerializeObject(req), "id", res.id.ToString(), "jsonrpc", res.jsonrpc.ToString(), "result", JsonConvert.SerializeObject(res));
					goto ending;
				}

				DataTable dt = api.GetUserGameInfo(Convert.ToString(req.@params.loginId), Convert.ToString(req.@params.password));

				if (dt.Rows.Count == 0)
				{
					res.id = req.id;
					res.jsonrpc = req.jsonrpc;
					res.result = new ExpandoObject();
					res.result.success = "0";
					res.result.sessionId = null;
					res.result.msg = "Invalid player or password";
					res.error = null;
					_common.InsertLog("", "Login", "Login_Response", JsonConvert.SerializeObject(req), "id", res.id.ToString(), "jsonrpc", res.jsonrpc.ToString(), "result", JsonConvert.SerializeObject(res));
					goto ending;

				}

				res.id = req.id;
				res.jsonrpc = req.jsonrpc;
				res.result = new ExpandoObject();
				res.result.success = "1";
				res.result.sessionId = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
				res.result.msg = "Success";
				res.error = null;

				_common.InsertLog("", "Login", "Login_Response", JsonConvert.SerializeObject(req), "id", res.id.ToString(), "jsonrpc", res.jsonrpc.ToString(), "result", JsonConvert.SerializeObject(res));

			}
			catch (Exception ex)
			{
				res.id = req.id;
				res.jsonrpc = req.jsonrpc;
				res.result = new ExpandoObject();
				res.result.success = "0";
				res.result.sessionId = null;
				res.result.msg = "System error";
				res.error = null;
				_common.LogError("Login " + DateTime.Now.ToString() + " " + JsonConvert.SerializeObject(req) + " " + ex.Message.ToString(), P_FilePrefix);
				_common.InsertLog("", "Login", "Exception", ex.Message.ToString(), "req", JsonConvert.SerializeObject(req), "id", res.id.ToString(), "jsonrpc", res.jsonrpc.ToString(), "result", JsonConvert.SerializeObject(res));
				goto ending;
			}

		ending:

			respData = JsonConvert.SerializeObject(res);
			context.Response.ContentType = "application/json";
			context.Response.Write(respData);
			context.Response.Flush(); // Sends all currently buffered output to the client.
			context.Response.SuppressContent = true;  // Gets or sets a value indicating whether to send HTTP content to the client.
			context.ApplicationInstance.CompleteRequest(); // Causes ASP.NET to bypass all events and filtering in the HTTP pipeline chain of execution and directly execute the EndRequest event.
		}

		public bool IsReusable
		{
			get
			{
				return false;
			}
		}
	}

	public class LoginRequest
	{
		public string id { get; set; }
		public string method { get; set; }
		public ParamsClass @params { get; set; }
		public string jsonrpc { get; set; }

	}

	public class ParamsClass
	{
		public string method { get; set; }
		public string random { get; set; }
		public string digest { get; set; }
		public string sn { get; set; }
		public string loginId { get; set; }
		public string password { get; set; }
	}
}