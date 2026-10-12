using GameRecord.Controllers.V2;
using GameRecord.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static GameRecord.Controllers.BaseConnector;

namespace GameRecord.Controllers
{
	public class YeeBetController : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "YEEBET";
		Common _common = new Common();
		private YeeBetConnector api = new YeeBetConnector(Environment: BaseConnector.GameEnvironment.PROD);

		//logic app will call this (template for paged game record 2023-10-02)
		//http://localhost:51148/YeeBet/YeeBetJob
		public string YeeBetJob()
		{
			try
			{
				lock (MyLock)
				{
					api = new YeeBetConnector(P_GamePrefix, Environment: GameEnvironment.PROD);
					using (BingoConn db = new BingoConn())
					{
						var adm = db.ADMJobDates.Where(s => s.GameProvider == P_GamePrefix).FirstOrDefault();
						if (adm == null || adm.LastRunTillDate == null)
							return "Please setup ADMJobDates with LastRunTillDate.";

						DateTime dtStart = adm.LastRunTillDate.Value;
						DateTime dtEnd = _common.DtNowTrimMillisecond();

						int iMax = 2000;
						int iPage = 0;
						int iProcessed = 0;
						bool bEnd = false;
						bool bError = false;

						while (!bEnd)
						{
							var result = api.GetBetHistory(_common.ToUnixTime(dtStart.AddHours(-8).AddMinutes(-15)), _common.ToUnixTime(dtEnd.AddHours(-8)), iMax, iPage);
							if (result == null)
							{
								bEnd = true;
								bError = true;
								break;
							}
							else
							{
								if (result.array != null && result.array.Count > 0)
								{
									foreach (var r in result.array)
									{
										string s = "insert into YeeBetGame (id,serialnumber,userid,username,gameid,gameroundid,gameroundno,gameresult,state,bettype,betpoint,betamount,winlost,commamount,commratio,balance,betodds,updatetime,createtime,CreatedDate) ";
										s += "select * from (values (" + r.id + "," + r.serialnumber + "," + r.userid + "," + _common.F1(r.username) + "," + r.gameid + "," + r.gameroundid + "," + r.gameroundno + "," + _common.F1(r.gameresult) + "," + r.state + "," + r.bettype + "," + _common.F1(r.betpoint) + "," + r.betamount + "," + r.winlost + "," + r.commamount + "," + _common.F1(r.commratio) + "," + r.balance + "," + r.betodds + ",'" + r.updatetime.ToString("yyyy-MM-dd HH:mm:ss") + "','" + r.createtime.ToString("yyyy-MM-dd HH:mm:ss") + "',getdate())) as a(id,serialnumber,userid,username,gameid,gameroundid,gameroundno,gameresult,state,bettype,betpoint,betamount,winlost,commamount,commratio,balance,betodds,updatetime,createtime,CreatedDate) ";
										s += "where not exists (select * from YeeBetGame b with (updlock) where b.id=a.id);";
										_common.Sql_RunAndCommit(s);
										iProcessed++;
									}

									if (result.array.Count >= iMax)
										iPage++;
									else
										bEnd = true;
								}
								else
								{
									bEnd = true;
								}
							}
						}

						if (!bError)
						{
							adm.LastRunTillDate = dtEnd;
							db.SaveChanges();
						}

						return "YeeBetJob has processed " + iProcessed + " record(s) from " + dtStart.ToString("yyyy-MM-dd HH:mm:ss") + " until " + dtEnd.ToString("yyyy-MM-dd HH:mm:ss") + (bError ? " with error." : "");
					}
				}
			}
			catch (Exception ex)
			{
				_common.LogError("Error at YeeBet/YeeBetJob: " + ex.Message.ToString(), P_GamePrefix);
				return "Error at YeeBet/YeeBetJob: " + ex.Message.ToString();
			}
		}

		//http://localhost:51148/YeeBet/GetBetLimitInterface
		//https://c9live-gamerecord.azurewebsites.net/YeeBet/GetBetLimitInterface
		public string GetBetLimitInterface()
		{
			try
			{
				using (BingoConn db = new BingoConn())
				{
					api = new YeeBetConnector(P_GamePrefix, Environment: GameEnvironment.PROD);
					var result = api.BetLimitInterface();

					return JsonConvert.SerializeObject(result);
				}
			}
			catch (Exception ex)
			{
				_common.LogError("Error at YeeBet/GetBetLimitInterface: " + ex.Message.ToString(), P_GamePrefix);
				return "Error at YeeBet/GetBetLimitInterface: " + ex.Message.ToString();
			}
		}
	}
}