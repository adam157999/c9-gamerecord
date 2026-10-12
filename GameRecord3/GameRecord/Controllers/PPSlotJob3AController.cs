using System;
using System.Linq;
using System.Web.Mvc;
using System.Net;
using System.IO;
using GameRecord.Models;
using System.Data.Entity.Migrations;
using System.Data.Entity.Validation;
using GameRecord.Controllers.V2;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Web.Script.Serialization;
using System.Dynamic;
using Pragmatic.Model;
using BG.Model;
using System.Data.SqlClient;

namespace GameRecord.Controllers
{
	public class PPSlotJob3AController : Controller
	{
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "PPSLOT3A";
		private string P_FilePrefix = "PPSLOT3A";
		private string P_DTFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
		private string gameImgUrl = string.Empty;
		Common _common = new Common();
		private PPSlotConnector api = new PPSlotConnector(Environment: BaseConnector.GameEnvironment.PROD);


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
				_common.LogError("PPSlot/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
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
				_common.LogError("PPSlot/GetLastVersionKey" + " " + DateTime.Now.ToString() + "  " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
				return "Error:" + ex.InnerException.InnerException.ToString();
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
				_common.LogError("PPSlot/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
				return "Error:" + ex.InnerException.InnerException.ToString();
			}
		}

		public void PPSlotJob()
		{
			try
			{
				lock (MyLock)
				{
					string dataType = "RNG";
					bool isOK = GetGameRecord(dataType);
                }
			}
			catch (Exception ex)
			{
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
			}
		}

        public bool GetGameRecord(string dataType)
        {
            string startTime = GetJobTime("Start");
            string endTime = GetJobTime("End");
            string timeSpan = GetLastVersionKey();
            string sMainID = Guid.NewGuid().ToString();

            try
            {
                var result = api.RetrieveTransactionByTime(timeSpan, dataType);

                using (BingoConn db = new BingoConn())
                {
					List<SqlParameter> parameters = new List<SqlParameter>();
                    parameters.Add(new SqlParameter("@Id", sMainID));
                    parameters.Add(new SqlParameter("@StartTime", startTime));
					parameters.Add(new SqlParameter("@EndTime", endTime));
					parameters.Add(new SqlParameter("@JsonData", JsonConvert.SerializeObject(result.rounds)));
                    db.Database.ExecuteSqlCommand("EXEC sp_PPSlotGame_IUD @Id, @StartTime, @EndTime, @JsonData", parameters.ToArray());

                    timeSpan = Convert.ToString(result.timepoint);
                    bool isUpdJob = UpdJobTime(startTime, endTime, sMainID, timeSpan);
                }

                return true;
            }
            catch (DbEntityValidationException e)
            {
                foreach (var eve in e.EntityValidationErrors)
                {
                    _common.LogError("PPSlot/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                                                    eve.Entry.Entity.GetType().Name, eve.Entry.State), P_FilePrefix);
                    foreach (var ve in eve.ValidationErrors)
                    {
                        _common.LogError("PPSlot/GetGameRecord" + " " + DateTime.Now.ToString() + " " + string.Format("- Property: \"{0}\", Error: \"{1}\"",
                                                        ve.PropertyName, ve.ErrorMessage), P_FilePrefix);
                    }
                }
                return false;
            }
            catch (WebException ex)
            {
                var responseText = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
                _common.LogError("PPSlot/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString() + " " + responseText, P_FilePrefix);
                return false;
            }
            catch (Exception ex)
            {
                _common.LogError("PPSlot/GetGameRecord" + " " + DateTime.Now.ToString() + " " + ex.InnerException.InnerException.ToString(), P_FilePrefix);
                return false;
            }
        }
    }
}