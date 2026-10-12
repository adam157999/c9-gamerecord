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
	public class UGController : Controller
	{
		private string P_ApiUrl = "http://accessapi.ugamingservice.com";
		private string P_CompKey = "M3Z7BMjcHe";
		private string P_APIKey = "BYpneRzWF3DNPDiHHYcrk3shDWEycw6Y"; // eDjJMi3dYKS8xpBAWkdKhssn5jG4JCDX
		Common _common = new Common();

		//#########################################################################################################
		#region Declare 

		public class SubBet
		{
			public string BetID { get; set; }
			public long SubID { get; set; }
			public int SportID { get; set; }
			public int LeagueID { get; set; }
			public int MatchID { get; set; }
			public int HomeID { get; set; }
			public int AwayID { get; set; }
			public int Stage { get; set; }
			public int MarketID { get; set; }
			public double Odds { get; set; }
			public double Hdp { get; set; }
			public int HomeScore { get; set; }
			public int AwayScore { get; set; }
			public int HomeCard { get; set; }
			public int AwayCard { get; set; }
			public int BetPos { get; set; }
			public int OutLevel { get; set; }
			public int Result { get; set; }
			public int Status { get; set; }
		}

		public class InsBet
		{
			public long InsID { get; set; }
			public double BetValue { get; set; }
			public double BackValue { get; set; }
			public DateTime BetDate { get; set; }
			public double Win { get; set; }
		}

		public class Datum
		{
			public string BetID { get; set; }
			public string Account { get; set; }
			public double Payout { get; set; }
			public double BetAmount { get; set; }
			public double DeductAmount { get; set; }
			public double AllWin { get; set; }
			public double Turnover { get; set; }
			public double BetOdds { get; set; }
			public double Win { get; set; }
			public string OddsStyle { get; set; }
			public DateTime BetDate { get; set; }
			public int Status { get; set; }
			public int Result { get; set; }
			public DateTime ReportDate { get; set; }
			public string BetIP { get; set; }
			public DateTime UpdateTime { get; set; }
			public long AgentID { get; set; }
			public string GroupComm { get; set; }
			public int MpID { get; set; }
			public string Currency { get; set; }
			public int BetWay { get; set; }
			public long SortNo { get; set; }
			public List<SubBet> SubBets { get; set; }
			public List<InsBet> InsBets { get; set; }
		}

		public class GameLogVM
		{
			public List<Datum> Data { get; set; }
			public string ErrorCode { get; set; }
			public string ErrorMessage { get; set; }
		}

		#endregion

		//#########################################################################################################
		#region

		public bool GetGameRecord()
		{
			string sMainID = Guid.NewGuid().ToString();
			long Sort = GetJobSortNo();

			try
			{
				int Rows = 1000;
				string webAddr = P_ApiUrl + "/SportApi/GetBetSheetBySort";

				var httpWebRequest = (HttpWebRequest)WebRequest.Create(webAddr);
				httpWebRequest.ContentType = "application/json";
				httpWebRequest.Method = "POST";
				httpWebRequest.Timeout = 60000;
				httpWebRequest.ReadWriteTimeout = 60000;

				using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
				{
					string json = "{ \"CompanyKey\" : \"" + P_CompKey + "\", \"APIPassword\" : \"" + P_APIKey + "\" , \"Sort\" : " + Sort + " , \"Rows\" : " + Rows + "}";


					streamWriter.Write(json);
					streamWriter.Flush();
				}
				var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
				using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
				{
					var responseText = streamReader.ReadToEnd();
					GameLogVM result = JsonConvert.DeserializeObject<GameLogVM>(responseText);

					using (BingoConn db = new BingoConn())
					{
						string uuid = "";
						long lSortNo = 0;
						int iTotal = 0;

						//Insert Main table
						UGGameMain uGGameMain = new UGGameMain()
						{
							Id = sMainID,
							ErrorCode = result.ErrorCode,
							ErrorMessage = result.ErrorMessage,
							SortNo = lSortNo,
							CreatedDate = DateTime.Now
						};
						db.UGGameMains.Add(uGGameMain);
						db.SaveChanges();

						//Insert Result table
						foreach (var p in result.Data)
						{
							lSortNo = p.SortNo;
							iTotal = result.Data.Count;
							bool isInsert = IsBetIDNotExist(p.BetID);

							if (isInsert == true)
							{
								uuid = Guid.NewGuid().ToString();

								UGGame uGGame = new UGGame()
								{
									Id = uuid,
									fId = sMainID,
									BetID = p.BetID,
									Account = p.Account,
									Payout = p.Payout,
									BetAmount = p.BetAmount,
									DeductAmount = p.DeductAmount,
									AllWin = p.AllWin,
									Turnover = p.Turnover,
									BetOdds = p.BetOdds,
									Win = p.Win,
									OddsStyle = p.OddsStyle,
									BetDate = p.BetDate,
									Status = p.Status,
									Result = p.Result,
									ReportDate = p.ReportDate,
									BetIP = p.BetIP,
									UpdateTime = p.UpdateTime,
									AgentID = p.AgentID,
									GroupComm = p.GroupComm,
									MpID = p.MpID,
									Currency = p.Currency,
									BetWay = p.BetWay,
									SortNo = p.SortNo,
									Total = result.Data.Count,
									ModifiedDate = DateTime.Now,
									CreatedDate = DateTime.Now
								};
								db.UGGames.Add(uGGame);
								db.SaveChanges();

								foreach (var x in p.SubBets)
								{
									//string uuidSub = Guid.NewGuid().ToString();

									UGGameSub uGGameSub = new UGGameSub()
									{
										fId = sMainID,
										SubID = x.SubID,
										BetID = p.BetID,
										SportID = x.SportID,
										LeagueID = x.LeagueID,
										MatchID = x.MatchID,
										HomeID = x.HomeID,
										AwayID = x.AwayID,
										Stage = x.Stage,
										MarketID = x.MarketID,
										Odds = x.Odds,
										Hdp = x.Hdp,
										HomeScore = x.HomeScore,
										AwayScore = x.AwayScore,
										HomeCard = x.HomeCard,
										AwayCard = x.AwayCard,
										BetPos = x.BetPos,
										OutLevel = x.OutLevel,
										Result = x.Result,
										Status = x.Status
									};
									db.UGGameSubs.Add(uGGameSub);
									db.SaveChanges();
								}

								foreach (var y in p.InsBets)
								{
									//string uuidIns = Guid.NewGuid().ToString();

									UGGameIn uGGameIn = new UGGameIn()
									{
										fId = sMainID,
										InsID = y.InsID,
										BetID = p.BetID,
										BetDate = y.BetDate,
										BackValue = y.BackValue,
										BetValue = y.BetValue,
										Win = y.Win
									};
									db.UGGameIns.Add(uGGameIn);
									db.SaveChanges();
								}
							}
							else
							{
								var xyz = db.UGGames.Where(s => s.BetID == p.BetID).FirstOrDefault();
								if (xyz != null)
								{
									xyz.Payout = p.Payout;
									xyz.BetAmount = p.BetAmount;
									xyz.DeductAmount = p.DeductAmount;
									xyz.AllWin = p.AllWin;
									xyz.Turnover = p.Turnover;
									xyz.BetOdds = p.BetOdds;
									xyz.Win = p.Win;
									xyz.OddsStyle = p.OddsStyle;
									xyz.Status = p.Status;
									xyz.Result = p.Result;
									xyz.ReportDate = p.ReportDate;
									xyz.UpdateTime = p.UpdateTime;
									xyz.GroupComm = p.GroupComm;
									xyz.MpID = p.MpID;
									xyz.BetWay = p.BetWay;
									xyz.SortNo = p.SortNo;
									xyz.ModifiedDate = xyz.Status != p.Status ? DateTime.Now : xyz.ModifiedDate;

									db.Entry(xyz).State = System.Data.Entity.EntityState.Modified;
									db.SaveChanges();
								}

								foreach (var x in p.SubBets)
								{
									var lsSub = db.UGGameSubs.Where(s => s.BetID == p.BetID).FirstOrDefault();
									if (lsSub != null)
									{
										lsSub.Stage = x.Stage;
										lsSub.MarketID = x.MarketID;
										lsSub.Odds = x.Odds;
										lsSub.Hdp = x.Hdp;
										lsSub.HomeScore = x.HomeScore;
										lsSub.AwayScore = x.AwayScore;
										lsSub.HomeCard = x.HomeCard;
										lsSub.AwayCard = x.AwayCard;
										lsSub.BetPos = x.BetPos;
										lsSub.Result = x.Result;
										lsSub.Status = x.Status;

										db.Entry(lsSub).State = System.Data.Entity.EntityState.Modified;
										db.SaveChanges();
									}
								}

								foreach (var y in p.InsBets)
								{
									var lsIns = db.UGGameIns.Where(s => s.BetID == p.BetID).FirstOrDefault();
									if (lsIns != null)
									{
										lsIns.BackValue = y.BackValue;
										lsIns.BetValue = y.BetValue;
										lsIns.Win = y.Win;

										db.Entry(lsIns).State = System.Data.Entity.EntityState.Modified;
										db.SaveChanges();
									}
								}
							}
						}

						UGGameJob uGGameJob = new UGGameJob()
						{
							Id = sMainID,
							SortNo = lSortNo,
							Total = iTotal,
							CreatedDate = DateTime.Now
						};
						db.UGGameJobs.Add(uGGameJob);
						db.SaveChanges();
					}
					//json format display
					//string json = JsonConvert.SerializeObject(result);
					//string jsonFormatted = Newtonsoft.Json.Linq.JValue.Parse(json).ToString(Formatting.Indented);
				}

				return true;
			}
			catch (WebException ex)
			{
				_common.LogError("UG/GetGameRecord WebExp" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "UG");
				return false;

				//using (var streamReader = new StreamReader(ex.Response.GetResponseStream()))
				//{
				//    var responseText = streamReader.ReadToEnd();
				//    GameLogVM result = JsonConvert.DeserializeObject<GameLogVM>(responseText);

				//    using (BingoConn dbConn = new BingoConn())
				//    {
				//        string errID = Guid.NewGuid().ToString();

				//        UGGameMain uGGameMain = new UGGameMain()
				//        {
				//            Id = errID,
				//            ErrorCode = result.ErrorCode,
				//            ErrorMessage = result.ErrorMessage,
				//            CreatedDate = DateTime.Now
				//        };
				//        dbConn.UGGameMains.Add(uGGameMain);
				//        dbConn.SaveChanges();
				//    }
				//    return false;
				//}
			}
			catch (Exception ex)
			{
				_common.LogError("UG/GetGameRecord" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "UG");
				return false;

				//using (BingoConn dbConn = new BingoConn())
				//{
				//    string errID = Guid.NewGuid().ToString();

				//    UGGameMain uGGameMain = new UGGameMain()
				//    {
				//        Id = errID,
				//        ExMessage = "UG GetGameRecord Error: " + ex.Message,
				//        CreatedDate = DateTime.Now
				//    };
				//    dbConn.UGGameMains.Add(uGGameMain);
				//    dbConn.SaveChanges();
				//}
				//return false;
			}
		}

		public bool GetGameRecordByBetID(string betID)
		{
			string sMainID = Guid.NewGuid().ToString();

			try
			{
				string webAddr = P_ApiUrl + "/SportApi/GetBetSheetByBetID";

				var httpWebRequest = (HttpWebRequest)WebRequest.Create(webAddr);
				httpWebRequest.ContentType = "application/json";
				httpWebRequest.Method = "POST";
				httpWebRequest.Timeout = 60000;
				httpWebRequest.ReadWriteTimeout = 60000;

				using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
				{
					string json = "{ \"CompanyKey\" : \"" + P_CompKey + "\", \"APIPassword\" : \"" + P_APIKey + "\" , \"BetID\" : \"" + betID + "\" }";


					streamWriter.Write(json);
					streamWriter.Flush();
				}
				var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
				using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
				{
					var responseText = streamReader.ReadToEnd();
					GameLogVM result = JsonConvert.DeserializeObject<GameLogVM>(responseText);

					using (BingoConn db = new BingoConn())
					{
						string uuid = "";
						long lSortNo = 0;
						int iTotal = 0;

						//Insert Main table
						UGGameMain uGGameMain = new UGGameMain()
						{
							Id = sMainID,
							ErrorCode = result.ErrorCode,
							ErrorMessage = result.ErrorMessage,
							SortNo = lSortNo,
							CreatedDate = DateTime.Now
						};
						db.UGGameMains.Add(uGGameMain);
						db.SaveChanges();

						foreach (var p in result.Data)
						{
							uuid = Guid.NewGuid().ToString();
							lSortNo = p.SortNo;
							iTotal = result.Data.Count;

							UGGame uGGame = new UGGame()
							{
								Id = uuid,
								BetID = p.BetID,
								Account = p.Account,
								Payout = p.Payout,
								BetAmount = p.BetAmount,
								DeductAmount = p.DeductAmount,
								AllWin = p.AllWin,
								Turnover = p.Turnover,
								BetOdds = p.BetOdds,
								Win = p.Win,
								OddsStyle = p.OddsStyle,
								BetDate = p.BetDate,
								Status = p.Status,
								Result = p.Result,
								ReportDate = p.ReportDate,
								BetIP = p.BetIP,
								UpdateTime = p.UpdateTime,
								AgentID = p.AgentID,
								GroupComm = p.GroupComm,
								MpID = p.MpID,
								Currency = p.Currency,
								BetWay = p.BetWay,
								SortNo = p.SortNo,
								Total = result.Data.Count,
								CreatedDate = DateTime.Now
							};
							db.UGGames.Add(uGGame);
							db.SaveChanges();

							foreach (var x in p.SubBets)
							{
								UGGameSub uGGameSub = new UGGameSub()
								{
									fId = sMainID,
									SubID = x.SubID,
									BetID = p.BetID,
									SportID = x.SportID,
									LeagueID = x.LeagueID,
									MatchID = x.MatchID,
									HomeID = x.HomeID,
									AwayID = x.AwayID,
									Stage = x.Stage,
									MarketID = x.MarketID,
									Odds = x.Odds,
									Hdp = x.Hdp,
									HomeScore = x.HomeScore,
									AwayScore = x.AwayScore,
									HomeCard = x.HomeCard,
									AwayCard = x.AwayCard,
									BetPos = x.BetPos,
									OutLevel = x.OutLevel,
									Result = x.Result,
									Status = x.Status
								};
								db.UGGameSubs.Add(uGGameSub);
								db.SaveChanges();
							}

							foreach (var y in p.InsBets)
							{
								UGGameIn uGGameIn = new UGGameIn()
								{
									fId = sMainID,
									InsID = y.InsID,
									BetID = p.BetID,
									BetDate = y.BetDate,
									BackValue = y.BackValue,
									BetValue = y.BetValue,
									Win = y.Win
								};
								db.UGGameIns.Add(uGGameIn);
								db.SaveChanges();
							}


						}

						//    UGGameJob uGGameJob = new UGGameJob()
						//    {
						//        Id = uuid,
						//        SortNo = lSortNo,
						//        Total = iTotal,
						//        CreatedDate = DateTime.Now
						//    };
						//    db.UGGameJobs.Add(uGGameJob);
						//    db.SaveChanges();
					}

				}

				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("UG/GetGameRecordByBetID Exp" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "UG");
				return false;
				//using (BingoConn dbConn = new BingoConn())
				//{
				//    string errID = Guid.NewGuid().ToString();

				//    UGGameMain uGGameMain = new UGGameMain()
				//    {
				//        Id = errID,
				//        ExMessage = "UG GetGameRecordByBetID Error: " + ex.Message,
				//        CreatedDate = DateTime.Now
				//    };
				//    dbConn.UGGameMains.Add(uGGameMain);
				//    dbConn.SaveChanges();
				//}
				//return false;
			}
		}

		public long GetJobSortNo()
		{
			try
			{
				long lTime = 0;

				using (var bingoConn = new BingoConn())
				{
					var query = bingoConn.UGGameJobs
															.OrderByDescending(s => s.CreatedDate)
															.FirstOrDefault();

					lTime = Convert.ToInt64(query.SortNo);
				}

				return lTime;
			}
			catch (Exception ex)
			{
				_common.LogError("UG/GetJobSortNo" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "UG");
				return -999;

				//using (BingoConn dbConn = new BingoConn())
				//{
				//    string errID = Guid.NewGuid().ToString();

				//    UGGameMain uGGameMain = new UGGameMain()
				//    {
				//        Id = errID,
				//        ExMessage = "UG GetJobSortNo Error: " + ex.Message,
				//        CreatedDate = DateTime.Now
				//    };
				//    dbConn.UGGameMains.Add(uGGameMain);
				//    dbConn.SaveChanges();
				//}
				//return -999;
			}
		}

		public bool IsBetIDNotExist(string betID)
		{
			try
			{
				string sBetID = "";

				using (var bingoConn = new BingoConn())
				{
					var query = bingoConn.UGGames
															.Where(s => s.BetID == betID)
															.FirstOrDefault();
					if (query != null)
					{
						sBetID = query.BetID.ToString();
					}
				}

				if (sBetID == "")
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
				_common.LogError("UG/IsBetIDNotExist" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "UG");
				return false;

			}
		}

		#endregion

		// GET: UG
		public void UGJob()
		{
			try
			{
				bool isOK = GetGameRecord();
				//bool isOK = GetGameRecordByBetID("SP294561618");
			}
			catch (Exception ex)
			{
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "UG");
			}
		}
	}
}