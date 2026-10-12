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
using static GameRecord.Controllers.BaseConnector;

namespace GameRecord.Controllers
{
	public class VBossController : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "VBOSS";
		private string P_FilePrefix = "VBOSS";
		private string P_DTFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
		Common _common = new Common();

		private VBossConnector api = new VBossConnector(Environment: BaseConnector.GameEnvironment.PROD);

		public VBossController(GameEnvironment Environment = GameEnvironment.PROD)
		{
			api = new VBossConnector(Environment: Environment);
		}

		public VBossController()
		{
			api = new VBossConnector(Environment: GameEnvironment.PROD);
		}

		public bool UpdJobTime(string endTime)
		{
			try
			{
				using (var bingoConn = new BingoConn())
				{
					var xyz = bingoConn.ADMJobDates.Where(s => s.GameProvider == P_GamePrefix).FirstOrDefault();
					if (xyz != null)
					{
						xyz.LastRunTillDate = Convert.ToDateTime(endTime);
						bingoConn.SaveChanges();
					}
				}
				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("VBoss/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
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
					var query = bingoConn.ADMJobDates.Where(s => s.GameProvider == P_GamePrefix).FirstOrDefault();
					if (action == "Start")
					{
						//DateTime dtSTime = Convert.ToDateTime(query.LastRunTillDate?.ToString("yyyy-MM-dd HH:mm")) - new TimeSpan(0, 0, 15, 0, 0);
						DateTime dtSTime = Convert.ToDateTime(query.LastRunTillDate?.ToString("yyyy-MM-dd HH:mm"));
						sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					}
					else
					{
						//DateTime dtSTime = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm")) - new TimeSpan(0, 8, 0, 0, 0);
						DateTime dtSTime = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
						sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					}
				}

				return sTime;
			}
			catch (Exception ex)
			{
				_common.LogError("VBoss/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
				return "Error:" + ex.Message.ToString();
			}
		}

		//logic app will call this
		//http://localhost:51148/VBoss/VBossJob
		public void VBossJob()
		{
			try
			{
				lock (MyLock)
				{
					api = new VBossConnector(Environment: GameEnvironment.PROD);
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
			string mStartTime = startTime;
			string mEndTime = endTime;
			int limit = 1440; // 24 hours
			int count = 0;
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
						mEndTime = Convert.ToDateTime(mStartTime).AddMinutes(limit).ToString("yyyy-MM-dd HH:mm:ss");

					using (BingoConn db = new BingoConn())
					{
						//var players = db.VBossEntries.Where(x => !x.FetchedAll).ToList();
						var players = db.VBossEntry.ToList();

						foreach (var player in players)
						{
							var result = api.GetBetHistory(player.PlayerId,
								Convert.ToDateTime(mStartTime).ToString("yyyy-MM-dd"),
								Convert.ToDateTime(mEndTime).AddMonths(1).ToString("yyyy-MM-dd"), 0);
							if (result != null)
							{
								foreach (var r in result)
								{
									string s = "insert into VBossGame (content,currency,dateCreated,id,pageNo,receiptContent,receiptNo,totalBet,username,voided,CreatedDate) ";
									s += "select * from (values (" + _common.F1(r.content) + "," + _common.F1(r.currency) + "," + _common.F1(r.dateCreated) + "," + r.id + "," + r.pageNo + "," + _common.F1(r.receiptContent) + "," + _common.F1(r.receiptNo) + "," + r.totalBet + "," + _common.F1(r.username) + "," + (r.voided ? "1" : "0") + ",getdate())) as a(content,currency,dateCreated,id,pageNo,receiptContent,receiptNo,totalBet,username,voided,CreatedDate) ";
									s += "where not exists (select * from VBossGame b with (updlock) where b.id=a.id);";
									_common.Sql_RunAndCommit(s);
								}
							}

							//var result2 = api.GetWinningNumber(player.PlayerId, Convert.ToDateTime(mStartTime).ToString("yyyy-MM-dd"), Convert.ToDateTime(mEndTime).AddMonths(1).ToString("yyyy-MM-dd"));
							var result2 = api.GetWinningNumber(player.PlayerId, Convert.ToDateTime(mStartTime).AddDays(-2).ToString("yyyy-MM-dd"), Convert.ToDateTime(mEndTime).AddMonths(1).ToString("yyyy-MM-dd"));
							if (result2 != null && result2.winningDetails != null && result2.winningDetails.Count > 0)
							{
								foreach (var r in result2.winningDetails)
								{
									string s = "insert into VBossWinningNumber (betBig,betNumber,betNumberId,betOrderId,currency,currencyRate,drawDate,drawType,packageName,pageNo,prize,prizeBig,refId,strikeBig,CreatedDate) ";
									s += "select * from (values (" + r.betBig + "," + _common.F1(r.betNumber) + "," + r.betNumberId + "," + r.betOrderId + "," + _common.F1(r.currency) + ","
										+ r.currencyRate + "," + _common.F1(r.drawDate.ToString("yyyy-MM-dd")) + "," + _common.F1(r.drawType) + "," + _common.F1(r.packageName) + ","
										+ r.pageNo + "," + r.prize + "," + r.prizeBig + "," + _common.F1(r.refId) + "," + r.strikeBig
										+ ",getdate())) as a(betBig,betNumber,betNumberId,betOrderId,currency,currencyRate,drawDate,drawType,packageName,pageNo,prize,prizeBig,refId,strikeBig,CreatedDate) ";
									s += "where not exists (select * from VBossWinningNumber b with (updlock) where b.betNumberId=a.betNumberId and b.betOrderId=a.betOrderId);";
									_common.Sql_RunAndCommit(s);
								}
							}

							if (player.QuitDt != null && Convert.ToDateTime(mEndTime) > player.QuitDt)
								player.FetchedAll = true;
						}
						db.SaveChanges();

						bool isUpdJob = UpdJobTime(mEndTime);
					}
				}

				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("VBoss/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), P_FilePrefix);
				return false;
			}
		}
	}
}