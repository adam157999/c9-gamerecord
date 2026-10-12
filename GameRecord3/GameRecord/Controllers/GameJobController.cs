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
	public class GameJobController : Controller
	{
		public static readonly object MyLock = new object();
		public string P_FilePrefix = "GAMEJOB";
		public int timeDifference = Convert.ToInt32(ConfigurationManager.AppSettings["MinutesGap"]);
		public int playerTimeDiff = Convert.ToInt32(ConfigurationManager.AppSettings["IDLEPlayerMinutes"]);
		public string smsPhoneList = ConfigurationManager.AppSettings["GameJobPhoneList"];

		Common _common = new Common();

		public bool GetGameJobStatus()
		{
			string content = "";
			try
			{
				using (var bingoConn = new BingoConn())
				{
					var list = bingoConn.ADMJobDates.Where(s => s.IsActive == true).ToList();
					foreach(var l in list)
					{
						DateTime now = DateTime.Now.AddHours(Convert.ToInt32(l.TimeZone));
						if (Math.Abs((now - Convert.ToDateTime(l.LastRunTillDate)).TotalMinutes) > timeDifference)
						{
							content += (string.IsNullOrWhiteSpace(content) ? "" : "\n") + "MCITY " + l.GameProvider + $" game job had stop more than {timeDifference} minutes.";
						}
					}

					if (!string.IsNullOrWhiteSpace(content))
					{
						//send sms
						foreach (var phone in smsPhoneList.Split(','))
						{
							_common.SendSmsToBulkSms(phone, content);
						}
						_common.LogError("GetGameJobStatus " + DateTime.Now.ToString() + " " + content, P_FilePrefix);
					}
				}

				return true;
			}
			catch
			{
				return false;
			}
		}

		public void GameJob()
		{
			try
			{
				lock (MyLock)
				{
					bool isOK = GetGameJobStatus();
				}
			}
			catch (Exception ex)
			{
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
			}
		}

		public bool KickIDLEPlayer()
		{
			try
			{
				using (SqlConnection con_tw = new SqlConnection(ConfigurationManager.ConnectionStrings["TaiwanConn"].ConnectionString))
				{
					con_tw.Open();
					string sql1 = "select a.KEY1 from PLAYER_TASK a where a.[Current in Game] <> '' ";
					sql1 += "and DATEDIFF(MINUTE, CAST([Last in Game Time] AS DATETIME), GETDATE()) >= " + playerTimeDiff.ToString();
					using (SqlDataAdapter adapter = new SqlDataAdapter(sql1, con_tw))
					{
						DataTable table = new DataTable();
						adapter.Fill(table);
						if (table.Rows.Count > 0)
						{
							var list = table.Rows.OfType<DataRow>()
												.Select(dr => dr.Field<string>("KEY1").Trim()).ToList();

							string refStr = string.Join(",", list.ToArray());

							_common.LogError("KickIDLEPlayer" + " " + DateTime.Now.ToString() + " " + refStr, P_FilePrefix);

							foreach (DataRow row in table.Rows)
							{
								var client = new RestClient("http://180.215.206.36:4613/Cash/CashAPI.asmx?op=CloseGame");
								client.Timeout = -1;
								var request = new RestRequest(Method.POST);
								request.AddHeader("Content-Type", "text/xml");
								request.AddParameter("text/xml", "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n"
										+ "<soap:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">\r\n  "
										+ "<soap:Body>\r\n    "
										+ "<CloseGame xmlns=\"http://tempuri.org/\">\r\n      "
										+ "<CompanyID>BingoTown</CompanyID>\r\n      "
										+ "<Account>" + row["KEY1"].ToString().Trim() + "</Account>\r\n      "
										+ "<Password></Password>\r\n      "
										+ "</CloseGame>\r\n  "
										+ "</soap:Body>\r\n"
										+ "</soap:Envelope>", ParameterType.RequestBody);
								IRestResponse response = client.Execute(request);

								XmlDocument xmlDoc1 = new XmlDocument();
								xmlDoc1.LoadXml(response.Content);
								string responseText = xmlDoc1.InnerText;
							}
						}

						return true;
					}
				}
			}
			catch(Exception ex)
			{
				_common.LogError("KickIDLEPlayer " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
				return false;
			}
		}

		public void KickPlayerJob()
		{
			try
			{
				lock (MyLock)
				{
					bool isOK = KickIDLEPlayer();
				}
			}
			catch (Exception ex)
			{
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
			}
		}
	}
}