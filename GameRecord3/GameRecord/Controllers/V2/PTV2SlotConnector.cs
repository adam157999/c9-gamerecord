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
using Newtonsoft.Json.Linq;
using System.Text;
using GameRecord.Models;
using System.Data.Entity.Validation;
using vm = GameRecord.Controllers.GameAPIController;
using PlaytechV2;
using PlaytechV2.Models;

namespace GameRecord.Controllers
{
    public class PTV2SlotConnector : BaseConnector
    {
        private string gameID = "PTV2SLOT";
        private string apiUrl = "";
        private string lobbyUrl = "";
        private string entityKey = "";
        private string certPassword = "";
        private string certPath = "";
        private string adminName = "";
        private int timeOutSec = 30;
        private GameMapping gameMap;
        private PlaytechV2.PT api = null;
        private string systemId = "";
        private string virtualDatabase = "";
        private string mobileHub = "";

        public PTV2SlotConnector(string PlayerID = "", string CurrencyID = "", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD) : base(PlayerID, CurrencyID, IPAddr, Environment)
        {
            using (BingoConn db = new BingoConn())
            {
                gameMap = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == gameID.ToUpper().Trim() &&
                                                                                                x.PlayerID == playerID &&
                                                                                                (!string.IsNullOrEmpty(currencyID) ? x.Currency.ToUpper() : "") == currencyID &&
                                                                                                x.IsActive == true).FirstOrDefault();

                var apiConfig = CatchData.GetApiKeys(gameID.ToUpper().Trim(), Environment.ToString(), currencyID.ToUpper().Trim());
                if (string.IsNullOrEmpty(CurrencyID)) apiConfig = CatchData.GetApiKeys(gameID.ToUpper().Trim(), Environment.ToString());

                if (apiConfig != null)
                {
                    JObject jObj = JObject.Parse(apiConfig.ApiConfig);
                    apiUrl = jObj["ApiUrl"].ToString();
                    lobbyUrl = jObj["LobbyUrl"].ToString();
                    entityKey = jObj["EntityKey"].ToString();
                    certPassword = jObj["CertPassword"].ToString();
                    certPath = jObj["CertPath"].ToString();
                    adminName = jObj["AdminName"].ToString();
                    timeOutSec = Convert.ToInt32(jObj["TimeOutSec"].ToString());

                    systemId = jObj["SystemId"].ToString();
                    virtualDatabase = jObj["VirtualDatabase"].ToString();
                    mobileHub = jObj["MobileHub"].ToString();

                    api = new PlaytechV2.PT(apiUrl, entityKey, certPassword, certPath, adminName, timeOutSec);
                }
            }
        }

