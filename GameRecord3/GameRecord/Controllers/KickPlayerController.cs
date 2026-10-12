using GameRecord.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Xml;

namespace GameRecord.Controllers
{
	public class KickPlayerController : ApiController
	{
		public static readonly object MyLock = new object();
		public string excludePlayerList = ConfigurationManager.AppSettings["KickPlayerExcludeStr"];
		Common _common = new Common();

		[HttpGet]
		public void KickPlayer(string GameID)
		{
			try
			{
				using (SqlConnection con_tw = new SqlConnection(ConfigurationManager.ConnectionStrings["TaiwanConn"].ConnectionString))
				{
					con_tw.Open();

					string sql1 = "select * from PLAYER_INFO where INT4 = '" + GameID + "' and KEY1 not in " + excludePlayerList;
					//string sql1 = "select a.KEY1 from PLAYER_TASK a left join GAME_STATUS b on a.[Current in Game] = b.STR1 ";
					//sql1 += "where b.KEY3 = '" + GameID + "' and a.KEY1 not in " + excludePlayerList;
					using (SqlDataAdapter adapter = new SqlDataAdapter(sql1, con_tw))
					{
						DataTable table = new DataTable();
						adapter.Fill(table);
						if (table.Rows.Count > 0)
						{
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
					}
				}
			}
			catch(Exception ex)
			{
				_common.LogError("KickPlayer " + DateTime.Now.ToString() + " " + ex.Message.ToString(), "KickPlayer");
			}
		}

	}
}