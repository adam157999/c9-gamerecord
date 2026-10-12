using GameRecord.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;

namespace GameRecord.Controllers
{
    public class PTV2JobController : Controller
    {
        public static readonly object MyLock = new object();
        private string P_GamePrefix = "PTV2";
        Common _common = new Common();
        private PTV2SlotConnector api = new PTV2SlotConnector(Environment: BaseConnector.GameEnvironment.PROD);

        //###################################################################################

        public bool UpdJobTime(string startTime, string endTime, string sMainID)
        {
            try
            {
                DateTime dtEnd = Convert.ToDateTime(endTime);

                _common.UpdateADMJobDate(P_GamePrefix, dtEnd, "");

                return true;
            }
            catch (Exception ex)
            {
                _common.LogError($"{P_GamePrefix}/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_GamePrefix);
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
                        DateTime dtSTime = Convert.ToDateTime(query.LastRunTillDate?.ToString("yyyy-MM-dd HH:mm")) - new TimeSpan(0, 0, 15, 0, 0);
                        sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                    else
                    {
                        DateTime dtSTime = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm")) - new TimeSpan(0, 0, 0, 0, 0);
                        sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                }

                return sTime;
            }
            catch (Exception ex)
            {
                _common.LogError($"{P_GamePrefix}/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_GamePrefix);
                return "Error:" + ex.Message.ToString();
            }
        }

        public bool GetGameRecord()
        {
            string startTime = GetJobTime("Start");
            string endTime = GetJobTime("End");
            int currentPage = 1;
            int pageSize = 5000;
            string mStartTime = startTime;
            string mEndTime = endTime;
            int limit = 30;

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
                    {
                        mEndTime = Convert.ToString(Convert.ToDateTime(mStartTime).AddMinutes(limit));
                    }

                    bool HasNextPage = true;
                    currentPage = 1;
                    string sMainID = Guid.NewGuid().ToString();
                    while (HasNextPage)
                    {
                        int errorCode = 0;
                        string errorMessage = "";
                        PlaytechV2.Models.GameTransactionsResponse resp = api.GameTransactions(Convert.ToDateTime(mStartTime), Convert.ToDateTime(mEndTime), ref errorCode, ref errorMessage, currentPage, pageSize);

                        if (resp != null && resp.result != null && resp.result?.Count > 0)
                        {
                            using (BingoConn db = new BingoConn())
                            {
                                List<SqlParameter> parameters = new List<SqlParameter>();
                                parameters.Add(new SqlParameter("@Id", sMainID));
                                parameters.Add(new SqlParameter("@StartTime", startTime));
                                parameters.Add(new SqlParameter("@EndTime", endTime));
                                parameters.Add(new SqlParameter("@JsonData", JsonConvert.SerializeObject(resp.result)));
                                db.Database.ExecuteSqlCommand("EXEC sp_PTV2Game_IUD @Id, @StartTime, @EndTime, @JsonData", parameters.ToArray());

                                if (resp.pagination != null)
                                {
                                    //record more than limit
                                    HasNextPage = resp.pagination.currentPage < resp.pagination.totalPages;
                                    currentPage++;
                                }
                            }
                        }
                        else
                        {
                            HasNextPage = false;
                        }
                    }

                    bool isUpdJob = UpdJobTime(mStartTime, mEndTime, sMainID);
                }

                return true;
            }
            catch (Exception ex)
            {
                _common.LogError($"{P_GamePrefix}/GetGameRecord" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_GamePrefix);
                return false;
            }
        }

        //http://localhost:51148/PTV2Job/PTV2Job
        public void PTV2Job()
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
                _common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), P_GamePrefix);
            }
        }
    }
}