using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using GameRecord.Models;

namespace GameRecord.Controllers
{
	public class AppGameRecController : ApiController
	{
		public static readonly object MyLock = new object();
		public string BingoConn = System.Configuration.ConfigurationManager.ConnectionStrings["BingoConn1"].ToString();

		//##########################################################################################################################
		#region  Declaration
		public class GameLogVM
		{
			public int total { get; set; }
			public GameLogDataVM data { get; set; }
		}

		public class GameLogDataVM
		{
			public DateTime OrderTime { get; set; }
			public string GameName { get; set; }
			public decimal OrderBet { get; set; }
			public decimal WinLose { get; set; }
		}

		#endregion

		[HttpGet]
		public IHttpActionResult GetGameLog(string UID, string startTime, string endTime, string game)
		{
			lock (MyLock)
			{
				try
				{
					InsertLog("GetGameLog", "GetGameLog", "UID", UID, "startTime", startTime, "endTime", endTime, "game", game);

					DateTime dtStart = Convert.ToDateTime(startTime);
					DateTime dtEnd = Convert.ToDateTime(endTime);
					game = game.ToUpper();

					int iTotal = 0;
					List<GameLogDataVM> dataVMs = new List<GameLogDataVM>();
					string UUID = UID.ToUpper().Trim();
					int PageSize = 2000;

					//BG MYR
					//if (game == "BG")
					//{
					//	var ds = dtStart.AddHours(-12);
					//	var de = dtEnd.AddHours(-12);

					//	using (var db = new BingoConn())
					//	{
					//		var query = db.BGGames
					//								.Where(s => s.loginId.ToUpper() == UUID && (s.orderTime >= ds && s.orderTime <= de))
					//								.OrderByDescending(s => s.orderTime)
					//								.ToList();

					//		if (query != null)
					//		{
					//			iTotal = query.Count;

					//			foreach (var p in query)
					//			{
					//				GameLogDataVM betData = new GameLogDataVM();
					//				betData.OrderTime = Convert.ToDateTime(p.orderTime).AddHours(12);
					//				betData.GameName = p.gameName;
					//				betData.OrderBet = Convert.ToDecimal(p.validBet.ToString()); // validBet, if use bAmount, need -ve
					//				betData.WinLose = Convert.ToDecimal((p.payment + p.validBet).ToString());    // validBet + payment
					//				dataVMs.Add(betData);
					//			}

					//		}
					//	}
					//}

					//BG CNY
					if (game == "BG")
					{
						var ds = dtStart.AddHours(-12);
						var de = dtEnd.AddHours(-12);

						using (var db = new BingoConn())
						{
							var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == "BG" && x.PlayerID.ToUpper() == UUID).Select(x => x.GamePlayerID.ToUpper()).ToArray();

							var query = db.BGGames
													.Where(s => record.Contains(s.loginId.ToUpper()) && (s.orderTime >= ds && s.orderTime <= de))
													.OrderByDescending(s => s.orderTime).Take(PageSize)
													.ToList();

							if (query != null)
							{
								iTotal = query.Count;

								foreach (var p in query)
								{
									GameLogDataVM betData = new GameLogDataVM();
									betData.OrderTime = Convert.ToDateTime(p.orderTime).AddHours(12);
									betData.GameName = p.gameName;
									betData.OrderBet = Convert.ToDecimal(p.validBet.ToString()); // validBet, if use bAmount, need -ve
									betData.WinLose = Convert.ToDecimal((p.payment + p.validBet).ToString());    // validBet + payment
									dataVMs.Add(betData);
								}
							}
						}
					}

					//918KISS
					if (game == "918KISS")
					{

						using (var db = new BingoConn())
						{
							var query = db.KISGameRecords
													.Where(s => s.username.ToUpper() == UUID && (s.CreateTime >= dtStart && s.CreateTime <= dtEnd))
													.OrderByDescending(s => s.CreateTime).Take(PageSize)
													.ToList();

							if (query != null)
							{
								iTotal = query.Count;

								foreach (var p in query)
								{
									GameLogDataVM betData = new GameLogDataVM();
									betData.OrderTime = Convert.ToDateTime(p.CreateTime);
									betData.GameName = p.GameName;
									betData.OrderBet = Convert.ToDecimal(p.bet.ToString());
									betData.WinLose = Convert.ToDecimal(p.Win.ToString()) - Convert.ToDecimal(p.bet.ToString());
									dataVMs.Add(betData);
								}

							}
						}
					}

					//IG
					if (game == "IG")
					{
						using (var db = new BingoConn())
						{
							var query = db.IGGames
													.Where(s => s.UserName.ToUpper() == "BT_" + UUID && (s.OrderTime >= dtStart && s.OrderTime <= dtEnd))
													.OrderByDescending(s => s.OrderTime).Take(PageSize)
													.ToList();

							if (query != null)
							{
								iTotal = query.Count;

								foreach (var p in query)
								{
									GameLogDataVM betData = new GameLogDataVM();
									betData.OrderTime = Convert.ToDateTime(p.OrderTime);
									betData.GameName = p.GameName;
									betData.OrderBet = Convert.ToDecimal(p.BetCoin.ToString());
									betData.WinLose = Convert.ToDecimal(p.WinAmount.ToString());
									dataVMs.Add(betData);
								}

							}
						}
					}

					//BT28Win
					if (game == "BT28WIN")
					{
						using (var db = new BingoConn())
						{
							var query = db.BT28WinGame
													.Where(s => s.LoginID.ToUpper() == UUID && (s.OrderDateTime >= dtStart && s.OrderDateTime <= dtEnd))
													.OrderByDescending(s => s.OrderDateTime).Take(PageSize)
													.ToList();

							if (query != null)
							{
								iTotal = query.Count;

								foreach (var p in query)
								{
									GameLogDataVM betData = new GameLogDataVM();
									betData.OrderTime = Convert.ToDateTime(p.OrderDateTime);
									betData.GameName = "Lotte";
									betData.OrderBet = Convert.ToDecimal(p.BetAmount.ToString());
									betData.WinLose = Convert.ToDecimal(p.WinLoss.ToString()) + Convert.ToDecimal(p.BetAmount.ToString());
									dataVMs.Add(betData);
								}

							}
						}
					}

					//AG MYR
					//if (game == "AG")
					//{
					//	var ds = dtStart.AddHours(-12);
					//	var de = dtEnd.AddHours(-12);

					//	using (var db = new BingoConn())
					//	{
					//		var query = db.AGGames
					//								.Where(s => s.playerName.ToUpper() == UUID && (s.betTime >= ds && s.betTime <= de))
					//								.OrderByDescending(s => s.betTime)
					//								.ToList();

					//		if (query != null)
					//		{
					//			iTotal = query.Count;

					//			foreach (var p in query)
					//			{
					//				GameLogDataVM betData = new GameLogDataVM();
					//				betData.OrderTime = Convert.ToDateTime(Convert.ToDateTime(p.betTime).AddHours(12));
					//				betData.GameName = p.gameType;
					//				betData.OrderBet = Convert.ToDecimal(p.validBetAmount.ToString());
					//				betData.WinLose = Convert.ToDecimal((p.netAmount + p.validBetAmount).ToString());
					//				dataVMs.Add(betData);
					//			}

					//		}
					//	}
					//}

					//AG
					if (game == "AG")
					{
						var ds = dtStart.AddHours(-12);
						var de = dtEnd.AddHours(-12);

						using (var db = new BingoConn())
						{
							var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == "AG" && x.PlayerID.ToUpper() == UUID).Select(x => x.GamePlayerID.ToUpper()).ToArray();

							var query = db.AGGames
													.Where(s => record.Contains(s.playerName.ToUpper()) && (s.betTime >= ds && s.betTime <= de))
													.OrderByDescending(s => s.betTime).Take(PageSize)
													.ToList();

							if (query != null)
							{
								iTotal = query.Count;

								foreach (var p in query)
								{
									GameLogDataVM betData = new GameLogDataVM();
									betData.OrderTime = Convert.ToDateTime(Convert.ToDateTime(p.betTime).AddHours(12));
									betData.GameName = p.gameType;
									betData.OrderBet = Convert.ToDecimal(p.validBetAmount.ToString());
									betData.WinLose = Convert.ToDecimal(p.netAmount.ToString());
									dataVMs.Add(betData);
								}
							}
						}
					}

					//BKA
					if (game == "BKA")
					{
						using (var db = new BingoConn())
						{
							var query = db.BKAGames
													.Where(s => s.Account.ToUpper() == UUID && (s.DateTime >= dtStart && s.DateTime <= dtEnd))
													.OrderByDescending(s => s.DateTime).Take(PageSize)
													.ToList();

							if (query != null)
							{
								iTotal = query.Count;

								foreach (var p in query)
								{
									GameLogDataVM betData = new GameLogDataVM();
									betData.OrderTime = Convert.ToDateTime(p.DateTime);
									betData.GameName = p.Title;
									betData.OrderBet = Convert.ToDecimal(p.Amount.ToString());
									betData.WinLose = Convert.ToDecimal((p.WinLoss - p.Amount).ToString());
									dataVMs.Add(betData);
								}

							}
						}
					}

					//TPG
					if (game == "TPG")
					{
						using (var db = new BingoConn())
						{
							var query = db.TPGGames
													.Where(s => s.Username.ToUpper() == UUID && (s.CreatedAt >= dtStart && s.CreatedAt <= dtEnd) && s.GameType != 10)
													//.Where(s => s.Username.ToUpper() == UUID && (s.CreatedAt >= dtStart && s.CreatedAt <= dtEnd))
													.OrderByDescending(s => s.CreatedAt).Take(PageSize)
													.ToList();

							if (query != null)
							{
								iTotal = query.Count;

								foreach (var p in query)
								{
									GameLogDataVM betData = new GameLogDataVM();
									betData.OrderTime = Convert.ToDateTime(p.CreatedAt);
									betData.GameName = p.GameName;
									betData.OrderBet = Convert.ToDecimal(p.TotalBetAmount.ToString());
									betData.WinLose = Convert.ToDecimal((p.TotalPayoutAmount - p.TotalBetAmount).ToString());
									dataVMs.Add(betData);
								}

							}
						}
					}

					//PT
					if (game == "PT")
					{
						using (var db = new BingoConn())
						{
							var query = db.PTGames
													.Where(s => s.PlayerName.ToUpper() == "MCY_" + UUID && (s.GameDate >= dtStart && s.GameDate <= dtEnd))
													.OrderByDescending(s => s.GameDate).Take(PageSize)
													.ToList();

							if (query != null)
							{
								iTotal = query.Count;

								foreach (var p in query)
								{
									GameLogDataVM betData = new GameLogDataVM();
									betData.OrderTime = Convert.ToDateTime(p.GameDate);
									betData.GameName = p.GameName;
									betData.OrderBet = Convert.ToDecimal(p.Bet.ToString());
									betData.WinLose = Convert.ToDecimal((p.Win - p.Bet).ToString());
									dataVMs.Add(betData);
								}

							}
						}
					}

					//AllCity
					if (game == "ALLCITY")
					{
						using (var db = new BingoConn())
						{
							var query = db.ACGames
													.Where(s => s.Account.ToUpper() == "ML" + UUID && (s.time >= dtStart && s.time <= dtEnd))
													.OrderByDescending(s => s.time).Take(PageSize)
													.ToList();

							if (query != null)
							{
								iTotal = query.Count;

								foreach (var p in query)
								{
									GameLogDataVM betData = new GameLogDataVM();
									betData.OrderTime = Convert.ToDateTime(p.time);
									betData.GameName = p.GameCode;
									betData.OrderBet = Convert.ToDecimal(p.TotalPlay.ToString());
									betData.WinLose = Convert.ToDecimal((p.TotalWin - p.TotalPlay).ToString());
									dataVMs.Add(betData);
								}

							}
						}
					}

					//IBC
					if (game == "IBC")
					{
						var ds = dtStart.AddHours(-12);
						var de = dtEnd.AddHours(-12);

						using (var db = new BingoConn())
						{
							var bd = db.IBCBetDetails.Select(s => new { s.transaction_time, s.sport_name, s.stake, s.winlost_amount, s.vendor_member_id })
													.Where(s => s.vendor_member_id.ToUpper() == "MANCITY" + UUID && (s.transaction_time >= ds && s.transaction_time <= de)).Take(PageSize)
													.ToList();

							var bn = db.IBCBetNumberDetails.Select(s => new { s.transaction_time, s.sport_name, s.stake, s.winlost_amount, s.vendor_member_id })
													.Where(s => s.vendor_member_id.ToUpper() == "MANCITY" + UUID && (s.transaction_time >= ds && s.transaction_time <= de)).Take(PageSize)
													.ToList();

							var vs = db.IBCBetVirtualSportDetails.Select(s => new { s.transaction_time, s.sport_name, s.stake, s.winlost_amount, s.vendor_member_id })
											.Where(s => s.vendor_member_id.ToUpper() == "MANCITY" + UUID && (s.transaction_time >= ds && s.transaction_time <= de)).Take(PageSize)
											.ToList();

							var query = bd.Union(bn).Union(vs).ToList();

							if (query != null)
							{
								iTotal = query.Count;

								foreach (var p in query)
								{
									GameLogDataVM betData = new GameLogDataVM();
									betData.OrderTime = Convert.ToDateTime(p.transaction_time).AddHours(12);
									betData.GameName = p.sport_name;
									betData.OrderBet = Convert.ToDecimal(p.stake.ToString());
									betData.WinLose = Convert.ToDecimal((p.winlost_amount).ToString());
									dataVMs.Add(betData);
								}

							}
						}
					}

					//EVO888
					if (game == "EVO888")
					{
						var ds = dtStart.AddHours(-8);
						var de = dtEnd.AddHours(-8);

						using (var db = new BingoConn())
						{
							var query = db.Evo888Game
													.Where(s => s.UserName.ToUpper() == UUID && (s.DateTime >= ds && s.DateTime <= de))
													.OrderByDescending(s => s.DateTime).Take(PageSize)
													.ToList();

							if (query != null)
							{
								iTotal = query.Count;

								foreach (var p in query)
								{
									GameLogDataVM betData = new GameLogDataVM();
									betData.OrderTime = Convert.ToDateTime(Convert.ToDateTime(p.DateTime).AddHours(8));
									betData.GameName = p.GameName;
									betData.OrderBet = Convert.ToDecimal(p.Bet.ToString());
									betData.WinLose = Convert.ToDecimal((p.Win - p.Bet).ToString());
									dataVMs.Add(betData);
								}

							}
						}
					}

					//TPGFish
					if (game == "TPGFISH")
					{
						using (var db = new BingoConn())
						{
							var query = db.TPGGames
													.Where(s => s.Username.ToUpper() == UUID && (s.CreatedAt >= dtStart && s.CreatedAt <= dtEnd) && s.GameType == 10)
													.OrderByDescending(s => s.CreatedAt).Take(PageSize)
													.ToList();

							if (query != null)
							{
								iTotal = query.Count;

								foreach (var p in query)
								{
									GameLogDataVM betData = new GameLogDataVM();
									betData.OrderTime = Convert.ToDateTime(p.CreatedAt);
									betData.GameName = p.GameName;
									betData.OrderBet = Convert.ToDecimal(p.TotalBetAmount.ToString());
									betData.WinLose = Convert.ToDecimal((p.TotalPayoutAmount - p.TotalBetAmount).ToString());
									dataVMs.Add(betData);
								}

							}
						}
					}

					//VPOWER
					if (game == "VPOWER")
					{
						using (var db = new BingoConn())
						{
							var query = db.VPowerGames
													.Where(s => s.username.ToUpper() == UUID && (s.time >= dtStart && s.time <= dtEnd))
													.OrderByDescending(s => s.time).Take(PageSize)
													.ToList();

							if (query != null)
							{
								iTotal = query.Count;

								foreach (var p in query)
								{
									GameLogDataVM betData = new GameLogDataVM();
									betData.OrderTime = Convert.ToDateTime(p.time);
									betData.GameName = p.gameocode;
									betData.OrderBet = Convert.ToDecimal(p.pay.ToString());
									betData.WinLose = Convert.ToDecimal((p.win - p.pay).ToString());
									dataVMs.Add(betData);
								}

							}
						}
					}

					//SEXY
					if (game == "SEXYGAMING")
					{
						var ds = dtStart.AddHours(-8);
						var de = dtEnd.AddHours(-8);

						using (var db = new BingoConn())
						{
							var query = db.SexyGames
													.Where(s => s.UserName.ToUpper() == "MCM_" + UUID && (s.BetDate >= ds && s.BetDate <= de))
													.OrderByDescending(s => s.BetDate).Take(PageSize)
													.ToList();

							if (query != null)
							{
								iTotal = query.Count;

								foreach (var p in query)
								{
									GameLogDataVM betData = new GameLogDataVM();
									betData.OrderTime = Convert.ToDateTime(Convert.ToDateTime(p.BetDate).AddHours(8));
									betData.GameName = p.GameType;
									betData.OrderBet = Convert.ToDecimal(p.ActualStake);
									betData.WinLose = Convert.ToDecimal(p.Profit);
									dataVMs.Add(betData);
								}
							}
						}
					}

					//SPADE
					if (game == "SPADEGAMING")
					{
						using (var db = new BingoConn())
						{
							var query = db.SpadeGames
													.Where(s => s.acctId.ToUpper() == UUID && (s.ticketTime >= dtStart && s.ticketTime <= dtEnd))
													.OrderByDescending(s => s.ticketTime).Take(PageSize)
													.ToList();

							if (query != null)
							{
								iTotal = query.Count;

								foreach (var p in query)
								{
									GameLogDataVM betData = new GameLogDataVM();
									betData.OrderTime = Convert.ToDateTime(p.ticketTime);
									betData.GameName = p.gameCode;
									betData.OrderBet = Convert.ToDecimal(p.betAmount);
									betData.WinLose = Convert.ToDecimal(p.winLoss);
									dataVMs.Add(betData);
								}
							}
						}
					}

					//EVOLUTION
					if (game == "EVOLUTIONGAMING")
					{
						var ds = dtStart.AddHours(-8);
						var de = dtEnd.AddHours(-8);

						using (var db = new BingoConn())
						{
							var query = db.EvolutionGames
													.Where(s => s.playerId.ToUpper() == UUID && (s.startedAt >= ds && s.startedAt <= de))
													.OrderByDescending(s => s.startedAt).Take(PageSize)
													.ToList();

							if (query != null)
							{
								iTotal = query.Count;

								foreach (var p in query)
								{
									GameLogDataVM betData = new GameLogDataVM();
									betData.OrderTime = Convert.ToDateTime(Convert.ToDateTime(p.startedAt).AddHours(8));
									betData.GameName = p.gameType;
									betData.OrderBet = Convert.ToDecimal(p.wager);
									betData.WinLose = Convert.ToDecimal(p.payout) - Convert.ToDecimal(p.wager);
									dataVMs.Add(betData);
								}
							}
						}
					}

					//MENANG88
					if (game == "MENANG88")
					{
						using (var db = new BingoConn())
						{
							var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == "MENANG88" && x.GamePlayerID.ToUpper() == UUID).Select(x => x.PlayerID.ToUpper()).FirstOrDefault();

							var query = db.Menang88Game
													.Where(s => s.username.ToUpper() == record && (s.logdate >= dtStart && s.logdate <= dtEnd))
													.OrderByDescending(s => s.logdate).Take(PageSize)
													.ToList();

							if (query != null)
							{
								iTotal = query.Count;

								foreach (var p in query)
								{
									GameLogDataVM betData = new GameLogDataVM();
									betData.OrderTime = Convert.ToDateTime(p.logdate);
									betData.GameName = "Lotte";
									betData.OrderBet = Convert.ToDecimal(p.stake.ToString());
									betData.WinLose = Convert.ToDecimal(p.strike.ToString()) - Convert.ToDecimal(p.stake.ToString());
									dataVMs.Add(betData);
								}

							}
						}
					}

