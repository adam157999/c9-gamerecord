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

namespace GameRecord.Controllers
{
	public class TPGController : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "TPG";
		//private string P_ApiUrl = "https://stagingapi.triple-pg.com";
		//private string P_ApiUrl = "https://api.triple-pg.com";
		private string P_ApiUrl = "https://api.mcity8.com";
		Common _common = new Common();


		//###################################################################################
		#region Main Function 

		public bool GetGameRecord()
		{
			string startTime = GetJobTime("Start");
			string endTime = GetJobTime("End");
			//string startTime = "2021-02-10T16:22:00";
			//string endTime = "2021-02-10T17:00:00";
			string[] oprArr = { "187" };

			try
			{
				foreach(var opr in oprArr)
				{
					string sMainID = Guid.NewGuid().ToString();
					int limit = 100;
					int offset = 0;
					string method = "/NewGetBatchTxnHistory";
					string param = string.Format("?operatorId={0}&from={1}&to={2}&limit={3}&offset={4}", opr, startTime, endTime, limit.ToString(), offset.ToString());
					string webAddr = P_ApiUrl + method + param;

					var httpWebRequest = (HttpWebRequest)WebRequest.Create(webAddr);
					httpWebRequest.ContentType = "application/json";
					httpWebRequest.Method = "GET";
					httpWebRequest.Timeout = 1200000;
					httpWebRequest.ReadWriteTimeout = 1200000;

					var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
					using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
					{
						var responseText = streamReader.ReadToEnd();

						dynamic result = JsonConvert.DeserializeObject<ExpandoObject>(responseText);
						var r = result as IDictionary<string, object>;

						if (r.ContainsKey("status") && r.ContainsKey("message"))
						{
							using (BingoConn db = new BingoConn())
							{
								TPGGameMain tpgGameMain = new TPGGameMain()
								{
									Id = sMainID,
									OperatorId = Convert.ToInt32(opr),
									Total = r.ContainsKey("totalRows") ? Convert.ToInt32(result.totalRows) : 0,
									Error = result.message,
									StartTime = Convert.ToDateTime(startTime),
									EndTime = Convert.ToDateTime(endTime),
									CreatedDate = DateTime.Now
								};
								db.TPGGameMains.Add(tpgGameMain);
								db.SaveChanges();

								if (result.status == 1 && Convert.ToString(result.message).ToLower() == "success")
								{
									if (Convert.ToInt32(result.totalRows) > 0)
									{
										foreach (var obj in result.data)
										{
											var d = obj as IDictionary<string, object>;
											if (d.ContainsKey("transaction_id") && d.ContainsKey("transaction_detail"))
											{
												dynamic transDetail = JsonConvert.DeserializeObject<ExpandoObject>(obj.transaction_detail);
												var td = transDetail as IDictionary<string, object>;

												string transID = Convert.ToString(obj.transaction_id);
												try
												{
													if (db.TPGGames.Where(x => x.TransactionID == transID).FirstOrDefault() == null)
													{
														TPGGame tpgGame = new TPGGame()
														{
															fId = sMainID,
															TransactionID = Convert.ToString(obj.transaction_id),
															CreatedAt = d.ContainsKey("created_at") ? Convert.ToDateTime(obj.created_at) : null,
															UpdatedAt = d.ContainsKey("updated_at") ? Convert.ToDateTime(obj.updated_at) : null,
															OperatorID = d.ContainsKey("operator_id") ? Convert.ToInt32(obj.operator_id) : null,
															Username = d.ContainsKey("username") ? Convert.ToString(obj.username) : null,
															Currency = d.ContainsKey("currency") ? Convert.ToString(obj.currency) : null,
															LogType = d.ContainsKey("log_type") ? Convert.ToInt32(obj.log_type) : null,
															GameType = d.ContainsKey("game_type") ? Convert.ToInt32(obj.game_type) : null,
															GameTheme = d.ContainsKey("game_theme") ? Convert.ToInt32(obj.game_theme) : null,
															GameName = d.ContainsKey("game_type") ? GetGameName(Convert.ToInt32(obj.game_type)) : null,
															GameClientPlatform = d.ContainsKey("game_client_platform") ? Convert.ToInt32(obj.game_client_platform) : null,
															PlatformName = d.ContainsKey("game_client_platform") ? GetPlatformName(Convert.ToInt32(obj.game_client_platform)) : null,
															IsTestAccount = d.ContainsKey("is_test_account") ? Convert.ToBoolean(obj.is_test_account) : null,
															Completed = d.ContainsKey("completed") ? Convert.ToBoolean(obj.completed) : null,
															TotalDeductAmount = td.ContainsKey("total_deduct_amount") ? Math.Round(Convert.ToDecimal(transDetail.total_deduct_amount), 4) : null,
															TotalBetAmount = td.ContainsKey("total_bet_amount") ? Math.Round(Convert.ToDecimal(transDetail.total_bet_amount), 4) : null,
															TotalTurnover = td.ContainsKey("total_valid_bet_amount") ? Math.Round(Convert.ToDecimal(transDetail.total_valid_bet_amount), 4) : td.ContainsKey("total_bet_amount") ? Math.Round(Convert.ToDecimal(transDetail.total_bet_amount), 4) : null,
															TotalPayoutAmount = td.ContainsKey("total_payout_amount") ? Math.Round(Convert.ToDecimal(transDetail.total_payout_amount), 4) : null,
															BoosterPrice = td.ContainsKey("booster_price") ? Math.Round(Convert.ToDecimal(transDetail.booster_price), 4) : null,
															TransactionDetail = d.ContainsKey("transaction_detail") ? JsonConvert.SerializeObject(obj.transaction_detail) : null,
															CreatedDate = DateTime.Now
														};
														db.TPGGames.Add(tpgGame);
													}
												}
												catch (Exception ex)
												{
													_common.LogError("Record " + transID + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "TPG");
												}
											}
										}
										db.SaveChanges();

										//record more than limit
										if (Convert.ToInt32(result.totalRows) > limit)
										{
											int times = (Convert.ToInt32(result.totalRows) / limit) + 1;
											for (int i = limit; i < times * limit; i += limit)
											{
												GetGameRecordExtend(limit, i, startTime, endTime, sMainID, opr);
											}
										}
									}
								}

								if (result.status == 1 || result.status == 63)
								{
									bool isUpdJob = UpdJobTime(startTime, endTime, sMainID);
								}
							}
						}
					}
				}
				

				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("TPG/GetGameRecord" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "TPG");
				return false;
			}
		}

