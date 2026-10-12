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
//using System.Web.Http;
//using HttpGetAttribute = System.Web.Http.HttpGetAttribute;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;
using Newtonsoft.Json.Linq;

namespace GameRecord.Controllers
{
	public class BTPlayerController : Controller
	{
		public static readonly object MyLock = new object();
		public static int timeoutsec = 180000; //3 mins
		Common _common = new Common();

		public JsonResult CreateNewPlayer(string playerID, string uplineID, string phoneNum)
		{
			lock (MyLock)
			{
				try
				{
					_common.LogError("BTPlayer/CreateNewPlayer" + " " + DateTime.Now.ToString() + string.Format(" Start. | playerID:{0} uplineID:{1} phoneNum:{2}", playerID, uplineID, phoneNum), "BTPlayer");
					if (string.IsNullOrWhiteSpace(playerID))
					{
						_common.LogError("BTPlayer/CreateNewPlayer" + " " + DateTime.Now.ToString() + string.Format(" Player ID is required. | playerID:{0} uplineID:{1} phoneNum:{2}", playerID, uplineID, phoneNum), "BTPlayer");
						return Json(new
						{
							ErrorCode = -1,
							ErrorDesc = "Player ID is required."
						}, JsonRequestBehavior.AllowGet);
					}
					else
					{
						if (CheckAccountIDExist(playerID))
						{
							_common.LogError("BTPlayer/CreateNewPlayer" + " " + DateTime.Now.ToString() + string.Format(" Player ID already exists. | playerID:{0} uplineID:{1} phoneNum:{2}", playerID, uplineID, phoneNum), "BTPlayer");
							return Json(new
							{
								ErrorCode = -2,
								ErrorDesc = "Account ID already exists."
							}, JsonRequestBehavior.AllowGet);
						}
					}

					if (!string.IsNullOrWhiteSpace(uplineID))
					{
						if (!CheckAccountIDExist(uplineID))
						{
							_common.LogError("BTPlayer/CreateNewPlayer" + " " + DateTime.Now.ToString() + string.Format(" Upline ID not exists. | playerID:{0} uplineID:{1} phoneNum:{2}", playerID, uplineID, phoneNum), "BTPlayer");
							return Json(new
							{
								ErrorCode = -4,
								ErrorDesc = "Upline ID not exists."
							}, JsonRequestBehavior.AllowGet);
						}

						if (!CheckUplineAccountType(uplineID))
						{
							_common.LogError("BTPlayer/CreateNewPlayer" + " " + DateTime.Now.ToString() + string.Format(" Incorrect upline account type. | playerID:{0} uplineID:{1} phoneNum:{2}", playerID, uplineID, phoneNum), "BTPlayer");
							return Json(new
							{
								ErrorCode = -5,
								ErrorDesc = "Player only can register under member or player."
							}, JsonRequestBehavior.AllowGet);
						}
					}

					if (string.IsNullOrWhiteSpace(phoneNum))
					{
						_common.LogError("BTPlayer/CreateNewPlayer" + " " + DateTime.Now.ToString() + string.Format(" Phone Number is required. | playerID:{0} uplineID:{1} phoneNum:{2}", playerID, uplineID, phoneNum), "BTPlayer");
						return Json(new
						{
							ErrorCode = -6,
							ErrorDesc = "Phone Number is required."
						}, JsonRequestBehavior.AllowGet);
					}
					//else
					//{
					//	if (CheckPhoneNumExist(phoneNum))
					//	{
					//		_common.LogError("BTPlayer/CreateNewPlayer" + " " + DateTime.Now.ToString() + string.Format(" Phone Number already exists. | playerID:{0} uplineID:{1} phoneNum:{2}", playerID, uplineID, phoneNum), "BTPlayer");
					//		return Json(new
					//		{
					//			ErrorCode = -7,
					//			ErrorDesc = "Phone Number already exists."
					//		}, JsonRequestBehavior.AllowGet);
					//	}
					//}

					//if (string.IsNullOrWhiteSpace(uplineID))
					//{
					//	_common.LogError("BT/CreateNewPlayer" + " " + DateTime.Now.ToString() + string.Format("Upline ID is required. | playerID:{0} uplineID:{1} phoneNum:{2}", playerID, uplineID, phoneNum), "BT");
					//	return Json(new
					//	{
					//		ErrorCode = -3,
					//		ErrorDesc = "Upline ID is required."
					//	}, JsonRequestBehavior.AllowGet);
					//}
					//else
					//{
					//	if (!CheckAccountIDExist(uplineID))
					//	{
					//		_common.LogError("BT/CreateNewPlayer" + " " + DateTime.Now.ToString() + string.Format("Upline ID not exists. | playerID:{0} uplineID:{1} phoneNum:{2}", playerID, uplineID, phoneNum), "BT");
					//		return Json(new
					//		{
					//			ErrorCode = -4,
					//			ErrorDesc = "Upline ID not exists."
					//		}, JsonRequestBehavior.AllowGet);
					//	}
					//}

					using (BingoConn db = new BingoConn())
					{
						db.sp_AccountHierarchy_Insert(playerID.Trim(), string.IsNullOrWhiteSpace(uplineID) ? null : uplineID.Trim(), "Player", phoneNum.Trim());
						db.SaveChanges();

						Update_PlayerTask_NearestAgent(playerID.Trim());
						MigratePlayer(playerID.Trim());
						//InactivatePlayer(playerID.Trim());
						_common.LogError("BTPlayer/CreateNewPlayer" + " " + DateTime.Now.ToString() + string.Format(" Success. | playerID:{0} uplineID:{1} phoneNum:{2}", playerID, uplineID, phoneNum), "BTPlayer");
						return Json(new
						{
							ErrorCode = 0,
							ErrorDesc = "Success."
						}, JsonRequestBehavior.AllowGet);
					}
				}
				catch (WebException ex)
				{
					_common.LogError("BTPlayer/CreateNewPlayer" + " " + DateTime.Now.ToString() + string.Format(" {3} | playerID:{0} uplineID:{1} phoneNum:{2}", playerID, uplineID, phoneNum, ex.Message), "BTPlayer");
					return null;
				}
			}
		}