					//MEGA
					if (game == "MEGA")
					{
						using (var db = new BingoConn())
						{
							var query = db.MegaGames
													.Where(s => s.UserName.ToUpper() == UUID && (s.DateTime >= dtStart && s.DateTime <= dtEnd))
													.OrderByDescending(s => s.DateTime).Take(PageSize)
													.ToList();

							if (query != null)
							{
								iTotal = query.Count;

								foreach (var p in query)
								{
									GameLogDataVM betData = new GameLogDataVM();
									betData.OrderTime = Convert.ToDateTime(Convert.ToDateTime(p.DateTime));
									betData.GameName = p.GameName;
									betData.OrderBet = Convert.ToDecimal(p.Bet.ToString());
									betData.WinLose = Convert.ToDecimal((p.Win - p.Bet).ToString());
									dataVMs.Add(betData);
								}
							}
						}
					}

					//SABAESPORTS
					if (game == "SABAESPORTS")
					{
						var ds = dtStart.AddHours(-12);
						var de = dtEnd.AddHours(-12);

						using (var db = new BingoConn())
						{
							var bd = db.IBCBetDetails.Select(s => new { s.transaction_time, s.sport_name, s.stake, s.winlost_amount, s.vendor_member_id })
													.Where(s => s.vendor_member_id.ToUpper() == "MANCITY" + UUID && (s.transaction_time >= ds && s.transaction_time <= de)).Take(PageSize)
													.ToList();

							var bn = db.IBCBetNumberDetails.Select(s => new { s.transaction_time, s.sport_name, s.stake, s.winlost_amount, s.vendor_member_id })
													.Where(s => s.vendor_member_id.ToUpper() == "MANCITY" + UUID && (s.transaction_time >= ds && s.transaction_time <= de)).Take(PageSize)
													.ToList();

							var vs = db.IBCBetVirtualSportDetails.Select(s => new { s.transaction_time, s.sport_name, s.stake, s.winlost_amount, s.vendor_member_id })
											.Where(s => s.vendor_member_id.ToUpper() == "MANCITY" + UUID && (s.transaction_time >= ds && s.transaction_time <= de)).Take(PageSize)
											.ToList();

							var query = bd.Union(bn).Union(vs).ToList();

							if (query != null)
							{
								iTotal = query.Count;

								foreach (var p in query)
								{
									GameLogDataVM betData = new GameLogDataVM();
									betData.OrderTime = Convert.ToDateTime(p.transaction_time).AddHours(12);
									betData.GameName = p.sport_name;
									betData.OrderBet = Convert.ToDecimal(p.stake.ToString());
									betData.WinLose = Convert.ToDecimal((p.winlost_amount).ToString());
									dataVMs.Add(betData);
								}

							}
						}
					}

