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
using Newtonsoft.Json.Linq;
using System.Data.Entity.Migrations;

namespace GameRecord.Controllers
{
	public class SexyOldController : Controller
	{
		//staging
		//public static readonly object MyLock = new object();
		//private string P_GamePrefix = "SexyGaming";
		//private string P_ApiUrl = "https://api.mm-api.com/";
		//private string P_AgentName = "MCM";
		//private string P_GameRecordUser = "ERICTEST";
		//private string P_ApiKey = "f1951a49b49c0e5f561d01b9a6cf565d0496dbb9fb5501c8554541894b7a2ca3";
		//private int P_PageSize = 1000;
		//private int P_PageIndex = 1;
		//private int P_TimeoutSec = 60000;
		//private bool IsNextPage = false;

		//production
		public static readonly object MyLock = new object();
		private string P_GamePrefix = "SexyGaming";
		private string P_ApiUrl = "https://mmapi.12connect.net/";
		private string P_AgentName = "MCM";
		private string P_GameRecordUser = "ERICTEST";
		private string P_ApiKey = "821814a6d7c534055a93819f5999f527707d705a3d8a95bb1d9f51b68558c702";
		private int P_PageSize = 1000;
		private int P_PageIndex = 1;
		private int P_TimeoutSec = 60000;
		private bool IsNextPage = false;
		Common _common = new Common();



		//###################################################################################
		#region Main Function 

		public bool GetGameRecord()
		{
			string startTime = GetJobTime("Start");
			string endTime = GetJobTime("End");

			try
			{
				string sMainID = Guid.NewGuid().ToString();
				string method = "data/betlistv2";
				string webAddr = P_ApiUrl + method;

				var httpWebRequest = (HttpWebRequest)WebRequest.Create(webAddr);
				httpWebRequest.ContentType = "application/json";
				httpWebRequest.Method = "POST";
				httpWebRequest.Timeout = P_TimeoutSec;
				httpWebRequest.ReadWriteTimeout = P_TimeoutSec;
				httpWebRequest.Headers.Add("Authorization", "Bearer " + GetToken());

				string requestStr = "";
				using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
				{
					dynamic request = new ExpandoObject();
					request.fromDate = startTime;
					request.toDate = endTime;
					request.pageIndex = 1;
					request.pageSize = P_PageSize;

					requestStr = JsonConvert.SerializeObject(request);

					streamWriter.Write(requestStr);
					streamWriter.Flush();
				}

				var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
				using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
				{
					var responseText = streamReader.ReadToEnd();
					dynamic result = JObject.Parse(responseText);

					using (BingoConn db = new BingoConn())
					{
						SexyGameMain sexyGameMain = new SexyGameMain()
						{
							Id = sMainID,
							Total = result.error == 0 && result.data != null ? result.data.Count : 0,
							Code = result.error != null ? Convert.ToInt16(result.error) : null,
							Msg = result.message != null ? Convert.ToString(result.message) : null,
							StartTime = Convert.ToDateTime(startTime),
							EndTime = Convert.ToDateTime(endTime),
							CreatedDate = DateTime.Now
						};
						db.SexyGameMains.Add(sexyGameMain);
						db.SaveChanges();

						if (result.error == 0)
						{
							if (result.data.Count > 0)
							{
								foreach (var obj in result.data)
								{
									string transGuid = Convert.ToString(obj.TransGuid);

									try
									{
										SexyGame sexyGame = new SexyGame()
										{
											fId = sMainID,
											TransGuid = obj.TransGuid,
											UserName = obj.UserName,
											ActualStake = obj.ActualStake != null ? Convert.ToDecimal(obj.ActualStake) : null,
											TurnOver = obj.TurnOver != null ? Convert.ToDecimal(obj.TurnOver) : null,
											WinLost = obj.WinLost != null ? Convert.ToDecimal(obj.WinLost) : null,
											Profit = obj.Profit != null ? Convert.ToDecimal(obj.Profit) : null,
											StartAmount = obj.StartAmount != null ? Convert.ToDecimal(obj.StartAmount) : null,
											BetDate = obj.BetDate != null ? Convert.ToDateTime(obj.BetDate) : null,
											GameType = obj.GameType,
											GameId = obj.GameId,
											ProductType = obj.ProductType,
											TableName = obj.TableName,
											Status = obj.Status,
											ProviderStatus = obj.ProviderStatus,
											ProviderId = obj.ProviderId,
											Provider = obj.Provider,
											Result = obj.Result,
											PlayTypeName = obj.PlayTypeName,
											BetChoiceName = obj.BetChoiceName
										};

										var record = db.SexyGames.Where(x => x.TransGuid == transGuid).FirstOrDefault();

										if (record == null)
										{
											sexyGame.CreatedDate = DateTime.Now;
										}
										else
										{
											sexyGame.CreatedDate = record.CreatedDate;
											sexyGame.ModifiedDate = DateTime.Now;
										}
										db.SexyGames.AddOrUpdate(sexyGame);
									}
									catch (Exception ex)
									{
										_common.LogError("Record " + transGuid + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), "Sexy");
									}
								}
								db.SaveChanges();

								if (Convert.ToInt16(result.data.Count) < P_PageSize)
									IsNextPage = false;
								else
									IsNextPage = true;

								while (IsNextPage == true)
								{
									P_PageIndex += 1;
									GetGameRecordExtend(startTime, endTime, sMainID);
								}
							}
						}

						if (result.error == 0 && !IsNextPage)
						{
							bool isUpdJob = UpdJobTime(startTime, endTime, sMainID);
						}
					}
				}


				return true;
			}
			catch (Exception ex)
			{
				_common.LogError("Sexy/GetGameRecord" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "Sexy");
				return false;
			}
		}

