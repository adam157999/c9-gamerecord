using GameRecord.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using vm = GameRecord.Controllers.GameAPIController;

namespace GameRecord.Controllers
{
    public class VivoConnector : BaseConnector
    {
        // GET: DGConnector
        private string gameID = "VIVO";
        private string apiUrl = "";
        private string gameUrl = "";
        private string passKey = "";
        private string operatorId = "";
        private string serverId = "";
        private string gameId = "";
        private int timeOutSec = 30;
        private GameMapping gameMap;

        public VivoConnector(string PlayerID = "", string CurrencyID = "", string IPAddr = "", GameEnvironment Environment = GameEnvironment.PROD) : base(PlayerID, CurrencyID, IPAddr, Environment)
        {
            using (BingoConn db = new BingoConn())
            {

                var apiConfig = CatchData.GetApiKeys(gameID.ToUpper().Trim(), Environment.ToString(), currencyID.ToUpper().Trim());
                if (string.IsNullOrEmpty(CurrencyID)) apiConfig = CatchData.GetApiKeys(gameID.ToUpper().Trim(), Environment.ToString());

                if (apiConfig != null)
                {
                    JObject jObj = JObject.Parse(apiConfig.ApiConfig);
                    gameUrl = jObj["VIVOGameUrl"].ToString();
                    passKey = jObj["VIVOPassKey"].ToString();
                    operatorId = jObj["VIVOOperatorId"].ToString();
                    serverId = jObj["VIVOServerId"].ToString();
                    timeOutSec = Convert.ToInt32(jObj["VIVOTimeOutSec"].ToString());

                }
            }
        }

        public override vm.CheckAndCreateAccountVM CreatePlayer(string loginID, string loginPass, string fullName, string mcUID)
        {
            throw new NotImplementedException();
        }

        public override bool GameDeposit(decimal Amount, string TransactionId, ref Error Error)
        {
                var apiCall = AddBalanceLogic(Amount);
                InsertLog(gameID, "Deposit", "request", $"TransactionID : {TransactionId}, Amount : {Amount}, PlayerID : {playerID}", "response", apiCall);

                if (apiCall)
                {
                    return true;
                }
                else
                {
                    Error = new Error
                    {
                        ErrorCode = "-9999",
                        ErrorDesc = "Failed Game Deposite"
                    };
                    return false;
                }

        }

        public override bool GameWithdraw(decimal Amount, string TransactionId, ref Error Error)
        {
                var apiCall = ReduceBalanceLogic(Amount);
                InsertLog(gameID, "Withdraw", "request", $"TransactionID : {TransactionId}, Amount : {Amount}, PlayerID : {playerID}", "response", apiCall);

                if (apiCall)
                {
                    return true;
                }
                else
                {
                    Error = new Error
                    {
                        ErrorCode = "-9999",
                        ErrorDesc = "Failed Game Withdraw"
                    };
                    return false;
                }
        }

        public override vm.GetBalanceVM GetBalance()
        {
            var res = new vm.GetBalanceVM();

            try
            {
                    var apiCall = GetBalanceLogic();
                    InsertLog(gameID, "GetBalance", "request", "Seamless Wallet No Request", "response", apiCall);

                    if (!string.IsNullOrEmpty(apiCall))
                    {
                        res.PlayerID = playerID;
                        res.Balance = Convert.ToDecimal(apiCall);
                        res.Game = gameID;
                        res.Message = "Successfully get balance.";
                        res.Code = 0;
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

        public override decimal? GetGameBalance()
        {
                var apiCall = GetBalanceLogic();
                InsertLog(gameID, "GetBalance", "request", "Seamless Wallet No Request", "response", apiCall);

                if (!string.IsNullOrEmpty(apiCall))
                {
                    return Convert.ToDecimal(apiCall);
                }
                else
                {
                    return null;
                }
        }

        public override vm.GetGameUrlVM GetGameUrl(string langId, bool IsMobile = false)
        {
            throw new NotImplementedException();
        }

        public vm.GetGameUrlVM GetGameSeamlessUrl(string langId,string token, bool IsMobile = false)
        {
            var res = new vm.GetGameUrlVM();

            try
            {
                langId = GetGameLanguage(gameID, langId);
                InsertLog(gameID, "GetGameUrl", "request", "Vivo no request", "response", "Vivo no response");
                //token = "a4bccaeb3554b1c0b8bf76f034451d40caa24eee";
                gameUrl = $"{gameUrl}/?token={token}&operatorID={operatorId}&application=lobby&serverid={serverId}";

                res.PlayerID = playerID;
                res.Url = gameUrl;
                res.Game = gameID;
                res.Message = "Success";
                res.Code = 0;
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

        public override string RetryDeposit(decimal Amount, string TrackerId, ref Error Error)
        {
            throw new NotImplementedException();
        }

        public override string RetryWithdraw(decimal Amount, string TrackerId, ref Error Error)
        {
            throw new NotImplementedException();
        }

        public override bool KickPlayer(string GamePlayerId, ref Error Error)
        {
            throw new NotImplementedException();
        }

        private string GetBalanceLogic()
        {
            try
            {
                using (BingoConn db = new BingoConn())
                {
                    var foundRecord = db.SeamlessWallets.SingleOrDefault(
                        x => x.Game == gameID
                    && x.Username == playerID);

                    if (foundRecord != null)
                    {
                        return (foundRecord.Cash + foundRecord.Chip).ToString("F");
                    }
                    else
                    {
                        return "0";
                    }
                }
            }

            catch (Exception ex)
            {
                throw;
            }
        }

        private bool AddBalanceLogic(decimal Amount)
        {
            try
            {
                using (BingoConn db = new BingoConn())
                {
                    var foundRecord = db.SeamlessWallets.SingleOrDefault(
                        x => x.Game == gameID
                    && x.Username == playerID);

                    if (foundRecord != null)
                    {
                        decimal totalValue = foundRecord.Cash + Amount;
                        db.SaveChanges();
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            catch (Exception ex)
            {
                throw;
            }
        }

        private bool ReduceBalanceLogic(decimal Amount)
        {
            try
            {
                using (BingoConn db = new BingoConn())
                {
                    var foundRecord = db.SeamlessWallets.SingleOrDefault(
                        x => x.Game == gameID
                    && x.Username == playerID);

                    if (foundRecord != null)
                    {
                        decimal totalValue = foundRecord.Cash - Amount;
                        foundRecord.Cash = totalValue;
                        db.SaveChanges();
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            catch (Exception ex)
            {
                throw;
            }
        }
    }
}