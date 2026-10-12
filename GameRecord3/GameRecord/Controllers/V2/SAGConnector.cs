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

namespace GameRecord.Controllers
{
    public class SAGConnector : BaseConnector
    {
        private string gameID = "SAG";
        private string apiUrl = "";
        private string secretKey = "";
        private string encKey = "";
        private string desKey = "";
        private string clientLoader = "";
        private string lobbyName = "";
        private int timeOutSec = 30;
        private GameMapping gameMap;
        private SAG.SAG api = null;

        public SAGConnector(string PlayerID = "", string CurrencyID = "", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD) : base(PlayerID, CurrencyID, IPAddr, Environment)
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
                    apiUrl = jObj["SAGApiUrl"].ToString();
                    secretKey = jObj["SAGSecretKey"].ToString();
                    encKey = jObj["SAGEncKey"].ToString();
                    desKey = jObj["SAGDesKey"].ToString();
                    clientLoader = jObj["SAGClientLoader"].ToString();
                    lobbyName = jObj["SAGLobbyName"].ToString();
                    timeOutSec = Convert.ToInt32(jObj["SAGTimeOutSec"].ToString());

                    api = new SAG.SAG(apiUrl, secretKey, encKey, desKey, clientLoader, lobbyName, timeOutSec);
                }
            }
        }

        public override vm.CheckAndCreateAccountVM CreatePlayer(string loginID, string loginPass, string fullName, string mcUID)
        {
            var res = new vm.CheckAndCreateAccountVM();

            try
            {
                if (gameMap == null)
                {
                    var apiCheck = api.VerifyUsername(loginID);
                    if (apiCheck && !api.VerifyUsernameResponse.IsExist)
                    {
                        var apiCall = api.RegisterUser(loginID, currencyID);
                        InsertLog(gameID, "RegisterUser", "request", api.Request, "response", api.Response);

                        targetApiUrl = api.Request;
                        request = api.Request;
                        response = api.Response;

                        if (apiCall)
                        {
                            InsertGameMapping(gameID, loginID, loginPass, mcUID);
                            gameMap = LoadGameMap(gameID, playerID, currencyID);

                            res.PlayerID = playerID;
                            res.GamePlayerID = loginID;
                            res.Game = gameID;
                            res.Message = "Success";
                            res.Code = 0;
                        }
                        else
                        {
                            res.PlayerID = playerID;
                            res.GamePlayerID = null;
                            res.Game = gameID;
                            res.Message = api.ErrorMessage;
                            res.Code = 2;
                        }
                    }
                    else
                    {
                        res.PlayerID = playerID;
                        res.GamePlayerID = null;
                        res.Game = gameID;
                        res.Message = api.ErrorMessage;
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
                _common.LogError("Register" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

                var err = new
                {
                    ErrorCode = "999",
                    ErrorDesc = ex.Message.ToString()
                };

                InsertLog(gameID, "RegisterUser", "Error", JsonConvert.SerializeObject(err));

                res.PlayerID = playerID;
                res.GamePlayerID = loginID;
                res.Game = gameID;
                res.Message = ex.Message.ToString();
                res.Code = 99;
            }

            return res;
        }

        public override bool GameDeposit(decimal Amount, string TrackerId, ref Error Error)
        {
            if (gameMap != null)
            {
                var apiCall = api.Deposit(gameMap.GamePlayerID, Amount, TrackerId);
                InsertLog(gameID, "Deposit", "request", api.Request, "response", api.Response);

                targetApiUrl = api.Request;
                request = api.Request;
                response = api.Response;

                if (apiCall)
                {
                    return true;
                }
                else
                {
                    Error = new Error
                    {
                        ErrorCode = "-9999",
                        ErrorDesc = api.ErrorMessage
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

        public override bool GameWithdraw(decimal Amount, string TrackerId, ref Error Error)
        {
            if (gameMap != null)
            {
                var apiCall = api.Withdraw(gameMap.GamePlayerID, Amount, TrackerId);
                InsertLog(gameID, "Withdraw", "request", api.Request, "response", api.Response);

                targetApiUrl = api.Request;
                request = api.Request;
                response = api.Response;

                if (apiCall)
                {
                    return true;
                }
                else
                {
                    Error = new Error
                    {
                        ErrorCode = "-9999",
                        ErrorDesc = api.ErrorMessage
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

        public override vm.GetBalanceVM GetBalance()
        {
            var res = new vm.GetBalanceVM();

            try
            {
                if (gameMap != null)
                {
                    var apiCall = api.GetUserStatusDV(gameMap.GamePlayerID);
                    InsertLog(gameID, "GetBalance", "request", api.Request, "response", api.Response);

                    targetApiUrl = api.Request;
                    request = api.Request;
                    response = api.Response;

                    if (apiCall)
                    {
                        res.PlayerID = playerID;
                        res.Balance = api.GetUserStatusDVResponse.Balance;
                        res.Game = gameID;
                        res.Message = "Successfully get balance.";
                        res.Code = 0;
                    }
                    else
                    {
                        res.PlayerID = playerID;
                        res.Balance = null;
                        res.Game = gameID;
                        res.Message = api.ErrorMessage;
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

                InsertLog(gameID, "GetUserStatusDV",  "Error", JsonConvert.SerializeObject(err));

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
            if (gameMap != null)
            {
                var apiCall = api.GetUserStatusDV(gameMap.GamePlayerID);
                InsertLog(gameID, "GetBalance", "request", api.Request, "response", api.Response);

                targetApiUrl = api.Request;
                request = api.Request;
                response = api.Response;

                if (apiCall)
                {
                    return Convert.ToDecimal(api.GetUserStatusDVResponse.Balance);
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

        public override vm.GetGameUrlVM GetGameUrl(string langId, bool IsMobile = false)
        {
            var res = new vm.GetGameUrlVM();

            try
            {
                if (gameMap != null)
                {
                    var sessionId = new Guid().ToString();
                    langId = GetGameLanguage(gameID, langId);
                    var apiCall = api.Login(gameMap.GamePlayerID, currencyID, Lang: langId, Mobile: IsMobile);
                    InsertLog(gameID, "Login", "request", api.Request, "response", api.Response);

                    targetApiUrl = api.Request;
                    request = api.Request;
                    response = api.Response;

                    if (apiCall)
                    {
                        res.PlayerID = playerID;
                        res.Url = api.LoginResponse.GameURL;
                        res.Game = gameID;
                        res.Message = "Success";
                        res.Code = 0;
                    }
                    else
                    {
                        res.PlayerID = playerID;
                        res.Url = null;
                        res.Game = gameID;
                        res.Message = api.ErrorMessage;
                        res.Code = 2;
                    }
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

                InsertLog(gameID, "Login",  "Error", JsonConvert.SerializeObject(err));

                res.PlayerID = playerID;
                res.Url = null;
                res.Game = gameID;
                res.Message = ex.Message.ToString();
                res.Code = 99;
            }

            return res;
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

        public SAG.Models.GetAllBetDetailsForTimeIntervalDVResponse GetBetHistory(DateTime dateFrom, DateTime dateTo)
        {
            var apiCall = api.GetAllBetDetailsForTimeIntervalDV(dateFrom, dateTo);

            targetApiUrl = api.Request;
            request = api.Request;
            response = api.Response;

            if (apiCall)
            {
                return api.GetAllBetDetailsForTimeIntervalDVResponse;
            }
            return null;
        }
    }
}