		public bool GetGameRecordExtend(string startTime, string endTime, string sMainID)
		{
			string method = "data/betlistv2";
			string webAddr = P_ApiUrl + method;

			var httpWebRequest = (HttpWebRequest)WebRequest.Create(webAddr);
			httpWebRequest.ContentType = "application/json";
			httpWebRequest.Method = "POST";
			httpWebRequest.Timeout = P_TimeoutSec;
			httpWebRequest.ReadWriteTimeout = P_TimeoutSec;
			httpWebRequest.Headers.Add("Authorization", "Bearer " + GetToken());

			string requestStr = "";
			using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
			{
				dynamic request = new ExpandoObject();
				request.fromDate = startTime;
				request.toDate = endTime;
				request.pageIndex = P_PageIndex;
				request.pageSize = P_PageSize;

				requestStr = JsonConvert.SerializeObject(request);

				streamWriter.Write(requestStr);
				streamWriter.Flush();
			}

			var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
			using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
			{
				var responseText = streamReader.ReadToEnd();
				dynamic result = JObject.Parse(responseText);

				using (BingoConn db = new BingoConn())
				{
					if (result.error == 0)
					{
						if (result.data.Count > 0)
						{
							foreach (var obj in result.data)
							{
								string transGuid = Convert.ToString(obj.TransGuid);

								try
								{
									SexyGame sexyGame = new SexyGame()
									{
										fId = sMainID,
										TransGuid = obj.TransGuid,
										UserName = obj.UserName,
										ActualStake = obj.ActualStake != null ? Convert.ToDecimal(obj.ActualStake) : null,
										TurnOver = obj.TurnOver != null ? Convert.ToDecimal(obj.TurnOver) : null,
										WinLost = obj.WinLost != null ? Convert.ToDecimal(obj.WinLost) : null,
										Profit = obj.Profit != null ? Convert.ToDecimal(obj.Profit) : null,
										StartAmount = obj.StartAmount != null ? Convert.ToDecimal(obj.StartAmount) : null,
										BetDate = obj.BetDate != null ? Convert.ToDateTime(obj.BetDate) : null,
										GameType = obj.GameType,
										GameId = obj.GameId,
										ProductType = obj.ProductType,
										TableName = obj.TableName,
										Status = obj.Status,
										ProviderStatus = obj.ProviderStatus,
										ProviderId = obj.ProviderId,
										Provider = obj.Provider,
										Result = obj.Result,
										PlayTypeName = obj.PlayTypeName,
										BetChoiceName = obj.BetChoiceName
									};

									var record = db.SexyGames.Where(x => x.TransGuid == transGuid).FirstOrDefault();

									if (record == null)
									{
										sexyGame.CreatedDate = DateTime.Now;
									}
									else
									{
										sexyGame.CreatedDate = record.CreatedDate;
										sexyGame.ModifiedDate = DateTime.Now;
									}
									db.SexyGames.AddOrUpdate(sexyGame);
								}
								catch (Exception ex)
								{
									_common.LogError("Record " + transGuid + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), "Sexy");
								}
							}
							db.SaveChanges();
						}

						if (Convert.ToInt16(result.data.Count) < P_PageSize)
							IsNextPage = false;
						else
							IsNextPage = true;
					}
				}
			}

			return true;
		}

		public bool UpdJobTime(string startTime, string endTime, string sMainID)
		{
			try
			{
				DateTime dtEnd = Convert.ToDateTime(endTime);

				using (var bingoConn = new BingoConn())
				{
					var xyz = bingoConn.ADMJobDates.Where(s => s.GameProvider == P_GamePrefix).FirstOrDefault();
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
				_common.LogError("Sexy/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "Sexy");
				return false;
			}
		}

		public string GetToken()
		{
			lock (MyLock)
			{
				try
				{
					string method = "account/login";
					string webAddr = P_ApiUrl + method;
					var httpWebRequest = (HttpWebRequest)WebRequest.Create(webAddr);
					httpWebRequest.ContentType = "application/json";
					httpWebRequest.Method = "POST";
					httpWebRequest.Timeout = P_TimeoutSec;
					httpWebRequest.ReadWriteTimeout = P_TimeoutSec;

					string requestStr = "";
					using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
					{
						dynamic request = new ExpandoObject();
						request.ApiKey = P_ApiKey;
						request.UserName = P_GameRecordUser;
						request.AgentName = P_AgentName;

						requestStr = JsonConvert.SerializeObject(request);

						streamWriter.Write(requestStr);
						streamWriter.Flush();
					}

					var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
					using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
					{
						var responseText = streamReader.ReadToEnd();

						dynamic result = JObject.Parse(responseText);

						if (Convert.ToInt16(result.error) == 0)
						{
							return Convert.ToString(result.data);
						}
					}

					return "";
				}
				catch (Exception ex)
				{
					_common.LogError("Sexy/GetToken" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", P_GameRecordUser) + " " + ex.Message.ToString(), "Sexy");
					return "";
				}
			}
		}

		#endregion

		//###################################################################################
		#region Function 

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
						DateTime dtSTime = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm")) - new TimeSpan(0, 8, 3, 0, 0);
						sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					}
				}

				return sTime;

			}
			catch (Exception ex)
			{
				_common.LogError("Sexy/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "Sexy");
				return "Error:" + ex.Message.ToString();
			}
		}

		#endregion

		// GET: Sexy
		public void SexyJob()
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
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "Sexy");
			}
		}
	}
}