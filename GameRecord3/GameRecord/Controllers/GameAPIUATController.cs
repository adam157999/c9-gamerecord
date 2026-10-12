using GameRecord.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Xml;
using RestSharp;

namespace GameRecord.Controllers
{
	public class GameAPIUATController : ApiController
	{
		public static readonly object MyLock = new object();
		private static Random random = new Random();
		private static string FilePrefix = "GAMEAPIUAT";
		public string apiPhoneList = ConfigurationManager.AppSettings["GameAPIPhoneList"];

		public class GetBalanceVM
		{
			public string PlayerID { get; set; }
			public decimal? Balance { get; set; }
			public string Game { get; set; }
			public string Message { get; set; }
			public int Code { get; set; }
		}

		public class CheckAndCreateAccountVM
		{
			public string PlayerID { get; set; }
			public string GamePlayerID { get; set; }
			public string Game { get; set; }
			public string Message { get; set; }
			public int Code { get; set; }
		}

		public class DepositVM
		{
			public string PlayerID { get; set; }
			public string Game { get; set; }
			public string Message { get; set; }
			public int Code { get; set; }
		}

		public class WithdrawVM
		{
			public string PlayerID { get; set; }
			public string Game { get; set; }
			public decimal? Amount { get; set; }
			[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
			public bool? LockFromGame { get; set; }
			public string Message { get; set; }
			public int Code { get; set; }
		}