		public bool CheckUplineAccountType(string uplineID)
		{
			try
			{
				using (BingoConn db = new BingoConn())
				{
					var query = db.AccountHierarchies.Where(s => s.AccountID == uplineID.Trim()).FirstOrDefault();
					if (query != null)
					{
						if (query.AccountType.ToUpper() == "MEMBER" || query.AccountType.ToUpper() == "PLAYER")
							return true;
					}

					return false;
				}
			}
			catch (Exception ex)
			{
				return false;
			}
		}

		public bool CheckAccountIDExist(string accountID)
		{
			try
			{
				using (BingoConn db = new BingoConn())
				{
					var query = db.AccountHierarchies.Where(s => s.AccountID == accountID.Trim()).FirstOrDefault();
					if (query != null)
						return true;

					return false;
				}
			}
			catch (Exception ex)
			{
				return false;
			}
		}

		public bool CheckPhoneNumExist(string phoneNum)
		{
			try
			{
				using (BingoConn db = new BingoConn())
				{
					var query = db.AccountHierarchies.Where(s => s.PhoneNum == phoneNum.Trim() && s.AccountType.ToUpper() == "PLAYER").FirstOrDefault();
					if (query != null)
						return true;

					return false;
				}
			}
			catch (Exception ex)
			{
				return false;
			}
		}

		public void Update_PlayerTask_NearestAgent(string PlayerId)
		{
			using (SqlConnection con_tw = new SqlConnection(ConfigurationManager.ConnectionStrings["TaiwanConn"].ConnectionString))
			using (SqlConnection con_game = new SqlConnection(ConfigurationManager.ConnectionStrings["BingoConn1"].ConnectionString))
			{
				con_tw.Open();
				con_game.Open();

				string sNearestUpline = "";

				string sql1 = "select * from dbo.GetNearestAgentByPlayer('" + PlayerId + "')";
				//_common.LogError("BTPlayer/CreateNewPlayer" + " " + PlayerId + " " + DateTime.Now.ToString() + " " + sql1, "BTPlayer");

				using (SqlDataAdapter adapter = new SqlDataAdapter(sql1, con_game))
				{
					DataTable table = new DataTable();
					adapter.Fill(table);
					if (table.Rows.Count > 0)
					{
						sNearestUpline = table.Rows[0]["AccountID"].ToString();
						//_common.LogError("BTPlayer/CreateNewPlayer" + " " + PlayerId + " " + DateTime.Now.ToString() + " " + sNearestUpline, "BTPlayer");
					}

				}

				using (SqlTransaction tran = con_tw.BeginTransaction())
				{
					string sql = "update PLAYER_TASK set [Near Agent]='" + sNearestUpline + "' where KEY1='" + PlayerId + "'";
					//_common.LogError("BTPlayer/CreateNewPlayer" + " " + PlayerId + " " + DateTime.Now.ToString() + " " + sql, "BTPlayer");
					SqlCommand cmd = new SqlCommand(sql, con_tw, tran);
					int iRow = cmd.ExecuteNonQuery();
					if (iRow == 1)
					{
						tran.Commit();
						//_common.LogError("BTPlayer/CreateNewPlayer" + " " + PlayerId + " " + DateTime.Now.ToString() + " commit", "BTPlayer");
					}
				}
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
				s += String.Format("{0}={1}", key, val);
				if (i != d.Count - 1)
					s += "&";
			}
			return s;
		}

		public bool MigratePlayer(string PlayerId)
		{
			try
			{
				string apiurl = string.Format(ConfigurationManager.AppSettings["WebServerDomain"] + "/api/Wapi/MigratePlayer");

				var dic = new Dictionary<string, string>(){
						{"Username", ConfigurationManager.AppSettings["WapiUsername"]},
						{"Password", ConfigurationManager.AppSettings["WapiPassword"]},
						{"PlayerId", PlayerId}
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

		public void InactivatePlayer(string PlayerId)
		{
			using (SqlConnection con_tw = new SqlConnection(ConfigurationManager.ConnectionStrings["TaiwanConn"].ConnectionString))
			{
				con_tw.Open();

				using (SqlTransaction tran = con_tw.BeginTransaction())
				{
					string sql = "update PLAYER_REC set INT2='1' where KEY1='" + PlayerId + "'";
					SqlCommand cmd = new SqlCommand(sql, con_tw, tran);
					int iRow = cmd.ExecuteNonQuery();
					if (iRow == 1)
					{
						tran.Commit();
						_common.LogError("BTPlayer/InactivatePlayer" + " " + PlayerId + " " + DateTime.Now.ToString() + " commit", "BTPlayer");
					}
				}
			}
		}
	}
}