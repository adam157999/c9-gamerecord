using GameRecord.Gamess.EvoGaming.CasinoGamesDetail;
using GameRecord.Models;
using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pragmatic.Model;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;
using System.Xml.Serialization;
using vm = GameRecord.Controllers.GameAPIController;

namespace GameRecord.Controllers.V2
{
	public class ArrowEdgeConnector : BaseConnector
	{
		// GET: JiLiConnector
		private string gameID = "ARROWEDGE";
		private string gameUrl = "";
        private string gameListUrl = "";
		private string serverId = "";
        private string imgUrl = "";
		private string operatorId = "";
		private string tableGuid = "";
		private int timeOutSec = 30;
		private GameMapping gameMap;
		private string lobbyUrl = HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.PathAndQuery, "/") + "ARROWEDGE/Lobby";

        private readonly Encoding Charset = Encoding.UTF8;
        public ArrowEdgeConnector(string PlayerID = "", string CurrencyID = "MYR", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD) : base(PlayerID, CurrencyID, IPAddr, Environment)
		{
			using (BingoConn db = new BingoConn())
			{
				gameMap = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == gameID.ToUpper().Trim() &&
																								x.PlayerID == playerID &&
																								x.Currency.ToUpper() == currencyID &&
																								x.IsActive == true).FirstOrDefault();

				var apiConfig = CatchData.GetApiKeys(gameID.ToUpper().Trim(), Environment.ToString(), currencyID.ToUpper().Trim());
				if (string.IsNullOrEmpty(CurrencyID)) apiConfig = CatchData.GetApiKeys(gameID.ToUpper().Trim(), Environment.ToString());

				if (apiConfig != null)
				{
					JObject jObj = JObject.Parse(apiConfig.ApiConfig);
                    gameUrl = jObj["ArrowEdgeGameUrl"].ToString();
                    gameListUrl = jObj["ArrowEdgeGameListUrl"].ToString();
                    tableGuid = jObj["ArrowEdgeTableGuid"].ToString();
					operatorId = jObj["ArrowEdgeOperatorId"].ToString();
					serverId = jObj["ArrowEdgeServerId"].ToString();
					timeOutSec = Convert.ToInt32(jObj["ArrowEdgeTimeOutSec"].ToString());
				}
			}
		}

		public override vm.CheckAndCreateAccountVM CreatePlayer(string loginID, string loginPass, string fullName, string mcUID)
		{
			throw new NotImplementedException();
		}

		public override bool GameDeposit(decimal Amount, string TransactionID, ref Error Error)
		{
			throw new NotImplementedException();
		}

		public override bool GameWithdraw(decimal Amount, string TransactionID, ref Error Error)
		{
			throw new NotImplementedException();
		}

		public override vm.GetBalanceVM GetBalance()
		{
			throw new NotImplementedException();
		}

		public override decimal? GetGameBalance()
		{
			throw new NotImplementedException();
		}

		public override vm.GetGameUrlVM GetGameUrl(string langId, bool IsMobile = false)
		{
			throw new NotImplementedException();
		}
		public override bool KickPlayer(string GamePlayerId, ref Error Error)
		{
			throw new NotImplementedException();
		}

		public override string RetryDeposit(decimal Amount, string TrackerId, ref Error Error)
		{
			throw new NotImplementedException();
		}

		public override string RetryWithdraw(decimal Amount, string TrackerId, ref Error Error)
		{
			throw new NotImplementedException();
		}

		public vm.GetGameUrlVM GetGameSeamlessUrl(string langId, string token, bool IsMobile = false)
		{
			var res = new vm.GetGameUrlVM();

			try
			{
				string platform = "Desktop";
				if(IsMobile == true) { platform = "Mobile"; }
				InsertLog(gameID, "GetGameSeamlessUrlARROWEDGE", "langId", langId, "IsMobile", IsMobile, "token", token, "gameID", gameID);

				langId = GetGameLanguage(gameID, langId);

				string url = string.Format("{0}?username={1}&langid={2}&token={3}&currency={4}&Mobile={5}", lobbyUrl, playerID, langId, token,currencyID,platform);

				InsertLog(gameID, "GetGameSeamlessUrlARROWEDGE", "langId", langId, "IsMobile", IsMobile, "token", token, "gameID", gameID, "url", url);

				res.PlayerID = playerID;
				res.Url = url;
				res.Game = gameID;
				res.Message = "Success";
				res.Code = 0;
			}
			catch (Exception ex)
			{
				_common.LogError("GetGameSeamlessUrlARROWEDGE" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

				var err = new Error
				{
					ErrorCode = "999",
					ErrorDesc = ex.Message.ToString()
				};

				InsertLog(gameID, "GetGameSeamlessUrlARROWEDGE", "Error", JsonConvert.SerializeObject(err));

				res.PlayerID = playerID;
				res.Url = null;
				res.Game = gameID;
				res.Message = ex.Message.ToString();
				res.Code = 99;
			}

			_common.InsertLog("GetGameSeamlessUrl", "langId", langId, "token", token, "IsMobile", IsMobile);

			return res;
		}

        //lobby
        public vm.GetBalanceVM GetBalanceLobby(string username)
		{
			var res = new vm.GetBalanceVM();

			try
			{
				if (!string.IsNullOrEmpty(username))
				{
					using (BingoConn db = new BingoConn())
					{
						var foundRecord = db.SeamlessWallets.SingleOrDefault(x => x.Game == gameID && x.Username == username);
						if (foundRecord != null)
						{
							res.PlayerID = playerID;
							res.Balance = foundRecord.Cash + foundRecord.Chip;
							res.Game = gameID;
							res.Message = "Successfully get balance.";
							res.Code = 0;
						}
						else
						{
							res.PlayerID = playerID;
							res.Balance = null;
							res.Game = gameID;
							res.Message = "Failed to get balance";
							res.Code = 2;
						}
					}
				}
				else
				{
					res.PlayerID = playerID;
					res.Balance = null;
					res.Game = gameID;
					res.Message = "Game account not found.";
					res.Code = 1;
				}
			}
			catch (Exception ex)
			{
				_common.LogError("Balance" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

				var err = new Error
				{
					ErrorCode = "999",
					ErrorDesc = ex.Message.ToString()
				};

				InsertLog(gameID, "GetCredit", "Error", JsonConvert.SerializeObject(err));

				res.PlayerID = playerID;
				res.Balance = null;
				res.Game = gameID;
				res.Message = ex.Message.ToString();
				res.Code = 99;
			}

			return res;
		}

		public vm.GetGameUrlVM GetSlotGameUrl(string username, string langId, string gameCode, string token, string currency)
		{
			var res = new vm.GetGameUrlVM();

			try
			{
				if (!string.IsNullOrEmpty(username))
				{
					//var token = new Guid().ToString();
					//langId = GetGameLanguage(gameID, langId);
					string url = $"{gameUrl}?tableguid={tableGuid}&token={token}&OperatorID={operatorId}&language={langId}&gameID={gameCode}&gameMode=real&currency={currency}&serverid={serverId}";
                    InsertLog(gameID, "GetSlotGameUrl", "Url", url);

                    res.PlayerID = playerID;
                    res.Url = url;
                    res.Game = gameID;
                    res.Message = "Success";
                    res.Code = 0;
                }
				else
				{
					res.PlayerID = playerID;
					res.Url = null;
					res.Game = gameID;
					res.Message = "Game account not found.";
					res.Code = 1;
				}
			}
			catch (Exception ex)
			{
				_common.LogError("UserLogin" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

				var err = new Error
				{
					ErrorCode = "999",
					ErrorDesc = ex.Message.ToString()
				};

				InsertLog(gameID, "Login", "Error", JsonConvert.SerializeObject(err));

				res.PlayerID = playerID;
				res.Url = null;
				res.Game = gameID;
				res.Message = ex.Message.ToString();
				res.Code = 99;
			}

			return res;
		}

		public GameList GetGameList()
		{
            //         var client = new RestClient(gameListUrl);
            //         client.Timeout = timeOutSec * 1000;
            //         var request = new RestRequest(Method.GET);
            //         request.AddHeader("Cookie", "BIGipServerviv_rgs_http=2389140746.20480.0000");
            //         IRestResponse response = client.Execute(request);
            //         Console.WriteLine(response.Content);
            //if (response.StatusCode == HttpStatusCode.OK)
            //{
            //             GameList gameList = new GameList();
            //             gameList.Games = ConvertXmlToClass(response.Content);
            //             return gameList;
            //         }
            //else
            //{
            //	return null;
            //}
            GameList gameList = new GameList();
			gameList.Games = new List<Games>();
            using (BingoConn db = new BingoConn())
			{

                string s = "SELECT Id,FriendlyName,Image, Platform FROM ArrowEdgeGameList WHERE Enabled = 1";
                var datatable = _common.Sql_Query(s);
				foreach(DataRow row in datatable.Rows)
				{
					Games game = new Games();
					game.friendlyName = row["FriendlyName"] != null ? row["FriendlyName"].ToString() : string.Empty;
                    game.id = row["Id"] != null ? row["Id"].ToString() : string.Empty;
					game.image = row["Image"] != null ? row["Image"].ToString() : string.Empty;
                    game.platform = row["Platform"] != null ? row["Platform"].ToString() : string.Empty;
                    gameList.Games.Add(game);
				}
			}

			return gameList;
        }

		private List<Games> ConvertXmlToClass(string xml)
		{
            var xmlOutput = XElement.Parse(xml);
            var xmlWithoutRoot = JsonConvert.SerializeXNode(xmlOutput, Newtonsoft.Json.Formatting.None, true);
            var newJson = Regex.Replace(xmlWithoutRoot, "(?<=\")(@)(?!.*\":\\s )", string.Empty, RegexOptions.IgnoreCase);
			JObject jObj = JObject.Parse(newJson);
			string json = jObj["Game"].ToString();
            List<Games> games = JsonConvert.DeserializeObject<List<Games>>(jObj["Game"].ToString());
            return games;
		}

		public class GameList
		{
			public List<Games> Games { get; set; }
		}

        public class Games
		{
			public string id { get; set; }
			public string friendlyName { get; set; }
			public string platform { get; set; }
			public string displayType { get; set; }
			public string enabled { get; set; }
			public string width { get; set; }
			public string height { get; set; }
			public string languages { get; set; }
			public string freeSpinAmounts { get; set; }
			public string defaultFreeSpinAmount { get; set; }
			public string freeSpinsCurrency { get; set; }
			public string categoryName { get; set; }
			public string image { get; set; }
			public string jackpots { get; set; }

        }

    }
}