		public class GetGameUrlVM
		{
			public string PlayerID { get; set; }
			[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
			public string Url { get; set; }
			public string Game { get; set; }
			[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
			public string Type { get; set; }
			[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
			public bool? IsRefund { get; set; }
			[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
			public bool? LockFromGame { get; set; }
			[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
			public GameApkVM ApkInfo { get; set; }
			public string Message { get; set; }
			public int Code { get; set; }
		}

		public class GameApkVM
		{
			public GameApkInfoVM Android { get; set; }
			public GameApkInfoVM iOS { get; set; }

		}

		public class GameApkInfoVM
		{
			public string DownloadLink { get; set; }
			public string Package { get; set; }
			public string Activity { get; set; }
			public string DeepLink { get; set; }
			public string GamePlayerID { get; set; }
			public string Passwords { get; set; }
		}

		public class GetGameRecordVM
		{
			public string Message { get; set; }
			public int Code { get; set; }
			public string PlayerID { get; set; }
			public string Game { get; set; }
			public int TotalRows { get; set; }
			//public List<GameRecordData> Records { get; set; }
			public DataTable Table { get; set; }
		}

		public class GameRecordData
		{
			public DateTime BetTime { get; set; }
			public string GameName { get; set; }
			public decimal Turnover { get; set; }
			public decimal WinLose { get; set; }
		}

		public class Get4DPackageVM
		{
			public string PlayerID { get; set; }
			public dynamic Package { get; set; }
			public string Game { get; set; }
			public string Message { get; set; }
			public int Code { get; set; }
		}

		public class GetMDramaUrlVM
		{
			public string Url { get; set; }
			public string Message { get; set; }
			public int Code { get; set; }
		}

		public class SplitCashAndChipVM
		{
			public string PlayerID { get; set; }
			public decimal? Cash { get; set; }
			public decimal? Chip { get; set; }
			public string Game { get; set; }
			public string Message { get; set; }
			public int Code { get; set; }
		}

		[HttpGet]
		public HttpResponseMessage GetBalance(string Game, string PlayerID, string Currency, string TrackerId)
		{
			lock (MyLock)
			{
				string apiurl = "";
				Common _common = new Common();
				int TimeoutSec = 180000; //3 mins

				try
				{
					_common.InsertLog("GetBalance", "TrackerId", TrackerId, "Request", "", "Game", Game, "PlayerID", PlayerID, "Currency", Currency);

					using (Bingo_stagingConn db = new Bingo_stagingConn())
					{
						var res = new GetBalanceVM();
						string GameID = Game.ToUpper().Trim();
						string UPlayerID = PlayerID.ToUpper().Trim();
						string UCurrency = Currency.ToUpper().Trim();

						//bg
						if (GameID == "BG")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["BGApiUrl"] + "/GetBalance?UID={0}&IP={1}", record.GamePlayerID, GetIPAddress());
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.error == null)
									{
										res.PlayerID = UPlayerID;
										res.Balance = Convert.ToDecimal(result.result);
										res.Game = GameID;
										res.Message = "Successfully get balance.";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = UPlayerID;
										res.Balance = null;
										res.Game = GameID;
										res.Message = result.error.message;
										//res.Message = "Get balance api error.";
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.Balance = null;
								res.Game = GameID;
								res.Message = "Game account not found.";
								res.Code = 1;
							}
						}
						//ag
						else if (GameID == "AG")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["AGApiUrl"] + "/GetBalance?loginname={0}&currency={1}", record.GamePlayerID, Currency);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (string.IsNullOrWhiteSpace(Convert.ToString(result.ErrorDesc)))
									{
										res.PlayerID = UPlayerID;
										res.Balance = Convert.ToDecimal(result.ErrorCode);
										res.Game = GameID;
										res.Message = "Successfully get balance.";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = UPlayerID;
										res.Balance = null;
										res.Game = GameID;
										res.Message = result.ErrorDesc;
										//res.Message = "Get balance api error.";
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.Balance = null;
								res.Game = GameID;
								res.Message = "Game account not found.";
								res.Code = 1;
							}
						}
						//playtech
						else if (GameID == "PT")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["PTApiUrl"] + "/GetPlayerBalance?playerName={0}", record.GamePlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.error == null)
									{
										res.PlayerID = UPlayerID;
										res.Balance = Convert.ToDecimal(result.result.balance);
										res.Game = GameID;
										res.Message = "Successfully get balance.";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = UPlayerID;
										res.Balance = null;
										res.Game = GameID;
										res.Message = result.error;
										//res.Message = "Get balance api error.";
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.Balance = null;
								res.Game = GameID;
								res.Message = "Game account not found.";
								res.Code = 1;
							}
						}
						//play8oy
						else if (GameID == "IG")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["IGApiUrl"] + "/GetBalance?UID={0}&IP={1}", record.GamePlayerID, GetIPAddress());
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (Convert.ToString(result.Code) == "0")
									{
										res.PlayerID = UPlayerID;
										res.Balance = Convert.ToDecimal(result.Data.Balance);
										res.Game = GameID;
										res.Message = "Successfully get balance.";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = UPlayerID;
										res.Balance = null;
										res.Game = GameID;
										res.Message = result.Message;
										//res.Message = "Get balance api error.";
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.Balance = null;
								res.Game = GameID;
								res.Message = "Game account not found.";
								res.Code = 1;
							}
						}
						//28win
						else if (GameID == "BT28WIN")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["BT28WINApiUrl"] + "/GetProfile?loginID={0}", record.GamePlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;

								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (Convert.ToString(result.ErrorCode) == "0")
									{
										res.PlayerID = UPlayerID;
										res.Balance = Convert.ToDecimal(result.Data.balance);
										res.Game = GameID;
										res.Message = "Successfully get balance.";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = UPlayerID;
										res.Balance = null;
										res.Game = GameID;
										//res.Message = "Get balance api error.";
										res.Message = result.ErrorDesc;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.Balance = null;
								res.Game = GameID;
								res.Message = "Game account not found.";
								res.Code = 1;
							}

						}
						//allcity
						else if (GameID == "ALLCITY")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["ALLCITYApiUrl"] + "/GetUserInfo?account={0}", record.GamePlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.code == 200)
									{
										res.PlayerID = UPlayerID;
										res.Balance = Convert.ToDecimal(result.balance);
										res.Game = GameID;
										res.Message = "Successfully get balance.";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = UPlayerID;
										res.Balance = null;
										res.Game = GameID;
										//res.Message = "Get balance api error.";
										res.Message = result.msg;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.Balance = null;
								res.Game = GameID;
								res.Message = "Game account not found.";
								res.Code = 1;
							}
						}
						//ibc
						else if (GameID == "IBC")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["IBCApiUrl"] + "/CheckUserBalance?username={0}", record.GamePlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.error_code == 0 && result.Data != null)
									{
										if (result.Data.Count > 0)
										{
											res.PlayerID = UPlayerID;
											res.Balance = (result.Data[0].error_code == 0 ? Convert.ToDecimal(result.Data[0].balance) : (result.Data[0].error_code == 6 ? 0 : (decimal?)null));
											res.Game = GameID;
											res.Code = (result.Data[0].error_code == 0 || result.Data[0].error_code == 6) ? 0 : 2;
											res.Message = (result.Data[0].error_code == 0 || result.Data[0].error_code == 6) ? "Successfully get balance." : result.message;
										}
										else
										{
											res.PlayerID = UPlayerID;
											res.Balance = null;
											res.Game = GameID;
											res.Code = 2;
											//res.Message = "Get balance api error.";
											res.Message = result.message;
										}
									}
									else
									{
										res.PlayerID = UPlayerID;
										res.Balance = null;
										res.Game = GameID;
										res.Code = 2;
										//res.Message = "Get balance api error.";
										res.Message = result.message;
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.Balance = null;
								res.Game = GameID;
								res.Code = 1;
								res.Message = "Game account not found.";
							}
						}
						//evo888
						else if (GameID == "EVO888")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["EVO888ApiUrl"] + "/GetUserInfo?username={0}", record.GamePlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.code == 0 && Convert.ToString(result.success).Trim().ToUpper() == "TRUE" && result.results != null)
									{
										res.PlayerID = UPlayerID;
										res.Balance = Convert.ToDecimal(result.results.balance);
										res.Game = GameID;
										res.Code = 0;
										res.Message = "Successfully get balance.";
									}
									else
									{
										res.PlayerID = UPlayerID;
										res.Balance = null;
										res.Game = GameID;
										res.Message = result.msg;
										//res.Message = "Get balance api error.";
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.Balance = null;
								res.Game = GameID;
								res.Message = "Game account not found.";
								res.Code = 1;
							}
						}
						//vpower
						else if (GameID == "VPOWER")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["VPOWERApiUrl"] + "/GetBalance?username={0}", record.GamePlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (Convert.ToString(result.ErrorCode) == "0")
									{
										res.PlayerID = UPlayerID;
										res.Balance = Convert.ToDecimal(result.Credit);
										res.Game = GameID;
										res.Code = 0;
										res.Message = "Successfully get balance.";
									}
									else
									{
										res.PlayerID = UPlayerID;
										res.Balance = null;
										res.Game = GameID;
										//res.Message = "Get balance api error.";
										res.Message = result.msg;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.Balance = null;
								res.Game = GameID;
								res.Message = "Game account not found.";
								res.Code = 1;
							}
						}
						//sexygaming
						else if (GameID == "SEXYGAMING")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["SEXYGAMINGApiUrl"] + "/GetBalance?username={0}", record.GamePlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (Convert.ToString(result.error) == "0" && Convert.ToString(result.message).Trim().ToUpper() == "SUCCESS" && result.data != null)
									{
										res.PlayerID = UPlayerID;
										res.Balance = Convert.ToDecimal(result.data.Balance);
										res.Game = GameID;
										res.Code = 0;
										res.Message = "Successfully get balance.";
									}
									else
									{
										res.PlayerID = UPlayerID;
										res.Balance = null;
										res.Game = GameID;
										res.Message = result.msg;
										//res.Message = "Get balance api error.";
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.Balance = null;
								res.Game = GameID;
								res.Message = "Game account not found.";
								res.Code = 1;
							}
						}
						//spadegaming
						else if (GameID == "SPADEGAMING")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["SPADEGAMINGApiUrl"] + "/GetBalance?username={0}", record.GamePlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (Convert.ToString(result.code) == "0" && Convert.ToString(result.msg).Trim().ToUpper() == "SUCCESS" && result.list != null)
									{
										if (result.list.Count > 0)
										{
											res.PlayerID = UPlayerID;
											res.Balance = Convert.ToDecimal(result.list[0].balance);
											res.Game = GameID;
											res.Code = 0;
											res.Message = "Successfully get balance.";
										}
										else
										{
											res.PlayerID = UPlayerID;
											res.Balance = null;
											res.Game = GameID;
											res.Message = result.msg;
											//res.Message = "Get balance api error.";
											res.Code = 2;
										}
									}
									else
									{
										res.PlayerID = UPlayerID;
										res.Balance = null;
										res.Game = GameID;
										res.Message = result.msg;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.Balance = null;
								res.Game = GameID;
								res.Message = "Game account not found.";
								res.Code = 1;
							}
						}
						//mega
						else if (GameID == "MEGA")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["MEGAApiUrl"] + "/GetBalance?loginId={0}", record.GamePlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.result != null)
									{
										res.PlayerID = UPlayerID;
										res.Balance = Convert.ToDecimal(result.result);
										res.Game = GameID;
										res.Message = "Successfully get balance.";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = UPlayerID;
										res.Balance = null;
										res.Game = GameID;
										res.Message = result.error.message;
										//res.Message = "Get balance api error.";
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.Balance = null;
								res.Game = GameID;
								res.Message = "Game account not found.";
								res.Code = 1;
							}
						}
						//evolution
						else if (GameID == "EVOLUTIONGAMING")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["EVOLUTIONGAMINGApiUrl"] + "/GetBalance?username={0}", record.GamePlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.userbalance != null)
									{
										if (result.userbalance.result == "Y")
										{
											res.PlayerID = UPlayerID;
											res.Balance = Convert.ToDecimal(result.userbalance.tbalance);
											res.Game = GameID;
											res.Message = "Successfully get balance.";
											res.Code = 0;
										}
										else
										{
											res.PlayerID = UPlayerID;
											res.Balance = null;
											res.Game = GameID;
											res.Message = result.userbalance.errormsg;
											//res.Message = "Get balance api error.";
											res.Code = 2;
										}
									}
									else
									{
										res.PlayerID = UPlayerID;
										res.Balance = null;
										res.Game = GameID;
										res.Message = "Get balance api error.";
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.Balance = null;
								res.Game = GameID;
								res.Message = "Game account not found.";
								res.Code = 1;
							}
						}
						//menang88
						else if (GameID == "MENANG88")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["MENANG88ApiUrl"] + "/GetBalance?userId={0}", record.GamePlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (Convert.ToString(result.status) == "1")
									{
										res.PlayerID = UPlayerID;
										res.Balance = Convert.ToDecimal(result.creditbalance);
										res.Game = GameID;
										res.Message = "Successfully get balance.";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = UPlayerID;
										res.Balance = null;
										res.Game = GameID;
										//res.Message = "Get balance api error.";
										res.Message = result.message;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.Balance = null;
								res.Game = GameID;
								res.Message = "Game account not found.";
								res.Code = 1;
							}
						}
						//xe88
						else if (GameID == "XE88")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["XE88ApiUrl"] + "/GetBalance?username={0}", record.GamePlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (Convert.ToString(result.code) == "0")
									{
										res.PlayerID = UPlayerID;
										res.Balance = Convert.ToDecimal(result.result.balance);
										res.Game = GameID;
										res.Message = "Successfully get balance.";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = UPlayerID;
										res.Balance = null;
										res.Game = GameID;
										res.Message = result.message;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.Balance = null;
								res.Game = GameID;
								res.Message = "Game account not found.";
								res.Code = 1;
							}
						}
						//kypoker
						else if (GameID == "KYPOKER")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["KYPOKERApiUrl"] + "/GetBalance?username={0}", record.GamePlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.d != null)
									{
										if (Convert.ToString(result.d.code) == "0")
										{
											res.PlayerID = UPlayerID;
											res.Balance = Convert.ToDecimal(result.d.freeMoney);
											res.Game = GameID;
											res.Message = "Successfully get balance.";
											res.Code = 0;
										}
										else
										{
											res.PlayerID = UPlayerID;
											res.Balance = null;
											res.Game = GameID;
											res.Message = result.d.code;
											res.Code = 2;
										}
									}
									else
									{
										res.PlayerID = UPlayerID;
										res.Balance = null;
										res.Game = GameID;
										res.Message = "Get balance api error.";
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.Balance = null;
								res.Game = GameID;
								res.Message = "Game account not found.";
								res.Code = 1;
							}
						}
						//sabaesports
						else if (GameID == "SABAESPORTS")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["SABAESPORTSApiUrl"] + "/CheckUserBalance?username={0}", record.GamePlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.error_code == 0 && result.Data != null)
									{
										if (result.Data.Count > 0)
										{
											res.PlayerID = UPlayerID;
											res.Balance = (result.Data[0].error_code == 0 ? Convert.ToDecimal(result.Data[0].balance) : (result.Data[0].error_code == 6 ? 0 : (decimal?)null));
											res.Game = GameID;
											res.Code = (result.Data[0].error_code == 0 || result.Data[0].error_code == 6) ? 0 : 2;
											res.Message = (result.Data[0].error_code == 0 || result.Data[0].error_code == 6) ? "Successfully get balance." : result.message;
										}
										else
										{
											res.PlayerID = UPlayerID;
											res.Balance = null;
											res.Game = GameID;
											res.Code = 2;
											//res.Message = "Get balance api error.";
											res.Message = result.message;
										}
									}
									else
									{
										res.PlayerID = UPlayerID;
										res.Balance = null;
										res.Game = GameID;
										res.Code = 2;
										//res.Message = "Get balance api error.";
										res.Message = result.message;
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.Balance = null;
								res.Game = GameID;
								res.Code = 1;
								res.Message = "Game account not found.";
							}
						}
						//tpg
						else if (GameID == "TPG")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["TPGApiUrl"] + "/GetPlayerGameBalance?playerName={0}&displayName={1}&currency={2}&mcUID={3}",
																				record.GamePlayerID, UPlayerID, UCurrency, UPlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.status == 1)
									{
										res.PlayerID = UPlayerID;
										res.Balance = Convert.ToDecimal(result.totalBalance);
										res.Game = GameID;
										res.Message = "Successfully get balance.";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = UPlayerID;
										res.Balance = null;
										res.Game = GameID;
										res.Message = result.message;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.Balance = null;
								res.Game = GameID;
								res.Message = "Game account not found.";
								res.Code = 1;
							}
						}
						//tpgfish
						else if (GameID == "TPGFISH")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["TPGFISHApiUrl"] + "/GetPlayerGameBalance?playerName={0}&displayName={1}&currency={2}&mcUID={3}",
																				record.GamePlayerID, UPlayerID, UCurrency, UPlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.status == 1)
									{
										res.PlayerID = UPlayerID;
										res.Balance = Convert.ToDecimal(result.totalBalance);
										res.Game = GameID;
										res.Message = "Successfully get balance.";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = UPlayerID;
										res.Balance = null;
										res.Game = GameID;
										res.Message = result.message;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.Balance = null;
								res.Game = GameID;
								res.Message = "Game account not found.";
								res.Code = 1;
							}
						}
						//vgaming
						else if (GameID == "VGAMING")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["VGAMINGApiUrl"] + "/CheckUserBalance?username={0}", record.GamePlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.error_code == 0 && result.Data != null)
									{
										if (result.Data.Count > 0)
										{
											res.PlayerID = UPlayerID;
											res.Balance = (result.Data[0].error_code == 0 ? Convert.ToDecimal(result.Data[0].balance) : (result.Data[0].error_code == 6 ? 0 : (decimal?)null));
											res.Game = GameID;
											res.Code = (result.Data[0].error_code == 0 || result.Data[0].error_code == 6) ? 0 : 2;
											res.Message = (result.Data[0].error_code == 0 || result.Data[0].error_code == 6) ? "Successfully get balance." : result.message;
										}
										else
										{
											res.PlayerID = UPlayerID;
											res.Balance = null;
											res.Game = GameID;
											res.Code = 2;
											//res.Message = "Get balance api error.";
											res.Message = result.message;
										}
									}
									else
									{
										res.PlayerID = UPlayerID;
										res.Balance = null;
										res.Game = GameID;
										res.Code = 2;
										//res.Message = "Get balance api error.";
										res.Message = result.message;
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.Balance = null;
								res.Game = GameID;
								res.Code = 1;
								res.Message = "Game account not found.";
							}
						}
						//cq9
						else if (GameID == "CQ9")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["CQ9ApiUrl"] + "/GetBalance?username={0}", record.GamePlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.status != null)
									{
										if (Convert.ToString(result.status.code) == "0")
										{
											if (result.data != null)
											{
												res.PlayerID = UPlayerID;
												res.Balance = Convert.ToDecimal(result.data.balance);
												res.Game = GameID;
												res.Message = "Successfully get balance.";
												res.Code = 0;
											}
											else
											{
												res.PlayerID = UPlayerID;
												res.Balance = null;
												res.Game = GameID;
												res.Message = result.status.message;
												res.Code = 2;
											}
										}
										else
										{
											res.PlayerID = UPlayerID;
											res.Balance = null;
											res.Game = GameID;
											res.Message = result.status.message;
											res.Code = 2;
										}
									}
									else
									{
										res.PlayerID = UPlayerID;
										res.Balance = null;
										res.Game = GameID;
										res.Message = "Unknown error";
										res.Code = 3;
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.Balance = null;
								res.Game = GameID;
								res.Message = "Game account not found.";
								res.Code = 1;
							}
						}
						//default
						else
						{
							res.PlayerID = UPlayerID;
							res.Balance = null;
							res.Game = GameID;
							res.Message = "Invalid game.";
							res.Code = 4;
						}

						_common.InsertLog("GetBalance", "TrackerId", TrackerId, "Response", JsonConvert.SerializeObject(res), "Game", Game, "PlayerID", PlayerID, "Currency", Currency);

						return new HttpResponseMessage()
						{
							StatusCode = HttpStatusCode.OK,
							Content = new StringContent(JsonConvert.SerializeObject(res), System.Text.Encoding.UTF8, "application/json")
						};
					}
				}
				catch (Exception ex)
				{
					_common.LogError($"{TrackerId}:GetBalance:EXCEPTION " + string.Format("{0}|{1}|{2}", Game, PlayerID, Currency) + " " + ex.Message.ToString(), FilePrefix);
					_common.InsertLog("GetBalance", "TrackerId", TrackerId, "Exception", ex.Message.ToString(), "Game", Game, "PlayerID", PlayerID, "Currency", Currency);
					dynamic res = new ExpandoObject();
					res.Code = -9999;
					res.Message = ex.Message.ToString();
					return new HttpResponseMessage()
					{
						StatusCode = HttpStatusCode.InternalServerError,
						Content = new StringContent(JsonConvert.SerializeObject(res), System.Text.Encoding.UTF8, "application/json")
					};
				}
			}
		}

		[HttpGet]
		public HttpResponseMessage CheckAndCreateAccount(string Game, string PlayerID, string Currency, string TrackerId)
		{
			lock (MyLock)
			{
				string apiurl = "";
				Common _common = new Common();
				int TimeoutSec = 180000; //3 mins
				string gamePlayerID = string.Empty;

				try
				{
					_common.InsertLog("CheckAndCreateAccount", "TrackerId", TrackerId, "Request", "", "Game", Game, "PlayerID", PlayerID, "Currency", Currency);

					using (Bingo_stagingConn db = new Bingo_stagingConn())
					{
						var res = new CheckAndCreateAccountVM();
						string GameID = Game.ToUpper().Trim();
						string UPlayerID = PlayerID.ToUpper().Trim();
						string UCurrency = Currency.ToUpper().Trim();

						//bg
						if (GameID == "BG")
						{
							var record = db.GameMappingUATs.Where(x =>
								x.Game.ToUpper().Trim() == GameID &&
								x.PlayerID.ToUpper() == UPlayerID &&
								x.Currency.Trim().ToUpper() == UCurrency &&
								x.IsActive == true
							).FirstOrDefault();

							if (record == null)
							{
								gamePlayerID = FormGamePlayerID(GameID, UPlayerID);
								apiurl = string.Format(ConfigurationManager.AppSettings["BGApiUrl"] + "/Register?UID={0}&IP={1}&mcUID={2}", gamePlayerID, GetIPAddress(), UPlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.error == null)
									{
										if (result.result.success == true)
										{
											res.PlayerID = UPlayerID;
											res.GamePlayerID = Convert.ToString(result.result.loginId);
											res.Game = GameID;
											res.Message = "Success";
											res.Code = 0;
										}
										else
										{
											res.PlayerID = UPlayerID;
											res.GamePlayerID = null;
											res.Game = GameID;
											//res.Message = "Create account api error.";
											res.Message = result.error.message;
											res.Code = 2;
										}
									}
									else
									{
										res.PlayerID = UPlayerID;
										res.GamePlayerID = null;
										res.Game = GameID;
										//res.Message = "Create account api error.";
										res.Message = result.error.message;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.GamePlayerID = record.GamePlayerID;
								res.Game = GameID;
								res.Message = "Retrieve Success";
								res.Code = 1;
							}
						}
						//ag
						else if (GameID == "AG")
						{
							var record = db.GameMappingUATs.Where(x =>
								x.Game.ToUpper().Trim() == GameID &&
								x.PlayerID.ToUpper() == UPlayerID &&
								x.Currency.Trim().ToUpper() == UCurrency &&
								x.IsActive == true
							).FirstOrDefault();

							if (record == null)
							{
								gamePlayerID = FormGamePlayerID(GameID, PlayerID);
								apiurl = string.Format(ConfigurationManager.AppSettings["AGApiUrl"] + "/CheckOrCreateAccount?loginname={0}&currency={1}&mcUID={2}", gamePlayerID, Currency, UPlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (string.IsNullOrWhiteSpace(Convert.ToString(result.ErrorDesc)))
									{
										res.PlayerID = UPlayerID;
										res.GamePlayerID = gamePlayerID;
										res.Game = GameID;
										res.Message = "Success";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = UPlayerID;
										res.GamePlayerID = null;
										res.Game = GameID;
										//res.Message = "Create account api error.";
										res.Message = result.ErrorDesc;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.GamePlayerID = record.GamePlayerID;
								res.Game = GameID;
								res.Message = "Retrieve Success";
								res.Code = 1;
							}
						}
						//playtech
						else if (GameID == "PT")
						{
							var record = db.GameMappingUATs.Where(x =>
								x.Game.ToUpper().Trim() == GameID &&
								x.PlayerID.ToUpper() == UPlayerID &&
								x.Currency.Trim().ToUpper() == UCurrency &&
								x.IsActive == true
							).FirstOrDefault();

							if (record == null)
							{
								gamePlayerID = FormGamePlayerID(GameID, PlayerID);
								apiurl = string.Format(ConfigurationManager.AppSettings["PTApiUrl"] + "/CreatePlayer?playerName={0}&mcUID={1}", gamePlayerID, UPlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.error == null)
									{
										if (Convert.ToString(result.result.result).ToUpper() == "NEW PLAYER HAS BEEN CREATED")
										{
											res.PlayerID = UPlayerID;
											res.GamePlayerID = gamePlayerID;
											res.Game = GameID;
											res.Message = "Success";
											res.Code = 0;
										}
										else
										{
											res.PlayerID = UPlayerID;
											res.GamePlayerID = null;
											res.Game = GameID;
											//res.Message = "Create account api error.";
											res.Message = result.error;
											res.Code = 2;
										}
									}
									else
									{
										res.PlayerID = UPlayerID;
										res.GamePlayerID = null;
										res.Game = GameID;
										//res.Message = "Create account api error.";
										res.Message = result.error;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.GamePlayerID = record.GamePlayerID;
								res.Game = GameID;
								res.Message = "Retrieve Success";
								res.Code = 1;
							}
						}
						//play8oy
						else if (GameID == "IG")
						{
							var record = db.GameMappingUATs.Where(x =>
								x.Game.ToUpper().Trim() == GameID &&
								x.PlayerID.ToUpper() == UPlayerID &&
								x.Currency.Trim().ToUpper() == UCurrency &&
								x.IsActive == true
							).FirstOrDefault();

							if (record == null)
							{
								gamePlayerID = FormGamePlayerID(GameID, PlayerID);
								apiurl = string.Format(ConfigurationManager.AppSettings["IGApiUrl"] + "/Register?UID={0}&IP={1}&mcUID={2}", gamePlayerID, GetIPAddress(), UPlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (Convert.ToString(result.Code) == "0")
									{
										res.PlayerID = UPlayerID;
										res.GamePlayerID = gamePlayerID;
										res.Game = GameID;
										res.Message = "Success";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = UPlayerID;
										res.GamePlayerID = null;
										res.Game = GameID;
										res.Message = result.Message;
										//res.Message = "Create account api error.";
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.GamePlayerID = record.GamePlayerID;
								res.Game = GameID;
								res.Message = "Retrieve Success";
								res.Code = 1;
							}
						}
						//28win
						else if (GameID == "BT28WIN")
						{
							var record = db.GameMappingUATs.Where(x =>
								x.Game.ToUpper().Trim() == GameID &&
								x.PlayerID.ToUpper() == UPlayerID &&
								x.Currency.Trim().ToUpper() == UCurrency &&
								x.IsActive == true
							).FirstOrDefault();

							if (record == null)
							{
								gamePlayerID = FormGamePlayerID(GameID, PlayerID);
								apiurl = string.Format(ConfigurationManager.AppSettings["BT28WINApiUrl"] + "/CreatePlayer?loginID={0}&loginPass={1}&fullName={2}&mcUID={3}", gamePlayerID, "123456789", UPlayerID, UPlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;

								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (Convert.ToString(result.ErrorCode) == "0")
									{
										res.PlayerID = UPlayerID;
										res.GamePlayerID = gamePlayerID;
										res.Game = GameID;
										res.Message = "Success";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = UPlayerID;
										res.GamePlayerID = null;
										res.Game = GameID;
										//res.Message = "Create account api error.";
										res.Message = result.ErrorDesc;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.GamePlayerID = record.GamePlayerID;
								res.Game = GameID;
								res.Message = "Retrieve Success";
								res.Code = 1;
							}

						}
						//allcity
						else if (GameID == "ALLCITY")
						{
							var record = db.GameMappingUATs.Where(x =>
								x.Game.ToUpper().Trim() == GameID &&
								x.PlayerID.ToUpper() == UPlayerID &&
								x.Currency.Trim().ToUpper() == UCurrency &&
								x.IsActive == true
							).FirstOrDefault();

							if (record == null)
							{
								gamePlayerID = FormGamePlayerID(GameID, PlayerID);
								apiurl = string.Format(ConfigurationManager.AppSettings["ALLCITYApiUrl"] + "/AddUser?account={0}&mcUID={1}", gamePlayerID, UPlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.code == 200)
									{
										res.PlayerID = UPlayerID;
										res.GamePlayerID = gamePlayerID;
										res.Game = GameID;
										res.Message = "Success";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = UPlayerID;
										res.GamePlayerID = null;
										res.Game = GameID;
										//res.Message = "Create account api error.";
										res.Message = result.msg;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.GamePlayerID = record.GamePlayerID;
								res.Game = GameID;
								res.Message = "Retrieve Success";
								res.Code = 1;
							}
						}
						//ibc
						else if (GameID == "IBC")
						{
							var record = db.GameMappingUATs.Where(x =>
								x.Game.ToUpper().Trim() == GameID &&
								x.PlayerID.ToUpper() == UPlayerID &&
								x.Currency.Trim().ToUpper() == UCurrency &&
								x.IsActive == true
							).FirstOrDefault();

							if (record == null)
							{
								gamePlayerID = FormGamePlayerID(GameID, PlayerID);
								apiurl = string.Format(ConfigurationManager.AppSettings["IBCApiUrl"] + "/CreateMember?username={0}&mcUID={1}", gamePlayerID, UPlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.error_code == 0)
									{
										res.PlayerID = UPlayerID;
										res.GamePlayerID = gamePlayerID;
										res.Game = GameID;
										res.Message = "Success";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = UPlayerID;
										res.GamePlayerID = null;
										res.Game = GameID;
										//res.Message = "Create account api error.";
										res.Message = result.message;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.GamePlayerID = record.GamePlayerID;
								res.Game = GameID;
								res.Message = "Retrieve Success";
								res.Code = 1;
							}
						}
						//evo888
						else if (GameID == "EVO888")
						{
							var record = db.GameMappingUATs.Where(x =>
								x.Game.ToUpper().Trim() == GameID &&
								x.PlayerID.ToUpper() == UPlayerID &&
								x.Currency.Trim().ToUpper() == UCurrency &&
								x.IsActive == true
							).FirstOrDefault();

							if (record == null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["EVO888ApiUrl"] + "/AddUser?mcUID={0}", UPlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.code == 0 && Convert.ToString(result.success).Trim().ToUpper() == "TRUE" && result.username != null)
									{
										res.PlayerID = UPlayerID;
										res.GamePlayerID = Convert.ToString(result.username);
										res.Game = GameID;
										res.Message = "Success";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = UPlayerID;
										res.GamePlayerID = null;
										res.Game = GameID;
										//res.Message = "Create account api error.";
										res.Message = result.msg;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.GamePlayerID = record.GamePlayerID;
								res.Game = GameID;
								res.Message = "Retrieve Success";
								res.Code = 1;
							}
						}
						//vpower
						else if (GameID == "VPOWER")
						{
							var record = db.GameMappingUATs.Where(x =>
								x.Game.ToUpper().Trim() == GameID &&
								x.PlayerID.ToUpper() == UPlayerID &&
								x.Currency.Trim().ToUpper() == UCurrency &&
								x.IsActive == true
							).FirstOrDefault();

							if (record == null)
							{
								gamePlayerID = FormGamePlayerID(GameID, PlayerID);
								apiurl = string.Format(ConfigurationManager.AppSettings["VPOWERApiUrl"] + "/AddUser?username={0}&mcUID={1}", gamePlayerID, UPlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.ErrorCode == 1 || result.ErrorCode == 0)
									{
										res.PlayerID = UPlayerID;
										res.GamePlayerID = Convert.ToString(result.Username);
										res.Game = GameID;
										res.Message = "Success";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = UPlayerID;
										res.GamePlayerID = null;
										res.Game = GameID;
										//res.Message = "Create account api error.";
										res.Message = GetVPowerErrorMsg(result.ErrorCode);
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.GamePlayerID = record.GamePlayerID;
								res.Game = GameID;
								res.Message = "Retrieve Success";
								res.Code = 1;
							}
						}
						//sexygaming
						else if (GameID == "SEXYGAMING")
						{
							var record = db.GameMappingUATs.Where(x =>
								x.Game.ToUpper().Trim() == GameID &&
								x.PlayerID.ToUpper() == UPlayerID &&
								x.Currency.Trim().ToUpper() == UCurrency &&
								x.IsActive == true
							).FirstOrDefault();

							if (record == null)
							{
								gamePlayerID = FormGamePlayerID(GameID, PlayerID);
								apiurl = string.Format(ConfigurationManager.AppSettings["SEXYGAMINGApiUrl"] + "/CreatePlayer?username={0}&mcUID={1}",
																				gamePlayerID, UPlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.error == 0 && Convert.ToString(result.message).Trim().ToUpper() == "SUCCESS")
									{
										res.PlayerID = UPlayerID;
										res.GamePlayerID = gamePlayerID;
										res.Game = GameID;
										res.Message = "Success";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = UPlayerID;
										res.GamePlayerID = null;
										res.Game = GameID;
										//res.Message = "Create account api error.";
										res.Message = result.message;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.GamePlayerID = record.GamePlayerID;
								res.Game = GameID;
								res.Message = "Retrieve Success";
								res.Code = 1;
							}
						}
						//spadegaming
						else if (GameID == "SPADEGAMING")
						{
							var record = db.GameMappingUATs.Where(x =>
								x.Game.ToUpper().Trim() == GameID &&
								x.PlayerID.ToUpper() == UPlayerID &&
								x.Currency.Trim().ToUpper() == UCurrency &&
								x.IsActive == true
							).FirstOrDefault();

							if (record == null)
							{
								gamePlayerID = FormGamePlayerID(GameID, PlayerID);
								apiurl = string.Format(ConfigurationManager.AppSettings["SPADEGAMINGApiUrl"] + "/CreatePlayer?username={0}&mcUID={1}",
																				gamePlayerID, UPlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.error == 0 && Convert.ToString(result.message).Trim().ToUpper() == "SUCCESS")
									{
										res.PlayerID = UPlayerID;
										res.GamePlayerID = gamePlayerID;
										res.Game = GameID;
										res.Message = "Success";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = UPlayerID;
										res.GamePlayerID = null;
										res.Game = GameID;
										//res.Message = "Create account api error.";
										res.Message = result.message;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.GamePlayerID = record.GamePlayerID;
								res.Game = GameID;
								res.Message = "Retrieve Success";
								res.Code = 1;
							}
						}
						//mega
						else if (GameID == "MEGA")
						{
							var record = db.GameMappingUATs.Where(x =>
								x.Game.ToUpper().Trim() == GameID &&
								x.PlayerID.ToUpper() == UPlayerID &&
								x.Currency.Trim().ToUpper() == UCurrency &&
								x.IsActive == true
							).FirstOrDefault();

							if (record == null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["MEGAApiUrl"] + "/CreateUser?mcUID={0}", UPlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.error == null)
									{
										if (result.result.success == true)
										{
											res.PlayerID = UPlayerID;
											res.GamePlayerID = Convert.ToString(result.result.loginId);
											res.Game = GameID;
											res.Message = "Success";
											res.Code = 0;
										}
										else
										{
											res.PlayerID = UPlayerID;
											res.GamePlayerID = null;
											res.Game = GameID;
											//res.Message = "Create account api error.";
											res.Message = result.error.message;
											res.Code = 2;
										}
									}
									else
									{
										res.PlayerID = UPlayerID;
										res.GamePlayerID = null;
										res.Game = GameID;
										//res.Message = "Create account api error.";
										res.Message = result.error.message;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.GamePlayerID = record.GamePlayerID;
								res.Game = GameID;
								res.Message = "Retrieve Success";
								res.Code = 1;
							}
						}
						//evolution
						else if (GameID == "EVOLUTIONGAMING")
						{
							var record = db.GameMappingUATs.Where(x =>
								x.Game.ToUpper().Trim() == GameID &&
								x.PlayerID.ToUpper() == UPlayerID &&
								x.Currency.Trim().ToUpper() == UCurrency &&
								x.IsActive == true
							).FirstOrDefault();

							if (record == null)
							{
								gamePlayerID = FormGamePlayerID(GameID, PlayerID);
								apiurl = string.Format(ConfigurationManager.AppSettings["EVOLUTIONGAMINGApiUrl"] + "/UserAuth?username={0}&mcUID={1}", gamePlayerID, UPlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.entry != null)
									{
										res.PlayerID = UPlayerID;
										res.GamePlayerID = gamePlayerID;
										res.Game = GameID;
										res.Message = "Success";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = UPlayerID;
										res.GamePlayerID = null;
										res.Game = GameID;
										//res.Message = "Create account api error.";
										res.Message = result.errors.message;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.GamePlayerID = record.GamePlayerID;
								res.Game = GameID;
								res.Message = "Retrieve Success";
								res.Code = 1;
							}
						}
						//menang88
						else if (GameID == "MENANG88")
						{
							var record = db.GameMappingUATs.Where(x =>
								x.Game.ToUpper().Trim() == GameID &&
								x.PlayerID.ToUpper() == UPlayerID &&
								x.Currency.Trim().ToUpper() == UCurrency &&
								x.IsActive == true
							).FirstOrDefault();

							if (record == null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["MENANG88ApiUrl"] + "/CreatePlayer?mcUID={0}", UPlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (Convert.ToString(result.status) == "1")
									{
										res.PlayerID = UPlayerID;
										res.GamePlayerID = Convert.ToString(result.userid);
										res.Game = GameID;
										res.Message = "Success";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = UPlayerID;
										res.GamePlayerID = null;
										res.Game = GameID;
										//res.Message = "Create account api error.";
										res.Message = result.message;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.GamePlayerID = record.GamePlayerID;
								res.Game = GameID;
								res.Message = "Retrieve Success";
								res.Code = 1;
							}
						}
						//xe88
						else if (GameID == "XE88")
						{
							var record = db.GameMappingUATs.Where(x =>
								x.Game.ToUpper().Trim() == GameID &&
								x.PlayerID.ToUpper() == UPlayerID &&
								x.Currency.Trim().ToUpper() == UCurrency &&
								x.IsActive == true
							).FirstOrDefault();

							if (record == null)
							{
								gamePlayerID = FormGamePlayerID(GameID, PlayerID);
								apiurl = string.Format(ConfigurationManager.AppSettings["XE88ApiUrl"] + "/CreatePlayer?username={0}&mcUID={1}", gamePlayerID, UPlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (Convert.ToString(result.code) == "0")
									{
										res.PlayerID = UPlayerID;
										res.GamePlayerID = gamePlayerID;
										res.Game = GameID;
										res.Message = "Success";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = UPlayerID;
										res.GamePlayerID = null;
										res.Game = GameID;
										//res.Message = "Create account api error.";
										res.Message = result.message;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.GamePlayerID = record.GamePlayerID;
								res.Game = GameID;
								res.Message = "Retrieve Success";
								res.Code = 1;
							}
						}
						//kypoker
						else if (GameID == "KYPOKER")
						{
							var record = db.GameMappingUATs.Where(x =>
								x.Game.ToUpper().Trim() == GameID &&
								x.PlayerID.ToUpper() == UPlayerID &&
								x.Currency.Trim().ToUpper() == UCurrency &&
								x.IsActive == true
							).FirstOrDefault();

							if (record == null)
							{
								gamePlayerID = FormGamePlayerID(GameID, PlayerID);
								apiurl = string.Format(ConfigurationManager.AppSettings["KYPOKERApiUrl"] + "/CreatePlayer?username={0}&mcUID={1}", gamePlayerID, UPlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.d != null)
									{
										if (Convert.ToString(result.d.code) == "0")
										{
											res.PlayerID = UPlayerID;
											res.GamePlayerID = gamePlayerID;
											res.Game = GameID;
											res.Message = "Success";
											res.Code = 0;
										}
										else
										{
											res.PlayerID = UPlayerID;
											res.GamePlayerID = null;
											res.Game = GameID;
											//res.Message = "Create account api error.";
											res.Message = result.d.code;
											res.Code = 2;
										}
									}
									else
									{
										res.PlayerID = UPlayerID;
										res.GamePlayerID = null;
										res.Game = GameID;
										res.Message = "Create account api error.";
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.GamePlayerID = record.GamePlayerID;
								res.Game = GameID;
								res.Message = "Retrieve Success";
								res.Code = 1;
							}
						}
						//sabaesports
						else if (GameID == "SABAESPORTS")
						{
							var record = db.GameMappingUATs.Where(x =>
								x.Game.ToUpper().Trim() == GameID &&
								x.PlayerID.ToUpper() == UPlayerID &&
								x.Currency.Trim().ToUpper() == UCurrency &&
								x.IsActive == true
							).FirstOrDefault();

							if (record == null)
							{
								gamePlayerID = FormGamePlayerID(GameID, PlayerID);
								apiurl = string.Format(ConfigurationManager.AppSettings["SABAESPORTSApiUrl"] + "/CreateMember?username={0}&mcUID={1}", gamePlayerID, UPlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.error_code == 0)
									{
										res.PlayerID = UPlayerID;
										res.GamePlayerID = gamePlayerID;
										res.Game = GameID;
										res.Message = "Success";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = UPlayerID;
										res.GamePlayerID = null;
										res.Game = GameID;
										//res.Message = "Create account api error.";
										res.Message = result.message;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.GamePlayerID = record.GamePlayerID;
								res.Game = GameID;
								res.Message = "Retrieve Success";
								res.Code = 1;
							}
						}
						//tpg
						else if (GameID == "TPG")
						{
							var record = db.GameMappingUATs.Where(x =>
								x.Game.ToUpper().Trim() == GameID &&
								x.PlayerID.ToUpper() == UPlayerID &&
								x.Currency.Trim().ToUpper() == UCurrency &&
								x.IsActive == true
							).FirstOrDefault();

							if (record == null)
							{
								gamePlayerID = FormGamePlayerID(GameID, PlayerID);
								apiurl = string.Format(ConfigurationManager.AppSettings["TPGApiUrl"] + "/CreatePlayer?playerName={0}&displayName={1}&currency={2}&mcUID={3}",
																				gamePlayerID, UPlayerID, UCurrency, UPlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.status == 1)
									{
										res.PlayerID = UPlayerID;
										res.GamePlayerID = gamePlayerID;
										res.Game = GameID;
										res.Message = "Success";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = UPlayerID;
										res.GamePlayerID = null;
										res.Game = GameID;
										res.Message = result.message;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.GamePlayerID = record.GamePlayerID;
								res.Game = GameID;
								res.Message = "Retrieve Success";
								res.Code = 1;
							}
						}
						//tpgfish
						else if (GameID == "TPGFISH")
						{
							var record = db.GameMappingUATs.Where(x =>
								x.Game.ToUpper().Trim() == GameID &&
								x.PlayerID.ToUpper() == UPlayerID &&
								x.Currency.Trim().ToUpper() == UCurrency &&
								x.IsActive == true
							).FirstOrDefault();

							if (record == null)
							{
								gamePlayerID = FormGamePlayerID(GameID, PlayerID);
								apiurl = string.Format(ConfigurationManager.AppSettings["TPGFISHApiUrl"] + "/CreatePlayer?playerName={0}&displayName={1}&currency={2}&mcUID={3}",
																				gamePlayerID, UPlayerID, UCurrency, UPlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.status == 1)
									{
										res.PlayerID = UPlayerID;
										res.GamePlayerID = gamePlayerID;
										res.Game = GameID;
										res.Message = "Success";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = UPlayerID;
										res.GamePlayerID = null;
										res.Game = GameID;
										res.Message = result.message;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.GamePlayerID = record.GamePlayerID;
								res.Game = GameID;
								res.Message = "Retrieve Success";
								res.Code = 1;
							}
						}
						//vgaming
						else if (GameID == "VGAMING")
						{
							var record = db.GameMappingUATs.Where(x =>
								x.Game.ToUpper().Trim() == GameID &&
								x.PlayerID.ToUpper() == UPlayerID &&
								x.Currency.Trim().ToUpper() == UCurrency &&
								x.IsActive == true
							).FirstOrDefault();

							if (record == null)
							{
								gamePlayerID = FormGamePlayerID(GameID, PlayerID);
								apiurl = string.Format(ConfigurationManager.AppSettings["VGAMINGApiUrl"] + "/CreateMember?username={0}&mcUID={1}", gamePlayerID, UPlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.error_code == 0)
									{
										res.PlayerID = UPlayerID;
										res.GamePlayerID = gamePlayerID;
										res.Game = GameID;
										res.Message = "Success";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = UPlayerID;
										res.GamePlayerID = null;
										res.Game = GameID;
										//res.Message = "Create account api error.";
										res.Message = result.message;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.GamePlayerID = record.GamePlayerID;
								res.Game = GameID;
								res.Message = "Retrieve Success";
								res.Code = 1;
							}
						}
						//cq9
						else if (GameID == "CQ9")
						{
							var record = db.GameMappingUATs.Where(x =>
								x.Game.ToUpper().Trim() == GameID &&
								x.PlayerID.ToUpper() == UPlayerID &&
								x.Currency.Trim().ToUpper() == UCurrency &&
								x.IsActive == true
							).FirstOrDefault();

							if (record == null)
							{
								gamePlayerID = FormGamePlayerID(GameID, PlayerID);
								apiurl = string.Format(ConfigurationManager.AppSettings["CQ9ApiUrl"] + "/CreatePlayer?username={0}&mcUID={1}", gamePlayerID, UPlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.status != null)
									{
										if (Convert.ToString(result.status.code) == "0")
										{
											res.PlayerID = UPlayerID;
											res.GamePlayerID = gamePlayerID;
											res.Game = GameID;
											res.Message = "Success";
											res.Code = 0;
										}
										else
										{
											res.PlayerID = UPlayerID;
											res.GamePlayerID = null;
											res.Game = GameID;
											res.Message = result.status.message;
											res.Code = 2;
										}
									}
									else
									{
										res.PlayerID = UPlayerID;
										res.GamePlayerID = null;
										res.Game = GameID;
										res.Message = "Unknown error";
										res.Code = 3;
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.GamePlayerID = record.GamePlayerID;
								res.Game = GameID;
								res.Message = "Retrieve Success";
								res.Code = 1;
							}
						}
						//default
						else
						{
							res.PlayerID = UPlayerID;
							res.GamePlayerID = null;
							res.Game = GameID;
							res.Message = "Invalid game.";
							res.Code = 8;
						}

						_common.InsertLog("CheckAndCreateAccount", "TrackerId", TrackerId, "Response", JsonConvert.SerializeObject(res), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "GamePlayerID", gamePlayerID);

						return new HttpResponseMessage()
						{
							StatusCode = HttpStatusCode.OK,
							Content = new StringContent(JsonConvert.SerializeObject(res), System.Text.Encoding.UTF8, "application/json")
						};
					}
				}
				catch (Exception ex)
				{
					_common.LogError($"{TrackerId}:CheckAndCreateAccount:EXCEPTION " + string.Format("{0}|{1}|{2}", Game, PlayerID, Currency) + " " + ex.Message.ToString(), FilePrefix);
					_common.InsertLog("CheckAndCreateAccount", "TrackerId", TrackerId, "Exception", ex.Message.ToString(), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "GamePlayerID", gamePlayerID);
					dynamic res = new ExpandoObject();
					res.Code = -9999;
					res.Message = ex.Message.ToString();
					return new HttpResponseMessage()
					{
						StatusCode = HttpStatusCode.InternalServerError,
						Content = new StringContent(JsonConvert.SerializeObject(res), System.Text.Encoding.UTF8, "application/json")
					};
				}
			}
		}

		public decimal? GetGameBalance(string Game, string PlayerID, string Currency, string TrackerId)
		{
			lock (MyLock)
			{
				string apiurl = "";
				int TimeoutSec = 180000; //3 mins
				Common _common = new Common();

				try
				{
					using (Bingo_stagingConn db = new Bingo_stagingConn())
					{
						var res = new GetBalanceVM();
						string GameID = Game.ToUpper().Trim();
						string UPlayerID = PlayerID.ToUpper().Trim();
						string UCurrency = Currency.ToUpper().Trim();

						//bg
						if (GameID == "BG")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["BGApiUrl"] + "/GetBalance?UID={0}&IP={1}", record.GamePlayerID, GetIPAddress());
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.error == null)
										return Convert.ToDecimal(result.result);
									else
										return null;
								}
							}
							else
								return null;
						}
						//ag
						else if (GameID == "AG")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["AGApiUrl"] + "/GetBalance?loginname={0}&currency={1}", record.GamePlayerID, Currency);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (string.IsNullOrWhiteSpace(Convert.ToString(result.ErrorDesc)))
										return Convert.ToDecimal(result.ErrorCode);
									else
										return null;
								}
							}
							else
								return null;
						}
						//playtech
						else if (GameID == "PT")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["PTApiUrl"] + "/GetPlayerBalance?playerName={0}", record.GamePlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.error == null)
										return Convert.ToDecimal(result.result.balance);
									else
										return null;
								}
							}
							else
								return null;
						}
						//play8oy
						else if (GameID == "IG")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["IGApiUrl"] + "/GetBalance?UID={0}&IP={1}", record.GamePlayerID, GetIPAddress());
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (Convert.ToString(result.Code) == "0")
										return Convert.ToDecimal(result.Data.Balance);
									else
										return null;

								}
							}
							else
								return null;
						}
						//28win
						else if (GameID == "BT28WIN")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["BT28WINApiUrl"] + "/GetProfile?loginID={0}", record.GamePlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;

								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (Convert.ToString(result.ErrorCode) == "0")
										return Convert.ToDecimal(result.Data.balance);
									else
										return null;
								}
							}
							else
								return null;

						}
						//allcity
						else if (GameID == "ALLCITY")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["ALLCITYApiUrl"] + "/GetUserInfo?account={0}", record.GamePlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.code == 200)
										return Convert.ToDecimal(result.pending) + Convert.ToDecimal(result.balance);
									else
										return null;
								}
							}
							else
								return null;
						}
						//ibc
						else if (GameID == "IBC")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["IBCApiUrl"] + "/CheckUserBalance?username={0}", record.GamePlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (Convert.ToString(result.error_code) == "0" && result.Data != null)
									{
										if (result.Data.Count > 0)
										{
											if (result.Data[0].error_code == 0)
												return Convert.ToDecimal(result.Data[0].balance);
											else if (result.Data[0].error_code == 6) //player never transfer
												return 0;
											else
												return null;
										}
										else
											return null;
									}
									else
										return null;
								}
							}
							else
								return null;
						}
						//evo888
						else if (GameID == "EVO888")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["EVO888ApiUrl"] + "/GetUserInfo?username={0}", record.GamePlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (Convert.ToString(result.code) == "0" && Convert.ToString(result.success).Trim().ToUpper() == "TRUE" && result.results != null)
										return Convert.ToDecimal(result.results.balance);
									else
										return null;
								}
							}
							else
								return null;
						}
						//vpower
						else if (GameID == "VPOWER")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["VPOWERApiUrl"] + "/GetBalance?username={0}", record.GamePlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (Convert.ToString(result.ErrorCode) == "0")
										return Convert.ToDecimal(result.Credit);
									else
										return null;
								}
							}
							else
								return null;
						}
						//sexygaming
						else if (GameID == "SEXYGAMING")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["SEXYGAMINGApiUrl"] + "/GetBalance?username={0}", record.GamePlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (Convert.ToString(result.error) == "0" && Convert.ToString(result.message).Trim().ToUpper() == "SUCCESS" && result.data != null)
										return Convert.ToDecimal(result.data.Balance);
									else
										return null;
								}
							}
							else
								return null;
						}
						//spadegaming
						else if (GameID == "SPADEGAMING")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["SPADEGAMINGApiUrl"] + "/GetBalance?username={0}", record.GamePlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (Convert.ToString(result.code) == "0" && Convert.ToString(result.msg).Trim().ToUpper() == "SUCCESS" && result.list != null)
									{
										if (result.list.Count > 0)
											return Convert.ToDecimal(result.list[0].balance);
										else
											return null;
									}
									else
										return null;
								}
							}
							else
								return null;
						}
						//mega
						else if (GameID == "MEGA")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["MEGAApiUrl"] + "/GetBalance?loginId={0}", record.GamePlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.result != null)
										return Convert.ToDecimal(result.result);
									else
										return null;
								}
							}
							else
								return null;
						}
						//evolution
						else if (GameID == "EVOLUTIONGAMING")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["EVOLUTIONGAMINGApiUrl"] + "/GetBalance?username={0}", record.GamePlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.userbalance != null)
									{
										if (result.userbalance.result == "Y")
											return Convert.ToDecimal(result.userbalance.tbalance);
										else
											return null;
									}
									else
										return null;
								}
							}
							else
								return null;
						}
						//menang88
						else if (GameID == "MENANG88")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["MENANG88ApiUrl"] + "/GetBalance?userId={0}", record.GamePlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (Convert.ToString(result.status) == "1")
										return Convert.ToDecimal(result.creditbalance);
									else
										return null;
								}
							}
							else
								return null;
						}
						//xe88
						else if (GameID == "XE88")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["XE88ApiUrl"] + "/GetBalance?username={0}", record.GamePlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (Convert.ToString(result.code) == "0")
										return Convert.ToDecimal(result.result.balance);
									else
										return null;
								}
							}
							else
								return null;
						}
						//kypoker
						else if (GameID == "KYPOKER")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["KYPOKERApiUrl"] + "/GetBalance?username={0}", record.GamePlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.d != null)
									{
										if (Convert.ToString(result.d.code) == "0")
											return Convert.ToDecimal(result.d.freeMoney);
										else
											return null;
									}
									else
										return null;
								}
							}
							else
								return null;
						}
						//sabaesports
						else if (GameID == "SABAESPORTS")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["SABAESPORTSApiUrl"] + "/CheckUserBalance?username={0}", record.GamePlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (Convert.ToString(result.error_code) == "0" && result.Data != null)
									{
										if (result.Data.Count > 0)
										{
											if (result.Data[0].error_code == 0)
												return Convert.ToDecimal(result.Data[0].balance);
											else if (result.Data[0].error_code == 6) //player never transfer
												return 0;
											else
												return null;
										}
										else
											return null;
									}
									else
										return null;
								}
							}
							else
								return null;
						}
						//tpg
						else if (GameID == "TPG")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["TPGApiUrl"] + "/GetPlayerGameBalance?playerName={0}&displayName={1}&currency={2}&mcUID={3}",
																				record.GamePlayerID, UPlayerID, UCurrency, UPlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.status == 1)
										return Convert.ToDecimal(result.totalBalance);
									else
										return null;
								}
							}
							else
								return null;
						}
						//tpgfish
						else if (GameID == "TPGFISH")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["TPGFISHApiUrl"] + "/GetPlayerGameBalance?playerName={0}&displayName={1}&currency={2}&mcUID={3}",
																				record.GamePlayerID, UPlayerID, UCurrency, UPlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.status == 1)
										return Convert.ToDecimal(result.totalBalance);
									else
										return null;
								}
							}
							else
								return null;
						}
						//vgaming
						else if (GameID == "VGAMING")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["VGAMINGApiUrl"] + "/CheckUserBalance?username={0}", record.GamePlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (Convert.ToString(result.error_code) == "0" && result.Data != null)
									{
										if (result.Data.Count > 0)
										{
											if (result.Data[0].error_code == 0)
												return Convert.ToDecimal(result.Data[0].balance);
											else if (result.Data[0].error_code == 6) //player never transfer
												return 0;
											else
												return null;
										}
										else
											return null;
									}
									else
										return null;
								}
							}
							else
								return null;
						}
						//cq9
						else if (GameID == "CQ9")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["CQ9ApiUrl"] + "/GetBalance?username={0}", record.GamePlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.status != null)
									{
										if (Convert.ToString(result.status.code) == "0")
										{
											if (result.data != null)
												return Convert.ToDecimal(result.data.balance);
											else
												return null;
										}
										else
											return null;
									}
									else
										return null;
								}
							}
							else
								return null;
						}

						return null;
					}
				}
				catch (Exception ex)
				{
					_common.LogError($"{TrackerId}:GetGameBalance:EXCEPTION " + string.Format("{0}|{1}|{2}", Game, PlayerID, Currency) + " " + ex.Message.ToString(), FilePrefix);
					return null;
				}
			}
		}

		[HttpGet]
		public HttpResponseMessage Withdraw(string Game, string PlayerID, string Currency, decimal Amount, string TrackerId) //Currency need to get from bingotown Game table and pass in here (by myself)
		{
			lock (MyLock)
			{
				string apiurl = "";
				Common _common = new Common();
				int TimeoutSec = 180000; //3 mins
				string msg = string.Empty;
				decimal? WMinAmount = null;
				decimal? WMaxAmount = null;
				bool? LockFromGame = false;

				try
				{
					_common.InsertLog("Withdraw", "TrackerId", TrackerId, "Request", "", "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Amount", Amount.ToString(), "WMinAmount", WMinAmount, "WMaxAmount", WMaxAmount);

					using (Bingo_stagingConn db = new Bingo_stagingConn())
					{
						var res = new WithdrawVM();
						string GameID = Game.ToUpper().Trim();
						string UPlayerID = PlayerID.ToUpper().Trim();
						string UCurrency = Currency.ToUpper().Trim();

						if (Amount > 0)
						{
							GetGameTransferLimit(GameID, "WITHDRAW", UCurrency, TrackerId, ref WMinAmount, ref WMaxAmount);

							//unable to get withdraw transfer limit
							if (WMinAmount.Equals(null) || WMaxAmount.Equals(null))
							{
								res.PlayerID = UPlayerID;
								res.Game = GameID;
								res.Message = "Failed to get withdraw transfer limit.";
								res.Code = 7;
								res.LockFromGame = false;
								_common.LogError($"{TrackerId}:Withdraw:FAILED - Failed To Get Withdraw Transfer Limit " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Currency, Amount.ToString()), FilePrefix);
								_common.InsertLog("Withdraw", "TrackerId", TrackerId, "Response", JsonConvert.SerializeObject(res), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Amount", Amount.ToString(), "WMinAmount", WMinAmount, "WMaxAmount", WMaxAmount);
								return new HttpResponseMessage()
								{
									StatusCode = HttpStatusCode.OK,
									Content = new StringContent(JsonConvert.SerializeObject(res), System.Text.Encoding.UTF8, "application/json")
								};
							}

							//less than min amount, skip withdraw process
							if (WMinAmount > Amount)
							{
								res.PlayerID = UPlayerID;
								res.Game = GameID;
								res.Message = "Successfully quit game.";
								res.Code = 0;
								res.Amount = 0;
								_common.LogError($"{TrackerId}:Withdraw:SUCCESS - Withdraw Less Than Min Limit, Skip Withdraw " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Currency, Amount.ToString()), FilePrefix);
							}
							//more than max amount, break into few steps
							else if (Amount > WMaxAmount)
							{
								_common.LogError($"{TrackerId}:Withdraw:SUCCESS - Withdraw More Than Max Limit, Amount To Be Withdraw: {Amount.ToString()}" + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Currency, Amount.ToString()), FilePrefix);
								decimal withdrawAmt = 0;
								decimal remainAmt = Convert.ToDecimal(Amount);
								decimal amt = 0;
								decimal? failedGameBalance = null;

								while (remainAmt != 0)
								{
									amt = remainAmt > WMaxAmount ? (decimal)WMaxAmount : remainAmt;
									bool isSuccess = GameWithdraw(GameID, UPlayerID, UCurrency, (decimal)amt, TrackerId, ref LockFromGame);
									_common.LogError($"{TrackerId}:Withdraw:" + (isSuccess == true ? "SUCCESS" : "FAILED") + $" - Current Withdraw Amount: {amt.ToString()} " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Currency, Amount.ToString()), FilePrefix);
									if (isSuccess == false)
									{
										failedGameBalance = GetGameBalance(GameID, UPlayerID, UCurrency, TrackerId);
										break;
									}

									remainAmt -= amt;
									withdrawAmt += amt;

									_common.LogError($"{TrackerId}:Withdraw:SUCCESS - Remaining Amount: {remainAmt.ToString()}, Already Withdraw Amount: {withdrawAmt.ToString()} " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Currency, Amount.ToString()), FilePrefix);
								}

								if (!failedGameBalance.Equals(null))
								{
									_common.LogError($"{TrackerId}:Withdraw:FAILED - End Balance: {failedGameBalance.ToString()} " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Currency, Amount.ToString()), FilePrefix);
									res.PlayerID = UPlayerID;
									res.Game = GameID;
									res.Message = "Withdraw failed, please contact customer service.";
									res.Code = 3;
									res.Amount = Amount;
									res.LockFromGame = false; //no need lock first since failed response return will not credit back money
									_common.LogError($"{TrackerId}:Withdraw:FAILED - Withdraw Failed, Return To App " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Currency, Amount.ToString()), FilePrefix);
									msg = $"Withdraw:FAILED - {UPlayerID} failed to withdraw {Amount.ToString()} from {GameID}.";
									foreach (var phone in apiPhoneList.Split(','))
									{
										_common.SendSmsToBulkSms(phone, msg);
									}
								}
								else
								{
									_common.LogError($"{TrackerId}:Withdraw:SUCCESS - Total Amount: {Amount.ToString()} " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Currency, Amount.ToString()), FilePrefix);
									res.PlayerID = UPlayerID;
									res.Game = GameID;
									res.Message = "Successfully withdraw.";
									res.Code = 0;
									res.Amount = Amount;
								}
							}
							//normal condition
							else
							{
								_common.LogError($"{TrackerId}:Withdraw:SUCCESS - Withdraw Normal Condition " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Currency, Amount.ToString()), FilePrefix);
								bool isSuccess = GameWithdraw(GameID, UPlayerID, UCurrency, Amount, TrackerId, ref LockFromGame);
								if (isSuccess == false)
								{
									res.PlayerID = UPlayerID;
									res.Game = GameID;
									res.Message = "Withdraw failed, please contact customer service.";
									res.Code = 3;
									res.Amount = Amount;
									res.LockFromGame = false; //no need lock first since failed response return will not credit back money
									_common.LogError($"{TrackerId}:Withdraw:FAILED - Withdraw Failed, Return To App " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Currency, Amount.ToString()), FilePrefix);
									msg = $"Withdraw:FAILED - {UPlayerID} failed to withdraw {Amount.ToString()} from {GameID}.";
									foreach (var phone in apiPhoneList.Split(','))
									{
										_common.SendSmsToBulkSms(phone, msg);
									}
								}
								else
								{
									res.PlayerID = UPlayerID;
									res.Game = GameID;
									res.Message = "Successfully withdraw.";
									res.Code = 0;
									res.Amount = Amount;
									_common.LogError($"{TrackerId}:Withdraw:SUCCESS - Total Amount: {Amount.ToString()} " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Currency, Amount.ToString()), FilePrefix);
								}
							}
						}
						else
						{
							res.PlayerID = UPlayerID;
							res.Game = GameID;
							res.Message = "Player has negative balance.";
							res.Code = 4;
							res.Amount = Amount;
							res.LockFromGame = true;
							_common.LogError($"{TrackerId}:Withdraw:FAILED - Player Has Negative Balance " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Currency, Amount.ToString()) + " " + JsonConvert.SerializeObject(res), FilePrefix);
							msg = $"Withdraw:FAILED - {UPlayerID} has negative balance for {GameID}.";
							foreach (var phone in apiPhoneList.Split(','))
							{
								_common.SendSmsToBulkSms(phone, msg);
							}
						}

						_common.InsertLog("Withdraw", "TrackerId", TrackerId, "Response", JsonConvert.SerializeObject(res), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Amount", Amount.ToString(), "WMinAmount", WMinAmount, "WMaxAmount", WMaxAmount);

						return new HttpResponseMessage()
						{
							StatusCode = HttpStatusCode.OK,
							Content = new StringContent(JsonConvert.SerializeObject(res), System.Text.Encoding.UTF8, "application/json")
						};
					}
				}
				catch (Exception ex)
				{
					_common.LogError($"{TrackerId}:Withdraw:EXCEPTION " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Currency, Amount.ToString()) + " " + ex.Message.ToString(), FilePrefix);
					_common.InsertLog("Withdraw", "TrackerId", TrackerId, "Exception", ex.Message.ToString(), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Amount", Amount.ToString(), "WMinAmount", WMinAmount, "WMaxAmount", WMaxAmount);
					dynamic result = new ExpandoObject();
					result.Code = -9999;
					result.Message = "System error.";
					return new HttpResponseMessage()
					{
						StatusCode = HttpStatusCode.InternalServerError,
						Content = new StringContent(JsonConvert.SerializeObject(result), System.Text.Encoding.UTF8, "application/json")
					};
				}
			}
		}

		public bool GameDeposit(string Game, string PlayerID, string Currency, decimal Amount, string TrackerId, ref bool? LockFromGame) //Currency need to get from bingotown Game table and pass in here (by myself)
		{
			lock (MyLock)
			{
				string apiurl = "";
				Common _common = new Common();
				int TimeoutSec = 180000; //3 mins

				try
				{
					using (Bingo_stagingConn db = new Bingo_stagingConn())
					{
						var res = new DepositVM();
						string GameID = Game.ToUpper().Trim();
						string UPlayerID = PlayerID.ToUpper().Trim();
						string UCurrency = Currency.ToUpper().Trim();

						//bg | able to return after balance | result.result | currency based on agent
						if (GameID == "BG")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["BGApiUrl"] + "/TransferBiz?UID={0}&IP={1}&Amount={2}&BizId={3}&CheckBizId=1",
																	record.GamePlayerID, GetIPAddress(), string.Format("{0:0.##}", Amount), GetGameRunningNumber(GameID));
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.error == null)
										return true;
									else
									{
										if (Convert.ToString(result.error.code) == "2474")
										{
											bool isOk = false;
											string transferText = string.Empty;
											while (!isOk)
											{
												transferText = RetryDeposit(GameID, UPlayerID, UCurrency, Amount, TrackerId);
												dynamic transferResult = JObject.Parse(transferText);

												if (transferResult.error == null)
													isOk = true;
												else if (Convert.ToString(transferResult.error.code) != "2474")
												{
													isOk = false;
													LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(transferResult.error.code));
													break;
												}
											}

											return isOk;
										}
										else
										{
											LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(result.error.code));
											return false;
										}
									}
								}
							}
							else
							{
								LockFromGame = false;
								return false;
							}
						}
						//ag | not able to return after balance | currency based on what we pass in
						else if (GameID == "AG")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["AGApiUrl"] + "/Deposit?loginname={0}&credit={1}&currency={2}", record.GamePlayerID, string.Format("{0:0.##}", Amount), Currency);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (string.IsNullOrWhiteSpace(Convert.ToString(result.ErrorDesc)))
										return true;
									else
									{
										LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(result.ErrorCode));
										return false;
									}
								}
							}
							else
							{
								LockFromGame = false;
								return false;
							}
						}
						//playtech | able to return balance | result.result.currentplayerbalance | currency define by provider
						else if (GameID == "PT")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["PTApiUrl"] + "/Deposit?playerName={0}&amount={1}", record.GamePlayerID, string.Format("{0:0.##}", Amount));
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.error == null)
										return true;
									else
									{
										LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(result.errorcode));
										return false;
									}
								}
							}
							else
							{
								LockFromGame = false;
								return false;
							}
						}
						//play8oy | not able to return after balance | currency define by provider
						else if (GameID == "IG")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["IGApiUrl"] + "/Transfer?UID={0}&Action=0&Amount={1}&IP={2}", record.GamePlayerID, string.Format("{0:0.##}", Amount), GetIPAddress());
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (Convert.ToString(result.Code) == "0")
										return true;
									else
									{
										LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(result.Code));
										return false;
									}
								}
							}
							else
							{
								LockFromGame = false;
								return false;
							}
						}
						//28win | able to return after balance but not return from own 28win api | currency define by provider
						else if (GameID == "BT28WIN")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["BT28WINApiUrl"] + "/Deposit?loginID={0}&amount={1}", record.GamePlayerID, string.Format("{0:0.##}", Amount));
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;

								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (Convert.ToString(result.ErrorCode) == "0")
										return true;
									else
									{
										LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(result.ErrorCode));
										return false;
									}
								}
							}
							else
							{
								LockFromGame = false;
								return false;
							}
						}
						//allcity | not able to return after balance | currency fix in provider side
						else if (GameID == "ALLCITY")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["ALLCITYApiUrl"] + "/Deposit?account={0}&amount={1}", record.GamePlayerID, string.Format("{0:0.##}", Amount));
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.code == 200)
										return true;
									else
									{
										LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(result.code));
										return false;
									}
								}
							}
							else
							{
								LockFromGame = false;
								return false;
							}
						}
						//ibc | able to return after balance | result.Data.after_amount | currency hardcode in our own api
						else if (GameID == "IBC")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["IBCApiUrl"] + "/DepositTrans?username={0}&amount={1}&transId={2}",
																			record.GamePlayerID, string.Format("{0:0.##}", Amount), GetGameRunningNumber(GameID));
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.error_code == 0)
									{
										if (result.Data != null)
										{
											if (result.Data.status == 0)
												return true;
											else
											{
												LockFromGame = true;
												return false;
											}
										}
										else
										{
											LockFromGame = true;
											return false;
										}
									}
									else
									{
										if (result.error_code == 5)
										{
											bool isOk = false;
											string transferText = string.Empty;
											while (!isOk)
											{
												transferText = RetryDeposit(GameID, UPlayerID, UCurrency, Amount, TrackerId);
												dynamic transferResult = JObject.Parse(transferText);

												if (transferResult.error_code == 0)
													isOk = true;
												else if (transferResult.error_code != 5)
												{
													LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(transferResult.error_code));
													isOk = false;
													break;
												}
											}

											return isOk;
										}
										else
										{
											LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(result.error_code));
											return false;
										}
									}
								}
							}
							else
							{
								LockFromGame = false;
								return false;
							}
						}
						//evo888 | not able to return after balance | able to set country in our own api
						else if (GameID == "EVO888")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["EVO888ApiUrl"] + "/Deposit?username={0}&amount={1}", record.GamePlayerID, string.Format("{0:0.##}", Amount));
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.code == 0 && Convert.ToString(result.success).Trim().ToUpper() == "TRUE")
										return true;
									else
									{
										LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(result.code));
										return false;
									}
								}
							}
							else
							{
								LockFromGame = false;
								return false;
							}
						}
						//vpower | able to return balance | result.Credit | currency fix in provider side
						else if (GameID == "VPOWER")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["VPOWERApiUrl"] + "/Deposit?username={0}&amount={1}", record.GamePlayerID, string.Format("{0:0.##}", Amount));
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (Convert.ToInt16(result.ErrorCode) == 0)
										return true;
									else
									{
										LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(result.ErrorCode));
										return false;
									}
								}
							}
							else
							{
								LockFromGame = false;
								return false;
							}
						}
						//sexygaming | able to return balance | result.data.Balance | currency fix in provider side
						else if (GameID == "SEXYGAMING")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["SEXYGAMINGApiUrl"] + "/Deposit?username={0}&amount={1}",
																				record.GamePlayerID, string.Format("{0:0.##}", Amount));
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.error == 0 && Convert.ToString(result.message).Trim().ToUpper() == "SUCCESS")
										return true;
									else
									{
										LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(result.error));
										return false;
									}
								}
							}
							else
							{
								LockFromGame = false;
								return false;
							}
						}
						//spadegaming | able to return balance | result.afterBalance | currency hardcode in our own api
						else if (GameID == "SPADEGAMING")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["SPADEGAMINGApiUrl"] + "/Deposit?username={0}&amount={1}",
																				record.GamePlayerID, string.Format("{0:0.##}", Amount));
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.code == 0 && Convert.ToString(result.msg).Trim().ToUpper() == "SUCCESS")
										return true;
									else
									{
										LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(result.code));
										return false;
									}
								}
							}
							else
							{
								LockFromGame = false;
								return false;
							}
						}
						//mega
						else if (GameID == "MEGA")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["MEGAApiUrl"] + "/Deposit?loginId={0}&amount={1}&bizId={2}&checkBizId=1",
																				record.GamePlayerID, string.Format("{0:0.##}", Amount), GetGameRunningNumber(GameID));
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.error == null)
										return true;
									else
									{
										if (Convert.ToString(result.error.code) == "37122")
										{
											bool isOk = false;
											string transferText = string.Empty;
											while (!isOk)
											{
												transferText = RetryDeposit(GameID, UPlayerID, UCurrency, Amount, TrackerId);
												dynamic transferResult = JObject.Parse(transferText);

												if (transferResult.error == null)
													isOk = true;
												else if (Convert.ToString(transferResult.error.code) != "37122")
												{
													LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(transferResult.error.code));
													isOk = false;
													break;
												}
											}

											return isOk;
										}
										else
										{
											LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(result.error.code));
											return false;
										}
									}
								}
							}
							else
							{
								LockFromGame = false;
								return false;
							}
						}
						//evolution
						else if (GameID == "EVOLUTIONGAMING")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["EVOLUTIONGAMINGApiUrl"] + "/Deposit?username={0}&amount={1}&mcUID={2}",
																				record.GamePlayerID, string.Format("{0:0.##}", Amount), UPlayerID);
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.transfer != null)
									{
										if (Convert.ToString(result.transfer.result) == "Y")
											return true;
										else
										{
											LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(result.transfer.errormsg));
											return false;
										}
									}
									else
									{
										LockFromGame = true;
										return false;
									}
								}
							}
							else
							{
								LockFromGame = false;
								return false;
							}
						}
						//menang88
						else if (GameID == "MENANG88")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["MENANG88ApiUrl"] + "/Deposit?userId={0}&amount={1}",
																				record.GamePlayerID, string.Format("{0:0.##}", Amount));
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (Convert.ToString(result.status) == "1")
										return true;
									else
									{
										LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(result.status));
										return false;
									}
								}
							}
							else
							{
								LockFromGame = false;
								return false;
							}
						}
						//xe88
						else if (GameID == "XE88")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["XE88ApiUrl"] + "/Deposit?username={0}&amount={1}",
																				record.GamePlayerID, string.Format("{0:0.##}", Amount));
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (Convert.ToString(result.code) == "0")
										return true;
									else
									{
										LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(result.code));
										return false;
									}
								}
							}
							else
							{
								LockFromGame = false;
								return false;
							}
						}
						//kypoker
						else if (GameID == "KYPOKER")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["KYPOKERApiUrl"] + "/Deposit?username={0}&amount={1}",
																				record.GamePlayerID, string.Format("{0:0.##}", Amount));
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.d != null)
									{
										if (Convert.ToString(result.d.code) == "0")
											return true;
										else
										{
											LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(result.d.code));
											return false;
										}
									}
									else
									{
										LockFromGame = true;
										return false;
									}
								}
							}
							else
							{
								LockFromGame = false;
								return false;
							}
						}
						//sabaesports | able to return after balance | result.Data.after_amount | currency hardcode in our own api
						else if (GameID == "SABAESPORTS")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["SABAESPORTSApiUrl"] + "/DepositTrans?username={0}&amount={1}&transId={2}",
																			record.GamePlayerID, string.Format("{0:0.##}", Amount), GetGameRunningNumber(GameID));
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.error_code == 0)
									{
										if (result.Data != null)
										{
											if (result.Data.status == 0)
												return true;
											else
											{
												LockFromGame = true;
												return false;
											}
										}
										else
										{
											LockFromGame = true;
											return false;
										}
									}
									else
									{
										if (result.error_code == 5)
										{
											bool isOk = false;
											string transferText = string.Empty;
											while (!isOk)
											{
												transferText = RetryDeposit(GameID, UPlayerID, UCurrency, Amount, TrackerId);
												dynamic transferResult = JObject.Parse(transferText);

												if (transferResult.error_code == 0)
													isOk = true;
												else if (transferResult.error_code != 5)
												{
													LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(transferResult.error_code));
													isOk = false;
													break;
												}
											}

											return isOk;
										}
										else
										{
											LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(result.error_code));
											return false;
										}
									}
								}
							}
							else
							{
								LockFromGame = false;
								return false;
							}
						}
						//tpg | not able to return after balance | currency based on what we pass in
						else if (GameID == "TPG")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["TPGApiUrl"] + "/Deposit?username={0}&displayName={1}&currency={2}&amount={3}",
																				record.GamePlayerID, UPlayerID, UCurrency, string.Format("{0:0.##}", Amount));
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.status == 1)
										return true;
									else
										return false;
								}
							}
							else
								return false;
						}
						//tpgfish | not able to return after balance | currency based on what we pass in
						else if (GameID == "TPGFISH")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["TPGFISHApiUrl"] + "/Deposit?username={0}&displayName={1}&currency={2}&amount={3}",
																				record.GamePlayerID, UPlayerID, UCurrency, string.Format("{0:0.##}", Amount));
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.status == 1)
										return true;
									else
										return false;
								}
							}
							else
								return false;
						}
						//vgaming | able to return after balance | result.Data.after_amount | currency hardcode in our own api
						else if (GameID == "VGAMING")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["VGAMINGApiUrl"] + "/DepositTrans?username={0}&amount={1}&transId={2}",
																			record.GamePlayerID, string.Format("{0:0.##}", Amount), GetGameRunningNumber(GameID));
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.error_code == 0)
									{
										if (result.Data != null)
										{
											if (result.Data.status == 0)
												return true;
											else
											{
												LockFromGame = true;
												return false;
											}
										}
										else
										{
											LockFromGame = true;
											return false;
										}
									}
									else
									{
										if (result.error_code == 5)
										{
											bool isOk = false;
											string transferText = string.Empty;
											while (!isOk)
											{
												transferText = RetryDeposit(GameID, UPlayerID, UCurrency, Amount, TrackerId);
												dynamic transferResult = JObject.Parse(transferText);

												if (transferResult.error_code == 0)
													isOk = true;
												else if (transferResult.error_code != 5)
												{
													LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(transferResult.error_code));
													isOk = false;
													break;
												}
											}

											return isOk;
										}
										else
										{
											LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(result.error_code));
											return false;
										}
									}
								}
							}
							else
							{
								LockFromGame = false;
								return false;
							}
						}
						//cq9
						else if (GameID == "CQ9")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["CQ9ApiUrl"] + "/Deposit?username={0}&amount={1}",
																				record.GamePlayerID, string.Format("{0:0.##}", Amount));
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									if (result.status != null)
									{
										if (Convert.ToString(result.status.code) == "0")
										{
											return true;
										}
										else
										{
											LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(result.status.code));
											return false;
										}
									}
									else
									{
										LockFromGame = true;
										return false;
									}
								}
							}
							else
							{
								LockFromGame = false;
								return false;
							}
						}
						else
						{
							LockFromGame = false;
							return false;
						}
					}
				}
				catch (Exception ex)
				{
					_common.LogError($"{TrackerId}:GameDeposit " + DateTime.Now.ToString() + " " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Amount.ToString(), Currency) + " " + ex.Message.ToString(), FilePrefix);
					LockFromGame = true;
					return false;
				}
			}
		}

		public string RetryDeposit(string Game, string PlayerID, string Currency, decimal Amount, string TrackerId)
		{
			lock (MyLock)
			{
				string apiurl = "";
				Common _common = new Common();
				int TimeoutSec = 180000; //3 mins

				try
				{
					using (Bingo_stagingConn db = new Bingo_stagingConn())
					{
						var res = new DepositVM();
						string GameID = Game.ToUpper().Trim();
						string UPlayerID = PlayerID.ToUpper().Trim();
						string UCurrency = Currency.ToUpper().Trim();

						//bg
						if (GameID == "BG")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["BGApiUrl"] + "/TransferBiz?UID={0}&IP={1}&Amount={2}&BizId={3}&CheckBizId=1",
																	record.GamePlayerID, GetIPAddress(), string.Format("{0:0.##}", Amount), GetGameRunningNumber(GameID));
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									return responseText;
								}
							}
							else
								return "";
						}
						//ibc
						else if (GameID == "IBC")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["IBCApiUrl"] + "/DepositTrans?username={0}&amount={1}&transId={2}",
																			record.GamePlayerID, string.Format("{0:0.##}", Amount), GetGameRunningNumber(GameID));
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									return responseText;
								}
							}
							else
								return "";
						}
						//mega
						else if (GameID == "MEGA")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["MEGAApiUrl"] + "/Deposit?loginId={0}&amount={1}&bizId={2}&checkBizId=1",
																				record.GamePlayerID, string.Format("{0:0.##}", Amount), GetGameRunningNumber(GameID));
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									return responseText;
								}
							}
							else
								return "";
						}
						//sabaesports
						else if (GameID == "SABAESPORTS")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["SABAESPORTSApiUrl"] + "/DepositTrans?username={0}&amount={1}&transId={2}",
																			record.GamePlayerID, string.Format("{0:0.##}", Amount), GetGameRunningNumber(GameID));
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									return responseText;
								}
							}
							else
								return "";
						}
						//vgaming
						else if (GameID == "VGAMING")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["VGAMINGApiUrl"] + "/DepositTrans?username={0}&amount={1}&transId={2}",
																			record.GamePlayerID, string.Format("{0:0.##}", Amount), GetGameRunningNumber(GameID));
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									return responseText;
								}
							}
							else
								return "";
						}

						return "";
					}
				}
				catch (Exception ex)
				{
					_common.LogError($"{TrackerId}:RetryDeposit " + DateTime.Now.ToString() + " " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Amount.ToString(), Currency) + " " + ex.Message.ToString(), FilePrefix);
					return "";
				}
			}
		}

		public bool GameWithdraw(string Game, string PlayerID, string Currency, decimal Amount, string TrackerId, ref bool? LockFromGame) //Currency need to get from bingotown Game table and pass in here (by myself)
		{
			lock (MyLock)
			{
				string apiurl = "";
				Common _common = new Common();
				int TimeoutSec = 180000; //3 mins

				try
				{
					using (Bingo_stagingConn db = new Bingo_stagingConn())
					{
						var res = new WithdrawVM();
						string GameID = Game.ToUpper().Trim();
						string UPlayerID = PlayerID.ToUpper().Trim();
						string UCurrency = Currency.ToUpper().Trim();

						if (Amount > 0)
						{
							//bg | able to return after balance | result.result | currency based on agent
							if (GameID == "BG")
							{
								var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																									 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format(ConfigurationManager.AppSettings["BGApiUrl"] + "/TransferBiz?UID={0}&IP={1}&Amount={2}&BizId={3}&CheckBizId=1",
																			record.GamePlayerID, GetIPAddress(), string.Format("{0:0.##}", -1 * Amount), GetGameRunningNumber(GameID));
									var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
									httpWebRequest.ContentType = "application/json";
									httpWebRequest.Method = "GET";
									httpWebRequest.Timeout = TimeoutSec;
									httpWebRequest.ReadWriteTimeout = TimeoutSec;
									var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

									using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
									{
										var responseText = streamReader.ReadToEnd();
										dynamic result = JObject.Parse(responseText);

										if (result.error == null)
											return true;
										else
										{
											if (Convert.ToString(result.error.code) == "2474")
											{
												bool isOk = false;
												string transferText = string.Empty;
												while (!isOk)
												{
													transferText = RetryWithdraw(GameID, UPlayerID, UCurrency, Amount, TrackerId);
													dynamic transferResult = JObject.Parse(transferText);

													if (transferResult.error == null)
														isOk = true;
													else if (Convert.ToString(transferResult.error.code) != "2474")
													{
														isOk = false;
														LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(transferResult.error.code));
														break;
													}
												}

												return isOk;
											}
											else
											{
												LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(result.error.code));
												return false;
											}

										}
									}
								}
								else
								{
									LockFromGame = false;
									return false;
								}
							}
							//ag | not able to return after balance | currency based on what we pass in
							else if (GameID == "AG")
							{
								var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																									 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format(ConfigurationManager.AppSettings["AGApiUrl"] + "/Withdraw?loginname={0}&credit={1}&currency={2}", record.GamePlayerID, string.Format("{0:0.##}", Amount), Currency);
									var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
									httpWebRequest.ContentType = "application/json";
									httpWebRequest.Method = "GET";
									httpWebRequest.Timeout = TimeoutSec;
									httpWebRequest.ReadWriteTimeout = TimeoutSec;
									var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

									using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
									{
										var responseText = streamReader.ReadToEnd();
										dynamic result = JObject.Parse(responseText);

										if (string.IsNullOrWhiteSpace(Convert.ToString(result.ErrorDesc)))
											return true;
										else
										{
											LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(result.ErrorCode));
											return false;
										}

									}
								}
								else
								{
									LockFromGame = false;
									return false;
								}
							}
							//playtech | able to return balance | result.result.currentplayerbalance | currency define by provider
							else if (GameID == "PT")
							{
								var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																									 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format(ConfigurationManager.AppSettings["PTApiUrl"] + "/Withdraw?playerName={0}&amount={1}", record.GamePlayerID, string.Format("{0:0.##}", Amount));
									var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
									httpWebRequest.ContentType = "application/json";
									httpWebRequest.Method = "GET";
									httpWebRequest.Timeout = TimeoutSec;
									httpWebRequest.ReadWriteTimeout = TimeoutSec;
									var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

									using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
									{
										var responseText = streamReader.ReadToEnd();
										dynamic result = JObject.Parse(responseText);

										if (result.error == null)
											return true;
										else
										{
											LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(result.errorcode));
											return false;
										}
									}
								}
								else
								{
									LockFromGame = false;
									return false;
								}
							}
							//play8oy | not able to return after balance | currency define by provider
							else if (GameID == "IG")
							{
								var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																									 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format(ConfigurationManager.AppSettings["IGApiUrl"] + "/Transfer?UID={0}&Action=1&Amount={1}&IP={2}", record.GamePlayerID, string.Format("{0:0.##}", Amount), GetIPAddress());
									var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
									httpWebRequest.ContentType = "application/json";
									httpWebRequest.Method = "GET";
									httpWebRequest.Timeout = TimeoutSec;
									httpWebRequest.ReadWriteTimeout = TimeoutSec;
									var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

									using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
									{
										var responseText = streamReader.ReadToEnd();
										dynamic result = JObject.Parse(responseText);

										if (Convert.ToString(result.Code) == "0")
											return true;
										else
										{
											LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(result.Code));
											return false;
										}
									}
								}
								else
								{
									LockFromGame = false;
									return false;
								}
							}
							//28win | able to return after balance but not return from own 28win api | currency define by provider
							else if (GameID == "BT28WIN")
							{
								var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																									 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format(ConfigurationManager.AppSettings["BT28WINApiUrl"] + "/Withdraw?loginID={0}&amount={1}", record.GamePlayerID, string.Format("{0:0.##}", Amount));
									var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
									httpWebRequest.ContentType = "application/json";
									httpWebRequest.Method = "GET";
									httpWebRequest.Timeout = TimeoutSec;
									httpWebRequest.ReadWriteTimeout = TimeoutSec;

									var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

									using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
									{
										var responseText = streamReader.ReadToEnd();
										dynamic result = JObject.Parse(responseText);

										if (Convert.ToString(result.ErrorCode) == "0")
											return true;
										else
										{
											LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(result.ErrorCode));
											return false;
										}
									}
								}
								else
								{
									LockFromGame = false;
									return false;
								}
							}
							//allcity | not able to return after balance | currency fix in provider side
							else if (GameID == "ALLCITY")
							{
								var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																									 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format(ConfigurationManager.AppSettings["ALLCITYApiUrl"] + "/Withdraw?account={0}&amount={1}", record.GamePlayerID, string.Format("{0:0.##}", Amount));
									var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
									httpWebRequest.ContentType = "application/json";
									httpWebRequest.Method = "GET";
									httpWebRequest.Timeout = TimeoutSec;
									httpWebRequest.ReadWriteTimeout = TimeoutSec;
									var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

									using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
									{
										var responseText = streamReader.ReadToEnd();
										dynamic result = JObject.Parse(responseText);

										if (result.code == 200)
											return true;
										else
										{
											LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(result.code));
											return false;
										}
									}
								}
								else
								{
									LockFromGame = false;
									return false;
								}
							}
							//ibc | able to return after balance | result.Data.after_amount | currency hardcode in our own api
							else if (GameID == "IBC")
							{
								var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																									 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format(ConfigurationManager.AppSettings["IBCApiUrl"] + "/WithdrawTrans?username={0}&amount={1}&transId={2}",
																				record.GamePlayerID, string.Format("{0:0.##}", Amount), GetGameRunningNumber(GameID));
									var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
									httpWebRequest.ContentType = "application/json";
									httpWebRequest.Method = "GET";
									httpWebRequest.Timeout = TimeoutSec;
									httpWebRequest.ReadWriteTimeout = TimeoutSec;
									var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

									using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
									{
										var responseText = streamReader.ReadToEnd();
										dynamic result = JObject.Parse(responseText);

										if (result.error_code == 0)
										{
											if (result.Data != null)
											{
												if (result.Data.status == 0)
													return true;
												else
												{
													LockFromGame = true;
													return false;
												}
											}
											else
											{
												LockFromGame = true;
												return false;
											}
										}
										else
										{
											if (result.error_code == 5)
											{
												bool isOk = false;
												string transferText = string.Empty;
												while (!isOk)
												{
													transferText = RetryWithdraw(GameID, UPlayerID, UCurrency, Amount, TrackerId);
													dynamic transferResult = JObject.Parse(transferText);

													if (transferResult.error_code == 0)
														isOk = true;
													else if (transferResult.error_code != 5)
													{
														isOk = false;
														LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(transferResult.error_code));
														break;
													}
												}

												return isOk;
											}
											else
											{
												LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(result.error_code));
												return false;
											}
										}
									}
								}
								else
								{
									LockFromGame = false;
									return false;
								}
							}
							//evo888 | not able to return after balance | able to set country in our own api
							else if (GameID == "EVO888")
							{
								var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																									 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format(ConfigurationManager.AppSettings["EVO888ApiUrl"] + "/Withdraw?username={0}&amount={1}", record.GamePlayerID, string.Format("{0:0.##}", Amount));
									var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
									httpWebRequest.ContentType = "application/json";
									httpWebRequest.Method = "GET";
									httpWebRequest.Timeout = TimeoutSec;
									httpWebRequest.ReadWriteTimeout = TimeoutSec;
									var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

									using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
									{
										var responseText = streamReader.ReadToEnd();
										dynamic result = JObject.Parse(responseText);

										if (result.code == 0 && Convert.ToString(result.success).Trim().ToUpper() == "TRUE")
											return true;
										else
										{
											LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(result.code));
											return false;
										}
									}
								}
								else
								{
									LockFromGame = false;
									return false;
								}
							}
							//vpower | able to return balance | result.Credit | currency fix in provider side
							else if (GameID == "VPOWER")
							{
								var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																									 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format(ConfigurationManager.AppSettings["VPOWERApiUrl"] + "/Withdraw?username={0}&amount={1}", record.GamePlayerID, string.Format("{0:0.##}", Amount));
									var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
									httpWebRequest.ContentType = "application/json";
									httpWebRequest.Method = "GET";
									httpWebRequest.Timeout = TimeoutSec;
									httpWebRequest.ReadWriteTimeout = TimeoutSec;
									var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

									using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
									{
										var responseText = streamReader.ReadToEnd();
										dynamic result = JObject.Parse(responseText);

										if (Convert.ToInt16(result.ErrorCode) == 0)
											return true;
										else
										{
											LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(result.ErrorCode));
											return false;
										}
									}
								}
								else
								{
									LockFromGame = false;
									return false;
								}
							}
							//sexygaming | able to return balance | result.data.Balance | currency fix in provider side
							else if (GameID == "SEXYGAMING")
							{
								var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																									 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format(ConfigurationManager.AppSettings["SEXYGAMINGApiUrl"] + "/Withdraw?username={0}&amount={1}",
																					record.GamePlayerID, string.Format("{0:0.##}", Amount));
									var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
									httpWebRequest.ContentType = "application/json";
									httpWebRequest.Method = "GET";
									httpWebRequest.Timeout = TimeoutSec;
									httpWebRequest.ReadWriteTimeout = TimeoutSec;
									var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

									using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
									{
										var responseText = streamReader.ReadToEnd();
										dynamic result = JObject.Parse(responseText);

										if (result.error == 0 && Convert.ToString(result.message).Trim().ToUpper() == "SUCCESS")
											return true;
										else
										{
											LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(result.error));
											return false;
										}
									}
								}
								else
								{
									LockFromGame = false;
									return false;
								}
							}
							//spadegaming | able to return balance | result.afterBalance | currency hardcode in our own api
							else if (GameID == "SPADEGAMING")
							{
								var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																									 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format(ConfigurationManager.AppSettings["SPADEGAMINGApiUrl"] + "/Withdraw?username={0}&amount={1}",
																					record.GamePlayerID, string.Format("{0:0.##}", Amount));
									var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
									httpWebRequest.ContentType = "application/json";
									httpWebRequest.Method = "GET";
									httpWebRequest.Timeout = TimeoutSec;
									httpWebRequest.ReadWriteTimeout = TimeoutSec;
									var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

									using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
									{
										var responseText = streamReader.ReadToEnd();
										dynamic result = JObject.Parse(responseText);

										if (result.code == 0 && Convert.ToString(result.msg).Trim().ToUpper() == "SUCCESS")
											return true;
										else
										{
											LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(result.code));
											return false;
										}
									}
								}
								else
								{
									LockFromGame = false;
									return false;
								}
							}
							//mega
							else if (GameID == "MEGA")
							{
								var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																									 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format(ConfigurationManager.AppSettings["MEGAApiUrl"] + "/Withdraw?loginId={0}&amount={1}&bizId={2}&checkBizId=1",
																					record.GamePlayerID, string.Format("{0:0.##}", Amount), GetGameRunningNumber(GameID));
									var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
									httpWebRequest.ContentType = "application/json";
									httpWebRequest.Method = "GET";
									httpWebRequest.Timeout = TimeoutSec;
									httpWebRequest.ReadWriteTimeout = TimeoutSec;
									var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

									using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
									{
										var responseText = streamReader.ReadToEnd();
										dynamic result = JObject.Parse(responseText);

										if (result.error == null)
											return true;
										else
										{
											if (Convert.ToString(result.error.code) == "37124")
											{
												bool isOk = false;
												string transferText = string.Empty;
												while (!isOk)
												{
													transferText = RetryWithdraw(GameID, UPlayerID, UCurrency, Amount, TrackerId);
													dynamic transferResult = JObject.Parse(transferText);

													if (transferResult.error == null)
														isOk = true;
													else if (Convert.ToString(transferResult.error.code) != "37124")
													{
														LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(transferResult.error.code));
														isOk = false;
														break;
													}
												}

												return isOk;
											}
											else
											{
												LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(result.error.code));
												return false;
											}
										}
									}
								}
								else
								{
									LockFromGame = false;
									return false;
								}
							}
							//evolution
							else if (GameID == "EVOLUTIONGAMING")
							{
								var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																									 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format(ConfigurationManager.AppSettings["EVOLUTIONGAMINGApiUrl"] + "/Withdraw?username={0}&amount={1}",
																					record.GamePlayerID, string.Format("{0:0.##}", Amount));
									var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
									httpWebRequest.ContentType = "application/json";
									httpWebRequest.Method = "GET";
									httpWebRequest.Timeout = TimeoutSec;
									httpWebRequest.ReadWriteTimeout = TimeoutSec;
									var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

									using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
									{
										var responseText = streamReader.ReadToEnd();
										dynamic result = JObject.Parse(responseText);

										if (result.transfer != null)
										{
											if (Convert.ToString(result.transfer.result) == "Y")
												return true;
											else
											{
												LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(result.transfer.errormsg));
												return false;
											}
										}
										else
										{
											LockFromGame = true;
											return false;
										}
									}
								}
								else
								{
									LockFromGame = false;
									return false;
								}
							}
							//menang88
							else if (GameID == "MENANG88")
							{
								var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																									 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format(ConfigurationManager.AppSettings["MENANG88ApiUrl"] + "/Withdraw?userId={0}&amount={1}",
																					record.GamePlayerID, string.Format("{0:0.##}", Amount));
									var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
									httpWebRequest.ContentType = "application/json";
									httpWebRequest.Method = "GET";
									httpWebRequest.Timeout = TimeoutSec;
									httpWebRequest.ReadWriteTimeout = TimeoutSec;
									var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

									using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
									{
										var responseText = streamReader.ReadToEnd();
										dynamic result = JObject.Parse(responseText);

										if (Convert.ToString(result.status) == "1")
											return true;
										else
										{
											LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(result.status));
											return false;
										}
									}
								}
								else
								{
									LockFromGame = false;
									return false;
								}
							}
							//xe88
							else if (GameID == "XE88")
							{
								var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																									 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format(ConfigurationManager.AppSettings["XE88ApiUrl"] + "/Withdraw?username={0}&amount={1}",
																					record.GamePlayerID, string.Format("{0:0.##}", Amount));
									var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
									httpWebRequest.ContentType = "application/json";
									httpWebRequest.Method = "GET";
									httpWebRequest.Timeout = TimeoutSec;
									httpWebRequest.ReadWriteTimeout = TimeoutSec;
									var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

									using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
									{
										var responseText = streamReader.ReadToEnd();
										dynamic result = JObject.Parse(responseText);

										if (Convert.ToString(result.code) == "0")
											return true;
										else
										{
											LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(result.code));
											return false;
										}
									}
								}
								else
								{
									LockFromGame = false;
									return false;
								}
							}
							//kypoker
							else if (GameID == "KYPOKER")
							{
								var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																									 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format(ConfigurationManager.AppSettings["KYPOKERApiUrl"] + "/Withdraw?username={0}&amount={1}",
																					record.GamePlayerID, string.Format("{0:0.##}", Amount));
									var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
									httpWebRequest.ContentType = "application/json";
									httpWebRequest.Method = "GET";
									httpWebRequest.Timeout = TimeoutSec;
									httpWebRequest.ReadWriteTimeout = TimeoutSec;
									var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

									using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
									{
										var responseText = streamReader.ReadToEnd();
										dynamic result = JObject.Parse(responseText);

										if (result.d != null)
										{
											if (Convert.ToString(result.d.code) == "0")
												return true;
											else
											{
												LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(result.d.code));
												return false;
											}
										}
										else
										{
											LockFromGame = true;
											return false;
										}
									}
								}
								else
								{
									LockFromGame = false;
									return false;
								}
							}
							//sabaesports | able to return after balance | result.Data.after_amount | currency hardcode in our own api
							else if (GameID == "SABAESPORTS")
							{
								var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																									 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format(ConfigurationManager.AppSettings["SABAESPORTSApiUrl"] + "/WithdrawTrans?username={0}&amount={1}&transId={2}",
																				record.GamePlayerID, string.Format("{0:0.##}", Amount), GetGameRunningNumber(GameID));
									var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
									httpWebRequest.ContentType = "application/json";
									httpWebRequest.Method = "GET";
									httpWebRequest.Timeout = TimeoutSec;
									httpWebRequest.ReadWriteTimeout = TimeoutSec;
									var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

									using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
									{
										var responseText = streamReader.ReadToEnd();
										dynamic result = JObject.Parse(responseText);

										if (result.error_code == 0)
										{
											if (result.Data != null)
											{
												if (result.Data.status == 0)
													return true;
												else
												{
													LockFromGame = true;
													return false;
												}
											}
											else
											{
												LockFromGame = true;
												return false;
											}
										}
										else
										{
											if (result.error_code == 5)
											{
												bool isOk = false;
												string transferText = string.Empty;
												while (!isOk)
												{
													transferText = RetryWithdraw(GameID, UPlayerID, UCurrency, Amount, TrackerId);
													dynamic transferResult = JObject.Parse(transferText);

													if (transferResult.error_code == 0)
														isOk = true;
													else if (transferResult.error_code != 5)
													{
														isOk = false;
														LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(transferResult.error_code));
														break;
													}
												}

												return isOk;
											}
											else
											{
												LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(result.error_code));
												return false;
											}
										}
									}
								}
								else
								{
									LockFromGame = false;
									return false;
								}
							}
							//tpg | not able to return after balance | currency based on what we pass in
							else if (GameID == "TPG")
							{
								var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																									 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format(ConfigurationManager.AppSettings["TPGApiUrl"] + "/Withdraw?username={0}&displayName={1}&currency={2}&amount={3}",
																					record.GamePlayerID, UPlayerID, UCurrency, string.Format("{0:0.##}", Amount));
									var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
									httpWebRequest.ContentType = "application/json";
									httpWebRequest.Method = "GET";
									httpWebRequest.Timeout = TimeoutSec;
									httpWebRequest.ReadWriteTimeout = TimeoutSec;
									var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

									using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
									{
										var responseText = streamReader.ReadToEnd();
										dynamic result = JObject.Parse(responseText);

										if (result.status == 1)
											return true;
										else
											return false;
									}
								}
								else
									return false;
							}
							//tpgfish | not able to return after balance | currency based on what we pass in
							else if (GameID == "TPGFISH")
							{
								var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																									 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format(ConfigurationManager.AppSettings["TPGFISHApiUrl"] + "/Withdraw?username={0}&displayName={1}&currency={2}&amount={3}",
																					record.GamePlayerID, UPlayerID, UCurrency, string.Format("{0:0.##}", Amount));
									var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
									httpWebRequest.ContentType = "application/json";
									httpWebRequest.Method = "GET";
									httpWebRequest.Timeout = TimeoutSec;
									httpWebRequest.ReadWriteTimeout = TimeoutSec;
									var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

									using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
									{
										var responseText = streamReader.ReadToEnd();
										dynamic result = JObject.Parse(responseText);

										if (result.status == 1)
											return true;
										else
											return false;
									}
								}
								else
									return false;
							}
							//vgaming | able to return after balance | result.Data.after_amount | currency hardcode in our own api
							else if (GameID == "VGAMING")
							{
								var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																									 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format(ConfigurationManager.AppSettings["VGAMINGApiUrl"] + "/WithdrawTrans?username={0}&amount={1}&transId={2}",
																				record.GamePlayerID, string.Format("{0:0.##}", Amount), GetGameRunningNumber(GameID));
									var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
									httpWebRequest.ContentType = "application/json";
									httpWebRequest.Method = "GET";
									httpWebRequest.Timeout = TimeoutSec;
									httpWebRequest.ReadWriteTimeout = TimeoutSec;
									var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

									using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
									{
										var responseText = streamReader.ReadToEnd();
										dynamic result = JObject.Parse(responseText);

										if (result.error_code == 0)
										{
											if (result.Data != null)
											{
												if (result.Data.status == 0)
													return true;
												else
												{
													LockFromGame = true;
													return false;
												}
											}
											else
											{
												LockFromGame = true;
												return false;
											}
										}
										else
										{
											if (result.error_code == 5)
											{
												bool isOk = false;
												string transferText = string.Empty;
												while (!isOk)
												{
													transferText = RetryWithdraw(GameID, UPlayerID, UCurrency, Amount, TrackerId);
													dynamic transferResult = JObject.Parse(transferText);

													if (transferResult.error_code == 0)
														isOk = true;
													else if (transferResult.error_code != 5)
													{
														isOk = false;
														LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(transferResult.error_code));
														break;
													}
												}

												return isOk;
											}
											else
											{
												LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(result.error_code));
												return false;
											}
										}
									}
								}
								else
								{
									LockFromGame = false;
									return false;
								}
							}
							//cq9
							else if (GameID == "CQ9")
							{
								var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																									 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format(ConfigurationManager.AppSettings["CQ9ApiUrl"] + "/Withdraw?username={0}&amount={1}",
																					record.GamePlayerID, string.Format("{0:0.##}", Amount));
									var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
									httpWebRequest.ContentType = "application/json";
									httpWebRequest.Method = "GET";
									httpWebRequest.Timeout = TimeoutSec;
									httpWebRequest.ReadWriteTimeout = TimeoutSec;
									var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

									using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
									{
										var responseText = streamReader.ReadToEnd();
										dynamic result = JObject.Parse(responseText);

										if (result.status != null)
										{
											if (Convert.ToString(result.status.code) == "0")
											{
												return true;
											}
											else
											{
												LockFromGame = CheckIsLockPlayer(GameID, Convert.ToString(result.status.code));
												return false;
											}
										}
										else
										{
											LockFromGame = true;
											return false;
										}
									}
								}
								else
								{
									LockFromGame = false;
									return false;
								}
							}
							//default
							else
							{
								LockFromGame = false;
								return false;
							}
						}

						LockFromGame = false;
						return false;
					}
				}
				catch (Exception ex)
				{
					_common.LogError($"{TrackerId}:GameWithdraw " + DateTime.Now.ToString() + " " + string.Format("{0}|{1}|{2}", Game, PlayerID, Currency) + " " + ex.Message.ToString(), FilePrefix);
					LockFromGame = true;
					return false;
				}
			}
		}

		public string RetryWithdraw(string Game, string PlayerID, string Currency, decimal Amount, string TrackerId)
		{
			lock (MyLock)
			{
				string apiurl = "";
				Common _common = new Common();
				int TimeoutSec = 180000; //3 mins

				try
				{
					using (Bingo_stagingConn db = new Bingo_stagingConn())
					{
						var res = new DepositVM();
						string GameID = Game.ToUpper().Trim();
						string UPlayerID = PlayerID.ToUpper().Trim();
						string UCurrency = Currency.ToUpper().Trim();

						//bg
						if (GameID == "BG")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["BGApiUrl"] + "/TransferBiz?UID={0}&IP={1}&Amount={2}&BizId={3}&CheckBizId=1",
																		record.GamePlayerID, GetIPAddress(), string.Format("{0:0.##}", -1 * Amount), GetGameRunningNumber(GameID));
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									return responseText;
								}
							}
							else
								return "";
						}
						//ibc
						else if (GameID == "IBC")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["IBCApiUrl"] + "/WithdrawTrans?username={0}&amount={1}&transId={2}",
																			record.GamePlayerID, string.Format("{0:0.##}", Amount), GetGameRunningNumber(GameID));
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									return responseText;
								}
							}
							else
								return "";
						}
						//mega
						else if (GameID == "MEGA")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["MEGAApiUrl"] + "/Withdraw?loginId={0}&amount={1}&bizId={2}&checkBizId=1",
																				record.GamePlayerID, string.Format("{0:0.##}", Amount), GetGameRunningNumber(GameID));
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									return responseText;
								}
							}
							else
								return "";
						}
						//sabaesports
						else if (GameID == "SABAESPORTS")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["SABAESPORTSApiUrl"] + "/WithdrawTrans?username={0}&amount={1}&transId={2}",
																			record.GamePlayerID, string.Format("{0:0.##}", Amount), GetGameRunningNumber(GameID));
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									return responseText;
								}
							}
							else
								return "";
						}
						//vgaming
						else if (GameID == "VGAMING")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format(ConfigurationManager.AppSettings["VGAMINGApiUrl"] + "/WithdrawTrans?username={0}&amount={1}&transId={2}",
																			record.GamePlayerID, string.Format("{0:0.##}", Amount), GetGameRunningNumber(GameID));
								var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
								httpWebRequest.ContentType = "application/json";
								httpWebRequest.Method = "GET";
								httpWebRequest.Timeout = TimeoutSec;
								httpWebRequest.ReadWriteTimeout = TimeoutSec;
								var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

								using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
								{
									var responseText = streamReader.ReadToEnd();
									dynamic result = JObject.Parse(responseText);

									return responseText;
								}
							}
							else
								return "";
						}

						return "";
					}
				}
				catch (Exception ex)
				{
					_common.LogError($"{TrackerId}:RetryWithdraw " + DateTime.Now.ToString() + " " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Amount.ToString(), Currency) + " " + ex.Message.ToString(), FilePrefix);
					return "";
				}
			}
		}

		[HttpGet]
		public HttpResponseMessage GetGameUrl(string Game, string PlayerID, string Currency, decimal Amount, string TrackerId, string Language = "en")
		{
			lock (MyLock)
			{
				string apiurl = "";
				Common _common = new Common();
				bool canGetUrl = false;
				int TimeoutSec = 180000; //3 mins
				string msg = "";
				decimal? DMinAmount = null;
				decimal? DMaxAmount = null;
				decimal? WMinAmount = null;
				decimal? WMaxAmount = null;
				bool? LockFromGame = false;

				try
				{
					_common.InsertLog("GetGameUrl", "TrackerId", TrackerId, "Request", "", "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Amount", Amount.ToString(), "Language", Language,
														"DMinAmount", DMinAmount, "DMaxAmount", DMaxAmount, "WMinAmount", WMinAmount, "WMaxAmount", WMaxAmount);

					using (Bingo_stagingConn db = new Bingo_stagingConn())
					{
						var res = new GetGameUrlVM();
						string GameID = Game.ToUpper().Trim();
						string UPlayerID = PlayerID.ToUpper().Trim();
						string UCurrency = Currency.ToUpper().Trim();

						if (Amount < 0)
						{
							res.PlayerID = UPlayerID;
							res.Url = null;
							res.Game = GameID;
							res.Message = "Negative amount.";
							res.Code = 1;
							res.LockFromGame = true;
							_common.LogError($"{TrackerId}:GetGameUrl:FAILED - Negative Amount " + string.Format("{0}|{1}|{2}|{3}|{4}", Game, PlayerID, Currency, Amount.ToString(), Language), FilePrefix);
							_common.InsertLog("GetGameUrl", "TrackerId", TrackerId, "Response", JsonConvert.SerializeObject(res), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Amount", Amount.ToString(), "Language", Language,
																"DMinAmount", DMinAmount, "DMaxAmount", DMaxAmount, "WMinAmount", WMinAmount, "WMaxAmount", WMaxAmount);
							msg = $"FAILED: {UPlayerID} deposit negative balance to {GameID}.";
							foreach (var phone in apiPhoneList.Split(','))
							{
								_common.SendSmsToBulkSms(phone, msg);
							}
							return new HttpResponseMessage()
							{
								StatusCode = HttpStatusCode.OK,
								Content = new StringContent(JsonConvert.SerializeObject(res), System.Text.Encoding.UTF8, "application/json")
							};
						}

						if (Amount == 0)
						{
							canGetUrl = true;
							_common.LogError($"{TrackerId}:GetGameUrl:SUCCESS - Amount 0, Skip Deposit " + string.Format("{0}|{1}|{2}|{3}|{4}", Game, PlayerID, Currency, Amount.ToString(), Language), FilePrefix);
						}
						else
						{
							GetGameTransferLimit(GameID, "DEPOSIT", UCurrency, TrackerId, ref DMinAmount, ref DMaxAmount);

							//unable to get deposit transfer limit
							if (DMinAmount.Equals(null) || DMaxAmount.Equals(null))
							{
								res.PlayerID = UPlayerID;
								res.Url = null;
								res.Game = GameID;
								res.Message = "Failed to get deposit transfer limit.";
								res.Code = 7;
								res.IsRefund = true;
								res.LockFromGame = false;
								_common.LogError($"{TrackerId}:GetGameUrl:FAILED - Failed To Get Deposit Transfer Limit " + string.Format("{0}|{1}|{2}|{3}|{4}", Game, PlayerID, Currency, Amount.ToString(), Language), FilePrefix);
								_common.InsertLog("GetGameUrl", "TrackerId", TrackerId, "Response", JsonConvert.SerializeObject(res), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Amount", Amount.ToString(), "Language", Language,
																	"DMinAmount", DMinAmount, "DMaxAmount", DMaxAmount, "WMinAmount", WMinAmount, "WMaxAmount", WMaxAmount);
								msg = $"FAILED: {UPlayerID} failed to get deposit transfer limit for {GameID}.";
								foreach (var phone in apiPhoneList.Split(','))
								{
									_common.SendSmsToBulkSms(phone, msg);
								}
								return new HttpResponseMessage()
								{
									StatusCode = HttpStatusCode.OK,
									Content = new StringContent(JsonConvert.SerializeObject(res), System.Text.Encoding.UTF8, "application/json")
								};
							}

							//less than min amount, skip deposit process
							if (DMinAmount > Amount)
							{
								canGetUrl = true;
								//res.IsRefund = true;
								_common.LogError($"{TrackerId}:GetGameUrl:SUCCESS - Deposit Less Than Min Limit, Skip Deposit " + string.Format("{0}|{1}|{2}|{3}|{4}", Game, PlayerID, Currency, Amount.ToString(), Language), FilePrefix);
							}
							//more than max amount, break into few steps
							else if (Amount > DMaxAmount)
							{
								_common.LogError($"{TrackerId}:GetGameUrl:SUCCESS - Deposit More Than Max Limit " + string.Format("{0}|{1}|{2}|{3}|{4}", Game, PlayerID, Currency, Amount.ToString(), Language), FilePrefix);
								decimal depositAmt = 0;
								decimal remainAmt = Amount;
								decimal? initialGameBalance = GetGameBalance(GameID, UPlayerID, UCurrency, TrackerId);
								decimal amt = 0;
								if (!initialGameBalance.Equals(null))
								{
									_common.LogError($"{TrackerId}:GetGameUrl:SUCCESS - Deposit Initial Balance: {initialGameBalance.ToString()} " + string.Format("{0}|{1}|{2}|{3}|{4}", Game, PlayerID, Currency, Amount.ToString(), Language), FilePrefix);
									decimal? failedGameBalance = null;

									_common.LogError($"{TrackerId}:GetGameUrl:SUCCESS - Amount Going To Deposit: {remainAmt.ToString()} " + string.Format("{0}|{1}|{2}|{3}|{4}", Game, PlayerID, Currency, Amount.ToString(), Language), FilePrefix);

									while (remainAmt != 0)
									{
										amt = remainAmt > DMaxAmount ? (decimal)DMaxAmount : remainAmt;
										canGetUrl = GameDeposit(GameID, UPlayerID, UCurrency, amt, TrackerId, ref LockFromGame);
										_common.LogError($"{TrackerId}:GetGameUrl:" + (canGetUrl == true ? "SUCCESS" : "FAILED") + $" - Current Deposit Amount: {amt.ToString()} " + string.Format("{0}|{1}|{2}|{3}|{4}", Game, PlayerID, Currency, Amount.ToString(), Language), FilePrefix);
										if (canGetUrl == false)
										{
											failedGameBalance = GetGameBalance(GameID, UPlayerID, UCurrency, TrackerId);
											break;
										}

										remainAmt -= amt;
										depositAmt += amt;

										_common.LogError($"{TrackerId}:GetGameUrl:SUCCESS - Remaining Deposit Amount: {remainAmt.ToString()}, Deposited Amount: {depositAmt.ToString()} " + string.Format("{0}|{1}|{2}|{3}|{4}", Game, PlayerID, Currency, Amount.ToString(), Language), FilePrefix);
									}

									if (canGetUrl == false)
									{
										_common.LogError($"{TrackerId}:GetGameUrl:FAILED - Deposit Failed, Start To Get End Balance " + string.Format("{0}|{1}|{2}|{3}|{4}", Game, PlayerID, Currency, Amount.ToString(), Language) + " " + JsonConvert.SerializeObject(res), FilePrefix);

										if (!failedGameBalance.Equals(null))
										{
											_common.LogError($"{TrackerId}:GetGameUrl:SUCCESS - End Balance: {failedGameBalance.ToString()} " + string.Format("{0}|{1}|{2}|{3}|{4}", Game, PlayerID, Currency, Amount.ToString(), Language), FilePrefix);
											if ((decimal)failedGameBalance - initialGameBalance > 0)
											{
												//to check the last deposit is success, if success then add the last deposit amount and deduct from final balance
												//just to prevent ibc, 28win and menang88 if win money got credit money to player account in between the deposit process
												if ((decimal)failedGameBalance - initialGameBalance >= depositAmt + amt)
												{
													decimal totalAmt = depositAmt + amt;
													//withdraw depositAmt + amt and set refund amount = Amount
													if (totalAmt > 0)
													{
														_common.LogError($"{TrackerId}:GetGameUrl:SUCCESS - Start Rollback Deposit " + string.Format("{0}|{1}|{2}|{3}|{4}", Game, PlayerID, Currency, Amount.ToString(), Language), FilePrefix);

														GetGameTransferLimit(GameID, "WITHDRAW", UCurrency, TrackerId, ref WMinAmount, ref WMaxAmount);

														if (WMinAmount.Equals(null) || WMaxAmount.Equals(null))
														{
															//need to call pp api to block player
															res.PlayerID = UPlayerID;
															res.Url = null;
															res.Game = GameID;
															res.Message = "Failed to get withdraw transfer limit for deposit rollback.";
															res.Code = 7;
															res.LockFromGame = false;
															_common.LogError($"{TrackerId}:GetGameUrl:FAILED - Failed To Get Withdraw Transfer Limit " + string.Format("{0}|{1}|{2}|{3}|{4}", Game, PlayerID, Currency, Amount.ToString(), Language) + " " + JsonConvert.SerializeObject(res), FilePrefix);
															_common.InsertLog("GetGameUrl", "TrackerId", TrackerId, "Response", JsonConvert.SerializeObject(res), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Amount", Amount.ToString(), "Language", Language,
																								"DMinAmount", DMinAmount, "DMaxAmount", DMaxAmount, "WMinAmount", WMinAmount, "WMaxAmount", WMaxAmount);
															msg = $"FAILED: {UPlayerID} get {GameID} withdraw transfer limit failed for deposit rollback.";
															foreach (var phone in apiPhoneList.Split(','))
															{
																_common.SendSmsToBulkSms(phone, msg);
															}
															return new HttpResponseMessage()
															{
																StatusCode = HttpStatusCode.OK,
																Content = new StringContent(JsonConvert.SerializeObject(res), System.Text.Encoding.UTF8, "application/json")
															};
														}

														RollbackDeposit(totalAmt, (decimal)WMaxAmount, PlayerID, Game, Currency, Language, TrackerId, ref res, ref LockFromGame);
													}
												}
												//last deposit failed
												else
												{
													decimal totalAmt = depositAmt;
													//withdraw depositAmt and set refund amount = Amount
													if (totalAmt > 0)
													{
														_common.LogError($"{TrackerId}:GetGameUrl:SUCCESS - Start Rollback Deposit " + string.Format("{0}|{1}|{2}|{3}|{4}", Game, PlayerID, Currency, Amount.ToString(), Language), FilePrefix);

														GetGameTransferLimit(GameID, "WITHDRAW", UCurrency, TrackerId, ref WMinAmount, ref WMaxAmount);

														if (WMinAmount.Equals(null) || WMaxAmount.Equals(null))
														{
															res.PlayerID = UPlayerID;
															res.Url = null;
															res.Game = GameID;
															res.Message = "Failed to get withdraw transfer limit for deposit rollback.";
															res.Code = 7;
															res.LockFromGame = false;
															_common.LogError($"{TrackerId}:GetGameUrl:FAILED - Failed To Get Withdraw Transfer Limit " + string.Format("{0}|{1}|{2}|{3}|{4}", Game, PlayerID, Currency, Amount.ToString(), Language) + " " + JsonConvert.SerializeObject(res), FilePrefix);
															_common.InsertLog("GetGameUrl", "TrackerId", TrackerId, "Response", JsonConvert.SerializeObject(res), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Amount", Amount.ToString(), "Language", Language,
																								"DMinAmount", DMinAmount, "DMaxAmount", DMaxAmount, "WMinAmount", WMinAmount, "WMaxAmount", WMaxAmount);
															msg = $"FAILED: {UPlayerID} get {GameID} withdraw transfer limit failed for deposit rollback.";
															foreach (var phone in apiPhoneList.Split(','))
															{
																_common.SendSmsToBulkSms(phone, msg);
															}
															return new HttpResponseMessage()
															{
																StatusCode = HttpStatusCode.OK,
																Content = new StringContent(JsonConvert.SerializeObject(res), System.Text.Encoding.UTF8, "application/json")
															};
														}

														RollbackDeposit(totalAmt, (decimal)WMaxAmount, PlayerID, Game, Currency, Language, TrackerId, ref res, ref LockFromGame);
													}
												}
											}
											else
											{
												res.IsRefund = true;
												res.LockFromGame = false;
												_common.LogError($"{TrackerId}:GetGameUrl:SUCCESS - Initial Balance = End Balance, Skip Deposit Rollback " + string.Format("{0}|{1}|{2}|{3}|{4}", Game, PlayerID, Currency, Amount.ToString(), Language), FilePrefix);
												_common.InsertLog("GetGameUrl", "TrackerId", TrackerId, "Response", JsonConvert.SerializeObject(res), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Amount", Amount.ToString(), "Language", Language,
																					"DMinAmount", DMinAmount, "DMaxAmount", DMaxAmount, "WMinAmount", WMinAmount, "WMaxAmount", WMaxAmount);
											}
										}
										else
										{
											//need to call pp api to block player
											res.PlayerID = UPlayerID;
											res.Url = null;
											res.Game = GameID;
											res.Message = "Get end game balance failed for deposit rollback.";
											res.Code = 6;
											res.Type = GetGameType(GameID);
											res.LockFromGame = false; //no need block since already deduct his balance in main wallet first
											_common.LogError($"{TrackerId}:GetGameUrl:FAILED - Failed To Get Withdraw Transfer Limit For Deposit Rollback " + string.Format("{0}|{1}|{2}|{3}|{4}", Game, PlayerID, Currency, Amount.ToString(), Language) + " " + JsonConvert.SerializeObject(res), FilePrefix);
											_common.InsertLog("GetGameUrl", "TrackerId", TrackerId, "Response", JsonConvert.SerializeObject(res), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Amount", Amount.ToString(), "Language", Language,
																				"DMinAmount", DMinAmount, "DMaxAmount", DMaxAmount, "WMinAmount", WMinAmount, "WMaxAmount", WMaxAmount);
											msg = $"FAILED: {UPlayerID} get {GameID} end balance failed for deposit rollback.";
											foreach (var phone in apiPhoneList.Split(','))
											{
												_common.SendSmsToBulkSms(phone, msg);
											}
											return new HttpResponseMessage()
											{
												StatusCode = HttpStatusCode.OK,
												Content = new StringContent(JsonConvert.SerializeObject(res), System.Text.Encoding.UTF8, "application/json")
											};
										}
									}
								}
								else
								{
									res.PlayerID = UPlayerID;
									res.Url = null;
									res.Game = GameID;
									res.Message = "Get start game balance failed.";
									res.Code = 6;
									res.Type = GetGameType(GameID);
									res.IsRefund = true;
									res.LockFromGame = false;
									_common.LogError($"{TrackerId}:GetGameUrl:FAILED - Failed To Get Start Game Balance " + string.Format("{0}|{1}|{2}|{3}|{4}", Game, PlayerID, Currency, Amount.ToString(), Language) + " " + JsonConvert.SerializeObject(res), FilePrefix);
									_common.InsertLog("GetGameUrl", "TrackerId", TrackerId, "Response", JsonConvert.SerializeObject(res), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Amount", Amount.ToString(), "Language", Language,
										"DMinAmount", DMinAmount, "DMaxAmount", DMaxAmount, "WMinAmount", WMinAmount, "WMaxAmount", WMaxAmount);

									msg = $"FAILED: {UPlayerID} get {GameID} start balance failed.";
									foreach (var phone in apiPhoneList.Split(','))
									{
										_common.SendSmsToBulkSms(phone, msg);
									}
									return new HttpResponseMessage()
									{
										StatusCode = HttpStatusCode.OK,
										Content = new StringContent(JsonConvert.SerializeObject(res), System.Text.Encoding.UTF8, "application/json")
									};
								}
							}
							//normal condition
							else
							{
								_common.LogError($"{TrackerId}:GetGameUrl:SUCCESS - Deposit Normal Condition " + string.Format("{0}|{1}|{2}|{3}|{4}", Game, PlayerID, Currency, Amount.ToString(), Language), FilePrefix);

								decimal? initialGameBalance = GetGameBalance(GameID, UPlayerID, UCurrency, TrackerId);
								if (!initialGameBalance.Equals(null))
								{
									_common.LogError($"{TrackerId}:GetGameUrl:SUCCESS - Deposit Initial Balance: {initialGameBalance.ToString()} " + string.Format("{0}|{1}|{2}|{3}|{4}", Game, PlayerID, Currency, Amount.ToString(), Language), FilePrefix);

									canGetUrl = GameDeposit(GameID, UPlayerID, UCurrency, Amount, TrackerId, ref LockFromGame);

									if (canGetUrl == false)
									{
										_common.LogError($"{TrackerId}:GetGameUrl:FAILED - Deposit Failed, Proceed To Withdraw Rollback " + string.Format("{0}|{1}|{2}|{3}|{4}", Game, PlayerID, Currency, Amount.ToString(), Language), FilePrefix);

										decimal? failedGameBalance = GetGameBalance(GameID, UPlayerID, UCurrency, TrackerId);
										if (!failedGameBalance.Equals(null))
										{
											_common.LogError($"{TrackerId}:GetGameUrl:SUCCESS - End Balance: {failedGameBalance.ToString()} " + string.Format("{0}|{1}|{2}|{3}|{4}", Game, PlayerID, Currency, Amount.ToString(), Language), FilePrefix);

											if ((decimal)failedGameBalance - initialGameBalance > 0)
											{
												if ((decimal)failedGameBalance - initialGameBalance >= Amount)
												{
													_common.LogError($"{TrackerId}:GetGameUrl:SUCCESS - Start Withdraw Rollback " + string.Format("{0}|{1}|{2}|{3}|{4}", Game, PlayerID, Currency, Amount.ToString(), Language), FilePrefix);

													GetGameTransferLimit(GameID, "WITHDRAW", UCurrency, TrackerId, ref WMinAmount, ref WMaxAmount);

													if (WMinAmount.Equals(null) || WMaxAmount.Equals(null))
													{
														res.PlayerID = UPlayerID;
														res.Url = null;
														res.Game = GameID;
														res.Message = "Failed to get withdraw transfer limit.";
														res.Code = 7;
														res.LockFromGame = false;
														_common.LogError($"{TrackerId}:GetGameUrl:FAILED - Failed To Get Withdraw Transfer Limit " + string.Format("{0}|{1}|{2}|{3}|{4}", Game, PlayerID, Currency, Amount.ToString(), Language) + " " + JsonConvert.SerializeObject(res), FilePrefix);
														_common.InsertLog("GetGameUrl", "TrackerId", TrackerId, "Response", JsonConvert.SerializeObject(res), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Amount", Amount.ToString(), "Language", Language,
																"DMinAmount", DMinAmount, "DMaxAmount", DMaxAmount, "WMinAmount", WMinAmount, "WMaxAmount", WMaxAmount);
														return new HttpResponseMessage()
														{
															StatusCode = HttpStatusCode.OK,
															Content = new StringContent(JsonConvert.SerializeObject(res), System.Text.Encoding.UTF8, "application/json")
														};
													}

													RollbackDeposit(Amount, (decimal)WMaxAmount, PlayerID, Game, Currency, Language, TrackerId, ref res, ref LockFromGame);
												}
											}
											else
											{
												res.IsRefund = true;
												res.LockFromGame = false;
												_common.LogError($"{TrackerId}:GetGameUrl:SUCCESS - Initial Balance = End Balance, Skip Deposit Rollback " + string.Format("{0}|{1}|{2}|{3}|{4}", Game, PlayerID, Currency, Amount.ToString(), Language), FilePrefix);
											}
										}
										else
										{
											res.PlayerID = UPlayerID;
											res.Url = null;
											res.Game = GameID;
											res.Message = "Get end game balance failed.";
											res.Code = 6;
											res.Type = GetGameType(GameID);
											res.LockFromGame = false; //no need lock since already deduct his main wallet balance first
											_common.LogError($"{TrackerId}:GetGameUrl:FAILED - Failed To Get End Game Balance " + string.Format("{0}|{1}|{2}|{3}|{4}", Game, PlayerID, Currency, Amount.ToString(), Language) + " " + JsonConvert.SerializeObject(res), FilePrefix);
											_common.InsertLog("GetGameUrl", "TrackerId", TrackerId, "Response", JsonConvert.SerializeObject(res), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Amount", Amount.ToString(), "Language", Language,
																			"DMinAmount", DMinAmount, "DMaxAmount", DMaxAmount, "WMinAmount", WMinAmount, "WMaxAmount", WMaxAmount);
											msg = $"FAILED: {UPlayerID} get {GameID} end balance failed.";
											foreach (var phone in apiPhoneList.Split(','))
											{
												_common.SendSmsToBulkSms(phone, msg);
											}
											return new HttpResponseMessage()
											{
												StatusCode = HttpStatusCode.OK,
												Content = new StringContent(JsonConvert.SerializeObject(res), System.Text.Encoding.UTF8, "application/json")
											};
										}
									}
								}
								else
								{
									res.PlayerID = UPlayerID;
									res.Url = null;
									res.Game = GameID;
									res.Message = "Get start game balance failed.";
									res.Code = 6;
									res.Type = GetGameType(GameID);
									res.IsRefund = true;
									res.LockFromGame = false;
									_common.LogError($"{TrackerId}:GetGameUrl:FAILED - Failed To Get Start Game Balance " + string.Format("{0}|{1}|{2}|{3}|{4}", Game, PlayerID, Currency, Amount.ToString(), Language) + " " + JsonConvert.SerializeObject(res), FilePrefix);
									_common.InsertLog("GetGameUrl", "TrackerId", TrackerId, "Response", JsonConvert.SerializeObject(res), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Amount", Amount.ToString(), "Language", Language,
																		"DMinAmount", DMinAmount, "DMaxAmount", DMaxAmount, "WMinAmount", WMinAmount, "WMaxAmount", WMaxAmount);
									msg = $"FAILED: {UPlayerID} get {GameID} start balance failed.";
									foreach (var phone in apiPhoneList.Split(','))
									{
										_common.SendSmsToBulkSms(phone, msg);
									}
									return new HttpResponseMessage()
									{
										StatusCode = HttpStatusCode.OK,
										Content = new StringContent(JsonConvert.SerializeObject(res), System.Text.Encoding.UTF8, "application/json")
									};
								}
							}
						}

						if (canGetUrl)
						{
							//bg
							if (GameID == "BG")
							{
								var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																									 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

								if (record != null)
								{
									string lang = Language.ToUpper() == "ZH" ? "zh_CN" : "en_US";
									apiurl = string.Format(ConfigurationManager.AppSettings["BGApiUrl"] + "/Login?UID={0}&IP={1}&Language={2}", record.GamePlayerID, GetIPAddress(), lang);
									var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
									httpWebRequest.ContentType = "application/json";
									httpWebRequest.Method = "GET";
									httpWebRequest.Timeout = TimeoutSec;
									httpWebRequest.ReadWriteTimeout = TimeoutSec;
									var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

									using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
									{
										var responseText = streamReader.ReadToEnd();
										dynamic result = JObject.Parse(responseText);

										if (result.error == null)
										{
											res.PlayerID = UPlayerID;
											res.Url = Convert.ToString(result.result);
											res.Game = GameID;
											res.Type = GetGameType(GameID);
											res.Message = "Successfully get url.";
											res.Code = 0;
										}
										else
										{
											res.PlayerID = UPlayerID;
											res.Url = null;
											res.Game = GameID;
											res.Type = GetGameType(GameID);
											//res.Message = "Launch game api error.";
											res.Message = result.error.message;
											res.Code = 4;
										}
									}
								}
								else
								{
									res.PlayerID = UPlayerID;
									res.Url = null;
									res.Game = GameID;
									res.Type = GetGameType(GameID);
									res.Message = "Game account not found.";
									res.Code = 3;
								}
							}
							//ag
							else if (GameID == "AG")
							{
								var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																	 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format(ConfigurationManager.AppSettings["AGApiUrl"] + "/ForwardGame?loginname={0}&currency={1}&lang={2}", record.GamePlayerID, Currency, Language);
									var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
									httpWebRequest.ContentType = "application/json";
									httpWebRequest.Method = "GET";
									httpWebRequest.Timeout = TimeoutSec;
									httpWebRequest.ReadWriteTimeout = TimeoutSec;
									var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

									using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
									{
										var responseText = streamReader.ReadToEnd();
										dynamic result = JObject.Parse(responseText);

										if (Convert.ToString(result.ErrorCode) == "0")
										{
											res.PlayerID = UPlayerID;
											res.Url = Convert.ToString(result.ErrorDesc);
											res.Game = GameID;
											res.Type = GetGameType(GameID);
											res.Message = "Successfully get url.";
											res.Code = 0;
										}
										else
										{
											res.PlayerID = UPlayerID;
											res.Url = null;
											res.Game = GameID;
											res.Type = GetGameType(GameID);
											//res.Message = "Launch game api error.";
											res.Message = result.ErrorDesc;
											res.Code = 4;
										}
									}
								}
								else
								{
									res.PlayerID = UPlayerID;
									res.Url = null;
									res.Game = GameID;
									res.Type = GetGameType(GameID);
									res.Message = "Game account not found.";
									res.Code = 3;
								}
							}
							//playtech
							else if (GameID == "PT")
							{
								var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																									 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

								if (record != null)
								{
									string lang = Language.ToUpper() == "ZH" ? "ZH-CN" : "EN";
									apiurl = string.Format(ConfigurationManager.AppSettings["PTApiUrl"] + "/LaunchGame?playerName={0}&lang={1}", record.GamePlayerID, lang);
									var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
									httpWebRequest.ContentType = "application/json";
									httpWebRequest.Method = "GET";
									httpWebRequest.Timeout = TimeoutSec;
									httpWebRequest.ReadWriteTimeout = TimeoutSec;
									var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

									using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
									{
										var responseText = streamReader.ReadToEnd();
										dynamic result = JObject.Parse(responseText);

										if (result.errorcode == 0)
										{
											res.PlayerID = UPlayerID;
											res.Url = Convert.ToString(result.error);
											res.Game = GameID;
											res.Type = GetGameType(GameID);
											res.Message = "Successfully get url.";
											res.Code = 0;
										}
										else
										{
											res.PlayerID = UPlayerID;
											res.Url = null;
											res.Game = GameID;
											res.Type = GetGameType(GameID);
											//res.Message = "Launch game api error.";
											res.Message = result.error;
											res.Code = 4;
										}
									}
								}
								else
								{
									res.PlayerID = UPlayerID;
									res.Url = null;
									res.Game = GameID;
									res.Type = GetGameType(GameID);
									res.Message = "Game account not found.";
									res.Code = 3;
								}
							}
							//play8oy
							else if (GameID == "IG")
							{
								var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																									 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format(ConfigurationManager.AppSettings["IGApiUrl"] + "/GetUrl?UID={0}&IP={1}", record.GamePlayerID, GetIPAddress());
									var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
									httpWebRequest.ContentType = "application/json";
									httpWebRequest.Method = "GET";
									httpWebRequest.Timeout = TimeoutSec;
									httpWebRequest.ReadWriteTimeout = TimeoutSec;
									var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

									using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
									{
										var responseText = streamReader.ReadToEnd();
										dynamic result = JObject.Parse(responseText);

										if (Convert.ToString(result.Code) == "0")
										{
											string passwords = "BT8989bt";
											string prefix = "bt_";
											res.PlayerID = UPlayerID;
											res.Game = GameID;
											res.Message = "Successfully get url.";
											res.Code = 0;
											res.Url = null;
											res.Type = GetGameType(GameID);
											res.ApkInfo = new GameApkVM()
											{
												Android = new GameApkInfoVM()
												{
													DownloadLink = "https://pb168api.gotiger888.com/app/",
													Package = null,
													Activity = null,
													DeepLink = $"lobbyplay8oy://lobbyplay8oy?account={prefix + record.GamePlayerID}&password={passwords}",
													GamePlayerID = record.GamePlayerID,
													Passwords = passwords
												},
												iOS = new GameApkInfoVM()
												{
													DownloadLink = "https://pb168api.gotiger888.com/app/",
													Package = null,
													Activity = null,
													DeepLink = $"lobbyplay8oy://lobbyplay8oy?account={prefix + record.GamePlayerID}&password={passwords}",
													GamePlayerID = record.GamePlayerID,
													Passwords = passwords
												}
											};
										}
										else
										{
											res.PlayerID = UPlayerID;
											res.Game = GameID;
											//res.Message = "Launch game api error.";
											res.Message = result.Message;
											res.Code = 3;
											res.Type = GetGameType(GameID);
											res.Url = null;
											res.ApkInfo = null;
										}
									}
								}
								else
								{
									res.PlayerID = UPlayerID;
									res.Game = GameID;
									res.Message = "Game account not found.";
									res.Code = 3;
									res.Type = GetGameType(GameID);
									res.Url = null;
									res.ApkInfo = null;
								}
							}
							//28win
							else if (GameID == "BT28WIN")
							{
								var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																									 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format(ConfigurationManager.AppSettings["BT28WINApiUrl"] + "/Bet?user={0}&pass={1}", record.GamePlayerID, "123456789");
									var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
									httpWebRequest.ContentType = "application/json";
									httpWebRequest.Method = "GET";
									httpWebRequest.Timeout = TimeoutSec;
									httpWebRequest.ReadWriteTimeout = TimeoutSec;

									var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

									using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
									{
										var responseText = streamReader.ReadToEnd();
										dynamic result = JObject.Parse(responseText);

										if (Convert.ToString(result.ErrorCode) == "0")
										{
											res.PlayerID = UPlayerID;
											res.Url = Convert.ToString(result.Data);
											res.Game = GameID;
											res.Message = "Successfully get url.";
											res.Code = 0;
											res.Type = GetGameType(GameID);
										}
										else
										{
											res.PlayerID = UPlayerID;
											res.Url = null;
											res.Game = GameID;
											//res.Message = result.ErrorDesc;
											//res.Message = "Launch game api error.";
											res.Code = 4;
											res.Type = GetGameType(GameID);
										}
									}
								}
								else
								{
									res.PlayerID = UPlayerID;
									res.Url = null;
									res.Game = GameID;
									res.Message = "Game account not found.";
									res.Code = 3;
									res.Type = GetGameType(GameID);
								}
							}
							//allcity
							else if (GameID == "ALLCITY")
							{
								var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																									 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format(ConfigurationManager.AppSettings["ALLCITYApiUrl"] + "/GetGameApk");
									var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
									httpWebRequest.ContentType = "application/json";
									httpWebRequest.Method = "GET";
									httpWebRequest.Timeout = TimeoutSec;
									httpWebRequest.ReadWriteTimeout = TimeoutSec;
									var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

									using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
									{
										var responseText = streamReader.ReadToEnd();
										dynamic result = JObject.Parse(responseText);

										if (result.code == 200)
										{
											string passwords = "Bt123456";
											string prefix = "ML";
											res.PlayerID = UPlayerID;
											res.Url = null;
											res.Game = GameID;
											res.Message = "Successfully get url.";
											res.Code = 0;
											res.Url = null;
											res.Type = GetGameType(GameID);
											res.ApkInfo = new GameApkVM()
											{
												Android = new GameApkInfoVM()
												{
													DownloadLink = Convert.ToString(result.apk),
													Package = Convert.ToString(result.package),
													Activity = Convert.ToString(result.activity),
													DeepLink = $"jfg://com.jfgame.tcyl?account={prefix + record.GamePlayerID}&password={passwords}",
													GamePlayerID = prefix + record.GamePlayerID,
													Passwords = passwords
												},
												iOS = new GameApkInfoVM()
												{
													DownloadLink = Convert.ToString(result.apk),
													Package = null,
													Activity = null,
													DeepLink = $"jfgtcyl2://account={prefix + record.GamePlayerID}&password={passwords}",
													GamePlayerID = prefix + record.GamePlayerID,
													Passwords = passwords
												}
											};
										}
										else
										{
											res.PlayerID = UPlayerID;
											res.Url = null;
											res.Game = GameID;
											//res.Message = "Launch game api error.";
											res.Message = result.msg;
											res.Code = 4;
											res.Type = GetGameType(GameID);
											res.ApkInfo = null;
										}
									}
								}
								else
								{
									res.PlayerID = UPlayerID;
									res.Url = null;
									res.Game = GameID;
									res.Message = "Game account not found.";
									res.Code = 3;
									res.Type = GetGameType(GameID);
									res.ApkInfo = null;
								}
							}
							//ibc
							else if (GameID == "IBC")
							{
								var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																									 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

								if (record != null)
								{
									string lang = Language.ToUpper() == "ZH" ? "cs" : "en";

									apiurl = string.Format(ConfigurationManager.AppSettings["IBCApiUrl"] + "/LaunchGame?username={0}&lang={1}", record.GamePlayerID, lang);
									var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
									httpWebRequest.ContentType = "application/json";
									httpWebRequest.Method = "GET";
									httpWebRequest.Timeout = TimeoutSec;
									httpWebRequest.ReadWriteTimeout = TimeoutSec;
									var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

									using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
									{
										var responseText = streamReader.ReadToEnd();
										dynamic result = JObject.Parse(responseText);

										if (result.error_code == 0)
										{
											res.PlayerID = UPlayerID;
											res.Url = Convert.ToString(result.Data);
											res.Game = GameID;
											res.Code = 0;
											res.Message = "Successfully get url.";
											res.Type = GetGameType(GameID);
										}
										else
										{
											res.PlayerID = UPlayerID;
											res.Url = null;
											res.Game = GameID;
											res.Code = 4;
											//res.Message = "Launch game api error.";
											res.Message = result.message;
											res.Type = GetGameType(GameID);
										}
									}
								}
								else
								{
									res.PlayerID = UPlayerID;
									res.Url = null;
									res.Game = GameID;
									res.Code = 3;
									res.Message = "Game account not found.";
									res.Type = GetGameType(GameID);
								}
							}
							//evo888
							else if (GameID == "EVO888")
							{
								var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																									 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format(ConfigurationManager.AppSettings["EVO888ApiUrl"] + "/GetGameApk");
									var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
									httpWebRequest.ContentType = "application/json";
									httpWebRequest.Method = "GET";
									httpWebRequest.Timeout = TimeoutSec;
									httpWebRequest.ReadWriteTimeout = TimeoutSec;
									var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

									using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
									{
										var responseText = streamReader.ReadToEnd();
										dynamic result = JObject.Parse(responseText);

										if (result.code == 0 && Convert.ToString(result.success).Trim().ToUpper() == "TRUE")
										{
											string passwords = "Bt123456";

											string adeeplink = $"evo888android://lobbyevoandroid?account={record.GamePlayerID.Trim()}&password={passwords}​";
											while (!adeeplink.EndsWith(passwords))
												adeeplink = adeeplink.Remove(adeeplink.Length - 1, 1);

											string ideeplink = $"evo888ios://lobbyevoios?account=​{record.GamePlayerID.Trim()}​&password=​{passwords}";
											while (!ideeplink.EndsWith(passwords))
												ideeplink = ideeplink.Remove(ideeplink.Length - 1, 1);


											res.PlayerID = UPlayerID;
											res.Url = Convert.ToString(result.apk);
											res.Game = GameID;
											res.Code = 0;
											res.Message = "Successfully get url.";
											res.Type = GetGameType(GameID);
											res.ApkInfo = new GameApkVM()
											{
												Android = new GameApkInfoVM()
												{
													DownloadLink = Convert.ToString(result.apk),
													Package = null,
													Activity = null,
													DeepLink = adeeplink,
													GamePlayerID = record.GamePlayerID,
													Passwords = passwords
												},
												iOS = new GameApkInfoVM()
												{
													DownloadLink = Convert.ToString(result.apk),
													Package = null,
													Activity = null,
													DeepLink = ideeplink,
													GamePlayerID = record.GamePlayerID,
													Passwords = passwords
												}
											};
										}
										else
										{
											res.PlayerID = UPlayerID;
											res.Url = null;
											res.Game = GameID;
											//res.Message = "Launch game api error.";
											res.Message = result.msg;
											res.Code = 4;
											res.Type = GetGameType(GameID);
										}
									}
								}
								else
								{
									res.PlayerID = UPlayerID;
									res.Url = null;
									res.Game = GameID;
									res.Message = "Game account not found.";
									res.Code = 3;
									res.Type = GetGameType(GameID);
								}
							}
							//vpower
							else if (GameID == "VPOWER")
							{
								var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																									 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format(ConfigurationManager.AppSettings["VPOWERApiUrl"] + "/LaunchLobby?username={0}&lang={1}",
																					record.GamePlayerID, Language);
									var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
									httpWebRequest.ContentType = "application/json";
									httpWebRequest.Method = "GET";
									httpWebRequest.Timeout = TimeoutSec;
									httpWebRequest.ReadWriteTimeout = TimeoutSec;
									var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

									using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
									{
										var responseText = streamReader.ReadToEnd();
										dynamic result = JObject.Parse(responseText);

										if (result.ErrorCode == 0)
										{
											res.PlayerID = UPlayerID;
											res.Url = Convert.ToString(result.Url);
											res.Game = GameID;
											res.Message = "Successfully get url.";
											res.Code = 0;
											res.Type = GetGameType(GameID);
										}
										else
										{
											res.PlayerID = UPlayerID;
											res.Url = null;
											res.Game = GameID;
											//res.Message = "Launch game api error.";
											res.Message = result.message;
											res.Code = 4;
											res.Type = GetGameType(GameID);
										}
									}
								}
								else
								{
									res.PlayerID = UPlayerID;
									res.Url = null;
									res.Game = GameID;
									res.Message = "Game account not found.";
									res.Code = 3;
									res.Type = GetGameType(GameID);
								}
							}
							//sexygaming
							else if (GameID == "SEXYGAMING")
							{
								var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																									 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

								if (record != null)
								{
									string lang = Language.ToUpper() == "ZH" ? "zh-cn" : "en-us";
									apiurl = string.Format(ConfigurationManager.AppSettings["SEXYGAMINGApiUrl"] + "/LaunchGame?username={0}&lang={1}", record.GamePlayerID, lang);
									var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
									httpWebRequest.ContentType = "application/json";
									httpWebRequest.Method = "GET";
									httpWebRequest.Timeout = TimeoutSec;
									httpWebRequest.ReadWriteTimeout = TimeoutSec;
									var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

									using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
									{
										var responseText = streamReader.ReadToEnd();
										dynamic result = JObject.Parse(responseText);

										if (Convert.ToInt16(result.error) == 0 && Convert.ToString(result.message).ToUpper() == "SUCCESS")
										{
											res.PlayerID = UPlayerID;
											res.Url = Convert.ToString(result.data);
											res.Game = GameID;
											res.Message = "Successfully get url.";
											res.Code = 0;
											res.Type = GetGameType(GameID);
										}
										else
										{
											res.PlayerID = UPlayerID;
											res.Url = null;
											res.Game = GameID;
											//res.Message = "Launch game api error.";
											res.Message = result.ErrorDesc;
											res.Code = 4;
											res.Type = GetGameType(GameID);
										}
									}
								}
								else
								{
									res.PlayerID = UPlayerID;
									res.Url = null;
									res.Game = GameID;
									res.Message = "Game account not found.";
									res.Code = 3;
									res.Type = GetGameType(GameID);
								}
							}
							//spadegaming
							else if (GameID == "SPADEGAMING")
							{
								var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																									 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

								if (record != null)
								{
									string lang = Language.ToUpper() == "ZH" ? "zh_CN" : "en_US";
									apiurl = string.Format(ConfigurationManager.AppSettings["SPADEGAMINGApiUrl"] + "/LaunchGame?username={0}&mcUID={1}&lang={2}", record.GamePlayerID, record.PlayerID, lang);
									var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
									httpWebRequest.ContentType = "application/json";
									httpWebRequest.Method = "GET";
									httpWebRequest.Timeout = TimeoutSec;
									httpWebRequest.ReadWriteTimeout = TimeoutSec;
									var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

									using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
									{
										var responseText = streamReader.ReadToEnd();
										dynamic result = JObject.Parse(responseText);

										if (Convert.ToInt16(result.code) == 0 && Convert.ToString(result.msg).ToUpper() == "SUCCESS")
										{
											res.PlayerID = UPlayerID;
											res.Url = Convert.ToString(result.url);
											res.Game = GameID;
											res.Message = "Successfully get url.";
											res.Code = 0;
											res.Type = GetGameType(GameID);
										}
										else
										{
											res.PlayerID = UPlayerID;
											res.Url = null;
											res.Game = GameID;
											//res.Message = "Launch game api error.";
											res.Message = result.ErrorDesc;
											res.Code = 4;
											res.Type = GetGameType(GameID);
										}
									}
								}
								else
								{
									res.PlayerID = UPlayerID;
									res.Url = null;
									res.Game = GameID;
									res.Message = "Game account not found.";
									res.Code = 3;
									res.Type = GetGameType(GameID);
								}
							}
							//mega
							else if (GameID == "MEGA")
							{
								var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																									 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format(ConfigurationManager.AppSettings["MEGAApiUrl"] + "/GetGameApk");
									var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
									httpWebRequest.ContentType = "application/json";
									httpWebRequest.Method = "GET";
									httpWebRequest.Timeout = TimeoutSec;
									httpWebRequest.ReadWriteTimeout = TimeoutSec;
									var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

									using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
									{
										var responseText = streamReader.ReadToEnd();
										dynamic result = JObject.Parse(responseText);

										if (result.result != null)
										{
											//string deeplink = "lobbymegarelease://lobbymegarelease";
											string deeplink = null;
											string package = "com.mega888.Release";

											res.PlayerID = UPlayerID;
											res.Url = Convert.ToString(result.result);
											res.Game = GameID;
											res.Code = 0;
											res.Message = "Successfully get url.";
											res.Type = GetGameType(GameID);
											res.ApkInfo = new GameApkVM()
											{
												Android = new GameApkInfoVM()
												{
													DownloadLink = Convert.ToString(result.result),
													Package = package,
													Activity = null,
													DeepLink = deeplink,
													GamePlayerID = record.GamePlayerID,
													Passwords = null
												},
												iOS = new GameApkInfoVM()
												{
													DownloadLink = Convert.ToString(result.result),
													Package = null,
													Activity = null,
													DeepLink = "lobbymegarelease://lobbymegarelease",
													GamePlayerID = record.GamePlayerID,
													Passwords = null
												}
											};
										}
										else
										{
											res.PlayerID = UPlayerID;
											res.Url = null;
											res.Game = GameID;
											//res.Message = "Launch game api error.";
											res.Message = result.error.message;
											res.Code = 4;
											res.Type = GetGameType(GameID);
										}
									}
								}
								else
								{
									res.PlayerID = UPlayerID;
									res.Url = null;
									res.Game = GameID;
									res.Message = "Game account not found.";
									res.Code = 3;
									res.Type = GetGameType(GameID);
								}
							}
							//evolution
							else if (GameID == "EVOLUTIONGAMING")
							{
								var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																									 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format(ConfigurationManager.AppSettings["EVOLUTIONGAMINGApiUrl"] + "/UserAuth?username={0}&mcUID={1}", record.GamePlayerID, record.PlayerID);
									var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
									httpWebRequest.ContentType = "application/json";
									httpWebRequest.Method = "GET";
									httpWebRequest.Timeout = TimeoutSec;
									httpWebRequest.ReadWriteTimeout = TimeoutSec;
									var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

									using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
									{
										var responseText = streamReader.ReadToEnd();
										dynamic result = JObject.Parse(responseText);

										if (result.entry != null)
										{
											res.PlayerID = UPlayerID;
											res.Url = Convert.ToString(result.entry);
											res.Game = GameID;
											res.Message = "Successfully get url.";
											res.Code = 0;
											res.Type = GetGameType(GameID);
										}
										else
										{
											res.PlayerID = UPlayerID;
											res.Url = null;
											res.Game = GameID;
											//res.Message = "Launch game api error.";
											res.Message = result.errors.message;
											res.Code = 4;
											res.Type = GetGameType(GameID);
										}
									}
								}
								else
								{
									res.PlayerID = UPlayerID;
									res.Url = null;
									res.Game = GameID;
									res.Message = "Game account not found.";
									res.Code = 3;
									res.Type = GetGameType(GameID);
								}
							}
							//menang88
							else if (GameID == "MENANG88")
							{
								var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																									 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

								if (record != null)
								{
									string lang = Language.ToUpper() == "ZH" ? "cn" : "en";
									apiurl = string.Format(ConfigurationManager.AppSettings["MENANG88ApiUrl"] + "/LaunchGame?username={0}&mcUID={1}&lang={2}", record.GamePlayerID, record.PlayerID, lang);
									var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
									httpWebRequest.ContentType = "application/json";
									httpWebRequest.Method = "GET";
									httpWebRequest.Timeout = TimeoutSec;
									httpWebRequest.ReadWriteTimeout = TimeoutSec;
									var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

									using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
									{
										var responseText = streamReader.ReadToEnd();
										dynamic result = JObject.Parse(responseText);

										if (Convert.ToString(result.status) == "1")
										{
											res.PlayerID = UPlayerID;
											res.Url = Convert.ToString(result.url);
											res.Game = GameID;
											res.Message = "Successfully get url.";
											res.Code = 0;
											res.Type = GetGameType(GameID);
										}
										else
										{
											res.PlayerID = UPlayerID;
											res.Url = null;
											res.Game = GameID;
											res.Message = result.message;
											//res.Message = "Launch game api error.";
											res.Code = 4;
											res.Type = GetGameType(GameID);
										}
									}
								}
								else
								{
									res.PlayerID = UPlayerID;
									res.Url = null;
									res.Game = GameID;
									res.Message = "Game account not found.";
									res.Code = 3;
									res.Type = GetGameType(GameID);
								}
							}
							//xe88
							else if (GameID == "XE88")
							{
								var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																									 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

								if (record != null)
								{
									string lang = Language.ToUpper() == "ZH" ? "CN" : "En";
									apiurl = string.Format(ConfigurationManager.AppSettings["XE88ApiUrl"] + "/LaunchGame?username={0}&lang={1}", record.GamePlayerID, lang);
									var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
									httpWebRequest.ContentType = "application/json";
									httpWebRequest.Method = "GET";
									httpWebRequest.Timeout = TimeoutSec;
									httpWebRequest.ReadWriteTimeout = TimeoutSec;
									var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

									using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
									{
										var responseText = streamReader.ReadToEnd();
										dynamic result = JObject.Parse(responseText);

										if (Convert.ToString(result.code) == "0")
										{
											res.PlayerID = UPlayerID;
											res.Url = Convert.ToString(result.url);
											res.Game = GameID;
											res.Message = "Successfully get url.";
											res.Code = 0;
											res.Type = GetGameType(GameID);
										}
										else
										{
											res.PlayerID = UPlayerID;
											res.Url = null;
											res.Game = GameID;
											res.Message = result.message;
											//res.Message = "Launch game api error.";
											res.Code = 4;
											res.Type = GetGameType(GameID);
										}
									}
								}
								else
								{
									res.PlayerID = UPlayerID;
									res.Url = null;
									res.Game = GameID;
									res.Message = "Game account not found.";
									res.Code = 3;
									res.Type = GetGameType(GameID);
								}
							}
							//kypoker
							else if (GameID == "KYPOKER")
							{
								var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																									 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

								if (record != null)
								{
									string lang = Language.ToUpper() == "ZH" ? "CN" : "En";
									apiurl = string.Format(ConfigurationManager.AppSettings["KYPOKERApiUrl"] + "/LaunchGame?username={0}&lang={1}", record.GamePlayerID, lang);
									var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
									httpWebRequest.ContentType = "application/json";
									httpWebRequest.Method = "GET";
									httpWebRequest.Timeout = TimeoutSec;
									httpWebRequest.ReadWriteTimeout = TimeoutSec;
									var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

									using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
									{
										var responseText = streamReader.ReadToEnd();
										dynamic result = JObject.Parse(responseText);

										if (result.d != null)
										{
											if (Convert.ToString(result.d.code) == "0")
											{
												res.PlayerID = UPlayerID;
												res.Url = Convert.ToString(result.d.url);
												res.Game = GameID;
												res.Message = "Successfully get url.";
												res.Code = 0;
												res.Type = GetGameType(GameID);
											}
											else
											{
												res.PlayerID = UPlayerID;
												res.Url = null;
												res.Game = GameID;
												res.Message = result.d.code;
												//res.Message = "Launch game api error.";
												res.Code = 4;
												res.Type = GetGameType(GameID);
											}
										}
										else
										{
											res.PlayerID = UPlayerID;
											res.Url = null;
											res.Game = GameID;
											//res.Message = result.message;
											res.Message = "Launch game api error.";
											res.Code = 4;
											res.Type = GetGameType(GameID);
										}
									}
								}
								else
								{
									res.PlayerID = UPlayerID;
									res.Url = null;
									res.Game = GameID;
									res.Message = "Game account not found.";
									res.Code = 3;
									res.Type = GetGameType(GameID);
								}
							}
							//sabaesports
							else if (GameID == "SABAESPORTS")
							{
								var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																									 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

								if (record != null)
								{
									string lang = Language.ToUpper() == "ZH" ? "cs" : "en";

									apiurl = string.Format(ConfigurationManager.AppSettings["SABAESPORTSApiUrl"] + "/LaunchGame?username={0}&lang={1}", record.GamePlayerID, lang);
									var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
									httpWebRequest.ContentType = "application/json";
									httpWebRequest.Method = "GET";
									httpWebRequest.Timeout = TimeoutSec;
									httpWebRequest.ReadWriteTimeout = TimeoutSec;
									var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

									using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
									{
										var responseText = streamReader.ReadToEnd();
										dynamic result = JObject.Parse(responseText);

										if (result.error_code == 0)
										{
											res.PlayerID = UPlayerID;
											res.Url = Convert.ToString(result.Data);
											res.Game = GameID;
											res.Code = 0;
											res.Message = "Successfully get url.";
											res.Type = GetGameType(GameID);
										}
										else
										{
											res.PlayerID = UPlayerID;
											res.Url = null;
											res.Game = GameID;
											res.Code = 4;
											//res.Message = "Launch game api error.";
											res.Message = result.message;
											res.Type = GetGameType(GameID);
										}
									}
								}
								else
								{
									res.PlayerID = UPlayerID;
									res.Url = null;
									res.Game = GameID;
									res.Code = 3;
									res.Message = "Game account not found.";
									res.Type = GetGameType(GameID);
								}
							}
							//tpg
							else if (GameID == "TPG")
							{
								var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																									 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

								if (record != null)
								{
									string lang = Language.ToUpper() == "ZH" ? "zh-ch" : "en";
									apiurl = string.Format(ConfigurationManager.AppSettings["TPGApiUrl"] + "/LaunchLobby?playerName={0}&displayName={1}&currency={2}&lang={3}",
																					record.GamePlayerID, UPlayerID, UCurrency, lang);
									var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
									httpWebRequest.ContentType = "application/json";
									httpWebRequest.Method = "GET";
									httpWebRequest.Timeout = TimeoutSec;
									httpWebRequest.ReadWriteTimeout = TimeoutSec;
									var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

									using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
									{
										var responseText = streamReader.ReadToEnd();
										dynamic result = JObject.Parse(responseText);

										if (result.status == 1)
										{
											res.PlayerID = UPlayerID;
											res.Url = Convert.ToString(result.message);
											res.Game = GameID;
											res.Message = "Successfully get url.";
											res.Code = 0;
											res.Type = GetGameType(GameID);
										}
										else
										{
											res.PlayerID = UPlayerID;
											res.Url = null;
											res.Game = GameID;
											res.Message = result.message;
											res.Code = 4;
											res.Type = GetGameType(GameID);
										}
									}
								}
								else
								{
									res.PlayerID = UPlayerID;
									res.Url = null;
									res.Game = GameID;
									res.Message = "Game account not found.";
									res.Code = 3;
									res.Type = GetGameType(GameID);
								}
							}
							//tpg
							else if (GameID == "TPGFISH")
							{
								var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																									 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

								if (record != null)
								{
									string lang = Language.ToUpper() == "ZH" ? "zh-ch" : "en";
									apiurl = string.Format(ConfigurationManager.AppSettings["TPGFISHApiUrl"] + "/LaunchLobby?playerName={0}&displayName={1}&currency={2}&lang={3}",
																					record.GamePlayerID, UPlayerID, UCurrency, lang);
									var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
									httpWebRequest.ContentType = "application/json";
									httpWebRequest.Method = "GET";
									httpWebRequest.Timeout = TimeoutSec;
									httpWebRequest.ReadWriteTimeout = TimeoutSec;
									var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

									using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
									{
										var responseText = streamReader.ReadToEnd();
										dynamic result = JObject.Parse(responseText);

										if (result.status == 1)
										{
											res.PlayerID = UPlayerID;
											res.Url = Convert.ToString(result.message);
											res.Game = GameID;
											res.Message = "Successfully get url.";
											res.Code = 0;
											res.Type = GetGameType(GameID);
										}
										else
										{
											res.PlayerID = UPlayerID;
											res.Url = null;
											res.Game = GameID;
											res.Message = result.message;
											res.Code = 4;
											res.Type = GetGameType(GameID);
										}
									}
								}
								else
								{
									res.PlayerID = UPlayerID;
									res.Url = null;
									res.Game = GameID;
									res.Message = "Game account not found.";
									res.Code = 3;
									res.Type = GetGameType(GameID);
								}
							}
							//vgaming
							else if (GameID == "VGAMING")
							{
								var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																									 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

								if (record != null)
								{
									string lang = Language.ToUpper() == "ZH" ? "cs" : "en";

									apiurl = string.Format(ConfigurationManager.AppSettings["VGAMINGApiUrl"] + "/LaunchGame?username={0}&lang={1}", record.GamePlayerID, lang);
									var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
									httpWebRequest.ContentType = "application/json";
									httpWebRequest.Method = "GET";
									httpWebRequest.Timeout = TimeoutSec;
									httpWebRequest.ReadWriteTimeout = TimeoutSec;
									var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

									using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
									{
										var responseText = streamReader.ReadToEnd();
										dynamic result = JObject.Parse(responseText);

										if (result.error_code == 0)
										{
											res.PlayerID = UPlayerID;
											res.Url = Convert.ToString(result.Data);
											res.Game = GameID;
											res.Code = 0;
											res.Message = "Successfully get url.";
											res.Type = GetGameType(GameID);
										}
										else
										{
											res.PlayerID = UPlayerID;
											res.Url = null;
											res.Game = GameID;
											res.Code = 4;
											//res.Message = "Launch game api error.";
											res.Message = result.message;
											res.Type = GetGameType(GameID);
										}
									}
								}
								else
								{
									res.PlayerID = UPlayerID;
									res.Url = null;
									res.Game = GameID;
									res.Code = 3;
									res.Message = "Game account not found.";
									res.Type = GetGameType(GameID);
								}
							}
							//cq9
							else if (GameID == "CQ9")
							{
								var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																									 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

								if (record != null)
								{
									string lang = Language.ToUpper() == "ZH" ? "zh-cn" : "en";
									apiurl = string.Format(ConfigurationManager.AppSettings["CQ9ApiUrl"] + "/LaunchGame?username={0}&lang={1}", record.GamePlayerID, lang);
									var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
									httpWebRequest.ContentType = "application/json";
									httpWebRequest.Method = "GET";
									httpWebRequest.Timeout = TimeoutSec;
									httpWebRequest.ReadWriteTimeout = TimeoutSec;
									var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

									using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
									{
										var responseText = streamReader.ReadToEnd();
										dynamic result = JObject.Parse(responseText);

										if (result.status != null)
										{
											if (Convert.ToString(result.status.code) == "0")
											{
												if (result.data != null)
												{
													res.PlayerID = UPlayerID;
													res.Url = Convert.ToString(result.data.url);
													res.Game = GameID;
													res.Message = "Successfully get url.";
													res.Code = 0;
													res.Type = GetGameType(GameID);
												}
												else
												{
													res.PlayerID = UPlayerID;
													res.Url = null;
													res.Game = GameID;
													res.Message = result.status.message;
													res.Code = 4;
													res.Type = GetGameType(GameID);
												}
											}
											else
											{
												res.PlayerID = UPlayerID;
												res.Url = null;
												res.Game = GameID;
												res.Message = result.status.message;
												res.Code = 4;
												res.Type = GetGameType(GameID);
											}
										}
										else
										{
											res.PlayerID = UPlayerID;
											res.Url = null;
											res.Game = GameID;
											res.Message = "Unknown error";
											res.Code = 3;
											res.Type = GetGameType(GameID);
										}
									}
								}
								else
								{
									res.PlayerID = UPlayerID;
									res.Url = null;
									res.Game = GameID;
									res.Message = "Game account not found.";
									res.Code = 3;
									res.Type = GetGameType(GameID);
								}
							}
							//default
							else
							{
								res.PlayerID = UPlayerID;
								res.Game = GameID;
								res.Message = "Invalid game.";
								res.Code = 8;
								res.Url = null;
								res.Type = GetGameType(GameID);
								_common.LogError($"{TrackerId}:GetGameUrl:FAILED - Invalid Game " + string.Format("{0}|{1}|{2}|{3}|{4}", Game, PlayerID, Currency, Amount.ToString(), Language) + " " + JsonConvert.SerializeObject(res), FilePrefix);
							}

							if (res.Code != 0)
							{
								_common.LogError($"{TrackerId}:GetGameUrl:FAILED - Failed To Get Launch Game Url " + string.Format("{0}|{1}|{2}|{3}|{4}", Game, PlayerID, Currency, Amount.ToString(), Language) + " " + JsonConvert.SerializeObject(res), FilePrefix);
								msg = $"FAILED: {UPlayerID} get {GameID} game url failed, balance remain in game.";
								res.LockFromGame = false;
								//res.IsRefund = false;
								foreach (var phone in apiPhoneList.Split(','))
								{
									_common.SendSmsToBulkSms(phone, msg);
								}
								//RollbackDeposit(Amount, (decimal)WMaxAmount, PlayerID, Game, Currency, Language, TrackerId, ref res, ref LockFromGame);
							}

						}
						else
						{
							res.PlayerID = UPlayerID;
							res.Url = null;
							res.Game = GameID;
							res.Message = "Game deposit failed.";
							res.Code = 2;
							res.Type = GetGameType(GameID);
							_common.LogError($"{TrackerId}:GetGameUrl:FAILED - Game Deposit Failed " + string.Format("{0}|{1}|{2}|{3}|{4}", Game, PlayerID, Currency, Amount.ToString(), Language), FilePrefix);
							msg = $"FAILED: {UPlayerID} deposit {Amount.ToString()} to {GameID} failed.";
							foreach (var phone in apiPhoneList.Split(','))
							{
								_common.SendSmsToBulkSms(phone, msg);
							}
						}

						_common.InsertLog("GetGameUrl", "TrackerId", TrackerId, "Response", JsonConvert.SerializeObject(res), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Amount", Amount.ToString(), "Language", Language,
														"DMinAmount", DMinAmount, "DMaxAmount", DMaxAmount, "WMinAmount", WMinAmount, "WMaxAmount", WMaxAmount);

						return new HttpResponseMessage()
						{
							StatusCode = HttpStatusCode.OK,
							Content = new StringContent(JsonConvert.SerializeObject(res), System.Text.Encoding.UTF8, "application/json")
						};
					}
				}
				catch (Exception ex)
				{
					_common.LogError($"{TrackerId}:GetGameUrl:EXCEPTION " + string.Format("{0}|{1}|{2}|{3}|{4}", Game, PlayerID, Currency, Amount.ToString(), Language) + " " + ex.Message.ToString(), FilePrefix);
					_common.InsertLog("GetGameUrl", "TrackerId", TrackerId, "Exception", ex.Message.ToString(), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Amount", Amount.ToString(), "Language", Language,
														"DMinAmount", DMinAmount, "DMaxAmount", DMaxAmount, "WMinAmount", WMinAmount, "WMaxAmount", WMaxAmount);
					dynamic res = new ExpandoObject();
					res.Code = -9999;
					res.Message = ex.Message.ToString();
					return new HttpResponseMessage()
					{
						StatusCode = HttpStatusCode.InternalServerError,
						Content = new StringContent(JsonConvert.SerializeObject(res), System.Text.Encoding.UTF8, "application/json")
					};
				}
			}
		}

		[HttpGet]
		public HttpResponseMessage GetGameRecord(string Game, string PlayerID, DateTime StartDt, DateTime EndDt, int PageNumber, int PageSize)
		{
			lock (MyLock)
			{
				Common _common = new Common();
				try
				{
					using (Bingo_stagingConn db = new Bingo_stagingConn())
					{
						GetGameRecordVM res = new GetGameRecordVM();
						string GameID = string.Empty;
						if (!string.IsNullOrWhiteSpace(Game))
							GameID = Game.ToUpper().Trim();
						string UPlayerID = PlayerID.ToUpper().Trim();

						using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["Bingo_stagingConn1"].ConnectionString))
						{
							SqlCommand cmd = new SqlCommand("sp_PlayerGameRecordAPI", con);
							cmd.CommandType = CommandType.StoredProcedure;
							cmd.Parameters.Add(new SqlParameter("@StartDt", StartDt));
							cmd.Parameters.Add(new SqlParameter("@EndDt", EndDt));
							cmd.Parameters.Add(new SqlParameter("@PlayerID", UPlayerID));
							cmd.Parameters.Add(new SqlParameter("@PageNumber", PageNumber));
							cmd.Parameters.Add(new SqlParameter("@PageSize", PageSize));
							if (!string.IsNullOrWhiteSpace(GameID))
								cmd.Parameters.Add(new SqlParameter("@GameProvider", GameID));


							DataTable table = new DataTable();
							SqlDataAdapter da = new SqlDataAdapter(cmd);
							da.Fill(table);
							//List<GameRecordData> data = table.AsEnumerable().Select(row =>
							//																new GameRecordData
							//																{
							//																	BetTime = row.Field<DateTime>("BetTime"),
							//																	GameName = row.Field<string>("GameName"),
							//																	Turnover = row.Field<decimal>("Turnover"),
							//																	WinLoss = row.Field<decimal>("WinLoss")
							//																}).ToList();

							res.Table = table;
							res.TotalRows = table.Rows.Count;
							res.PlayerID = UPlayerID;
							res.Game = GameID;
							res.Message = "Successfully get record.";
							res.Code = 0;
						}

						return new HttpResponseMessage()
						{
							StatusCode = HttpStatusCode.OK,
							Content = new StringContent(JsonConvert.SerializeObject(res), System.Text.Encoding.UTF8, "application/json")
						};
					}
				}
				catch (Exception ex)
				{
					_common.LogError("GetGameRecord " + DateTime.Now.ToString() + " " + ex.Message.ToString(), FilePrefix);
					dynamic res = new ExpandoObject();
					res.Code = -9999;
					res.Message = ex.Message.ToString();
					return new HttpResponseMessage()
					{
						StatusCode = HttpStatusCode.InternalServerError,
						Content = new StringContent(JsonConvert.SerializeObject(res), System.Text.Encoding.UTF8, "application/json")
					};
				}
			}
		}

		[HttpGet]
		public HttpResponseMessage Get4DPackage(string PlayerID, string Currency, string TrackerId)
		{
			lock (MyLock)
			{
				string apiurl = "";
				Common _common = new Common();
				int TimeoutSec = 180000; //3 mins

				try
				{
					_common.InsertLog("Get4DPackage", "TrackerId", TrackerId, "Request", "", "PlayerID", PlayerID, "Currency", Currency);

					using (Bingo_stagingConn db = new Bingo_stagingConn())
					{
						var res = new Get4DPackageVM();
						string GameID = "BT28WIN";
						string UPlayerID = PlayerID.ToUpper().Trim();
						string UCurrency = Currency.ToUpper().Trim();

						//28win
						var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																							 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

						if (record != null)
						{
							apiurl = string.Format(ConfigurationManager.AppSettings["BT28WINApiUrl"] + "/GetPackageNew?loginID={0}", record.GamePlayerID);
							var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
							httpWebRequest.ContentType = "application/json";
							httpWebRequest.Method = "GET";
							httpWebRequest.Timeout = TimeoutSec;
							httpWebRequest.ReadWriteTimeout = TimeoutSec;

							var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

							using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
							{
								var responseText = streamReader.ReadToEnd();
								dynamic result = JObject.Parse(responseText);

								if (Convert.ToString(result.ErrorCode) == "0")
								{
									res.PlayerID = UPlayerID;
									res.Package = result.Data;
									res.Game = GameID;
									res.Message = "Successfully get package.";
									res.Code = 0;
								}
								else
								{
									res.PlayerID = UPlayerID;
									res.Package = null;
									res.Game = GameID;
									//res.Message = "Get package api error.";
									res.Message = result.ErrorDesc;
									res.Code = 2;
								}
							}
						}
						else
						{
							res.PlayerID = UPlayerID;
							res.Package = null;
							res.Game = GameID;
							res.Message = "Game account not found.";
							res.Code = 1;
						}

						_common.InsertLog("Get4DPackage", "TrackerId", TrackerId, "Response", JsonConvert.SerializeObject(res), "PlayerID", PlayerID, "Currency", Currency);

						return new HttpResponseMessage()
						{
							StatusCode = HttpStatusCode.OK,
							Content = new StringContent(JsonConvert.SerializeObject(res), System.Text.Encoding.UTF8, "application/json")
						};
					}
				}
				catch (Exception ex)
				{
					_common.LogError($"{TrackerId}:Get4DPackage:EXCEPTION " + string.Format("{0}|{1}", PlayerID, Currency) + " " + ex.Message.ToString(), FilePrefix);
					_common.InsertLog("Get4DPackage", "TrackerId", TrackerId, "Exception", ex.Message.ToString(), "PlayerID", PlayerID, "Currency", Currency);
					dynamic res = new ExpandoObject();
					res.Code = -9999;
					res.Message = ex.Message.ToString();
					return new HttpResponseMessage()
					{
						StatusCode = HttpStatusCode.InternalServerError,
						Content = new StringContent(JsonConvert.SerializeObject(res), System.Text.Encoding.UTF8, "application/json")
					};
				}
			}
		}

		[HttpGet]
		public HttpResponseMessage GetMDramaUrl(string PlayerID, string TrackerId)
		{
			lock (MyLock)
			{
				string apiurl = "";
				Common _common = new Common();
				int TimeoutSec = 180000; //3 mins
				string gamePlayerID = string.Empty;
				bool canGetUrl = false;

				try
				{
					_common.InsertLog("GetMDramaUrl", "TrackerId", TrackerId, "Request", "", "PlayerID", PlayerID);

					using (Bingo_stagingConn db = new Bingo_stagingConn())
					{
						var res = new GetMDramaUrlVM();
						string GameID = "MDRAMA";
						string UPlayerID = PlayerID.ToUpper().Trim();

						//mdrama
						var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																							 x.IsActive == true).FirstOrDefault();

						//check and create account
						if (record == null)
						{
							gamePlayerID = FormGamePlayerID(GameID, PlayerID);
							apiurl = string.Format(ConfigurationManager.AppSettings["MDRAMAApiUrl"] + "/CreateUser?username={0}&mcUID={1}", gamePlayerID, UPlayerID);
							var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
							httpWebRequest.ContentType = "application/json";
							httpWebRequest.Method = "GET";
							httpWebRequest.Timeout = TimeoutSec;
							httpWebRequest.ReadWriteTimeout = TimeoutSec;

							var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

							using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
							{
								var responseText = streamReader.ReadToEnd();
								dynamic result = JObject.Parse(responseText);

								if (result.status == 1)
									canGetUrl = true;
							}
						}
						else
						{
							canGetUrl = true;
							gamePlayerID = record.GamePlayerID;
						}

						if (canGetUrl == true)
						{
							string apiurl1 = string.Format(ConfigurationManager.AppSettings["MDRAMAApiUrl"] + "/LaunchLobby?username={0}", gamePlayerID);
							var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl1);
							httpWebRequest.ContentType = "application/json";
							httpWebRequest.Method = "GET";
							httpWebRequest.Timeout = TimeoutSec;
							httpWebRequest.ReadWriteTimeout = TimeoutSec;

							var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

							using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
							{
								var responseText = streamReader.ReadToEnd();
								dynamic result = JObject.Parse(responseText);

								if (result.status == 1)
								{
									res.Url = Convert.ToString(result.url);
									res.Message = "Successfully get url.";
									res.Code = 0;
								}
								else
								{
									res.Url = null;
									res.Message = "Unknown error.";
									res.Code = 2;
								}
							}
						}
						else
						{
							res.Url = null;
							res.Message = "Failed to create mdrama account.";
							res.Code = 1;
						}

						_common.InsertLog("GetMDramaUrl", "TrackerId", TrackerId, "Response", JsonConvert.SerializeObject(res), "PlayerID", PlayerID);

						return new HttpResponseMessage()
						{
							StatusCode = HttpStatusCode.OK,
							Content = new StringContent(JsonConvert.SerializeObject(res), System.Text.Encoding.UTF8, "application/json")
						};
					}
				}
				catch (Exception ex)
				{
					_common.LogError($"{TrackerId}:GetMDramaUrl:EXCEPTION " + string.Format("{0}", PlayerID) + " " + ex.Message.ToString(), FilePrefix);
					_common.InsertLog("GetMDramaUrl", "TrackerId", TrackerId, "Exception", ex.Message.ToString(), "PlayerID", PlayerID);
					dynamic res = new ExpandoObject();
					res.Code = -9999;
					res.Message = ex.Message.ToString();
					return new HttpResponseMessage()
					{
						StatusCode = HttpStatusCode.InternalServerError,
						Content = new StringContent(JsonConvert.SerializeObject(res), System.Text.Encoding.UTF8, "application/json")
					};
				}
			}
		}

		[HttpGet]
		public HttpResponseMessage GetECommerceUrl(string PlayerID, string TrackerId)
		{
			lock (MyLock)
			{
				Common _common = new Common();
				int TimeoutSec = 180000; //3 mins
				string gamePlayerID = string.Empty;

				try
				{
					_common.InsertLog("GetECommerceUrl", "TrackerId", TrackerId, "Request", "", "PlayerID", PlayerID);

					using (Bingo_stagingConn db = new Bingo_stagingConn())
					{
						var res = new GetMDramaUrlVM();
						string UPlayerID = PlayerID.ToUpper().Trim();


						string apiurl1 = string.Format(ConfigurationManager.AppSettings["ECOMMERCEApiUrl"] + "/LaunchLobby?username={0}", gamePlayerID);
						var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl1);
						httpWebRequest.ContentType = "application/json";
						httpWebRequest.Method = "GET";
						httpWebRequest.Timeout = TimeoutSec;
						httpWebRequest.ReadWriteTimeout = TimeoutSec;

						var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

						using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
						{
							var responseText = streamReader.ReadToEnd();
							dynamic result = JObject.Parse(responseText);

							if (result.status == 1)
							{
								res.Url = Convert.ToString(result.url);
								res.Message = "Successfully get url.";
								res.Code = 0;
							}
							else
							{
								res.Url = null;
								res.Message = "Unknown error.";
								res.Code = 2;
							}
						}

						_common.InsertLog("GetECommerceUrl", "TrackerId", TrackerId, "Response", JsonConvert.SerializeObject(res), "PlayerID", PlayerID);

						return new HttpResponseMessage()
						{
							StatusCode = HttpStatusCode.OK,
							Content = new StringContent(JsonConvert.SerializeObject(res), System.Text.Encoding.UTF8, "application/json")
						};
					}
				}
				catch (Exception ex)
				{
					_common.LogError($"{TrackerId}:GetECommerceUrl:EXCEPTION " + string.Format("{0}", PlayerID) + " " + ex.Message.ToString(), FilePrefix);
					_common.InsertLog("GetECommerceUrl", "TrackerId", TrackerId, "Exception", ex.Message.ToString(), "PlayerID", PlayerID);
					dynamic res = new ExpandoObject();
					res.Code = -9999;
					res.Message = ex.Message.ToString();
					return new HttpResponseMessage()
					{
						StatusCode = HttpStatusCode.InternalServerError,
						Content = new StringContent(JsonConvert.SerializeObject(res), System.Text.Encoding.UTF8, "application/json")
					};
				}
			}
		}

		[HttpGet]
		public HttpResponseMessage SplitCashAndChip(string Game, string PlayerID, string Currency, decimal Amount, string TrackerId)
		{
			lock (MyLock)
			{
				string apiurl = "";
				Common _common = new Common();
				int TimeoutSec = 180000; //3 mins

				try
				{
					_common.InsertLog("SplitCashAndChip", "TrackerId", TrackerId, "Request", "", "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Amount", Amount.ToString());

					using (Bingo_stagingConn db = new Bingo_stagingConn())
					{
						var res = new SplitCashAndChipVM();
						string GameID = Game.ToUpper().Trim();
						string UPlayerID = PlayerID.ToUpper().Trim();
						string UCurrency = Currency.ToUpper().Trim();
						decimal Cash = 0;
						decimal Chip = 0;

						//28win
						if (GameID == "BT28WIN")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								GeneralSplitOutCashChip(GameID, UPlayerID, Amount, ref Cash, ref Chip);
								res.PlayerID = UPlayerID;
								res.Cash = Cash;
								res.Chip = Chip;
								res.Game = GameID;
								res.Message = "Successfully split.";
								res.Code = 0;
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.Cash = 0;
								res.Chip = 0;
								res.Game = GameID;
								res.Message = "Game account not found.";
								res.Code = 1;
							}
						}
						//menang88
						else if (GameID == "MENANG88")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								GeneralSplitOutCashChip(GameID, UPlayerID, Amount, ref Cash, ref Chip);
								res.PlayerID = UPlayerID;
								res.Cash = Cash;
								res.Chip = Chip;
								res.Game = GameID;
								res.Message = "Successfully split.";
								res.Code = 0;
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.Cash = null;
								res.Chip = null;
								res.Game = GameID;
								res.Message = "Game account not found.";
								res.Code = 1;
							}
						}
						//tpg
						else if (GameID == "TPG")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["Bingo_stagingConn1"].ConnectionString))
								{
									con.Open();
									SqlCommand cmd = new SqlCommand("sp_GameTransaction_Get", con);
									cmd.CommandType = CommandType.StoredProcedure;
									cmd.Parameters.Add(new SqlParameter("@PlayerID", PlayerID));
									cmd.Parameters.Add(new SqlParameter("@Game", Game));

									using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
									{
										DataTable table = new DataTable();
										adapter.Fill(table);

										if (table.Rows.Count > 0)
										{
											string from = Convert.ToDateTime(table.Rows[0]["CreatedDate"]).ToString("yyyy-MM-dd HH:mm:ss");
											string to = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
											apiurl = string.Format(ConfigurationManager.AppSettings["TPGApiUrl"] + "/GetPlayerProfitLoss?username={0}&from={1}&to={2}",
												record.GamePlayerID, from, to);
											var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
											httpWebRequest.ContentType = "application/json";
											httpWebRequest.Method = "GET";
											httpWebRequest.Timeout = TimeoutSec;
											httpWebRequest.ReadWriteTimeout = TimeoutSec;
											var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

											using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
											{
												var responseText = streamReader.ReadToEnd();
												dynamic result = JObject.Parse(responseText);

												if (result.status == 1)
												{
													if (Convert.ToDecimal(result.data.totalBet) >= Convert.ToDecimal(table.Rows[0]["Chip"]))
													{
														res.Chip = 0;
														res.Cash = Amount;
													}
													else
													{
														res.Chip = Convert.ToDecimal(table.Rows[0]["Chip"]) - Convert.ToDecimal(result.data.totalBet);
														res.Cash = Amount - res.Chip;
													}

													res.PlayerID = UPlayerID;
													res.Game = GameID;
													res.Message = "Successfully split.";
													res.Code = 0;
												}
												//no profit and loss information available
												else if (result.status == 57)
												{
													res.PlayerID = UPlayerID;
													res.Game = GameID;
													res.Cash = Convert.ToDecimal(table.Rows[0]["Cash"]);
													res.Chip = Convert.ToDecimal(table.Rows[0]["Chip"]);
													res.Message = result.message;
													res.Code = 0;
												}
												else
												{
													res.PlayerID = UPlayerID;
													res.Game = GameID;
													res.Cash = Amount;
													res.Chip = 0;
													res.Message = result.message;
													res.Code = 2;
												}
											}
										}
										else
										{
											Cash = Amount;
											Chip = 0;
										}
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.Cash = null;
								res.Chip = null;
								res.Game = GameID;
								res.Message = "Game account not found.";
								res.Code = 1;
							}
						}
						//tpgfish
						else if (GameID == "TPGFISH")
						{
							var record = db.GameMappingUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
																								 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

							if (record != null)
							{
								using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["Bingo_stagingConn1"].ConnectionString))
								{
									con.Open();
									SqlCommand cmd = new SqlCommand("sp_GameTransaction_Get", con);
									cmd.CommandType = CommandType.StoredProcedure;
									cmd.Parameters.Add(new SqlParameter("@PlayerID", PlayerID));
									cmd.Parameters.Add(new SqlParameter("@Game", Game));

									using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
									{
										DataTable table = new DataTable();
										adapter.Fill(table);

										if (table.Rows.Count > 0)
										{
											string from = Convert.ToDateTime(table.Rows[0]["CreatedDate"]).ToString("yyyy-MM-dd HH:mm:ss");
											string to = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
											apiurl = string.Format(ConfigurationManager.AppSettings["TPGFISHApiUrl"] + "/GetPlayerProfitLoss?username={0}&from={1}&to={2}",
												record.GamePlayerID, from, to);
											var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
											httpWebRequest.ContentType = "application/json";
											httpWebRequest.Method = "GET";
											httpWebRequest.Timeout = TimeoutSec;
											httpWebRequest.ReadWriteTimeout = TimeoutSec;
											var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

											using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
											{
												var responseText = streamReader.ReadToEnd();
												dynamic result = JObject.Parse(responseText);

												if (result.status == 1)
												{
													if (Convert.ToDecimal(result.data.totalBet) >= Convert.ToDecimal(table.Rows[0]["Chip"]))
													{
														res.Chip = 0;
														res.Cash = Amount;
													}
													else
													{
														res.Chip = Convert.ToDecimal(table.Rows[0]["Chip"]) - Convert.ToDecimal(result.data.totalBet);
														res.Cash = Amount - res.Chip;
													}

													res.PlayerID = UPlayerID;
													res.Game = GameID;
													res.Message = "Successfully split.";
													res.Code = 0;
												}
												//no profit and loss information available
												else if (result.status == 57)
												{
													res.PlayerID = UPlayerID;
													res.Game = GameID;
													res.Cash = Convert.ToDecimal(table.Rows[0]["Cash"]);
													res.Chip = Convert.ToDecimal(table.Rows[0]["Chip"]);
													res.Message = result.message;
													res.Code = 0;
												}
												else
												{
													res.PlayerID = UPlayerID;
													res.Game = GameID;
													res.Cash = Amount;
													res.Chip = 0;
													res.Message = result.message;
													res.Code = 2;
												}
											}
										}
										else
										{
											Cash = Amount;
											Chip = 0;
										}
									}
								}
							}
							else
							{
								res.PlayerID = UPlayerID;
								res.Cash = null;
								res.Chip = null;
								res.Game = GameID;
								res.Message = "Game account not found.";
								res.Code = 1;
							}
						}
						//default
						else
						{
							res.PlayerID = UPlayerID;
							res.Cash = null;
							res.Chip = null;
							res.Game = GameID;
							res.Message = "Invalid game.";
							res.Code = 4;
						}

						_common.InsertLog("SplitCashAndChip", "TrackerId", TrackerId, "Response", JsonConvert.SerializeObject(res), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Amount", Amount.ToString());

						return new HttpResponseMessage()
						{
							StatusCode = HttpStatusCode.OK,
							Content = new StringContent(JsonConvert.SerializeObject(res), System.Text.Encoding.UTF8, "application/json")
						};
					}
				}
				catch (Exception ex)
				{
					_common.LogError($"{TrackerId}:SplitCashAndChip:EXCEPTION " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Currency, Amount.ToString()) + " " + ex.Message.ToString(), FilePrefix);
					_common.InsertLog("SplitCashAndChip", "TrackerId", TrackerId, "Exception", ex.Message.ToString(), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Amount", Amount.ToString());
					dynamic res = new ExpandoObject();
					res.Code = -9999;
					res.Message = ex.Message.ToString();
					return new HttpResponseMessage()
					{
						StatusCode = HttpStatusCode.InternalServerError,
						Content = new StringContent(JsonConvert.SerializeObject(res), System.Text.Encoding.UTF8, "application/json")
					};
				}
			}
		}

		public void GeneralSplitOutCashChip(string Game, string PlayerID, decimal Amount, ref decimal Cash, ref decimal Chip)
		{
			try
			{
				using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["Bingo_stagingConn1"].ConnectionString))
				{
					con.Open();
					SqlCommand cmd = new SqlCommand("sp_GameTransaction_Get", con);
					cmd.CommandType = CommandType.StoredProcedure;
					cmd.Parameters.Add(new SqlParameter("@PlayerID", PlayerID));
					cmd.Parameters.Add(new SqlParameter("@Game", Game));

					using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
					{
						DataTable table = new DataTable();
						adapter.Fill(table);

						if (table.Rows.Count > 0)
						{
							//go here means win money
							if (Amount > Convert.ToDecimal(table.Rows[0]["Cash"]) + Convert.ToDecimal(table.Rows[0]["Chip"]))
							{
								Chip = Convert.ToDecimal(table.Rows[0]["Chip"]);
								Cash = Amount - Convert.ToDecimal(table.Rows[0]["Chip"]);
							}
							//here means normal bet 
							else
							{
								//player didn't use up all his chip
								if (Amount >= Convert.ToDecimal(table.Rows[0]["Cash"]))
								{
									Cash = Convert.ToDecimal(table.Rows[0]["Cash"]);
									Chip = Amount - Cash;
								}
								else
								{
									Cash = Amount;
									Chip = 0;
								}
							}
						}
						else
						{
							Cash = Amount;
							Chip = 0;
						}
					}
				}
			}
			catch
			{
				Cash = Amount;
				Chip = 0;
			}
		}

		public void RollbackDeposit(decimal Amount, decimal WMaxAmount, string PlayerID, string Game, string Currency, string Language, string TrackerId, ref GetGameUrlVM res, ref bool? LockFromGame)
		{
			Common _common = new Common();

			try
			{
				string GameID = Game.ToUpper().Trim();
				string UPlayerID = PlayerID.ToUpper().Trim();
				string UCurrency = Currency.ToUpper().Trim();

				string msg = string.Empty;

				if (Amount > WMaxAmount)
				{
					decimal withdrawAmt = 0;
					decimal remainAmt = Amount;
					decimal amt = 0;
					bool isWithdrawSuccess = false;

					_common.LogError($"{TrackerId}:RollbackDeposit:SUCCESS - More Than Withdraw Max Amount, Amount To Be Withdraw: {remainAmt.ToString()} " + string.Format("{0}|{1}|{2}|{3}|{4}", Game, PlayerID, Currency, Amount.ToString(), Language), FilePrefix);

					while (remainAmt != 0)
					{
						amt = remainAmt > WMaxAmount ? (decimal)WMaxAmount : remainAmt;
						isWithdrawSuccess = GameWithdraw(GameID, UPlayerID, UCurrency, amt, TrackerId, ref LockFromGame);

						_common.LogError($"{TrackerId}:RollbackDeposit:" + (isWithdrawSuccess == true ? "SUCCESS" : "FAILED") + $" - Current Withdraw Amount: {amt.ToString()} " + string.Format("{0}|{1}|{2}|{3}|{4}", Game, PlayerID, Currency, Amount.ToString(), Language), FilePrefix);

						if (isWithdrawSuccess == false)
						{
							break;
						}

						remainAmt -= amt;
						withdrawAmt += amt;

						_common.LogError($"{TrackerId}:RollbackDeposit:SUCCESS - Remaining Amount: {remainAmt.ToString()}, Withdraw Amount: {withdrawAmt.ToString()} " + string.Format("{0}|{1}|{2}|{3}|{4}", Game, PlayerID, Currency, Amount.ToString(), Language), FilePrefix);
					}

					if (isWithdrawSuccess == false)
					{
						msg = $"FAILED: {UPlayerID} rollback deposit {Amount.ToString()} failed from {Game}. ( > Withdraw Max Amount)";
						foreach (var phone in apiPhoneList.Split(','))
						{
							_common.SendSmsToBulkSms(phone, msg);
						}

						res.LockFromGame = true;
					}
					else
					{
						res.IsRefund = true;
						res.LockFromGame = false;
					}
				}
				else
				{
					_common.LogError($"{TrackerId}:RollbackDeposit:SUCCESS - Less Than Withdraw Max Amount, Amount To Be Withdraw: {Amount.ToString()} " + string.Format("{0}|{1}|{2}|{3}|{4}", Game, PlayerID, Currency, Amount.ToString(), Language), FilePrefix);

					bool isWithdrawSuccess = GameWithdraw(GameID, UPlayerID, UCurrency, Amount, TrackerId, ref LockFromGame);

					_common.LogError($"{TrackerId}:RollbackDeposit:" + (isWithdrawSuccess == true ? "SUCCESS" : "FAILED") + $" - Current Withdraw Amount: {Amount.ToString()} " + string.Format("{0}|{1}|{2}|{3}|{4}", Game, PlayerID, Currency, Amount.ToString(), Language), FilePrefix);

					if (isWithdrawSuccess == false)
					{
						//need to call pp api to block player here
						msg = $"FAILED: {UPlayerID} rollback deposit {Amount.ToString()} failed from {GameID}. ( <= Withdraw Max Amount)";
						foreach (var phone in apiPhoneList.Split(','))
						{
							_common.SendSmsToBulkSms(phone, msg);
						}

						res.LockFromGame = true;
					}
					else
					{
						res.IsRefund = true;
						res.LockFromGame = false;
					}
				}
			}
			catch (Exception ex)
			{
				_common.LogError($"{TrackerId}:RollbackDeposit:EXCEPTION " + string.Format("{0}|{1}|{2}|{3}|{4}", Game, PlayerID, Currency, Amount.ToString(), Language) + " " + ex.Message.ToString(), FilePrefix);
				LockFromGame = false;
			}
		}

		public string GetIPAddress()
		{
			var result = string.Empty;
			System.Web.HttpRequest request = System.Web.HttpContext.Current.Request;
			string forwardedFor = request.ServerVariables["HTTP_X_FORWARDED_FOR"];
			string remoteAddr = string.IsNullOrWhiteSpace(forwardedFor) ? request.ServerVariables["REMOTE_ADDR"] : forwardedFor.Split(',').Select(s => s.Trim()).First();
			result = String.IsNullOrWhiteSpace(remoteAddr) ? request.UserHostAddress : remoteAddr;
			return result;
		}

		public string RandomString(int length)
		{
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
			return new string(Enumerable.Repeat(chars, length)
				.Select(s => s[random.Next(s.Length)]).ToArray());
		}

		public string FormGamePlayerID(string GameID, string PlayerID)
		{
			string UPlayerID = PlayerID.Trim().ToUpper();
			string random = RandomString(2);

			try
			{
				if (GameID == "BG")
				{
					string prefix = "BGC";

					if ((prefix + random + UPlayerID).Length <= 20)
						return prefix + random + UPlayerID;
					else
						return prefix + RandomString(10);
				}
				else if (GameID == "AG")
				{
					string prefix = "AGC";

					if ((prefix + random + UPlayerID).Length <= 20)
						return prefix + random + UPlayerID;
					else
						return prefix + RandomString(10);
				}
				else if (GameID == "PT")
				{
					string prefix = "PT";

					if ((prefix + random + UPlayerID).Length <= 30)
						return prefix + random + UPlayerID;
					else
						return prefix + RandomString(10);
				}
				else if (GameID == "IG")
				{
					string prefix = "IGA";

					if ((prefix + random + UPlayerID).Length <= 17)
						return prefix + random + UPlayerID;
					else
						return prefix + RandomString(10);
				}
				else if (GameID == "BT28WIN")
				{
					string prefix = "4D";

					if ((prefix + random + UPlayerID).Length <= 20)
						return prefix + random + UPlayerID;
					else
						return prefix + RandomString(10);
				}
				else if (GameID == "ALLCITY")
				{
					string prefix = "AC";

					if ((prefix + random + UPlayerID).Length <= 10)
						return prefix + random + UPlayerID;
					else
						return prefix + RandomString(8);
				}
				else if (GameID == "IBC")
				{
					string prefix = "IBC";

					if ((prefix + random + UPlayerID).Length <= 30)
						return prefix + random + UPlayerID;
					else
						return prefix + RandomString(9);
				}
				else if (GameID == "VPOWER")
				{
					string prefix = "VP";

					if ((prefix + random + UPlayerID).Length <= 17)
						return prefix + random + UPlayerID;
					else
						return prefix + RandomString(10);
				}
				else if (GameID == "SEXYGAMING")
				{
					string prefix = "SX";

					if ((prefix + random + UPlayerID).Length <= 15)
						return prefix + random + UPlayerID;
					else
						return prefix + RandomString(10);
				}
				else if (GameID == "SPADEGAMING")
				{
					string prefix = "SPA";

					if ((prefix + random + UPlayerID).Length <= 50)
						return prefix + random + UPlayerID;
					else
						return prefix + RandomString(10);
				}
				else if (GameID == "EVOLUTIONGAMING")
				{
					string prefix = "EVG";

					if ((prefix + random + UPlayerID).Length <= 50)
						return prefix + random + UPlayerID;
					else
						return prefix + RandomString(10);
				}
				else if (GameID == "TPG")
				{
					string prefix = "TPG";

					if ((prefix + random + UPlayerID).Length <= 50)
						return prefix + random + UPlayerID;
					else
						return prefix + RandomString(10);
				}
				else if (GameID == "TPGFISH")
				{
					string prefix = "TPF";

					if ((prefix + random + UPlayerID).Length <= 50)
						return prefix + random + UPlayerID;
					else
						return prefix + RandomString(10);
				}
				else if (GameID == "MDRAMA")
				{
					string prefix = "MDR";

					if ((prefix + random + UPlayerID).Length <= 20)
						return prefix + random + UPlayerID;
					else
						return prefix + RandomString(10);
				}
				else if (GameID == "XE88")
				{
					string prefix = "XE";

					if ((prefix + random + UPlayerID).Length <= 20)
						return prefix + random + UPlayerID;
					else
						return prefix + RandomString(10);
				}
				else if (GameID == "KYPOKER")
				{
					string prefix = "KYP2";

					if ((prefix + random + UPlayerID).Length <= 30)
						return prefix + random + UPlayerID;
					else
						return prefix + RandomString(10);
				}
				else if (GameID == "SABAESPORTS")
				{
					string prefix = "SA";

					if ((prefix + random + UPlayerID).Length <= 30)
						return prefix + random + UPlayerID;
					else
						return prefix + RandomString(9);
				}
				else if (GameID == "VGAMING")
				{
					string prefix = "VG";

					if ((prefix + random + UPlayerID).Length <= 30)
						return prefix + random + UPlayerID;
					else
						return prefix + RandomString(9);
				}
				else if (GameID == "CQ9")
				{
					string prefix = "CQ9";

					if ((prefix + random + UPlayerID).Length <= 30)
						return prefix + random + UPlayerID;
					else
						return prefix + RandomString(10);
				}

				return "";
			}
			catch (Exception ex)
			{
				return "";
			}
		}

		public string GetVPowerErrorMsg(int code)
		{
			switch (code)
			{
				case 0:
					return "Success";
				case 1:
					return "New User is created";
				case 2:
					return "User does not exist";
				case 3:
					return "Parameter Error";
				case 4:
					return "Invalid Signature";
				case 5:
					return "Token Expired";
				case 6:
					return "RequestId Has Already Been Used";
				case 7:
					return "RequestId Format Error";
				case 8:
					return "Exceeded the Maximum Limit";
				case 9:
					return "Signing Out Failed";
				case 10:
					return "Invalid Request";
				case 11:
					return "Unknown Database Error";
				case 12:
					return "User Already Exist";
				case 13:
					return "Topup fail";
				case 14:
					return "Insufficient credit";
				case 15:
					return "No data found";
				default:
					return "";
			}


		}

		public string GetGameType(string GameID)
		{
			switch (GameID.ToUpper())
			{
				//case "AG":
				//case "BG":
				case "SPADEGAMING":
				case "VPOWER":
				case "BT28WIN":
				//case "PT":
				case "IBC":
				case "MENANG88":
				case "TPG":
				case "TPGFISH":
				case "SABAESPORTS":
				case "KYPOKER":
				case "XE88":
				case "VGAMING":
					//case "EVOLUTIONGAMING":
					return "WebView";
				case "EVO888":
				case "ALLCITY":
				case "IG":
				case "MEGA":
					return "App";
				case "SEXYGAMING":
				case "PT":
				case "EVOLUTIONGAMING":
				case "AG":
				case "BG":
					return "Browser";
				default:
					return "";
			}
		}

		public void GetGameTransferLimit(string Game, string Type, string Currency, string TrackerId, ref decimal? MinAmount, ref decimal? MaxAmount)
		{
			Common _common = new Common();
			try
			{
				using (Bingo_stagingConn db = new Bingo_stagingConn())
				{
					string GameID = Game.ToUpper().Trim();
					string UCurrency = Currency.ToUpper().Trim();

					var record = db.GameTransferLimitUATs.Where(x => x.Game.ToUpper().Trim() == GameID && x.Type.ToUpper().Trim() == Type &&
																	 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

					if (record != null)
					{
						MinAmount = record.MinAmount;
						MaxAmount = record.MaxAmount;
					}
				}
			}
			catch (Exception ex)
			{
				_common.LogError($"{TrackerId}:GetGameTransferLimit:EXCEPTION " + string.Format("{0}|{1}|{2}", Game, Type, Currency) + " " + ex.Message.ToString(), FilePrefix);
			}
		}

		public string GetGameRunningNumber(string Game)
		{
			Common _common = new Common();
			try
			{
				using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["Bingo_stagingConn1"].ConnectionString))
				{
					con.Open();
					SqlCommand cmd = new SqlCommand("sp_AutoNumber_Get", con);
					cmd.CommandType = CommandType.StoredProcedure;
					cmd.Parameters.Add(new SqlParameter("@Game", Game));
					using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
					{
						DataTable table = new DataTable();
						adapter.Fill(table);
						if (table.Rows.Count > 0)
						{
							return table.Rows[0]["RunningNumber"].ToString();
						}

						return "";
					}
				}
			}
			catch (Exception ex)
			{
				_common.LogError($"{Game}:GetGameRunningNumber:EXCEPTION " + string.Format("{0}", Game) + " " + ex.Message.ToString(), FilePrefix);
				return "";
			}
		}

		public bool CheckIsLockPlayer(string Game, string Code)
		{
			try
			{
				string GameID = Game.ToUpper().Trim();
				string code = Code.ToUpper().Trim();
				if (GameID == "BG")
				{
					int index = Convert.ToInt32(code);
					if (index >= 0 && index <= 99) //系统错误
						return true;
					else if (index >= 300 && index <= 399) //数据库错误 
						return true;
					else if (index >= 400 && index <= 499) //缓存错误  
						return true;
					else if (index >= 500 && index <= 599) //RPC错误  
						return true;
					else if (index >= 2400 && index <= 2499) //[平台:金流服务]相关  
						return true;
					else
						return false;
				}
				else if (GameID == "AG")
				{
					switch (code)
					{
						case "1": //失败, 订单未处理状态
						case "2": //因无效的转账金额引致的失败
						case "DUPLICATE_TRANSFER": //重复转账
						case "NETWORK_ERROR": //网络问题导致资料遗失
						case "ERROR": //转账错误, 参看 msg 错误信息描述
							return true;
						default:
							return false;
					}
				}
				else if (GameID == "PT")
				{
					switch (code)
					{
						case "71": //Could not load player data. Error: 'UMS API return error on access request attempt'
						case "72": //Could not load player data. Error: 'Service error accessing UMS API'
						case "73": //Could not load player data from database. Error: 'Database error occured, please contact support'. Please, try again late
						case "95": //Casino not open
						case "106": //Kioskadmin deposit balance update failed.
						case "170": //Deposit error.
						case "301": //API error.
						case "302": //External transaction id already exists.
							return true;
						default:
							return false;
					}
				}
				else if (GameID == "IG")
				{
					switch (code)
					{
						case "-90002": //未知的錯誤。
						case "-90003": //例外錯誤。
						case "-90500": //訪問 GTAPI 玩家開分餘額的時候失敗。
						case "-90501": //訪問 GTAPI 玩家資訊失敗。
						case "-90600": //無效金額格式。
						case "-90610": //外部交易 Id 在 API 系統重複。
						case "-90617": //玩家在遊戲中不能洗分。
							return true;
						default:
							return false;
					}
				}
				else if (GameID == "BT28WIN")
				{
					switch (code)
					{
						case "999": //系統錯誤信息，請聯絡管理員
							return true;
						default:
							return false;
					}
				}
				else if (GameID == "ALLCITY")
				{
					switch (code)
					{
						case "201": //失败
							return true;
						default:
							return false;
					}
				}
				else if (GameID == "IBC" || GameID == "SABAESPORTS" || GameID == "VGAMING")
				{
					switch (code)
					{
						case "1": //failed during executed
											//case "5": //repeated same transfer ID
						case "8": //[Win Limit Withdraw RED Alert] Member reach limit
						case "10": //system maintenance
						case "11": //system is busy, please try again after 2 minutes
							return true;
						default:
							return false;
					}
				}
				else if (GameID == "EVO888")
				{
					switch (code)
					{
						case "-2": //database error
						case "-5": //system exception
							return true;
						default:
							return false;
					}
				}
				else if (GameID == "VPOWER")
				{
					switch (code)
					{
						case "6": //requestid has already been used
						case "13": //credit transfer failed
							return true;
						default:
							return false;
					}
				}
				else if (GameID == "SEXYGAMING")
				{
					switch (code)
					{
						default:
							return true;
					}
				}
				else if (GameID == "SPADEGAMING")
				{
					switch (code)
					{
						case "1": //system error
						case "3": //service inaccessible
						case "100": //request timeout
						case "101": //call limited
						case "104": //request forbidden
						case "107": //duplicate serial no
						case "112": //api call limited
						case "50111": //exceed max amount
						case "50113": //amount invalid
							return true;
						default:
							return false;
					}
				}
				else if (GameID == "MEGA")
				{
					switch (code)
					{
						default:
							return true;
					}
				}
				else if (GameID == "EVOLUTIONGAMING")
				{
					switch (code)
					{
						case string s when s.StartsWith("TRANSACTION ID ALREADY PROCESSED"):
							return true;
						default:
							return false;
					}
				}
				else if (GameID == "MENANG88")
				{
					switch (code)
					{
						default:
							return true;
					}
				}
				else if (GameID == "XE88")
				{
					switch (code)
					{
						case "72": //could not load data. Error: 'Service error accessing API'
						case "73": //could not load data from database. Error: 'Database error occured, please contact support'. Please try again later
							return true;
						default:
							return false;
					}
				}
				else if (GameID == "KYPOKER")
				{
					switch (code)
					{
						case "27": //数据库异常
						case "34": //订单重复
						case "39": //禁止同一账号登录带分、上分、下分并发请求，后一个请求被拒
							return true;
						default:
							return false;
					}
				}
				else if (GameID == "CQ9")
				{
					switch (code)
					{
						case "23": //Game is under maintenance.
						case "30": //Pool uninitialize.
						case "33": //Transaction in progress, please check later.
						case "100": //Something wrong.
						case "101": //Auth service error.
						case "102": //User service error.
						case "103": //Transaction service error.
						case "104": //Game Manager service error.
						case "105": //Wallet service error.
						case "106": //Tviewer service error.
						case "107": //Orderview service error.
						case "108": //Report service error.
						case "110": //Promotion service error.
						case "111": //PromoRed service error.
						case "112": //LuckyBag service error.
							return true;
						default:
							return false;
					}
				}
				else
					return true;

			}
			catch (Exception ex)
			{
				return true;
			}
		}
	}
}