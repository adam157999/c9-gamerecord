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
using System.Data.Entity.Migrations;
using System.Data.Entity.Validation;
using System.Configuration;

namespace GameRecord.Controllers
{
	public class IBCController : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "IBC";
		//private string P_ApiUrl = " http://api.A0290.mcibc.btagent.net";
		private string P_ApiUrl = "http://api.A0290.mctvvip.com";
		private string P_VendorId = "yzz7xxwtxr";
		public string smsPhoneList = ConfigurationManager.AppSettings["IBCJobPhoneList"];
		public string smsContent = "New ticket coming in. \n https://bit.ly/3vMHNZA";
		//public string smsContent = "New IBC betting ticket coming in. View latest running ticket list by clicking below link. \n https://agent2u.vip/ReportExternal/IbcPendingFundReport_Pw";
		public bool isSend = false;
		Common _common = new Common();
		private IBCConnector api = new IBCConnector(Environment: BaseConnector.GameEnvironment.PROD);

		public string GetSportName(int id)
		{
			switch (id)
			{
				case 1:
					return "Soccer";
				case 2:
					return "Basketball";
				case 3:
					return "Football";
				case 4:
					return "Ice Hockey";
				case 5:
					return "Tennis";
				case 6:
					return "Volleyball";
				case 7:
					return "Snooker/Pool";
				case 8:
					return "Baseball";
				case 9:
					return "Badminton";
				case 10:
					return "Golf";
				case 11:
					return "Motorsports";
				case 12:
					return "Swimming";
				case 13:
					return "Politics";
				case 14:
					return "Water Polo";
				case 15:
					return "Diving";
				case 16:
					return "Boxing/MMA";
				case 17:
					return "Archery";
				case 18:
					return "Table Tennis";
				case 19:
					return "Weightlifting";
				case 20:
					return "Canoeing";
				case 21:
					return "Gymnastics";
				case 22:
					return "Athletics";
				case 23:
					return "Equestrian";
				case 24:
					return "Handball";
				case 25:
					return "Darts";
				case 26:
					return "Rugby";
				case 28:
					return "Field Hockey";
				case 29:
					return "Winter Sports";
				case 30:
					return "Squash";
				case 31:
					return "Entertainment";
				case 32:
					return "Netball";
				case 33:
					return "Cycling";
				case 34:
					return "Fencing";
				case 35:
					return "Judo";
				case 36:
					return "M. Pentathlon";
				case 37:
					return "Rowing";
				case 38:
					return "Sailing";
				case 39:
					return "Shooting";
				case 40:
					return "Taekwondo";
				case 41:
					return "Triathlon";
				case 42:
					return "Wrestling";
				case 43:
					return "E-Sports";
				case 44:
					return "Muay Thai";
				case 47:
					return "Kabaddi";
				case 48:
					return "Sepak Takraw";
				case 49:
					return "Futsal";
				case 50:
					return "Cricket";
				case 51:
					return "Beach Soccer";
				case 52:
					return "Poker";
				case 53:
					return "Chess";
				case 54:
					return "Olympics";
				case 55:
					return "Finance";
				case 99:
					return "Other Sports";
				case 161:
					return "Number Game";
				case 164:
					return "Happy 5";
				case 180:
					return "Virtual Soccer";
				case 181:
					return "Virtual Horse Racing";
				case 182:
					return "Virtual Greyhound";
				case 183:
					return "Virtual Speedway";
				case 184:
					return "Virtual Motorsports";
				case 185:
					return "Virtual Cycling";
				case 186:
					return "Virtual Tennis";
				case 190:
					return "Virtual Soccer League";
				case 191:
					return "Virtual Soccer Nation";
				case 192:
					return "Virtual Soccer World Cup";
				case 193:
					return "Virtual Basketball";
				case 194:
					return "Virtual Soccer Asian Cup";
				case 195:
					return "Virtual Soccer English Premier";
				case 196:
					return "Virtual Soccer Champions Cup";
				case 197:
					return "Virtual Soccer Euro Cup";
				case 199:
					return "Virtual Sports Parlay";
				case 154:
					return "HorseRacing FixedOdds";
				case 204:
					return "Colossus Bet";
				case 165:
					return "Card Club";
				case 200:
					return "Saba.club";
				case 209:
					return "RNG - Mini Games";
				case 210:
					return "RNG - Mobile";
				case 212:
					return "Macau Games";
				case 219:
					return "Fishing World";
				case 245:
					return "Virtual Games";
				case 247:
					return "SG";
				case 248:
					return "Pragmatic Play";
				case 162:
					return "Live Casino";
				case 211:
					return "Allbet";
				case 243:
					return "AE Casino";
				case 202:
					return "RNG Keno";
				case 220:
					return "Lottery";
				case 222:
					return "Table Game";
				case 223:
					return "Indo 4D";
				default:
					return "";
			}
		}