		public bool GetGameRecordExtend(int limit, int offset, string startTime, string endTime, string sMainID, string opr)
		{
			string method = "/NewGetBatchTxnHistory";
			string param = string.Format("?operatorId={0}&from={1}&to={2}&limit={3}&offset={4}", opr, startTime, endTime, limit.ToString(), offset.ToString());
			string webAddr = P_ApiUrl + method + param;

			var httpWebRequest = (HttpWebRequest)WebRequest.Create(webAddr);
			httpWebRequest.ContentType = "application/json";
			httpWebRequest.Method = "GET";
			httpWebRequest.Timeout = 1200000;
			httpWebRequest.ReadWriteTimeout = 1200000;

			var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
			using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
			{
				var responseText = streamReader.ReadToEnd();
				dynamic result = JsonConvert.DeserializeObject<ExpandoObject>(responseText);
				var r = result as IDictionary<string, object>;

				if (r.ContainsKey("status") && r.ContainsKey("message"))
				{
					using (BingoConn db = new BingoConn()) 
					{
						if (result.status == 1 && Convert.ToString(result.message).ToLower() == "success")
						{
							if (Convert.ToInt32(result.totalRows) > 0)
							{
								foreach (var obj in result.data)
								{
									var d = obj as IDictionary<string, object>;
									if (d.ContainsKey("transaction_id") && d.ContainsKey("transaction_detail"))
									{
										dynamic transDetail = JsonConvert.DeserializeObject<ExpandoObject>(obj.transaction_detail);
										var td = transDetail as IDictionary<string, object>;

										string transID = Convert.ToString(obj.transaction_id);
										try
										{
											if (db.TPGGames.Where(x => x.TransactionID == transID).FirstOrDefault() == null)
											{
												TPGGame tpgGame = new TPGGame()
												{
													fId = sMainID,
													TransactionID = Convert.ToString(obj.transaction_id),
													CreatedAt = d.ContainsKey("created_at") ? Convert.ToDateTime(obj.created_at) : null,
													UpdatedAt = d.ContainsKey("updated_at") ? Convert.ToDateTime(obj.updated_at) : null,
													OperatorID = d.ContainsKey("operator_id") ? Convert.ToInt32(obj.operator_id) : null,
													Username = d.ContainsKey("username") ? Convert.ToString(obj.username) : null,
													Currency = d.ContainsKey("currency") ? Convert.ToString(obj.currency) : null,
													LogType = d.ContainsKey("log_type") ? Convert.ToInt32(obj.log_type) : null,
													GameType = d.ContainsKey("game_type") ? Convert.ToInt32(obj.game_type) : null,
													GameTheme = d.ContainsKey("game_theme") ? Convert.ToInt32(obj.game_theme) : null,
													GameName = d.ContainsKey("game_type") ? GetGameName(Convert.ToInt32(obj.game_type)) : null,
													GameClientPlatform = d.ContainsKey("game_client_platform") ? Convert.ToInt32(obj.game_client_platform) : null,
													PlatformName = d.ContainsKey("game_client_platform") ? GetPlatformName(Convert.ToInt32(obj.game_client_platform)) : null,
													IsTestAccount = d.ContainsKey("is_test_account") ? Convert.ToBoolean(obj.is_test_account) : null,
													Completed = d.ContainsKey("completed") ? Convert.ToBoolean(obj.completed) : null,
													TotalDeductAmount = td.ContainsKey("total_deduct_amount") ? Math.Round(Convert.ToDecimal(transDetail.total_deduct_amount), 4) : null,
													TotalBetAmount = td.ContainsKey("total_bet_amount") ? Math.Round(Convert.ToDecimal(transDetail.total_bet_amount), 4) : null,
													TotalTurnover = td.ContainsKey("total_valid_bet_amount") ? Math.Round(Convert.ToDecimal(transDetail.total_valid_bet_amount), 4) : td.ContainsKey("total_bet_amount") ? Math.Round(Convert.ToDecimal(transDetail.total_bet_amount), 4) : null,
													TotalPayoutAmount = td.ContainsKey("total_payout_amount") ? Math.Round(Convert.ToDecimal(transDetail.total_payout_amount), 4) : null,
													BoosterPrice = td.ContainsKey("booster_price") ? Math.Round(Convert.ToDecimal(transDetail.booster_price), 4) : null,
													TransactionDetail = d.ContainsKey("transaction_detail") ? JsonConvert.SerializeObject(obj.transaction_detail) : null,
													CreatedDate = DateTime.Now
												};
												db.TPGGames.Add(tpgGame);
											}
										}
										catch(Exception ex)
										{
											_common.LogError("Record " + transID + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "TPG");
										}
									}
								}
								db.SaveChanges();
							}
						}
					}
				}
			}

			return true;
		}