					//XE88
					if (game == "XE88")
					{
						using (var db = new BingoConn())
						{
							var query = db.Xe88Game
													.Where(s => s.username.ToUpper() == "MCT_" + UUID && (s.logtime >= dtStart && s.logtime <= dtEnd))
													.OrderByDescending(s => s.logtime).Take(PageSize)
													.ToList();

							if (query != null)
							{
								iTotal = query.Count;

								foreach (var p in query)
								{
									GameLogDataVM betData = new GameLogDataVM();
									betData.OrderTime = Convert.ToDateTime(Convert.ToDateTime(p.logtime));
									betData.GameName = p.gamename;
									betData.OrderBet = Convert.ToDecimal(p.bet.ToString());
									betData.WinLose = Convert.ToDecimal((p.win - p.bet).ToString());
									dataVMs.Add(betData);
								}
							}
						}
					}

					//KYPOKER
					//if (game == "KYPOKER")
					//{
					//	using (var db = new BingoConn())
					//	{
					//		var query = db.KYPokerGames
					//								.Where(s => s.Accounts.ToUpper() == "60602_" + UUID && (s.GameStartTime >= dtStart && s.GameStartTime <= dtEnd))
					//								.OrderByDescending(s => s.GameStartTime).Take(PageSize)
					//								.ToList();

