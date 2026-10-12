using GameRecord.Gamess.BeSoft.Models;
using GameRecord.Gamess.OctoPlay;
using GameRecord.Gamess.OctoPlay.Models;
using GameRecord.Gamess.OnlyPlay;
using GameRecord.Gamess.OnlyPlay.Models;
using GameRecord.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using vm = GameRecord.Controllers.GameAPIController;

namespace GameRecord.Controllers.V2
{
    public class OctoPlayConnector : BaseConnector
    {
        private string gameID = "OCTOPLAY";
        private string apiUrl = "";
        private string privateKey = "";
        private string publicKey = "";
        private string brandId = "";
        private int timeout = 30;
        private GameMapping gameMap;
        private OctoPlay api = null;
        private string lobbyUrl = HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.PathAndQuery, "/") + "OctoPlay/Lobby";
        
        public OctoPlayConnector(string playerID = "", string currencyID = "", string ipAddr = "", GameEnvironment environment = GameEnvironment.PROD) : base(playerID, currencyID, ipAddr, environment)
        {
            using (BingoConn db = new BingoConn())
            {
                gameMap = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == gameID &&
                                                     x.PlayerID == playerID &&
                                                     x.Currency.ToUpper() == currencyID &&
                                                     x.IsActive == true).FirstOrDefault();

                var apiConfig = CatchData.GetApiKeys(gameID.ToUpper().Trim(), environment.ToString(), currencyID.ToUpper().Trim());
                if (string.IsNullOrEmpty(currencyID)) apiConfig = CatchData.GetApiKeys(gameID.ToUpper().Trim(), environment.ToString());

                if (apiConfig != null)
                {
                    JObject jObj = JObject.Parse(apiConfig.ApiConfig);
                    apiUrl = jObj["OctoPlayApiUrl"].ToString();
                    privateKey = jObj["OctoPlayPrivateKey"].ToString();
                    publicKey = jObj["OctoPlayPublicKey"].ToString();
                    brandId = jObj["OctoPlayBrandId"].ToString();
                    timeout = int.Parse(jObj["OctoPlayTimeOutSec"].ToString());

                    api = new OctoPlay(apiUrl, privateKey, publicKey, brandId, timeout);
                }
            }
        }

        public override vm.CheckAndCreateAccountVM CreatePlayer(string loginID, string loginPass, string fullName, string mcUID)
        {
            var res = new vm.CheckAndCreateAccountVM();

            try
            {
                var createWalletRequest = new CreateWalletRequest
                {
                    PlayerId = loginID,
                    BrandId = brandId,
                    Currency = gameMap == null ? "MYR" : gameMap.Currency
                };

                if (api.CreateWallet(createWalletRequest))
                {
                    InsertLog(gameID, "CreateUser", "request", JsonConvert.SerializeObject(createWalletRequest), "response", JsonConvert.SerializeObject(api.CreateWalletResponse));

                    InsertGameMapping(gameID, loginID, loginPass, mcUID);

                    res.PlayerID = playerID;
                    res.GamePlayerID = loginID;
                    res.Game = gameID;
                    res.Message = "Success";
                    res.Code = 0;
                }
                else
                {
                    InsertLog(gameID, "CreateUser", "request", JsonConvert.SerializeObject(createWalletRequest), "response", JsonConvert.SerializeObject(api.ErrorMessage));

                    res.PlayerID = playerID;
                    res.GamePlayerID = null;
                    res.Game = gameID;
                    res.Message = api.ErrorMessage;
                    res.Code = 2;
                }
            }
            catch (Exception ex)
            {
                _common.LogError("CreateUser" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

                var err = new
                {
                    ErrorCode = "999",
                    ErrorDesc = ex.Message.ToString()
                };

                InsertLog(gameID, "CreateUser", "Error", JsonConvert.SerializeObject(err));

                res.PlayerID = playerID;
                res.GamePlayerID = loginID;
                res.Game = gameID;
                res.Message = ex.Message.ToString();
                res.Code = 99;
            }

            return res;
        }

        public override bool GameDeposit(decimal amount, string transactionID, ref Error error)
        {
            var depositRequest = new TransferWalletRequest
            {
                Amount = amount,
                BrandId = brandId,
                Currency = currencyID,
                PlayerId = gameMap.GamePlayerID,
                TransactionId = transactionID,
                Type = "deposit"
            };
            if (api.TransferWallet(depositRequest))
            {
                InsertLog(gameID, "Deposit", "request", JsonConvert.SerializeObject(depositRequest), "response", JsonConvert.SerializeObject(api.TransferWalletResponse));
                return true;
            }
            else
            {
                InsertLog(gameID, "Deposit", "request", JsonConvert.SerializeObject(depositRequest), "response", JsonConvert.SerializeObject(api.ErrorMessage));
                error = new Error
                {
                    ErrorCode = "-9999",
                    ErrorDesc = api.ErrorMessage
                };
                return false;
            }
        }

        public override bool GameWithdraw(decimal amount, string transactionID, ref Error error)
        {
            var withdrawalRequest = new TransferWalletRequest
            {
                Amount = amount,
                BrandId = brandId,
                Currency = currencyID,
                PlayerId = gameMap.GamePlayerID,
                TransactionId = transactionID,
                Type = "withdraw"
            };
            if (api.TransferWallet(withdrawalRequest))
            {
                InsertLog(gameID, "Withdraw", "request", JsonConvert.SerializeObject(withdrawalRequest), "response", JsonConvert.SerializeObject(api.TransferWalletResponse));
                return true;
            }
            else
            {
                InsertLog(gameID, "Withdraw", "request", JsonConvert.SerializeObject(withdrawalRequest), "response", JsonConvert.SerializeObject(api.ErrorMessage));
                error = new Error
                {
                    ErrorCode = "-9999",
                    ErrorDesc = api.ErrorMessage
                };
                return false;
            }
        }

        public override vm.GetBalanceVM GetBalance()
        {
            var res = new vm.GetBalanceVM();
            try
            {
                var getBalanceRequest = new FetchWalletRequest
                {
                    BrandId = brandId,
                    Currency = currencyID,
                    PlayerId = gameMap.GamePlayerID,
                };
                if (api.FetchWallet(getBalanceRequest))
                {
                    InsertLog(gameID, "GetBalance", "request", JsonConvert.SerializeObject(getBalanceRequest), "response", JsonConvert.SerializeObject(api.FetchWalletResponse));
                    res.PlayerID = playerID;
                    res.Balance = api.FetchWalletResponse.Balance;
                    res.Game = gameID;
                    res.Message = "Successfully retrieved balance.";
                    res.Code = 0;
                }
                else
                {
                    InsertLog(gameID, "GetBalance", "request", JsonConvert.SerializeObject(getBalanceRequest), "response", JsonConvert.SerializeObject(api.ErrorMessage));
                    res.PlayerID = playerID;
                    res.Balance = null;
                    res.Game = gameID;
                    res.Message = api.ErrorMessage;
                    res.Code = 2;
                }
            }
            catch (Exception ex)
            {
                _common.LogError("GetBalance" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);
                var err = new Error
                {
                    ErrorCode = "999",
                    ErrorDesc = ex.Message.ToString()
                };
                InsertLog(gameID, "GetBalance", "Error", JsonConvert.SerializeObject(err));
                res.PlayerID = playerID;
                res.Balance = null;
                res.Game = gameID;
                res.Message = ex.Message.ToString();
                res.Code = 99;
            }
            return res;
        }

        public override decimal? GetGameBalance()
        {
            var getBalanceRequest = new FetchWalletRequest
            {
                BrandId = brandId,
                Currency = currencyID,
                PlayerId = gameMap.GamePlayerID,
            };
            if (api.FetchWallet(getBalanceRequest))
            {
                InsertLog(gameID, "GetGameBalance", "request", JsonConvert.SerializeObject(getBalanceRequest), "response", JsonConvert.SerializeObject(api.FetchWalletResponse));
                return (decimal)api.FetchWalletResponse.Balance;
            }
            else
            {
                return null;
            }
        }

        public vm.GetGameUrlVM GetOctoPlayGameUrl(string langId, string gameId, bool isMobile = false)
        {
            ipAddr = Utils.GetClientIp();
            var startGameRequest = new GameLaunchRequest
            {
                BrandId = brandId,
                Country = "MY",
                Currency = currencyID,
                GameId = gameId,
                Ip = ipAddr,
                LobbyUrl = lobbyUrl,
                Locale = langId,
                Mode = "wallet",
                Platform = isMobile ? "mobile" : "web",
                PlayerId = gameMap.GamePlayerID,
                SessionId = Guid.NewGuid().ToString()
            };
            var res = new vm.GetGameUrlVM();
            if (api.GameLaunch(startGameRequest))
            {
                InsertLog(gameID, "StartGame", "request", JsonConvert.SerializeObject(startGameRequest), "response", JsonConvert.SerializeObject(api.GameLaunchResponse));
                res.PlayerID = playerID;
                res.Url = api.GameLaunchResponse.Url;
                res.Game = gameID;
                res.Message = "Success";
                res.Code = 0;
            }
            else
            {
                InsertLog(gameID, "StartGame", "request", JsonConvert.SerializeObject(startGameRequest), "response", JsonConvert.SerializeObject(api.ErrorMessage));
                res.PlayerID = playerID;
                res.Url = null;
                res.Game = gameID;
                res.Message = api.ErrorMessage;
                res.Code = 2;
            }
            return res;
        }

        public override vm.GetGameUrlVM GetGameUrl(string langId, bool isMobile = false)
        {
            var res = new vm.GetGameUrlVM();
            try
            {
                if (gameMap != null)
                {
                    string url = string.Format("{0}?playerid={1}&currency={2}&ip={3}&lang={4}&mobile={5}", lobbyUrl, playerID, currencyID, ipAddr, langId, isMobile.ToString().ToLower());
                    InsertLog(gameID, "GetGameUrl", "playerid", playerID, "currency", currencyID, "ip", ipAddr, "lang", langId, "url", url);
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
                _common.LogError("GetGameUrl" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);
                var err = new Error { ErrorCode = "999", ErrorDesc = ex.Message.ToString() };
                InsertLog(gameID, "Login", "Error", JsonConvert.SerializeObject(err));
                res.PlayerID = playerID;
                res.Url = null;
                res.Game = gameID;
                res.Message = ex.Message.ToString();
                res.Code = 99;
            }
            return res;
        }

        public override bool KickPlayer(string gamePlayerId, ref Error error)
        {
            error = new Error { ErrorCode = "999", ErrorDesc = "KickPlayer not implemented for OctoPlay." };
            return false;
        }

        public override string RetryDeposit(decimal amount, string trackerId, ref Error error)
        {
            throw new NotImplementedException();
        }

        public override string RetryWithdraw(decimal amount, string trackerId, ref Error error)
        {
            throw new NotImplementedException();
        }

        public List<OctoPlayLobbyGame> GetGameList()
        {
            api = new OctoPlay(apiUrl, privateKey, publicKey, brandId, timeout);

            var gameListRequest = new GameListRequest
            {
                BrandId = brandId
            };

            var apiCall = api.GetGameList(gameListRequest);

            if (apiCall && api.GameListResponse != null && api.GameListResponse.Games != null)
            {
                InsertLog(gameID, "GetGameList", "request", JsonConvert.SerializeObject(gameListRequest), "response", JsonConvert.SerializeObject(api.GameListResponse));

                var data = api.GameListResponse.Games
                    .Select(x =>
                        new OctoPlayLobbyGame
                        {
                            AllSeq = 1,
                            GameName = x.GameName,
                            GameId = x.GameId,  
                            ThumbnailUrl = x.ThumbnailUrl,
                            ProviderName = x.ProviderName,
                            ReleaseDate = x.ReleaseDate,
                            Rtp = x.Rtp,
                            HotSeq = 1,
                            IsHot = false,
                            IsAll = true
                        })
                    .ToList();

                return data;
            }
            else
            {
                return new List<OctoPlayLobbyGame>();
            }
        }

    }
}
