using GameRecord.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
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

namespace GameRecord.Controllers
{
	public class PlayerController : ApiController
	{
		public static readonly object MyLock = new object();
		private static Random random = new Random();

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
			public string Message { get; set; }
			public int Code { get; set; }
		}

		public class GetGameUrlVM
		{
			public string PlayerID { get; set; }
			public string Url { get; set; }
			public string Game { get; set; }
			public string Message { get; set; }
			public int Code { get; set; }
		}

		public class GetGameRecordVM
		{
			public int Code { get; set; }
			public string Message { get; set; }
			public string PlayerID { get; set; }
			public string Game { get; set; }
			public int TotalRows { get; set; }
			public List<GameRecordData> Records { get; set; }
		}

		public class GameRecordData
		{
			public DateTime BetTime { get; set; }
			public string GameName { get; set; }
			public decimal Turnover { get; set; }
			public decimal WinLoss { get; set; }
		}

		[HttpGet]
		public HttpResponseMessage GetBalance(string Game, string PlayerID, string Currency)
		{
			lock (MyLock)
			{
				string apiurl = "";
				Common _common = new Common();
				int TimeoutSec = 180000; //3 mins

				try
				{
					using (BingoConn db = new BingoConn())
					{
						var res = new GetBalanceVM();
						string GameID = Game.ToUpper().Trim();

						//bka
						if (GameID == "BKA")
						{
							var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

							if (record != null)
							{
								var client = new RestClient("http://180.215.206.36:4613/Cash/CashAPI.asmx?op=GameBalance");
								client.Timeout = -1;
								var request = new RestRequest(Method.POST);
								request.AddHeader("Content-Type", "text/xml");
								request.AddParameter("text/xml", "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n"
										+ "<soap:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">\r\n  "
										+ "<soap:Body>\r\n    "
										+ "<GameBalance xmlns=\"http://tempuri.org/\">\r\n      "
										+ "<CompanyID>BingoTown</CompanyID>\r\n      "
										+ "<Account>" + PlayerID + "</Account>\r\n      "
										+ "<Password></Password>\r\n      "
										+ "<GameID>3</GameID>\r\n    "
										+ "</GameBalance>\r\n  "
										+ "</soap:Body>\r\n"
										+ "</soap:Envelope>", ParameterType.RequestBody);
								IRestResponse response = client.Execute(request);

								XmlDocument xmlDoc1 = new XmlDocument();
								xmlDoc1.LoadXml(response.Content);
								string responseText = xmlDoc1.InnerText;

								dynamic result = JObject.Parse(responseText);

								if (result.Code == 1)
								{
									res.PlayerID = PlayerID;
									res.Balance = Convert.ToDecimal(result.Message);
									res.Game = GameID;
									res.Message = "Successfully get balance.";
									res.Code = 0;
								}
								else
								{
									res.PlayerID = PlayerID;
									res.Balance = null;
									res.Game = GameID;
									res.Message = result.Message;
									res.Code = 2;
								}
							}
							else
							{
								res.PlayerID = PlayerID;
								res.Balance = null;
								res.Game = GameID;
								res.Message = "Game account not found.";
								res.Code = 1;
							}
						}
						//918kiss
						else if (GameID == "918KISS")
						{
							var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == "918" + PlayerID.ToUpper()).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format("http://gameapi-918.azurewebsites.net/api/BT918/GetBalance?UID={0}&IP={1}", record.GamePlayerID, GetIPAddress());
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

									if (Convert.ToString(result.Message).ToUpper() == "SUCCESS")
									{
										res.PlayerID = PlayerID;
										res.Balance = Convert.ToDecimal(result.ScoreNum);
										res.Game = GameID;
										res.Message = "Successfully get balance.";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = PlayerID;
										res.Balance = null;
										res.Game = GameID;
										res.Message = result.Message;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = PlayerID;
								res.Balance = null;
								res.Game = GameID;
								res.Message = "Game account not found.";
								res.Code = 1;
							}
						}
						//bg
						else if (GameID == "BG")
						{
							var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format("http://gameapi-bg.azurewebsites.net/api/BG/GetBalance?UID={0}&IP={1}", record.GamePlayerID, GetIPAddress());
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
										res.PlayerID = PlayerID;
										res.Balance = Convert.ToDecimal(result.result);
										res.Game = GameID;
										res.Message = "Successfully get balance.";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = PlayerID;
										res.Balance = null;
										res.Game = GameID;
										res.Message = result.error.message;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = PlayerID;
								res.Balance = null;
								res.Game = GameID;
								res.Message = "Game account not found.";
								res.Code = 1;
							}
						}
						//ag
						else if (GameID == "AG")
						{
							var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format("http://gameapi-ag.azurewebsites.net/api/AG/GetBalance?loginname={0}&currency={1}", record.GamePlayerID, Currency);
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
										res.PlayerID = PlayerID;
										res.Balance = Convert.ToDecimal(result.ErrorCode);
										res.Game = GameID;
										res.Message = "Successfully get balance.";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = PlayerID;
										res.Balance = null;
										res.Game = GameID;
										res.Message = result.ErrorDesc;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = PlayerID;
								res.Balance = null;
								res.Game = GameID;
								res.Message = "Game account not found.";
								res.Code = 1;
							}
						}
						//playtech
						else if (GameID == "PT")
						{
							var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format("http://gameapi-pt.azurewebsites.net/api/PT/GetPlayerBalance?playerName={0}", record.GamePlayerID);
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
										res.PlayerID = PlayerID;
										res.Balance = Convert.ToDecimal(result.result.balance);
										res.Game = GameID;
										res.Message = "Successfully get balance.";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = PlayerID;
										res.Balance = null;
										res.Game = GameID;
										res.Message = result.error;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = PlayerID;
								res.Balance = null;
								res.Game = GameID;
								res.Message = "Game account not found.";
								res.Code = 1;
							}
						}
						//ig
						else if (GameID == "IG")
						{
							var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format("http://gameapi-play8.azurewebsites.net/api/Play8/GetBalance?UID={0}&IP={1}", record.GamePlayerID, GetIPAddress());
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
										res.PlayerID = PlayerID;
										res.Balance = Convert.ToDecimal(result.Data.Balance);
										res.Game = GameID;
										res.Message = "Successfully get balance.";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = PlayerID;
										res.Balance = null;
										res.Game = GameID;
										res.Message = result.Message;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = PlayerID;
								res.Balance = null;
								res.Game = GameID;
								res.Message = "Game account not found.";
								res.Code = 1;
							}
						}
						//bt28win
						else if (GameID == "BT28WIN")
						{
							var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format("http://gameapi-28win.azurewebsites.net/api/BT28Win/GetProfile?loginID={0}", record.GamePlayerID);
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
										res.PlayerID = PlayerID;
										res.Balance = Convert.ToDecimal(result.Data.balance);
										res.Game = GameID;
										res.Message = "Successfully get balance.";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = PlayerID;
										res.Balance = null;
										res.Game = GameID;
										res.Message = result.ErrorDesc;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = PlayerID;
								res.Balance = null;
								res.Game = GameID;
								res.Message = "Game account not found.";
								res.Code = 1;
							}

						}
						//tpg
						else if (GameID == "TPG")
						{
							var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format("http://game-tiger.azurewebsites.net/api/TPG/GetPlayerGameBalance?playerName={0}&displayName={1}&currency={2}&mcUID={3}",
																				record.GamePlayerID, PlayerID, Currency, PlayerID);
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
										res.PlayerID = PlayerID;
										res.Balance = Convert.ToDecimal(result.totalBalance);
										res.Game = GameID;
										res.Message = "Successfully get balance.";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = PlayerID;
										res.Balance = null;
										res.Game = GameID;
										res.Message = result.message;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = PlayerID;
								res.Balance = null;
								res.Game = GameID;
								res.Message = "Game account not found.";
								res.Code = 1;
							}
						}
						//tpgfish
						else if (GameID == "TPGFISH")
						{
							var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format("http://game-tigerfish.azurewebsites.net/api/TPGFish/GetPlayerGameBalance?playerName={0}&displayName={1}&currency={2}&mcUID={3}",
																				record.GamePlayerID, PlayerID, Currency, PlayerID);
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
										res.PlayerID = PlayerID;
										res.Balance = Convert.ToDecimal(result.totalBalance);
										res.Game = GameID;
										res.Message = "Successfully get balance.";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = PlayerID;
										res.Balance = null;
										res.Game = GameID;
										res.Message = result.message;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = PlayerID;
								res.Balance = null;
								res.Game = GameID;
								res.Message = "Game account not found.";
								res.Code = 1;
							}
						}
						//allcity
						else if (GameID == "ALLCITY")
						{
							var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format("http://gameapi-allcity.azurewebsites.net/api/AC/GetUserInfo?account={0}", record.GamePlayerID);
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
										res.PlayerID = PlayerID;
										res.Balance = Convert.ToDecimal(result.pending) + Convert.ToDecimal(result.balance);
										res.Game = GameID;
										res.Message = "Successfully get balance.";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = PlayerID;
										res.Balance = null;
										res.Game = GameID;
										res.Message = result.msg;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = PlayerID;
								res.Balance = null;
								res.Game = GameID;
								res.Message = "Game account not found.";
								res.Code = 1;
							}
						}
						//ibc
						else if (GameID == "IBC")
						{
							var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format("http://ibc.bingotown88.com/api/IBC/CheckUserBalance?username={0}", record.GamePlayerID);
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
											res.PlayerID = PlayerID;
											res.Balance = result.Data[0].error_code == 0 ? Convert.ToDecimal(result.Data[0].balance) : null;
											res.Game = GameID;
											res.Code = result.Data[0].error_code == 0 ? 0 : 2;
											res.Message = result.Data[0].error_code == 0 ? "Successfully get balance." : result.message;
										}
										else
										{
											res.PlayerID = PlayerID;
											res.Balance = null;
											res.Game = GameID;
											res.Code = 2;
											res.Message = result.message;
										}
									}
									else
									{
										res.PlayerID = PlayerID;
										res.Balance = null;
										res.Game = GameID;
										res.Code = 2;
										res.Message = result.message;
									}
								}
							}
							else
							{
								res.PlayerID = PlayerID;
								res.Balance = null;
								res.Game = GameID;
								res.Code = 1;
								res.Message = "Game account not found.";
							}
						}
						//evo888
						else if (GameID == "EVO888")
						{
							var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format("http://gameapi-evo888.azurewebsites.net/api/Evo888/GetUserInfo?username={0}", record.GamePlayerID);
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
										res.PlayerID = PlayerID;
										res.Balance = Convert.ToDecimal(result.results.balance);
										res.Game = GameID;
										res.Code = 0;
										res.Message = "Successfully get balance.";
									}
									else
									{
										res.PlayerID = PlayerID;
										res.Balance = null;
										res.Game = GameID;
										res.Message = result.msg;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = PlayerID;
								res.Balance = null;
								res.Game = GameID;
								res.Message = "Game account not found.";
								res.Code = 1;
							}
						}
						//vpower
						else if (GameID == "VPOWER")
						{
							var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format("http://gameapi-vpower.azurewebsites.net/api/VPower/GetBalance?username={0}", record.GamePlayerID);
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
										res.PlayerID = PlayerID;
										res.Balance = Convert.ToDecimal(result.Credit);
										res.Game = GameID;
										res.Code = 0;
										res.Message = "Successfully get balance.";
									}
									else
									{
										res.PlayerID = PlayerID;
										res.Balance = null;
										res.Game = GameID;
										res.Message = result.msg;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = PlayerID;
								res.Balance = null;
								res.Game = GameID;
								res.Message = "Game account not found.";
								res.Code = 1;
							}
						}
						//sexygaming
						else if (GameID == "SEXYGAMING")
						{
							var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format("http://gameapi-sexy.azurewebsites.net/api/Sexy/GetBalance?username={0}", record.GamePlayerID);
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
										res.PlayerID = PlayerID;
										res.Balance = Convert.ToDecimal(result.data.Balance);
										res.Game = GameID;
										res.Code = 0;
										res.Message = "Successfully get balance.";
									}
									else
									{
										res.PlayerID = PlayerID;
										res.Balance = null;
										res.Game = GameID;
										res.Message = result.msg;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = PlayerID;
								res.Balance = null;
								res.Game = GameID;
								res.Message = "Game account not found.";
								res.Code = 1;
							}
						}
						//spadegaming
						else if (GameID == "SPADEGAMING")
						{
							var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format("http://gameapi-spade.azurewebsites.net/api/Spade/GetBalance?username={0}", record.GamePlayerID);
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
											res.PlayerID = PlayerID;
											res.Balance = Convert.ToDecimal(result.list[0].balance);
											res.Game = GameID;
											res.Code = 0;
											res.Message = "Successfully get balance.";
										}
										else
										{
											res.PlayerID = PlayerID;
											res.Balance = null;
											res.Game = GameID;
											res.Message = result.msg;
											res.Code = 2;
										}
									}
									else
									{
										res.PlayerID = PlayerID;
										res.Balance = null;
										res.Game = GameID;
										res.Message = result.msg;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = PlayerID;
								res.Balance = null;
								res.Game = GameID;
								res.Message = "Game account not found.";
								res.Code = 1;
							}
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
					_common.LogError("GetBalance " + DateTime.Now.ToString() + " " + ex.Message.ToString(), "Player");
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

		//[HttpGet]
		//public HttpResponseMessage Deposit(string Game, string PlayerID, decimal Amount, string Currency) //Currency need to get from bingotown Game table and pass in here (by myself)
		//{
		//	lock (MyLock)
		//	{
		//		string apiurl = "";
		//		Common _common = new Common();

		//		try
		//		{
		//			using (BingoConn db = new BingoConn())
		//			{
		//				var res = new DepositVM();
		//				string GameID = Game.ToUpper().Trim();

		//				//918kiss | able to return after balance | result.ScoreNum | no currency for 918 
		//				if (GameID == "918KISS")
		//				{
		//					var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == "918" + PlayerID.ToUpper()).FirstOrDefault();

		//					if (record != null)
		//					{
		//						apiurl = string.Format("http://gameapi-918.azurewebsites.net/api/BT918/Transfer?UID={0}&Num={1}&IP={2}", record.GamePlayerID, string.Format("{0:0.##}", Amount), GetIPAddress());
		//						var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
		//						httpWebRequest.ContentType = "application/json";
		//						httpWebRequest.Method = "GET";
		//						httpWebRequest.Timeout = TimeoutSec;
		//						httpWebRequest.ReadWriteTimeout = TimeoutSec;
		//						var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

		//						using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
		//						{
		//							var responseText = streamReader.ReadToEnd();
		//							dynamic result = JObject.Parse(responseText);

		//							if (Convert.ToString(result.Message).ToUpper().Trim() == "SUCCESS")
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Message = "Success";
		//								res.Code = 0;
		//							}
		//							else
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Message = result.Message;
		//								res.Code = 1;
		//							}
		//						}
		//					}
		//					else
		//					{
		//						res.PlayerID = PlayerID;
		//						res.Game = GameID;
		//						res.Message = "Game account not found.";
		//						res.Code = 2;
		//					}
		//				}
		//				//bg | able to return after balance | result.result | currency based on agent
		//				else if (GameID == "BG")
		//				{
		//					var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

		//					if (record != null)
		//					{
		//						apiurl = string.Format("http://gameapi-bg.azurewebsites.net/api/BG/Transfer?UID={0}&IP={1}&Amount={2}", record.GamePlayerID, GetIPAddress(), string.Format("{0:0.##}", Amount));
		//						var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
		//						httpWebRequest.ContentType = "application/json";
		//						httpWebRequest.Method = "GET";
		//						httpWebRequest.Timeout = TimeoutSec;
		//						httpWebRequest.ReadWriteTimeout = TimeoutSec;
		//						var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

		//						using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
		//						{
		//							var responseText = streamReader.ReadToEnd();
		//							dynamic result = JObject.Parse(responseText);

		//							if (result.error == null)
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Message = "Success";
		//								res.Code = 0;
		//							}
		//							else
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Message = result.error.message;
		//								res.Code = 1;
		//							}
		//						}
		//					}
		//					else
		//					{
		//						res.PlayerID = PlayerID;
		//						res.Game = GameID;
		//						res.Message = "Game account not found.";
		//						res.Code = 2;
		//					}
		//				}
		//				//ag | not able to return after balance | currency based on what we pass in
		//				else if (GameID == "AG")
		//				{
		//					var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

		//					if (record != null)
		//					{
		//						apiurl = string.Format("http://gameapi-ag.azurewebsites.net/api/AG/Deposit?loginname={0}&credit={1}&currency={2}", record.GamePlayerID, string.Format("{0:0.##}", Amount), Currency);
		//						var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
		//						httpWebRequest.ContentType = "application/json";
		//						httpWebRequest.Method = "GET";
		//						httpWebRequest.Timeout = TimeoutSec;
		//						httpWebRequest.ReadWriteTimeout = TimeoutSec;
		//						var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

		//						using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
		//						{
		//							var responseText = streamReader.ReadToEnd();
		//							dynamic result = JObject.Parse(responseText);

		//							if (string.IsNullOrWhiteSpace(Convert.ToString(result.ErrorDesc)))
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Message = "Success";
		//								res.Code = 0;
		//							}
		//							else
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Message = result.ErrorDesc;
		//								res.Code = 1;
		//							}
		//						}
		//					}
		//					else
		//					{
		//						res.PlayerID = PlayerID;
		//						res.Game = GameID;
		//						res.Message = "Game account not found.";
		//						res.Code = 2;
		//					}
		//				}
		//				//playtech | able to return balance | result.result.currentplayerbalance | currency define by provider
		//				else if (GameID == "PT")
		//				{
		//					var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

		//					if (record != null)
		//					{
		//						apiurl = string.Format("http://gameapi-pt.azurewebsites.net/api/PT/Deposit?playerName={0}&amount={1}", record.GamePlayerID, string.Format("{0:0.##}", Amount));
		//						var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
		//						httpWebRequest.ContentType = "application/json";
		//						httpWebRequest.Method = "GET";
		//						httpWebRequest.Timeout = TimeoutSec;
		//						httpWebRequest.ReadWriteTimeout = TimeoutSec;
		//						var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

		//						using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
		//						{
		//							var responseText = streamReader.ReadToEnd();
		//							dynamic result = JObject.Parse(responseText);

		//							if (result.error == null)
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Message = "Success";
		//								res.Code = 0;
		//							}
		//							else
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Message = result.error;
		//								res.Code = 1;
		//							}
		//						}
		//					}
		//					else
		//					{
		//						res.PlayerID = PlayerID;
		//						res.Game = GameID;
		//						res.Message = "Game account not found.";
		//						res.Code = 2;
		//					}
		//				}
		//				//ig | not able to return after balance | currency define by provider
		//				else if (GameID == "IG")
		//				{
		//					var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

		//					if (record != null)
		//					{
		//						apiurl = string.Format("http://gameapi-play8.azurewebsites.net/api/Play8/Transfer?UID={0}&Action=0&Amount={1}&IP={1}", record.GamePlayerID, string.Format("{0:0.##}", Amount), GetIPAddress());
		//						var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
		//						httpWebRequest.ContentType = "application/json";
		//						httpWebRequest.Method = "GET";
		//						httpWebRequest.Timeout = TimeoutSec;
		//						httpWebRequest.ReadWriteTimeout = TimeoutSec;
		//						var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

		//						using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
		//						{
		//							var responseText = streamReader.ReadToEnd();
		//							dynamic result = JObject.Parse(responseText);

		//							if (Convert.ToString(result.Code) == "0")
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Message = "Success";
		//								res.Code = 0;
		//							}
		//							else
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Message = result.Message;
		//								res.Code = 1;
		//							}
		//						}
		//					}
		//					else
		//					{
		//						res.PlayerID = PlayerID;
		//						res.Game = GameID;
		//						res.Message = "Game account not found.";
		//						res.Code = 2;
		//					}
		//				}
		//				//bt28win | able to return after balance but not return from own 28win api | currency define by provider
		//				else if (GameID == "BT28WIN")
		//				{
		//					var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

		//					if (record != null)
		//					{
		//						apiurl = string.Format("http://gameapi-28win.azurewebsites.net/api/BT28Win/Deposit?loginID={0}&amount={1}", record.GamePlayerID, string.Format("{0:0.##}", Amount));
		//						var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
		//						httpWebRequest.ContentType = "application/json";
		//						httpWebRequest.Method = "GET";
		//						httpWebRequest.Timeout = TimeoutSec;
		//						httpWebRequest.ReadWriteTimeout = TimeoutSec;

		//						var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

		//						using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
		//						{
		//							var responseText = streamReader.ReadToEnd();
		//							dynamic result = JObject.Parse(responseText);

		//							if (Convert.ToString(result.ErrorCode) == "0")
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Message = "Success";
		//								res.Code = 0;
		//							}
		//							else
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Message = result.ErrorDesc;
		//								res.Code = 1;
		//							}
		//						}
		//					}
		//					else
		//					{
		//						res.PlayerID = PlayerID;
		//						res.Game = "BT28Win";
		//						res.Message = "Game account not found.";
		//						res.Code = 2;
		//					}

		//				}
		//				//tpg | not able to return after balance | currency based on what we pass in
		//				else if (GameID == "TPG")
		//				{
		//					var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

		//					if (record != null)
		//					{
		//						apiurl = string.Format("http://game-tiger.azurewebsites.net/api/TPG/Deposit?username={0}&displayName={1}&currency={2}&amount={3}",
		//																		record.GamePlayerID, PlayerID, Currency, string.Format("{0:0.##}", Amount));
		//						var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
		//						httpWebRequest.ContentType = "application/json";
		//						httpWebRequest.Method = "GET";
		//						httpWebRequest.Timeout = TimeoutSec;
		//						httpWebRequest.ReadWriteTimeout = TimeoutSec;
		//						var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

		//						using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
		//						{
		//							var responseText = streamReader.ReadToEnd();
		//							dynamic result = JObject.Parse(responseText);

		//							if (result.status == 1)
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Message = "Success";
		//								res.Code = 0;
		//							}
		//							else
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Message = result.message;
		//								res.Code = 1;
		//							}
		//						}
		//					}
		//					else
		//					{
		//						res.PlayerID = PlayerID;
		//						res.Game = GameID;
		//						res.Message = "Game account not found.";
		//						res.Code = 2;
		//					}
		//				}
		//				//allcity | not able to return after balance | currency fix in provider side
		//				else if (GameID == "ALLCITY")
		//				{
		//					var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

		//					if (record != null)
		//					{
		//						apiurl = string.Format("http://gameapi-allcity.azurewebsites.net/api/AC/Deposit?account={0}&amount={1}", record.GamePlayerID, string.Format("{0:0.##}", Amount));
		//						var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
		//						httpWebRequest.ContentType = "application/json";
		//						httpWebRequest.Method = "GET";
		//						httpWebRequest.Timeout = TimeoutSec;
		//						httpWebRequest.ReadWriteTimeout = TimeoutSec;
		//						var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

		//						using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
		//						{
		//							var responseText = streamReader.ReadToEnd();
		//							dynamic result = JObject.Parse(responseText);

		//							if (result.code == 200)
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Message = "Success";
		//								res.Code = 0;
		//							}
		//							else
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Message = result.msg;
		//								res.Code = 1;
		//							}
		//						}
		//					}
		//					else
		//					{
		//						res.PlayerID = PlayerID;
		//						res.Game = GameID;
		//						res.Message = "Game account not found.";
		//						res.Code = 2;
		//					}
		//				}
		//				//ibc | able to return after balance | result.Data.after_amount | currency hardcode in our own api
		//				else if (GameID == "IBC")
		//				{
		//					var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

		//					if (record != null)
		//					{
		//						apiurl = string.Format("http://ibc.bingotown88.com/api/IBC/Deposit?username={0}&amount={1}", record.GamePlayerID, string.Format("{0:0.##}", Amount));
		//						var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
		//						httpWebRequest.ContentType = "application/json";
		//						httpWebRequest.Method = "GET";
		//						httpWebRequest.Timeout = TimeoutSec;
		//						httpWebRequest.ReadWriteTimeout = TimeoutSec;
		//						var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

		//						using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
		//						{
		//							var responseText = streamReader.ReadToEnd();
		//							dynamic result = JObject.Parse(responseText);

		//							if (result.error_code == 0 && result.Data != null)
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Code = result.Data.status == 0 ? 0 : 1;
		//								res.Message = result.Data.status == 0 ? "Success" : "System Error";
		//							}
		//							else
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Code = 1;
		//								res.Message = result.message;
		//							}
		//						}
		//					}
		//					else
		//					{
		//						res.PlayerID = PlayerID;
		//						res.Game = GameID;
		//						res.Code = 2;
		//						res.Message = "Game account not found.";
		//					}
		//				}
		//				//evo888 | not able to return after balance | able to set country in our own api
		//				else if (GameID == "EVO888")
		//				{
		//					var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

		//					if (record != null)
		//					{
		//						apiurl = string.Format("http://gameapi-evo888.azurewebsites.net/api/Evo888/Deposit?username={0}&amount={1}", record.GamePlayerID, string.Format("{0:0.##}", Amount));
		//						var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
		//						httpWebRequest.ContentType = "application/json";
		//						httpWebRequest.Method = "GET";
		//						httpWebRequest.Timeout = TimeoutSec;
		//						httpWebRequest.ReadWriteTimeout = TimeoutSec;
		//						var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

		//						using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
		//						{
		//							var responseText = streamReader.ReadToEnd();
		//							dynamic result = JObject.Parse(responseText);

		//							if (result.code == 0 && Convert.ToString(result.success).Trim().ToUpper() == "TRUE")
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Code = 0;
		//								res.Message = "Success";
		//							}
		//							else
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Message = result.msg;
		//								res.Code = 1;
		//							}
		//						}
		//					}
		//					else
		//					{
		//						res.PlayerID = PlayerID;
		//						res.Game = GameID;
		//						res.Message = "Game account not found.";
		//						res.Code = 2;
		//					}
		//				}
		//				//allcity | not able to return after balance | currency fix in provider side
		//				else if (GameID == "ALLCITY")
		//				{
		//					var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

		//					if (record != null)
		//					{
		//						apiurl = string.Format("http://gameapi-allcity.azurewebsites.net/api/AC/Deposit?account={0}&amount={1}", record.GamePlayerID, string.Format("{0:0.##}", Amount));
		//						var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
		//						httpWebRequest.ContentType = "application/json";
		//						httpWebRequest.Method = "GET";
		//						httpWebRequest.Timeout = TimeoutSec;
		//						httpWebRequest.ReadWriteTimeout = TimeoutSec;
		//						var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

		//						using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
		//						{
		//							var responseText = streamReader.ReadToEnd();
		//							dynamic result = JObject.Parse(responseText);

		//							if (Convert.ToInt16(result.code) == 200)
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Code = 0;
		//								res.Message = "Success";
		//							}
		//							else
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Message = result.msg;
		//								res.Code = 1;
		//							}
		//						}
		//					}
		//					else
		//					{
		//						res.PlayerID = PlayerID;
		//						res.Game = GameID;
		//						res.Message = "Game account not found.";
		//						res.Code = 2;
		//					}
		//				}
		//				//vpower | able to return balance | result.Credit | currency fix in provider side
		//				else if (GameID == "VPOWER")
		//				{
		//					var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

		//					if (record != null)
		//					{
		//						apiurl = string.Format("http://gameapi-vpower.azurewebsites.net/api/VPower/Deposit?username={0}&amount={1}", record.GamePlayerID, string.Format("{0:0.##}", Amount));
		//						var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
		//						httpWebRequest.ContentType = "application/json";
		//						httpWebRequest.Method = "GET";
		//						httpWebRequest.Timeout = TimeoutSec;
		//						httpWebRequest.ReadWriteTimeout = TimeoutSec;
		//						var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

		//						using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
		//						{
		//							var responseText = streamReader.ReadToEnd();
		//							dynamic result = JObject.Parse(responseText);

		//							if (Convert.ToInt16(result.ErrorCode) == 0)
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Code = 0;
		//								res.Message = "Success";
		//							}
		//							else
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Message = GetVPowerErrorMsg(result.ErrorCode);
		//								res.Code = 1;
		//							}
		//						}
		//					}
		//					else
		//					{
		//						res.PlayerID = PlayerID;
		//						res.Game = GameID;
		//						res.Message = "Game account not found.";
		//						res.Code = 2;
		//					}
		//				}

		//				return new HttpResponseMessage()
		//				{
		//					StatusCode = HttpStatusCode.OK,
		//					Content = new StringContent(JsonConvert.SerializeObject(res), System.Text.Encoding.UTF8, "application/json")
		//				};
		//			}


		//		}
		//		catch (Exception ex)
		//		{
		//			_common.LogError("Deposit" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), "Player");
		//			return new HttpResponseMessage(HttpStatusCode.InternalServerError);
		//		}
		//	}
		//}

		//[HttpGet]
		//public HttpResponseMessage Withdraw(string Game, string PlayerID, decimal Amount, string Currency) //Currency need to get from bingotown Game table and pass in here (by myself)
		//{
		//	lock (MyLock)
		//	{
		//		string apiurl = "";
		//		Common _common = new Common();

		//		try
		//		{
		//			using (BingoConn db = new BingoConn())
		//			{
		//				var res = new WithdrawVM();
		//				string GameID = Game.ToUpper().Trim();

		//				//918kiss | able to return after balance | result.ScoreNum | no currency for 918 
		//				if (GameID == "918KISS")
		//				{
		//					var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == "918" + PlayerID.ToUpper()).FirstOrDefault();

		//					if (record != null)
		//					{
		//						apiurl = string.Format("http://gameapi-918.azurewebsites.net/api/BT918/Transfer?UID={0}&Num={1}&IP={2}", record.GamePlayerID, string.Format("{0:0.##}", -1 * Amount), GetIPAddress());
		//						var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
		//						httpWebRequest.ContentType = "application/json";
		//						httpWebRequest.Method = "GET";
		//						httpWebRequest.Timeout = TimeoutSec;
		//						httpWebRequest.ReadWriteTimeout = TimeoutSec;
		//						var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

		//						using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
		//						{
		//							var responseText = streamReader.ReadToEnd();
		//							dynamic result = JObject.Parse(responseText);

		//							if (Convert.ToString(result.Message).ToUpper().Trim() == "SUCCESS")
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Message = "Success";
		//								res.Code = 0;
		//							}
		//							else
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Message = result.Message;
		//								res.Code = 1;
		//							}
		//						}
		//					}
		//					else
		//					{
		//						res.PlayerID = PlayerID;
		//						res.Game = GameID;
		//						res.Message = "Game account not found.";
		//						res.Code = 2;
		//					}
		//				}
		//				//bg | able to return after balance | result.result | currency based on agent
		//				else if (GameID == "BG")
		//				{
		//					var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

		//					if (record != null)
		//					{
		//						apiurl = string.Format("http://gameapi-bg.azurewebsites.net/api/BG/Transfer?UID={0}&IP={1}&Amount={2}", record.GamePlayerID, GetIPAddress(), string.Format("{0:0.##}", -1 * Amount));
		//						var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
		//						httpWebRequest.ContentType = "application/json";
		//						httpWebRequest.Method = "GET";
		//						httpWebRequest.Timeout = TimeoutSec;
		//						httpWebRequest.ReadWriteTimeout = TimeoutSec;
		//						var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

		//						using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
		//						{
		//							var responseText = streamReader.ReadToEnd();
		//							dynamic result = JObject.Parse(responseText);

		//							if (result.error == null)
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Message = "Success";
		//								res.Code = 0;
		//							}
		//							else
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Message = result.error.message;
		//								res.Code = 1;
		//							}
		//						}
		//					}
		//					else
		//					{
		//						res.PlayerID = PlayerID;
		//						res.Game = GameID;
		//						res.Message = "Game account not found.";
		//						res.Code = 2;
		//					}
		//				}
		//				//ag | not able to return after balance | currency based on what we pass in
		//				else if (GameID == "AG")
		//				{
		//					var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

		//					if (record != null)
		//					{
		//						apiurl = string.Format("http://gameapi-ag.azurewebsites.net/api/AG/Withdraw?loginname={0}&credit={1}&currency={2}", record.GamePlayerID, string.Format("{0:0.##}", Amount), Currency);
		//						var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
		//						httpWebRequest.ContentType = "application/json";
		//						httpWebRequest.Method = "GET";
		//						httpWebRequest.Timeout = TimeoutSec;
		//						httpWebRequest.ReadWriteTimeout = TimeoutSec;
		//						var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

		//						using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
		//						{
		//							var responseText = streamReader.ReadToEnd();
		//							dynamic result = JObject.Parse(responseText);

		//							if (string.IsNullOrWhiteSpace(Convert.ToString(result.ErrorDesc)))
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Message = "Success";
		//								res.Code = 0;
		//							}
		//							else
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Message = result.ErrorDesc;
		//								res.Code = 1;
		//							}
		//						}
		//					}
		//					else
		//					{
		//						res.PlayerID = PlayerID;
		//						res.Game = GameID;
		//						res.Message = "Game account not found.";
		//						res.Code = 2;
		//					}
		//				}
		//				//playtech | able to return balance | result.result.currentplayerbalance | currency define by provider
		//				else if (GameID == "PT")
		//				{
		//					var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

		//					if (record != null)
		//					{
		//						apiurl = string.Format("http://gameapi-pt.azurewebsites.net/api/PT/Withdraw?playerName={0}&amount={1}", record.GamePlayerID, string.Format("{0:0.##}", Amount));
		//						var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
		//						httpWebRequest.ContentType = "application/json";
		//						httpWebRequest.Method = "GET";
		//						httpWebRequest.Timeout = TimeoutSec;
		//						httpWebRequest.ReadWriteTimeout = TimeoutSec;
		//						var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

		//						using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
		//						{
		//							var responseText = streamReader.ReadToEnd();
		//							dynamic result = JObject.Parse(responseText);

		//							if (result.error == null)
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Message = "Success";
		//								res.Code = 0;
		//							}
		//							else
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Message = result.error;
		//								res.Code = 1;
		//							}
		//						}
		//					}
		//					else
		//					{
		//						res.PlayerID = PlayerID;
		//						res.Game = GameID;
		//						res.Message = "Game account not found.";
		//						res.Code = 2;
		//					}
		//				}
		//				//ig | not able to return after balance | currency define by provider
		//				else if (GameID == "IG")
		//				{
		//					var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

		//					if (record != null)
		//					{
		//						apiurl = string.Format("http://gameapi-play8.azurewebsites.net/api/Play8/Transfer?UID={0}&Action=1&Amount={1}&IP={1}", record.GamePlayerID, string.Format("{0:0.##}", Amount), GetIPAddress());
		//						var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
		//						httpWebRequest.ContentType = "application/json";
		//						httpWebRequest.Method = "GET";
		//						httpWebRequest.Timeout = TimeoutSec;
		//						httpWebRequest.ReadWriteTimeout = TimeoutSec;
		//						var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

		//						using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
		//						{
		//							var responseText = streamReader.ReadToEnd();
		//							dynamic result = JObject.Parse(responseText);

		//							if (Convert.ToString(result.Code) == "0")
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Message = "Success";
		//								res.Code = 0;
		//							}
		//							else
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Message = result.Message;
		//								res.Code = 1;
		//							}
		//						}
		//					}
		//					else
		//					{
		//						res.PlayerID = PlayerID;
		//						res.Game = GameID;
		//						res.Message = "Game account not found.";
		//						res.Code = 2;
		//					}
		//				}
		//				//bt28win | able to return after balance but not return from own 28win api | currency define by provider
		//				else if (GameID == "BT28WIN")
		//				{
		//					var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

		//					if (record != null)
		//					{
		//						apiurl = string.Format("http://gameapi-28win.azurewebsites.net/api/BT28Win/Withdraw?loginID={0}&amount={1}", record.GamePlayerID, string.Format("{0:0.##}", Amount));
		//						var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
		//						httpWebRequest.ContentType = "application/json";
		//						httpWebRequest.Method = "GET";
		//						httpWebRequest.Timeout = TimeoutSec;
		//						httpWebRequest.ReadWriteTimeout = TimeoutSec;

		//						var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

		//						using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
		//						{
		//							var responseText = streamReader.ReadToEnd();
		//							dynamic result = JObject.Parse(responseText);

		//							if (Convert.ToString(result.ErrorCode) == "0")
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Message = "Success";
		//								res.Code = 0;
		//							}
		//							else
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Message = result.ErrorDesc;
		//								res.Code = 1;
		//							}
		//						}
		//					}
		//					else
		//					{
		//						res.PlayerID = PlayerID;
		//						res.Game = "BT28Win";
		//						res.Message = "Game account not found.";
		//						res.Code = 2;
		//					}

		//				}
		//				//tpg | not able to return after balance | currency based on what we pass in
		//				else if (GameID == "TPG")
		//				{
		//					var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

		//					if (record != null)
		//					{
		//						apiurl = string.Format("http://game-tiger.azurewebsites.net/api/TPG/Withdraw?username={0}&displayName={1}&currency={2}&amount={3}",
		//																		record.GamePlayerID, PlayerID, Currency, string.Format("{0:0.##}", Amount));
		//						var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
		//						httpWebRequest.ContentType = "application/json";
		//						httpWebRequest.Method = "GET";
		//						httpWebRequest.Timeout = TimeoutSec;
		//						httpWebRequest.ReadWriteTimeout = TimeoutSec;
		//						var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

		//						using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
		//						{
		//							var responseText = streamReader.ReadToEnd();
		//							dynamic result = JObject.Parse(responseText);

		//							if (result.status == 1)
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Message = "Success";
		//								res.Code = 0;
		//							}
		//							else
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Message = result.message;
		//								res.Code = 1;
		//							}
		//						}
		//					}
		//					else
		//					{
		//						res.PlayerID = PlayerID;
		//						res.Game = GameID;
		//						res.Message = "Game account not found.";
		//						res.Code = 2;
		//					}
		//				}
		//				//allcity | not able to return after balance | currency fix in provider side
		//				else if (GameID == "ALLCITY")
		//				{
		//					var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

		//					if (record != null)
		//					{
		//						apiurl = string.Format("http://gameapi-allcity.azurewebsites.net/api/AC/Withdraw?account={0}&amount={1}", record.GamePlayerID, string.Format("{0:0.##}", Amount));
		//						var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
		//						httpWebRequest.ContentType = "application/json";
		//						httpWebRequest.Method = "GET";
		//						httpWebRequest.Timeout = TimeoutSec;
		//						httpWebRequest.ReadWriteTimeout = TimeoutSec;
		//						var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

		//						using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
		//						{
		//							var responseText = streamReader.ReadToEnd();
		//							dynamic result = JObject.Parse(responseText);

		//							if (result.code == 200)
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Message = "Success";
		//								res.Code = 0;
		//							}
		//							else
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Message = result.msg;
		//								res.Code = 1;
		//							}
		//						}
		//					}
		//					else
		//					{
		//						res.PlayerID = PlayerID;
		//						res.Game = GameID;
		//						res.Message = "Game account not found.";
		//						res.Code = 2;
		//					}
		//				}
		//				//ibc | able to return after balance | result.Data.after_amount | currency hardcode in our own api
		//				else if (GameID == "IBC")
		//				{
		//					var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

		//					if (record != null)
		//					{
		//						apiurl = string.Format("http://ibc.bingotown88.com/api/IBC/Withdraw?username={0}&amount={1}", record.GamePlayerID, string.Format("{0:0.##}", Amount));
		//						var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
		//						httpWebRequest.ContentType = "application/json";
		//						httpWebRequest.Method = "GET";
		//						httpWebRequest.Timeout = TimeoutSec;
		//						httpWebRequest.ReadWriteTimeout = TimeoutSec;
		//						var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

		//						using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
		//						{
		//							var responseText = streamReader.ReadToEnd();
		//							dynamic result = JObject.Parse(responseText);

		//							if (result.error_code == 0 && result.Data != null)
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Code = result.Data.status == 0 ? 0 : 1;
		//								res.Message = result.Data.status == 0 ? "Success" : "System Error";
		//							}
		//							else
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Code = 1;
		//								res.Message = result.message;
		//							}
		//						}
		//					}
		//					else
		//					{
		//						res.PlayerID = PlayerID;
		//						res.Game = GameID;
		//						res.Code = 2;
		//						res.Message = "Game account not found.";
		//					}
		//				}
		//				//evo888 | not able to return after balance | able to set country in our own api
		//				else if (GameID == "EVO888")
		//				{
		//					var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

		//					if (record != null)
		//					{
		//						apiurl = string.Format("http://gameapi-evo888.azurewebsites.net/api/Evo888/Withdraw?username={0}&amount={1}", record.GamePlayerID, string.Format("{0:0.##}", Amount));
		//						var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
		//						httpWebRequest.ContentType = "application/json";
		//						httpWebRequest.Method = "GET";
		//						httpWebRequest.Timeout = TimeoutSec;
		//						httpWebRequest.ReadWriteTimeout = TimeoutSec;
		//						var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

		//						using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
		//						{
		//							var responseText = streamReader.ReadToEnd();
		//							dynamic result = JObject.Parse(responseText);

		//							if (result.code == 0 && Convert.ToString(result.success).Trim().ToUpper() == "TRUE")
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Code = 0;
		//								res.Message = "Success";
		//							}
		//							else
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Message = result.msg;
		//								res.Code = 1;
		//							}
		//						}
		//					}
		//					else
		//					{
		//						res.PlayerID = PlayerID;
		//						res.Game = GameID;
		//						res.Message = "Game account not found.";
		//						res.Code = 2;
		//					}
		//				}
		//				//allcity | not able to return after balance | currency fix in provider side
		//				else if (GameID == "ALLCITY")
		//				{
		//					var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

		//					if (record != null)
		//					{
		//						apiurl = string.Format("http://gameapi-allcity.azurewebsites.net/api/AC/Withdraw?account={0}&amount={1}", record.GamePlayerID, string.Format("{0:0.##}", Amount));
		//						var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
		//						httpWebRequest.ContentType = "application/json";
		//						httpWebRequest.Method = "GET";
		//						httpWebRequest.Timeout = TimeoutSec;
		//						httpWebRequest.ReadWriteTimeout = TimeoutSec;
		//						var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

		//						using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
		//						{
		//							var responseText = streamReader.ReadToEnd();
		//							dynamic result = JObject.Parse(responseText);

		//							if (Convert.ToInt16(result.code) == 200)
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Code = 0;
		//								res.Message = "Success";
		//							}
		//							else
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Message = result.msg;
		//								res.Code = 1;
		//							}
		//						}
		//					}
		//					else
		//					{
		//						res.PlayerID = PlayerID;
		//						res.Game = GameID;
		//						res.Message = "Game account not found.";
		//						res.Code = 2;
		//					}
		//				}
		//				//vpower | able to return balance | result.Credit | currency fix in provider side
		//				else if (GameID == "VPOWER")
		//				{
		//					var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

		//					if (record != null)
		//					{
		//						apiurl = string.Format("http://gameapi-vpower.azurewebsites.net/api/VPower/Withdraw?username={0}&amount={1}", record.GamePlayerID, string.Format("{0:0.##}", Amount));
		//						var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
		//						httpWebRequest.ContentType = "application/json";
		//						httpWebRequest.Method = "GET";
		//						httpWebRequest.Timeout = TimeoutSec;
		//						httpWebRequest.ReadWriteTimeout = TimeoutSec;
		//						var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

		//						using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
		//						{
		//							var responseText = streamReader.ReadToEnd();
		//							dynamic result = JObject.Parse(responseText);

		//							if (Convert.ToInt16(result.ErrorCode) == 0)
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Code = 0;
		//								res.Message = "Success";
		//							}
		//							else
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Game = GameID;
		//								res.Message = GetVPowerErrorMsg(result.ErrorCode);
		//								res.Code = 1;
		//							}
		//						}
		//					}
		//					else
		//					{
		//						res.PlayerID = PlayerID;
		//						res.Game = GameID;
		//						res.Message = "Game account not found.";
		//						res.Code = 2;
		//					}
		//				}

		//				return new HttpResponseMessage()
		//				{
		//					StatusCode = HttpStatusCode.OK,
		//					Content = new StringContent(JsonConvert.SerializeObject(res), System.Text.Encoding.UTF8, "application/json")
		//				};
		//			}


		//		}
		//		catch (Exception ex)
		//		{
		//			_common.LogError("Withdraw" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), "Player");
		//			return new HttpResponseMessage(HttpStatusCode.InternalServerError);
		//		}
		//	}
		//}

		[HttpGet]
		public HttpResponseMessage CheckAndCreateAccount(string Game, string PlayerID, string Currency)
		{
			lock (MyLock)
			{
				string apiurl = "";
				Common _common = new Common();
				int TimeoutSec = 180000; //3 mins

				try
				{
					using (BingoConn db = new BingoConn())
					{
						var res = new CheckAndCreateAccountVM();
						string GameID = Game.ToUpper().Trim();

						//bka
						//if (GameID == "BKA")
						//{
						//	var client = new RestClient("http://180.215.206.36:4613/Cash/CashAPI.asmx?op=GameBalance");
						//	client.Timeout = -1;
						//	var request = new RestRequest(Method.POST);
						//	request.AddHeader("Content-Type", "text/xml");
						//	request.AddParameter("text/xml", "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n"
						//			+ "<soap:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">\r\n  "
						//			+ "<soap:Body>\r\n    "
						//			+ "<GameBalance xmlns=\"http://tempuri.org/\">\r\n      "
						//			+ "<CompanyID>BingoTown</CompanyID>\r\n      "
						//			+ "<Account>" + PlayerID + "</Account>\r\n      "
						//			+ "<Password></Password>\r\n      "
						//			+ "<GameID>3</GameID>\r\n    "
						//			+ "</GameBalance>\r\n  "
						//			+ "</soap:Body>\r\n"
						//			+ "</soap:Envelope>", ParameterType.RequestBody);
						//	IRestResponse response = client.Execute(request);

						//	XmlDocument xmlDoc1 = new XmlDocument();
						//	xmlDoc1.LoadXml(response.Content);
						//	string responseText = xmlDoc1.InnerText;

						//	//_common.LogError("GetPlayerGameBalance|BKA|" + PlayerID + " " + DateTime.Now.ToString() + " " + responseText, "Player");

						//	dynamic result = JObject.Parse(responseText);

						//	if (result.Code == 1)
						//	{
						//		res.PlayerID = PlayerID;
						//		res.Balance = Convert.ToDecimal(result.Message);
						//		res.Game = GameID;
						//		res.Message = "Success";
						//		res.Code = 0;
						//	}
						//	else
						//	{
						//		res.PlayerID = PlayerID;
						//		res.Balance = null;
						//		res.Game = GameID;
						//		res.Message = result.Message;
						//		res.Code = 1;
						//	}
						//}
						//918kiss
						if (GameID == "918KISS")
						{
							var record = db.GameMappings.Where(x =>
								x.Game.ToUpper().Trim() == GameID &&
								x.PlayerID.ToUpper() == "918" + PlayerID.ToUpper() &&
								x.Currency.Trim().ToUpper() == Currency.ToUpper()
							).FirstOrDefault();

							if (record == null)
							{
								apiurl = string.Format("http://gameapi-918.azurewebsites.net/api/BT918/Register?UID={0}&IP={1}", "918" + PlayerID.ToUpper(), GetIPAddress());
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

									if (Convert.ToString(result.Message).ToUpper() == "SUCCESS")
									{
										res.PlayerID = PlayerID;
										res.GamePlayerID = result.Account;
										res.Game = GameID;
										res.Message = "Successfully created account.";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = PlayerID;
										res.GamePlayerID = null;
										res.Game = GameID;
										res.Message = result.Message;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = PlayerID;
								res.GamePlayerID = record.GamePlayerID;
								res.Game = GameID;
								res.Message = "Retrieve Success";
								res.Code = 1;
							}
						}
						//bg
						else if (GameID == "BG")
						{
							var record = db.GameMappings.Where(x =>
								x.Game.ToUpper().Trim() == GameID &&
								x.PlayerID.ToUpper() == PlayerID.ToUpper() &&
								x.Currency.Trim().ToUpper() == Currency.ToUpper()
							).FirstOrDefault();

							if (record == null)
							{
								string gamePlayerID = FormGamePlayerID(GameID, PlayerID, Currency);
								apiurl = string.Format("http://gameapi-bg.azurewebsites.net/api/BG/Register?UID={0}&IP={1}&mcUID={2}", gamePlayerID, GetIPAddress(), PlayerID);
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
											res.PlayerID = PlayerID;
											res.GamePlayerID = Convert.ToString(result.result.loginId);
											res.Game = GameID;
											res.Message = "Success";
											res.Code = 0;
										}
										else
										{
											res.PlayerID = PlayerID;
											res.GamePlayerID = null;
											res.Game = GameID;
											res.Message = result.error.message;
											res.Code = 2;
										}
									}
									else
									{
										res.PlayerID = PlayerID;
										res.GamePlayerID = null;
										res.Game = GameID;
										res.Message = result.error.message;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = PlayerID;
								res.GamePlayerID = record.GamePlayerID;
								res.Game = GameID;
								res.Message = "Retrieve Success";
								res.Code = 1;
							}
						}
						//ag
						else if (GameID == "AG")
						{
							var record = db.GameMappings.Where(x =>
								x.Game.ToUpper().Trim() == GameID &&
								x.PlayerID.ToUpper() == PlayerID.ToUpper() &&
								x.Currency.Trim().ToUpper() == Currency.ToUpper()
							).FirstOrDefault();

							if (record == null)
							{
								string gamePlayerID = FormGamePlayerID(GameID, PlayerID, Currency);
								apiurl = string.Format("http://gameapi-ag.azurewebsites.net/api/AG/CheckOrCreateAccount?loginname={0}&currency={1}&mcUID", gamePlayerID, Currency, PlayerID);
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
										res.PlayerID = PlayerID;
										res.GamePlayerID = gamePlayerID;
										res.Game = GameID;
										res.Message = "Success";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = PlayerID;
										res.GamePlayerID = null;
										res.Game = GameID;
										res.Message = result.ErrorDesc;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = PlayerID;
								res.GamePlayerID = record.GamePlayerID;
								res.Game = GameID;
								res.Message = "Retrieve Success";
								res.Code = 1;
							}
						}
						//playtech
						else if (GameID == "PT")
						{
							var record = db.GameMappings.Where(x =>
								x.Game.ToUpper().Trim() == GameID &&
								x.PlayerID.ToUpper() == PlayerID.ToUpper() &&
								x.Currency.Trim().ToUpper() == Currency.ToUpper()
							).FirstOrDefault();

							if (record == null)
							{
								string gamePlayerID = FormGamePlayerID(GameID, PlayerID, Currency);
								apiurl = string.Format("http://gameapi-pt.azurewebsites.net/api/PT/CreatePlayer?playerName={0}&mcUID", gamePlayerID, PlayerID);
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
											res.PlayerID = PlayerID;
											res.GamePlayerID = gamePlayerID;
											res.Game = GameID;
											res.Message = "Success";
											res.Code = 0;
										}
										else
										{
											res.PlayerID = PlayerID;
											res.GamePlayerID = null;
											res.Game = GameID;
											res.Message = result.error;
											res.Code = 2;
										}
									}
									else
									{
										res.PlayerID = PlayerID;
										res.GamePlayerID = null;
										res.Game = GameID;
										res.Message = result.error;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = PlayerID;
								res.GamePlayerID = record.GamePlayerID;
								res.Game = GameID;
								res.Message = "Retrieve Success";
								res.Code = 1;
							}
						}
						//ig
						else if (GameID == "IG")
						{
							var record = db.GameMappings.Where(x =>
								x.Game.ToUpper().Trim() == GameID &&
								x.PlayerID.ToUpper() == PlayerID.ToUpper() &&
								x.Currency.Trim().ToUpper() == Currency.ToUpper()
							).FirstOrDefault();

							if (record == null)
							{
								string gamePlayerID = FormGamePlayerID(GameID, PlayerID, Currency);
								apiurl = string.Format("http://gameapi-play8.azurewebsites.net/api/Play8/Register?UID={0}&IP={1}&mcUID", gamePlayerID, GetIPAddress(), PlayerID);
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
										res.PlayerID = PlayerID;
										res.GamePlayerID = gamePlayerID;
										res.Game = GameID;
										res.Message = "Success";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = PlayerID;
										res.GamePlayerID = null;
										res.Game = GameID;
										res.Message = result.Message;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = PlayerID;
								res.GamePlayerID = record.GamePlayerID;
								res.Game = GameID;
								res.Message = "Retrieve Success";
								res.Code = 1;
							}
						}
						//bt28win
						else if (GameID == "BT28WIN")
						{
							var record = db.GameMappings.Where(x =>
								x.Game.ToUpper().Trim() == GameID &&
								x.PlayerID.ToUpper() == PlayerID.ToUpper() &&
								x.Currency.Trim().ToUpper() == Currency.ToUpper()
							).FirstOrDefault();

							if (record == null)
							{
								string gamePlayerID = FormGamePlayerID(GameID, PlayerID, Currency);
								apiurl = string.Format("http://gameapi-28win.azurewebsites.net/api/BT28Win/CreatePlayer?loginID={0}&loginPass={1}&fullName={2}&mcUID={3}", gamePlayerID, "123456789", PlayerID, PlayerID);
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
										res.PlayerID = PlayerID;
										res.GamePlayerID = gamePlayerID;
										res.Game = GameID;
										res.Message = "Success";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = PlayerID;
										res.GamePlayerID = null;
										res.Game = GameID;
										res.Message = result.ErrorDesc;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = PlayerID;
								res.GamePlayerID = record.GamePlayerID;
								res.Game = GameID;
								res.Message = "Retrieve Success";
								res.Code = 1;
							}

						}
						//tpg
						else if (GameID == "TPG")
						{
							var record = db.GameMappings.Where(x =>
								x.Game.ToUpper().Trim() == GameID &&
								x.PlayerID.ToUpper() == PlayerID.ToUpper() &&
								x.Currency.Trim().ToUpper() == Currency.ToUpper()
							).FirstOrDefault();

							if (record == null)
							{
								string gamePlayerID = FormGamePlayerID(GameID, PlayerID, Currency);
								apiurl = string.Format("http://game-tiger.azurewebsites.net/api/TPG/CreatePlayer?playerName={0}&displayName={1}&currency={2}&mcUID={3}",
																				gamePlayerID, PlayerID, Currency, PlayerID);
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
										res.PlayerID = PlayerID;
										res.GamePlayerID = gamePlayerID;
										res.Game = GameID;
										res.Message = "Success";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = PlayerID;
										res.GamePlayerID = null;
										res.Game = GameID;
										res.Message = result.message;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = PlayerID;
								res.GamePlayerID = record.GamePlayerID;
								res.Game = GameID;
								res.Message = "Retrieve Success";
								res.Code = 1;
							}
						}
						//allcity
						else if (GameID == "ALLCITY")
						{
							var record = db.GameMappings.Where(x =>
								x.Game.ToUpper().Trim() == GameID &&
								x.PlayerID.ToUpper() == PlayerID.ToUpper() &&
								x.Currency.Trim().ToUpper() == Currency.ToUpper()
							).FirstOrDefault();

							if (record == null)
							{
								string gamePlayerID = FormGamePlayerID(GameID, PlayerID, Currency);
								apiurl = string.Format("http://gameapi-allcity.azurewebsites.net/api/AC/AddUser?account={0}&mcUID", gamePlayerID, PlayerID);
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
										res.PlayerID = PlayerID;
										res.GamePlayerID = gamePlayerID;
										res.Game = GameID;
										res.Message = "Success";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = PlayerID;
										res.GamePlayerID = null;
										res.Game = GameID;
										res.Message = result.msg;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = PlayerID;
								res.GamePlayerID = record.GamePlayerID;
								res.Game = GameID;
								res.Message = "Retrieve Success";
								res.Code = 1;
							}
						}
						//ibc
						else if (GameID == "IBC")
						{
							var record = db.GameMappings.Where(x =>
								x.Game.ToUpper().Trim() == GameID &&
								x.PlayerID.ToUpper() == PlayerID.ToUpper() &&
								x.Currency.Trim().ToUpper() == Currency.ToUpper()
							).FirstOrDefault();

							if (record == null)
							{
								string gamePlayerID = FormGamePlayerID(GameID, PlayerID, Currency);
								apiurl = string.Format("http://ibc.bingotown88.com/api/IBC/CreateMember?username={0}&mcUID={1}", gamePlayerID, PlayerID);
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
										res.PlayerID = PlayerID;
										res.GamePlayerID = gamePlayerID;
										res.Game = GameID;
										res.Message = "Success";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = PlayerID;
										res.GamePlayerID = null;
										res.Game = GameID;
										res.Message = result.message;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = PlayerID;
								res.GamePlayerID = record.GamePlayerID;
								res.Game = GameID;
								res.Message = "Retrieve Success";
								res.Code = 1;
							}
						}
						//evo888
						else if (GameID == "EVO888")
						{
							var record = db.GameMappings.Where(x =>
								x.Game.ToUpper().Trim() == GameID &&
								x.PlayerID.ToUpper() == PlayerID.ToUpper() &&
								x.Currency.Trim().ToUpper() == Currency.ToUpper()
							).FirstOrDefault();

							if (record == null)
							{
								apiurl = string.Format("http://gameapi-evo888.azurewebsites.net/api/Evo888/AddUser?mcUID={0}", PlayerID);
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
										res.PlayerID = PlayerID;
										res.GamePlayerID = Convert.ToString(result.results.username);
										res.Game = GameID;
										res.Message = "Success";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = PlayerID;
										res.GamePlayerID = null;
										res.Game = GameID;
										res.Message = result.msg;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = PlayerID;
								res.GamePlayerID = record.GamePlayerID;
								res.Game = GameID;
								res.Message = "Retrieve Success";
								res.Code = 1;
							}
						}
						//vpower
						else if (GameID == "VPOWER")
						{
							var record = db.GameMappings.Where(x =>
								x.Game.ToUpper().Trim() == GameID &&
								x.PlayerID.ToUpper() == PlayerID.ToUpper() &&
								x.Currency.Trim().ToUpper() == Currency.ToUpper()
							).FirstOrDefault();

							if (record == null)
							{
								string gamePlayerID = FormGamePlayerID(GameID, PlayerID, Currency);
								apiurl = string.Format("http://gameapi-vpower.azurewebsites.net/api/VPower/AddUser?username={0}&mcUID={1}", gamePlayerID, PlayerID);
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
										res.PlayerID = PlayerID;
										res.GamePlayerID = Convert.ToString(result.Username);
										res.Game = GameID;
										res.Message = "Success";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = PlayerID;
										res.GamePlayerID = null;
										res.Game = GameID;
										res.Message = GetVPowerErrorMsg(result.ErrorCode);
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = PlayerID;
								res.GamePlayerID = record.GamePlayerID;
								res.Game = GameID;
								res.Message = "Retrieve Success";
								res.Code = 1;
							}
						}
						//tpgfish
						else if (GameID == "TPGFISH")
						{
							var record = db.GameMappings.Where(x =>
								x.Game.ToUpper().Trim() == GameID &&
								x.PlayerID.ToUpper() == PlayerID.ToUpper() &&
								x.Currency.Trim().ToUpper() == Currency.ToUpper()
							).FirstOrDefault();

							if (record == null)
							{
								string gamePlayerID = FormGamePlayerID(GameID, PlayerID, Currency);
								apiurl = string.Format("http://game-tiger.azurewebsites.net/api/TPG/CreatePlayer?playerName={0}&displayName={1}&currency={2}&mcUID={3}",
																				gamePlayerID, PlayerID, Currency, PlayerID);
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
										res.PlayerID = PlayerID;
										res.GamePlayerID = gamePlayerID;
										res.Game = GameID;
										res.Message = "Success";
										res.Code = 0;
									}
									else
									{
										res.PlayerID = PlayerID;
										res.GamePlayerID = null;
										res.Game = GameID;
										res.Message = result.message;
										res.Code = 2;
									}
								}
							}
							else
							{
								res.PlayerID = PlayerID;
								res.GamePlayerID = record.GamePlayerID;
								res.Game = GameID;
								res.Message = "Retrieve Success";
								res.Code = 1;
							}
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
					_common.LogError("CheckAndCreateAccount " + DateTime.Now.ToString() + " " + ex.Message.ToString(), "Player");
					return new HttpResponseMessage(HttpStatusCode.InternalServerError);
				}
			}
		}

		//[HttpGet]
		//public HttpResponseMessage GetGameUrl(string Game, string PlayerID, string Currency, string Language)
		//{
		//	lock (MyLock)
		//	{
		//		string apiurl = "";
		//		Common _common = new Common();
		//		int TimeoutSec = 180000; //3 mins

		//		try
		//		{
		//			using (BingoConn db = new BingoConn())
		//			{
		//				var res = new GetGameUrlVM();
		//				string GameID = Game.ToUpper().Trim();

		//				//918kiss
		//				if (GameID == "918KISS")
		//				{
		//					var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == "918" + PlayerID.ToUpper()).FirstOrDefault();

		//					if (record != null)
		//					{
		//						res.PlayerID = PlayerID;
		//						res.Url = "";
		//						res.Game = GameID;
		//						res.Message = "Success";
		//						res.Code = 0;
		//					}
		//					else
		//					{
		//						res.PlayerID = PlayerID;
		//						res.Url = null;
		//						res.Game = GameID;
		//						res.Message = "Game account not found.";
		//						res.Code = 2;
		//					}
		//				}
		//				//bg
		//				else if (GameID == "BG")
		//				{
		//					var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

		//					if (record != null)
		//					{
		//						string lang = Language.ToUpper() == "ZH" ? "zh_CN" : "en_US";
		//						apiurl = string.Format("http://gameapi-bg.azurewebsites.net/api/BG/Login?UID={0}&IP={1}&Language={2}", record.GamePlayerID, GetIPAddress(), lang);
		//						var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
		//						httpWebRequest.ContentType = "application/json";
		//						httpWebRequest.Method = "GET";
		//						httpWebRequest.Timeout = TimeoutSec;
		//						httpWebRequest.ReadWriteTimeout = TimeoutSec;
		//						var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

		//						using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
		//						{
		//							var responseText = streamReader.ReadToEnd();
		//							dynamic result = JObject.Parse(responseText);

		//							if (result.error == null)
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Url = Convert.ToString(result.result);
		//								res.Game = GameID;
		//								res.Message = "Success";
		//								res.Code = 0;
		//							}
		//							else
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Url = null;
		//								res.Game = GameID;
		//								res.Message = result.error.message;
		//								res.Code = 1;
		//							}
		//						}
		//					}
		//					else
		//					{
		//						res.PlayerID = PlayerID;
		//						res.Url = null;
		//						res.Game = GameID;
		//						res.Message = "Game account not found.";
		//						res.Code = 2;
		//					}
		//				}
		//				//ag
		//				else if (GameID == "AG")
		//				{
		//					var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

		//					if (record != null)
		//					{
		//						apiurl = string.Format("http://gameapi-ag.azurewebsites.net/api/AG/ForwardGame?loginname={0}&currency={1}&lang={2}", record.GamePlayerID, Currency, Language);
		//						var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
		//						httpWebRequest.ContentType = "application/json";
		//						httpWebRequest.Method = "GET";
		//						httpWebRequest.Timeout = TimeoutSec;
		//						httpWebRequest.ReadWriteTimeout = TimeoutSec;
		//						var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

		//						using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
		//						{
		//							var responseText = streamReader.ReadToEnd();
		//							dynamic result = JObject.Parse(responseText);

		//							if (string.IsNullOrWhiteSpace(Convert.ToString(result.ErrorDesc)))
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Url = Convert.ToString(result.ErrorDesc);
		//								res.Game = GameID;
		//								res.Message = "Success";
		//								res.Code = 0;
		//							}
		//							else
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Url = null;
		//								res.Game = GameID;
		//								res.Message = result.ErrorDesc;
		//								res.Code = 1;
		//							}
		//						}
		//					}
		//					else
		//					{
		//						res.PlayerID = PlayerID;
		//						res.Url = null;
		//						res.Game = GameID;
		//						res.Message = "Game account not found.";
		//						res.Code = 2;
		//					}
		//				}
		//				//playtech
		//				else if (GameID == "PT")
		//				{
		//					var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

		//					if (record != null)
		//					{
		//						string lang = Language.ToUpper() == "ZH" ? "ZH-CN" : "EN";
		//						apiurl = string.Format("http://gameapi-pt.azurewebsites.net/api/PT/LaunchGame?playerName={0}&lang={1}", record.GamePlayerID, lang);
		//						var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
		//						httpWebRequest.ContentType = "application/json";
		//						httpWebRequest.Method = "GET";
		//						httpWebRequest.Timeout = TimeoutSec;
		//						httpWebRequest.ReadWriteTimeout = TimeoutSec;
		//						var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

		//						using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
		//						{
		//							var responseText = streamReader.ReadToEnd();
		//							dynamic result = JObject.Parse(responseText);

		//							if (result.error == null)
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Url = Convert.ToString(result.error);
		//								res.Game = GameID;
		//								res.Message = "Success";
		//								res.Code = 0;
		//							}
		//							else
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Url = null;
		//								res.Game = GameID;
		//								res.Message = result.error;
		//								res.Code = 1;
		//							}
		//						}
		//					}
		//					else
		//					{
		//						res.PlayerID = PlayerID;
		//						res.Url = null;
		//						res.Game = GameID;
		//						res.Message = "Game account not found.";
		//						res.Code = 2;
		//					}
		//				}
		//				//ig
		//				else if (GameID == "IG")
		//				{
		//					var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

		//					if (record != null)
		//					{
		//						res.PlayerID = PlayerID;
		//						res.Url = "http://www.igamingdemo.com/app/";
		//						res.Game = GameID;
		//						res.Message = "Success";
		//						res.Code = 0;
		//					}
		//					else
		//					{
		//						res.PlayerID = PlayerID;
		//						res.Url = null;
		//						res.Game = GameID;
		//						res.Message = "Game account not found.";
		//						res.Code = 2;
		//					}
		//				}
		//				//bt28win
		//				else if (GameID == "BT28WIN")
		//				{
		//					var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

		//					if (record != null)
		//					{
		//						apiurl = string.Format("http://gameapi-28win.azurewebsites.net/api/BT28Win/Bet?user={0}&pass={1}", record.GamePlayerID, "123456789");
		//						var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
		//						httpWebRequest.ContentType = "application/json";
		//						httpWebRequest.Method = "GET";
		//						httpWebRequest.Timeout = TimeoutSec;
		//						httpWebRequest.ReadWriteTimeout = TimeoutSec;

		//						var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

		//						using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
		//						{
		//							var responseText = streamReader.ReadToEnd();
		//							dynamic result = JObject.Parse(responseText);

		//							if (Convert.ToString(result.ErrorCode) == "0")
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Url = Convert.ToString(result.Data);
		//								res.Game = GameID;
		//								res.Message = "Success";
		//								res.Code = 0;
		//							}
		//							else
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Url = null;
		//								res.Game = GameID;
		//								res.Message = result.ErrorDesc;
		//								res.Code = 1;
		//							}
		//						}
		//					}
		//					else
		//					{
		//						res.PlayerID = PlayerID;
		//						res.Url = null;
		//						res.Game = "BT28Win";
		//						res.Message = "Game account not found.";
		//						res.Code = 2;
		//					}
		//				}
		//				//tpg
		//				else if (GameID == "TPG")
		//				{
		//					var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

		//					if (record != null)
		//					{
		//						string lang = Language.ToUpper() == "ZH" ? "zh-ch" : "en";
		//						apiurl = string.Format("http://game-tiger.azurewebsites.net/api/TPG/LaunchLobby?playerName={0}&displayName={1}&currency={2}&lang={3}",
		//																		record.GamePlayerID, PlayerID, Currency, lang);
		//						var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
		//						httpWebRequest.ContentType = "application/json";
		//						httpWebRequest.Method = "GET";
		//						httpWebRequest.Timeout = TimeoutSec;
		//						httpWebRequest.ReadWriteTimeout = TimeoutSec;
		//						var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

		//						using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
		//						{
		//							var responseText = streamReader.ReadToEnd();
		//							dynamic result = JObject.Parse(responseText);

		//							if (result.status == 1)
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Url = Convert.ToString(result.message);
		//								res.Game = GameID;
		//								res.Message = "Success";
		//								res.Code = 0;
		//							}
		//							else
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Url = null;
		//								res.Game = GameID;
		//								res.Message = result.message;
		//								res.Code = 1;
		//							}
		//						}
		//					}
		//					else
		//					{
		//						res.PlayerID = PlayerID;
		//						res.Url = null;
		//						res.Game = GameID;
		//						res.Message = "Game account not found.";
		//						res.Code = 2;
		//					}
		//				}
		//				//allcity
		//				else if (GameID == "ALLCITY")
		//				{
		//					var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

		//					if (record != null)
		//					{
		//						apiurl = string.Format("http://gameapi-allcity.azurewebsites.net/api/AC/GetGameApk");
		//						var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
		//						httpWebRequest.ContentType = "application/json";
		//						httpWebRequest.Method = "GET";
		//						httpWebRequest.Timeout = TimeoutSec;
		//						httpWebRequest.ReadWriteTimeout = TimeoutSec;
		//						var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

		//						using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
		//						{
		//							var responseText = streamReader.ReadToEnd();
		//							dynamic result = JObject.Parse(responseText);

		//							if (result.code == 200)
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Url = Convert.ToString(result.apk);
		//								res.Game = GameID;
		//								res.Message = "Success";
		//								res.Code = 0;
		//							}
		//							else
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Url = null;
		//								res.Game = GameID;
		//								res.Message = result.msg;
		//								res.Code = 1;
		//							}
		//						}
		//					}
		//					else
		//					{
		//						res.PlayerID = PlayerID;
		//						res.Url = null;
		//						res.Game = GameID;
		//						res.Message = "Game account not found.";
		//						res.Code = 2;
		//					}
		//				}
		//				//ibc
		//				else if (GameID == "IBC")
		//				{
		//					var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

		//					if (record != null)
		//					{
		//						string lang = Language.ToUpper() == "ZH" ? "cs" : "en";

		//						apiurl = string.Format("http://ibc.bingotown88.com/api/IBC/LaunchGame?username={0}&lang={1}", record.GamePlayerID, lang);
		//						var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
		//						httpWebRequest.ContentType = "application/json";
		//						httpWebRequest.Method = "GET";
		//						httpWebRequest.Timeout = TimeoutSec;
		//						httpWebRequest.ReadWriteTimeout = TimeoutSec;
		//						var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

		//						using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
		//						{
		//							var responseText = streamReader.ReadToEnd();
		//							dynamic result = JObject.Parse(responseText);

		//							if (result.error_code == 0)
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Url = Convert.ToString(result.Data);
		//								res.Game = GameID;
		//								res.Code = 0;
		//								res.Message = result.message;
		//							}
		//							else
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Url = null;
		//								res.Game = GameID;
		//								res.Code = 1;
		//								res.Message = result.message;
		//							}
		//						}
		//					}
		//					else
		//					{
		//						res.PlayerID = PlayerID;
		//						res.Url = null;
		//						res.Game = GameID;
		//						res.Code = 2;
		//						res.Message = "Game account not found.";
		//					}
		//				}
		//				//evo888
		//				else if (GameID == "EVO888")
		//				{
		//					var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

		//					if (record != null)
		//					{
		//						apiurl = string.Format("http://gameapi-evo888.azurewebsites.net/api/Evo888/GetGameApk");
		//						var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
		//						httpWebRequest.ContentType = "application/json";
		//						httpWebRequest.Method = "GET";
		//						httpWebRequest.Timeout = TimeoutSec;
		//						httpWebRequest.ReadWriteTimeout = TimeoutSec;
		//						var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

		//						using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
		//						{
		//							var responseText = streamReader.ReadToEnd();
		//							dynamic result = JObject.Parse(responseText);

		//							if (result.code == 0 && Convert.ToString(result.success).Trim().ToUpper() == "TRUE")
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Url = Convert.ToString(result.apk);
		//								res.Game = GameID;
		//								res.Code = 0;
		//								res.Message = "Success";
		//							}
		//							else
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Url = null;
		//								res.Game = GameID;
		//								res.Message = result.msg;
		//								res.Code = 1;
		//							}
		//						}
		//					}
		//					else
		//					{
		//						res.PlayerID = PlayerID;
		//						res.Url = null;
		//						res.Game = GameID;
		//						res.Message = "Game account not found.";
		//						res.Code = 2;
		//					}
		//				}
		//				//vpower
		//				else if (GameID == "VPOWER")
		//				{
		//					var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

		//					if (record != null)
		//					{
		//						apiurl = string.Format("http://gameapi-vpower.azurewebsites.net/api/VPower/LaunchLobby?username={0}&lang={1}",
		//																		record.GamePlayerID, Language);
		//						var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
		//						httpWebRequest.ContentType = "application/json";
		//						httpWebRequest.Method = "GET";
		//						httpWebRequest.Timeout = TimeoutSec;
		//						httpWebRequest.ReadWriteTimeout = TimeoutSec;
		//						var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

		//						using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
		//						{
		//							var responseText = streamReader.ReadToEnd();
		//							dynamic result = JObject.Parse(responseText);

		//							if (result.ErrorCode == 0)
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Url = Convert.ToString(result.Url);
		//								res.Game = GameID;
		//								res.Message = "Success";
		//								res.Code = 0;
		//							}
		//							else
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Url = null;
		//								res.Game = GameID;
		//								res.Message = result.message;
		//								res.Code = 1;
		//							}
		//						}
		//					}
		//					else
		//					{
		//						res.PlayerID = PlayerID;
		//						res.Url = null;
		//						res.Game = GameID;
		//						res.Message = "Game account not found.";
		//						res.Code = 2;
		//					}
		//				}
		//				//tpgfish
		//				else if (GameID == "TPGFISH")
		//				{
		//					var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

		//					if (record != null)
		//					{
		//						string lang = Language.ToUpper() == "ZH" ? "zh-ch" : "en";
		//						apiurl = string.Format("http://game-tigerfish.azurewebsites.net/api/TPGFish/LaunchLobby?playerName={0}&displayName={1}&currency={2}&lang={3}",
		//																		record.GamePlayerID, PlayerID, Currency, lang);
		//						var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
		//						httpWebRequest.ContentType = "application/json";
		//						httpWebRequest.Method = "GET";
		//						httpWebRequest.Timeout = TimeoutSec;
		//						httpWebRequest.ReadWriteTimeout = TimeoutSec;
		//						var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

		//						using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
		//						{
		//							var responseText = streamReader.ReadToEnd();
		//							dynamic result = JObject.Parse(responseText);

		//							if (result.status == 1)
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Url = Convert.ToString(result.message);
		//								res.Game = GameID;
		//								res.Message = "Success";
		//								res.Code = 0;
		//							}
		//							else
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Url = null;
		//								res.Game = GameID;
		//								res.Message = result.message;
		//								res.Code = 1;
		//							}
		//						}
		//					}
		//					else
		//					{
		//						res.PlayerID = PlayerID;
		//						res.Url = null;
		//						res.Game = GameID;
		//						res.Message = "Game account not found.";
		//						res.Code = 2;
		//					}
		//				}
		//				//sexygaming
		//				else if (GameID == "SEXYGAMING")
		//				{
		//					var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

		//					if (record != null)
		//					{
		//						string lang = Language.ToUpper() == "ZH" ? "zh-cn" : "en-us";
		//						apiurl = string.Format("http://gameapi-sexy.azurewebsites.net/api/Sexy/LaunchGame?username={0}&lang={1}", record.GamePlayerID, lang);
		//						var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
		//						httpWebRequest.ContentType = "application/json";
		//						httpWebRequest.Method = "GET";
		//						httpWebRequest.Timeout = TimeoutSec;
		//						httpWebRequest.ReadWriteTimeout = TimeoutSec;
		//						var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

		//						using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
		//						{
		//							var responseText = streamReader.ReadToEnd();
		//							dynamic result = JObject.Parse(responseText);

		//							if (Convert.ToInt16(result.error) == 0 && Convert.ToString(result.message).ToUpper() == "SUCCESS")
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Url = Convert.ToString(result.data);
		//								res.Game = "SEXYGAMING";
		//								res.Message = "Success";
		//								res.Code = 0;
		//							}
		//							else
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Url = null;
		//								res.Game = "SEXYGAMING";
		//								res.Message = result.ErrorDesc;
		//								res.Code = 1;
		//							}
		//						}
		//					}
		//					else
		//					{
		//						res.PlayerID = PlayerID;
		//						res.Url = null;
		//						res.Game = "SEXYGAMING";
		//						res.Message = "Game account not found.";
		//						res.Code = 2;
		//					}
		//				}
		//				//sexygaming
		//				else if (GameID == "SPADEGAMING")
		//				{
		//					var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

		//					if (record != null)
		//					{
		//						string lang = Language.ToUpper() == "ZH" ? "zh_CN" : "en_US";
		//						apiurl = string.Format("http://gameapi-spade.azurewebsites.net/api/Spade/LaunchGame?username={0}&mcUID={1}&lang={2}", record.GamePlayerID, record.PlayerID, lang);
		//						var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
		//						httpWebRequest.ContentType = "application/json";
		//						httpWebRequest.Method = "GET";
		//						httpWebRequest.Timeout = TimeoutSec;
		//						httpWebRequest.ReadWriteTimeout = TimeoutSec;
		//						var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

		//						using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
		//						{
		//							var responseText = streamReader.ReadToEnd();
		//							dynamic result = JObject.Parse(responseText);

		//							if (Convert.ToInt16(result.code) == 0 && Convert.ToString(result.msg).ToUpper() == "SUCCESS")
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Url = Convert.ToString(result.url);
		//								res.Game = GameID;
		//								res.Message = "Success";
		//								res.Code = 0;
		//							}
		//							else
		//							{
		//								res.PlayerID = PlayerID;
		//								res.Url = null;
		//								res.Game = GameID;
		//								res.Message = result.ErrorDesc;
		//								res.Code = 1;
		//							}
		//						}
		//					}
		//					else
		//					{
		//						res.PlayerID = PlayerID;
		//						res.Url = null;
		//						res.Game = GameID;
		//						res.Message = "Game account not found.";
		//						res.Code = 2;
		//					}
		//				}

		//				return new HttpResponseMessage()
		//				{
		//					StatusCode = HttpStatusCode.OK,
		//					Content = new StringContent(JsonConvert.SerializeObject(res), System.Text.Encoding.UTF8, "application/json")
		//				};
		//			}


		//		}
		//		catch (Exception ex)
		//		{
		//			_common.LogError("GetGameUrl" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), "Player");
		//			return new HttpResponseMessage(HttpStatusCode.InternalServerError);
		//		}
		//	}
		//}

		public decimal? GetGameBalance(string Game, string PlayerID, string Currency)
		{
			try
			{
				lock (MyLock)
				{
					string apiurl = "";
					int TimeoutSec = 180000; //3 mins
					Common _common = new Common();

					try
					{
						using (BingoConn db = new BingoConn())
						{
							var res = new GetBalanceVM();
							string GameID = Game.ToUpper().Trim();

							//bka
							if (GameID == "BKA")
							{
								var client = new RestClient("http://180.215.206.36:4613/Cash/CashAPI.asmx?op=GameBalance");
								client.Timeout = -1;
								var request = new RestRequest(Method.POST);
								request.AddHeader("Content-Type", "text/xml");
								request.AddParameter("text/xml", "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n"
										+ "<soap:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">\r\n  "
										+ "<soap:Body>\r\n    "
										+ "<GameBalance xmlns=\"http://tempuri.org/\">\r\n      "
										+ "<CompanyID>BingoTown</CompanyID>\r\n      "
										+ "<Account>" + PlayerID + "</Account>\r\n      "
										+ "<Password></Password>\r\n      "
										+ "<GameID>3</GameID>\r\n    "
										+ "</GameBalance>\r\n  "
										+ "</soap:Body>\r\n"
										+ "</soap:Envelope>", ParameterType.RequestBody);
								IRestResponse response = client.Execute(request);

								XmlDocument xmlDoc1 = new XmlDocument();
								xmlDoc1.LoadXml(response.Content);
								string responseText = xmlDoc1.InnerText;

								dynamic result = JObject.Parse(responseText);

								if (result.Code == 1)
									return Convert.ToDecimal(result.Message);
								else
									return null;
							}
							//918kiss
							else if (GameID == "918KISS")
							{
								var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == "918" + PlayerID.ToUpper()).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format("http://gameapi-918.azurewebsites.net/api/BT918/GetBalance?UID={0}&IP={1}", record.GamePlayerID, GetIPAddress());
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

										if (Convert.ToString(result.Message).ToUpper() == "SUCCESS")
											return result.ScoreNum;
										else
											return null;
									}
								}
								else
									return null;
							}
							//bg
							else if (GameID == "BG")
							{
								var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format("http://gameapi-bg.azurewebsites.net/api/BG/GetBalance?UID={0}&IP={1}", record.GamePlayerID, GetIPAddress());
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
								var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format("http://gameapi-ag.azurewebsites.net/api/AG/GetBalance?loginname={0}&currency={1}", record.GamePlayerID, Currency);
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
								var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format("http://gameapi-pt.azurewebsites.net/api/PT/GetPlayerBalance?playerName={0}", record.GamePlayerID);
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
							//ig
							else if (GameID == "IG")
							{
								var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format("http://gameapi-play8.azurewebsites.net/api/Play8/GetBalance?UID={0}&IP={1}", record.GamePlayerID, GetIPAddress());
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
							//bt28win
							else if (GameID == "BT28WIN")
							{
								var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format("http://gameapi-28win.azurewebsites.net/api/BT28Win/GetProfile?loginID={0}", record.GamePlayerID);
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
							//tpg
							else if (GameID == "TPG")
							{
								var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format("http://game-tiger.azurewebsites.net/api/TPG/GetPlayerGameBalance?playerName={0}&displayName={1}&currency={2}&mcUID={3}",
																					record.GamePlayerID, PlayerID, Currency, PlayerID);
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
								var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format("http://game-tigerfish.azurewebsites.net/api/TPGFish/GetPlayerGameBalance?playerName={0}&displayName={1}&currency={2}&mcUID={3}",
																					record.GamePlayerID, PlayerID, Currency, PlayerID);
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
											return Convert.ToDecimal(result.totalBalance);
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
								var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format("http://gameapi-allcity.azurewebsites.net/api/AC/GetUserInfo?account={0}", record.GamePlayerID);
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
								var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format("http://ibc.bingotown88.com/api/IBC/CheckUserBalance?username={0}", record.GamePlayerID);
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
												return result.Data[0].error_code == 0 ? Convert.ToDecimal(result.Data[0].balance) : null;
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
								var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format("http://gameapi-evo888.azurewebsites.net/api/Evo888/GetUserInfo?username={0}", record.GamePlayerID);
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
								var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format("http://gameapi-vpower.azurewebsites.net/api/VPower/GetBalance?username={0}", record.GamePlayerID);
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
								var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format("http://gameapi-sexy.azurewebsites.net/api/Sexy/GetBalance?username={0}", record.GamePlayerID);
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
								var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format("http://gameapi-spade.azurewebsites.net/api/Spade/GetBalance?username={0}", record.GamePlayerID);
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

							return null;
						}
					}
					catch (Exception ex)
					{
						_common.LogError("GetGameBalance" + " " + DateTime.Now.ToString() + " " + ex.Message.ToString(), "Player");
						return null;
					}
				}
			}
			catch (Exception ex)
			{
				return null;
			}
		}

		[HttpGet]
		public HttpResponseMessage Withdraw(string Game, string PlayerID, string Currency) //Currency need to get from bingotown Game table and pass in here (by myself)
		{
			lock (MyLock)
			{
				string apiurl = "";
				Common _common = new Common();
				int TimeoutSec = 180000; //3 mins

				try
				{
					using (BingoConn db = new BingoConn())
					{
						var res = new WithdrawVM();
						string GameID = Game.ToUpper().Trim();

						decimal? Amount = GetGameBalance(GameID, PlayerID, Currency);

						if (!Amount.Equals(null))
						{
							//918kiss | able to return after balance | result.ScoreNum | no currency for 918 
							if (GameID == "918KISS")
							{
								var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == "918" + PlayerID.ToUpper()).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format("http://gameapi-918.azurewebsites.net/api/BT918/Transfer?UID={0}&Num={1}&IP={2}", record.GamePlayerID, string.Format("{0:0.##}", -1 * Amount), GetIPAddress());
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

										if (Convert.ToString(result.Message).ToUpper().Trim() == "SUCCESS")
										{
											res.PlayerID = PlayerID;
											res.Game = GameID;
											res.Message = "Successfully withdraw.";
											res.Code = 0;
											res.Amount = Amount;
										}
										else
										{
											res.PlayerID = PlayerID;
											res.Game = GameID;
											res.Message = result.Message;
											res.Code = 3;
											res.Amount = null;
										}
									}
								}
								else
								{
									res.PlayerID = PlayerID;
									res.Game = GameID;
									res.Message = "Game account not found.";
									res.Code = 2;
									res.Amount = null;
								}
							}
							//bg | able to return after balance | result.result | currency based on agent
							else if (GameID == "BG")
							{
								var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format("http://gameapi-bg.azurewebsites.net/api/BG/Transfer?UID={0}&IP={1}&Amount={2}", record.GamePlayerID, GetIPAddress(), string.Format("{0:0.##}", -1 * Amount));
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
											res.PlayerID = PlayerID;
											res.Game = GameID;
											res.Message = "Successfully withdraw.";
											res.Code = 0;
											res.Amount = Amount;
										}
										else
										{
											res.PlayerID = PlayerID;
											res.Game = GameID;
											res.Message = result.error.message;
											res.Code = 3;
											res.Amount = null;
										}
									}
								}
								else
								{
									res.PlayerID = PlayerID;
									res.Game = GameID;
									res.Message = "Game account not found.";
									res.Code = 2;
									res.Amount = null;
								}
							}
							//ag | not able to return after balance | currency based on what we pass in
							else if (GameID == "AG")
							{
								var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format("http://gameapi-ag.azurewebsites.net/api/AG/Withdraw?loginname={0}&credit={1}&currency={2}", record.GamePlayerID, string.Format("{0:0.##}", Amount), Currency);
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
											res.PlayerID = PlayerID;
											res.Game = GameID;
											res.Message = "Successfully withdraw.";
											res.Code = 0;
											res.Amount = Amount;
										}
										else
										{
											res.PlayerID = PlayerID;
											res.Game = GameID;
											res.Message = result.ErrorDesc;
											res.Code = 3;
											res.Amount = null;
										}
									}
								}
								else
								{
									res.PlayerID = PlayerID;
									res.Game = GameID;
									res.Message = "Game account not found.";
									res.Code = 2;
									res.Amount = null;
								}
							}
							//playtech | able to return balance | result.result.currentplayerbalance | currency define by provider
							else if (GameID == "PT")
							{
								var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format("http://gameapi-pt.azurewebsites.net/api/PT/Withdraw?playerName={0}&amount={1}", record.GamePlayerID, string.Format("{0:0.##}", Amount));
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
											res.PlayerID = PlayerID;
											res.Game = GameID;
											res.Message = "Successfully withdraw.";
											res.Code = 0;
											res.Amount = Amount;
										}
										else
										{
											res.PlayerID = PlayerID;
											res.Game = GameID;
											res.Message = result.error;
											res.Code = 3;
											res.Amount = null;
										}
									}
								}
								else
								{
									res.PlayerID = PlayerID;
									res.Game = GameID;
									res.Message = "Game account not found.";
									res.Code = 2;
									res.Amount = null;
								}
							}
							//ig | not able to return after balance | currency define by provider
							else if (GameID == "IG")
							{
								var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format("http://gameapi-play8.azurewebsites.net/api/Play8/Transfer?UID={0}&Action=1&Amount={1}&IP={1}", record.GamePlayerID, string.Format("{0:0.##}", Amount), GetIPAddress());
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
											res.PlayerID = PlayerID;
											res.Game = GameID;
											res.Message = "Successfully withdraw.";
											res.Code = 0;
											res.Amount = Amount;
										}
										else
										{
											res.PlayerID = PlayerID;
											res.Game = GameID;
											res.Message = result.Message;
											res.Code = 3;
											res.Amount = null;
										}
									}
								}
								else
								{
									res.PlayerID = PlayerID;
									res.Game = GameID;
									res.Message = "Game account not found.";
									res.Code = 2;
									res.Amount = null;
								}
							}
							//bt28win | able to return after balance but not return from own 28win api | currency define by provider
							else if (GameID == "BT28WIN")
							{
								var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format("http://gameapi-28win.azurewebsites.net/api/BT28Win/Withdraw?loginID={0}&amount={1}", record.GamePlayerID, string.Format("{0:0.##}", Amount));
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
											res.PlayerID = PlayerID;
											res.Game = GameID;
											res.Message = "Successfully withdraw.";
											res.Code = 0;
											res.Amount = Amount;
										}
										else
										{
											res.PlayerID = PlayerID;
											res.Game = GameID;
											res.Message = result.ErrorDesc;
											res.Code = 3;
											res.Amount = null;
										}
									}
								}
								else
								{
									res.PlayerID = PlayerID;
									res.Game = GameID;
									res.Message = "Game account not found.";
									res.Code = 2;
									res.Amount = null;
								}
							}
							//tpg | not able to return after balance | currency based on what we pass in
							else if (GameID == "TPG")
							{
								var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format("http://game-tiger.azurewebsites.net/api/TPG/Withdraw?username={0}&displayName={1}&currency={2}&amount={3}",
																					record.GamePlayerID, PlayerID, Currency, string.Format("{0:0.##}", Amount));
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
											res.PlayerID = PlayerID;
											res.Game = GameID;
											res.Message = "Successfully withdraw.";
											res.Code = 0;
											res.Amount = Amount;
										}
										else
										{
											res.PlayerID = PlayerID;
											res.Game = GameID;
											res.Message = result.message;
											res.Code = 3;
											res.Amount = null;
										}
									}
								}
								else
								{
									res.PlayerID = PlayerID;
									res.Game = GameID;
									res.Message = "Game account not found.";
									res.Code = 2;
									res.Amount = null;
								}
							}
							//tpgfish | not able to return after balance | currency based on what we pass in
							else if (GameID == "TPGFISH")
							{
								var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format("http://game-tigerfish.azurewebsites.net/api/TPGFish/Withdraw?username={0}&displayName={1}&currency={2}&amount={3}",
																					record.GamePlayerID, PlayerID, Currency, string.Format("{0:0.##}", Amount));
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
											res.PlayerID = PlayerID;
											res.Game = GameID;
											res.Message = "Successfully withdraw.";
											res.Code = 0;
											res.Amount = Amount;
										}
										else
										{
											res.PlayerID = PlayerID;
											res.Game = GameID;
											res.Message = result.message;
											res.Code = 3;
											res.Amount = null;
										}
									}
								}
								else
								{
									res.PlayerID = PlayerID;
									res.Game = GameID;
									res.Message = "Game account not found.";
									res.Code = 2;
									res.Amount = null;
								}
							}
							//allcity | not able to return after balance | currency fix in provider side
							else if (GameID == "ALLCITY")
							{
								var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format("http://gameapi-allcity.azurewebsites.net/api/AC/Withdraw?account={0}&amount={1}", record.GamePlayerID, string.Format("{0:0.##}", Amount));
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
											res.PlayerID = PlayerID;
											res.Game = GameID;
											res.Message = "Successfully withdraw.";
											res.Code = 0;
											res.Amount = Amount;
										}
										else
										{
											res.PlayerID = PlayerID;
											res.Game = GameID;
											res.Message = result.msg;
											res.Code = 3;
											res.Amount = null;
										}
									}
								}
								else
								{
									res.PlayerID = PlayerID;
									res.Game = GameID;
									res.Message = "Game account not found.";
									res.Code = 2;
									res.Amount = null;
								}
							}
							//ibc | able to return after balance | result.Data.after_amount | currency hardcode in our own api
							else if (GameID == "IBC")
							{
								var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format("http://ibc.bingotown88.com/api/IBC/Withdraw?username={0}&amount={1}", record.GamePlayerID, string.Format("{0:0.##}", Amount));
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
											res.PlayerID = PlayerID;
											res.Game = GameID;
											res.Code = result.Data.status == 0 ? 0 : 1;
											res.Message = result.Data.status == 0 ? "Successfully withdraw." : "System error.";
											res.Amount = result.Data.status == 0 ? Amount : null;
										}
										else
										{
											res.PlayerID = PlayerID;
											res.Game = GameID;
											res.Code = 3;
											res.Message = result.message;
											res.Amount = null;
										}
									}
								}
								else
								{
									res.PlayerID = PlayerID;
									res.Game = GameID;
									res.Code = 2;
									res.Message = "Game account not found.";
									res.Amount = null;
								}
							}
							//evo888 | not able to return after balance | able to set country in our own api
							else if (GameID == "EVO888")
							{
								var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format("http://gameapi-evo888.azurewebsites.net/api/Evo888/Withdraw?username={0}&amount={1}", record.GamePlayerID, string.Format("{0:0.##}", Amount));
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
											res.PlayerID = PlayerID;
											res.Game = GameID;
											res.Code = 0;
											res.Message = "Successfully withdraw.";
											res.Amount = Amount;
										}
										else
										{
											res.PlayerID = PlayerID;
											res.Game = GameID;
											res.Message = result.msg;
											res.Code = 3;
											res.Amount = null;
										}
									}
								}
								else
								{
									res.PlayerID = PlayerID;
									res.Game = GameID;
									res.Message = "Game account not found.";
									res.Code = 2;
									res.Amount = null;
								}
							}
							//vpower | able to return balance | result.Credit | currency fix in provider side
							else if (GameID == "VPOWER")
							{
								var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format("http://gameapi-vpower.azurewebsites.net/api/VPower/Withdraw?username={0}&amount={1}", record.GamePlayerID, string.Format("{0:0.##}", Amount));
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
										{
											res.PlayerID = PlayerID;
											res.Game = GameID;
											res.Code = 0;
											res.Message = "Successfully withdraw.";
											res.Amount = Amount;
										}
										else
										{
											res.PlayerID = PlayerID;
											res.Game = GameID;
											res.Message = GetVPowerErrorMsg(result.ErrorCode);
											res.Code = 3;
											res.Amount = null;
										}
									}
								}
								else
								{
									res.PlayerID = PlayerID;
									res.Game = GameID;
									res.Message = "Game account not found.";
									res.Code = 2;
									res.Amount = null;
								}
							}
							//sexygaming | able to return balance | result.data.Balance | currency fix in provider side
							else if (GameID == "SEXYGAMING")
							{
								var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format("http://game-sexy.azurewebsites.net/api/Sexy/Withdraw?username={0}&amount={1}",
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
										{
											res.PlayerID = PlayerID;
											res.Game = GameID;
											res.Message = "Successfully withdraw.";
											res.Code = 0;
											res.Amount = Amount;
										}
										else
										{
											res.PlayerID = PlayerID;
											res.Game = GameID;
											res.Message = result.message;
											res.Code = 3;
											res.Amount = null;
										}
									}
								}
								else
								{
									res.PlayerID = PlayerID;
									res.Game = GameID;
									res.Message = "Game account not found.";
									res.Code = 2;
									res.Amount = null;
								}
							}
							//spadegaming | able to return balance | result.afterBalance | currency hardcode in our own api
							else if (GameID == "SPADEGAMING")
							{
								var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format("http://game-spade.azurewebsites.net/api/Spade/Withdraw?username={0}&amount={1}",
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
										{
											res.PlayerID = PlayerID;
											res.Game = GameID;
											res.Message = "Successfully withdraw.";
											res.Code = 0;
											res.Amount = Amount;
										}
										else
										{
											res.PlayerID = PlayerID;
											res.Game = GameID;
											res.Message = result.msg;
											res.Code = 3;
											res.Amount = null;
										}
									}
								}
								else
								{
									res.PlayerID = PlayerID;
									res.Game = GameID;
									res.Message = "Game account not found.";
									res.Code = 2;
									res.Amount = null;
								}
							}
						}
						else
						{
							res.PlayerID = PlayerID;
							res.Game = GameID;
							res.Message = "Get game balance failed.";
							res.Code = 1;
							res.Amount = null;
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
					_common.LogError("Withdraw " + DateTime.Now.ToString() + " " + string.Format("{0}|{1}|{2}", Game, PlayerID, Currency) + " " + ex.Message.ToString(), "Player");
					dynamic result = new ExpandoObject();
					result.Code = -9999;
					result.Message = ex.Message.ToString();
					return new HttpResponseMessage()
					{
						StatusCode = HttpStatusCode.InternalServerError,
						Content = new StringContent(JsonConvert.SerializeObject(result), System.Text.Encoding.UTF8, "application/json")
					};
				}
			}
		}

		public bool GameDeposit(string Game, string PlayerID, string Currency, decimal Amount) //Currency need to get from bingotown Game table and pass in here (by myself)
		{
			lock (MyLock)
			{
				string apiurl = "";
				Common _common = new Common();
				int TimeoutSec = 180000; //3 mins

				try
				{
					using (BingoConn db = new BingoConn())
					{
						var res = new DepositVM();
						string GameID = Game.ToUpper().Trim();

						//918kiss | able to return after balance | result.ScoreNum | no currency for 918 
						if (GameID == "918KISS")
						{
							var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == "918" + PlayerID.ToUpper()).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format("http://gameapi-918.azurewebsites.net/api/BT918/Transfer?UID={0}&Num={1}&IP={2}", record.GamePlayerID, string.Format("{0:0.##}", Amount), GetIPAddress());
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

									if (Convert.ToString(result.Message).ToUpper().Trim() == "SUCCESS")
										return true;
									else
										return false;
								}
							}
							else
								return false;
						}
						//bg | able to return after balance | result.result | currency based on agent
						else if (GameID == "BG")
						{
							var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format("http://gameapi-bg.azurewebsites.net/api/BG/Transfer?UID={0}&IP={1}&Amount={2}", record.GamePlayerID, GetIPAddress(), string.Format("{0:0.##}", Amount));
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
										return false;
								}
							}
							else
								return false;
						}
						//ag | not able to return after balance | currency based on what we pass in
						else if (GameID == "AG")
						{
							var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format("http://gameapi-ag.azurewebsites.net/api/AG/Deposit?loginname={0}&credit={1}&currency={2}", record.GamePlayerID, string.Format("{0:0.##}", Amount), Currency);
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
										return false;
									else
										return true;
								}
							}
							else
								return false;
						}
						//playtech | able to return balance | result.result.currentplayerbalance | currency define by provider
						else if (GameID == "PT")
						{
							var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format("http://gameapi-pt.azurewebsites.net/api/PT/Deposit?playerName={0}&amount={1}", record.GamePlayerID, string.Format("{0:0.##}", Amount));
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
										return false;
								}
							}
							else
								return false;
						}
						//ig | not able to return after balance | currency define by provider
						else if (GameID == "IG")
						{
							var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format("http://gameapi-play8.azurewebsites.net/api/Play8/Transfer?UID={0}&Action=0&Amount={1}&IP={1}", record.GamePlayerID, string.Format("{0:0.##}", Amount), GetIPAddress());
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
										return false;
								}
							}
							else
								return false;
						}
						//bt28win | able to return after balance but not return from own 28win api | currency define by provider
						else if (GameID == "BT28WIN")
						{
							var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format("http://gameapi-28win.azurewebsites.net/api/BT28Win/Deposit?loginID={0}&amount={1}", record.GamePlayerID, string.Format("{0:0.##}", Amount));
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
										return false;
								}
							}
							else
								return false;
						}
						//tpg | not able to return after balance | currency based on what we pass in
						else if (GameID == "TPG")
						{
							var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format("http://game-tiger.azurewebsites.net/api/TPG/Deposit?username={0}&displayName={1}&currency={2}&amount={3}",
																				record.GamePlayerID, PlayerID, Currency, string.Format("{0:0.##}", Amount));
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
						//tpg | not able to return after balance | currency based on what we pass in
						else if (GameID == "TPGFISH")
						{
							var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format("http://game-tigerfish.azurewebsites.net/api/TPGFish/Deposit?username={0}&displayName={1}&currency={2}&amount={3}",
																				record.GamePlayerID, PlayerID, Currency, string.Format("{0:0.##}", Amount));
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
						//allcity | not able to return after balance | currency fix in provider side
						else if (GameID == "ALLCITY")
						{
							var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format("http://gameapi-allcity.azurewebsites.net/api/AC/Deposit?account={0}&amount={1}", record.GamePlayerID, string.Format("{0:0.##}", Amount));
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
										return false;
								}
							}
							else
								return false;
						}
						//ibc | able to return after balance | result.Data.after_amount | currency hardcode in our own api
						else if (GameID == "IBC")
						{
							var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format("http://ibc.bingotown88.com/api/IBC/Deposit?username={0}&amount={1}", record.GamePlayerID, string.Format("{0:0.##}", Amount));
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
										if (result.Data.status == 0)
											return true;
										else
											return false;
									}
									else
										return false;
								}
							}
							else
								return false;
						}
						//evo888 | not able to return after balance | able to set country in our own api
						else if (GameID == "EVO888")
						{
							var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format("http://gameapi-evo888.azurewebsites.net/api/Evo888/Deposit?username={0}&amount={1}", record.GamePlayerID, string.Format("{0:0.##}", Amount));
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
										return false;
								}
							}
							else
								return false;
						}
						//vpower | able to return balance | result.Credit | currency fix in provider side
						else if (GameID == "VPOWER")
						{
							var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format("http://gameapi-vpower.azurewebsites.net/api/VPower/Deposit?username={0}&amount={1}", record.GamePlayerID, string.Format("{0:0.##}", Amount));
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
										return false;
								}
							}
							else
								return false;
						}
						//sexygaming | able to return balance | result.data.Balance | currency fix in provider side
						else if (GameID == "SEXYGAMING")
						{
							var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format("http://game-sexy.azurewebsites.net/api/Sexy/Deposit?username={0}&amount={1}",
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
										return false;
								}
							}
							else
								return false;
						}
						//spadegaming | able to return balance | result.afterBalance | currency hardcode in our own api
						else if (GameID == "SPADEGAMING")
						{
							var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

							if (record != null)
							{
								apiurl = string.Format("http://game-spade.azurewebsites.net/api/Spade/Deposit?username={0}&amount={1}",
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
										return false;
								}
							}
							else
								return false;
						}

						return false;
					}
				}
				catch (Exception ex)
				{
					_common.LogError("Deposit " + DateTime.Now.ToString() + " " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Amount.ToString(), Currency) + " " + ex.Message.ToString(), "Player");
					return false;
				}
			}
		}

		[HttpGet]
		public HttpResponseMessage GetGameUrl(string Game, string PlayerID, string Currency, decimal Amount, string Language = "en")
		{
			lock (MyLock)
			{
				string apiurl = "";
				Common _common = new Common();
				bool canGetUrl = true;
				int TimeoutSec = 180000; //3 mins

				try
				{
					using (BingoConn db = new BingoConn())
					{
						var res = new GetGameUrlVM();
						string GameID = Game.ToUpper().Trim();

						if (Amount < 0)
						{
							res.PlayerID = PlayerID;
							res.Url = null;
							res.Game = GameID;
							res.Message = "Negative amount not accepted.";
							res.Code = 1;
							return new HttpResponseMessage()
							{
								StatusCode = HttpStatusCode.BadRequest,
								Content = new StringContent(JsonConvert.SerializeObject(res), System.Text.Encoding.UTF8, "application/json")
							};
						}

						if (Amount == 0)
							canGetUrl = true;
						else
							canGetUrl = GameDeposit(Game, PlayerID, Currency, Amount);

						if (canGetUrl)
						{
							//918kiss
							if (GameID == "918KISS")
							{
								var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == "918" + PlayerID.ToUpper()).FirstOrDefault();

								if (record != null)
								{
									res.PlayerID = PlayerID;
									res.Url = "";
									res.Game = GameID;
									res.Message = "Successfully get url.";
									res.Code = 0;
								}
								else
								{
									res.PlayerID = PlayerID;
									res.Url = null;
									res.Game = GameID;
									res.Message = "Game account not found.";
									res.Code = 3;
								}
							}
							//bg
							else if (GameID == "BG")
							{
								var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

								if (record != null)
								{
									string lang = Language.ToUpper() == "ZH" ? "zh_CN" : "en_US";
									apiurl = string.Format("http://gameapi-bg.azurewebsites.net/api/BG/Login?UID={0}&IP={1}&Language={2}", record.GamePlayerID, GetIPAddress(), lang);
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
											res.PlayerID = PlayerID;
											res.Url = Convert.ToString(result.result);
											res.Game = GameID;
											res.Message = "Successfully get url.";
											res.Code = 0;
										}
										else
										{
											res.PlayerID = PlayerID;
											res.Url = null;
											res.Game = GameID;
											res.Message = result.error.message;
											res.Code = 4;
										}
									}
								}
								else
								{
									res.PlayerID = PlayerID;
									res.Url = null;
									res.Game = GameID;
									res.Message = "Game account not found.";
									res.Code = 3;
								}
							}
							//ag
							else if (GameID == "AG")
							{
								var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format("http://gameapi-ag.azurewebsites.net/api/AG/ForwardGame?loginname={0}&currency={1}&lang={2}", record.GamePlayerID, Currency, Language);
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
											res.PlayerID = PlayerID;
											res.Url = Convert.ToString(result.ErrorDesc);
											res.Game = GameID;
											res.Message = "Successfully get url.";
											res.Code = 0;
										}
										else
										{
											res.PlayerID = PlayerID;
											res.Url = null;
											res.Game = GameID;
											res.Message = result.ErrorDesc;
											res.Code = 4;
										}
									}
								}
								else
								{
									res.PlayerID = PlayerID;
									res.Url = null;
									res.Game = GameID;
									res.Message = "Game account not found.";
									res.Code = 3;
								}
							}
							//playtech
							else if (GameID == "PT")
							{
								var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

								if (record != null)
								{
									string lang = Language.ToUpper() == "ZH" ? "ZH-CN" : "EN";
									apiurl = string.Format("http://gameapi-pt.azurewebsites.net/api/PT/LaunchGame?playerName={0}&lang={1}", record.GamePlayerID, lang);
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
											res.PlayerID = PlayerID;
											res.Url = Convert.ToString(result.error);
											res.Game = GameID;
											res.Message = "Successfully get url.";
											res.Code = 0;
										}
										else
										{
											res.PlayerID = PlayerID;
											res.Url = null;
											res.Game = GameID;
											res.Message = result.error;
											res.Code = 4;
										}
									}
								}
								else
								{
									res.PlayerID = PlayerID;
									res.Url = null;
									res.Game = GameID;
									res.Message = "Game account not found.";
									res.Code = 3;
								}
							}
							//ig
							else if (GameID == "IG")
							{
								var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

								if (record != null)
								{
									res.PlayerID = PlayerID;
									res.Url = "http://www.igamingdemo.com/app/";
									res.Game = GameID;
									res.Message = "Successfully get url.";
									res.Code = 0;
								}
								else
								{
									res.PlayerID = PlayerID;
									res.Url = null;
									res.Game = GameID;
									res.Message = "Game account not found.";
									res.Code = 3;
								}
							}
							//bt28win
							else if (GameID == "BT28WIN")
							{
								var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format("http://gameapi-28win.azurewebsites.net/api/BT28Win/Bet?user={0}&pass={1}", record.GamePlayerID, "123456789");
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
											res.PlayerID = PlayerID;
											res.Url = Convert.ToString(result.Data);
											res.Game = GameID;
											res.Message = "Successfully get url.";
											res.Code = 0;
										}
										else
										{
											res.PlayerID = PlayerID;
											res.Url = null;
											res.Game = GameID;
											res.Message = result.ErrorDesc;
											res.Code = 4;
										}
									}
								}
								else
								{
									res.PlayerID = PlayerID;
									res.Url = null;
									res.Game = GameID;
									res.Message = "Game account not found.";
									res.Code = 3;
								}
							}
							//tpg
							else if (GameID == "TPG")
							{
								var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

								if (record != null)
								{
									string lang = Language.ToUpper() == "ZH" ? "zh-ch" : "en";
									apiurl = string.Format("http://game-tiger.azurewebsites.net/api/TPG/LaunchLobby?playerName={0}&displayName={1}&currency={2}&lang={3}",
																					record.GamePlayerID, PlayerID, Currency, lang);
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
											res.PlayerID = PlayerID;
											res.Url = Convert.ToString(result.message);
											res.Game = GameID;
											res.Message = "Successfully get url.";
											res.Code = 0;
										}
										else
										{
											res.PlayerID = PlayerID;
											res.Url = null;
											res.Game = GameID;
											res.Message = result.message;
											res.Code = 4;
										}
									}
								}
								else
								{
									res.PlayerID = PlayerID;
									res.Url = null;
									res.Game = GameID;
									res.Message = "Game account not found.";
									res.Code = 3;
								}
							}
							//allcity
							else if (GameID == "ALLCITY")
							{
								var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format("http://gameapi-allcity.azurewebsites.net/api/AC/GetGameApk");
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
											res.PlayerID = PlayerID;
											res.Url = Convert.ToString(result.apk);
											res.Game = GameID;
											res.Message = "Successfully get url.";
											res.Code = 0;
										}
										else
										{
											res.PlayerID = PlayerID;
											res.Url = null;
											res.Game = GameID;
											res.Message = result.msg;
											res.Code = 4;
										}
									}
								}
								else
								{
									res.PlayerID = PlayerID;
									res.Url = null;
									res.Game = GameID;
									res.Message = "Game account not found.";
									res.Code = 3;
								}
							}
							//ibc
							else if (GameID == "IBC")
							{
								var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

								if (record != null)
								{
									string lang = Language.ToUpper() == "ZH" ? "cs" : "en";

									apiurl = string.Format("http://ibc.bingotown88.com/api/IBC/LaunchGame?username={0}&lang={1}", record.GamePlayerID, lang);
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
											res.PlayerID = PlayerID;
											res.Url = Convert.ToString(result.Data);
											res.Game = GameID;
											res.Code = 0;
											res.Message = "Successfully get url.";
										}
										else
										{
											res.PlayerID = PlayerID;
											res.Url = null;
											res.Game = GameID;
											res.Code = 4;
											res.Message = result.message;
										}
									}
								}
								else
								{
									res.PlayerID = PlayerID;
									res.Url = null;
									res.Game = GameID;
									res.Code = 3;
									res.Message = "Game account not found.";
								}
							}
							//evo888
							else if (GameID == "EVO888")
							{
								var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format("http://gameapi-evo888.azurewebsites.net/api/Evo888/GetGameApk");
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
											res.PlayerID = PlayerID;
											res.Url = Convert.ToString(result.apk);
											res.Game = GameID;
											res.Code = 0;
											res.Message = "Successfully get url.";
										}
										else
										{
											res.PlayerID = PlayerID;
											res.Url = null;
											res.Game = GameID;
											res.Message = result.msg;
											res.Code = 4;
										}
									}
								}
								else
								{
									res.PlayerID = PlayerID;
									res.Url = null;
									res.Game = GameID;
									res.Message = "Game account not found.";
									res.Code = 3;
								}
							}
							//vpower
							else if (GameID == "VPOWER")
							{
								var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

								if (record != null)
								{
									apiurl = string.Format("http://gameapi-vpower.azurewebsites.net/api/VPower/LaunchLobby?username={0}&lang={1}",
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
											res.PlayerID = PlayerID;
											res.Url = Convert.ToString(result.Url);
											res.Game = GameID;
											res.Message = "Successfully get url.";
											res.Code = 0;
										}
										else
										{
											res.PlayerID = PlayerID;
											res.Url = null;
											res.Game = GameID;
											res.Message = result.message;
											res.Code = 4;
										}
									}
								}
								else
								{
									res.PlayerID = PlayerID;
									res.Url = null;
									res.Game = GameID;
									res.Message = "Game account not found.";
									res.Code = 3;
								}
							}
							//tpgfish
							else if (GameID == "TPGFISH")
							{
								var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

								if (record != null)
								{
									string lang = Language.ToUpper() == "ZH" ? "zh-ch" : "en";
									apiurl = string.Format("http://game-tigerfish.azurewebsites.net/api/TPGFish/LaunchLobby?playerName={0}&displayName={1}&currency={2}&lang={3}",
																					record.GamePlayerID, PlayerID, Currency, lang);
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
											res.PlayerID = PlayerID;
											res.Url = Convert.ToString(result.message);
											res.Game = GameID;
											res.Message = "Successfully get url.";
											res.Code = 0;
										}
										else
										{
											res.PlayerID = PlayerID;
											res.Url = null;
											res.Game = GameID;
											res.Message = result.message;
											res.Code = 4;
										}
									}
								}
								else
								{
									res.PlayerID = PlayerID;
									res.Url = null;
									res.Game = GameID;
									res.Message = "Game account not found.";
									res.Code = 3;
								}
							}
							//sexygaming
							else if (GameID == "SEXYGAMING")
							{
								var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

								if (record != null)
								{
									string lang = Language.ToUpper() == "ZH" ? "zh-cn" : "en-us";
									apiurl = string.Format("http://gameapi-sexy.azurewebsites.net/api/Sexy/LaunchGame?username={0}&lang={1}", record.GamePlayerID, lang);
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
											res.PlayerID = PlayerID;
											res.Url = Convert.ToString(result.data);
											res.Game = GameID;
											res.Message = "Successfully get url.";
											res.Code = 0;
										}
										else
										{
											res.PlayerID = PlayerID;
											res.Url = null;
											res.Game = GameID;
											res.Message = result.ErrorDesc;
											res.Code = 4;
										}
									}
								}
								else
								{
									res.PlayerID = PlayerID;
									res.Url = null;
									res.Game = GameID;
									res.Message = "Game account not found.";
									res.Code = 3;
								}
							}
							//sexygaming
							else if (GameID == "SPADEGAMING")
							{
								var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == PlayerID.ToUpper()).FirstOrDefault();

								if (record != null)
								{
									string lang = Language.ToUpper() == "ZH" ? "zh_CN" : "en_US";
									apiurl = string.Format("http://gameapi-spade.azurewebsites.net/api/Spade/LaunchGame?username={0}&mcUID={1}&lang={2}", record.GamePlayerID, record.PlayerID, lang);
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
											res.PlayerID = PlayerID;
											res.Url = Convert.ToString(result.url);
											res.Game = GameID;
											res.Message = "Successfully get url.";
											res.Code = 0;
										}
										else
										{
											res.PlayerID = PlayerID;
											res.Url = null;
											res.Game = GameID;
											res.Message = result.ErrorDesc;
											res.Code = 4;
										}
									}
								}
								else
								{
									res.PlayerID = PlayerID;
									res.Url = null;
									res.Game = GameID;
									res.Message = "Game account not found.";
									res.Code = 3;
								}
							}
						}
						else
						{
							res.PlayerID = PlayerID;
							res.Url = null;
							res.Game = GameID;
							res.Message = "Game deposit failed.";
							res.Code = 2;
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
					_common.LogError("GetGameUrl " + DateTime.Now.ToString() + " " + ex.Message.ToString(), "Player");
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
		public HttpResponseMessage GetGameRecord(string Game, string PlayerID, string Currency, DateTime StartDt, DateTime EndDt)
		{
			lock (MyLock)
			{
				Common _common = new Common();
				try
				{
					using (BingoConn db = new BingoConn())
					{
						GetGameRecordVM res = new GetGameRecordVM();
						string GameID = Game.ToUpper().Trim();
						string UPlayerID = PlayerID.ToUpper().Trim();
						string UCurrency = Currency.ToUpper().Trim();

						using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["BingoConn1"].ConnectionString))
						{
							SqlCommand cmd = new SqlCommand("sp_PlayerGameRecordReport", con);
							cmd.CommandType = CommandType.StoredProcedure;
							cmd.Parameters.Add(new SqlParameter("@StartDt", StartDt));
							cmd.Parameters.Add(new SqlParameter("@EndDt", EndDt));
							cmd.Parameters.Add(new SqlParameter("@PlayerID", UPlayerID));
							cmd.Parameters.Add(new SqlParameter("@GameProvider", GameID));

							DataTable table = new DataTable();
							SqlDataAdapter da = new SqlDataAdapter(cmd);
							da.Fill(table);
							if (table.Rows.Count > 0)
							{
								List<GameRecordData> data = table.AsEnumerable().Select(row =>
																								new GameRecordData
																								{
																									BetTime = row.Field<DateTime>("BetTime"),
																									GameName = row.Field<string>("GameName"),
																									Turnover = row.Field<decimal>("Turnover"),
																									WinLoss = row.Field<decimal>("WinLoss")
																								}).ToList();

								res.Records = data;
								res.TotalRows = data != null ? data.Count : 0;
								res.PlayerID = UPlayerID;
								res.Game = GameID;
								res.Message = "Successfully get record.";
								res.Code = 0;
							}
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

			try
			{
				if (GameID == "BG")
				{
					string prefix = "BG";

					if ((prefix + UPlayerID).Length <= 20)
						return prefix + UPlayerID;
					else if ((prefix + DateTime.Now.ToString("yyyyMMdd") + UPlayerID).Length <= 20)
						return prefix + DateTime.Now.ToString("yyyyMMdd") + UPlayerID;
					else
						return prefix + RandomString(10);
				}
				else if (GameID == "AG")
				{
					string prefix = "AG";

					if ((prefix + UPlayerID).Length <= 20)
						return prefix + UPlayerID;
					else if ((prefix + DateTime.Now.ToString("yyyyMMdd") + UPlayerID).Length <= 20)
						return prefix + DateTime.Now.ToString("yyyyMMdd") + UPlayerID;
					else
						return prefix + RandomString(10);
				}
				else if (GameID == "PLAYTECH")
				{
					string prefix = "PT";

					if ((prefix + UPlayerID).Length <= 12)
						return prefix + UPlayerID;
					else if ((prefix + DateTime.Now.ToString("yyyyMMdd") + UPlayerID).Length <= 12)
						return prefix + DateTime.Now.ToString("yyyyMMdd") + UPlayerID;
					else
						return prefix + RandomString(10);
				}
				else if (GameID == "PLAY8OY")
				{
					string prefix = "P8";

					if ((prefix + UPlayerID).Length <= 17)
						return prefix + UPlayerID;
					else if ((prefix + DateTime.Now.ToString("yyyyMMdd") + UPlayerID).Length <= 17)
						return prefix + DateTime.Now.ToString("yyyyMMdd") + UPlayerID;
					else
						return prefix + RandomString(10);
				}
				else if (GameID == "28WIN")
				{
					string prefix = "4D";

					if ((prefix + UPlayerID).Length <= 20)
						return prefix + UPlayerID;
					else if ((prefix + DateTime.Now.ToString("yyyyMMdd") + UPlayerID).Length <= 20)
						return prefix + DateTime.Now.ToString("yyyyMMdd") + UPlayerID;
					else
						return prefix + RandomString(10);
				}
				else if (GameID == "ALLCITY")
				{
					string prefix = "AC";

					if ((prefix + UPlayerID).Length <= 10)
						return prefix + UPlayerID;
					else if ((prefix + DateTime.Now.ToString("yyyyMMdd") + UPlayerID).Length <= 10)
						return prefix + DateTime.Now.ToString("yyyyMMdd") + UPlayerID;
					else
						return prefix + RandomString(8);
				}
				else if (GameID == "IBC")
				{
					string prefix = "IBC";

					if ((prefix + UPlayerID).Length <= 12)
						return prefix + UPlayerID;
					else if ((prefix + DateTime.Now.ToString("yyyyMMdd") + UPlayerID).Length <= 12)
						return prefix + DateTime.Now.ToString("yyyyMMdd") + UPlayerID;
					else
						return prefix + RandomString(9);
				}
				else if (GameID == "VPOWER")
				{
					string prefix = "VP";

					if ((prefix + UPlayerID).Length <= 17)
						return prefix + UPlayerID;
					else if ((prefix + DateTime.Now.ToString("yyyyMMdd") + UPlayerID).Length <= 17)
						return prefix + DateTime.Now.ToString("yyyyMMdd") + UPlayerID;
					else
						return prefix + RandomString(10);
				}
				else if (GameID == "SEXYGAMING")
				{
					string prefix = "SEX";

					if ((prefix + UPlayerID).Length <= 15)
						return prefix + UPlayerID;
					else if ((prefix + DateTime.Now.ToString("yyyyMMdd") + UPlayerID).Length <= 15)
						return prefix + DateTime.Now.ToString("yyyyMMdd") + UPlayerID;
					else
						return prefix + RandomString(10);
				}
				else if (GameID == "SPADEGAMING")
				{
					string prefix = "SPA";

					if ((prefix + UPlayerID).Length <= 50)
						return prefix + UPlayerID;
					else if ((prefix + DateTime.Now.ToString("yyyyMMdd") + UPlayerID).Length <= 50)
						return prefix + DateTime.Now.ToString("yyyyMMdd") + UPlayerID;
					else
						return prefix + RandomString(10);
				}

				return UPlayerID;
			}
			catch (Exception ex)
			{
				return UPlayerID;
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
				case "AG":
				case "BG":
				case "SPADEGAMING":
				case "SEXYGAMING":
				case "VPOWER":
				case "28WIN":
				case "PLAYTECH":
				case "IBC":
					return "WebView";
				case "EVO888":
				case "ALLCITY":
				case "PLAY8OY":
					return "App";
				default:
					return "";
			}
		}
	}
}