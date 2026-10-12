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
using System.Data.Entity.Migrations;

namespace GameRecord.Controllers
{
	public class IGController : Controller
	{
		public static readonly object MyLock = new object();
		//Live need to have change API URL, create new agenet ID
		//private string P_ApiUrl = "http://www.igamingdemo.com:8002/";
		private string P_ApiUrl = "https://playboyboapi.goapi888.com/";
		private string P_CompKey = "j%eY4NsBcpNX@*si8X7a";
		//private string P_ServerId = "SVRBT01";
		private string P_AgentId = "btw"; //Live, need to create agent id first, then put the agent id here
																			//private string P_AgentPwd = "aw@WEQ$01";
		private string P_GamePrefix = "IG";
		Common _common = new Common();

		//###################################################################################
		#region Declare 

		public class CommonVM
		{
			public int Code { get; set; }
			public string Message { get; set; }
		}

		public class TransactionRecordVM
		{
			public int Code { get; set; }
			public string Message { get; set; }
			public TransactionRecordDataVM Data { get; set; }
		}

		public class TransactionRecordDataVM
		{
			public PageDataVM Page { get; set; }
			public List<TransactionDataVM> Record { get; set; }
		}

		public class TransactionDataVM
		{
			public string ExternalTransactionId { get; set; }
			public int OperationMethod { get; set; }
			public string PlayerAccount { get; set; }
			public double TransAmount { get; set; }
			public int Result { get; set; }
			public DateTime CreateTime { get; set; }
		}

		public class GameRecordVM
		{
			public int Code { get; set; }
			public string Message { get; set; }
			public GameRecordDataVM Data { get; set; }
		}

		public class GameRecordDataVM
		{
			public PageDataVM Page { get; set; }
			public List<RecordDataVM> Record { get; set; }
		}

		public class PageDataVM
		{
			public string PageNumber { get; set; }
			public string PageSize { get; set; }
			public string TotalSize { get; set; }
			public string TotalPage { get; set; }
		}

		public class RecordDataVM
		{
			public long ThemeID { get; set; }
			public long GameNo { get; set; }
			public long GameSerialID { get; set; }
			public long AccountID { get; set; }
			public string UserName { get; set; }
			public float ValueBefore { get; set; }
			public float ValueAfter { get; set; }
			public float BetCoin { get; set; }
			public float WinAmount { get; set; }
			public float AvailTotalBet { get; set; }
			public float AvailTotalWin { get; set; }
			public string RemoteIP { get; set; }
			public string DeviceType { get; set; }
			public string GameName { get; set; }
			public DateTime OrderTime { get; set; }
		}

		public class GameListVM
		{
			public int Code { get; set; }
			public string Message { get; set; }
			public List<GameListDataVM> Data { get; set; }
		}

		public class GameListDataVM
		{
			public int GameID { get; set; }
			public string GameName { get; set; }
			public int GameTypeID { get; set; }
			public bool HighRoller { get; set; }
			public string GameTypeName { get; set; }
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
					var query = bingoConn.ADMJobDates
															.Where(s => s.GameProvider == P_GamePrefix)
															.FirstOrDefault();

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

					//if (action == "Start")
					//{
					//	DateTime dtSTime = Convert.ToDateTime(query.LastRunTillDate);
					//	sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					//}
					//else
					//{
					//	DateTime dtSTime = DateTime.Now - new TimeSpan(0, 0, 3, 0);
					//	sTime = dtSTime.ToString("yyyy-MM-dd HH:mm:ss");
					//}
				}

				return sTime;

			}
			catch (Exception ex)
			{
				_common.LogError("IG/GetJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "IG");
				return "Error:" + ex.Message.ToString();
			}
		}



		#endregion

		//##########################################################################################################################
		#region API Funtion

