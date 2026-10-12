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
	public class PlayerInGameBalanceController : Controller
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

		//new method class
		public class AppPlayerRecsVM
		{
			public List<AppPlayerRecVM> Rows { get; set; }
		}

		public class AppPlayerRecVM
		{
			public string KEY1 { get; set; }
			public string KEY2 { get; set; }
			public string KEY3 { get; set; }
			public int NO_INDEX { get; set; }
			public string STR1 { get; set; } //Password
			public string STR3 { get; set; } //Last Login IP
			public string STR8 { get; set; } //Referral Code
			public string STR9 { get; set; } //Member ID
			public int INT1 { get; set; } //Cash
			public int INT2 { get; set; } // 0=Active, 1=Inactive
			public int INT22 { get; set; } //Role 1=Player, 0=NonPlayer
			public int INT26 { get; set; } //Chip
			public string DATE1 { get; set; }
			public string DATE2 { get; set; } //Reg date
		}

		public class AppPlayerHistoriesVM_1
		{
			public List<AppPlayerHistoryVM_1> Rows { get; set; }
		}

		public class AppPlayerHistoryVM_1
		{
			public string Game { get; set; }
			public string KEY1 { get; set; }
			public string STR1 { get; set; }
			public int INT1 { get; set; }
			public int INT2 { get; set; }
			public string DATE1 { get; set; }
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

				//DateTime dtTo = Date.AddHours(12);
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

					AppPlayerHistoriesVM_1 PlayerHistories = new AppPlayerHistoriesVM_1 { Rows = new List<AppPlayerHistoryVM_1>() };

					using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["TaiwanConn"].ConnectionString))
					{
						con.Open();

						//#1 get the latest trans record before the date passed in 
						string sql = "select * from ( ";
						sql += "select a.*, b.STR1 as 'Game', row_number() over(partition by a.key1 order by a.date1 desc, a.no_index desc) as rn ";
						sql += "from PLAYER_HISTORY a left join GAME_STATUS b on a.INT1=b.KEY3 ";
						sql += "inner join PLAYER_REC c on UPPER(a.KEY1) = UPPER(c.KEY1) and c.INT22=1 ";
						sql += "where a.DATE1 < '" + dtTo.ToString("yyyy-MM-dd HH:mm:ss") + "' "; //exclude ag and bg myr, 4d and ibc
						sql += ") t ";
						sql += "where t.rn = 1 and t.INT1 not in (16,19,27,29) and (t.STR1 = 'Cash In' or t.STR1 = 'Chip In') ";
						sql += "order by t.DATE1 desc ";

						//#2 straight away get current in game and run at 12pm
						//string sql = "select a.KEY1 'PlayerID', a.[Current in Game] 'CurrentInGame', b.key3, a.[Last in Game Time] 'LastInGameTime' ";
						//sql += "from PLAYER_TASK a left join GAME_STATUS b on a.[Current in Game] = b.STR1 ";
						//sql += "where a.[Current in Game] <> '' and a.[Current in Game] is not null ";


						//using (SqlDataAdapter adapter = new SqlDataAdapter("select b.STR1 as 'Game', a.* from PLAYER_HISTORY a left join GAME_STATUS b on a.INT1=b.KEY3 where a.STR1 in ('Cash In','Chip In','Cash Out') and a.DATE1 < '" + dtTo.ToString("yyyy-MM-dd HH:mm:ss") + "' and a.INT1 not in (16,18,19) order by a.DATE1 desc", con))
						using (SqlDataAdapter adapter = new SqlDataAdapter(sql, con))
						{
							DataTable table = new DataTable();
							adapter.Fill(table);
							if (table.Rows.Count > 0)
							{
								foreach (DataRow row in table.Rows)
								{
									PlayerHistories.Rows.Add(new AppPlayerHistoryVM_1
									{
										Game = row["Game"].ToString(),
										KEY1 = row["KEY1"].ToString(),
										STR1 = row["STR1"].ToString(),
										INT1 = Convert.ToInt32(row["INT1"]),
										INT2 = Convert.ToInt32(row["INT2"]),
										DATE1 = row["DATE1"].ToString()
									});
								}
							}
						}
					}

					using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["BingoConn1"].ConnectionString))
					using (SqlConnection contw = new SqlConnection(ConfigurationManager.ConnectionStrings["TaiwanConn"].ConnectionString))
					{

						foreach (var latestrec in PlayerHistories.Rows)
						{
							try
							{
								int GameID = latestrec.INT1;
								string PlayerID = latestrec.KEY1?.Trim();
								decimal dAmount = 0;
								if (latestrec.INT2 > 0)
									dAmount = Convert.ToDecimal(latestrec.INT2) / 100;


								decimal cAmount = 0;
								int[] chipGames = { 16, 17, 24, 27 }; //4d, tiger, tigerfish, menang88

								if (Array.IndexOf(chipGames, GameID) > -1)
								{
									string sql1 = "";
									if (latestrec.STR1.ToUpper().Trim() == "CASH IN")
									{
										sql1 = "select top 1 * from PLAYER_HISTORY where date1 = '" + latestrec.DATE1 + "' and UPPER(KEY1) = '" + latestrec.KEY1?.Trim().ToUpper() + "' ";
										sql1 += "and INT1 = '" + GameID + "' and STR1 = 'Chip In' order by DATE1 ";
									}
									else if (latestrec.STR1.ToUpper().Trim() == "CHIP IN")
									{
										sql1 = "select top 1 * from PLAYER_HISTORY where date1 = '" + latestrec.DATE1 + "' and UPPER(KEY1) = '" + latestrec.KEY1?.Trim().ToUpper() + "' ";
										sql1 += "and INT1 = '" + GameID + "' and STR1 = 'Cash In' order by DATE1 ";
									}

									using (SqlDataAdapter adapter = new SqlDataAdapter(sql1, contw))
									{
										DataTable table = new DataTable();
										adapter.Fill(table);
										if (table.Rows.Count > 0)
											cAmount = Convert.ToDecimal(Convert.ToInt32(table.Rows[0]["INT2"])) / 100;
									}
								}

								//bka
								if (GameID == 3)
								{
									//var vRow = new PlayerBalanceVM_Row();
									PlayerInGameBalance vRow = new PlayerInGameBalance();
									var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == "BKA" && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

									if (record != null)
									{
										string sql = "select COALESCE(SUM(CAST(ISNULL(WinLoss,0) AS DECIMAL(10, 2)) - CAST(ISNULL(Amount,0) AS DECIMAL(10, 2))),0) 'winlose' from BKAGame ";
										sql += "where Account = '" + record.GamePlayerID + "' and DateTime >= '" + latestrec.DATE1.ToString() + "' and DateTime < '" + dtTo.ToString("yyyy-MM-dd HH:mm:ss") + "'";
										using (SqlDataAdapter adapter = new SqlDataAdapter(sql, con))
										{
											DataTable table = new DataTable();
											adapter.Fill(table);
											if (table.Rows.Count == 1)
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount, 2);
												vRow.BalanceAt12pm = Math.Round(dAmount + Convert.ToDecimal(table.Rows[0]["winlose"]), 2);
												vRow.Game = "BKA";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.IsSuccess = true;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;
											}
											else
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount, 2);
												vRow.BalanceAt12pm = Math.Round(dAmount, 2);
												vRow.Game = "BKA";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.IsSuccess = true;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;
											}
											successCnt++;
										}
									}
									else
									{
										vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
										vRow.PlayerID = PlayerID;
										vRow.CashInBalance = Math.Round(dAmount, 2);
										vRow.BalanceAt12pm = null;
										vRow.Game = "BKA";
										vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
										vRow.Remark = "Game Id Not Found";
										vRow.IsSuccess = false;
										vRow.CreatedDate = DateTime.Now;
										vRow.IsVerify = true;

										failedCnt++;
										_common.LogError("GetPlayerBalanceRecord|BKA|" + PlayerID + " " + DateTime.Now.ToString() + " Game Id Not Found", "PlayerInGameBalance");
									}

									//model.Data.Add(vRow);
									db.PlayerInGameBalances.AddOrUpdate(vRow);
								}
								//918kiss
								else if (GameID == 5)
								{
									//var vRow = new PlayerBalanceVM_Row();
									PlayerInGameBalance vRow = new PlayerInGameBalance();
									var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == "918KISS" && x.PlayerID.ToUpper() == "918" + PlayerID.ToUpper()).FirstOrDefault();

									if (record != null)
									{
										//string sql = "select top 1 CAST(EndBalance AS DECIMAL(10,2)) 'EndBalance' from KISGameRecord where username = '" + record.GamePlayerID + "' and CreateTime >= '" + latestrec.DATE1.ToString() + "' and CreateTime < '" + dtTo.ToString("yyyy-MM-dd HH:mm:ss") + "' ";
										//sql += "order by CreateTime desc";
										string sql = "select top 1 CAST(EndBalance AS DECIMAL(10,2)) 'EndBalance' from ( ";
										sql += "select username, uid, GameID, bet, win, BeginBalance, EndBalance, CreateTime from KISGameRecord where username = '" + record.GamePlayerID + "' and CreateTime >= '" + latestrec.DATE1.ToString() + "' and CreateTime < '" + dtTo.ToString("yyyy-MM-dd HH:mm:ss") + "' ";
										sql += "union ";
										sql += "select username, uid, GameID, bet, win, BeginBalance, EndBalance, CreateTime from KISGame where username = '" + record.GamePlayerID + "' and CreateTime >= '" + latestrec.DATE1.ToString() + "' and CreateTime < '" + dtTo.ToString("yyyy-MM-dd HH:mm:ss") + "' ";
										sql += ") x ";
										sql += "order by CreateTime desc";
										using (SqlDataAdapter adapter = new SqlDataAdapter(sql, con))
										{
											DataTable table = new DataTable();
											adapter.Fill(table);
											if (table.Rows.Count == 1)
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount, 2);
												vRow.BalanceAt12pm = Math.Round(Convert.ToDecimal(table.Rows[0]["EndBalance"]), 2);
												vRow.Game = "918KISS";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.IsSuccess = true;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;
											}
											else
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount, 2);
												vRow.BalanceAt12pm = Math.Round(dAmount, 2);
												vRow.Game = "918KISS";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.IsSuccess = true;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;
											}
											successCnt++;
										}
									}
									else
									{
										vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
										vRow.PlayerID = PlayerID;
										vRow.CashInBalance = Math.Round(dAmount, 2);
										vRow.BalanceAt12pm = null;
										vRow.Game = "918KISS";
										vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
										vRow.Remark = "Game Id Not Found";
										vRow.IsSuccess = false;
										vRow.CreatedDate = DateTime.Now;
										vRow.IsVerify = true;

										failedCnt++;
										_common.LogError("GetPlayerBalanceRecord|918KISS|" + PlayerID + " " + DateTime.Now.ToString() + " Game Id Not Found", "PlayerInGameBalance");
									}

									//model.Data.Add(vRow);
									db.PlayerInGameBalances.AddOrUpdate(vRow);
								}
								//bg
								else if (GameID == 7)
								{
									//var vRow = new PlayerBalanceVM_Row();
									PlayerInGameBalance vRow = new PlayerInGameBalance();

									var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == "BG" && x.PlayerID.ToUpper() == PlayerID.ToUpper() && x.Currency.ToUpper().Trim() == "MYR").FirstOrDefault();

									if (record != null)
									{
										string sql = "";
										sql = "select CAST(COALESCE(sum(payment),0) AS DECIMAL(10,2)) 'winlose' from BGGame where loginId = '" + record.GamePlayerID + "' ";
										sql += "and DATEADD(HOUR, 12, orderTime) >= '" + latestrec.DATE1.ToString() + "' and DATEADD(HOUR, 12, orderTime) < '" + dtTo.ToString("yyyy-MM-dd HH:mm:ss") + "'";
										//DateTime cDate = new DateTime(2020, 11, 4);
										//if (dtTo.Date < cDate.Date)
										//{
										//	sql = "select CAST(COALESCE(sum(payment),0) AS DECIMAL(10,2)) 'winlose' from BGGame where loginId = '" + record.GamePlayerID + "' ";
										//	sql += "and DATEADD(HOUR, 12, orderTime) >= '" + latestrec.DATE1.ToString() + "' and DATEADD(HOUR, 12, orderTime) < '" + dtTo.ToString("yyyy-MM-dd HH:mm:ss") + "'";
										//}
										//else
										//{
										//	sql = "select CAST(COALESCE(sum(payment),0) AS DECIMAL(10,2)) 'winlose' from BGGame where loginId = '" + record.GamePlayerID + "' ";
										//	sql += "and DATEADD(HOUR, 12, lastUpdateTime) >= '" + latestrec.DATE1.ToString() + "' and DATEADD(HOUR, 12, lastUpdateTime) < '" + dtTo.ToString("yyyy-MM-dd HH:mm:ss") + "'";
										//}
										using (SqlDataAdapter adapter = new SqlDataAdapter(sql, con))
										{
											DataTable table = new DataTable();
											adapter.Fill(table);
											if (table.Rows.Count == 1)
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount, 2);
												vRow.BalanceAt12pm = Math.Round(dAmount + Convert.ToDecimal(table.Rows[0]["winlose"]), 2);
												vRow.Game = "BG";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.IsSuccess = true;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;
											}
											else
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount, 2);
												vRow.BalanceAt12pm = Math.Round(dAmount, 2);
												vRow.Game = "BG";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.IsSuccess = true;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;
											}
										}
										successCnt++;
									}
									else
									{
										vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
										vRow.PlayerID = PlayerID;
										vRow.CashInBalance = Math.Round(dAmount, 2);
										vRow.BalanceAt12pm = null;
										vRow.Game = "BG";
										vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
										vRow.Remark = "Game Id Not Found";
										vRow.IsSuccess = false;
										vRow.CreatedDate = DateTime.Now;
										vRow.IsVerify = true;

										failedCnt++;
										_common.LogError("GetPlayerBalanceRecord|BG|" + PlayerID + " " + DateTime.Now.ToString() + " Game Id Not Found", "PlayerInGameBalance");
									}

									//model.Data.Add(vRow);
									db.PlayerInGameBalances.AddOrUpdate(vRow);
								}
								//ag
								else if (GameID == 9)
								{
									//var vRow = new PlayerBalanceVM_Row();
									PlayerInGameBalance vRow = new PlayerInGameBalance();

									var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == "AG" && x.PlayerID.ToUpper() == PlayerID.ToUpper() && x.Currency.ToUpper().Trim() == "MYR").FirstOrDefault();

									if (record != null)
									{
										string sql = "select top 1 CAST(BeforeCredit + ISNULL(netAmount,0) AS DECIMAL(10,2)) 'aftercredit' from AGGame where playerName = '" + record.GamePlayerID + "' and DATEADD(HOUR, 12, betTime) >= '" + latestrec.DATE1.ToString() + "' and DATEADD(HOUR, 12, betTime) < '" + dtTo.ToString("yyyy-MM-dd HH:mm:ss") + "' ";
										sql += "order by betTime desc";
										using (SqlDataAdapter adapter = new SqlDataAdapter(sql, con))
										{
											DataTable table = new DataTable();
											adapter.Fill(table);
											if (table.Rows.Count == 1)
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount, 2);
												vRow.BalanceAt12pm = Math.Round(Convert.ToDecimal(table.Rows[0]["aftercredit"]), 2);
												vRow.Game = "AG";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.IsSuccess = true;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;
											}
											else
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount, 2);
												vRow.BalanceAt12pm = Math.Round(dAmount, 2);
												vRow.Game = "AG";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.IsSuccess = true;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;
											}
											successCnt++;
										}
									}
									else
									{
										vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
										vRow.PlayerID = PlayerID;
										vRow.CashInBalance = Math.Round(dAmount, 2);
										vRow.BalanceAt12pm = null;
										vRow.Game = "AG";
										vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
										vRow.Remark = "Game Id Not Found";
										vRow.IsSuccess = false;
										vRow.CreatedDate = DateTime.Now;
										vRow.IsVerify = true;

										failedCnt++;
										_common.LogError("GetPlayerBalanceRecord|AG|" + PlayerID + " " + DateTime.Now.ToString() + " Game Id Not Found", "PlayerInGameBalance");
									}

									//model.Data.Add(vRow);
									db.PlayerInGameBalances.AddOrUpdate(vRow);

								}
								//playtech
								else if (GameID == 11)
								{
									//var vRow = new PlayerBalanceVM_Row();
									PlayerInGameBalance vRow = new PlayerInGameBalance();

									var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == "PT" && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

									if (record != null)
									{
										string sql = "select top 1 CAST(Balance AS DECIMAL(10,2)) 'Balance' from PTGame where PlayerName = 'MCY_" + record.GamePlayerID + "' and GameDate >= '" + latestrec.DATE1.ToString() + "' and GameDate < '" + dtTo.ToString("yyyy-MM-dd HH:mm:ss") + "' ";
										sql += "order by GameDate desc";
										using (SqlDataAdapter adapter = new SqlDataAdapter(sql, con))
										{
											DataTable table = new DataTable();
											adapter.Fill(table);
											if (table.Rows.Count == 1)
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount, 2);
												vRow.BalanceAt12pm = Math.Round(Convert.ToDecimal(table.Rows[0]["Balance"]), 2);
												vRow.Game = "PT";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.IsSuccess = true;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;
											}
											else
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount, 2);
												vRow.BalanceAt12pm = Math.Round(dAmount, 2);
												vRow.Game = "PT";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.IsSuccess = true;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;
											}
											successCnt++;
										}
									}
									else
									{
										vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
										vRow.PlayerID = PlayerID;
										vRow.CashInBalance = Math.Round(dAmount, 2);
										vRow.BalanceAt12pm = null;
										vRow.Game = "PT";
										vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
										vRow.Remark = "Game Id Not Found";
										vRow.IsSuccess = false;
										vRow.CreatedDate = DateTime.Now;
										vRow.IsVerify = true;

										failedCnt++;
										_common.LogError("GetPlayerBalanceRecord|PT|" + PlayerID + " " + DateTime.Now.ToString() + " Game Id Not Found", "PlayerInGameBalance");
									}

									//model.Data.Add(vRow);
									db.PlayerInGameBalances.AddOrUpdate(vRow);

								}
								//ig
								else if (GameID == 15)
								{
									//var vRow = new PlayerBalanceVM_Row();
									PlayerInGameBalance vRow = new PlayerInGameBalance();

									var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == "IG" && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

									if (record != null)
									{
										string sql = "select top 1 CAST(ValueAfter AS DECIMAL(10,2)) 'ValueAfter' from IGGame where UserName = 'BT_" + record.GamePlayerID + "' and OrderTime >= '" + latestrec.DATE1.ToString() + "' and OrderTime < '" + dtTo.ToString("yyyy-MM-dd HH:mm:ss") + "' and GameName <> 'angpao' ";
										sql += "order by OrderTime desc";
										using (SqlDataAdapter adapter = new SqlDataAdapter(sql, con))
										{
											DataTable table = new DataTable();
											adapter.Fill(table);
											if (table.Rows.Count == 1)
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount, 2);
												vRow.BalanceAt12pm = Math.Round(Convert.ToDecimal(table.Rows[0]["ValueAfter"]), 2);
												vRow.Game = "IG";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.IsSuccess = true;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;
											}
											else
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount, 2);
												vRow.BalanceAt12pm = Math.Round(dAmount, 2);
												vRow.Game = "IG";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.IsSuccess = true;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;
											}
											successCnt++;
										}
									}
									else
									{
										vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
										vRow.PlayerID = PlayerID;
										vRow.CashInBalance = Math.Round(dAmount, 2);
										vRow.BalanceAt12pm = null;
										vRow.Game = "IG";
										vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
										vRow.Remark = "Game Id Not Found";
										vRow.IsSuccess = false;
										vRow.CreatedDate = DateTime.Now;
										vRow.IsVerify = true;

										failedCnt++;
										_common.LogError("GetPlayerBalanceRecord|IG|" + PlayerID + " " + DateTime.Now.ToString() + " Game Id Not Found", "PlayerInGameBalance");
									}

									//model.Data.Add(vRow);
									db.PlayerInGameBalances.AddOrUpdate(vRow);

								}
								//bt28win
								else if (GameID == 16)
								{
									//var vRow = new PlayerBalanceVM_Row();
									PlayerInGameBalance vRow = new PlayerInGameBalance();

									var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == "BT28WIN" && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

									if (record != null)
									{
										vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
										vRow.PlayerID = PlayerID;
										vRow.CashInBalance = Math.Round(dAmount + cAmount, 2);
										vRow.BalanceAt12pm = Math.Round(dAmount + cAmount, 2);
										vRow.Game = "BT28WIN";
										vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
										vRow.Remark = "Direct use cash in balance";
										vRow.IsSuccess = true;
										vRow.CreatedDate = DateTime.Now;
										vRow.IsVerify = true;

										successCnt++;
										_common.LogError("GetPlayerBalanceRecord|BT28WIN|" + PlayerID + " " + DateTime.Now.ToString() + " Direct use cash in balance", "PlayerInGameBalance");
									}
									else
									{
										vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
										vRow.PlayerID = PlayerID;
										vRow.CashInBalance = Math.Round(dAmount + cAmount, 2);
										vRow.BalanceAt12pm = null;
										vRow.Game = "BT28WIN";
										vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
										vRow.Remark = "Game Id Not Found";
										vRow.IsSuccess = false;
										vRow.CreatedDate = DateTime.Now;
										vRow.IsVerify = true;

										failedCnt++;
										_common.LogError("GetPlayerBalanceRecord|BT28WIN|" + PlayerID + " " + DateTime.Now.ToString() + " Game Id Not Found", "PlayerInGameBalance");
									}

									//model.Data.Add(vRow);
									db.PlayerInGameBalances.AddOrUpdate(vRow);

								}
								//tpg
								else if (GameID == 17)
								{
									//var vRow = new PlayerBalanceVM_Row();
									PlayerInGameBalance vRow = new PlayerInGameBalance();

									var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == "TPG" && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

									if (record != null)
									{
										string sql = "select CAST(COALESCE(SUM(ISNULL(TotalPayoutAmount,0) - ISNULL(TotalBetAmount,0)),0) AS DECIMAL(10, 2)) 'winlose' from TPGGame ";
										sql += "where Username = '" + record.GamePlayerID + "' and UpdatedAt >= '" + latestrec.DATE1.ToString() + "' and UpdatedAt < '" + dtTo.ToString("yyyy-MM-dd HH:mm:ss") + "' ";
										sql += "and GameType <> 10 ";
										using (SqlDataAdapter adapter = new SqlDataAdapter(sql, con))
										{
											DataTable table = new DataTable();
											adapter.Fill(table);
											if (table.Rows.Count == 1)
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount + cAmount, 2);
												vRow.BalanceAt12pm = Math.Round(dAmount + cAmount + Convert.ToDecimal(table.Rows[0]["winlose"]), 2);
												vRow.Game = "TPG";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.IsSuccess = true;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;
											}
											else
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount + cAmount, 2);
												vRow.BalanceAt12pm = Math.Round(dAmount + cAmount, 2);
												vRow.Game = "TPG";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.IsSuccess = true;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;
											}
											successCnt++;
										}
									}
									else
									{
										vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
										vRow.PlayerID = PlayerID;
										vRow.CashInBalance = Math.Round(dAmount + cAmount, 2);
										vRow.BalanceAt12pm = null;
										vRow.Game = "TPG";
										vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
										vRow.Remark = "Game Id Not Found";
										vRow.IsSuccess = false;
										vRow.CreatedDate = DateTime.Now;
										vRow.IsVerify = true;

										failedCnt++;
										_common.LogError("GetPlayerBalanceRecord|TPG|" + PlayerID + " " + DateTime.Now.ToString() + " Game Id Not Found", "PlayerInGameBalance");
									}

									//model.Data.Add(vRow);
									db.PlayerInGameBalances.AddOrUpdate(vRow);

								}
								//allcity
								else if (GameID == 18)
								{
									//var vRow = new PlayerBalanceVM_Row();
									PlayerInGameBalance vRow = new PlayerInGameBalance();

									var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == "ALLCITY" && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

									if (record != null)
									{
										string sql = "select top 1 CAST(ISNULL(Credit,0)+ISNULL(TotalWin,0) AS DECIMAL(10,2)) 'BalanceAfter' from ACGame where Account = 'ML" + record.GamePlayerID + "' and time >= '" + latestrec.DATE1.ToString() + "' and time < '" + dtTo.ToString("yyyy-MM-dd HH:mm:ss") + "' ";
										sql += "order by time desc";
										using (SqlDataAdapter adapter = new SqlDataAdapter(sql, con))
										{
											DataTable table = new DataTable();
											adapter.Fill(table);
											if (table.Rows.Count == 1)
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount, 2);
												vRow.BalanceAt12pm = Math.Round(Convert.ToDecimal(table.Rows[0]["BalanceAfter"]), 2);
												vRow.Game = "ALLCITY";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.IsSuccess = true;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;
											}
											else
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount, 2);
												vRow.BalanceAt12pm = Math.Round(dAmount, 2);
												vRow.Game = "ALLCITY";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.IsSuccess = true;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;
											}
											successCnt++;
										}
									}
									else
									{
										vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
										vRow.PlayerID = PlayerID;
										vRow.CashInBalance = Math.Round(dAmount, 2);
										vRow.BalanceAt12pm = null;
										vRow.Game = "ALLCITY";
										vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
										vRow.Remark = "Game Id Not Found";
										vRow.IsSuccess = false;
										vRow.CreatedDate = DateTime.Now;
										vRow.IsVerify = true;

										failedCnt++;
										_common.LogError("GetPlayerBalanceRecord|ALLCITY|" + PlayerID + " " + DateTime.Now.ToString() + " Game Id Not Found", "PlayerInGameBalance");
									}
									//model.Data.Add(vRow);
									db.PlayerInGameBalances.AddOrUpdate(vRow);

								}
								//ibc
								else if (GameID == 19)
								{
									//var vRow = new PlayerBalanceVM_Row();
									PlayerInGameBalance vRow = new PlayerInGameBalance();

									var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == "IBC" && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

									if (record != null)
									{
										vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
										vRow.PlayerID = PlayerID;
										vRow.CashInBalance = Math.Round(dAmount, 2);
										vRow.BalanceAt12pm = Math.Round(dAmount, 2);
										vRow.Game = "IBC";
										vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
										vRow.Remark = "Direct use cash in balance";
										vRow.IsSuccess = true;
										vRow.CreatedDate = DateTime.Now;
										vRow.IsVerify = true;

										successCnt++;
										_common.LogError("GetPlayerBalanceRecord|IBC|" + PlayerID + " " + DateTime.Now.ToString() + " Direct use cash in balance", "PlayerInGameBalance");
									}
									else
									{
										vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
										vRow.PlayerID = PlayerID;
										vRow.CashInBalance = Math.Round(dAmount, 2);
										vRow.BalanceAt12pm = null;
										vRow.Game = "IBC";
										vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
										vRow.Remark = "Game Id Not Found";
										vRow.IsSuccess = false;
										vRow.CreatedDate = DateTime.Now;
										vRow.IsVerify = true;


										failedCnt++;
										_common.LogError("GetPlayerBalanceRecord|IBC|" + PlayerID + " " + DateTime.Now.ToString() + " Game Id Not Found", "PlayerInGameBalance");
									}

									//model.Data.Add(vRow);
									db.PlayerInGameBalances.AddOrUpdate(vRow);
								}
								//evo888
								else if (GameID == 20)
								{
									//var vRow = new PlayerBalanceVM_Row();
									PlayerInGameBalance vRow = new PlayerInGameBalance();

									var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == "EVO888" && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

									if (record != null)
									{
										string sql = "select top 1 CAST(EndMoney AS DECIMAL(10,2)) 'EndMoney' from Evo888Game where UserName = '" + record.GamePlayerID + "' and DATEADD(HOUR, 8, DateTime) >= '" + latestrec.DATE1.ToString() + "' and DATEADD(HOUR, 8, DateTime) < '" + dtTo.ToString("yyyy-MM-dd HH:mm:ss") + "' ";
										sql += "order by DateTime desc";
										using (SqlDataAdapter adapter = new SqlDataAdapter(sql, con))
										{
											DataTable table = new DataTable();
											adapter.Fill(table);
											if (table.Rows.Count == 1)
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount, 2);
												vRow.BalanceAt12pm = Math.Round(Convert.ToDecimal(table.Rows[0]["EndMoney"]), 2);
												vRow.Game = "EVO888";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.IsSuccess = true;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;
											}
											else
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount, 2);
												vRow.BalanceAt12pm = Math.Round(dAmount, 2);
												vRow.Game = "EVO888";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.IsSuccess = true;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;
											}
											successCnt++;
										}
									}
									else
									{
										vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
										vRow.PlayerID = PlayerID;
										vRow.CashInBalance = Math.Round(dAmount, 2);
										vRow.BalanceAt12pm = null;
										vRow.Game = "EVO888";
										vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
										vRow.Remark = "Game Id Not Found";
										vRow.IsSuccess = false;
										vRow.CreatedDate = DateTime.Now;
										vRow.IsVerify = true;

										failedCnt++;
										_common.LogError("GetPlayerBalanceRecord|EVO888|" + PlayerID + " " + DateTime.Now.ToString() + " Game Id Not Found", "PlayerInGameBalance");
									}

									db.PlayerInGameBalances.AddOrUpdate(vRow);
								}
								//bg
								else if (GameID == 21)
								{
									//var vRow = new PlayerBalanceVM_Row();
									PlayerInGameBalance vRow = new PlayerInGameBalance();

									var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == "BG" && x.PlayerID.ToUpper() == PlayerID.ToUpper() && x.Currency.ToUpper().Trim() == "CNY").FirstOrDefault();

									if (record != null)
									{
										string sql = "";
										sql = "select CAST(COALESCE(sum(payment),0) AS DECIMAL(10,2)) 'winlose' from BGGame where loginId = '" + record.GamePlayerID + "' ";
										sql += "and DATEADD(HOUR, 12, orderTime) >= '" + latestrec.DATE1.ToString() + "' and DATEADD(HOUR, 12, orderTime) < '" + dtTo.ToString("yyyy-MM-dd HH:mm:ss") + "'";
										//DateTime cDate = new DateTime(2020, 11, 4);
										//if (dtTo.Date < cDate.Date)
										//{
										//	sql = "select CAST(COALESCE(sum(payment),0) AS DECIMAL(10,2)) 'winlose' from BGGame where loginId = '" + record.GamePlayerID + "' ";
										//	sql += "and DATEADD(HOUR, 12, orderTime) >= '" + latestrec.DATE1.ToString() + "' and DATEADD(HOUR, 12, orderTime) < '" + dtTo.ToString("yyyy-MM-dd HH:mm:ss") + "'";
										//}
										//else
										//{
										//	sql = "select CAST(COALESCE(sum(payment),0) AS DECIMAL(10,2)) 'winlose' from BGGame where loginId = '" + record.GamePlayerID + "' ";
										//	sql += "and DATEADD(HOUR, 12, lastUpdateTime) >= '" + latestrec.DATE1.ToString() + "' and DATEADD(HOUR, 12, lastUpdateTime) < '" + dtTo.ToString("yyyy-MM-dd HH:mm:ss") + "'";
										//}
										using (SqlDataAdapter adapter = new SqlDataAdapter(sql, con))
										{
											DataTable table = new DataTable();
											adapter.Fill(table);
											if (table.Rows.Count == 1)
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount, 2);
												vRow.BalanceAt12pm = Math.Round(dAmount + Convert.ToDecimal(table.Rows[0]["winlose"]), 2);
												vRow.Game = "BG";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.IsSuccess = true;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;
											}
											else
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount, 2);
												vRow.BalanceAt12pm = Math.Round(dAmount, 2);
												vRow.Game = "BG";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.IsSuccess = true;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;
											}
										}
										successCnt++;
									}
									else
									{
										vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
										vRow.PlayerID = PlayerID;
										vRow.CashInBalance = Math.Round(dAmount, 2);
										vRow.BalanceAt12pm = null;
										vRow.Game = "BG";
										vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
										vRow.Remark = "Game Id Not Found";
										vRow.IsSuccess = false;
										vRow.CreatedDate = DateTime.Now;
										vRow.IsVerify = true;

										failedCnt++;
										_common.LogError("GetPlayerBalanceRecord|BG|" + PlayerID + " " + DateTime.Now.ToString() + " Game Id Not Found", "PlayerInGameBalance");
									}

									//model.Data.Add(vRow);
									db.PlayerInGameBalances.AddOrUpdate(vRow);
								}
								//ag
								else if (GameID == 22)
								{
									//var vRow = new PlayerBalanceVM_Row();
									PlayerInGameBalance vRow = new PlayerInGameBalance();

									var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == "AG" && x.PlayerID.ToUpper() == PlayerID.ToUpper() && x.Currency.ToUpper().Trim() == "CNY").FirstOrDefault();

									if (record != null)
									{
										string sql = "select top 1 CAST(BeforeCredit + ISNULL(netAmount,0) AS DECIMAL(10,2)) 'aftercredit' from AGGame where playerName = '" + record.GamePlayerID + "' and DATEADD(HOUR, 12, betTime) >= '" + latestrec.DATE1.ToString() + "' and DATEADD(HOUR, 12, betTime) < '" + dtTo.ToString("yyyy-MM-dd HH:mm:ss") + "' ";
										sql += "order by betTime desc";
										using (SqlDataAdapter adapter = new SqlDataAdapter(sql, con))
										{
											DataTable table = new DataTable();
											adapter.Fill(table);
											if (table.Rows.Count == 1)
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount, 2);
												vRow.BalanceAt12pm = Math.Round(Convert.ToDecimal(table.Rows[0]["aftercredit"]), 2);
												vRow.Game = "AG";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.IsSuccess = true;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;
											}
											else
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount, 2);
												vRow.BalanceAt12pm = Math.Round(dAmount, 2);
												vRow.Game = "AG";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.IsSuccess = true;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;
											}
											successCnt++;
										}
									}
									else
									{
										vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
										vRow.PlayerID = PlayerID;
										vRow.CashInBalance = Math.Round(dAmount, 2);
										vRow.BalanceAt12pm = null;
										vRow.Game = "AG";
										vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
										vRow.Remark = "Game Id Not Found";
										vRow.IsSuccess = false;
										vRow.CreatedDate = DateTime.Now;
										vRow.IsVerify = true;

										failedCnt++;
										_common.LogError("GetPlayerBalanceRecord|AG|" + PlayerID + " " + DateTime.Now.ToString() + " Game Id Not Found", "PlayerInGameBalance");
									}

									//model.Data.Add(vRow);
									db.PlayerInGameBalances.AddOrUpdate(vRow);

								}
								//vpower
								else if (GameID == 23)
								{
									//var vRow = new PlayerBalanceVM_Row();
									PlayerInGameBalance vRow = new PlayerInGameBalance();

									var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == "VPOWER" && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

									if (record != null)
									{
										string sql = "select top 1 CAST(finalscore AS DECIMAL(10,2)) 'finalscore' from VPowerGame where username = '" + record.GamePlayerID + "' and time >= '" + latestrec.DATE1.ToString() + "' and time < '" + dtTo.ToString("yyyy-MM-dd HH:mm:ss") + "' ";
										sql += "order by time desc";
										using (SqlDataAdapter adapter = new SqlDataAdapter(sql, con))
										{
											DataTable table = new DataTable();
											adapter.Fill(table);
											if (table.Rows.Count == 1)
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount, 2);
												vRow.BalanceAt12pm = Math.Round(Convert.ToDecimal(table.Rows[0]["finalscore"]), 2);
												vRow.Game = "VPOWER";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.IsSuccess = true;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;
											}
											else
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount, 2);
												vRow.BalanceAt12pm = Math.Round(dAmount, 2);
												vRow.Game = "VPOWER";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.IsSuccess = true;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;
											}
											successCnt++;
										}
									}
									else
									{
										vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
										vRow.PlayerID = PlayerID;
										vRow.CashInBalance = Math.Round(dAmount, 2);
										vRow.BalanceAt12pm = null;
										vRow.Game = "VPOWER";
										vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
										vRow.Remark = "Game Id Not Found";
										vRow.IsSuccess = false;
										vRow.CreatedDate = DateTime.Now;
										vRow.IsVerify = true;

										failedCnt++;
										_common.LogError("GetPlayerBalanceRecord|VPOWER|" + PlayerID + " " + DateTime.Now.ToString() + " Game Id Not Found", "PlayerInGameBalance");
									}

									db.PlayerInGameBalances.AddOrUpdate(vRow);

								}
								//tpgfish
								else if (GameID == 24)
								{
									//var vRow = new PlayerBalanceVM_Row();
									PlayerInGameBalance vRow = new PlayerInGameBalance();

									var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == "TPGFISH" && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

									if (record != null)
									{
										string sql = "select CAST(COALESCE(SUM(ISNULL(TotalPayoutAmount,0) - ISNULL(TotalBetAmount,0)),0) AS DECIMAL(10, 2)) 'winlose' from TPGGame ";
										sql += "where Username = '" + record.GamePlayerID + "' and UpdatedAt >= '" + latestrec.DATE1.ToString() + "' and UpdatedAt < '" + dtTo.ToString("yyyy-MM-dd HH:mm:ss") + "' ";
										sql += "and GameType = 10 ";
										using (SqlDataAdapter adapter = new SqlDataAdapter(sql, con))
										{
											DataTable table = new DataTable();
											adapter.Fill(table);
											if (table.Rows.Count == 1)
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount + cAmount, 2);
												vRow.BalanceAt12pm = Math.Round(dAmount + cAmount + Convert.ToDecimal(table.Rows[0]["winlose"]), 2);
												vRow.Game = "TPGFISH";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.IsSuccess = true;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;
											}
											else
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount + cAmount, 2);
												vRow.BalanceAt12pm = Math.Round(dAmount + cAmount, 2);
												vRow.Game = "TPGFISH";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.IsSuccess = true;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;
											}
											successCnt++;
										}
									}
									else
									{
										vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
										vRow.PlayerID = PlayerID;
										vRow.CashInBalance = Math.Round(dAmount + cAmount, 2);
										vRow.BalanceAt12pm = null;
										vRow.Game = "TPGFISH";
										vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
										vRow.Remark = "Game Id Not Found";
										vRow.IsSuccess = false;
										vRow.CreatedDate = DateTime.Now;
										vRow.IsVerify = true;

										failedCnt++;
										_common.LogError("GetPlayerBalanceRecord|TPGFISH|" + PlayerID + " " + DateTime.Now.ToString() + " Game Id Not Found", "PlayerInGameBalance");
									}

									//model.Data.Add(vRow);
									db.PlayerInGameBalances.AddOrUpdate(vRow);

								}
								//sexygaming
								else if (GameID == 14)
								{
									//var vRow = new PlayerBalanceVM_Row();
									PlayerInGameBalance vRow = new PlayerInGameBalance();

									var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == "SEXYGAMING" && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

									if (record != null)
									{
										string sql = "select top 1 CAST(startAmount AS DECIMAL(10,2)) + CAST(WinLost AS DECIMAL(10,2)) 'endAmount' from SexyGame where UserName = 'MCM_" + record.GamePlayerID + "' and DATEADD(HOUR, 8, BetDate) >= '" + latestrec.DATE1.ToString() + "' and DATEADD(HOUR, 8, BetDate) < '" + dtTo.ToString("yyyy-MM-dd HH:mm:ss") + "' ";
										sql += "order by BetDate desc";
										using (SqlDataAdapter adapter = new SqlDataAdapter(sql, con))
										{
											DataTable table = new DataTable();
											adapter.Fill(table);
											if (table.Rows.Count == 1)
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount, 2);
												vRow.BalanceAt12pm = Math.Round(Convert.ToDecimal(table.Rows[0]["endAmount"]), 2);
												vRow.Game = "SEXYGAMING";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.IsSuccess = true;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;
											}
											else
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount, 2);
												vRow.BalanceAt12pm = Math.Round(dAmount, 2);
												vRow.Game = "SEXYGAMING";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.IsSuccess = true;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;
											}
											successCnt++;
										}
									}
									else
									{
										vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
										vRow.PlayerID = PlayerID;
										vRow.CashInBalance = Math.Round(dAmount, 2);
										vRow.BalanceAt12pm = null;
										vRow.Game = "SEXYGAMING";
										vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
										vRow.Remark = "Game Id Not Found";
										vRow.IsSuccess = false;
										vRow.CreatedDate = DateTime.Now;
										vRow.IsVerify = true;

										failedCnt++;
										_common.LogError("GetPlayerBalanceRecord|SEXYGAMING|" + PlayerID + " " + DateTime.Now.ToString() + " Game Id Not Found", "PlayerInGameBalance");
									}
									//model.Data.Add(vRow);
									db.PlayerInGameBalances.AddOrUpdate(vRow);
								}
								//spadegaming
								else if (GameID == 26)
								{
									//var vRow = new PlayerBalanceVM_Row();
									PlayerInGameBalance vRow = new PlayerInGameBalance();

									var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == "SPADEGAMING" && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

									if (record != null)
									{
										string sql = "select top 1 CAST(balance AS DECIMAL(10,2)) 'endAmount' from SpadeGame where acctId = '" + record.GamePlayerID + "' and ticketTime >= '" + latestrec.DATE1.ToString() + "' and ticketTime < '" + dtTo.ToString("yyyy-MM-dd HH:mm:ss") + "' ";
										sql += "order by ticketTime desc";
										using (SqlDataAdapter adapter = new SqlDataAdapter(sql, con))
										{
											DataTable table = new DataTable();
											adapter.Fill(table);
											if (table.Rows.Count == 1)
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount, 2);
												vRow.BalanceAt12pm = Math.Round(Convert.ToDecimal(table.Rows[0]["endAmount"]), 2);
												vRow.Game = "SPADEGAMING";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.IsSuccess = true;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;
											}
											else
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount, 2);
												vRow.BalanceAt12pm = Math.Round(dAmount, 2);
												vRow.Game = "SPADEGAMING";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.IsSuccess = true;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;
											}
											successCnt++;
										}
									}
									else
									{
										vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
										vRow.PlayerID = PlayerID;
										vRow.CashInBalance = Math.Round(dAmount, 2);
										vRow.BalanceAt12pm = null;
										vRow.Game = "SPADEGAMING";
										vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
										vRow.Remark = "Game Id Not Found";
										vRow.IsSuccess = false;
										vRow.CreatedDate = DateTime.Now;
										vRow.IsVerify = true;

										failedCnt++;
										_common.LogError("GetPlayerBalanceRecord|SPADEGAMING|" + PlayerID + " " + DateTime.Now.ToString() + " Game Id Not Found", "PlayerInGameBalance");
									}
									db.PlayerInGameBalances.AddOrUpdate(vRow);
								}
								//evolutiongaming
								else if (GameID == 28)
								{
									//var vRow = new PlayerBalanceVM_Row();
									PlayerInGameBalance vRow = new PlayerInGameBalance();

									var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == "EVOLUTIONGAMING" && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

									if (record != null)
									{
										string sql = "select CAST(COALESCE(SUM(ISNULL(payout,0) - ISNULL(wager,0)),0) AS DECIMAL(10, 2)) 'winlose' from EvolutionGame ";
										sql += "where playerId = '" + record.GamePlayerID + "' and DATEADD(HOUR, 8, settledAt) >= '" + latestrec.DATE1.ToString() + "' and DATEADD(HOUR, 8, settledAt) < '" + dtTo.ToString("yyyy-MM-dd HH:mm:ss") + "' ";
										using (SqlDataAdapter adapter = new SqlDataAdapter(sql, con))
										{
											DataTable table = new DataTable();
											adapter.Fill(table);
											if (table.Rows.Count == 1)
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount, 2);
												vRow.BalanceAt12pm = Math.Round(dAmount + Convert.ToDecimal(table.Rows[0]["winlose"]), 2);
												vRow.Game = "EVOLUTIONGAMING";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.IsSuccess = true;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;
											}
											else
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount, 2);
												vRow.BalanceAt12pm = Math.Round(dAmount, 2);
												vRow.Game = "EVOLUTIONGAMING";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.IsSuccess = true;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;
											}
											successCnt++;
										}
									}
									else
									{
										vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
										vRow.PlayerID = PlayerID;
										vRow.CashInBalance = Math.Round(dAmount, 2);
										vRow.BalanceAt12pm = null;
										vRow.Game = "EVOLUTIONGAMING";
										vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
										vRow.Remark = "Game Id Not Found";
										vRow.IsSuccess = false;
										vRow.CreatedDate = DateTime.Now;
										vRow.IsVerify = true;

										failedCnt++;
										_common.LogError("GetPlayerBalanceRecord|EVOLUTIONGAMING|" + PlayerID + " " + DateTime.Now.ToString() + " Game Id Not Found", "PlayerInGameBalance");
									}

									//model.Data.Add(vRow);
									db.PlayerInGameBalances.AddOrUpdate(vRow);

								}
								//mega
								else if (GameID == 25)
								{
									//var vRow = new PlayerBalanceVM_Row();
									PlayerInGameBalance vRow = new PlayerInGameBalance();

									var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == "MEGA" && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

									if (record != null)
									{
										string sql = "select top 1 CAST(EndMoney AS DECIMAL(10,2)) 'EndMoney' from MegaGame where UserName = '" + record.GamePlayerID + "' and DateTime >= '" + latestrec.DATE1.ToString() + "' and DateTime < '" + dtTo.ToString("yyyy-MM-dd HH:mm:ss") + "' and TableID not like '%Red Envelope%' ";
										sql += "order by DateTime desc";
										using (SqlDataAdapter adapter = new SqlDataAdapter(sql, con))
										{
											DataTable table = new DataTable();
											adapter.Fill(table);
											if (table.Rows.Count == 1)
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount, 2);
												vRow.BalanceAt12pm = Math.Round(Convert.ToDecimal(table.Rows[0]["EndMoney"]), 2);
												vRow.Game = "MEGA";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.IsSuccess = true;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;
											}
											else
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount, 2);
												vRow.BalanceAt12pm = Math.Round(dAmount, 2);
												vRow.Game = "MEGA";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.IsSuccess = true;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;
											}
											successCnt++;
										}
									}
									else
									{
										vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
										vRow.PlayerID = PlayerID;
										vRow.CashInBalance = Math.Round(dAmount, 2);
										vRow.BalanceAt12pm = null;
										vRow.Game = "MEGA";
										vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
										vRow.Remark = "Game Id Not Found";
										vRow.IsSuccess = false;
										vRow.CreatedDate = DateTime.Now;
										vRow.IsVerify = true;

										failedCnt++;
										_common.LogError("GetPlayerBalanceRecord|MEGA|" + PlayerID + " " + DateTime.Now.ToString() + " Game Id Not Found", "PlayerInGameBalance");
									}

									db.PlayerInGameBalances.AddOrUpdate(vRow);
								}
								//xe88
								else if (GameID == 30)
								{
									//var vRow = new PlayerBalanceVM_Row();
									PlayerInGameBalance vRow = new PlayerInGameBalance();

									var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == "XE88" && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

									if (record != null)
									{
										string sql = "select top 1 CAST(EndMoney AS DECIMAL(10,2)) 'EndMoney' from Xe88Game where username = 'MCT_" + record.GamePlayerID + "' and logtime >= '" + latestrec.DATE1.ToString() + "' and logtime < '" + dtTo.ToString("yyyy-MM-dd HH:mm:ss") + "' and gamename not like '%Red Envelope%' ";
										sql += "order by logtime desc";
										using (SqlDataAdapter adapter = new SqlDataAdapter(sql, con))
										{
											DataTable table = new DataTable();
											adapter.Fill(table);
											if (table.Rows.Count == 1)
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount, 2);
												vRow.BalanceAt12pm = Math.Round(Convert.ToDecimal(table.Rows[0]["EndMoney"]), 2);
												vRow.Game = "XE88";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.IsSuccess = true;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;
											}
											else
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount, 2);
												vRow.BalanceAt12pm = Math.Round(dAmount, 2);
												vRow.Game = "XE88";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.IsSuccess = true;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;
											}
											successCnt++;
										}
									}
									else
									{
										vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
										vRow.PlayerID = PlayerID;
										vRow.CashInBalance = Math.Round(dAmount, 2);
										vRow.BalanceAt12pm = null;
										vRow.Game = "XE88";
										vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
										vRow.Remark = "Game Id Not Found";
										vRow.IsSuccess = false;
										vRow.CreatedDate = DateTime.Now;
										vRow.IsVerify = true;

										failedCnt++;
										_common.LogError("GetPlayerBalanceRecord|XE88|" + PlayerID + " " + DateTime.Now.ToString() + " Game Id Not Found", "PlayerInGameBalance");
									}

									db.PlayerInGameBalances.AddOrUpdate(vRow);
								}
								//kypoker
								else if (GameID == 31)
								{
									//var vRow = new PlayerBalanceVM_Row();
									PlayerInGameBalance vRow = new PlayerInGameBalance();

									var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == "KYPOKER" && x.PlayerID.ToUpper() == PlayerID.ToUpper() && x.IsActive == false).FirstOrDefault();

									if (record != null)
									{
										string sql = "select CAST(COALESCE(SUM(ISNULL(Profit,0)),0) AS DECIMAL(10, 2)) 'winlose' from KYPokerGame ";
										sql += "where Accounts = '60602_" + record.GamePlayerID + "' and GameEndTime >= '" + latestrec.DATE1.ToString() + "' and GameEndTime < '" + dtTo.ToString("yyyy-MM-dd HH:mm:ss") + "' ";
										using (SqlDataAdapter adapter = new SqlDataAdapter(sql, con))
										{
											DataTable table = new DataTable();
											adapter.Fill(table);
											if (table.Rows.Count == 1)
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount, 2);
												vRow.BalanceAt12pm = Math.Round(dAmount + Convert.ToDecimal(table.Rows[0]["winlose"]), 2);
												vRow.Game = "KYPOKER";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.IsSuccess = true;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;
											}
											else
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount, 2);
												vRow.BalanceAt12pm = Math.Round(dAmount, 2);
												vRow.Game = "KYPOKER";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.IsSuccess = true;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;
											}
											successCnt++;
										}
									}
									else
									{
										vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
										vRow.PlayerID = PlayerID;
										vRow.CashInBalance = Math.Round(dAmount, 2);
										vRow.BalanceAt12pm = null;
										vRow.Game = "KYPOKER";
										vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
										vRow.Remark = "Game Id Not Found";
										vRow.IsSuccess = false;
										vRow.CreatedDate = DateTime.Now;
										vRow.IsVerify = true;

										failedCnt++;
										_common.LogError("GetPlayerBalanceRecord|KYPOKER|" + PlayerID + " " + DateTime.Now.ToString() + " Game Id Not Found", "PlayerInGameBalance");
									}

									//model.Data.Add(vRow);
									db.PlayerInGameBalances.AddOrUpdate(vRow);

								}
								//kypoker
								else if (GameID == 32)
								{
									//var vRow = new PlayerBalanceVM_Row();
									PlayerInGameBalance vRow = new PlayerInGameBalance();

									var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == "KYPOKER" && x.PlayerID.ToUpper() == PlayerID.ToUpper() && x.IsActive == true).FirstOrDefault();

									if (record != null)
									{
										string sql = "select CAST(COALESCE(SUM(ISNULL(Profit,0)),0) AS DECIMAL(10, 2)) 'winlose' from KYPokerGame ";
										sql += "where Accounts = '60016_" + record.GamePlayerID + "' and GameEndTime >= '" + latestrec.DATE1.ToString() + "' and GameEndTime < '" + dtTo.ToString("yyyy-MM-dd HH:mm:ss") + "' ";
										using (SqlDataAdapter adapter = new SqlDataAdapter(sql, con))
										{
											DataTable table = new DataTable();
											adapter.Fill(table);
											if (table.Rows.Count == 1)
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount, 2);
												vRow.BalanceAt12pm = Math.Round(dAmount + Convert.ToDecimal(table.Rows[0]["winlose"]), 2);
												vRow.Game = "KYPOKER";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.IsSuccess = true;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;
											}
											else
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount, 2);
												vRow.BalanceAt12pm = Math.Round(dAmount, 2);
												vRow.Game = "KYPOKER";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.IsSuccess = true;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;
											}
											successCnt++;
										}
									}
									else
									{
										vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
										vRow.PlayerID = PlayerID;
										vRow.CashInBalance = Math.Round(dAmount, 2);
										vRow.BalanceAt12pm = null;
										vRow.Game = "KYPOKER";
										vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
										vRow.Remark = "Game Id Not Found";
										vRow.IsSuccess = false;
										vRow.CreatedDate = DateTime.Now;
										vRow.IsVerify = true;

										failedCnt++;
										_common.LogError("GetPlayerBalanceRecord|KYPOKER|" + PlayerID + " " + DateTime.Now.ToString() + " Game Id Not Found", "PlayerInGameBalance");
									}

									//model.Data.Add(vRow);
									db.PlayerInGameBalances.AddOrUpdate(vRow);

								}

							}
							catch (DbEntityValidationException e)
							{
								foreach (var eve in e.EntityValidationErrors)
								{
									_common.LogError("Record " + string.Format("{0}|{1}|{2}", Convert.ToString(latestrec.KEY1?.Trim()), Convert.ToString(latestrec.INT1), Convert.ToString(Convert.ToDecimal(latestrec.INT2) / 100)) + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
											eve.Entry.Entity.GetType().Name, eve.Entry.State), "PlayerInGameBalance");
									foreach (var ve in eve.ValidationErrors)
									{
										_common.LogError("Record " + string.Format("{0}|{1}|{2}", Convert.ToString(latestrec.KEY1?.Trim()), Convert.ToString(latestrec.INT1), Convert.ToString(Convert.ToDecimal(latestrec.INT2) / 100)) + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
												ve.PropertyName, ve.ErrorMessage), "PlayerInGameBalance");
									}
								}
								return false;
							}
							catch (Exception ex)
							{
								_common.LogError("Trans Record " + string.Format("{0}|{1}|{2}", Convert.ToString(latestrec.KEY1?.Trim()), Convert.ToString(latestrec.INT1),  Convert.ToString(Convert.ToDecimal(latestrec.INT2) / 100)) + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), "PlayerInGameBalance");
							}
						}

						db.SaveChanges();
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

					AppPlayerHistoriesVM_1 PlayerHistories = new AppPlayerHistoriesVM_1 { Rows = new List<AppPlayerHistoryVM_1>() };

					using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["TaiwanConn"].ConnectionString))
					{
						con.Open();
						//#1 get the latest trans record before the date passed in 
						string sql = "select * from ( ";
						sql += "select a.*, b.STR1 as 'Game', row_number() over(partition by a.key1 order by a.date1 desc, a.no_index desc) as rn ";
						sql += "from PLAYER_HISTORY a left join GAME_STATUS b on a.INT1=b.KEY3 ";
						sql += "inner join PLAYER_REC c on UPPER(a.KEY1) = UPPER(c.KEY1) and c.INT22=1 ";
						sql += "where a.DATE1 < '" + dtTo.ToString("yyyy-MM-dd HH:mm:ss") + "' ";
						sql += ") t ";
						sql += "where t.rn = 1 and t.INT1 in (16,19,27,29) and (t.STR1 = 'Cash In' or t.STR1 = 'Chip In') ";
						sql += "order by t.DATE1 desc ";

						using (SqlDataAdapter adapter = new SqlDataAdapter(sql, con))
						{
							DataTable table = new DataTable();
							adapter.Fill(table);
							if (table.Rows.Count > 0)
							{
								foreach (DataRow row in table.Rows)
								{
									PlayerHistories.Rows.Add(new AppPlayerHistoryVM_1
									{
										Game = row["Game"].ToString(),
										KEY1 = row["KEY1"].ToString(),
										STR1 = row["STR1"].ToString(),
										INT1 = Convert.ToInt32(row["INT1"]),
										INT2 = Convert.ToInt32(row["INT2"]),
										DATE1 = row["DATE1"].ToString()
									});
								}
							}
						}
					}

					using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["BingoConn1"].ConnectionString))
					using (SqlConnection contw = new SqlConnection(ConfigurationManager.ConnectionStrings["TaiwanConn"].ConnectionString))
					{
						foreach (var latestrec in PlayerHistories.Rows)
						{
							try
							{
								int GameID = latestrec.INT1;
								string PlayerID = latestrec.KEY1?.Trim();
								decimal dAmount = 0;
								if (latestrec.INT2 > 0)
									dAmount = Convert.ToDecimal(latestrec.INT2) / 100;

								decimal cAmount = 0;
								int[] chipGames = { 16, 17, 24, 27 }; //4d, tiger, tigerfish, menang88

								if (Array.IndexOf(chipGames, GameID) > -1)
								{
									string sql1 = "";
									if (latestrec.STR1.ToUpper().Trim() == "CASH IN")
									{
										sql1 = "select top 1 * from PLAYER_HISTORY where date1 = '" + latestrec.DATE1 + "' and UPPER(KEY1) = '" + latestrec.KEY1?.Trim().ToUpper() + "' ";
										sql1 += "and INT1 = '" + GameID + "' and STR1 = 'Chip In' order by DATE1 ";
									}
									else if (latestrec.STR1.ToUpper().Trim() == "CHIP IN")
									{
										sql1 = "select top 1 * from PLAYER_HISTORY where date1 = '" + latestrec.DATE1 + "' and UPPER(KEY1) = '" + latestrec.KEY1?.Trim().ToUpper() + "' ";
										sql1 += "and INT1 = '" + GameID + "' and STR1 = 'Cash In' order by DATE1 ";
									}

									using (SqlDataAdapter adapter = new SqlDataAdapter(sql1, contw))
									{
										DataTable table = new DataTable();
										adapter.Fill(table);
										if (table.Rows.Count > 0)
											cAmount = Convert.ToDecimal(Convert.ToInt32(table.Rows[0]["INT2"])) / 100;
									}
								}

								//bt28win
								if (GameID == 16)
								{
									//var vRow = new PlayerBalanceVM_Row();
									PlayerInGameBalance vRow = new PlayerInGameBalance();

									var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == "BT28WIN" && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

									if (record != null)
									{
										//string apiurl = string.Format("http://gameapi-28win.azurewebsites.net/api/BT28Win/GetProfile?loginID={0}", record.GamePlayerID);
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
											_common.LogError("GetPlayerBalanceRecordAPI|BT28WIN|" + PlayerID + " " + DateTime.Now.ToString() + " " + responseText, "PlayerInGameBalance");
											dynamic result = JObject.Parse(responseText);

											if (Convert.ToString(result.ErrorCode) == "0")
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount + cAmount, 2);
												vRow.BalanceAt12pm = Math.Round(Convert.ToDecimal(result.Data.balance), 2);
												vRow.Game = "BT28WIN";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
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
												vRow.Game = "BT28WIN";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.Remark = "API Error";
												vRow.IsSuccess = false;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;

												failedCnt++;
											}
										}
									}
									else
									{
										vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
										vRow.PlayerID = PlayerID;
										vRow.CashInBalance = Math.Round(dAmount + cAmount, 2);
										vRow.BalanceAt12pm = null;
										vRow.Game = "BT28WIN";
										vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
										vRow.Remark = "Game Id Not Found";
										vRow.IsSuccess = false;
										vRow.CreatedDate = DateTime.Now;
										vRow.IsVerify = true;

										failedCnt++;

										_common.LogError("GetPlayerBalanceRecordAPI|BT28WIN|" + PlayerID + " " + DateTime.Now.ToString() + " Game Id Not Found", "PlayerInGameBalance");
									}

									db.PlayerInGameBalances.AddOrUpdate(vRow);

								}
								//ibc
								else if (GameID == 19)
								{
									//var vRow = new PlayerBalanceVM_Row();
									PlayerInGameBalance vRow = new PlayerInGameBalance();

									var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == "IBC" && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

									if (record != null)
									{
										//string apiurl = string.Format("http://ibc.bingotown88.com/api/IBC/CheckUserBalance?username={0}", record.GamePlayerID);
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
											_common.LogError("GetPlayerBalanceRecordAPI|IBC|" + PlayerID + " " + DateTime.Now.ToString() + " " + responseText, "PlayerInGameBalance");
											dynamic result = JObject.Parse(responseText);

											if (result.error_code == 0 && result.Data != null)
											{
												if (result.Data.Count > 0)
												{
													vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
													vRow.PlayerID = PlayerID;
													vRow.CashInBalance = Math.Round(dAmount, 2);
													vRow.BalanceAt12pm = result.Data[0].error_code == 0 ? Convert.ToDecimal(result.Data[0].balance) : null;
													vRow.Game = "IBC";
													vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
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
													vRow.CashInBalance = Math.Round(dAmount, 2);
													vRow.BalanceAt12pm = null;
													vRow.Game = "IBC";
													vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
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
												vRow.CashInBalance = Math.Round(dAmount, 2);
												vRow.BalanceAt12pm = null;
												vRow.Game = "IBC";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.Remark = "API Error";
												vRow.IsSuccess = false;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;

												failedCnt++;
											}
										}
									}
									else
									{
										vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
										vRow.PlayerID = PlayerID;
										vRow.CashInBalance = Math.Round(dAmount, 2);
										vRow.BalanceAt12pm = null;
										vRow.Game = "IBC";
										vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
										vRow.Remark = "Game Id Not Found";
										vRow.IsSuccess = false;
										vRow.CreatedDate = DateTime.Now;
										vRow.IsVerify = true;
										failedCnt++;

										_common.LogError("GetPlayerBalanceRecordAPI|IBC|" + PlayerID + " " + DateTime.Now.ToString() + " Game Id Not Found", "PlayerInGameBalance");
									}

									db.PlayerInGameBalances.AddOrUpdate(vRow);
								}
								//menang88
								else if (GameID == 27)
								{
									//var vRow = new PlayerBalanceVM_Row();
									PlayerInGameBalance vRow = new PlayerInGameBalance();

									var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == "MENANG88" && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

									if (record != null)
									{
										string apiurl = string.Format("http://gameapi-menang88.azurewebsites.net/api/Menang88/GetBalance?userId={0}", record.GamePlayerID);
										var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
										httpWebRequest.ContentType = "application/json";
										httpWebRequest.Method = "GET";
										httpWebRequest.Timeout = 900000;
										httpWebRequest.ReadWriteTimeout = 900000;

										var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

										using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
										{
											var responseText = streamReader.ReadToEnd();
											_common.LogError("GetPlayerBalanceRecordAPI|MENANG88|" + PlayerID + " " + DateTime.Now.ToString() + " " + responseText, "PlayerInGameBalance");
											dynamic result = JObject.Parse(responseText);

											if (Convert.ToString(result.status) == "1")
											{
												vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
												vRow.PlayerID = PlayerID;
												vRow.CashInBalance = Math.Round(dAmount + cAmount, 2);
												vRow.BalanceAt12pm = Math.Round(Convert.ToDecimal(result.creditbalance), 2);
												vRow.Game = "MENANG88";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
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
												vRow.Game = "MENANG88";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.Remark = "API Error";
												vRow.IsSuccess = false;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;

												failedCnt++;
											}
										}
									}
									else
									{
										vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
										vRow.PlayerID = PlayerID;
										vRow.CashInBalance = Math.Round(dAmount + cAmount, 2);
										vRow.BalanceAt12pm = null;
										vRow.Game = "MENANG88";
										vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
										vRow.Remark = "Game Id Not Found";
										vRow.IsSuccess = false;
										vRow.CreatedDate = DateTime.Now;
										vRow.IsVerify = true;

										failedCnt++;

										_common.LogError("GetPlayerBalanceRecordAPI|MENANG88|" + PlayerID + " " + DateTime.Now.ToString() + " Game Id Not Found", "PlayerInGameBalance");
									}

									db.PlayerInGameBalances.AddOrUpdate(vRow);

								}
								//sabaesports
								else if (GameID == 29)
								{
									//var vRow = new PlayerBalanceVM_Row();
									PlayerInGameBalance vRow = new PlayerInGameBalance();

									var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == "SABAESPORTS" && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

									if (record != null)
									{
										//string apiurl = string.Format("http://ibc.bingotown88.com/api/IBC/CheckUserBalance?username={0}", record.GamePlayerID);
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
											_common.LogError("GetPlayerBalanceRecordAPI|SABAESPORTS|" + PlayerID + " " + DateTime.Now.ToString() + " " + responseText, "PlayerInGameBalance");
											dynamic result = JObject.Parse(responseText);

											if (result.error_code == 0 && result.Data != null)
											{
												if (result.Data.Count > 0)
												{
													vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
													vRow.PlayerID = PlayerID;
													vRow.CashInBalance = Math.Round(dAmount, 2);
													vRow.BalanceAt12pm = result.Data[0].error_code == 0 ? Convert.ToDecimal(result.Data[0].balance) : null;
													vRow.Game = "SABAESPORTS";
													vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
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
													vRow.CashInBalance = Math.Round(dAmount, 2);
													vRow.BalanceAt12pm = null;
													vRow.Game = "SABAESPORTS";
													vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
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
												vRow.CashInBalance = Math.Round(dAmount, 2);
												vRow.BalanceAt12pm = null;
												vRow.Game = "SABAESPORTS";
												vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
												vRow.Remark = "API Error";
												vRow.IsSuccess = false;
												vRow.CreatedDate = DateTime.Now;
												vRow.IsVerify = true;

												failedCnt++;
											}
										}
									}
									else
									{
										vRow.Date = Convert.ToDateTime(dtTo.ToString("yyyy-MM-dd"));
										vRow.PlayerID = PlayerID;
										vRow.CashInBalance = Math.Round(dAmount, 2);
										vRow.BalanceAt12pm = null;
										vRow.Game = "SABAESPORTS";
										vRow.CashInDate = Convert.ToDateTime(latestrec.DATE1);
										vRow.Remark = "Game Id Not Found";
										vRow.IsSuccess = false;
										vRow.CreatedDate = DateTime.Now;
										vRow.IsVerify = true;
										failedCnt++;

										_common.LogError("GetPlayerBalanceRecordAPI|SABAESPORTS|" + PlayerID + " " + DateTime.Now.ToString() + " Game Id Not Found", "PlayerInGameBalance");
									}

									db.PlayerInGameBalances.AddOrUpdate(vRow);
								}

							}
							catch (DbEntityValidationException e)
							{
								foreach (var eve in e.EntityValidationErrors)
								{
									_common.LogError("Record " + string.Format("{0}|{1}|{2}", Convert.ToString(latestrec.KEY1?.Trim()), Convert.ToString(latestrec.INT1), Convert.ToString(Convert.ToDecimal(latestrec.INT2) / 100)) + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
											eve.Entry.Entity.GetType().Name, eve.Entry.State), "PlayerInGameBalance");
									foreach (var ve in eve.ValidationErrors)
									{
										_common.LogError("Record " + string.Format("{0}|{1}|{2}", Convert.ToString(latestrec.KEY1?.Trim()), Convert.ToString(latestrec.INT1), Convert.ToString(Convert.ToDecimal(latestrec.INT2) / 100)) + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
												ve.PropertyName, ve.ErrorMessage), "PlayerInGameBalance");
									}
								}
								return false;
							}
							catch (Exception ex)
							{
								_common.LogError("Trans Record " + string.Format("{0}|{1}|{2}", Convert.ToString(latestrec.KEY1?.Trim()), Convert.ToString(latestrec.INT1), Convert.ToString(Convert.ToDecimal(latestrec.INT2) / 100)) + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), "PlayerInGameBalance");
							}
						}
						db.SaveChanges();
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