using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using Newtonsoft.Json;
using System.Text;
using GameRecord.Models;
using RestSharp;
using System.Dynamic;
using System.Data.SqlClient;
using System.Configuration;
using System.Data;
using System.Data.Entity.Migrations;
using Newtonsoft.Json.Linq;
using System.Data.Entity.Validation;

namespace GameRecord.Controllers
{
	public class CheckTWTimeController : Controller
	{
		public static readonly object MyLock = new object();
		public static int timeDifference = Convert.ToInt32(ConfigurationManager.AppSettings["SecondsGap"]);
		public string smsPhoneList = ConfigurationManager.AppSettings["TWServerJobPhoneList"];
		public string smsContent = $"Time difference between tw server and bingo server has gap of {(timeDifference / 1000).ToString()} seconds now.";

		Common _common = new Common();

		public void ServerTimeJob()
		{
			try
			{
				lock (MyLock)
				{
					CompareTime();
				}
			}
			catch (Exception ex)
			{
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "PlayerInGameBalance");
			}
		}

		public DateTime GetBingoTime()
		{
			DateTime BingoTime = DateTime.Now;

			try
			{
				using (SqlConnection con_Bingo = new SqlConnection(ConfigurationManager.ConnectionStrings["BingoConn1"].ConnectionString))
				{
					con_Bingo.Open();

					string query = "select CAST(SYSDATETIMEOFFSET() AT TIME ZONE 'Singapore Standard Time' AS datetime) 'BingoTime' ";
					SqlCommand cmd = new SqlCommand(query, con_Bingo);
					DataTable dt = new DataTable();
					SqlDataAdapter da = new SqlDataAdapter(cmd);
					da.Fill(dt);

					if (dt.Rows.Count > 0)
					{
						BingoTime = Convert.ToDateTime(dt.Rows[0]["BingoTime"]);
					}
				}

				return BingoTime;
			}
			catch
			{
				return BingoTime;
			}
		}

		public DateTime GetTWTime()
		{
			DateTime TWTime = DateTime.Now;

			try
			{
				var client = new RestClient("http://180.215.206.36:4613/Cash/CashAPI.asmx?op=GetDateTime");
				client.Timeout = -1;
				var request = new RestRequest(Method.POST);
				request.AddHeader("Content-Type", "text/xml");
				request.AddParameter("text/xml", "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n"
						+ "<soap:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">\r\n  "
						+ "<soap:Body>\r\n    "
						+ "<GetDateTime xmlns=\"http://tempuri.org/\">\r\n      "
						+ "<CompanyID>BingoTown</CompanyID>\r\n      "
						+ "</GetDateTime>\r\n  "
						+ "</soap:Body>\r\n"
						+ "</soap:Envelope>", ParameterType.RequestBody);
				IRestResponse response = client.Execute(request);

				XmlDocument xmlDoc1 = new XmlDocument();
				xmlDoc1.LoadXml(response.Content);
				string responseText = xmlDoc1.InnerText;
				dynamic result = JObject.Parse(responseText);
				if (result.Code == 1)
				{
					TWTime = Convert.ToDateTime(result.Message);
				}

				return TWTime;
			}
			catch
			{
				return TWTime;
			}
		}

		public async Task<bool> CompareTime()
		{
			try
			{
				var cTask = Task.Run(() => GetBingoTime());
				var fTask = Task.Run(() => GetTWTime());

				DateTime bingotime = await cTask;
				DateTime twtime = await fTask;

				if (Math.Abs((bingotime - twtime).TotalMilliseconds) > timeDifference)
				{
					foreach (var phone in smsPhoneList.Split(','))
					{
						_common.SendSmsToBulkSms(phone, smsContent);
					}
				}

				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("CheckTW " + DateTime.Now.ToString() + " " + ex.Message.ToString(), "CheckTW");
				return false;
			}
		}
	}
}