		public bool GetGameRecord(string startTime, string endTime)
		{
			//string startTime = "2020-08-08 12:00:00.000";
			//string endTime = "2020-08-08 16:00:00.000";
			string sMainID = Guid.NewGuid().ToString();

			try
			{
				string method = "game/log/record";
				string webAddr = P_ApiUrl + method;
				//string txnid = P_GamePrefix + "GL" + Guid.NewGuid().ToString("N").Substring(0, 10);
				int p_page = 1;
				string P_Sign = "EndDate=" + endTime + "&PageNumber=" + p_page + "&StartDate=" + startTime + "&PrivateKey=" + P_CompKey;

				HashAlgorithm hashAlgorithm = new SHA256Managed();
				byte[] byteAry =
				hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(P_Sign));
				string stringEncryptResult = Convert.ToBase64String(byteAry);

				ServicePointManager.Expect100Continue = true;
				ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
				ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

				var httpWebRequest = (HttpWebRequest)WebRequest.Create(webAddr);
				httpWebRequest.ContentType = "application/json";
				httpWebRequest.Method = "POST";

				httpWebRequest.Headers.Add("merchantName", "BingoTown");
				httpWebRequest.Headers.Add("sign", stringEncryptResult);

				httpWebRequest.Timeout = 60000;
				httpWebRequest.ReadWriteTimeout = 60000;

				using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
				{
					string json = "{ \"PageNumber\" : " + p_page +
													", \"StartDate\" : \"" + startTime +
													"\", \"EndDate\" : \"" + endTime + "\"} ";


					streamWriter.Write(json);
					streamWriter.Flush();
				}
				var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
				using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
				{
					var responseText = streamReader.ReadToEnd();
					responseText = responseText.Replace("CreateTime", "OrderTime");
					responseText = responseText.Replace("ThemeID", "GameName");

					GameRecordVM result = JsonConvert.DeserializeObject<GameRecordVM>(responseText);

					GameListVM gameList = GetGameListName();

					using (BingoConn db = new BingoConn())
					{
						//Insert Main table
						IGGameMain iGGameMain = new IGGameMain()
						{
							Id = sMainID,
							Code = result.Code,
							Message = result.Message,
							PageNumber = Convert.ToInt32(result.Data.Page.TotalPage),
							PageSize = Convert.ToInt32(result.Data.Page.PageSize),
							TotalPage = Convert.ToInt32(result.Data.Page.TotalPage),
							TotalSize = Convert.ToInt32(result.Data.Page.TotalSize),
							startTime = Convert.ToDateTime(startTime),
							endTime = Convert.ToDateTime(endTime),
							CreatedDate = DateTime.Now
						};
						db.IGGameMains.Add(iGGameMain);
						db.SaveChanges();

						//Insert Result table
						foreach (var p in result.Data.Record)
						{
							try
							{
								var iggame = db.IGGames.Where(x => x.GameSerialNo == p.GameSerialID).FirstOrDefault();
								if (iggame == null)
								{
									string sGameName = "";

									foreach (var item in gameList.Data)
									{
										if (p.GameName.Substring(0, 4).ToString() == item.GameID.ToString())
										{
											sGameName = item.GameName.ToString();
										}
									}

									var iGGame = new IGGame()
									{
										fId = sMainID,
										ThemeID = Convert.ToInt64(p.GameName),
										GameNo = p.GameNo,
										GameSerialNo = p.GameSerialID,
										AccountID = p.AccountID,
										UserName = p.UserName,
										ValueBefore = p.ValueBefore,
										ValueAfter = p.ValueAfter,
										BetCoin = p.BetCoin,
										WinAmount = p.WinAmount,
										AvailTotalBet = p.AvailTotalBet,
										AvailTotalWin = p.AvailTotalWin,
										RemoteIP = p.RemoteIP,
										DeviceType = p.DeviceType,
										GameName = sGameName,
										OrderTime = p.OrderTime,
										Total = Convert.ToInt32(result.Data.Page.TotalSize),
										CreatedDate = DateTime.Now
									};
									db.IGGames.AddOrUpdate(iGGame);
								}
							}
							catch (Exception ex)
							{
								_common.LogError("Game Record " + p.GameSerialID + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "IG");
							}

						}
						db.SaveChanges();
					}

					//If record more than 1 page, need to get the record from Page 2 and above
					int iPageIndex = Int32.Parse(result.Data.Page.TotalPage);

					if (iPageIndex > 1)
					{
						for (int x = 2; x <= iPageIndex; x++)
						{
							bool IsOK = GetGameRecordExtend(x, startTime, endTime);
						}
					}

					GetTransactionRecord(startTime, endTime);

					bool isUpdJob = UpdJobTime(startTime, endTime, sMainID);

					return true;
				}
			}
			catch (WebException ex)
			{
				_common.LogError("IG/GetGameRecord WebExp" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "IG");
				return false;
			}
			catch (Exception ex)
			{
				_common.LogError("IG/GetGameRecord" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "IG");
				return false;
			}
		}

		public bool GetGameRecordExtend(int p_page, string startTime, string endTime)
		{
			//string startTime = GetJobTime("Start");
			//string endTime = GetJobTime("End");
			string sMainID = Guid.NewGuid().ToString();

			try
			{
				string method = "game/log/record";
				string webAddr = P_ApiUrl + method;
				string P_Sign = "EndDate=" + endTime + "&PageNumber=" + p_page + "&StartDate=" + startTime + "&PrivateKey=" + P_CompKey;

				HashAlgorithm hashAlgorithm = new SHA256Managed();
				byte[] byteAry =
				hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(P_Sign));
				string stringEncryptResult = Convert.ToBase64String(byteAry);

				ServicePointManager.Expect100Continue = true;
				ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
				ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

				var httpWebRequest = (HttpWebRequest)WebRequest.Create(webAddr);
				httpWebRequest.ContentType = "application/json";
				httpWebRequest.Method = "POST";

				httpWebRequest.Headers.Add("merchantName", "BingoTown");
				httpWebRequest.Headers.Add("sign", stringEncryptResult);

				httpWebRequest.Timeout = 60000;
				httpWebRequest.ReadWriteTimeout = 60000;

				using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
				{
					string json = "{ \"PageNumber\" : " + p_page +
													", \"StartDate\" : \"" + startTime +
													"\", \"EndDate\" : \"" + endTime + "\"} ";


					streamWriter.Write(json);
					streamWriter.Flush();
				}
				var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
				using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
				{
					var responseText = streamReader.ReadToEnd();
					responseText = responseText.Replace("CreateTime", "OrderTime");
					responseText = responseText.Replace("ThemeID", "GameName");

					GameRecordVM result = JsonConvert.DeserializeObject<GameRecordVM>(responseText);

					GameListVM gameList = GetGameListName();

					using (BingoConn db = new BingoConn())
					{
						//Insert Main table
						IGGameMain iGGameMain = new IGGameMain()
						{
							Id = sMainID,
							Code = result.Code,
							Message = result.Message,
							PageNumber = Convert.ToInt32(result.Data.Page.TotalPage),
							PageSize = Convert.ToInt32(result.Data.Page.PageSize),
							TotalPage = Convert.ToInt32(result.Data.Page.TotalPage),
							TotalSize = Convert.ToInt32(result.Data.Page.TotalSize),
							startTime = Convert.ToDateTime(startTime),
							endTime = Convert.ToDateTime(endTime),
							CreatedDate = DateTime.Now
						};
						db.IGGameMains.Add(iGGameMain);
						db.SaveChanges();

						//Insert Result table
						foreach (var p in result.Data.Record)
						{
							try
							{
								var iggame = db.IGGames.Where(x => x.GameSerialNo == p.GameSerialID).FirstOrDefault();
								if (iggame == null)
								{
									string sGameName = "";

									foreach (var item in gameList.Data)
									{
										if (p.GameName.Substring(0, 4).ToString() == item.GameID.ToString())
										{
											sGameName = item.GameName.ToString();
										}
									}

									IGGame iGGame = new IGGame()
									{
										fId = sMainID,
										ThemeID = Convert.ToInt64(p.GameName),
										GameNo = p.GameNo,
										GameSerialNo = p.GameSerialID,
										AccountID = p.AccountID,
										UserName = p.UserName,
										ValueBefore = p.ValueBefore,
										ValueAfter = p.ValueAfter,
										BetCoin = p.BetCoin,
										WinAmount = p.WinAmount,
										AvailTotalBet = p.AvailTotalBet,
										AvailTotalWin = p.AvailTotalWin,
										RemoteIP = p.RemoteIP,
										DeviceType = p.DeviceType,
										GameName = sGameName,
										OrderTime = p.OrderTime,
										Total = Convert.ToInt32(result.Data.Page.TotalSize),
										CreatedDate = DateTime.Now
									};
									db.IGGames.AddOrUpdate(iGGame);
								}
							}
							catch (Exception ex)
							{
								_common.LogError("Game Record " + p.GameSerialID + " " + DateTime.Now.ToString() + "  " + ex.ToString(), "IG");
							}
						}
						db.SaveChanges();
					}

					return true;
				}
			}
			catch (WebException ex)
			{
				_common.LogError("IG/GetGameRecordExtend WebExp" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "IG");
				return false;

			}
			catch (Exception ex)
			{
				_common.LogError("IG/GetGameRecordExtend" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "IG");
				return false;
			}
		}

		public bool GetTransactionRecord(string startTime, string endTime)
		{
			string sMainID = Guid.NewGuid().ToString();

			try
			{
				string method = "transaction/log";
				string webAddr = P_ApiUrl + method;
				int p_page = 1;
				string P_Sign = "EndTime=" + endTime + "&PageNumber=" + p_page + "&StartTime=" + startTime + "&PrivateKey=" + P_CompKey;

				HashAlgorithm hashAlgorithm = new SHA256Managed();
				byte[] byteAry =
				hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(P_Sign));
				string stringEncryptResult = Convert.ToBase64String(byteAry);

				ServicePointManager.Expect100Continue = true;
				ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
				ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

				var httpWebRequest = (HttpWebRequest)WebRequest.Create(webAddr);
				httpWebRequest.ContentType = "application/json";
				httpWebRequest.Method = "POST";

				httpWebRequest.Headers.Add("merchantName", "BingoTown");
				httpWebRequest.Headers.Add("sign", stringEncryptResult);

				httpWebRequest.Timeout = 60000;
				httpWebRequest.ReadWriteTimeout = 60000;

				using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
				{
					string json = "{ \"PageNumber\" : " + p_page +
													", \"StartTime\" : \"" + startTime +
													"\", \"EndTime\" : \"" + endTime + "\"} ";


					streamWriter.Write(json);
					streamWriter.Flush();
				}
				var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
				using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
				{
					var responseText = streamReader.ReadToEnd();

					TransactionRecordVM result = JsonConvert.DeserializeObject<TransactionRecordVM>(responseText);

					using (BingoConn db = new BingoConn())
					{
						IGGameMain iGGameMain = new IGGameMain()
						{
							Id = sMainID,
							Code = result.Code,
							Message = result.Message,
							PageNumber = Convert.ToInt32(result.Data.Page.TotalPage),
							PageSize = Convert.ToInt32(result.Data.Page.PageSize),
							TotalPage = Convert.ToInt32(result.Data.Page.TotalPage),
							TotalSize = Convert.ToInt32(result.Data.Page.TotalSize),
							startTime = Convert.ToDateTime(startTime),
							endTime = Convert.ToDateTime(endTime),
							CreatedDate = DateTime.Now
						};
						db.IGGameMains.Add(iGGameMain);
						db.SaveChanges();

						var angpaoList = result.Data.Record.Where(x => x.OperationMethod == 3 && x.Result == 1);
						foreach (var p in angpaoList)
						{
							try
							{
								var iggame = db.IGGames.Where(x => x.UserName.ToUpper() == p.PlayerAccount.ToUpper() && x.WinAmount == p.TransAmount && x.OrderTime == p.CreateTime).FirstOrDefault();
								if (iggame == null)
								{
									var iGGame = new IGGame()
									{
										fId = sMainID,
										GameSerialNo = Convert.ToInt64(p.CreateTime.ToString("yyMMddHHmmss")),
										UserName = p.PlayerAccount,
										BetCoin = 0,
										WinAmount = p.TransAmount,
										AvailTotalBet = 0,
										AvailTotalWin = p.TransAmount,
										GameName = "Angpao",
										OrderTime = p.CreateTime,
										Total = Convert.ToInt32(result.Data.Page.TotalSize),
										CreatedDate = DateTime.Now
									};
									db.IGGames.AddOrUpdate(iGGame);
								}
							}
							catch (Exception ex)
							{
								_common.LogError("Trans Record " + string.Format("{0}|{1}|{2}", p.PlayerAccount, p.TransAmount, p.CreateTime) + " " + DateTime.Now.ToString() + "  " + ex.ToString(), "IG");
							}

						}
						db.SaveChanges();
					}

					//If record more than 1 page, need to get the record from Page 2 and above
					int iPageIndex = Int32.Parse(result.Data.Page.TotalPage);

					if (iPageIndex > 1)
					{
						for (int x = 2; x <= iPageIndex; x++)
						{
							bool IsOK = GetTransactionRecordExtend(x, startTime, endTime);
						}
					}

					return true;
				}
			}
			catch (WebException ex)
			{
				_common.LogError("IG/GetTransactionRecord WebExp" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "IG");
				return false;
			}
			catch (Exception ex)
			{
				_common.LogError("IG/GetTransactionRecord" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "IG");
				return false;
			}
		}

		public bool GetTransactionRecordExtend(int p_page, string startTime, string endTime)
		{
			string sMainID = Guid.NewGuid().ToString();

			try
			{
				string method = "transaction/log";
				string webAddr = P_ApiUrl + method;
				string P_Sign = "EndTime=" + endTime + "&PageNumber=" + p_page + "&StartTime=" + startTime + "&PrivateKey=" + P_CompKey;

				HashAlgorithm hashAlgorithm = new SHA256Managed();
				byte[] byteAry =
				hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(P_Sign));
				string stringEncryptResult = Convert.ToBase64String(byteAry);

				ServicePointManager.Expect100Continue = true;
				ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
				ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

				var httpWebRequest = (HttpWebRequest)WebRequest.Create(webAddr);
				httpWebRequest.ContentType = "application/json";
				httpWebRequest.Method = "POST";

				httpWebRequest.Headers.Add("merchantName", "BingoTown");
				httpWebRequest.Headers.Add("sign", stringEncryptResult);

				httpWebRequest.Timeout = 60000;
				httpWebRequest.ReadWriteTimeout = 60000;

				using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
				{
					string json = "{ \"PageNumber\" : " + p_page +
													", \"StartTime\" : \"" + startTime +
													"\", \"EndTime\" : \"" + endTime + "\"} ";


					streamWriter.Write(json);
					streamWriter.Flush();
				}
				var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
				using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
				{
					var responseText = streamReader.ReadToEnd();

					TransactionRecordVM result = JsonConvert.DeserializeObject<TransactionRecordVM>(responseText);

					using (BingoConn db = new BingoConn())
					{
						IGGameMain iGGameMain = new IGGameMain()
						{
							Id = sMainID,
							Code = result.Code,
							Message = result.Message,
							PageNumber = Convert.ToInt32(result.Data.Page.TotalPage),
							PageSize = Convert.ToInt32(result.Data.Page.PageSize),
							TotalPage = Convert.ToInt32(result.Data.Page.TotalPage),
							TotalSize = Convert.ToInt32(result.Data.Page.TotalSize),
							startTime = Convert.ToDateTime(startTime),
							endTime = Convert.ToDateTime(endTime),
							CreatedDate = DateTime.Now
						};
						db.IGGameMains.Add(iGGameMain);
						db.SaveChanges();

						var angpaoList = result.Data.Record.Where(x => x.OperationMethod == 3 && x.Result == 1);
						foreach (var p in angpaoList)
						{
							try
							{
								var iggame = db.IGGames.Where(x => x.UserName.ToUpper() == p.PlayerAccount.ToUpper() && x.WinAmount == p.TransAmount && x.OrderTime == p.CreateTime).FirstOrDefault();
								if (iggame == null)
								{
									var iGGame = new IGGame()
									{
										fId = sMainID,
										GameSerialNo = Convert.ToInt64(p.CreateTime.ToString("yyMMddHHmmss")),
										UserName = p.PlayerAccount,
										BetCoin = 0,
										WinAmount = p.TransAmount,
										GameName = "Angpao",
										OrderTime = p.CreateTime,
										Total = Convert.ToInt32(result.Data.Page.TotalSize),
										CreatedDate = DateTime.Now
									};
									db.IGGames.AddOrUpdate(iGGame);
								}
							}
							catch (Exception ex)
							{
								_common.LogError("Trans Record " + string.Format("{0}|{1}|{2}", p.PlayerAccount, p.TransAmount, p.CreateTime) + " " + DateTime.Now.ToString() + "  " + ex.ToString(), "IG");
							}
						}
						db.SaveChanges();
					}

					return true;
				}
			}
			catch (WebException ex)
			{
				_common.LogError("IG/GetTransactionRecordExtend WebExp" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "IG");
				return false;

			}
			catch (Exception ex)
			{
				_common.LogError("IG/GetTransactionRecordExtend" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "IG");
				return false;
			}
		}

		public GameListVM GetGameListName()
		{
			try
			{
				string method = "codetable/gamelist";
				string webAddr = P_ApiUrl + method;
				string txnid = P_GamePrefix + "GM" + Guid.NewGuid().ToString("N").Substring(0, 10);
				string P_Sign = "PrivateKey=" + P_CompKey;

				HashAlgorithm hashAlgorithm = new SHA256Managed();
				byte[] byteAry =
				hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(P_Sign));
				string stringEncryptResult = Convert.ToBase64String(byteAry);

				ServicePointManager.Expect100Continue = true;
				ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
				ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

				var httpWebRequest = (HttpWebRequest)WebRequest.Create(webAddr);
				httpWebRequest.ContentType = "application/json";
				httpWebRequest.Method = "POST";

				httpWebRequest.Headers.Add("merchantName", "BingoTown");
				httpWebRequest.Headers.Add("sign", stringEncryptResult);

				httpWebRequest.Timeout = 60000;
				httpWebRequest.ReadWriteTimeout = 60000;

				using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
				{
					string json = "";
					streamWriter.Write(json);
					streamWriter.Flush();
				}

				var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
				using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
				{
					var responseText = streamReader.ReadToEnd();


					GameListVM result = JsonConvert.DeserializeObject<GameListVM>(responseText);

					return result;
				}
			}
			catch (WebException ex)
			{
				_common.LogError("IG/GetGameListName WebExp" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "IG");
				return null;

				//using (var streamReader = new StreamReader(ex.Response.GetResponseStream()))
				//{
				//    var responseText = streamReader.ReadToEnd();
				//    CommonVM result = JsonConvert.DeserializeObject<CommonVM>(responseText);

				//    using (BingoConn dbConn = new BingoConn())
				//    {
				//        string errID = Guid.NewGuid().ToString();

				//        IGGameMain iGGameMain = new IGGameMain()
				//        {
				//            Id = errID,
				//            Code = result.Code,
				//            Message = "IG GetGameList WebError: " + result.Message,
				//            CreatedDate = DateTime.Now
				//        };
				//        dbConn.IGGameMains.Add(iGGameMain);
				//        dbConn.SaveChanges();
				//    }
				//    return null;
				//}
			}
			catch (Exception ex)
			{
				_common.LogError("IG/GetGameListName" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "IG");
				return null;

				//using (BingoConn dbConn = new BingoConn())
				//{
				//    string errID = Guid.NewGuid().ToString();

				//    IGGameMain iGGameMain = new IGGameMain()
				//    {
				//        Id = errID,
				//        Message = "IG GetGameList Error: " + ex.Message,
				//        CreatedDate = DateTime.Now
				//    };
				//    dbConn.IGGameMains.Add(iGGameMain);
				//    dbConn.SaveChanges();
				//}
				//return null;
			}
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
				_common.LogError("IG/UpdJobTime" + " " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "IG");
				return false;
			}
		}

		#endregion


		// GET: IG
		public void IGJob()
		{
			try
			{
				lock (MyLock)
				{
					string startTime = GetJobTime("Start");
					string endTime = GetJobTime("End");
					if (Convert.ToDateTime(startTime).ToString("yyyy-MM-dd") != Convert.ToDateTime(endTime).ToString("yyyy-MM-dd"))
					{
						if (Convert.ToDateTime(startTime).ToString("HH:mm:ss.fff") == "23:59:59.999")
							startTime = Convert.ToDateTime(startTime).AddDays(1).ToString("yyyy-MM-dd") + " 00:00:00.000";
						else
							endTime = Convert.ToDateTime(startTime).ToString("yyyy-MM-dd") + " 23:59:59.999";
					}
					bool IsAllRecord = GetGameRecord(startTime, endTime);
				}
			}
			catch (Exception ex)
			{
				_common.LogError("Start - " + DateTime.Now.ToString() + "  " + ex.Message.ToString(), "IG");
			}
		}
	}
}