					//		if (query != null)
					//		{
					//			iTotal = query.Count;

					//			foreach (var p in query)
					//			{
					//				GameLogDataVM betData = new GameLogDataVM();
					//				betData.OrderTime = Convert.ToDateTime(Convert.ToDateTime(p.GameStartTime));
					//				betData.GameName = Convert.ToString(p.KindID);
					//				betData.OrderBet = Convert.ToDecimal(p.CellScore.ToString());
					//				betData.WinLose = Convert.ToDecimal((p.Profit).ToString());
					//				dataVMs.Add(betData);
					//			}
					//		}
					//	}
					//}

					if (game == "KYPOKER")
					{
						using (var db = new BingoConn())
						{
							var _60602 = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == "KYPOKER" && x.PlayerID.ToUpper() == UUID && x.IsActive == false).Select(x => x.GamePlayerID.ToUpper()).FirstOrDefault();
							var _60016 = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == "KYPOKER" && x.PlayerID.ToUpper() == UUID && x.IsActive == true).Select(x => x.GamePlayerID.ToUpper()).FirstOrDefault();

							var query = db.KYPokerGames
													.Where(s => (s.Accounts.ToUpper() == "60602_" + _60602 || s.Accounts.ToUpper() == "60016_" + _60016) && (s.GameStartTime >= dtStart && s.GameStartTime <= dtEnd))
													.OrderByDescending(s => s.GameStartTime).Take(PageSize)
													.ToList();

