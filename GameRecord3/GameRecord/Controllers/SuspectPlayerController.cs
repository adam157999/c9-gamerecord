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

namespace GameRecord.Controllers
{
	public class SuspectPlayerController : Controller
	{
		public static readonly object MyLock = new object();
		public string playerList = ConfigurationManager.AppSettings["SuspectPlayerStr"];
		public string smsPhoneList = ConfigurationManager.AppSettings["SMSJobPhoneList"];
		public string pnbPhoneList = ConfigurationManager.AppSettings["PNBJobPhoneList"];
		public string excludePlayerList = ConfigurationManager.AppSettings["PNBExludePlayerStr"];
		Common _common = new Common();
		private string P_GamePrefix = "Timeout";

		public bool CheckPlayerIsOnline()
		{
			try
			{
				using (BingoConn db = new BingoConn())
				{
					using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["TaiwanConn"].ConnectionString))
					{
						con.Open();
						using (SqlDataAdapter adapter = new SqlDataAdapter("select * from PLAYER_REC where INT10=1 and KEY1 in " + playerList, con))
						{
							DataTable table = new DataTable();
							adapter.Fill(table);
							if (table.Rows.Count > 0)
							{
								string key_cache = "smsjobdt";

								string content = "";
								//build sms content
								foreach (DataRow row in table.Rows)
								{
									if (HttpContext.Cache[key_cache.ToLower()] != null)
									{
										DataTable cacheDt = (DataTable)HttpContext.Cache[key_cache.ToLower()];
										var p = cacheDt.AsEnumerable().ToList().Where(s => s.Field<string>("KEY1").ToUpper().Trim() == row["KEY1"].ToString().Trim()).FirstOrDefault();
										if (p == null)
										{
											content += (string.IsNullOrWhiteSpace(content) ? "" : "\n") + row["KEY1"].ToString().Trim() + " is online.";
										}
										else
										{
											if (Convert.ToDateTime(p["DATE1"].ToString()) != Convert.ToDateTime(row["DATE1"].ToString()))
											{
												content += (string.IsNullOrWhiteSpace(content) ? "" : "\n") + row["KEY1"].ToString().Trim() + " is online.";
											}
										}
									}
									else
									{
										content += (string.IsNullOrWhiteSpace(content) ? "" : "\n") + row["KEY1"].ToString().Trim() + " is online.";
									}
								}

								if (!string.IsNullOrWhiteSpace(content))
								{
									//send sms
									foreach (var phone in smsPhoneList.Split(','))
									{
										_common.SendSmsToBulkSms(phone, content);
									}
								}

								HttpContext.Cache.Insert(key_cache.ToLower(), table, null, System.Web.Caching.Cache.NoAbsoluteExpiration, System.Web.Caching.Cache.NoSlidingExpiration);
							}
						}
					}
				}
				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("CheckPlayerIsOnline" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), "SuspectPlayer");
				return false;
			}
		}

		public void SMSJob()
		{
			try
			{
				lock (MyLock)
				{
					bool isOK = CheckPlayerIsOnline();
				}
			}
			catch (Exception ex)
			{
				_common.LogError("SMSJob/Start - " + DateTime.Now.ToString() + " " + ex.Message.ToString(), "SuspectPlayer");
			}
		}

		public bool CheckPlayerNegativeBalance()
		{
			try
			{
				using (BingoConn db = new BingoConn())
				{
					using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["TaiwanConn"].ConnectionString))
					{
						con.Open();
						using (SqlDataAdapter adapter = new SqlDataAdapter("select * from PLAYER_REC where (INT1 < 0 OR INT26 < 0) and KEY1 not in " + excludePlayerList, con))
						{
							DataTable table = new DataTable();
							adapter.Fill(table);
							if (table.Rows.Count > 0)
							{
								string content = "";
								//build sms content
								foreach (DataRow row in table.Rows)
								{
									content += (string.IsNullOrWhiteSpace(content) ? "" : "\n") + row["KEY1"].ToString().Trim() + " has negative balance.";
								}

								if (!string.IsNullOrWhiteSpace(content))
								{
									//send sms
									foreach (var phone in pnbPhoneList.Split(','))
									{
										_common.SendSmsToBulkSms(phone, content);
									}
									_common.LogError("CheckPlayerNegativeBalance " + DateTime.Now.ToString() + " " + content, "SuspectPlayer");
								}
							}
						}
					}
				}
				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("CheckPlayerNegativeBalance" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), "SuspectPlayer");
				return false;
			}
		}

		public void PNBJob()
		{
			try
			{
				lock (MyLock)
				{
					bool isOK = CheckPlayerNegativeBalance();
				}
			}
			catch (Exception ex)
			{
				_common.LogError("PNBJob/Start - " + DateTime.Now.ToString() + " " + ex.Message.ToString(), "SuspectPlayer");
			}
		}

		public string GetJobTime(string action)
		{
			try
			{
				string sTime = "";

				using (var bingoConn = new BingoConn())
				{
					var query = bingoConn.ADMJobDates
															.Where(s => s.GameProvider == P_GamePrefix)
															.FirstOrDefault();

					if (action == "Start")
					{
						DateTime dtSTime = Convert.ToDateTime(query.LastRunTillDate?.ToString("yyyy-MM-dd HH:mm")) - new TimeSpan(0, 0, 1, 0, 0);
						sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					}
					else
					{
						DateTime dtSTime = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
						sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					}
				}

				return sTime;

			}
			catch (Exception ex)
			{
				_common.LogError("CheckTimeout/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "CheckTimeout");
				return "Error:" + ex.Message.ToString();
			}
		}

		public bool UpdJobTime(string startTime, string endTime)
		{
			try
			{
				DateTime dtEnd = Convert.ToDateTime(endTime);

				using (var bingoConn = new BingoConn())
				{
					var xyz = bingoConn.ADMJobDates.Where(s => s.GameProvider == P_GamePrefix).FirstOrDefault();
					if (xyz != null)
					{
						xyz.LastRunTillDate = dtEnd;

						bingoConn.Entry(xyz).State = System.Data.Entity.EntityState.Modified;
						bingoConn.SaveChanges();
					}
				}
				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("Evo888/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "Evo888");
				return false;
			}
		}

		public bool CheckTimeout()
		{
			try
			{
				string startTime = GetJobTime("Start");
				string endTime = GetJobTime("End");

				using (BingoConn db = new BingoConn())
				{
					using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["BingoConn1"].ConnectionString))
					{
						SqlCommand cmd = new SqlCommand("sp_ExceptionJob", con);
						cmd.CommandType = CommandType.StoredProcedure;
						cmd.Parameters.Add(new SqlParameter("@StartDt", startTime));
						cmd.Parameters.Add(new SqlParameter("@EndDt", endTime));

						DataTable table = new DataTable();
						SqlDataAdapter da = new SqlDataAdapter(cmd);
						da.Fill(table);
						if (table.Rows.Count > 0)
						{
							string content = "";
							//build sms content
							foreach (DataRow row in table.Rows)
							{
								content += (string.IsNullOrWhiteSpace(content) ? "" : "\n\n") + row["game"].ToString().Trim() + "\n" + row["msg"].ToString().Trim() + "\n";
								content += row["playerid"].ToString().Trim() + "\n" + row["amount"].ToString().Trim() + "\n" + row["CreatedDate"].ToString().Trim() + "\n";
							}

							if (!string.IsNullOrWhiteSpace(content))
							{
								//send sms
								foreach (var phone in pnbPhoneList.Split(','))
								{
									_common.SendSmsToBulkSms(phone, content);
								}
							}
						}

						bool isUpdJob = UpdJobTime(startTime, endTime);
					}
				}
				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("CheckTimeout" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), "SuspectPlayer");
				return false;
			}
		}

		public void TimeoutJob()
		{
			try
			{
				lock (MyLock)
				{
					bool isOK = CheckTimeout();
				}
			}
			catch (Exception ex)
			{
				_common.LogError("TimeoutJob/Start - " + DateTime.Now.ToString() + " " + ex.Message.ToString(), "SuspectPlayer");
			}
		}
	}
}