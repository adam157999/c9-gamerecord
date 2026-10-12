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
using GameRecord.Controllers.V2;
using System.Web.Http.Cors;
using System.Threading;
using System.Net.Sockets;
using GameRecord.Gamess.OctoPlay;
using GameRecord.Gamess.BTG;

namespace GameRecord.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*", exposedHeaders: "X-Custom-Header")]

    public class GameAPIController : ApiController
    {
        public static readonly object MyLock = new object();
        private static Random random = new Random();
        private static string FilePrefix = "GAMEAPI";
        public string apiPhoneList = ConfigurationManager.AppSettings["GameAPIPhoneList"];

        #region models
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
            //public decimal? Amount { get; set; }
            public decimal? Cash { get; set; }
            public decimal? Chip { get; set; }
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
        public class GetKickPlayerUrlVM
        {
            public string PlayerID { get; set; }
            public string Message { get; set; }
            public string Game { get; set; }
            public int Code { get; set; }
            public decimal? Cash { get; set; }
            public decimal? Chip { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public bool? LockFromGame { get; set; }
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

        public class SeamlessWalletRequest
        {
            public string TrackerId { get; set; }
            public string UserId { get; set; }
            public decimal Cash { get; set; }
            public decimal Chips { get; set; }
            public string Currency { get; set; }
            public string GameCode { get; set; }


        // In FormGamePlayerID

        }
        #endregion

        [HttpGet]
        public HttpResponseMessage GetBalance(string Game, string PlayerID, string Currency, string TrackerId)
        {
            //lock (MyLock)
            //{
            string apiurl = "";
            Common _common = new Common();
            int TimeoutSec = 180000; //3 mins

            try
            {
                _common.InsertLog("GetBalance", "TrackerId", TrackerId, "Request", "", "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Ip", GetLocalIPAddress());

                using (BingoConn db = new BingoConn())
                {
                    var res = new GetBalanceVM();
                    string GameID = Game.ToUpper().Trim();
                    string UPlayerID = PlayerID.ToUpper().Trim();
                    string UCurrency = Currency.ToUpper().Trim();

                    var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID && x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

                    if (GameID == "VIVO")
                    {
                        var api = GetConnector(GameID, PlayerID, Currency, BaseConnector.GameEnvironment.STAG);
                        if (api != null)
                        {
                            res = api.GetBalance();
                        }
                        else
                        {
                            res.PlayerID = UPlayerID;
                            res.Balance = null;
                            res.Game = GameID;
                            res.Message = "Invalid game.";
                            res.Code = 4;
                        }
                    }
                    else if (record != null)
                    { 
                        var api = GetConnector(GameID, PlayerID, Currency, BaseConnector.GameEnvironment.STAG);
                        if (api != null)
                        {
                            res = api.GetBalance();
                        }
                        else
                        {
                            res.PlayerID = UPlayerID;
                            res.Balance = null;
                            res.Game = GameID;
                            res.Message = "Invalid game.";
                            res.Code = 4;
                        }
                    }
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
            //}
        }

        [HttpGet]
        public HttpResponseMessage CheckAndCreateAccount(string Game, string PlayerID, string Currency, string TrackerId)
        {
            //lock (MyLock)
            //{
            string apiurl = "";
            Common _common = new Common();
            int TimeoutSec = 180000; //3 mins
            string gamePlayerID = string.Empty;

            try
            {
                _common.InsertLog("CheckAndCreateAccount", "TrackerId", TrackerId, "Request", "", "Game", Game, "PlayerID", PlayerID, "Currency", Currency);

                using (BingoConn db = new BingoConn())
                {
                    var res = new CheckAndCreateAccountVM();
                    string GameID = Game.ToUpper().Trim();
                    string UPlayerID = PlayerID.ToUpper().Trim();
                    string UCurrency = Currency.ToUpper().Trim();

                    gamePlayerID = FormGamePlayerID(GameID, PlayerID);
                    var api = GetConnector(GameID, PlayerID, Currency, BaseConnector.GameEnvironment.STAG);
                    if (api != null)
                    {
                        res = api.CreatePlayer(gamePlayerID, "123456", gamePlayerID, UPlayerID);
                    }
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
            //}
        }

        public decimal? GetGameBalance(string Game, string PlayerID, string Currency, string TrackerId)
        {
            //lock (MyLock)
            //{
            string apiurl = "";
            int TimeoutSec = 180000; //3 mins
            Common _common = new Common();

            try
            {
                using (BingoConn db = new BingoConn())
                {
                    var res = new GetBalanceVM();
                    string GameID = Game.ToUpper().Trim();
                    string UPlayerID = PlayerID.ToUpper().Trim();
                    string UCurrency = Currency.ToUpper().Trim();

                    var api = GetConnector(GameID, PlayerID, Currency, BaseConnector.GameEnvironment.STAG);
                    if (api != null)
                    {
                        return api.GetGameBalance();
                    }

                    return null;

                }
            }
            catch (Exception ex)
            {
                _common.LogError($"{TrackerId}:GetGameBalance:EXCEPTION " + string.Format("{0}|{1}|{2}", Game, PlayerID, Currency) + " " + ex.Message.ToString(), FilePrefix);
                return null;
            }
            //}
        }

        [HttpGet]
        public HttpResponseMessage Withdraw(string Game, string PlayerID, string Currency, string TrackerId) //Currency need to get from bingotown Game table and pass in here (by myself)
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
                //if (Game == "EVOLUTIONGAMING")
                //{
                //    TrackerId = TrackerId.Substring(0, 15);
                //}
                _common.InsertLog("Withdraw", "TrackerId", TrackerId, "Request", "", "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "WMinAmount", WMinAmount, "WMaxAmount", WMaxAmount);

                using (BingoConn db = new BingoConn())
                {
                    var res = new WithdrawVM();
                    string GameID = Game.ToUpper().Trim();
                    string UPlayerID = PlayerID.ToUpper().Trim();
                    string UCurrency = Currency.ToUpper().Trim();
                    decimal WithdrawCash = 0;
                    decimal WithdrawChip = 0;

                    var qrecord = db.QuitGameTrackers.Where(x => x.TrackerID == TrackerId).FirstOrDefault();
                    if (qrecord == null)
                    {
                        QuitGameTracker obj = new QuitGameTracker()
                        {
                            TrackerID = TrackerId,
                            PlayerID = UPlayerID,
                            GameID = GameID,
                            Currency = UCurrency,
                            CreatedDt = DateTime.Now
                        };
                        db.QuitGameTrackers.Add(obj);
                        db.SaveChanges();
                    }
                    else
                    {
                        res.PlayerID = UPlayerID;
                        res.Game = GameID;
                        res.Message = "Duplicate transaction.";
                        res.Code = 5;
                        res.LockFromGame = false;
                        _common.InsertLog("Withdraw", "TrackerId", TrackerId, "Response", JsonConvert.SerializeObject(res), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "WMinAmount", WMinAmount, "WMaxAmount", WMaxAmount);
                        return new HttpResponseMessage()
                        {
                            StatusCode = HttpStatusCode.OK,
                            Content = new StringContent(JsonConvert.SerializeObject(res), System.Text.Encoding.UTF8, "application/json")
                        };
                    }

                    var game = db.Games.Where(x => !x.Deleted && x.Code.ToUpper().Trim() == GameID).FirstOrDefault();

                    decimal? gAmount = GetGameBalance(GameID, UPlayerID, UCurrency, TrackerId);

                    if (gAmount.Equals(null))
                    {
                        res.PlayerID = UPlayerID;
                        res.Game = GameID;
                        res.Message = "Failed to get withdraw game balance.";
                        res.Code = 5;
                        res.LockFromGame = false;
                        //_common.LogError($"{TrackerId}:Withdraw:FAILED - Failed To Get Withdraw Game Balance " + string.Format("{0}|{1}|{2}", Game, PlayerID, Currency), FilePrefix);
                        _common.InsertLog("Withdraw", "TrackerId", TrackerId, "Response", JsonConvert.SerializeObject(res), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "WMinAmount", WMinAmount, "WMaxAmount", WMaxAmount);
                        return new HttpResponseMessage()
                        {
                            StatusCode = HttpStatusCode.OK,
                            Content = new StringContent(JsonConvert.SerializeObject(res), System.Text.Encoding.UTF8, "application/json")
                        };
                    }

                    decimal Amount = Convert.ToDecimal(gAmount);
                    if (GameID == "AFB")
                    {
                        Amount = Amount / 100;
                    }

                    if (Amount >= 0)
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
                            //_common.LogError($"{TrackerId}:Withdraw:FAILED - Failed To Get Withdraw Transfer Limit " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Currency, Amount.ToString()), FilePrefix);
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
                            if (GameID == "KISS918")
                            {
                                var record = db.KISScoreLogs.Where(x => x.Username == UPlayerID && x.Flag == "R").FirstOrDefault();
                                if (record != null)
                                {
                                    record.ToDate = DateTime.Now;
                                    db.SaveChanges();
                                }
                            }

                            res.PlayerID = UPlayerID;
                            res.Game = GameID;
                            res.Message = "Successfully quit game.";
                            res.Code = 0;
                            res.Cash = 0;
                            res.Chip = 0;
                            //_common.LogError($"{TrackerId}:Withdraw:SUCCESS - Withdraw Less Than Min Limit, Skip Withdraw " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Currency, Amount.ToString()), FilePrefix);
                        }
                        //more than max amount, break into few steps
                        else if (Amount > WMaxAmount)
                        {
                            //_common.LogError($"{TrackerId}:Withdraw:SUCCESS - Withdraw More Than Max Limit, Amount To Be Withdraw: {Amount.ToString()}" + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Currency, Amount.ToString()), FilePrefix);
                            decimal withdrawAmt = 0;
                            decimal remainAmt = Convert.ToDecimal(Amount);
                            decimal amt = 0;
                            decimal? failedGameBalance = null;
                            int cnt = 1;

                            while (remainAmt != 0)
                            {
                                amt = remainAmt > WMaxAmount ? (decimal)WMaxAmount : remainAmt;
                                bool isSuccess = GameWithdraw(GameID, UPlayerID, UCurrency, (decimal)amt, TrackerId + "-" + cnt.ToString(), ref LockFromGame);
                                //_common.LogError($"{TrackerId}:Withdraw:" + (isSuccess == true ? "SUCCESS" : "FAILED") + $" - Current Withdraw Amount: {amt.ToString()} " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Currency, Amount.ToString()), FilePrefix);
                                if (isSuccess == false)
                                {
                                    failedGameBalance = GetGameBalance(GameID, UPlayerID, UCurrency, TrackerId);
                                    break;
                                }

                                remainAmt -= amt;
                                withdrawAmt += amt;
                                cnt++;

                                //_common.LogError($"{TrackerId}:Withdraw:SUCCESS - Remaining Amount: {remainAmt.ToString()}, Already Withdraw Amount: {withdrawAmt.ToString()} " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Currency, Amount.ToString()), FilePrefix);
                            }

                            if (!failedGameBalance.Equals(null))
                            {
                                //_common.LogError($"{TrackerId}:Withdraw:FAILED - End Balance: {failedGameBalance.ToString()} " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Currency, Amount.ToString()), FilePrefix);
                                res.PlayerID = UPlayerID;
                                res.Game = GameID;
                                res.Message = "Withdraw failed, please contact customer service.";
                                res.Code = 3;
                                //res.Amount = Amount;
                                res.Cash = Amount;
                                res.Chip = 0;
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
                                GameWithdraw rec = new GameWithdraw()
                                {
                                    TrackerID = TrackerId,
                                    PlayerID = PlayerID,
                                    GameID = GameID,
                                    Currency = UCurrency,
                                    Amount = Amount,
                                    CreatedDt = DateTime.Now
                                };
                                db.GameWithdraws.Add(rec);
                                db.SaveChanges();
                                //split cash and chip
                                if (game.HasChip)
                                {
                                    SplitWithdrawCashAndChip(GameID, UPlayerID, UCurrency, Amount, TrackerId, ref WithdrawCash, ref WithdrawChip);
                                }
                                else
                                {
                                    WithdrawCash = Amount;
                                    WithdrawChip = 0;
                                }

                                if (GameID == "KISS918")
                                {
                                    var record = db.KISScoreLogs.Where(x => x.Username == UPlayerID && x.Flag == "R").FirstOrDefault();
                                    if (record != null)
                                    {
                                        record.ToDate = DateTime.Now;
                                        db.SaveChanges();
                                    }
                                }
                                //_common.LogError($"{TrackerId}:Withdraw:SUCCESS - Total Amount: {Amount.ToString()} " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Currency, Amount.ToString()), FilePrefix);
                                res.PlayerID = UPlayerID;
                                res.Game = GameID;
                                res.Message = "Successfully withdraw.";
                                res.Code = 0;
                                //res.Amount = Amount;
                                res.Cash = WithdrawCash;
                                res.Chip = WithdrawChip;
                            }
                        }
                        //normal condition
                        else
                        {
                            //_common.LogError($"{TrackerId}:Withdraw:SUCCESS - Withdraw Normal Condition " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Currency, Amount.ToString()), FilePrefix);
                            bool isSuccess = GameWithdraw(GameID, UPlayerID, UCurrency, Amount, TrackerId, ref LockFromGame);
                            if (isSuccess == false)
                            {
                                res.PlayerID = UPlayerID;
                                res.Game = GameID;
                                res.Message = "Withdraw failed, please contact customer service.";
                                res.Code = 3;
                                //res.Amount = Amount;
                                res.Cash = Amount;
                                res.Chip = 0;
                                res.LockFromGame = false; //no need lock first since failed response return will not credit back money
                                                          //_common.LogError($"{TrackerId}:Withdraw:FAILED - Withdraw Failed, Return To App " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Currency, Amount.ToString()), FilePrefix);
                                msg = $"Withdraw:FAILED - {UPlayerID} failed to withdraw {Amount.ToString()} from {GameID}.";
                                foreach (var phone in apiPhoneList.Split(','))
                                {
                                    _common.SendSmsToBulkSms(phone, msg);
                                }
                            }
                            else
                            {
                                GameWithdraw rec = new GameWithdraw()
                                {
                                    TrackerID = TrackerId,
                                    PlayerID = PlayerID,
                                    GameID = GameID,
                                    Currency = UCurrency,
                                    Amount = Amount,
                                    CreatedDt = DateTime.Now
                                };
                                db.GameWithdraws.Add(rec);
                                db.SaveChanges();

                                //split cash and chip
                                if (game.HasChip)
                                {
                                    SplitWithdrawCashAndChip(GameID, UPlayerID, UCurrency, Amount, TrackerId, ref WithdrawCash, ref WithdrawChip);
                                }
                                else
                                {
                                    WithdrawCash = Amount;
                                    WithdrawChip = 0;
                                }

                                if (GameID == "KISS918")
                                {
                                    var record = db.KISScoreLogs.Where(x => x.Username == UPlayerID && x.Flag == "R").FirstOrDefault();
                                    if (record != null)
                                    {
                                        record.ToDate = DateTime.Now;
                                        db.SaveChanges();
                                    }
                                }

                                res.PlayerID = UPlayerID;
                                res.Game = GameID;
                                res.Message = "Successfully withdraw.";
                                res.Code = 0;
                                //res.Amount = Amount;
                                res.Cash = WithdrawCash;
                                res.Chip = WithdrawChip;
                                //_common.LogError($"{TrackerId}:Withdraw:SUCCESS - Total Amount: {Amount.ToString()} " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Currency, Amount.ToString()), FilePrefix);
                            }
                        }
                    }
                    else
                    {
                        res.PlayerID = UPlayerID;
                        res.Game = GameID;
                        res.Message = "Player has negative balance.";
                        res.Code = 4;
                        //res.Amount = Amount;
                        res.Cash = Amount;
                        res.Chip = 0;
                        res.LockFromGame = true;
                        //_common.LogError($"{TrackerId}:Withdraw:FAILED - Player Has Negative Balance " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Currency, Amount.ToString()) + " " + JsonConvert.SerializeObject(res), FilePrefix);
                        msg = $"Withdraw:FAILED - {UPlayerID} has negative balance for {GameID}.";
                        foreach (var phone in apiPhoneList.Split(','))
                        {
                            _common.SendSmsToBulkSms(phone, msg);
                        }
                    }

                    //special (pp)
                    if (res.Code == 0)
                    {
                        if (GameID == "918H5")
                            _common.Sql_RunAndCommit("update Kiss918H5Entry set QuitDt=CAST(SYSDATETIMEOFFSET() AT TIME ZONE 'Singapore Standard Time' AS datetime) where PlayerId='" + UPlayerID + "'");
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
                //_common.LogError($"{TrackerId}:Withdraw:EXCEPTION " + string.Format("{0}|{1}|{2}", Game, PlayerID, Currency) + " " + ex.Message.ToString(), FilePrefix);
                _common.InsertLog("Withdraw", "TrackerId", TrackerId, "Exception", ex.Message.ToString(), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "WMinAmount", WMinAmount, "WMaxAmount", WMaxAmount);
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

        public bool GameDeposit(string Game, string PlayerID, string Currency, decimal Amount, string TrackerId, ref bool? LockFromGame) //Currency need to get from bingotown Game table and pass in here (by myself)
        {
            //lock (MyLock)
            //{
            string apiurl = "";
            Common _common = new Common();
            int TimeoutSec = 180000; //3 mins

            try
            {
                using (BingoConn db = new BingoConn())
                {
                    var res = new DepositVM();
                    string GameID = Game.ToUpper().Trim();
                    string UPlayerID = PlayerID.ToUpper().Trim();
                    string UCurrency = Currency.ToUpper().Trim();

                    BaseConnector.Error error = new BaseConnector.Error();
                    var api = GetConnector(GameID, PlayerID, Currency, BaseConnector.GameEnvironment.STAG);
                    if (api != null)
                    {
                        bool result = api.GameDeposit(Amount, TrackerId, ref error);

                        if (!result)
                        {
                            LockFromGame = CheckIsLockPlayer(GameID, error.ErrorCode);
                        }

                        return result;
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
            //}
        }

        public string RetryDeposit(string Game, string PlayerID, string Currency, decimal Amount, string TrackerId)
        {
            lock (MyLock)
            {
                //string apiurl = "";
                Common _common = new Common();
                //int TimeoutSec = 180000; //3 mins

                try
                {
                    using (BingoConn db = new BingoConn())
                    {
                        var res = new DepositVM();
                        string GameID = Game.ToUpper().Trim();
                        string UPlayerID = PlayerID.ToUpper().Trim();
                        string UCurrency = Currency.ToUpper().Trim();

                        BaseConnector.Error error = new BaseConnector.Error();
                        var api = GetConnector(GameID, PlayerID, Currency, BaseConnector.GameEnvironment.STAG);
                        if (api != null)
                        {
                            return api.RetryDeposit(Amount, TrackerId, ref error);
                        }
                        else
                        {
                            return "";
                        }
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
            //lock (MyLock)
            //{
            string apiurl = "";
            Common _common = new Common();
            int TimeoutSec = 180000; //3 mins

            try
            {
                using (BingoConn db = new BingoConn())
                {
                    var res = new WithdrawVM();
                    string GameID = Game.ToUpper().Trim();
                    string UPlayerID = PlayerID.ToUpper().Trim();
                    string UCurrency = Currency.ToUpper().Trim();

                    if (Amount > 0)
                    {
                        BaseConnector.Error error = new BaseConnector.Error();
                        var api = GetConnector(GameID, PlayerID, Currency, BaseConnector.GameEnvironment.STAG);
                        if (api != null)
                        {
                            bool result = api.GameWithdraw(Amount, TrackerId, ref error);

                            if (!result)
                            {
                                LockFromGame = CheckIsLockPlayer(GameID, error.ErrorCode);
                            }

                            return result;
                        }
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
            //}
        }

        public string RetryWithdraw(string Game, string PlayerID, string Currency, decimal Amount, string TrackerId)
        {
            lock (MyLock)
            {
                Common _common = new Common();

                try
                {
                    using (BingoConn db = new BingoConn())
                    {
                        var res = new DepositVM();
                        string GameID = Game.ToUpper().Trim();
                        string UPlayerID = PlayerID.ToUpper().Trim();
                        string UCurrency = Currency.ToUpper().Trim();

                        BaseConnector.Error error = new BaseConnector.Error();
                        var api = GetConnector(GameID, PlayerID, Currency, BaseConnector.GameEnvironment.STAG);
                        if (api != null)
                        {
                            return api.RetryWithdraw(Amount, TrackerId, ref error);
                        }
                        else
                        {
                            return "";
                        }
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
        public HttpResponseMessage GetGameUrl(string Game, string PlayerID, string Currency, decimal Cash, decimal Chip, string TrackerId, string Language = "en", bool IsMobile = true)
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
                _common.InsertLog("GetGameUrl", "TrackerId", TrackerId, "Request", "", "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Cash", Cash.ToString(), "Chip", Chip.ToString(), "Language", Language, "DMinAmount", DMinAmount, "DMaxAmount", DMaxAmount, "WMinAmount", WMinAmount, "WMaxAmount", WMaxAmount, "Ip", GetIPAddress());

                using (BingoConn db = new BingoConn())
                {
                    var res = new GetGameUrlVM();
                    var cres = new CheckAndCreateAccountVM();

                    string GameID = Game.ToUpper().Trim();
                    string UPlayerID = PlayerID.ToUpper().Trim();
                    string UCurrency = Currency.ToUpper().Trim();
                    //insert for chip calculation
                    string UGameId = Game.ToUpper().Trim();


                    string s = "SELECT Id, UserId, GameId FROM LaunchGame WHERE Id = '" + TrackerId + "'";
                    var datatable = _common.Sql_Query(s);
                    if (datatable.Rows.Count == 1)
                    {
                        //Proceed
                    }
                    else
                    {
                        _common.Sql_RunAndCommit("Insert Into TrackerHack Values (NEWID(), '" + TrackerId + "','" + UPlayerID + "','" + GameID + "', CAST(SYSDATETIMEOFFSET() AT TIME ZONE 'Singapore Standard Time' AS DateTime)) ");

                        res.PlayerID = UPlayerID;
                        res.Url = null;
                        res.Game = GameID;
                        res.Message = "Invalid Call.";
                        res.Code = 7;
                        res.IsRefund = true;
                        res.LockFromGame = false;
                        _common.InsertLog("GetGameUrl", "TrackerId", TrackerId, "Response", JsonConvert.SerializeObject(res), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Cash", Cash.ToString(), "Chip", Chip.ToString(), "Language", Language,
                                          "DMinAmount", DMinAmount, "DMaxAmount", DMaxAmount, "WMinAmount", WMinAmount, "WMaxAmount", WMaxAmount);
                        return new HttpResponseMessage()
                        {
                            StatusCode = HttpStatusCode.BadRequest,
                            Content = new StringContent(JsonConvert.SerializeObject(res), System.Text.Encoding.UTF8, "application/json")
                        };
                    }


                    var game = db.Games.FirstOrDefault(x => !x.Deleted && x.Code.ToUpper().Trim() == UGameId);

                    var lrecord = db.LaunchGameTrackers.FirstOrDefault(x => x.TrackerID == TrackerId);
                    if (lrecord == null)
                    {
                        LaunchGameTracker obj = new LaunchGameTracker()
                        {
                            TrackerID = TrackerId,
                            PlayerID = UPlayerID,
                            GameID = GameID,
                            Currency = UCurrency,
                            Cash = Cash,
                            Chip = Chip,
                            Language = Language,
                            CreatedDt = DateTime.Now
                        };
                        db.LaunchGameTrackers.Add(obj);
                        db.SaveChanges();
                    }
                    else
                    {
                        res.PlayerID = UPlayerID;
                        res.Url = null;
                        res.Game = GameID;
                        res.Message = "Duplicate transaction.";
                        res.Code = 7;
                        res.IsRefund = false;
                        res.LockFromGame = false;
                        _common.InsertLog("GetGameUrl", "TrackerId", TrackerId, "Response", JsonConvert.SerializeObject(res), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Cash", Cash.ToString(), "Chip", Chip.ToString(), "Language", Language,
                                                                                                "DMinAmount", DMinAmount, "DMaxAmount", DMaxAmount, "WMinAmount", WMinAmount, "WMaxAmount", WMaxAmount);
                        return new HttpResponseMessage()
                        {
                            StatusCode = HttpStatusCode.OK,
                            Content = new StringContent(JsonConvert.SerializeObject(res), System.Text.Encoding.UTF8, "application/json")
                        };
                    }

                    decimal Amount = 0;
                    if (game.HasChip)
                        Amount = Cash + Chip;
                    else
                        Amount = Cash;

                    var record = db.GameMappings.FirstOrDefault(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID && x.Currency.ToUpper() == UCurrency && x.IsActive == true);
                    if (record == null)
                    {
                        string gamePlayerID = string.Empty;
                        gamePlayerID = FormGamePlayerID(GameID, PlayerID);
                        var api = GetConnector(GameID, PlayerID, Currency, BaseConnector.GameEnvironment.STAG);
                        if (api != null)
                        {
                            cres = api.CreatePlayer(gamePlayerID, "abc456123", gamePlayerID, UPlayerID);
                        }
                        else
                        {
                            res.PlayerID = UPlayerID;
                            res.Game = GameID;
                            res.Message = "Invalid game.";
                            res.Code = 8;
                            res.Url = null;
                            res.Type = GetGameType(GameID);
                            //_common.LogError($"{TrackerId}:GetGameUrl:FAILED - Invalid Game " + string.Format("{0}|{1}|{2}|{3}|{4}|{5}", Game, PlayerID, Currency, Cash.ToString(), Chip.ToString(), Language) + " " + JsonConvert.SerializeObject(res), FilePrefix);
                        }

                        if (cres.Code != 0)
                        {
                            res.PlayerID = UPlayerID;
                            res.Url = null;
                            res.Game = GameID;
                            res.Message = "Failed to create game account.";
                            res.Code = 7;
                            res.IsRefund = true;
                            res.LockFromGame = false;
                            //_common.LogError($"{TrackerId}:GetGameUrl:FAILED - Failed To Create Game Account " + string.Format("{0}|{1}|{2}|{3}|{4}|{5}", Game, PlayerID, Currency, Cash.ToString(), Chip.ToString(), Language), FilePrefix);
                            _common.InsertLog("GetGameUrl", "TrackerId", TrackerId, "Response", JsonConvert.SerializeObject(res), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Cash", Cash.ToString(), "Chip", Chip.ToString(), "Language", Language,
                                                                                                    "DMinAmount", DMinAmount, "DMaxAmount", DMaxAmount, "WMinAmount", WMinAmount, "WMaxAmount", WMaxAmount);
                            msg = $"FAILED: {UPlayerID} failed to create game account for {GameID}.";
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

                    if (Amount < 0)
                    {
                        res.PlayerID = UPlayerID;
                        res.Url = null;
                        res.Game = GameID;
                        res.Message = "Negative amount.";
                        res.Code = 1;
                        res.LockFromGame = true;
                        //_common.LogError($"{TrackerId}:GetGameUrl:FAILED - Negative Amount " + string.Format("{0}|{1}|{2}|{3}|{4}|{5}", Game, PlayerID, Currency, Cash.ToString(), Chip.ToString(), Language), FilePrefix);
                        _common.InsertLog("GetGameUrl", "TrackerId", TrackerId, "Response", JsonConvert.SerializeObject(res), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Cash", Cash.ToString(), "Chip", Chip.ToString(), "Language", Language,
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
                        //_common.LogError($"{TrackerId}:GetGameUrl:SUCCESS - Amount 0, Skip Deposit " + string.Format("{0}|{1}|{2}|{3}|{4}|{5}", Game, PlayerID, Currency, Cash.ToString(), Chip.ToString(), Language), FilePrefix);
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
                            //_common.LogError($"{TrackerId}:GetGameUrl:FAILED - Failed To Get Deposit Transfer Limit " + string.Format("{0}|{1}|{2}|{3}|{4}|{5}", Game, PlayerID, Currency, Cash.ToString(), Chip.ToString(), Language), FilePrefix);
                            _common.InsertLog("GetGameUrl", "TrackerId", TrackerId, "Response", JsonConvert.SerializeObject(res), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Cash", Cash.ToString(), "Chip", Chip.ToString(), "Language", Language,
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
                            res.IsRefund = true;
                            //_common.LogError($"{TrackerId}:GetGameUrl:SUCCESS - Deposit Less Than Min Limit, Skip Deposit " + string.Format("{0}|{1}|{2}|{3}|{4}|{5}", Game, PlayerID, Currency, Cash.ToString(), Chip.ToString(), Language), FilePrefix);
                        }
                        //more than max amount, break into few steps
                        else if (Amount > DMaxAmount)
                        {
                            //_common.LogError($"{TrackerId}:GetGameUrl:SUCCESS - Deposit More Than Max Limit " + string.Format("{0}|{1}|{2}|{3}|{4}|{5}", Game, PlayerID, Currency, Cash.ToString(), Chip.ToString(), Language), FilePrefix);
                            decimal depositAmt = 0;
                            decimal remainAmt = Amount;
                            decimal? initialGameBalance = GetGameBalance(GameID, UPlayerID, UCurrency, TrackerId);
                            decimal amt = 0;
                            int cnt = 1;

                            if (!initialGameBalance.Equals(null))
                            {
                                //_common.LogError($"{TrackerId}:GetGameUrl:SUCCESS - Deposit Initial Balance: {initialGameBalance.ToString()} " + string.Format("{0}|{1}|{2}|{3}|{4}|{5}", Game, PlayerID, Currency, Cash.ToString(), Chip.ToString(), Language), FilePrefix);
                                decimal? failedGameBalance = null;

                                //_common.LogError($"{TrackerId}:GetGameUrl:SUCCESS - Amount Going To Deposit: {remainAmt.ToString()} " + string.Format("{0}|{1}|{2}|{3}|{4}|{5}", Game, PlayerID, Currency, Cash.ToString(), Chip.ToString(), Language), FilePrefix);

                                while (remainAmt != 0)
                                {
                                    amt = remainAmt > DMaxAmount ? (decimal)DMaxAmount : remainAmt;
                                    canGetUrl = GameDeposit(GameID, UPlayerID, UCurrency, amt, TrackerId + "-" + cnt.ToString(), ref LockFromGame);
                                    //_common.LogError($"{TrackerId}:GetGameUrl:" + (canGetUrl == true ? "SUCCESS" : "FAILED") + $" - Current Deposit Amount: {amt.ToString()} " + string.Format("{0}|{1}|{2}|{3}|{4}|{5}", Game, PlayerID, Currency, Cash.ToString(), Chip.ToString(), Language), FilePrefix);
                                    if (canGetUrl == false)
                                    {
                                        failedGameBalance = GetGameBalance(GameID, UPlayerID, UCurrency, TrackerId);
                                        break;
                                    }

                                    remainAmt -= amt;
                                    depositAmt += amt;
                                    cnt++;

                                    //_common.LogError($"{TrackerId}:GetGameUrl:SUCCESS - Remaining Deposit Amount: {remainAmt.ToString()}, Deposited Amount: {depositAmt.ToString()} " + string.Format("{0}|{1}|{2}|{3}|{4}|{5}", Game, PlayerID, Currency, Cash.ToString(), Chip.ToString(), Language), FilePrefix);
                                }

                                if (canGetUrl == false)
                                {
                                    //_common.LogError($"{TrackerId}:GetGameUrl:FAILED - Deposit Failed, Start To Get End Balance " + string.Format("{0}|{1}|{2}|{3}|{4}|{5}", Game, PlayerID, Currency, Cash.ToString(), Chip.ToString(), Language) + " " + JsonConvert.SerializeObject(res), FilePrefix);

                                    if (!failedGameBalance.Equals(null))
                                    {
                                        //_common.LogError($"{TrackerId}:GetGameUrl:SUCCESS - End Balance: {failedGameBalance.ToString()} " + string.Format("{0}|{1}|{2}|{3}|{4}|{5}", Game, PlayerID, Currency, Cash.ToString(), Chip.ToString(), Language), FilePrefix);
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
                                                    //_common.LogError($"{TrackerId}:GetGameUrl:SUCCESS - Start Rollback Deposit " + string.Format("{0}|{1}|{2}|{3}|{4}|{5}", Game, PlayerID, Currency, Cash.ToString(), Chip.ToString(), Language), FilePrefix);

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
                                                        //_common.LogError($"{TrackerId}:GetGameUrl:FAILED - Failed To Get Withdraw Transfer Limit " + string.Format("{0}|{1}|{2}|{3}|{4}|{5}", Game, PlayerID, Currency, Cash.ToString(), Chip.ToString(), Language) + " " + JsonConvert.SerializeObject(res), FilePrefix);
                                                        _common.InsertLog("GetGameUrl", "TrackerId", TrackerId, "Response", JsonConvert.SerializeObject(res), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Cash", Cash.ToString(), "Chip", Chip.ToString(), "Language", Language,
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
                                                    //_common.LogError($"{TrackerId}:GetGameUrl:SUCCESS - Start Rollback Deposit " + string.Format("{0}|{1}|{2}|{3}|{4}|{5}", Game, PlayerID, Currency, Cash.ToString(), Chip.ToString(), Language), FilePrefix);

                                                    GetGameTransferLimit(GameID, "WITHDRAW", UCurrency, TrackerId, ref WMinAmount, ref WMaxAmount);

                                                    if (WMinAmount.Equals(null) || WMaxAmount.Equals(null))
                                                    {
                                                        res.PlayerID = UPlayerID;
                                                        res.Url = null;
                                                        res.Game = GameID;
                                                        res.Message = "Failed to get withdraw transfer limit for deposit rollback.";
                                                        res.Code = 7;
                                                        res.LockFromGame = false;
                                                        //_common.LogError($"{TrackerId}:GetGameUrl:FAILED - Failed To Get Withdraw Transfer Limit " + string.Format("{0}|{1}|{2}|{3}|{4}|{5}", Game, PlayerID, Currency, Cash.ToString(), Chip.ToString(), Language) + " " + JsonConvert.SerializeObject(res), FilePrefix);
                                                        _common.InsertLog("GetGameUrl", "TrackerId", TrackerId, "Response", JsonConvert.SerializeObject(res), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Cash", Cash.ToString(), "Chip", Chip.ToString(), "Language", Language,
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
                                            //_common.LogError($"{TrackerId}:GetGameUrl:SUCCESS - Initial Balance = End Balance, Skip Deposit Rollback " + string.Format("{0}|{1}|{2}|{3}|{4}|{5}", Game, PlayerID, Currency, Cash.ToString(), Chip.ToString(), Language), FilePrefix);
                                            _common.InsertLog("GetGameUrl", "TrackerId", TrackerId, "Response", JsonConvert.SerializeObject(res), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Cash", Cash.ToString(), "Chip", Chip.ToString(), "Language", Language,
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
                                                                  //_common.LogError($"{TrackerId}:GetGameUrl:FAILED - Failed To Get Withdraw Transfer Limit For Deposit Rollback " + string.Format("{0}|{1}|{2}|{3}|{4}|{5}", Game, PlayerID, Currency, Cash.ToString(), Chip.ToString(), Language) + " " + JsonConvert.SerializeObject(res), FilePrefix);
                                        _common.InsertLog("GetGameUrl", "TrackerId", TrackerId, "Response", JsonConvert.SerializeObject(res), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Cash", Cash.ToString(), "Chip", Chip.ToString(), "Language", Language,
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
                                //_common.LogError($"{TrackerId}:GetGameUrl:FAILED - Failed To Get Start Game Balance " + string.Format("{0}|{1}|{2}|{3}|{4}|{5}", Game, PlayerID, Currency, Cash.ToString(), Chip.ToString(), Language) + " " + JsonConvert.SerializeObject(res), FilePrefix);
                                _common.InsertLog("GetGameUrl", "TrackerId", TrackerId, "Response", JsonConvert.SerializeObject(res), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Cash", Cash.ToString(), "Chip", Chip.ToString(), "Language", Language,
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
                            //_common.LogError($"{TrackerId}:GetGameUrl:SUCCESS - Deposit Normal Condition " + string.Format("{0}|{1}|{2}|{3}|{4}|{5}", Game, PlayerID, Currency, Cash.ToString(), Chip.ToString(), Language), FilePrefix);

                            decimal? initialGameBalance = GetGameBalance(GameID, UPlayerID, UCurrency, TrackerId);
                            if (!initialGameBalance.Equals(null))
                            {
                                //_common.LogError($"{TrackerId}:GetGameUrl:SUCCESS - Deposit Initial Balance: {initialGameBalance.ToString()} " + string.Format("{0}|{1}|{2}|{3}|{4}|{5}", Game, PlayerID, Currency, Cash.ToString(), Chip.ToString(), Language), FilePrefix);

                                canGetUrl = GameDeposit(GameID, UPlayerID, UCurrency, Amount, TrackerId, ref LockFromGame);

                                if (canGetUrl == false)
                                {
                                    //_common.LogError($"{TrackerId}:GetGameUrl:FAILED - Deposit Failed, Proceed To Withdraw Rollback " + string.Format("{0}|{1}|{2}|{3}|{4}|{5}", Game, PlayerID, Currency, Cash.ToString(), Chip.ToString(), Language), FilePrefix);

                                    decimal? failedGameBalance = GetGameBalance(GameID, UPlayerID, UCurrency, TrackerId);
                                    if (!failedGameBalance.Equals(null))
                                    {
                                        //_common.LogError($"{TrackerId}:GetGameUrl:SUCCESS - End Balance: {failedGameBalance.ToString()} " + string.Format("{0}|{1}|{2}|{3}|{4}|{5}", Game, PlayerID, Currency, Cash.ToString(), Chip.ToString(), Language), FilePrefix);

                                        if ((decimal)failedGameBalance - initialGameBalance > 0)
                                        {
                                            if ((decimal)failedGameBalance - initialGameBalance >= Amount)
                                            {
                                                //_common.LogError($"{TrackerId}:GetGameUrl:SUCCESS - Start Withdraw Rollback " + string.Format("{0}|{1}|{2}|{3}|{4}|{5}", Game, PlayerID, Currency, Cash.ToString(), Chip.ToString(), Language), FilePrefix);

                                                GetGameTransferLimit(GameID, "WITHDRAW", UCurrency, TrackerId, ref WMinAmount, ref WMaxAmount);

                                                if (WMinAmount.Equals(null) || WMaxAmount.Equals(null))
                                                {
                                                    res.PlayerID = UPlayerID;
                                                    res.Url = null;
                                                    res.Game = GameID;
                                                    res.Message = "Failed to get withdraw transfer limit.";
                                                    res.Code = 7;
                                                    res.LockFromGame = false;
                                                    //_common.LogError($"{TrackerId}:GetGameUrl:FAILED - Failed To Get Withdraw Transfer Limit " + string.Format("{0}|{1}|{2}|{3}|{4}|{5}", Game, PlayerID, Currency, Cash.ToString(), Chip.ToString(), Language) + " " + JsonConvert.SerializeObject(res), FilePrefix);
                                                    _common.InsertLog("GetGameUrl", "TrackerId", TrackerId, "Response", JsonConvert.SerializeObject(res), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Cash", Cash.ToString(), "Chip", Chip.ToString(), "Language", Language,
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
                                            //_common.LogError($"{TrackerId}:GetGameUrl:SUCCESS - Initial Balance = End Balance, Skip Deposit Rollback " + string.Format("{0}|{1}|{2}|{3}|{4}|{5}", Game, PlayerID, Currency, Cash.ToString(), Chip.ToString(), Language), FilePrefix);
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
                                                                  //_common.LogError($"{TrackerId}:GetGameUrl:FAILED - Failed To Get End Game Balance " + string.Format("{0}|{1}|{2}|{3}|{4}|{5}", Game, PlayerID, Currency, Cash.ToString(), Chip.ToString(), Language) + " " + JsonConvert.SerializeObject(res), FilePrefix);
                                        _common.InsertLog("GetGameUrl", "TrackerId", TrackerId, "Response", JsonConvert.SerializeObject(res), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Cash", Cash.ToString(), "Chip", Chip.ToString(), "Language", Language,
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
                                //_common.LogError($"{TrackerId}:GetGameUrl:FAILED - Failed To Get Start Game Balance " + string.Format("{0}|{1}|{2}|{3}|{4}|{5}", Game, PlayerID, Currency, Cash.ToString(), Chip.ToString(), Language) + " " + JsonConvert.SerializeObject(res), FilePrefix);
                                _common.InsertLog("GetGameUrl", "TrackerId", TrackerId, "Response", JsonConvert.SerializeObject(res), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Cash", Cash.ToString(), "Chip", Chip.ToString(), "Language", Language,
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
                        GameDeposit rec = new GameDeposit()
                        {
                            TrackerID = TrackerId,
                            PlayerID = PlayerID,
                            GameID = GameID,
                            Currency = UCurrency,
                            Cash = Cash,
                            Chip = Chip,
                            CreatedDt = DateTime.Now
                        };
                        db.GameDeposits.Add(rec);
                        db.SaveChanges();

                        var api = GetConnector(GameID, PlayerID, Currency, BaseConnector.GameEnvironment.STAG);
                        if (api != null)
                        {
                            res = api.GetGameUrl(Language);
                            if (DMinAmount > Amount)
                                res.IsRefund = true;
                        }
                        else
                        {
                            res.PlayerID = UPlayerID;
                            res.Game = GameID;
                            res.Message = "Invalid game.";
                            res.Code = 8;
                            res.Url = null;
                            res.Type = GetGameType(GameID);
                        }

                        if (res.Code != 0)
                        {
                            //_common.LogError($"{TrackerId}:GetGameUrl:FAILED - Failed To Get Launch Game Url " + string.Format("{0}|{1}|{2}|{3}|{4}|{5}", Game, PlayerID, Currency, Cash.ToString(), Chip.ToString(), Language) + " " + JsonConvert.SerializeObject(res), FilePrefix);
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
                        //_common.LogError($"{TrackerId}:GetGameUrl:FAILED - Game Deposit Failed " + string.Format("{0}|{1}|{2}|{3}|{4}|{5}", Game, PlayerID, Currency, Cash.ToString(), Chip.ToString(), Language), FilePrefix);
                        msg = $"FAILED: {UPlayerID} deposit {Amount.ToString()} to {GameID} failed.";
                        foreach (var phone in apiPhoneList.Split(','))
                        {
                            _common.SendSmsToBulkSms(phone, msg);
                        }
                    }

                    _common.InsertLog("GetGameUrl", "TrackerId", TrackerId, "Response", JsonConvert.SerializeObject(res), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Cash", Cash.ToString(), "Chip", Chip.ToString(), "Language", Language, "DMinAmount", DMinAmount, "DMaxAmount", DMaxAmount, "WMinAmount", WMinAmount, "WMaxAmount", WMaxAmount);
                    if (game.HasChip)
                        InsertGameTransaction(UGameId, TrackerId, UPlayerID, Cash, Chip);
                    if (UGameId == "KISS918")
                        InsertKISScoreLog(TrackerId, UPlayerID);

                    return new HttpResponseMessage()
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(JsonConvert.SerializeObject(res), System.Text.Encoding.UTF8, "application/json")
                    };
                }
            }
            catch (Exception ex)
            {
                _common.InsertLog("GetGameUrl", "TrackerId", TrackerId, "Exception", ex.Message.ToString(), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Cash", Cash.ToString(), "Chip", Chip.ToString(), "Language", Language, "DMinAmount", DMinAmount, "DMaxAmount", DMaxAmount, "WMinAmount", WMinAmount, "WMaxAmount", WMaxAmount);
                dynamic res = new ExpandoObject();
                res.Code = -9999;
                res.Message = ex.InnerException.Message.ToString();
                return new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent(JsonConvert.SerializeObject(res), System.Text.Encoding.UTF8, "application/json")
                };
            }
                                                                                                                                                                                       }

        public void InsertGameTransaction(string GameId, string TrackerId, string PlayerID, decimal Cash, decimal Chip)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["BingoConn1"].ConnectionString))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("sp_GameTransaction_Insert", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@TransId", TrackerId));
                    cmd.Parameters.Add(new SqlParameter("@Game", GameId));
                    cmd.Parameters.Add(new SqlParameter("@PlayerID", PlayerID));
                    cmd.Parameters.Add(new SqlParameter("@Cash", Cash));
                    cmd.Parameters.Add(new SqlParameter("@Chip", Chip));
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {

            }
        }

        public void InsertKISScoreLog(string TrackerId, string PlayerID)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["BingoConn1"].ConnectionString))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("sp_KISScoreLog_Insert", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@TransId", TrackerId));
                    cmd.Parameters.Add(new SqlParameter("@PlayerID", PlayerID));
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {

            }
        }

        public void SplitWithdrawCashAndChip(string Game, string PlayerID, string Currency, decimal Amount, string TrackerId, ref decimal Cash, ref decimal Chip)
        {
            //lock (MyLock)
            //{
            Common _common = new Common();

            try
            {
                _common.InsertLog("SplitWithdrawCashAndChip", "TrackerId", TrackerId, "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Amount", Amount.ToString(), "Cash", Cash, "Chip", Chip);

                using (BingoConn db = new BingoConn())
                {
                    string GameID = Game.ToUpper().Trim();
                    string UPlayerID = PlayerID.ToUpper().Trim();
                    string UCurrency = Currency.ToUpper().Trim();
                    int TimeoutSec = 180000;

                    //28win
                    if (GameID == "BT28WIN")
                    {
                        var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
                                                                                                                                                                 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

                        if (record != null)
                        {
                            GeneralSplitOutCashChip(GameID, UPlayerID, Amount, ref Cash, ref Chip);
                        }
                        else
                        {
                            Cash = Amount;
                            Chip = 0;
                        }
                    }
                    //lkl365
                    else if (GameID == "LKL365")
                    {
                        var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
                                                                                                                                                                 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

                        if (record != null)
                        {
                            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["BingoConn1"].ConnectionString))
                            {
                                con.Open();
                                SqlCommand cmd = new SqlCommand("sp_GameTransaction_Get", con);
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.Add(new SqlParameter("@PlayerID", PlayerID));
                                cmd.Parameters.Add(new SqlParameter("@Game", GameID));

                                using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                                {
                                    DataTable table = new DataTable();
                                    adapter.Fill(table);

                                    if (table.Rows.Count > 0)
                                    {
                                        string from = Convert.ToDateTime(table.Rows[0]["CreatedDate"]).ToString("yyyy-MM-dd HH:mm:ss");
                                        string to = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                                        var apiurl = string.Format(HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.PathAndQuery, "/") + "LKL365/LKL365Job?From={0}&To={1}", from, to);
                                        var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
                                        httpWebRequest.ContentType = "application/json";
                                        httpWebRequest.Method = "GET";
                                        httpWebRequest.Timeout = TimeoutSec;
                                        httpWebRequest.ReadWriteTimeout = TimeoutSec;
                                        var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                                        using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                                        {
                                            _common.InsertLog("SplitWithdrawCashAndChip", "TrackerId", TrackerId, "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Amount", Amount.ToString(), "Cash", Cash, "Chip", Chip, "From", from, "To", to);
                                            decimal TotalBet = GetBetSummaryForChip(from, to, GameID, PlayerID);

                                            //if (result.status == 1)
                                            //{
                                            if (TotalBet >= Convert.ToDecimal(table.Rows[0]["Chip"]))
                                            {
                                                Chip = 0;
                                                Cash = Amount;
                                            }
                                            else
                                            {
                                                Chip = Convert.ToDecimal(table.Rows[0]["Chip"]) - TotalBet;
                                                Cash = Amount - Chip;
                                            }

                                            //}
                                            //else
                                            //{
                                            //	res.PlayerID = UPlayerID;
                                            //	res.Game = GameID;
                                            //	res.Cash = Amount;
                                            //	res.Chip = 0;
                                            //	res.Message = result.message;
                                            //	res.Code = 2;
                                            //}
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
                            Cash = Amount;
                            Chip = 0;
                        }
                    }
                    //LionKing
                    else if (GameID == "LIONKING")
                    {
                        var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
                                                                                                                                                                 x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

                        if (record != null)
                        {
                            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["BingoConn1"].ConnectionString))
                            {
                                con.Open();
                                SqlCommand cmd = new SqlCommand("sp_GameTransaction_Get", con);
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.Add(new SqlParameter("@PlayerID", PlayerID));
                                cmd.Parameters.Add(new SqlParameter("@Game", GameID));

                                using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                                {
                                    DataTable table = new DataTable();
                                    adapter.Fill(table);

                                    if (table.Rows.Count > 0)
                                    {
                                        string from = Convert.ToDateTime(table.Rows[0]["CreatedDate"]).ToString("yyyy-MM-dd HH:mm:ss");
                                        string to = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                                        var apiurl = string.Format(HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.PathAndQuery, "/") + "LionKing/LionKingJob?From={0}&To={1}", from, to);
                                        var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiurl);
                                        httpWebRequest.ContentType = "application/json";
                                        httpWebRequest.Method = "GET";
                                        httpWebRequest.Timeout = TimeoutSec;
                                        httpWebRequest.ReadWriteTimeout = TimeoutSec;
                                        var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                                        using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                                        {
                                            _common.InsertLog("SplitWithdrawCashAndChip", "TrackerId", TrackerId, "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Amount", Amount.ToString(), "Cash", Cash, "Chip", Chip, "From", from, "To", to);
                                            decimal TotalBet = GetBetSummaryForChip(from, to, GameID, PlayerID);

                                            //if (result.status == 1)
                                            //{
                                            if (TotalBet >= Convert.ToDecimal(table.Rows[0]["Chip"]))
                                            {
                                                Chip = 0;
                                                Cash = Amount;
                                            }
                                            else
                                            {
                                                Chip = Convert.ToDecimal(table.Rows[0]["Chip"]) - TotalBet;
                                                Cash = Amount - Chip;
                                            }

                                            //}
                                            //else
                                            //{
                                            //	res.PlayerID = UPlayerID;
                                            //	res.Game = GameID;
                                            //	res.Cash = Amount;
                                            //	res.Chip = 0;
                                            //	res.Message = result.message;
                                            //	res.Code = 2;
                                            //}
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
                            Cash = Amount;
                            Chip = 0;
                        }
                    }
                    //default
                    else
                    {
                        Cash = Amount;
                        Chip = 0;
                    }

                    _common.InsertLog("SplitWithdrawCashAndChip", "TrackerId", TrackerId, "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Amount", Amount.ToString(), "Cash", Cash, "Chip", Chip);

                }
            }
            catch (Exception ex)
            {
                Cash = Amount;
                Chip = 0;
                _common.LogError($"{TrackerId}:SplitWithdrawCashAndChip:EXCEPTION " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Currency, Amount.ToString()) + " " + ex.Message.ToString(), FilePrefix);
                _common.InsertLog("SplitWithdrawCashAndChip", "TrackerId", TrackerId, "Exception", ex.Message.ToString(), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Amount", Amount.ToString());

            }
            //}
        }

        public decimal GetBetSummaryForChip(string From, string To, string Game, string PlayerID)
        {
            Common _common = new Common();
            try
            {
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["BingoConn1"].ConnectionString))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("sp_GameTransaction_GetSummary", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@Game", Game));
                    cmd.Parameters.Add(new SqlParameter("@PlayerID", PlayerID));
                    cmd.Parameters.Add(new SqlParameter("@StartDt", From));
                    cmd.Parameters.Add(new SqlParameter("@EndDt", To));
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        DataTable table = new DataTable();
                        adapter.Fill(table);
                        _common.InsertLog("GetBetSummaryForChip", "From", From, "To", To, "PlayerID", PlayerID, "Game", Game, "Dt", JsonConvert.SerializeObject(table));

                        if (table.Rows.Count > 0)
                        {
                            return Convert.ToDecimal(table.Rows[0]["Turnover"]);
                        }

                        return 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _common.LogError($"{Game}:GetBetSummaryForChip:EXCEPTION " + string.Format("{0}|{1}|{2}|{3}", From, To, Game, PlayerID) + " " + ex.Message.ToString(), FilePrefix);
                return 0;
            }
        }

        [HttpGet]
        public HttpResponseMessage GetGameUrlSeamless(string Game, string PlayerID, string Currency, decimal Cash, decimal Chip, string TrackerId, string Language = "en", bool IsMobile = true)
        {
            Common _common = new Common();

            try
            {
                _common.InsertLog("GetGameUrlSeamless", "TrackerId", TrackerId, "Request", "", "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Cash", Cash.ToString(), "Chip", Chip.ToString(), "Language", Language, "Ip", GetIPAddress());

                using (BingoConn db = new BingoConn())
                {
                    var res = new GetGameUrlVM();
                    string GameID = Game.ToUpper().Trim();
                    string UPlayerID = PlayerID.ToUpper().Trim();
                    string UCurrency = Currency.ToUpper().Trim();
                    string UGameId = Game.ToUpper().Trim();
                    var lrecord = db.LaunchGameTrackers.FirstOrDefault(x => x.TrackerID == TrackerId);
                    if (lrecord == null)
                    {
                        LaunchGameTracker obj = new LaunchGameTracker()
                        {
                            TrackerID = TrackerId,
                            PlayerID = UPlayerID,
                            GameID = GameID,
                            Currency = UCurrency,
                            Cash = Cash,
                            Chip = Chip,
                            Language = Language,
                            CreatedDt = DateTime.Now
                        };
                        db.LaunchGameTrackers.Add(obj);
                        db.SaveChanges();
                    }
                    else
                    {
                        res.PlayerID = UPlayerID;
                        res.Url = null;
                        res.Game = GameID;
                        res.Message = "Duplicate transaction.";
                        res.Code = 7;
                        res.IsRefund = false;
                        res.LockFromGame = false;
                        _common.InsertLog("GetGameUrlSeamless", "TrackerId", TrackerId, "Response", JsonConvert.SerializeObject(res), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Cash", Cash.ToString(), "Chip", Chip.ToString(), "Language", Language);
                        return new HttpResponseMessage()
                        {
                            StatusCode = HttpStatusCode.OK,
                            Content = new StringContent(JsonConvert.SerializeObject(res), System.Text.Encoding.UTF8, "application/json")
                        };
                    }

                    string parameter = Game + PlayerID + Currency + Cash.ToString() + Chip.ToString() + TrackerId + Language;
                    string token = TokenGenerator.GetMD5Signature(parameter);
                    Models.SeamlessWallet foundRecord = null;
                    //Special handling for VIVO series game
                    if (Game == "VIVO" || Game == "ARROWEDGE" || Game == "ROCKETMAN")
                    {
                        //Exception: Sequence contains more than one element
                        //SingleOrDefault will only succeed when the collections contains exactly 0 or 1 element.
                        foundRecord = db.SeamlessWallets.SingleOrDefault(x => x.Username == PlayerID);

                        if (foundRecord != null)
                        {
                            if (foundRecord.Game.Equals(Game) != true)
                            {
                                res.PlayerID = UPlayerID;
                                res.Url = null;
                                res.Game = GameID;
                                res.Message = "Failed to create game account due to duplicate user ID.";
                                res.Code = 7;
                                res.IsRefund = true;
                                res.LockFromGame = false;
                                _common.InsertLog("GetGameUrlSeamless", "TrackerId", TrackerId, "Response", JsonConvert.SerializeObject(res), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Cash", Cash.ToString(), "Chip", Chip.ToString(), "Language", Language);
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
                        foundRecord = db.SeamlessWallets.SingleOrDefault(x => x.Game == Game && x.Username == PlayerID);
                    }

                    if (foundRecord == null)
                    {
                        var seamlessWalletModel = new Models.SeamlessWallet()
                        {
                            Cash = Cash,
                            Chip = Chip,
                            Game = Game,
                            Username = PlayerID,
                            TrackerId = TrackerId,
                            Currency = Currency,
                            Token = token
                        };

                        db.SeamlessWallets.Add(seamlessWalletModel);
                        db.SaveChanges();
                    }
                    else
                    {
                        foundRecord.Cash = foundRecord.Cash + Cash;
                        foundRecord.Chip = foundRecord.Chip + Chip;
                        foundRecord.Currency = Currency;
                        foundRecord.TrackerId = TrackerId;
                        foundRecord.Token = token;
                        db.SaveChanges();
                    }

                    var ip = GetIPAddress();
                    if (Game == "VIVO")
                    {
                        var api = new VivoConnector(PlayerID, Currency, "", BaseConnector.GameEnvironment.STAG);
                        if (api != null)
                        {
                            res = api.GetGameSeamlessUrl(Language, token);
                        }
                    }
                    else if (Game == "ROCKETMAN")
                    {
                        var api = new RocketmanConnector(PlayerID, Currency, "", BaseConnector.GameEnvironment.STAG);
                        if (api != null)
                        {
                            res = api.GetGameSeamlessUrl(Language, token);
                        }
                    }
                    else if (Game == "ARROWEDGE")
                    {
                        var api = new ArrowEdgeConnector(PlayerID, Currency, ip, BaseConnector.GameEnvironment.PROD);
                        if (api != null)
                        {
                            res = api.GetGameSeamlessUrl(Language, token, IsMobile);
                        }
                    }
                    else if (Game == "PPSLOT")
                    {
                        var api = new PPSlotConnector(PlayerID, Currency, ip, BaseConnector.GameEnvironment.PROD);
                        if (api != null)
                        {
                            res = api.GetGameSeamlessUrl(Language, token);
                        }
                    }
                    else if (Game == "PPLIVE")
                    {
                        var api = new PPLiveConnector(PlayerID, Currency, ip, BaseConnector.GameEnvironment.PROD);
                        if (api != null)
                        {
                            res = api.GetGameSeamlessUrl(Language, token);
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
                //_common.LogError($"{TrackerId}:GetGameUrl:EXCEPTION " + string.Format("{0}|{1}|{2}|{3}|{4}|{5}", Game, PlayerID, Currency, Cash.ToString(), Chip.ToString(), Language) + " " + ex.Message.ToString(), FilePrefix);
                _common.InsertLog("GetGameUrlSeamless", "TrackerId", TrackerId, "Exception", ex.Message.ToString(), "Game", Game, "UserID", PlayerID, "Currency", Currency, "Cash", Cash.ToString(), "Chip", Chip.ToString(), "Language", Language);
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

        [HttpGet]
        public HttpResponseMessage QuitGameUrlSeamless(string Game, string PlayerID, string Currency, string TrackerId)
        {
            Common _common = new Common();
            string Language = "en";

            try
            {
                using (BingoConn db = new BingoConn())
                {
                    var res = new WithdrawVM();
                    string GameID = Game.ToUpper().Trim();
                    string UPlayerID = PlayerID.ToUpper().Trim();
                    string UCurrency = Currency.ToUpper().Trim();
                    var qrecord = db.QuitGameTrackers.Where(x => x.TrackerID == TrackerId).FirstOrDefault();
                    if (qrecord == null)
                    {
                        QuitGameTracker obj = new QuitGameTracker()
                        {
                            TrackerID = TrackerId,
                            PlayerID = UPlayerID,
                            GameID = GameID,
                            Currency = UCurrency,
                            CreatedDt = DateTime.Now
                        };
                        db.QuitGameTrackers.Add(obj);
                        db.SaveChanges();
                    }
                    else
                    {
                        res.PlayerID = UPlayerID;
                        res.Game = GameID;
                        res.Message = "Duplicate transaction.";
                        res.Code = 5;
                        res.LockFromGame = false;
                        _common.InsertLog("Withdraw", "TrackerId", TrackerId, "Response", JsonConvert.SerializeObject(res), "Game", Game, "PlayerID", PlayerID, "Currency", Currency);
                        return new HttpResponseMessage()
                        {
                            StatusCode = HttpStatusCode.OK,
                            Content = new StringContent(JsonConvert.SerializeObject(res), System.Text.Encoding.UTF8, "application/json")
                        };
                    }

                    var foundRecord = db.SeamlessWallets.SingleOrDefault(
                            x => x.Game == Game && x.Username == PlayerID && x.Currency == Currency);

                    if (foundRecord != null)
                    {
                        res.Cash = Convert.ToDecimal(foundRecord.Cash);
                        res.Chip = Convert.ToDecimal(foundRecord.Chip);
                        res.Game = foundRecord.Game;
                        res.PlayerID = foundRecord.Username;
                        res.Message = "Successfully quit game and withdraw balance.";
                        res.Code = 0;
                        foundRecord.Cash = 0;
                        foundRecord.Token = null;
                        foundRecord.Chip = 0;
                        db.SaveChanges();
                    }

                    else
                    {
                        res.PlayerID = foundRecord.Username;
                        res.Game = Game;
                        res.Message = "Withdraw failed, user not found.";
                        res.Code = 3;
                        res.Chip = 0;
                        res.Cash = 0;
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
                _common.LogError($"{TrackerId}:QuitGameUrl:EXCEPTION " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Currency, Language) + " " + ex.Message.ToString(), FilePrefix);
                _common.InsertLog("QuitGameUrlSeamless", "TrackerId", TrackerId, "Exception", ex.Message.ToString(), "Game", Game, "UserID", PlayerID, "Currency", Currency, "Language", Language);
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

        [HttpGet]
        public HttpResponseMessage GetGameRecord(string Game, string PlayerID, DateTime StartDt, DateTime EndDt, int PageNumber, int PageSize)
        {
            //lock (MyLock)
            //{
            Common _common = new Common();
            try
            {
                using (BingoConn db = new BingoConn())
                {
                    GetGameRecordVM res = new GetGameRecordVM();
                    string GameID = string.Empty;
                    if (!string.IsNullOrWhiteSpace(Game))
                        GameID = Game.ToUpper().Trim();
                    string UPlayerID = PlayerID.ToUpper().Trim();

                    using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["BingoConn1"].ConnectionString))
                    {
                        SqlCommand cmd = new SqlCommand("sp_PlayerGameRecordAPI", con);
                        cmd.CommandTimeout = 0;
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
            //}
        }

        /// <summary>
        /// Specific URL to refresh the token when calling the new game
        /// Now only use for KA
        /// </summary>
        /// <param name="Game"></param>
        /// <param name="PlayerID"></param>
        /// <param name="StartDt"></param>
        /// <param name="EndDt"></param>
        /// <param name="PageNumber"></param>
        /// <param name="PageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public HttpResponseMessage RefreshGameToken(string Game, string PlayerID, string Currency, string TrackerId, string Language = "en", bool IsMobile = true)
        {
            //lock (MyLock)
            //{
            string msg = string.Empty;
            string UPlayerID = PlayerID.ToUpper().Trim();
            string UCurrency = Currency.ToUpper().Trim();
            Common _common = new Common();
            try
            {
                using (BingoConn db = new BingoConn())
                {
                    _common.InsertLog("RefreshGameToken", "TrackerId", TrackerId, "Request", "", "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Language", Language, "Ip", GetIPAddress());
                    var res = new GetGameUrlVM();
                    string GameID = Game.ToUpper().Trim();
                    var api = GetConnector(GameID, PlayerID, Currency, BaseConnector.GameEnvironment.STAG);
                    //Just temporary fixed bnut remove it in future
                    if (Game == "PPSLOT")
                    {

                        var ip = GetIPAddress();
                        var apiSeamless = new PPSlotConnector(PlayerID, Currency, ip, BaseConnector.GameEnvironment.PROD);
                        string parameter = PlayerID + Currency + TrackerId + Language;
                        string token = TokenGenerator.GetMD5Signature(parameter);
                        res = apiSeamless.GetGameSeamlessUrl(Language, token);

                    }
                    else
                    {
                        if (api != null)
                        {
                            res = api.GetGameUrl(Language);
                        }
                        else
                        {
                            res.PlayerID = UPlayerID;
                            res.Game = GameID;
                            res.Message = "Invalid game.";
                            res.Code = 8;
                            res.Url = null;
                            res.Type = GetGameType(GameID);
                            _common.LogError($"{TrackerId}:RefreshGameUrl:FAILED - Invalid Game " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Currency, Language) + " " + JsonConvert.SerializeObject(res), FilePrefix);
                        }
                    }

                    if (res.Code != 0)
                    {
                        _common.LogError($"{TrackerId}:RefreshGameToken:FAILED - Failed To Refresh {Game} Token " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Currency, Language) + " " + JsonConvert.SerializeObject(res), FilePrefix);
                        msg = $"FAILED: {UPlayerID} refresh {GameID} token failed, balance remain in game.";
                        res.LockFromGame = false;
                        //res.IsRefund = false;
                        foreach (var phone in apiPhoneList.Split(','))
                        {
                            _common.SendSmsToBulkSms(phone, msg);
                        }
                        //RollbackDeposit(Amount, (decimal)WMaxAmount, PlayerID, Game, Currency, Language, TrackerId, ref res, ref LockFromGame);
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
                _common.LogError("RefreshGame " + DateTime.Now.ToString() + " " + ex.Message.ToString(), FilePrefix);
                dynamic res = new ExpandoObject();
                res.Code = -9999;
                res.Message = ex.Message.ToString();
                return new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent(JsonConvert.SerializeObject(res), System.Text.Encoding.UTF8, "application/json")
                };
            }
            //}
        }

        /// <summary>
        /// Kick player out from the game
        /// </summary>
        /// <param name="Game"></param>
        /// <param name="PlayerID"></param>
        /// <returns></returns>
        [HttpGet]
        public HttpResponseMessage KickPlayer(string Game, string Currency, string TrackerId, string PlayerID, string password = "")
        {

            lock (MyLock)
            {

                string msg = string.Empty;
                decimal? WMinAmount = null;
                decimal? WMaxAmount = null;
                bool? LockFromGame = false;
                Common _common = new Common();
                try
                {
                    using (BingoConn db = new BingoConn())
                    {
                        var res = new GetKickPlayerUrlVM();
                        string GameID = Game.ToUpper().Trim();
                        string UPlayerID = PlayerID.ToUpper().Trim();
                        string UCurrency = Currency.ToUpper().Trim();
                        decimal WithdrawCash = 0;
                        decimal WithdrawChip = 0;

                        BaseConnector.Error error = new BaseConnector.Error();
                        var api = GetConnector(GameID, PlayerID, Currency, BaseConnector.GameEnvironment.STAG);
                        if (api != null)
                        {
                            bool result = api.KickPlayer(PlayerID, ref error);
                            if (result)
                            {
                                res.Game = Game;
                                res.PlayerID = PlayerID;
                                res.Message = $"Kick Player {PlayerID} Successful";
                                var game = db.Games.Where(x => !x.Deleted && x.Code.ToUpper().Trim() == GameID).FirstOrDefault();
                                decimal? gAmount = GetGameBalance(GameID, UPlayerID, UCurrency, TrackerId);
                                if (gAmount.Equals(null))
                                {
                                    res.PlayerID = UPlayerID;
                                    res.Game = GameID;
                                    res.Message = "Failed to get withdraw game balance.";
                                    res.Code = 5;
                                    res.LockFromGame = false;
                                    _common.LogError($"{TrackerId}:KickPlayer:FAILED - Failed To Get Withdraw Game Balance " + string.Format("{0}|{1}|{2}", Game, PlayerID, Currency), FilePrefix);
                                    _common.InsertLog("KickPlayer", "TrackerId", TrackerId, "Response", JsonConvert.SerializeObject(res), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "WMinAmount", WMinAmount, "WMaxAmount", WMaxAmount);
                                    return new HttpResponseMessage()
                                    {
                                        StatusCode = HttpStatusCode.OK,
                                        Content = new StringContent(JsonConvert.SerializeObject(res), System.Text.Encoding.UTF8, "application/json")
                                    };
                                }

                                decimal Amount = Convert.ToDecimal(gAmount);

                                if (Amount >= 0)
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
                                        _common.LogError($"{TrackerId}:KickPlayer:FAILED - Failed To Get Withdraw Transfer Limit " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Currency, Amount.ToString()), FilePrefix);
                                        _common.InsertLog("KickPlayer", "TrackerId", TrackerId, "Response", JsonConvert.SerializeObject(res), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Amount", Amount.ToString(), "WMinAmount", WMinAmount, "WMaxAmount", WMaxAmount);
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
                                        res.Cash = 0;
                                        res.Chip = 0;
                                        _common.LogError($"{TrackerId}:KickPlayer:SUCCESS - Withdraw Less Than Min Limit, Skip Withdraw " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Currency, Amount.ToString()), FilePrefix);
                                    }
                                    //more than max amount, break into few steps
                                    else if (Amount > WMaxAmount)
                                    {
                                        _common.LogError($"{TrackerId}:KickPlayer:SUCCESS - Withdraw More Than Max Limit, Amount To Be Withdraw: {Amount.ToString()}" + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Currency, Amount.ToString()), FilePrefix);
                                        decimal withdrawAmt = 0;
                                        decimal remainAmt = Convert.ToDecimal(Amount);
                                        decimal amt = 0;
                                        decimal? failedGameBalance = null;
                                        int cnt = 1;

                                        while (remainAmt != 0)
                                        {
                                            amt = remainAmt > WMaxAmount ? (decimal)WMaxAmount : remainAmt;
                                            bool isSuccess = GameWithdraw(GameID, UPlayerID, UCurrency, (decimal)amt, TrackerId + "-" + cnt.ToString(), ref LockFromGame);
                                            _common.LogError($"{TrackerId}:KickPlayer:" + (isSuccess == true ? "SUCCESS" : "FAILED") + $" - Current Withdraw Amount: {amt.ToString()} " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Currency, Amount.ToString()), FilePrefix);
                                            if (isSuccess == false)
                                            {
                                                failedGameBalance = GetGameBalance(GameID, UPlayerID, UCurrency, TrackerId);
                                                break;
                                            }

                                            remainAmt -= amt;
                                            withdrawAmt += amt;
                                            cnt++;

                                            _common.LogError($"{TrackerId}:KickPlayer:SUCCESS - Remaining Amount: {remainAmt.ToString()}, Already Withdraw Amount: {withdrawAmt.ToString()} " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Currency, Amount.ToString()), FilePrefix);
                                        }

                                        if (!failedGameBalance.Equals(null))
                                        {
                                            _common.LogError($"{TrackerId}:KickPlayer:FAILED - End Balance: {failedGameBalance.ToString()} " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Currency, Amount.ToString()), FilePrefix);
                                            res.PlayerID = UPlayerID;
                                            res.Game = GameID;
                                            res.Message = "KickPlayer failed, please contact customer service.";
                                            res.Code = 3;
                                            //res.Amount = Amount;
                                            res.Cash = Amount;
                                            res.Chip = 0;
                                            res.LockFromGame = false; //no need lock first since failed response return will not credit back money
                                            _common.LogError($"{TrackerId}:KickPlayer:FAILED - Withdraw Failed, Return To App " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Currency, Amount.ToString()), FilePrefix);
                                            msg = $"KickPlayer:FAILED - {UPlayerID} failed to withdraw {Amount.ToString()} from {GameID}.";
                                            foreach (var phone in apiPhoneList.Split(','))
                                            {
                                                _common.SendSmsToBulkSms(phone, msg);
                                            }
                                        }
                                        else
                                        {
                                            //split cash and chip
                                            if (game.HasChip)
                                            {
                                                SplitWithdrawCashAndChip(GameID, UPlayerID, UCurrency, Amount, TrackerId, ref WithdrawCash, ref WithdrawChip);
                                            }
                                            else
                                            {
                                                WithdrawCash = Amount;
                                                WithdrawChip = 0;
                                            }
                                            _common.LogError($"{TrackerId}:Withdraw:SUCCESS - Total Amount: {Amount.ToString()} " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Currency, Amount.ToString()), FilePrefix);
                                            res.PlayerID = UPlayerID;
                                            res.Game = GameID;
                                            res.Message = "Successfully withdraw.";
                                            res.Code = 0;
                                            //res.Amount = Amount;
                                            res.Cash = WithdrawCash;
                                            res.Chip = WithdrawChip;
                                        }
                                    }
                                    //normal condition
                                    else
                                    {
                                        _common.LogError($"{TrackerId}:KickPlayer:SUCCESS - Withdraw Normal Condition " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Currency, Amount.ToString()), FilePrefix);
                                        bool isSuccess = GameWithdraw(GameID, UPlayerID, UCurrency, Amount, TrackerId, ref LockFromGame);
                                        if (isSuccess == false)
                                        {
                                            res.PlayerID = UPlayerID;
                                            res.Game = GameID;
                                            res.Message = "Withdraw failed, please contact customer service.";
                                            res.Code = 3;
                                            //res.Amount = Amount;
                                            res.Cash = Amount;
                                            res.Chip = 0;
                                            res.LockFromGame = false; //no need lock first since failed response return will not credit back money
                                            _common.LogError($"{TrackerId}:KickPlayer:FAILED - Withdraw Failed, Return To App " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Currency, Amount.ToString()), FilePrefix);
                                            msg = $"KickPlayer:FAILED - {UPlayerID} failed to withdraw {Amount.ToString()} from {GameID}.";
                                            foreach (var phone in apiPhoneList.Split(','))
                                            {
                                                _common.SendSmsToBulkSms(phone, msg);
                                            }
                                        }
                                        else
                                        {
                                            //split cash and chip
                                            if (game.HasChip)
                                            {
                                                SplitWithdrawCashAndChip(GameID, UPlayerID, UCurrency, Amount, TrackerId, ref WithdrawCash, ref WithdrawChip);
                                            }
                                            else
                                            {
                                                WithdrawCash = Amount;
                                                WithdrawChip = 0;
                                            }
                                            res.PlayerID = UPlayerID;
                                            res.Game = GameID;
                                            res.Message = "Successfully withdraw and kick out player.";
                                            res.Code = 0;
                                            //res.Amount = Amount;
                                            res.Cash = WithdrawCash;
                                            res.Chip = WithdrawChip;
                                            _common.LogError($"{TrackerId}:KickPlayer:SUCCESS - Total Amount: {Amount.ToString()} " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Currency, Amount.ToString()), FilePrefix);
                                        }
                                    }
                                }
                                else
                                {
                                    res.PlayerID = UPlayerID;
                                    res.Game = GameID;
                                    res.Message = "Player has negative balance.";
                                    res.Code = 4;
                                    //res.Amount = Amount;
                                    res.Cash = Amount;
                                    res.Chip = 0;
                                    res.LockFromGame = true;
                                    _common.LogError($"{TrackerId}:KickPlayer:FAILED - Player Has Negative Balance " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Currency, Amount.ToString()) + " " + JsonConvert.SerializeObject(res), FilePrefix);
                                    msg = $"KickPlayer:FAILED - {UPlayerID} has negative balance for {GameID}.";
                                    foreach (var phone in apiPhoneList.Split(','))
                                    {
                                        _common.SendSmsToBulkSms(phone, msg);
                                    }
                                }

                                _common.InsertLog("KickPlayer", "TrackerId", TrackerId, "Response", JsonConvert.SerializeObject(res), "Game", Game, "PlayerID", PlayerID, "Currency", Currency, "Amount", Amount.ToString(), "WMinAmount", WMinAmount, "WMaxAmount", WMaxAmount);
                            }
                            else
                            {
                                res.Game = Game;
                                res.PlayerID = PlayerID;
                                res.Code = 10;
                                res.Message = $"Kick Player {PlayerID} Failed";
                            }
                        }
                        else
                        {
                            res.Game = Game;
                            res.PlayerID = PlayerID;
                            res.Code = 10;
                            res.Message = $"Invalid Game";
                            _common.LogError($"{TrackerId}:KickPlayer:FAILED - Invalid Game " + string.Format("{0}|{1}|{2}", Game, PlayerID, Currency) + " " + JsonConvert.SerializeObject(res), FilePrefix);
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
                    _common.LogError($"{TrackerId}:KickPlayer:EXCEPTION " + string.Format("{0}|{1}|{2}|{3}", Game, PlayerID, Currency, TrackerId) + " " + ex.Message.ToString(), FilePrefix);
                    _common.InsertLog("KickPlayer", "TrackerId", TrackerId, "Exception", ex.Message.ToString(), "Game", Game, "PlayerID", PlayerID);
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

                    using (BingoConn db = new BingoConn())
                    {
                        var res = new Get4DPackageVM();
                        //string GameID = "BT28WIN";
                        //string UPlayerID = PlayerID.ToUpper().Trim();
                        //string UCurrency = Currency.ToUpper().Trim();

                        //28win
                        var api = new BT28WINConnector(PlayerID, Currency, Environment: BaseConnector.GameEnvironment.STAG);
                        res = api.Get4DPackage();

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

                    using (BingoConn db = new BingoConn())
                    {
                        var res = new GetMDramaUrlVM();
                        string GameID = "MDRAMA";
                        string UPlayerID = PlayerID.ToUpper().Trim();

                        //mdrama
                        var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
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

                    using (BingoConn db = new BingoConn())
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

                    using (BingoConn db = new BingoConn())
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
                            var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
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
                            var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
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
                            var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
                                                                                                                                                                     x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

                            if (record != null)
                            {
                                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["BingoConn1"].ConnectionString))
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
                            var record = db.GameMappings.Where(x => x.Game.ToUpper().Trim() == GameID && x.PlayerID.ToUpper() == UPlayerID &&
                                                                                                                                                                     x.Currency.ToUpper() == UCurrency && x.IsActive == true).FirstOrDefault();

                            if (record != null)
                            {
                                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["BingoConn1"].ConnectionString))
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
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["BingoConn1"].ConnectionString))
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

        public string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
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
                else if (GameID == "PTLIVE")
                {
                    string prefix = "PTL";

                    if ((prefix + random + UPlayerID).Length <= 20)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "PTSLOT")
                {
                    string prefix = "PTS";

                    if ((prefix + random + UPlayerID).Length <= 20)
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
                else if (GameID == "SPADEGAMINGFISH")
                {
                    string prefix = "SPAF";

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
                // In FormGamePlayerID
                else if (GameID == "CQ9FISH")
                {
                    string prefix = "CQ9F";
                    if ((prefix + random + UPlayerID).Length <= 30)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "KISS918")
                {
                    string prefix = "K918";

                    if ((prefix + random + UPlayerID).Length <= 30)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "918H5")
                {
                    //string prefix = "918H5";

                    //if ((prefix + random + UPlayerID).Length <= 17)
                    //	return prefix + random + UPlayerID;
                    //else
                    //	return prefix + RandomString(10);

                    Kiss918H5Connector api = new Kiss918H5Connector(Environment: BaseConnector.GameEnvironment.PROD);
                    return api.RandomUserName();
                }
                else if (GameID == "HABANERO")
                {
                    string prefix = "HAB";

                    if ((prefix + random + UPlayerID).Length <= 30)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "JILI")
                {
                    string prefix = "JIL";

                    if ((prefix + random + UPlayerID).Length <= 30)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "DG")
                {
                    string prefix = "DG";

                    if ((prefix + random + UPlayerID).Length <= 30)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "PRAGMATICSLOT")
                {
                    string prefix = "PRAS";

                    if ((prefix + random + UPlayerID).Length <= 30)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "PRAGMATICLIVE")
                {
                    string prefix = "PRAL";

                    if ((prefix + random + UPlayerID).Length <= 30)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "GFG")
                {
                    string prefix = "GFG";

                    if ((prefix + random + UPlayerID).Length <= 30)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "KA")
                {
                    string prefix = "KA";

                    if ((prefix + random + UPlayerID).Length <= 30)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "JDB")
                {
                    string prefix = "JDB";

                    if ((prefix + random + UPlayerID).Length <= 30)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "JDBFISH")
                {
                    string prefix = "JDBF";

                    if ((prefix + random + UPlayerID).Length <= 30)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "LEG")
                {
                    string prefix = "LEG";

                    if ((prefix + random + UPlayerID).Length <= 30)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "LKL365")
                {
                    string prefix = "LKL365";

                    if ((prefix + random + UPlayerID).Length <= 30)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "EKOR")
                {
                    string prefix = "EKOR";

                    if ((prefix + random + UPlayerID).Length <= 30)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "LIONKING")
                {
                    string prefix = "LK";

                    if ((prefix + random + UPlayerID).Length <= 30)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "BG")
                {
                    string prefix = "BG";

                    if ((prefix + random + UPlayerID).Length <= 30)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "KING855")
                {
                    string prefix = "KING855";

                    if ((prefix + random + UPlayerID).Length <= 30)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "OBET")
                {
                    string prefix = "OBET";

                    if ((prefix + random + UPlayerID).Length <= 30)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "MKY888")
                {
                    string prefix = "MKY";

                    if ((prefix + random + UPlayerID).Length <= 30)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "RELAX")
                {
                    string prefix = "RLX";

                    if ((prefix + random + UPlayerID).Length <= 20)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "ABS")
                {
                    string prefix = "ABS";

                    if ((prefix + random + UPlayerID).Length <= 30)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "BNG")
                {
                    string prefix = "BNG";

                    if ((prefix + random + UPlayerID).Length <= 30)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "NEXTSPIN")
                {
                    string prefix = "NS";

                    if ((prefix + random + UPlayerID).Length <= 20)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "LIVE22")
                {
                    string prefix = "LIVE22";

                    if ((prefix + random + UPlayerID).Length <= 20)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "YEEBET")
                {
                    string prefix = "YEEBET";

                    if ((prefix + random + UPlayerID).Length <= 20)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "AP")
                {
                    string prefix = "AP";

                    if ((prefix + random + UPlayerID).Length <= 20)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "WCASINO")
                {
                    string prefix = "WC";

                    if ((prefix + random + UPlayerID).Length <= 20)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "MEGAH5")
                {
                    string prefix = "MEGAH5";

                    if ((prefix + random + UPlayerID).Length <= 20)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "DCT")
                {
                    string prefix = "DCT";

                    if ((prefix + random + UPlayerID).Length <= 20)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "PLAYSTAR")
                {
                    string prefix = "PLAYSTAR";

                    if ((prefix + random + UPlayerID).Length <= 30)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "ACE333")
                {
                    string prefix = "ACE333";

                    if ((prefix + random + UPlayerID).Length <= 30)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "AFB")
                {
                    string prefix = "AFB";

                    if ((prefix + random + UPlayerID).Length <= 30)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "HACKSAW")
                {
                    string prefix = "HS";

                    if ((prefix + random + UPlayerID).Length <= 20)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                //else if (GameID == "GEMINI")
                //{
                //    string prefix = "GEM";

                //    if ((prefix + random + UPlayerID).Length <= 20)
                //        return prefix + random + UPlayerID;
                //    else
                //        return prefix + RandomString(10);
                //}
                else if (GameID == "SIMPLEPLAY")
                {
                    string prefix = "SIP";

                    if ((prefix + random + UPlayerID).Length <= 20)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "SAGAMING")
                {
                    string prefix = "SAG";

                    if ((prefix + random + UPlayerID).Length <= 20)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "WIN28")
                {
                    string prefix = "W8";

                    if ((prefix + random + UPlayerID).Length <= 20)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "VPLUS")
                {
                    string prefix = "VPL";

                    if ((prefix + random + UPlayerID).Length <= 17)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "PEGASUS")
                {
                    string prefix = "PGS";

                    if ((prefix + random + UPlayerID).Length <= 17)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "JOKER")
                {
                    string prefix = "JK";

                    if ((prefix + random + UPlayerID).Length <= 17)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "FACHAI")
                {
                    string prefix = "FC";

                    if ((prefix + random + UPlayerID).Length <= 17)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "FACHAIFISH")
                {
                    string prefix = "FCF";

                    if ((prefix + random + UPlayerID).Length <= 17)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "JILIFISH")
                {
                    string prefix = "FCF";

                    if ((prefix + random + UPlayerID).Length <= 17)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "VPOWERFISH")
                {
                    string prefix = "VPF";
                    if ((prefix + random + UPlayerID).Length <= 30)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "EVOREDTIGER")
                {
                    string prefix = "EVORT";
                    if ((prefix + random + UPlayerID).Length <= 30)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "EVONETENT")
                {
                    string prefix = "EVONE";
                    if ((prefix + random + UPlayerID).Length <= 30)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "EVOBTG")
                {
                    string prefix = "EVOBTG";
                    if ((prefix + random + UPlayerID).Length <= 30)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "EVONLC")
                {
                    string prefix = "EVONLC";
                    if ((prefix + random + UPlayerID).Length <= 30)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "VBOSS")
                {
                    string prefix = "VB";

                    if ((prefix + random + UPlayerID).Length <= 20)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "VBOSSGD")
                {
                    string prefix = "VBGD";

                    if ((prefix + random + UPlayerID).Length <= 20)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "AWC")
                {
                    string prefix = "AWC";
                    if ((prefix + random + UPlayerID).Length <= 30)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "FASTSPINFISH")
                {
                    string prefix = "FPFF";
                    if ((prefix + random + UPlayerID).Length <= 30)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "FASTSPIN")
                {
                    string prefix = "FPF";
                    if ((prefix + random + UPlayerID).Length <= 30)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "CLOTPLAY")
                {
                    string prefix = "CPL";
                    if ((prefix + random + UPlayerID).Length <= 30)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "MT")
                {
                    string prefix = "MT";
                    if ((prefix + random + UPlayerID).Length <= 30)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "PTV2LIVE")
                {
                    string prefix = "PTV2L";

                    if ((prefix + random + UPlayerID).Length <= 20)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "PTV2SLOT")
                {
                    string prefix = "PTV2S";

                    if ((prefix + random + UPlayerID).Length <= 20)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "ONLYPLAY")
                {
                    string prefix = "OPL";
                    if ((prefix + random + UPlayerID).Length <= 14)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(9);
                }
                else if (GameID == "BESOFT")
                {
                    string prefix = "BS";
                    if ((prefix + random + UPlayerID).Length <= 30)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "OCTOPLAY")
                {
                    string prefix = "OPL";
                    if ((prefix + random + UPlayerID).Length <= 30)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "BTG")
                {
                    string prefix = "BTG";
                    if ((prefix + random + UPlayerID).Length <= 30)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "BTI")
                {
                    string prefix = "BTI";
                    if ((prefix + random + UPlayerID).Length <= 30)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "MG")
                {
                    string prefix = "MIG";

                    if ((prefix + random + UPlayerID).Length <= 20)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(10);
                }
                else if (GameID == "WINFINITY")
                {
                    string prefix = "WIF";

                    if ((prefix + random + UPlayerID).Length <= 45)
                        return prefix + random + UPlayerID;
                    else
                        return prefix + RandomString(25);
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
                using (BingoConn db = new BingoConn())
                {
                    string GameID = Game.ToUpper().Trim();
                    string UCurrency = Currency.ToUpper().Trim();

                    var record = db.GameTransferLimits.Where(x => x.Game.ToUpper().Trim() == GameID && x.Type.ToUpper().Trim() == Type &&
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
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["BingoConn1"].ConnectionString))
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
                else if (GameID == "SPADEGAMING" || GameID == "SPADEGAMINGFISH")
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

        public BaseConnector GetConnector(string GameID, string PlayerID, string Currency, BaseConnector.GameEnvironment env = BaseConnector.GameEnvironment.STAG)
        {
            if (string.IsNullOrEmpty(GameID)) return null;

            var ip = GetIPAddress();
            switch (GameID)
            {
                case "AG":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new AGConnector(PlayerID, Currency, ip, env);
                case "PT":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new PTConnector(PlayerID, Currency, ip, env);
                case "PTLIVE":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new PTLiveConnector(PlayerID, Currency, ip, env);
                case "PTSLOT":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new PTSlotConnector(PlayerID, Currency, ip, env);
                case "IG":
                    return null;
                case "BT28WIN":
                    return new BT28WINConnector(PlayerID, Currency, ip, env);
                case "ALLCITY":
                    return null;
                case "IBC":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new IBCConnector(PlayerID, Currency, ip, env);
                case "EVO888":
                    return null;
                case "SEXYGAMING":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new AESexyConnector(PlayerID, Currency, ip, env);
                case "SPADEGAMING":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new SpadeGamingConnector(PlayerID, Currency, ip, env);
                case "SPADEGAMINGFISH":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new SpadeGamingFishConnector(PlayerID, Currency, ip, env);
                case "MEGA":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new MEGAConnector(PlayerID, Currency, ip, env);
                case "MEGAH5":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new MegaH5Connector(PlayerID, Currency, ip, env);
                case "EVOLUTIONGAMING":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new EVOGMConnector(PlayerID, Currency, ip, env);
                case "MENANG88":
                    return null;
                case "XE88":
                    return null;
                case "KYPOKER":
                    return null;
                case "SABAESPORTS":
                    return null;
                case "TPG":
                    return null;
                case "TPGFISH":
                    return null;
                case "VGAMING":
                    return null;
                case "CQ9":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new CQ9Connector(PlayerID, Currency, ip, env);
                // In GetConnector
                case "CQ9FISH":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new CQ9FishConnector(PlayerID, Currency, ip, env);
                case "RELAX":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new RelaxConnector(PlayerID, Currency, ip, env);
                case "ABS":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new ABSConnector(PlayerID, Currency, ip, env);
                case "KISS918":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new KISS918Connector(PlayerID, Currency, ip, env);
                case "918H5":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new Kiss918H5Connector(PlayerID, Currency, ip, env);
                case "VGS":
                    return new VGSConnector(PlayerID, Currency, ip, env);
                case "GP":
                    return new GPConnector(PlayerID, Currency, ip, env);
                case "SAG":
                    return new SAGConnector(PlayerID, Currency, ip, env);
                case "MKY888":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new MKY888Connector(PlayerID, Currency, ip, env);
                case "HABANERO":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new HabaneroConnector(PlayerID, Currency, ip, env);
                case "JILI":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new JiLiConnector(PlayerID, Currency, ip, env);
                case "DG":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new DGConnector(PlayerID, Currency, ip, env);
                case "PRAGMATICSLOT":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new PragmaticSlotConnector(PlayerID, Currency, ip, env);
                case "PRAGMATICLIVE":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new PragmaticLiveConnector(PlayerID, Currency, ip, env);
                case "GFG":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new GFGConnector(PlayerID, Currency, ip, env);
                case "KA":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new KAConnector(PlayerID, Currency, ip, env);
                case "JDB":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new JDBConnector(PlayerID, Currency, ip, env);
                case "JDBFISH":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new JDBFishConnector(PlayerID, Currency, ip, env);
                case "LEG":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new LEGConnector(PlayerID, Currency, ip, env);
                case "LKL365":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new LKL365Connector(PlayerID, Currency, ip, env);
                case "EKOR":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new EkorConnector(PlayerID, Currency, ip, env);
                case "LIONKING":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new LionKingConnector(PlayerID, Currency, ip, env);
                case "BG":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new BGConnector(PlayerID, Currency, ip, env);
                case "KING855":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new KING855Connector(PlayerID, Currency, ip, env);
                case "OBET":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new OBETConnector(PlayerID, Currency, ip, env);
                case "VIVO":
                    return new VivoConnector(PlayerID, Currency, ip, env);
                case "ROCKETMAN":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new RocketmanConnector(PlayerID, Currency, ip, env);
                case "ARROWEDGE":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new ArrowEdgeConnector(PlayerID, Currency, ip, env);
                case "BNG":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new BNGConnector(PlayerID, Currency, ip, env);
                case "NEXTSPIN":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new NextSpinConnector(PlayerID, Currency, ip, env);
                case "LIVE22":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new Live22Connector(PlayerID, Currency, ip, env);
                case "YEEBET":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new YeeBetConnector(PlayerID, Currency, ip, env);
                case "AP":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new AdvantPlayConnector(PlayerID, Currency, ip, env);
                case "WCASINO":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new WCasinoConnector(PlayerID, Currency, ip, env);
                case "DCT":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new DCTConnector(PlayerID, Currency, ip, env);
                case "PLAYSTAR":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new PlayStarConnector(PlayerID, Currency, ip, env);
                case "ACE333":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new Ace333Connector(PlayerID, Currency, ip, env);
                case "AFB":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new AFBConnector(PlayerID, Currency, ip, env);
                case "HACKSAW":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new HackSawConnector(PlayerID, Currency, ip, env);
                //case "GEMINI":
                //    env = BaseConnector.GameEnvironment.PROD;
                //    return new GeminiConnector(PlayerID, Currency, ip, env);
                case "SIMPLEPLAY":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new SimplePlayConnector(PlayerID, Currency, ip, env);
                case "MG":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new MGConnector(PlayerID, Currency, ip, env);
                case "VPOWER":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new VPowerConnector(PlayerID, Currency, ip, env);
                case "VPOWERFISH":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new VPowerFishConnector(PlayerID, Currency, ip, env);
                case "SAGAMING":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new SAGamingConnector(PlayerID, Currency, ip, env);
                case "WIN28":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new WIN28Connector(PlayerID, Currency, ip, env);
                case "VPLUS":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new VPlusConnector(PlayerID, Currency, ip, env);
                case "PEGASUS":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new PegasusConnector(PlayerID, Currency, ip, env);
                case "JOKER":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new JokerConnector(PlayerID, Currency, ip, env);
                case "FACHAI":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new FACHAIConnector(PlayerID, Currency, ip, env);
                case "FACHAIFISH":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new FACHAIFISHConnector(PlayerID, Currency, ip, env);
                case "JILIFISH":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new JiliFishConnector(PlayerID, Currency, ip, env);
                case "EVOREDTIGER":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new EVORedtigerConnector(PlayerID, Currency, ip, env);
                case "EVONETENT":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new EVONetEntConnector(PlayerID, Currency, ip, env);
                case "EVOBTG":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new EVOBTGConnector(PlayerID, Currency, ip, env);
                case "EVONLC":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new EVONLCConnector(PlayerID, Currency, ip, env);
                case "FASTSPIN":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new AWCConnector(PlayerID, Currency, ip, env);
                case "FASTSPINFISH":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new AWCFishConnector(PlayerID, Currency, ip, env);
                case "CLOTPLAY":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new ClotPlayConnector(PlayerID, Currency, ip, env);
                case "VBOSS":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new VBossConnector(PlayerID, Currency, ip, env);
                case "VBOSSGD":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new VBossGDConnector(PlayerID, Currency, ip, env);
                case "MT":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new MTConnector(PlayerID, Currency, ip, env);
                case "PTV2LIVE":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new PTV2LiveConnector(PlayerID, Currency, ip, env);
                case "PTV2SLOT":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new PTV2SlotConnector(PlayerID, Currency, ip, env);
                case "ONLYPLAY":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new OnlyPlayConnector(PlayerID, Currency, ip, env);
                case "BESOFT":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new BeSoftConnector(PlayerID, Currency, ip, env);
                case "OCTOPLAY":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new OctoPlayConnector(PlayerID, Currency, ip, env);
                case "BTG":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new BTGConnector(PlayerID, Currency, ip, env);
                case "BTI":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new BTIConnector(PlayerID, Currency, ip, env);
                case "WINFINITY":
                    env = BaseConnector.GameEnvironment.PROD;
                    return new WINFINITYConnector(PlayerID, Currency, ip, env);
                default:
                    return null;
            }
        }

        [HttpGet]
        public HttpResponseMessage WinfinityGameList(string Currency = "MYR", string Type = null)
        {
            try
            {
                var api = GetConnector("WINFINITY", "SIRHK1", Currency.ToUpper().Trim(), BaseConnector.GameEnvironment.STAG) as WINFINITYConnector;
                if (api == null)
                    return new HttpResponseMessage()
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent("{\"Code\":8,\"Message\":\"GetConnector did not return WINFINITYConnector\"}", System.Text.Encoding.UTF8, "application/json")
                    };
                
                var res = api.GetGameList(false, Type);   // false = all vendors
                return new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonConvert.SerializeObject(res), System.Text.Encoding.UTF8, "application/json")
                };
            }
            catch (Exception ex)
            {
                return new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent(JsonConvert.SerializeObject(new { Code = -9999, Message = ex.ToString() }), System.Text.Encoding.UTF8, "application/json")
                };
            }
        }
    }
}