							if (query != null)
							{
								iTotal = query.Count;

								foreach (var p in query)
								{
									GameLogDataVM betData = new GameLogDataVM();
									betData.OrderTime = Convert.ToDateTime(Convert.ToDateTime(p.GameStartTime));
									betData.GameName = Convert.ToString(p.KindID);
									betData.OrderBet = Convert.ToDecimal(p.CellScore.ToString());
									betData.WinLose = Convert.ToDecimal((p.Profit).ToString());
									dataVMs.Add(betData);
								}
							}
						}
					}


					#region Obselete
					////SBO
					//if (game == "SBO")
					//{
					//	using (var db = new BingoConn())
					//	{
					//		var query = db.SBOGames
					//								.Where(s => s.username.ToUpper() == UUID && (s.modifyDate >= dtStart && s.modifyDate <= dtEnd))
					//								.OrderBy(s => s.orderTime)
					//								.ToList();

					//		if (query != null)
					//		{
					//			iTotal = query.Count;

					//			foreach (var p in query)
					//			{
					//				GameLogDataVM betData = new GameLogDataVM();
					//				betData.OrderTime = Convert.ToDateTime(p.orderTime);
					//				betData.GameName = p.sportsType;
					//				betData.OrderBet = Convert.ToDecimal(p.stake.ToString());  // stake
					//				betData.WinLose = Convert.ToDecimal((p.winlost + p.stake).ToString()); // stake + winlost
					//				dataVMs.Add(betData);
					//			}

					//		}
					//	}
					//}

					////UG
					//if (game == "UG")
					//{
					//	using (var db = new BingoConn())
					//	{
					//		var query = db.UGGames
					//								.Where(s => s.Account.ToUpper() == UUID && (s.BetDate >= dtStart && s.BetDate <= dtEnd))
					//								.OrderBy(s => s.BetDate)
					//								.ToList();

					//		if (query != null)
					//		{
					//			iTotal = query.Count;

					//			foreach (var p in query)
					//			{
					//				string sGameName = "";

					//				var subQuery = db.UGGameSubs
					//												.Where(x => x.BetID == p.BetID)
					//												.FirstOrDefault();

					//				if (subQuery != null)
					//				{
					//					var gameQuery = db.ADMGameSetups
					//													.Where(u => u.GameProvider == "UG" && u.GameID == subQuery.SportID)
					//													.FirstOrDefault();

					//					if (gameQuery != null)
					//					{
					//						sGameName = gameQuery.GameNameEN;
					//					}
					//				}

					//				GameLogDataVM betData = new GameLogDataVM();
					//				betData.OrderTime = Convert.ToDateTime(p.BetDate);
					//				betData.GameName = sGameName;
					//				betData.OrderBet = Convert.ToDecimal(p.BetAmount.ToString());
					//				betData.WinLose = Convert.ToDecimal(p.Payout.ToString());
					//				dataVMs.Add(betData);
					//			}

					//		}
					//	}
					//}

					////GD
					//if (game == "GD")
					//{
					//	using (var db = new BingoConn())
					//	{
					//		var query = db.GDGames
					//								.Where(s => s.UserID.ToUpper() == UUID && (s.BetTime >= dtStart && s.BetTime <= dtEnd))
					//								.OrderBy(s => s.BetTime)
					//								.ToList();

					//		if (query != null)
					//		{
					//			iTotal = query.Count;

					//			foreach (var p in query)
					//			{
					//				GameLogDataVM betData = new GameLogDataVM();
					//				betData.OrderTime = Convert.ToDateTime(p.BetTime);
					//				betData.GameName = p.ProductID;
					//				betData.OrderBet = Convert.ToDecimal(p.BetAmount.ToString());
					//				betData.WinLose = Convert.ToDecimal(p.WinLoss.ToString());
					//				dataVMs.Add(betData);
					//			}

					//		}
					//	}
					//}
					#endregion


					return Json(new
					{
						TotalRecord = iTotal.ToString(),
						BetData = dataVMs,
						ErrorDesc = ""
					});
				}
				catch (Exception ex)
				{
					return Json(new
					{
						TotalRecord = "",
						BetData = "",
						ErrorDesc = "Error: " + ex.Message
					});
				}
			}
		}

		public bool InsertLog(string username, string sAction, string sField2, string sValue2, string sField3, string sValue3, string sField4, string sValue4, string sField5, string sValue5,
													string sField6 = "", string sValue6 = "", string sField7 = "", string sValue7 = "")
		{
			try
			{
				SqlConnection scon = new SqlConnection(BingoConn);
				string sID = Guid.NewGuid().ToString();
				DateTime dtNow = DateTime.Now;

				string query = "INSERT INTO dbo.Tracker(Id, Type, Field1, Value1, Field2, Value2, Field3, Value3, Field4, Value4, Field5, Value5, Field6, Value6, Field7, Value7, CreatedDate, Deleted) VALUES ";
				query += "(@id,@type, @Field1, @Value1, @Field2, @Value2, @Field3, @Value3, @Field4, @Value4, @Field5, @Value5, @Field6, @Value6, @Field7, @Value7, @CreatedDate, @del)";
				SqlCommand cmd = new SqlCommand(query, scon);
				scon.Open();
				cmd.Parameters.AddWithValue("@id", sID);
				cmd.Parameters.AddWithValue("@type", sAction);
				cmd.Parameters.AddWithValue("@Field1", "Game");
				cmd.Parameters.AddWithValue("@Value1", "AppGameRec");
				cmd.Parameters.AddWithValue("@Field2", sField2);
				cmd.Parameters.AddWithValue("@Value2", sValue2);
				cmd.Parameters.AddWithValue("@Field3", sField3);
				cmd.Parameters.AddWithValue("@Value3", sValue3);
				cmd.Parameters.AddWithValue("@Field4", sField4);
				cmd.Parameters.AddWithValue("@Value4", sValue4);
				cmd.Parameters.AddWithValue("@Field5", sField5);
				cmd.Parameters.AddWithValue("@Value5", sValue5);
				cmd.Parameters.AddWithValue("@Field6", sField6);
				cmd.Parameters.AddWithValue("@Value6", sValue6);
				cmd.Parameters.AddWithValue("@Field7", sField7);
				cmd.Parameters.AddWithValue("@Value7", sValue7);
				cmd.Parameters.AddWithValue("@CreatedDate", dtNow);
				cmd.Parameters.AddWithValue("@del", '0');
				cmd.ExecuteNonQuery();
				scon.Close();

				return true;
			}
			catch (Exception ex)
			{
				//return ex.Message.ToString();
				return false;
			}
		}
	}
}