using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net;
using System.Net.Http;
using System.IO;
using Newtonsoft.Json;
using System.Text;
using System.Security.Cryptography;
using GameRecord.Models;

namespace GameRecord.Controllers
{
	public class SBOController : Controller
	{
		//Live need to have change API URL, create new agenet ID
		//private string P_ApiUrl = "http://ex-api-demo-yy.568win.com/";
		//private string P_CompKey = "F0E726C65B13470A9414A7B2D01254CE";
		//private string P_ServerId = "SVRBT01";
		//private string P_AgentId = "AgentBT03"; //Live, need to create agent id first, then put the agent id here
		//private string P_AgentPwd = "Qwer1234";
		//private string P_GamePrefix = "SBO";

		private string P_ApiUrl = "https://ex-api-yy.xxttgg.com/";
		private string P_CompKey = "F9CAAE167F1A403EBFA137FB6E188FF8";
		private string P_ServerId = "SVRBT01";
		private string P_AgentId = "BTSBOAG02"; //Live, need to create agent id first, then put the agent id here
		private string P_AgentPwd = "awWEQ03";
		private string P_GamePrefix = "SBO";

		Common _common = new Common();

		//##########################################################################################################################
		#region

		public class GameRecordVM
		{
			public ApiErrorVM error { get; set; }
			public string ServerId { get; set; }
			public List<ResultVM> result { get; set; }
		}

		public class ApiErrorVM
		{
			public int id { get; set; }
			public string msg { get; set; }
		}

		public class ResultVM
		{
			public string refNo { get; set; }
			public string username { get; set; }
			public string sportsType { get; set; }
			public DateTime orderTime { get; set; }
			public DateTime winLostDate { get; set; }
			public DateTime modifyDate { get; set; }
			public float odds { get; set; }
			public string oddsStyle { get; set; }
			public float actualStake { get; set; }
			public float stake { get; set; }
			public string currency { get; set; }
			public float winLost { get; set; }
			public float turnover { get; set; }
			public string status { get; set; }
			public bool isHalfWonLose { get; set; }
			public bool isLive { get; set; }
			public float maxWinWithoutActualStake { get; set; }
			public string ip { get; set; }
			public List<subBetVM> subBet { get; set; }
		}

		public class subBetVM
		{
			public string betOption { get; set; }
			public string marketType { get; set; }
			public float hdp { get; set; }
			public float odds { get; set; }
			public string league { get; set; }
			public string match { get; set; }
			public string status { get; set; }
			public DateTime winLostDate { get; set; }
			public string liveScore { get; set; }
			public string htScore { get; set; }
			public string ftScore { get; set; }
			public string customeizedBetType { get; set; }
		}

		#endregion

		//##########################################################################################################################
		#region API function

