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
using System.Data.Entity.Validation;
using RestSharp;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;
using System.Data.Entity.Migrations;
using Newtonsoft.Json.Linq;

namespace GameRecord.Controllers
{
	public class RejectPendingController : Controller
	{
		public static readonly object MyLock = new object();
		public static int timeoutsec = 180000; //3 mins
		public string minute = ConfigurationManager.AppSettings["RejectPendingTran"];
		Common _common = new Common();

		public bool RejectPending()
		{
			try
			{
				using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["TaiwanConn"].ConnectionString))
				{
					con.Open();
					string sql2 = "select * from PLAYER_TRANSFER where INT2 = 2 AND INT3 = 0 AND DATEDIFF(MINUTE, CAST(DATE1 AS DATETIME), GETDATE()) >=" + minute;
					string sql1 = "select INT4 from PLAYER_TRANSFER where INT2 = 2 AND INT3 = 0 AND DATEDIFF(MINUTE, CAST(DATE1 AS DATETIME), GETDATE()) >=" + minute;
					using (SqlDataAdapter adapter = new SqlDataAdapter(sql2, con))
					{
						DataTable table = new DataTable();
						adapter.Fill(table);

						if (table.Rows.Count > 0)
						{
							var list = table.Rows.OfType<DataRow>()
							 .Select(dr => dr.Field<long>("INT4")).ToList();

							string refStr = string.Join(",", list.ToArray());

							_common.LogError("RejectPending" + " " + DateTime.Now.ToString() + " " + refStr, "RejectPending");

							string query = "update PLAYER_TRANSFER set INT3 = 2, DATE2 = '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "' where INT2=2 and INT3=0 and INT4 in (" + sql1 + ")";
							SqlCommand cmd = new SqlCommand(query, con);
							cmd.ExecuteNonQuery();

							foreach (DataRow row in table.Rows)
							{
								string player = row["KEY1"].ToString().Trim();
								using (SqlDataAdapter adapter1 = new SqlDataAdapter("select * from PLAYER_REC where KEY1 = '" + player + "'", con))
								{
									DataTable table1 = new DataTable();
									adapter1.Fill(table1);
									if (table1.Rows.Count == 1)
									{
										string Player = player;
										string Reference = row["INT4"].ToString().Trim();
										decimal Amount = Convert.ToDecimal(row["INT1"]) / 100;
										decimal Before = Convert.ToDecimal(table1.Rows[0]["INT1"]) / 100;
										decimal After = Convert.ToDecimal(table1.Rows[0]["INT1"]) / 100;
										string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
										InsertTran_Deposit_Instant(Player, Amount, Before, After, Reference, "Reject", now);
									}
								}
							}

							_common.LogError("RejectPending" + " " + DateTime.Now.ToString() + " Done" , "RejectPending");
						}
						else
						{
							_common.LogError("RejectPending" + " " + DateTime.Now.ToString() + " no pending transaction", "RejectPending");
						}
					}
				}

				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("RejectPending" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "RejectPG");
				return false;
			}
		}

		public void RejectPG()
		{
			try
			{
				lock(MyLock)
				{
					bool isOK = RejectPending();
				}
			}
			catch (Exception ex)
			{
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "RejectPG");
			}
		}

		public string BuildPostData(Dictionary<string, string> d)
		{
			string s = "";
			for (int i = 0; i < d.Count; i++)
			{
				var item = d.ElementAt(i);
				var key = item.Key;
				var val = item.Value;
				s += String.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(val));
				if (i != d.Count - 1)
					s += "&";
			}
			return s;
		}

		public bool InsertTran_Deposit_Instant(string PlayerId, decimal Amount, decimal Before, decimal After, string RefId, string ActionDetail, string Datetime)
		{
			try
			{
				string apiurl = string.Format(ConfigurationManager.AppSettings["WebServerDomain"] + "/api/Wapi/InsertTran_Deposit_Instant");

				var dic = new Dictionary<string, string>(){
						{"Username", ConfigurationManager.AppSettings["WapiUsername"]},
						{"Password", ConfigurationManager.AppSettings["WapiPassword"]},
						{"PlayerId", PlayerId},
						{"ActionDetail", ActionDetail},
						{"RefId", RefId},
						{"Amount", Amount.ToString()},
						{"Before", Before.ToString()},
						{"After", After.ToString()},
						{"Datetime", Datetime}
				};
				var param = BuildPostData(dic);

				byte[] data = Encoding.ASCII.GetBytes(param);

				var httpWebRequest = WebRequest.Create(new Uri(apiurl)) as HttpWebRequest;
				httpWebRequest.ContentType = "application/x-www-form-urlencoded";
				httpWebRequest.Method = "POST";
				httpWebRequest.Timeout = timeoutsec;
				httpWebRequest.ReadWriteTimeout = timeoutsec;
				httpWebRequest.ContentLength = data.Length;

				Stream requestStream = httpWebRequest.GetRequestStream();
				requestStream.Write(data, 0, data.Length);
				requestStream.Close();

				var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
				using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
				{
					var responseText = streamReader.ReadToEnd();
					dynamic result = JObject.Parse(responseText);

					if (result.Code == 0)
						return true;

					return false;
				}
			}
			catch (Exception ex)
			{
				return false;
			}
		}
	}
}