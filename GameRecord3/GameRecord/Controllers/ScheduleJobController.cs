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
using System.Data.SqlClient;
using System.Data;
using System.Configuration;

namespace GameRecord.Controllers
{
	public class ScheduleJobController : Controller
	{
		private Common _common = new Common();
		private string P_FilePrefix = "ScheduleJob";
		public string BingoConn = System.Configuration.ConfigurationManager.ConnectionStrings["BingoConn1"].ToString();

		public void GameRecordSummaryJob()
		{
			try
			{
				using (SqlConnection con = new SqlConnection(BingoConn))
				{
					con.Open();
					SqlCommand cmd = new SqlCommand("sp_PlayerGameRecord_Upsert", con);
					cmd.CommandTimeout = 0;
					cmd.CommandType = CommandType.StoredProcedure;

					cmd.ExecuteNonQuery();

				}
			}
			catch (Exception ex)
			{
				_common.LogError("ScheduleJob/GameRecordSummaryJob" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
			}
		}

		public void PlayerBonusRolloverJob()
		{
			try
			{
				using (SqlConnection con = new SqlConnection(BingoConn))
				{
					con.Open();
					SqlCommand cmd = new SqlCommand("sp_PlayerBonusRolloverJob", con);
					cmd.CommandTimeout = 0;
					cmd.CommandType = CommandType.StoredProcedure;

					cmd.ExecuteNonQuery();

				}
			}
			catch (Exception ex)
			{
				_common.LogError("ScheduleJob/PlayerBonusRolloverJob" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_FilePrefix);
			}
		}
	}
}