        #region Main Function 
        public override vm.CheckAndCreateAccountVM CreatePlayer(string loginID, string loginPass, string fullName, string mcUID)
        {
            var res = new vm.CheckAndCreateAccountVM();

            try
            {
                if (gameMap == null)
                {

                    //var apiCall = api.CreatePlayer(loginID, loginPass, currencyID);
                    var apiCall = api.CreatePlayer(loginID, loginPass, "CNY");
                    InsertLog(gameID, "CreatePlayer", "request", api.request, "response", api.response);

                    targetApiUrl = api.request;
                    request = api.request;
                    response = api.response;

                    if (apiCall)
                    {
                        InsertGameMapping(gameID, loginID, loginPass, mcUID);
                        gameMap = LoadGameMap(gameID, playerID, currencyID);

                        res.PlayerID = playerID;
                        res.GamePlayerID = loginID;
                        res.Game = gameID;
                        res.Message = api.CreatePlayerResponse.result.result;
                        res.Code = 0;
                    }
                    else
                    {
                        res.PlayerID = playerID;
                        res.GamePlayerID = null;
                        res.Game = gameID;
                        res.Message = api.errorMessage;
                        res.Code = 2;
                    }

                }
                else
                {
                    res.PlayerID = playerID;
                    res.GamePlayerID = gameMap.GamePlayerID;
                    res.Game = gameID;
                    res.Message = "Retrieve Success";
                    res.Code = 1;
                }
            }
            catch (Exception ex)
            {
                _common.LogError("CreatePlayer" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

                var err = new
                {
                    ErrorCode = "999",
                    ErrorDesc = ex.Message.ToString()
                };

                InsertLog(gameID, "CreatePlayer_Exception", "Error", JsonConvert.SerializeObject(err));

                res.PlayerID = playerID;
                res.GamePlayerID = loginID;
                res.Game = gameID;
                res.Message = ex.Message.ToString();
                res.Code = 99;
            }

            return res;
        }
        public override vm.GetBalanceVM GetBalance()
        {
            var res = new vm.GetBalanceVM();

            try
            {
                if (gameMap != null)
                {
                    var apiCall = api.PlayerBalance(gameMap.GamePlayerID);
                    InsertLog(gameID, "GetBalance", "request", api.request, "response", api.response);

                    targetApiUrl = api.request;
                    request = api.request;
                    response = api.response;

                    if (apiCall)
                    {
                        res.PlayerID = playerID;
                        res.Balance = Convert.ToDecimal(api.PlayerBalanceResponse.result.balance);
                        res.Game = gameID;
                        res.Message = "Successfully get balance.";
                        res.Code = 0;
                    }
                    else
                    {
                        res.PlayerID = playerID;
                        res.Balance = null;
                        res.Game = gameID;
                        res.Message = api.errorMessage;
                        res.Code = 2;
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
                _common.LogError("GetBalance" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

                var err = new Error
                {
                    ErrorCode = "999",
                    ErrorDesc = ex.Message.ToString()
                };

                InsertLog(gameID, "GetBalance_Exception", "Error", JsonConvert.SerializeObject(err));

                res.PlayerID = playerID;
                res.Balance = null;
                res.Game = gameID;
                res.Message = ex.Message.ToString();
                res.Code = 99;
            }

            return res;
        }

        public override bool KickPlayer(string GamePlayerId, ref Error Error)
        {
            var res = new vm.GetKickPlayerUrlVM();

            try
            {
                if (gameMap != null)
                {
                    var apiCall = api.LogoutPlayer(gameMap.GamePlayerID);
                    InsertLog(gameID, "KickPlayer", "request", api.request, "response", api.response);

                    targetApiUrl = api.request;
                    request = api.request;
                    response = api.response;

                    if (apiCall)
                    {
                        res.PlayerID = playerID;
                        res.Game = gameID;
                        res.Message = api.LogoutPlayerResponse.result.result;
                        res.Code = 0;
                    }
                    else
                    {
                        res.PlayerID = playerID;
                        res.Game = gameID;
                        res.Message = api.errorMessage;
                        res.Code = 2;
                    }
                }
                else
                {
                    res.PlayerID = playerID;
                    res.Game = gameID;
                    res.Message = "Game account not found.";
                    res.Code = 1;
                }
            }
            catch (Exception ex)
            {
                _common.LogError("KickPlayer" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

                var err = new Error
                {
                    ErrorCode = "999",
                    ErrorDesc = ex.Message.ToString()
                };

                InsertLog(gameID, "KickPlayer_Exception", "Error", JsonConvert.SerializeObject(err));

                res.PlayerID = playerID;
                res.Game = gameID;
                res.Message = ex.Message.ToString();
                res.Code = 99;
            }

            return res.Code == 0;
        }

        public override vm.GetGameUrlVM GetGameUrl(string langId, bool IsMobile = false)
        {
            var res = new vm.GetGameUrlVM();

            try
            {
                if (gameMap != null)
                {
                    langId = GetGameLanguage(gameID, langId);
                    string param = string.Format("?playerName={0}&lang={1}{2}", (gameMap.GamePlayerID.Trim()).ToUpper(), langId, (IsMobile ? "&mb=1" : ""));

                    targetApiUrl = lobbyUrl + param;
                    request = param;
                    response = "";

                    InsertLog(gameID, "GetGameUrl", "request", lobbyUrl + param, "response", api.response);

                    res.PlayerID = playerID;
                    res.Url = lobbyUrl + param;
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
                _common.LogError("Bet" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

                var err = new Error
                {
                    ErrorCode = "999",
                    ErrorDesc = ex.Message.ToString()
                };

                InsertLog(gameID, "Bet_Exception", "Error", JsonConvert.SerializeObject(err));

                res.PlayerID = playerID;
                res.Url = null;
                res.Game = gameID;
                res.Message = ex.Message.ToString();
                res.Code = 99;
            }

            return res;
        }

        public PTV2Login LaunchGame(string playerName, string password, string gameCode, string lang = "en", bool isMobile = false)
        {
            //if (gameMap == null)
            //    return null;

            var model = new PTV2Login
            {
                PlayerId = playerName,
                Password = password,
                LangCode = GetGameLanguage(gameID, lang),
                GameCode = gameCode,
                SystemId = systemId,
                VirtualDatabase = virtualDatabase,
                MobileHub = mobileHub,
                Platform = (isMobile ? "mobile" : "web")
            };

            return model;
        }

        public override decimal? GetGameBalance()
        {
            if (gameMap != null)
            {
                var apiCall = api.PlayerBalance(gameMap.GamePlayerID);
                InsertLog(gameID, "GetBalance", "request", api.request, "response", api.response);

                targetApiUrl = api.request;
                request = api.request;
                response = api.response;

                if (apiCall)
                {
                    return Convert.ToDecimal(api.PlayerBalanceResponse.result.balance);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public override bool GameDeposit(decimal Amount, string TrackerId, ref Error Error)
        {
            if (gameMap != null)
            {
                var apiCall = api.Deposit(gameMap.GamePlayerID, Convert.ToDouble(Amount), TrackerId);
                InsertLog(gameID, "Deposit", "request", api.request, "response", api.response);

                targetApiUrl = api.request;
                request = api.request;
                response = api.response;

                if (apiCall)
                {
                    return true;
                }
                else
                {
                    Error = new Error
                    {
                        ErrorCode = "-9999",
                        ErrorDesc = api.errorMessage
                    };
                    return false;
                }
            }
            else
            {
                Error = new Error
                {
                    ErrorCode = "-9999",
                    ErrorDesc = "Retrieve Success"
                };
                return false;
            }
        }

        public override string RetryDeposit(decimal Amount, string TrackerId, ref Error Error)
        {
            throw new NotImplementedException();
        }

        public override bool GameWithdraw(decimal Amount, string TrackerId, ref Error Error)
        {
            if (gameMap != null)
            {
                var apiCall = api.Withdrawal(gameMap.GamePlayerID, Convert.ToDouble(Amount), TrackerId);
                InsertLog(gameID, "Withdraw", "request", api.request, "response", api.response);

                targetApiUrl = api.request;
                request = api.request;
                response = api.response;

                if (apiCall)
                {
                    return true;
                }
                else
                {
                    Error = new Error
                    {
                        ErrorCode = "-9999",
                        ErrorDesc = api.errorMessage
                    };
                    return false;
                }
            }
            else
            {
                Error = new Error
                {
                    ErrorCode = "-9999",
                    ErrorDesc = "Retrieve Success"
                };
                return false;
            }
        }

        public override string RetryWithdraw(decimal Amount, string TrackerId, ref Error Error)
        {
            throw new NotImplementedException();
        }

        public GameTransactionsResponse GameTransactions(DateTime dateFrom, DateTime dateTo, ref int errorCode, ref string errorMessage, int page = 1, int pageSize = 5000)
        {
            var apiCall = api.GameTransactions(dateFrom, dateTo, "", null, page, pageSize);

            //InsertLog(gameID, "GameTransactions", "request", api.request, "response", api.response);

            targetApiUrl = api.request;
            request = api.request;
            response = api.response;

            if (apiCall)
            {
                return api.GameTransactionsResponse;
            }
            else
            {
                errorCode = Convert.ToInt16(api.GameTransactionsResponse.errorcode);
                errorMessage = api.GameTransactionsResponse.error;
                return null;
            }
        }
        #endregion

        #region Private Function
        
        #endregion
    }
}