		public bool GetGameRecord()
		{
			string startTime = GetJobTime("Start");
			string endTime = GetJobTime("End");
			string sMainID = Guid.NewGuid().ToString();

			try
			{
				string method = "web-root/restricted/report/get-bet-list-by-modify-date.aspx";
				string webAddr = P_ApiUrl + method;
				string portfolio = "SportsBook";

				var httpWebRequest = (HttpWebRequest)WebRequest.Create(webAddr);
				httpWebRequest.ContentType = "application/json";
				httpWebRequest.Method = "POST";
				httpWebRequest.Timeout = 60000;
				httpWebRequest.ReadWriteTimeout = 60000;

				using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
				{
					string json = "{ \"Username\" : \"" + P_AgentId +
													"\", \"Portfolio\" : \"" + portfolio +
													"\", \"StartDate\" : \"" + startTime +
													"\", \"EndDate\" : \"" + endTime +
													"\", \"CompanyKey\" : \"" + P_CompKey +
													"\", \"ServerId\" : \"" + P_ServerId + "\"} ";


					streamWriter.Write(json);
					streamWriter.Flush();
				}
				var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
				using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
				{
					var responseText = streamReader.ReadToEnd();
					GameRecordVM result = JsonConvert.DeserializeObject<GameRecordVM>(responseText);

					using (BingoConn db = new BingoConn())
					{
						string uuid = "";
						int iTotal = 0;

						//Insert Main table
						SBOGameMain sBOGameMain = new SBOGameMain()
						{
							Id = sMainID,
							ServerId = result.ServerId,
							errId = result.error.id,
							msg = result.error.msg,
							startTime = Convert.ToDateTime(startTime),
							endTime = Convert.ToDateTime(endTime),
							CreatedDate = DateTime.Now
						};
						db.SBOGameMains.Add(sBOGameMain);
						db.SaveChanges();

						//Insert Result table
						foreach (var p in result.result)
						{
							iTotal = result.result.Count;
							bool isInsert = IsBetIDNotExist(p.refNo);

							if (isInsert == true)
							{
								uuid = Guid.NewGuid().ToString();

								SBOGame sBOGame = new SBOGame()
								{
									Id = uuid,
									fId = sMainID,
									refNo = p.refNo,
									username = p.username,
									sportsType = p.sportsType,
									orderTime = p.orderTime,
									winLostDate = p.winLostDate,
									modifyDate = p.modifyDate,
									odds = p.odds,
									oddsstyle = p.oddsStyle,
									stake = p.stake,
									actualStake = p.actualStake,
									currency = p.currency,
									status = p.status,
									winlost = p.winLost,
									turnover = p.turnover,
									isHalfWonLose = p.isHalfWonLose,
									isLive = p.isLive,
									maxWinWithoutActualStake = p.maxWinWithoutActualStake,
									ip = p.ip,
									Total = iTotal,
									ModifiedDate = DateTime.Now,
									CreatedDate = DateTime.Now
								};
								db.SBOGames.Add(sBOGame);
								db.SaveChanges();

								foreach (var x in p.subBet)
								{
									string uuidSub = Guid.NewGuid().ToString();

									SBOGameSub sBOGameSub = new SBOGameSub()
									{
										Id = uuidSub,
										fId = sMainID,
										refNo = p.refNo,
										betOption = x.betOption,
										marketType = x.marketType,
										hdp = x.hdp,
										odds = x.odds,
										league = x.league,
										match = x.match,
										status = x.status,
										winLostDate = x.winLostDate,
										liveScore = x.liveScore,
										htScore = x.htScore,
										ftScore = x.ftScore,
										customeizedBetType = x.customeizedBetType
									};
									db.SBOGameSubs.Add(sBOGameSub);
									db.SaveChanges();
								}
							}
							else
							{
								var xyz = db.SBOGames.Where(s => s.refNo == p.refNo).FirstOrDefault();
								if (xyz != null)
								{
									xyz.winLostDate = p.winLostDate;
									xyz.modifyDate = p.modifyDate;
									xyz.stake = p.stake;
									xyz.actualStake = p.actualStake;
									xyz.status = p.status;
									xyz.winlost = p.winLost;
									xyz.turnover = p.turnover;
									xyz.isHalfWonLose = p.isHalfWonLose;
									xyz.isLive = p.isLive;
									xyz.maxWinWithoutActualStake = p.maxWinWithoutActualStake;
									xyz.ModifiedDate = xyz.status != p.status ? DateTime.Now : xyz.ModifiedDate;

									db.Entry(xyz).State = System.Data.Entity.EntityState.Modified;
									db.SaveChanges();
								}

								foreach (var x in p.subBet)
								{
									var lsSub = db.SBOGameSubs.Where(s => s.refNo == p.refNo & s.betOption == x.betOption).FirstOrDefault();
									if (lsSub != null)
									{
										lsSub.betOption = x.betOption;
										lsSub.status = x.status;
										lsSub.winLostDate = x.winLostDate;
										lsSub.htScore = x.htScore;
										lsSub.ftScore = x.ftScore;

										db.Entry(lsSub).State = System.Data.Entity.EntityState.Modified;
										db.SaveChanges();
									}
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
				_common.LogError("SBO/GetGameRecord" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "SBO");
				return false;
			}
		}

		public bool GetGameRecordAll()
		{
			string startTime = GetJobTime("Start");
			string endTime = GetJobTime("End");
			string sMainID = Guid.NewGuid().ToString();

			try
			{
				string method = "web-root/restricted/report/v2/get-bet-list-by-modify-date.aspx";
				string webAddr = P_ApiUrl + method;
				string portfolio = "SportsBook";

				var httpWebRequest = (HttpWebRequest)WebRequest.Create(webAddr);
				httpWebRequest.ContentType = "application/json";
				httpWebRequest.Method = "POST";
				httpWebRequest.Timeout = 60000;
				httpWebRequest.ReadWriteTimeout = 60000;

				using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
				{
					string json = "{ \"Portfolio\" : \"" + portfolio +
													"\", \"StartDate\" : \"" + startTime +
													"\", \"EndDate\" : \"" + endTime +
													"\", \"CompanyKey\" : \"" + P_CompKey +
													"\", \"ServerId\" : \"" + P_ServerId + "\"} ";


					streamWriter.Write(json);
					streamWriter.Flush();
				}
				var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
				using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
				{
					var responseText = streamReader.ReadToEnd();
					GameRecordVM result = JsonConvert.DeserializeObject<GameRecordVM>(responseText);

					using (BingoConn db = new BingoConn())
					{
						string uuid = "";
						int iTotal = 0;

						//Insert Main table
						SBOGameMain sBOGameMain = new SBOGameMain()
						{
							Id = sMainID,
							ServerId = result.ServerId,
							errId = result.error.id,
							msg = result.error.msg,
							startTime = Convert.ToDateTime(startTime),
							endTime = Convert.ToDateTime(endTime),
							CreatedDate = DateTime.Now
						};
						db.SBOGameMains.Add(sBOGameMain);
						db.SaveChanges();

						//Insert Result table
						foreach (var p in result.result)
						{
							iTotal = result.result.Count;
							bool isInsert = IsBetIDNotExist(p.refNo);

							if (isInsert == true)
							{
								uuid = Guid.NewGuid().ToString();

								SBOGame sBOGame = new SBOGame()
								{
									Id = uuid,
									fId = sMainID,
									refNo = p.refNo,
									username = p.username,
									sportsType = p.sportsType,
									orderTime = p.orderTime,
									winLostDate = p.winLostDate,
									modifyDate = p.modifyDate,
									odds = p.odds,
									oddsstyle = p.oddsStyle,
									stake = p.stake,
									actualStake = p.actualStake,
									currency = p.currency,
									status = p.status,
									winlost = p.winLost,
									turnover = p.turnover,
									isHalfWonLose = p.isHalfWonLose,
									isLive = p.isLive,
									maxWinWithoutActualStake = p.maxWinWithoutActualStake,
									ip = p.ip,
									Total = iTotal,
									ModifiedDate = DateTime.Now,
									CreatedDate = DateTime.Now
								};
								db.SBOGames.Add(sBOGame);
								db.SaveChanges();

								foreach (var x in p.subBet)
								{
									string uuidSub = Guid.NewGuid().ToString();

									SBOGameSub sBOGameSub = new SBOGameSub()
									{
										Id = uuidSub,
										fId = sMainID,
										refNo = p.refNo,
										betOption = x.betOption,
										marketType = x.marketType,
										hdp = x.hdp,
										odds = x.odds,
										league = x.league,
										match = x.match,
										status = x.status,
										winLostDate = x.winLostDate,
										liveScore = x.liveScore,
										htScore = x.htScore,
										ftScore = x.ftScore,
										customeizedBetType = x.customeizedBetType
									};
									db.SBOGameSubs.Add(sBOGameSub);
									db.SaveChanges();
								}
							}
							else
							{
								var xyz = db.SBOGames.Where(s => s.refNo == p.refNo).FirstOrDefault();
								if (xyz != null)
								{
									xyz.winLostDate = p.winLostDate;
									xyz.modifyDate = p.modifyDate;
									xyz.stake = p.stake;
									xyz.actualStake = p.actualStake;
									xyz.status = p.status;
									xyz.winlost = p.winLost;
									xyz.turnover = p.turnover;
									xyz.isHalfWonLose = p.isHalfWonLose;
									xyz.isLive = p.isLive;
									xyz.maxWinWithoutActualStake = p.maxWinWithoutActualStake;
									xyz.ModifiedDate = DateTime.Now;

									db.Entry(xyz).State = System.Data.Entity.EntityState.Modified;
									db.SaveChanges();
								}

								foreach (var x in p.subBet)
								{
									var lsSub = db.SBOGameSubs.Where(s => s.refNo == p.refNo & s.betOption == x.betOption).FirstOrDefault();
									if (lsSub != null)
									{
										lsSub.betOption = x.betOption;
										lsSub.status = x.status;
										lsSub.winLostDate = x.winLostDate;
										lsSub.htScore = x.htScore;
										lsSub.ftScore = x.ftScore;

										db.Entry(lsSub).State = System.Data.Entity.EntityState.Modified;
										db.SaveChanges();
									}
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
				_common.LogError("SBO/GetGameRecordAll" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "SBO");
				return false;
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
						DateTime dtSTime = Convert.ToDateTime(query.LastRunTillDate?.ToString("yyyy-MM-dd HH:mm"));
						sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					}
					else
					{
						DateTime dtSTime = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm")) - new TimeSpan(0, 12, 3, 0, 0);
						sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					}

					//if (action == "Start")
					//{
					//	DateTime dtSTime = Convert.ToDateTime(query.LastRunTillDate);
					//	sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					//}
					//else
					//{
					//	DateTime dtSTime = DateTime.Now - new TimeSpan(0, 12, 3, 0);
					//	sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					//}
				}

				return sTime;

			}
			catch (Exception ex)
			{
				_common.LogError("SBO/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "SBO");
				return "Error:" + ex.Message.ToString();
			}
		}

		public bool IsBetIDNotExist(string drefNo)
		{
			try
			{
				string sRefNo = "";

				using (var bingoConn = new BingoConn())
				{
					var query = bingoConn.SBOGames
															.Where(s => s.refNo == drefNo)
															.FirstOrDefault();
					if (query != null)
					{
						sRefNo = query.refNo.ToString();
					}
				}

				if (sRefNo == "")
				{
					return true;
				}
				else
				{
					return false;
				}

			}
			catch (Exception ex)
			{
				_common.LogError("SBO/IsBetIDNotExist" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "SBO");
				return false;
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
				_common.LogError("SBO/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "SBO");
				return false;
			}
		}

		#endregion


		// GET: SBO
		public void SBOJob()
		{
			try
			{
				bool isOK = GetGameRecord();
				//bool isOK = GetGameRecordAll();
			}
			catch (Exception ex)
			{
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "IG");
			}
		}
	}
}