		public static string BuildPostData(Dictionary<string, string> d)
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

		public bool UpdJobTime(string startTime, string endTime, string sMainID, string lastVersionKey)
		{
			try
			{
				DateTime dtEnd = Convert.ToDateTime(endTime);

				_common.UpdateADMJobDate(P_GamePrefix, dtEnd, lastVersionKey);

				//using (var bingoConn = new BingoConn())
				//{
				//	var xyz = bingoConn.ADMJobDates.Where(s => s.GameProvider == P_GamePrefix).FirstOrDefault();
				//	if (xyz != null)
				//	{
				//		xyz.LastRunTillDate = dtEnd;
				//		xyz.LastVersionKey = lastVersionKey;

				//		//bingoConn.Entry(xyz).State = System.Data.Entity.EntityState.Modified;
				//		bingoConn.SaveChanges();
				//	}
				//}
				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("IBC/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "IBC");
				return false;
			}
		}

		public string GetLastVersionKey()
		{
			try
			{
				string sKey = "";

				using (var bingoConn = new BingoConn())
				{
					var query = bingoConn.ADMJobDates.Where(s => s.GameProvider == P_GamePrefix).FirstOrDefault();

					sKey = query.LastVersionKey;
				}

				return sKey;
			}
			catch (Exception ex)
			{
				_common.LogError("IBC/GetLastVersionKey" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "IBC");
				return "Error:" + ex.Message.ToString();
			}
		}

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
						sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					}
					else
					{
						DateTime dtSTime = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm")) - new TimeSpan(0, 0, 3, 0, 0);
						sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					}
				}

				return sTime;

			}
			catch (Exception ex)
			{
				_common.LogError("IBC/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "IBC");
				return "Error:" + ex.Message.ToString();
			}
		}

		//http://localhost:51148/ibc/IBCJob
		public void IBCJob()
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
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "IBC");
			}
		}

		public bool GetGameRecord()
		{
			string startTime = GetJobTime("Start");
			string endTime = GetJobTime("End");
			string versionKey = GetLastVersionKey();
			bool isEnd = false;

			try
			{
				while (!isEnd)
				{
					//pplim
					//IBC.GetVersionkeyByDateResponse result1 = api.GetVersionkeyByDate("2023-03-23T00:00:00");

					IBC.GetBetDetailResponse result = api.GetBetDetail(versionKey);

					using (BingoConn db = new BingoConn())
					{
						string sMainID = Guid.NewGuid().ToString();
						IBCGameMain ibcGameMain = new IBCGameMain()
						{
							Id = sMainID,
							VersionKey = versionKey,
							ErrorCode = result != null ? result.error_code : (int?)null,
							Message = result != null ? result.message : null,
							StartTime = Convert.ToDateTime(startTime),
							EndTime = Convert.ToDateTime(endTime),
							CreatedDate = DateTime.Now
						};
						db.IBCGameMains.Add(ibcGameMain);
						db.SaveChanges();

						if (Convert.ToString(result.error_code) == "0")
						{
							if (result.Data != null)
							{
								if (result.Data.BetDetails != null)
								{
									foreach (var obj in result.Data.BetDetails)
									{
										string sBetFrom = obj.betfrom?.Substring(0, 1);

										IBCBetDetail ibcBetDetail = new IBCBetDetail()
										{
											fId = sMainID,
											trans_id = Convert.ToInt64(obj.trans_id),
											vendor_member_id = obj.vendor_member_id,
											operator_id = obj.operator_id,
											league_id = obj.league_id.HasValue ? obj.league_id : null,
											leaguename = JsonConvert.SerializeObject(obj.leaguename),
											match_id = obj.match_id.HasValue ? obj.match_id : null,
											team_id = obj.team_id.HasValue ? obj.team_id : null,
											home_id = obj.home_id.HasValue ? obj.home_id : null,
											hometeamname = JsonConvert.SerializeObject(obj.hometeamname),
											away_id = obj.away_id.HasValue ? obj.away_id : null,
											awayteamname = JsonConvert.SerializeObject(obj.awayteamname),
											//match_datetime = b.ContainsKey("match_datetime") ? Convert.ToDateTime(obj.match_datetime) : null,
											match_datetime = obj.match_datetime.HasValue ? obj.match_datetime.Value == DateTime.MinValue ? null : obj.match_datetime : null,
											sport_type = obj.sport_type.HasValue ? obj.sport_type : null,
											sport_name = obj.sport_type.HasValue ? GetSportName(obj.sport_type.Value) : null,
											bet_type = obj.bet_type.HasValue ? obj.bet_type : null,
											parlay_ref_no = obj.parlay_ref_no.HasValue ? obj.parlay_ref_no : null,
											odds = obj.odds.HasValue ? obj.odds : null,
											original_stake = obj.original_stake.HasValue ? obj.original_stake : null,
											stake = obj.stake.HasValue ? obj.stake : null,
											range = obj.range.HasValue ? obj.range : null,
											validbetamount = obj.validbetamount.HasValue ? obj.validbetamount : null,
											transaction_time = obj.transaction_time.HasValue ? obj.transaction_time.Value == DateTime.MinValue ? null : obj.transaction_time : null,
											//transaction_time = b.ContainsKey("transaction_time") ? Convert.ToDateTime(obj.transaction_time) : null,
											ticket_status = obj.ticket_status,
											commission = obj.commission.HasValue ? obj.commission : null,
											buyback_amount = obj.buyback_amount.HasValue ? obj.buyback_amount : null,
											winlost_amount = obj.winlost_amount.HasValue ? obj.winlost_amount : null,
											after_amount = obj.after_amount.HasValue ? obj.after_amount : null,
											currency = obj.currency.HasValue ? obj.currency : null,
											winlost_datetime = obj.winlost_datetime.HasValue ? obj.winlost_datetime.Value == DateTime.MinValue ? null : obj.winlost_datetime : null,
											//winlost_datetime = b.ContainsKey("winlost_datetime") ? Convert.ToDateTime(obj.winlost_datetime) : null,
											odds_type = obj.odds_type.HasValue ? obj.odds_type : null,
											odds_info = obj.odds_info,
											lottery_bettype = obj.lottery_bettype,
											bet_team = obj.bet_team,
											game_no = obj.game_no,
											excluding = obj.excluding,
											isLucky = obj.isLucky,
											parlay_type = obj.parlay_type,
											combo_type = obj.combo_type,
											bet_tag = obj.bet_tag,
											pool_type = obj.pool_type,
											betchoice = obj.betchoice,
											home_hdp = obj.home_hdp.HasValue ? obj.home_hdp : null,
											away_hdp = obj.away_hdp.HasValue ? obj.away_hdp : null,
											hdp = obj.hdp.HasValue ? obj.hdp : null,
											ou_hdp = obj.ou_hdp.HasValue ? obj.ou_hdp : null,
											//betfrom = obj.betfrom,
											betfrom = sBetFrom,
											islive = obj.islive,
											home_score = obj.home_score.HasValue ? obj.home_score : null,
											away_score = obj.away_score.HasValue ? obj.away_score : null,
											os = obj.os,
											browser = obj.browser,
											settlement_time = obj.settlement_time.HasValue ? obj.settlement_time.Value == DateTime.MinValue ? null : obj.settlement_time : null,
											customInfo1 = obj.customInfo1,
											customInfo2 = obj.customInfo2,
											customInfo3 = obj.customInfo3,
											customInfo4 = obj.customInfo4,
											customInfo5 = obj.customInfo5,
											ba_status = obj.ba_status,
											bonus_code = obj.bonus_code,
											wallet_type = obj.wallet_type.HasValue ? obj.wallet_type : null,
											ref_code = obj.ref_code.HasValue ? obj.ref_code : null,
											version_key = obj.version_key.HasValue ? obj.version_key : null,
											ParlayData = JsonConvert.SerializeObject(obj.ParlayData),
											ColossusBetData = JsonConvert.SerializeObject(obj.ColossusBetData),
											CashOutData = JsonConvert.SerializeObject(obj.CashOutData),
											SingleParlayData = JsonConvert.SerializeObject(obj.SingleParlayData),
										};

										if (Convert.ToString(obj.vendor_member_id).ToUpper().Trim().StartsWith("AONE"))
										{
											long transID = Convert.ToInt64(obj.trans_id);
											var record = db.IBCBetDetails.Where(x => x.trans_id == transID).FirstOrDefault();

											if (record == null)
											{
												ibcBetDetail.CreatedDate = DateTime.Now;
												//send sms to alert got new ticket
												//if (!isSend)
												//{
												//	foreach (var phone in smsPhoneList.Split(','))
												//	{
												//		_common.SendSmsToBulkSms(phone, smsContent);
												//	}
												//	isSend = true;
												//}
											}
											else
											{
												ibcBetDetail.CreatedDate = record.CreatedDate;
												ibcBetDetail.ModifiedDate = DateTime.Now;
											}

											db.IBCBetDetails.AddOrUpdate(ibcBetDetail);
										}
									}
									db.SaveChanges();
								}

								if (result.Data.BetNumberDetails != null)
								{
									foreach (var obj in result.Data.BetNumberDetails)
									{
										IBCBetNumberDetail ibcNumberDetail = new IBCBetNumberDetail()
										{
											fId = sMainID,
											trans_id = Convert.ToInt64(obj.trans_id),
											vendor_member_id = obj.vendor_member_id,
											operator_id = obj.operator_id,
											match_id = obj.match_id.HasValue ? obj.match_id : null,
											transaction_time = obj.transaction_time.HasValue ? obj.transaction_time.Value == DateTime.MinValue ? null : obj.transaction_time : null,
											//transaction_time = n.ContainsKey("transaction_time") ? Convert.ToDateTime(obj.transaction_time) : null,
											odds = obj.odds,
											stake = obj.stake,
											ticket_status = obj.ticket_status,
											betfrom = obj.betfrom,
											islive = obj.islive,
											last_ball_no = obj.last_ball_no,
											bet_team = obj.bet_team,
											winlost_datetime = obj.winlost_datetime,
											//winlost_datetime = n.ContainsKey("winlost_datetime") ? Convert.ToDateTime(obj.winlost_datetime) : null,
											bet_type = obj.bet_type,
											currency = obj.currency,
											odds_type = obj.odds_type,
											sport_type = obj.sport_type,
											sport_name = obj.sport_type.HasValue ? GetSportName(obj.sport_type.Value) : null,
											winlost_amount = obj.winlost_amount,
											after_amount = obj.after_amount,
											customInfo1 = obj.customInfo1,
											customInfo2 = obj.customInfo2,
											customInfo3 = obj.customInfo3,
											customInfo4 = obj.customInfo4,
											customInfo5 = obj.customInfo5,
											ba_status = obj.ba_status,
											version_key = obj.version_key,
										};

										if (Convert.ToString(obj.vendor_member_id).ToUpper().Trim().StartsWith("AONE"))
										{
											long transID = Convert.ToInt64(obj.trans_id);
											var record = db.IBCBetNumberDetails.Where(x => x.trans_id == transID).FirstOrDefault();

											if (record == null)
											{
												ibcNumberDetail.CreatedDate = DateTime.Now;
											}
											else
											{
												ibcNumberDetail.CreatedDate = record.CreatedDate;
												ibcNumberDetail.ModifiedDate = DateTime.Now;
											}
											db.IBCBetNumberDetails.AddOrUpdate(ibcNumberDetail);
										}
									}
									db.SaveChanges();
								}

								if (result.Data.BetVirtualSportDetails != null)
								{
									foreach (var obj in result.Data.BetVirtualSportDetails)
									{
										IBCBetVirtualSportDetail ibcBetVirtualSportDetail = new IBCBetVirtualSportDetail()
										{
											fId = sMainID,
											trans_id = Convert.ToInt64(obj.trans_id),
											vendor_member_id = obj.vendor_member_id,
											operator_id = obj.operator_id,
											league_id = obj.league_id,
											leaguename = JsonConvert.SerializeObject(obj.leaguename),
											match_id = obj.match_id,
											home_id = obj.home_id,
											hometeamname = JsonConvert.SerializeObject(obj.hometeamname),
											away_id = obj.away_id,
											awayteamname = JsonConvert.SerializeObject(obj.awayteamname),
											match_datetime = obj.match_datetime,
											sport_type = obj.sport_type,
											sport_name = obj.sport_type.HasValue ? GetSportName(obj.sport_type.Value) : null,
											bet_type = obj.bet_type,
											parlay_ref_no = obj.parlay_ref_no,
											odds = obj.odds,
											stake = obj.stake,
											transaction_time = obj.transaction_time,
											ticket_status = obj.ticket_status,
											winlost_amount = obj.winlost_amount,
											after_amount = obj.after_amount,
											currency = obj.currency,
											winlost_datetime = obj.winlost_datetime,
											odds_type = obj.odds_type,
											bet_team = obj.bet_team,
											home_hdp = obj.home_hdp,
											away_hdp = obj.away_hdp,
											hdp = obj.hdp,
											betfrom = obj.betfrom,
											customInfo1 = obj.customInfo1,
											customInfo2 = obj.customInfo2,
											customInfo3 = obj.customInfo3,
											customInfo4 = obj.customInfo4,
											customInfo5 = obj.customInfo5,
											ba_status = obj.ba_status,
											version_key = obj.version_key,
											ParlayData = JsonConvert.SerializeObject(obj.ParlayData),
										};

										if (Convert.ToString(obj.vendor_member_id).ToUpper().Trim().StartsWith("AONE"))
										{
											long transID = Convert.ToInt64(obj.trans_id);
											var record = db.IBCBetVirtualSportDetails.Where(x => x.trans_id == transID).FirstOrDefault();

											if (record == null)
											{
												ibcBetVirtualSportDetail.CreatedDate = DateTime.Now;
											}
											else
											{
												ibcBetVirtualSportDetail.CreatedDate = record.CreatedDate;
												ibcBetVirtualSportDetail.ModifiedDate = DateTime.Now;
											}
											db.IBCBetVirtualSportDetails.AddOrUpdate(ibcBetVirtualSportDetail);
										}
									}
									db.SaveChanges();
								}

								if (result.Data.BetDetails != null || result.Data.BetNumberDetails != null || result.Data.BetVirtualSportDetails != null)
								{
									isEnd = false;
									versionKey = Convert.ToString(result.Data.last_version_key.Value);
								}
								else
								{
									isEnd = true;
									if (result.Data.last_version_key.HasValue)
										UpdJobTime(startTime, endTime, sMainID, Convert.ToString(result.Data.last_version_key.Value));
								}
							}
						}
					}
				}

				return true;
			}
			//catch (DbEntityValidationException e)
			//{
			//	foreach (var eve in e.EntityValidationErrors)
			//	{
			//		_common.LogError("IBC/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
			//				eve.Entry.Entity.GetType().Name, eve.Entry.State), "IBC");
			//		foreach (var ve in eve.ValidationErrors)
			//		{
			//			_common.LogError("IBC/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
			//					ve.PropertyName, ve.ErrorMessage), "IBC");
			//		}
			//	}
			//	return false;
			//}
			catch (Exception ex)
			{
				_common.LogError("IBC/GetGameRecord" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "IBC");
				return false;
			}
		}
	}
}