		public bool UpdJobTime(string startTime, string endTime, string sMainID)
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
				_common.LogError("TPG/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "TPG");
				return false;
			}
		}


		#endregion

		//###################################################################################
		#region Function 

		public string GetJobTime(string action)
		{
			try
			{
				string sTime = "";

				using (var bingoConn = new BingoConn())
				{
					var query = bingoConn.ADMJobDates.Where(s => s.GameProvider == P_GamePrefix).FirstOrDefault();

					if (action == "Start")
					{
						DateTime dtSTime = Convert.ToDateTime(query.LastRunTillDate?.ToString("yyyy-MM-dd HH:mm"));
						sTime = dtSTime.ToString("yyyy-MM-ddTHH:mm:ss");
					}
					else
					{
						DateTime dtSTime = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm")) - new TimeSpan(0, 0, 3, 0, 0);
						sTime = dtSTime.ToString("yyyy-MM-ddTHH:mm:ss");
					}
				}

				return sTime;

			}
			catch (Exception ex)
			{
				_common.LogError("TPG/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "TPG");
				return "Error:" + ex.Message.ToString();
			}
		}

		public string GetPlatformName(int id)
		{
			switch (id)
			{
				case 0:
					return "Unknown";
				case 1:
					return "Unity";
				case 2:
					return "IOS (APP)";
				case 3:
					return "Android (APP)";
				case 4:
					return "HTML5";
				case 5:
					return "IOS (HTML5)";
				case 6:
					return "Android (html5)";
				default:
					return "";
			}
		}

		public string GetGameName(int id)
		{
			switch (id)
			{
				case 1:
					return "Slot";
				case 2:
					return "Casual";
				case 3:
					return "Table";
				case 5:
					return "Keno";
				case 6:
					return "Fun Slot";
				case 10:
					return "Fishing App";
				default:
					return "";
			}
		}

		#endregion

		// GET: TPG
		public void TPGJob()
		{
			try
			{
				lock (MyLock)
				{
					bool isOK = GetGameRecord();
				}
			}
			catch (Exception ex)
			{
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "TPG");
			}
		}
	}
}