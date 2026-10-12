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
using System.Data.Entity.Validation;

namespace GameRecord.Controllers
{
    public class BT4DController : Controller
    {
        private string P_ApiUrl = "http://103.106.250.143/4DApi/api/BingoTown/Get4DBetTransaction";
        private string P_GamePrefix = "BT4D";

        //###################################################################################
        #region Declare 

        public class Data
        {
            public DateTime OrderDateTime { get; set; }
            public string ReceiptNo { get; set; }
            public DateTime DrawDate { get; set; }
            public string UserName { get; set; }
            public string Status { get; set; }
            public float Amount { get; set; }
            public float WinLose { get; set; }
        }

        #endregion

        //###################################################################################
        #region Main Function 

        public bool GetGameRecord()
        {
            //satrtTime = "MM/DD/YYYY HH:MM:SS"
            string startTime = GetJobTime("Start");
            string endTime = GetJobTime("End");
            string sMainID = Guid.NewGuid().ToString();
            string sTicketNo = "";

            try
            {

                var httpWebRequest = (HttpWebRequest)WebRequest.Create(P_ApiUrl);
                httpWebRequest.ContentType = "application/xml";
                httpWebRequest.Method = "POST";
                httpWebRequest.Timeout = 60000;
                httpWebRequest.ReadWriteTimeout = 60000;

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    string xml = "<Request>\r\n  " +
                                 "  <ReceiptNo>" + sTicketNo + "</ReceiptNo>\r\n    " +
                                 "  <StartDate>" + startTime + "</StartDate>\r\n    " +
                                 "  <EndDate>" + endTime + "</EndDate>\r\n  " +
                                 "</Request>";


                    streamWriter.Write(xml);
                    streamWriter.Flush();
                }
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var responseText = streamReader.ReadToEnd();

                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(responseText);

                    string rErrorCode = xmlDoc.SelectSingleNode("Response/Code").InnerText;
                    string rErrorDesc = xmlDoc.SelectSingleNode("Response/Message").InnerText;

                    if(rErrorCode == "1")
                    {
                        XmlNodeList xnList = xmlDoc.SelectNodes("/Response/Data/BetTransaction");

                        using (BingoConn db = new BingoConn())
                        {
                            //Insert Main table
                            BT4GGameMain bT4GGameMain = new BT4GGameMain()
                            {
                                Id = sMainID,
                                startTime = Convert.ToDateTime(startTime),
                                endTime = Convert.ToDateTime(endTime),
                                CreatedDate = DateTime.Now
                            };
                            db.BT4GGameMain.Add(bT4GGameMain);
                            db.SaveChanges();

                            //Insert Result table
                            foreach (XmlNode xn in xnList)
                            {
                                string uuid = Guid.NewGuid().ToString();

                                BT4GGame bT4GGame = new BT4GGame()
                                {
                                    Id = uuid,
                                    fId = sMainID,
                                    OrderDateTime = Convert.ToDateTime(xn["OrderDateTime"].InnerText),
                                    DrawDate = Convert.ToDateTime(xn["DrawDate"].InnerText),
                                    ReceiptNo = xn["ReceiptNo"].InnerText,
                                    UserName = xn["UserName"].InnerText,
                                    Amount = Convert.ToDouble(xn["Amount"].InnerText),
                                    WinLose = Convert.ToDouble(xn["WinLose"].InnerText),
                                    Status = xn["Status"].InnerText
                                };
                                db.BT4GGame.Add(bT4GGame);
                                db.SaveChanges();
                            }
                        }

                        bool isUpdJob = UpdJobTime(startTime, endTime, sMainID);
                    }
                    else
                    {
                        using (BingoConn dbConn = new BingoConn())
                        {
                            BT4GGameMain bT4GGameMain = new BT4GGameMain ()
                            {
                                Id = sMainID,
                                error = rErrorDesc,
                                startTime = Convert.ToDateTime(startTime),
                                endTime = Convert.ToDateTime(endTime),
                                CreatedDate = DateTime.Now
                            };
                            dbConn.BT4GGameMain.Add(bT4GGameMain);
                            dbConn.SaveChanges();
                        }
                    }

                    return true;
                }
            }
            catch (DbEntityValidationException e)
            {
                foreach (var eve in e.EntityValidationErrors)
                {
                    Console.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                        eve.Entry.Entity.GetType().Name, eve.Entry.State);
                    foreach (var ve in eve.ValidationErrors)
                    {
                        Console.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
                            ve.PropertyName, ve.ErrorMessage);
                    }
                }
                throw;
            }
            catch (Exception ex)
            {
                using (BingoConn dbConn = new BingoConn())
                {
                    string errID = Guid.NewGuid().ToString();

                    BT4GGameMain bT4GGameMain = new BT4GGameMain()
                    {
                        Id = errID,
                        error = "BT4D GetGameRecord Error: " + ex.Message,
                        //startTime = Convert.ToDateTime(startTime),
                        //endTime = Convert.ToDateTime(endTime),
                        CreatedDate = DateTime.Now
                    };
                    dbConn.BT4GGameMain.Add(bT4GGameMain);
                    dbConn.SaveChanges();
                }
                return false;
            }
        }

        public bool UpdGameRecord(string sTicketNo)
        {
            //satrtTime = "MM/DD/YYYY HH:MM:SS"
            string startTime = "";
            string endTime = "";
            string sMainID = Guid.NewGuid().ToString();

            try
            {

                var httpWebRequest = (HttpWebRequest)WebRequest.Create(P_ApiUrl);
                httpWebRequest.ContentType = "application/xml";
                httpWebRequest.Method = "POST";
                httpWebRequest.Timeout = 60000;
                httpWebRequest.ReadWriteTimeout = 60000;

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    string xml = "<Request>\r\n  " +
                                 "  <ReceiptNo>" + sTicketNo + "</ReceiptNo>\r\n    " +
                                 "  <StartDate>" + startTime + "</StartDate>\r\n    " +
                                 "  <EndDate>" + endTime + "</EndDate>\r\n  " +
                                 "</Request>";


                    streamWriter.Write(xml);
                    streamWriter.Flush();
                }
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var responseText = streamReader.ReadToEnd();

                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(responseText);

                    string rErrorCode = xmlDoc.SelectSingleNode("Response/Code").InnerText;
                    string rErrorDesc = xmlDoc.SelectSingleNode("Response/Message").InnerText;

                    if (rErrorCode == "1")
                    {
                        XmlNodeList xnList = xmlDoc.SelectNodes("/Response/Data/BetTransaction");

                        using (BingoConn db = new BingoConn())
                        {
                            foreach (XmlNode xn in xnList)
                            {
                                string sReceiptNo = xn["ReceiptNo"].InnerText;
                                var dtxn = db.BT4GGame.Where(x => x.ReceiptNo == sReceiptNo).FirstOrDefault();
                                if(dtxn != null)
                                {
                                    dtxn.Status = xn["Status"].InnerText;
                                    dtxn.WinLose = Convert.ToDouble(xn["WinLose"].InnerText);

                                    db.Entry(dtxn).State = System.Data.Entity.EntityState.Modified;
                                    db.SaveChanges();
                                }
                            }
                        }
                    }
                    else
                    {
                        using (BingoConn dbConn = new BingoConn())
                        {
                            BT4GGameMain bT4GGameMain = new BT4GGameMain()
                            {
                                Id = sMainID,
                                error = rErrorDesc,
                                CreatedDate = DateTime.Now
                            };
                            dbConn.BT4GGameMain.Add(bT4GGameMain);
                            dbConn.SaveChanges();
                        }
                    }




                    return true;
                }
            }
            catch (Exception ex)
            {
                using (BingoConn dbConn = new BingoConn())
                {
                    string errID = Guid.NewGuid().ToString();

                    BT4GGameMain bT4GGameMain = new BT4GGameMain()
                    {
                        Id = errID,
                        error = "BT4D UpdGameRecord Error: " + ex.Message,
                        CreatedDate = DateTime.Now
                    };
                    dbConn.BT4GGameMain.Add(bT4GGameMain);
                    dbConn.SaveChanges();
                }
                return false;
            }
        }

        public bool UpdJobTime(string startTime, string endTime, string sMainID)
        {
            try
            {
                DateTime dtStart = Convert.ToDateTime(startTime);
                DateTime dtEnd = Convert.ToDateTime(endTime);

                using (var bingoConn = new BingoConn())
                {
                    var xyz = bingoConn.ADMJobDates.Where(s => s.GameProvider == P_GamePrefix).FirstOrDefault();
                    if (xyz != null)
                    {
                        int iInterval = Convert.ToInt32(xyz.Interval);

                        DateTime dtStartAdd = dtStart.AddMinutes(iInterval);
                        DateTime dtEndAdd = dtEnd.AddMinutes(iInterval);

                        xyz.JobStart = dtStartAdd;
                        xyz.JobEnd = dtEndAdd;

                        bingoConn.Entry(xyz).State = System.Data.Entity.EntityState.Modified;
                        bingoConn.SaveChanges();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                using (BingoConn dbConn = new BingoConn())
                {
                    string errID = Guid.NewGuid().ToString();

                    BT4GGameMain bT4GGameMain = new BT4GGameMain()
                    {
                        Id = errID,
                        error = "BT4D UpdJobTime Error: " + ex.Message,
                        startTime = Convert.ToDateTime(startTime),
                        endTime = Convert.ToDateTime(endTime),
                        CreatedDate = DateTime.Now
                    };
                    dbConn.BT4GGameMain.Add(bT4GGameMain);
                    dbConn.SaveChanges();
                }
                return false;
            }
        }

        public string GetJobTime(string action)
        {
            //Format "yyyy-MM-DD HH:MM:SS"
            try
            {
                string sTime = "";

                using (var bingoConn = new BingoConn())
                {
                    var query = bingoConn.ADMJobDates
                                        .Where(s => s.GameProvider == P_GamePrefix)
                                        .FirstOrDefault();

                    if (action == "Start")
                    {
                        DateTime dtSTime = Convert.ToDateTime(query.JobStart);
                        sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                    else
                    {
                        DateTime dtSTime = Convert.ToDateTime(query.JobEnd);
                        sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                }

                return sTime;

            }
            catch (Exception ex)
            {
                using (BingoConn dbConn = new BingoConn())
                {
                    string errID = Guid.NewGuid().ToString();

                    BT4GGameMain bT4GGameMain = new BT4GGameMain()
                    {
                        Id = errID,
                        error = "BT4D GetJobTime Error: " + ex.Message,
                        CreatedDate = DateTime.Now
                    };
                    dbConn.BT4GGameMain.Add(bT4GGameMain);
                    dbConn.SaveChanges();
                }
                return "Error:" + ex.Message.ToString();
            }
        }

        public bool GetPending()
        {
            try
            {
                using (var bingoConn = new BingoConn())
                {
                    var query = bingoConn.BT4GGame
                                        .Where(s => s.Status == "Pending")
                                        .ToList();

                    if (query != null)
                    {
                        string sID = Guid.NewGuid().ToString();

                        BT4GGameMain bT4GGameMain = new BT4GGameMain()
                        {
                            Id = sID,
                            error = "GetPending",
                            total = query.Count,
                            CreatedDate = DateTime.Now
                        };
                        bingoConn.BT4GGameMain.Add(bT4GGameMain);
                        bingoConn.SaveChanges();

                        foreach (var x in query)
                        {
                            bool isUpdOK = UpdGameRecord(x.ReceiptNo);
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                string sErr = "BT4D GetPending Error: " + ex.Message;

                using (BingoConn dbConn = new BingoConn())
                {
                    string sMainID = Guid.NewGuid().ToString();

                    //Insetrt Main Table
                    BT4GGameMain bT4GGameMain = new BT4GGameMain()
                    {
                        Id = sMainID,
                        error = sErr,
                        total = 0,
                        CreatedDate = DateTime.Now
                    };
                    dbConn.BT4GGameMain.Add(bT4GGameMain);
                    dbConn.SaveChanges();
                }

                return false;
            }
        }

        #endregion

        // GET: BT4D
        public void BT4DJob()
        {
            bool isOK = GetGameRecord();

            if (isOK == true)
            {
                bool IsOK = GetPending();
            }
        }
    }
}