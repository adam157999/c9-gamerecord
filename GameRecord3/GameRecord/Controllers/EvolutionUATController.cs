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
using Newtonsoft.Json.Linq;
using System.Data.Entity.Migrations;
using System.Data.Entity.Validation;
using System.Globalization;

namespace GameRecord.Controllers
{
	public class EvolutionUATController : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "Evolution";
		private string P_ApiUrl = "http://staging.admin.asia-live.com";
		private string P_CasinoKey = "ruvp1u73g7geizld";
		private string P_ApiToken = "17073803e10786882df91fe823e37311";
		//private string P_ApiUrl = "https://admin.luckylivegames.com";
		//private string P_CasinoKey = "v6zjpkbec0awui69";
		//private string P_ApiToken = "7686953ce889de9bdd1c5eec06274c6c";
		private int P_TimeoutSec = 120000;
		private string P_FilePrefix = "Evolution";
		private string P_DTFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
		Common _common = new Common();

		public bool UpdJobTime(string startTime, string endTime, string sMainID)
		{
			try
			{
				DateTime dtEnd = Convert.ToDateTime(endTime);

				using (var bingoConn = new Bingo_stagingConn())
				{
					var xyz = bingoConn.ADMJobDateUATs.Where(s => s.GameProvider == P_GamePrefix).FirstOrDefault();
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
				_common.LogError("Evolution/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
				return false;
			}
		}

		public string GetJobTime(string action)
		{
			try
			{
				string sTime = "";

				using (var bingoConn = new Bingo_stagingConn())
				{
					var query = bingoConn.ADMJobDateUATs.Where(s => s.GameProvider == P_GamePrefix).FirstOrDefault();

					if (action == "Start")
					{
						DateTime dtSTime = Convert.ToDateTime(query.LastRunTillDate?.ToString("yyyy-MM-dd HH:mm"));
						sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					}
					else
					{
						DateTime dtSTime = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm")) - new TimeSpan(0, 8, 15, 0, 0);
						sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					}
				}

				return sTime;

			}
			catch (Exception ex)
			{
				_common.LogError("Evolution/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
				return "Error:" + ex.Message.ToString();
			}
		}

		public void EvoJob()
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
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
			}
		}

		public bool GetGameRecord()
		{
			string startTime = GetJobTime("Start");
			string endTime = GetJobTime("End");
			string sMainID = Guid.NewGuid().ToString();
			string mStartTime = startTime;
			string mEndTime = endTime;
			int limit = 1440; // 24 hours

			try
			{
				int MinDiff = Convert.ToInt32((Convert.ToDateTime(endTime) - Convert.ToDateTime(startTime)).TotalMinutes);
				int times = (MinDiff / limit) + 1;
				for (int j = limit; j <= times * limit; j += limit)
				{
					mStartTime = j == limit ? startTime : mEndTime;

					if ((Convert.ToDateTime(mStartTime).AddMinutes(limit) - Convert.ToDateTime(endTime)).TotalMinutes > 0)
						mEndTime = endTime;
					else
						mEndTime = Convert.ToString(Convert.ToDateTime(mStartTime).AddMinutes(limit));
					
					string webAddr = $"{P_ApiUrl}/api/gamehistory/v1/casino/games";
					string param = $"?startDate={Convert.ToDateTime(mStartTime).ToString(P_DTFormat)}&endDate={Convert.ToDateTime(mEndTime).ToString(P_DTFormat)}";
					webAddr = webAddr + param;
					var httpWebRequest = (HttpWebRequest)WebRequest.Create(webAddr);
					httpWebRequest.ContentType = "application/json";
					httpWebRequest.Method = "GET";
					httpWebRequest.Timeout = P_TimeoutSec;
					httpWebRequest.ReadWriteTimeout = P_TimeoutSec;
					httpWebRequest.Headers.Add("Authorization", "Basic " + GetAuthHeader(P_CasinoKey, P_ApiToken));

					var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
					using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
					{
						var responseText = streamReader.ReadToEnd();
						//_common.LogError(responseText, P_FilePrefix);
						dynamic result = JObject.Parse(responseText);

						using (Bingo_stagingConn db = new Bingo_stagingConn())
						{
							EvolutionGameMainUAT evoGameMain = new EvolutionGameMainUAT()
							{
								Id = sMainID,
								Total = null,
								StartTime = Convert.ToDateTime(startTime),
								EndTime = Convert.ToDateTime(endTime),
								CreatedDate = DateTime.Now
							};
							db.EvolutionGameMainUATs.Add(evoGameMain);
							db.SaveChanges();

							if (result.data != null)
							{
								if (result.data.Count > 0)
								{
									foreach (var d in result.data)
									{
										if (d.games.Count > 0)
										{
											foreach (var g in d.games)
											{
												string id = Convert.ToString(g.id);
												try
												{
													if (g.participants != null)
													{
														foreach (var p in g.participants)
														{
															string playerId = Convert.ToString(p.playerId);
															if (p.bets != null)
															{
																foreach (var b in p.bets)
																{
																	string betCode = b.code;
																	string betTransactionId = b.transactionId;
																	EvolutionGameUAT evoGame = new EvolutionGameUAT()
																	{
																		fId = sMainID,
																		uuid = result.uuid,
																		timestamp = Convert.ToDateTime(result.timestamp),
																		date = Convert.ToDateTime(d.date),
																		id = g.id,
																		startedAt = g.startedAt != null ? Convert.ToDateTime(g.startedAt) : null,
																		settledAt = g.settledAt != null ? Convert.ToDateTime(g.settledAt) : null,
																		status = g.status,
																		gameType = g.gameType,
																		gameSubType = g.gameSubType,
																		tableId = g.table != null ? g.table.id : null,
																		tableName = g.table != null ? g.table.name : null,
																		dealerUid = g.dealer != null ? g.dealer.uid : null,
																		dealerName = g.dealer != null ? g.dealer.name : null,
																		currency = g.currency,
																		wager = g.wager != null ? Convert.ToDecimal(g.wager) : null,
																		payout = g.payout != null ? Convert.ToDecimal(g.payout) : null,
																		casinoId = p.casinoId,
																		playerId = p.playerId,
																		screenName = p.screenName,
																		sessionId = p.sessionId,
																		casinoSessionId = p.casinoSessionId,
																		participantCurrency = p.currency,
																		betCoverage = p.betCoverage != null ? JsonConvert.SerializeObject(p.betCoverage) : null,
																		bets = p.bets != null ? JsonConvert.SerializeObject(p.bets) : null,
																		betCode = b.code,
																		betStake = b.stake != null ? Convert.ToDecimal(b.stake) : null,
																		betPayout = b.payout != null ? Convert.ToDecimal(b.payout) : null,
																		betPlacedOn = b.placedOn != null ? Convert.ToDateTime(b.placedOn) : null,
																		betDescription = b.description,
																		betTransactionId = b.transactionId,
																		configOverlays = p.configOverlays != null ? JsonConvert.SerializeObject(p.configOverlays) : null,
																		playMode = p.playMode,
																		channel = p.channel,
																		os = p.os,
																		device = p.device,
																		seats = p.seats != null ? JsonConvert.SerializeObject(p.seats) : null,
																		decisions = p.decisions != null ? JsonConvert.SerializeObject(p.decisions) : null,
																		sideBetPlayerPair = p.sideBetPlayerPair != null ? JsonConvert.SerializeObject(p.sideBetPlayerPair) : null,
																		sideBetBankerPair = p.sideBetBankerPair != null ? JsonConvert.SerializeObject(p.sideBetBankerPair) : null,
																		sideBetPerfectPair = p.sideBetPerfectPair != null ? JsonConvert.SerializeObject(p.sideBetPerfectPair) : null,
																		sideBetEitherPair = p.sideBetEitherPair != null ? JsonConvert.SerializeObject(p.sideBetEitherPair) : null,
																		sideBetPlayerBonus = p.sideBetPlayerBonus != null ? JsonConvert.SerializeObject(p.sideBetPlayerBonus) : null,
																		sideBetBankerBonus = p.sideBetBankerBonus != null ? JsonConvert.SerializeObject(p.sideBetBankerBonus) : null,
																		sideBetSuperSix = p.sideBetSuperSix != null ? JsonConvert.SerializeObject(p.sideBetSuperSix) : null,
																		sideBetAABonus = p.sideBetAABonus != null ? JsonConvert.SerializeObject(p.sideBetAABonus) : null,
																		sideBet5p1 = p.sideBet5p1 != null ? JsonConvert.SerializeObject(p.sideBet5p1) : null,
																		sideBetPairPlus = p.sideBetPairPlus != null ? JsonConvert.SerializeObject(p.sideBetPairPlus) : null,
																		sideBet6CardBonus = p.sideBet6CardBonus != null ? JsonConvert.SerializeObject(p.sideBet6CardBonus) : null,
																		sideBetPairOrBetter = p.sideBetPairOrBetter != null ? JsonConvert.SerializeObject(p.sideBetPairOrBetter) : null,
																		sideBetTrips = p.sideBetTrips != null ? JsonConvert.SerializeObject(p.sideBetTrips) : null,
																		sideBetBestFive = p.sideBetBestFive != null ? JsonConvert.SerializeObject(p.sideBetBestFive) : null,
																		suitedTie = p.suitedTie != null ? JsonConvert.SerializeObject(p.suitedTie) : null,
																		result = p.result != null ? JsonConvert.SerializeObject(p.result) : null
																	};

																	var record = db.EvolutionGameUATs.Where(x => x.id == id && x.playerId == playerId && x.betCode == betCode && x.betTransactionId == betTransactionId).FirstOrDefault();

																	if (record == null)
																	{
																		evoGame.CreatedDate = DateTime.Now;
																	}
																	else
																	{
																		evoGame.CreatedDate = record.CreatedDate;
																		evoGame.ModifiedDate = DateTime.Now;
																	}
																	db.EvolutionGameUATs.AddOrUpdate(evoGame);
																}
															}
														}
													}
												}
												catch (DbEntityValidationException e)
												{
													foreach (var eve in e.EntityValidationErrors)
													{
														_common.LogError("Evolution/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
																eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
														foreach (var ve in eve.ValidationErrors)
														{
															_common.LogError("Evolution/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
																	ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
														}
													}
												}
												catch (WebException ex)
												{
													var error = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
													_common.LogError("Record " + id + " " + DateTime.Now.ToString() + " " + ex.Message.ToString() + " " + error, P_FilePrefix);
												}
												catch (Exception ex)
												{
													_common.LogError("Record " + id + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
												}
											}
											db.SaveChanges();
										}
									}
								}
							}

							bool isUpdJob = UpdJobTime(startTime, endTime, sMainID);
						}
					}
				}


				return true;
			}
			catch (DbEntityValidationException e)
			{
				foreach (var eve in e.EntityValidationErrors)
				{
					_common.LogError("Evolution/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
							eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
					foreach (var ve in eve.ValidationErrors)
					{
						_common.LogError("Evolution/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
								ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
					}
				}
				return false;
			}
			catch (WebException ex)
			{
				var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
				_common.LogError("Evolution/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString() + " " + responseText, P_FilePrefix);
				return false;
			}
			catch (Exception ex)
			{
				_common.LogError("Evolution/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
				return false;
			}
		}

		public string GetAuthHeader(string key, string token)
		{
			string s = key + ":" + token;
			byte[] bytes = Encoding.UTF8.GetBytes(s);
			return Convert.ToBase64String(bytes);
		}
	}
}