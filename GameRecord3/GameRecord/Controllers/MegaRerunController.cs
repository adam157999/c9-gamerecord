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
using System.Data.Entity.Migrations;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using System.Configuration;
using System.Data.Entity.Validation;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using Newtonsoft.Json.Linq;
using System.Dynamic;
using OpenQA.Selenium.Firefox;
using System.Diagnostics;
using GameRecord.Models;

namespace GameRecord.Controllers
{
	public class MegaRerunController : Controller
	{
		public static readonly object MyLock = new object();
		//private string P_ApiUrl = "http://mgt3.36ozhushou.com/mega-cloud/api/";
		//private string P_SecretKey = "KOI9iMnTl3H72Nr1hqfza6JyC1o=";
		//private string P_AgentId = "Mega1-1303";
		//private string P_AgentPwd = "0524Bt0524";
		//private string P_sn = "ld00";
		private string P_GamePrefix = "MEGARERUN";
		private string P_FilePrefix = "MEGARERUN";
		private int P_TimeoutSec = 120000; //2 mins
		Common _common = new Common();
		bool isNextPage = false;
		bool isError = false;
		int page = 1;
		private MEGAConnector api = new MEGAConnector(Environment: BaseConnector.GameEnvironment.PROD);

		public bool UpdJobTime(string startTime, string endTime, string sMainID)
		{
			try
			{
				DateTime dtEnd = Convert.ToDateTime(endTime);

				_common.UpdateADMJobDate(P_GamePrefix, dtEnd, "");

				//using (var bingoConn = new BingoConn())
				//{
				//	var xyz = bingoConn.ADMJobDates.Where(s => s.GameProvider == P_GamePrefix).FirstOrDefault();
				//	if (xyz != null)
				//	{
				//		xyz.LastRunTillDate = dtEnd;

				//		//bingoConn.Entry(xyz).State = System.Data.Entity.EntityState.Modified;
				//		bingoConn.SaveChanges();
				//	}
				//}
				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("Mega/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
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
						DateTime dtSTime = Convert.ToDateTime(query.LastRunTillDate?.ToString("yyyy-MM-dd HH:mm"));
						sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					}
					else
					{
						DateTime dtSTime = Convert.ToDateTime(DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd HH:mm"));
						sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					}
				}

				return sTime;

			}
			catch (Exception ex)
			{
				_common.LogError("Mega/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
				return "Error:" + ex.Message.ToString();
			}
		}

		public bool GetPlayerList()
		{
			string startTime = GetJobTime("Start");
			string endTime = GetJobTime("End");
			string startDate = startTime;
			string endDate = endTime;
			int limit = 30;
			string[] job = { "MEGA1", "MEGA2", "MEGA3", "MEGA4", "MEGA5" };
			int idx = 0;

			startDate = Convert.ToDateTime(startDate).ToString("yyyy-MM-dd") + " 00:00:00";
			endDate = Convert.ToDateTime(startDate).ToString("yyyy-MM-dd") + " 23:59:59";

			try
			{
				MEGA.PlayerTotalReportResponse result = api.PlayerTotalReport("1", DateTime.Parse(startDate), DateTime.Parse(endDate));

				using (BingoConn db = new BingoConn())
				{
					if (result.error == null)
					{
						if (result.result != null)
						{
							foreach (var p in result.result)
							{
								if (idx == 5)
									idx = 0;

								MEGAPlayerRerun rec = new MEGAPlayerRerun()
								{
									PlayerID = Convert.ToString(p.loginId),
									Finish = false,
									IsRunning = false,
									Job = job[idx],
									StartDt = Convert.ToDateTime(startDate),
									EndDt = Convert.ToDateTime(endDate)
								};
								db.MEGAPlayerReruns.Add(rec);
								db.SaveChanges();

								idx++;
							}
						}
					}
				}

				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("Mega/GetPlayerList" + " " + DateTime.Now.ToString() + "  " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
				isError = true;
				return false;
			}
		}

		public void MegaJob()
		{
			try
			{
				lock (MyLock)
				{
					bool isOK = GetPlayerList();
				}
			}
			catch (Exception ex)
			{
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
			}
		}

	}
}