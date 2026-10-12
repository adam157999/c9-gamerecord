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
	public class PlayerInGameBalanceNewController : Controller
	{
		public static readonly object MyLock = new object();
		Common _common = new Common();

		//old method class
		public class PlayerBalanceVM
		{
			public List<PlayerBalanceVM_Row> Data { get; set; }
		}
		public class PlayerBalanceVM_Row
		{
			public string PlayerID { get; set; }
			public decimal? Balance { get; set; }
			public string Game { get; set; }
			public string Remark { get; set; }
			public bool IsSuccess { get; set; }
		}

		public bool GetPlayerBalanceRecord(string Date)
		{
			PlayerBalanceVM model = new PlayerBalanceVM { Data = new List<PlayerBalanceVM_Row>() };
			DateTime dtTo = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd")).AddHours(12);
			string sMainID = Guid.NewGuid().ToString();
			int successCnt = 0;
			int failedCnt = 0;

			try
			{
				if (!string.IsNullOrWhiteSpace(Date) && DateTime.TryParse(Date, out dtTo))
					dtTo = dtTo.AddHours(12);

				DateTime dtFrom = dtTo.AddDays(-1);

				using (BingoConn db = new BingoConn())
				{
					PlayerInGameBalanceMain main = new PlayerInGameBalanceMain()
					{
						Id = sMainID,
						Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd")),
						Type = "RECORD",
						CreatedDate = DateTime.Now
					};
					db.PlayerInGameBalanceMains.Add(main);
					db.SaveChanges();

					using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["BingoTownConn"].ConnectionString))
					{
						SqlCommand cmd = new SqlCommand("sp_PlayerInGameBalance_List", con);
						cmd.CommandType = CommandType.StoredProcedure;
						cmd.Parameters.Add(new SqlParameter("@Type", "RECORD"));
						cmd.Parameters.Add(new SqlParameter("@DateTime", dtTo.ToString("yyyy-MM-dd HH:mm:ss")));

						DataTable table = new DataTable();
						SqlDataAdapter da = new SqlDataAdapter(cmd);
						da.Fill(table);

						if (table.Rows.Count > 0)
						{
							foreach(DataRow row in table.Rows)
							{
								try
								{
									decimal dAmount = 0;
									if (Math.Abs(Convert.ToDecimal(row["Amount"].ToString())) > 0)
										dAmount = Math.Abs(Convert.ToDecimal(row["Amount"].ToString()));

									decimal cAmount = 0;

									using (SqlConnection conbt = new SqlConnection(ConfigurationManager.ConnectionStrings["BingoTownConn"].ConnectionString))
									using (SqlConnection conbingo = new SqlConnection(ConfigurationManager.ConnectionStrings["BingoConn1"].ConnectionString))
									{
										SqlCommand cmd1 = new SqlCommand("sp_PlayerInGameBalance_Detail", conbt);
										cmd1.CommandType = CommandType.StoredProcedure;
										cmd1.Parameters.Add(new SqlParameter("@Type", row["ActionDetail"].ToString().Trim().ToUpper()));
										cmd1.Parameters.Add(new SqlParameter("@PlayerId", row["PlayerId"]));
										cmd1.Parameters.Add(new SqlParameter("@DateTime", row["Datetime"]));
										cmd1.Parameters.Add(new SqlParameter("@Game", row["Game"]));

										DataTable table1 = new DataTable();
										SqlDataAdapter da1 = new SqlDataAdapter(cmd1);
										da1.Fill(table1);

										if (table1.Rows.Count > 0)
											cAmount = Math.Abs(Convert.ToDecimal(table1.Rows[0]["Amount"]));

										PlayerInGameBalance vRow = new PlayerInGameBalance();
										string PlayerID = row["PlayerId"].ToString().Trim();
										string Game = row["GameName"].ToString().Trim().ToUpper();
										DateTime CashInDt = Convert.ToDateTime(row["Datetime"]);

										var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == Game  && x.PlayerID.ToUpper() == PlayerID && x.IsActive == true).FirstOrDefault();

										if (record != null)
										{
											SqlCommand cmd2 = new SqlCommand("sp_PlayerInGameBalance_GetWinLose", conbingo);
											cmd2.CommandType = CommandType.StoredProcedure;
											cmd2.Parameters.Add(new SqlParameter("@CashInDt", row["Datetime"]));
											cmd2.Parameters.Add(new SqlParameter("@GamePlayerID", record.GamePlayerID));
											cmd2.Parameters.Add(new SqlParameter("@CheckBalanceDt", dtTo.ToString("yyyy-MM-dd HH:mm:ss")));
											cmd2.Parameters.Add(new SqlParameter("@Game", row["GameName"]));

											DataTable table2 = new DataTable();
											SqlDataAdapter da2 = new SqlDataAdapter(cmd2);
											da2.Fill(table2);

											if (table2.Rows.Count == 1)
											{
												if (table2.Columns.Contains("winlose") == true)
												{
													vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
													vRow.PlayerID = PlayerID;
													vRow.CashInBalance = Math.Round(dAmount + cAmount, 2);
													vRow.BalanceAt12pm = Math.Round(dAmount + cAmount + Convert.ToDecimal(table2.Rows[0]["winlose"]), 2);
													vRow.Game = Game;
													vRow.CashInDate = CashInDt;
													vRow.IsSuccess = true;
													vRow.CreatedDate = DateTime.Now;
													vRow.IsVerify = true;
												}
												else if (table2.Columns.Contains("balance") == true)
												{
													vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
													vRow.PlayerID = PlayerID;
													vRow.CashInBalance = Math.Round(dAmount + cAmount, 2);
													vRow.BalanceAt12pm = Math.Round(Convert.ToDecimal(table2.Rows[0]["balance"]), 2);
													vRow.Game = Game;
													vRow.CashInDate = CashInDt;
													vRow.IsSuccess = true;
													vRow.CreatedDate = DateTime.Now;
													vRow.IsVerify = true;
												}

												successCnt++;
											}
											else
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount + cAmount, 2);
												vRow.BalanceAt12pm = Math.Round(dAmount + cAmount, 2);
												vRow.Game = Game;
												vRow.CashInDate = CashInDt;
												vRow.IsSuccess = true;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;
											}
										}
										else
										{
											vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
											vRow.PlayerID = PlayerID;
											vRow.CashInBalance = Math.Round(dAmount + cAmount, 2);
											vRow.BalanceAt12pm = null;
											vRow.Game = Game;
											vRow.CashInDate = CashInDt;
											vRow.Remark = "Game Id Not Found";
											vRow.IsSuccess = false;
											vRow.CreatedDate = DateTime.Now;
											vRow.IsVerify = true;

											failedCnt++;
											_common.LogError($"GetPlayerBalanceRecord|{Game}|{PlayerID}" + " " + DateTime.Now.ToString() + " Game Id Not Found", "PlayerInGameBalance");
										}

										db.PlayerInGameBalances.AddOrUpdate(vRow);
									}
								}
								catch (DbEntityValidationException e)
								{
									foreach (var eve in e.EntityValidationErrors)
									{
										_common.LogError("Record " + string.Format("{0}|{1}", Convert.ToString(row["PlayerId"]), Convert.ToString(row["Game"])) + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
												eve.Entry.Entity.GetType().Name, eve.Entry.State), "PlayerInGameBalance");
										foreach (var ve in eve.ValidationErrors)
										{
											_common.LogError("Record " + string.Format("{0}|{1}", Convert.ToString(row["PlayerId"]), Convert.ToString(row["Game"])) + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
													ve.PropertyName, ve.ErrorMessage), "PlayerInGameBalance");
										}
									}
									return false;
								}
								catch (Exception ex)
								{
									_common.LogError("Trans Record " + string.Format("{0}|{1}", Convert.ToString(row["PlayerId"]), Convert.ToString(row["Game"])) + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), "PlayerInGameBalance");
								}
							}

							db.SaveChanges();
						}
					}

					db.PlayerInGameBalanceMains.Where(x => x.Id == sMainID).ToList().ForEach(q =>
					{
						q.SuccessCnt = successCnt;
						q.FailedCnt = failedCnt;
					});

					db.SaveChanges();
				}

				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("GetPlayerBalanceRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), "PlayerInGameBalance");
				return false;
			}
		}

		public void BalanceJob(string Date = "")
		{
			try
			{
				lock (MyLock)
				{
					bool isOK = GetPlayerBalanceRecord(Date);
				}
			}
			catch (Exception ex)
			{
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "PlayerInGameBalance");
			}
		}

		public bool GetPlayerBalanceRecordAPI(string Date)
		{
			PlayerBalanceVM model = new PlayerBalanceVM { Data = new List<PlayerBalanceVM_Row>() };
			DateTime dtTo = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd")).AddHours(12);
			string sMainID = Guid.NewGuid().ToString();
			int successCnt = 0;
			int failedCnt = 0;

			try
			{
				if (!string.IsNullOrWhiteSpace(Date) && DateTime.TryParse(Date, out dtTo))
					dtTo = dtTo.AddHours(12);

				DateTime dtFrom = dtTo.AddDays(-1);

				using (BingoConn db = new BingoConn())
				{
					PlayerInGameBalanceMain main = new PlayerInGameBalanceMain()
					{
						Id = sMainID,
						Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd")),
						Type = "API",
						CreatedDate = DateTime.Now
					};
					db.PlayerInGameBalanceMains.Add(main);
					db.SaveChanges();

					using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["BingoTownConn"].ConnectionString))
					{
						SqlCommand cmd = new SqlCommand("sp_PlayerInGameBalance_List", con);
						cmd.CommandType = CommandType.StoredProcedure;
						cmd.Parameters.Add(new SqlParameter("@Type", "API"));
						cmd.Parameters.Add(new SqlParameter("@DateTime", dtTo.ToString("yyyy-MM-dd HH:mm:ss")));

						DataTable table = new DataTable();
						SqlDataAdapter da = new SqlDataAdapter(cmd);
						da.Fill(table);

						if (table.Rows.Count > 0)
						{
							foreach (DataRow row in table.Rows)
							{
								try
								{
									decimal dAmount = 0;
									if (Math.Abs(Convert.ToDecimal(row["Amount"].ToString())) > 0)
										dAmount = Math.Abs(Convert.ToDecimal(row["Amount"].ToString()));

									decimal cAmount = 0;

									using (SqlConnection conbt = new SqlConnection(ConfigurationManager.ConnectionStrings["BingoTownConn"].ConnectionString))
									using (SqlConnection conbingo = new SqlConnection(ConfigurationManager.ConnectionStrings["BingoConn1"].ConnectionString))
									{
										SqlCommand cmd1 = new SqlCommand("sp_PlayerInGameBalance_Detail", conbt);
										cmd1.CommandType = CommandType.StoredProcedure;
										cmd1.Parameters.Add(new SqlParameter("@Type", row["ActionDetail"].ToString().Trim().ToUpper()));
										cmd1.Parameters.Add(new SqlParameter("@PlayerId", row["PlayerId"]));
										cmd1.Parameters.Add(new SqlParameter("@DateTime", row["Datetime"]));
										cmd1.Parameters.Add(new SqlParameter("@Game", row["Game"]));

										DataTable table1 = new DataTable();
										SqlDataAdapter da1 = new SqlDataAdapter(cmd1);
										da1.Fill(table1);

										if (table1.Rows.Count > 0)
											cAmount = Math.Abs(Convert.ToDecimal(table1.Rows[0]["Amount"]));

										PlayerInGameBalance vRow = new PlayerInGameBalance();
										string PlayerID = row["PlayerId"].ToString().Trim();
										string Game = row["GameName"].ToString().Trim().ToUpper();
										DateTime CashInDt = Convert.ToDateTime(row["Datetime"]);

										var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == Game && x.PlayerID.ToUpper() == PlayerID && x.IsActive == true).FirstOrDefault();

										if (record != null)
										{
											if (Game == "BT28WIN")
											{
												string apiurl = string.Format(ConfigurationManager.AppSettings["BT28WINApiUrl"] + "/GetProfile?loginID={0}", record.GamePlayerID);
												var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
												httpWebRequest.ContentType = "application/json";
												httpWebRequest.Method = "GET";
												httpWebRequest.Timeout = 900000;
												httpWebRequest.ReadWriteTimeout = 900000;

												var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

												using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
												{
													var responseText = streamReader.ReadToEnd();
													_common.LogError($"GetPlayerBalanceRecordAPI|{Game}|" + PlayerID + " " + DateTime.Now.ToString() + " " + responseText, "PlayerInGameBalance");
													dynamic result = JObject.Parse(responseText);

													if (Convert.ToString(result.ErrorCode) == "0")
													{
														vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
														vRow.PlayerID = PlayerID;
														vRow.CashInBalance = Math.Round(dAmount + cAmount, 2);
														vRow.BalanceAt12pm = Math.Round(Convert.ToDecimal(result.Data.balance), 2);
														vRow.Game = Game;
														vRow.CashInDate = CashInDt;
														vRow.IsSuccess = true;
														vRow.CreatedDate = DateTime.Now;
														vRow.IsVerify = true;

														successCnt++;
													}
													else
													{
														vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
														vRow.PlayerID = PlayerID;
														vRow.CashInBalance = Math.Round(dAmount + cAmount, 2);
														vRow.BalanceAt12pm = Math.Round(dAmount + cAmount, 2);
														vRow.Game = Game;
														vRow.CashInDate = CashInDt;
														vRow.Remark = "API Error";
														vRow.IsSuccess = false;
														vRow.CreatedDate = DateTime.Now;
														vRow.IsVerify = true;

														failedCnt++;
													}
												}
											}
											else if (Game == "IBC")
											{
												string apiurl = string.Format(ConfigurationManager.AppSettings["IBCApiUrl"] + "/CheckUserBalance?username={0}", record.GamePlayerID);
												var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
												httpWebRequest.ContentType = "application/json";
												httpWebRequest.Method = "GET";
												httpWebRequest.Timeout = 900000;
												httpWebRequest.ReadWriteTimeout = 900000;

												var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

												using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
												{
													var responseText = streamReader.ReadToEnd();
													_common.LogError($"GetPlayerBalanceRecordAPI|{Game}|" + PlayerID + " " + DateTime.Now.ToString() + " " + responseText, "PlayerInGameBalance");
													dynamic result = JObject.Parse(responseText);

													if (result.error_code == 0 && result.Data != null)
													{
														if (result.Data.Count > 0)
														{
															vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
															vRow.PlayerID = PlayerID;
															vRow.CashInBalance = Math.Round(dAmount + cAmount, 2);
															vRow.BalanceAt12pm = result.Data[0].error_code == 0 ? Convert.ToDecimal(result.Data[0].balance) : null;
															vRow.Game = Game;
															vRow.CashInDate = CashInDt;
															vRow.Remark = result.Data[0].error_code == 0 ? null : "API Error";
															vRow.IsSuccess = result.Data[0].error_code == 0 ? true : false;
															vRow.CreatedDate = DateTime.Now;
															vRow.IsVerify = true;

															if (result.Data[0].error_code == 0)
																successCnt++;
															else
																failedCnt++;
														}
														else
														{
															vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
															vRow.PlayerID = PlayerID;
															vRow.CashInBalance = Math.Round(dAmount + cAmount, 2);
															vRow.BalanceAt12pm = null;
															vRow.Game = Game;
															vRow.CashInDate = CashInDt;
															vRow.Remark = "API Error";
															vRow.IsSuccess = false;
															vRow.CreatedDate = DateTime.Now;
															vRow.IsVerify = true;

															failedCnt++;
														}
													}
													else
													{
														vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
														vRow.PlayerID = PlayerID;
														vRow.CashInBalance = Math.Round(dAmount + cAmount, 2);
														vRow.BalanceAt12pm = null;
														vRow.Game = Game;
														vRow.CashInDate = CashInDt;
														vRow.Remark = "API Error";
														vRow.IsSuccess = false;
														vRow.CreatedDate = DateTime.Now;
														vRow.IsVerify = true;

														failedCnt++;
													}
												}
											}
											else if (Game == "SABAESPORTS")
											{
												string apiurl = string.Format(ConfigurationManager.AppSettings["SABAESPORTSApiUrl"] + "/CheckUserBalance?username={0}", record.GamePlayerID);
												var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
												httpWebRequest.ContentType = "application/json";
												httpWebRequest.Method = "GET";
												httpWebRequest.Timeout = 900000;
												httpWebRequest.ReadWriteTimeout = 900000;

												var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

												using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
												{
													var responseText = streamReader.ReadToEnd();
													_common.LogError($"GetPlayerBalanceRecordAPI|{Game}|" + PlayerID + " " + DateTime.Now.ToString() + " " + responseText, "PlayerInGameBalance");
													dynamic result = JObject.Parse(responseText);

													if (result.error_code == 0 && result.Data != null)
													{
														if (result.Data.Count > 0)
														{
															vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
															vRow.PlayerID = PlayerID;
															vRow.CashInBalance = Math.Round(dAmount + cAmount, 2);
															vRow.BalanceAt12pm = result.Data[0].error_code == 0 ? Convert.ToDecimal(result.Data[0].balance) : null;
															vRow.Game = Game;
															vRow.CashInDate = CashInDt;
															vRow.Remark = result.Data[0].error_code == 0 ? null : "API Error";
															vRow.IsSuccess = result.Data[0].error_code == 0 ? true : false;
															vRow.CreatedDate = DateTime.Now;
															vRow.IsVerify = true;

															if (result.Data[0].error_code == 0)
																successCnt++;
															else
																failedCnt++;
														}
														else
														{
															vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
															vRow.PlayerID = PlayerID;
															vRow.CashInBalance = Math.Round(dAmount + cAmount, 2);
															vRow.BalanceAt12pm = null;
															vRow.Game = Game;
															vRow.CashInDate = CashInDt;
															vRow.Remark = "API Error";
															vRow.IsSuccess = false;
															vRow.CreatedDate = DateTime.Now;
															vRow.IsVerify = true;

															failedCnt++;
														}
													}
													else
													{
														vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
														vRow.PlayerID = PlayerID;
														vRow.CashInBalance = Math.Round(dAmount + cAmount, 2);
														vRow.BalanceAt12pm = null;
														vRow.Game = Game;
														vRow.CashInDate = CashInDt;
														vRow.Remark = "API Error";
														vRow.IsSuccess = false;
														vRow.CreatedDate = DateTime.Now;
														vRow.IsVerify = true;

														failedCnt++;
													}
												}
											}
											else if (Game == "MENANG88")
											{
												string apiurl = string.Format(ConfigurationManager.AppSettings["MENANG88ApiUrl"] + "/GetBalance?userId={0}", record.GamePlayerID);
												var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
												httpWebRequest.ContentType = "application/json";
												httpWebRequest.Method = "GET";
												httpWebRequest.Timeout = 900000;
												httpWebRequest.ReadWriteTimeout = 900000;

												var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

												using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
												{
													var responseText = streamReader.ReadToEnd();
													_common.LogError($"GetPlayerBalanceRecordAPI|{Game}|" + PlayerID + " " + DateTime.Now.ToString() + " " + responseText, "PlayerInGameBalance");
													dynamic result = JObject.Parse(responseText);

													if (Convert.ToString(result.status) == "1")
													{
														vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
														vRow.PlayerID = PlayerID;
														vRow.CashInBalance = Math.Round(dAmount + cAmount, 2);
														vRow.BalanceAt12pm = Math.Round(Convert.ToDecimal(result.creditbalance), 2);
														vRow.Game = Game;
														vRow.CashInDate = CashInDt;
														vRow.IsSuccess = true;
														vRow.CreatedDate = DateTime.Now;
														vRow.IsVerify = true;

														successCnt++;
													}
													else
													{
														vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
														vRow.PlayerID = PlayerID;
														vRow.CashInBalance = Math.Round(dAmount + cAmount, 2);
														vRow.BalanceAt12pm = Math.Round(dAmount + cAmount, 2);
														vRow.Game = Game;
														vRow.CashInDate = CashInDt;
														vRow.Remark = "API Error";
														vRow.IsSuccess = false;
														vRow.CreatedDate = DateTime.Now;
														vRow.IsVerify = true;

														failedCnt++;
													}
												}
											}
										}
										else
										{
											vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
											vRow.PlayerID = PlayerID;
											vRow.CashInBalance = Math.Round(dAmount + cAmount, 2);
											vRow.BalanceAt12pm = null;
											vRow.Game = Game;
											vRow.CashInDate = CashInDt;
											vRow.Remark = "Game Id Not Found";
											vRow.IsSuccess = false;
											vRow.CreatedDate = DateTime.Now;
											vRow.IsVerify = true;

											failedCnt++;
											_common.LogError($"GetPlayerBalanceRecord|{Game}|{PlayerID}" + " " + DateTime.Now.ToString() + " Game Id Not Found", "PlayerInGameBalance");
										}

										db.PlayerInGameBalances.AddOrUpdate(vRow);
									}
								}
								catch (DbEntityValidationException e)
								{
									foreach (var eve in e.EntityValidationErrors)
									{
										_common.LogError("Record " + string.Format("{0}|{1}", Convert.ToString(row["PlayerId"]), Convert.ToString(row["Game"])) + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
												eve.Entry.Entity.GetType().Name, eve.Entry.State), "PlayerInGameBalance");
										foreach (var ve in eve.ValidationErrors)
										{
											_common.LogError("Record " + string.Format("{0}|{1}", Convert.ToString(row["PlayerId"]), Convert.ToString(row["Game"])) + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
													ve.PropertyName, ve.ErrorMessage), "PlayerInGameBalance");
										}
									}
									return false;
								}
								catch (Exception ex)
								{
									_common.LogError("Trans Record " + string.Format("{0}|{1}", Convert.ToString(row["PlayerId"]), Convert.ToString(row["Game"])) + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), "PlayerInGameBalance");
								}
							}

							db.SaveChanges();
						}
					}

					db.PlayerInGameBalanceMains.Where(x => x.Id == sMainID).ToList().ForEach(q =>
					{
						q.SuccessCnt = successCnt;
						q.FailedCnt = failedCnt;
					});

					db.SaveChanges();
				}

				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("GetPlayerBalanceRecordAPI" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), "PlayerInGameBalance");
				return false;
			}
		}

		public void BalanceAPIJob(string Date = "")
		{
			try
			{
				lock (MyLock)
				{
					bool isOK = GetPlayerBalanceRecordAPI(Date);
				}
			}
			catch (Exception ex)
			{
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "PlayerInGameBalanceAPI");
			}
		}
	}
}