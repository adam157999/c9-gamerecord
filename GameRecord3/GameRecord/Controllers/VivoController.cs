using GameRecord.Controllers.SeamlessWallet.Model;
using GameRecord.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml.Style.XmlAccess;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics.Eventing.Reader;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web.Http;
using System.Xml;
using System.Xml.Serialization;
using static GameRecord.Controllers.BaseConnector;

namespace GameRecord.Controllers.SeamlessWallet
{

    public class VivoController : ApiController
    {
        private string p_ErrorMessage = "";
        private static string resultOk = "OK";
        private static string resultFailed = "FAILED";
        private static string authenticateFailed = "400";
        private static string changeBalanceFailed = "300";
        private static string cancelBetFailed = "299";
        private static string statusFailed = "302";
        private static string getBalanecFailed = "302";
        private static string invalidHash = "500";
        private const string rocketManGameCode = "ROCKETMAN";
        private const string arrowEdgeGameCode = "ARROWEDGE";
        private const string gameCode = "VIVO";
        private static string filePrefix = "VIVO";
        private const string cancelBet = "CANCELED_BET";
        private const string win = "WIN";
        private const string bet = "BET";
        private const string lose = "LOSE";
        private static string env = GameEnvironment.STAG.ToString();
        public string BingoConn1 = ConfigurationManager.ConnectionStrings["BingoConn1"].ToString();
        private string passKey = string.Empty;

        public VivoController()
        {
            passKey = GetPassKeyValue();
        }

        #region Private Function
        private string GetMD5Signature(string parameter)
        {
            using (MD5 md5Provider = MD5.Create())
            {
                if (string.IsNullOrEmpty(passKey))
                {
                    throw new Exception($"{gameCode} passKey not found");
                }
                var hash = md5Provider.ComputeHash(Encoding.UTF8.GetBytes(parameter + passKey));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hash)
                {
                    // 将字节转换成16进制表示的字符串，
                    sb.Append(b.ToString("x2"));
                }
                // 返回加密的字符串
                return sb.ToString();
            }
        }
        private string ConvertClassToXMLObject<T>(object TargetClass)
        {
            string jsonString = System.Text.Json.JsonSerializer.Serialize(TargetClass);
            XmlDocument doc = (XmlDocument)JsonConvert.DeserializeXmlNode(jsonString);
            using (var stringWriter = new StringWriter())
            using (var xmlTextWriter = XmlWriter.Create(stringWriter))
            {
                doc.WriteTo(xmlTextWriter);
                xmlTextWriter.Flush();
                return stringWriter.GetStringBuilder().ToString();
            }
        }
        private string GetPassKeyValue()
        {
            var apiConfig = CatchData.GetApiKeys(gameCode, env);

            if (apiConfig != null)
            {
                JObject jObj = JObject.Parse(apiConfig.ApiConfig);
                return jObj["VIVOPassKey"].ToString();
            }

            return string.Empty;
        }
        private bool VerifyMD5Signature(BaseRequest request, string hash)
        {
            bool flag = true;
            string hashString = GetMD5Signature(request.GetParameters());
            if (hashString != hash)
            {
                throw new Exception("Hash Value Error");
            }

            return flag;

        }

        private bool UpdateBetBalance(BingoConn db, Models.SeamlessWallet model, decimal amount, int gameId, string transactionId, string gameCode, ref dynamic childResponse)
        {
            var foundBetRecord = FormExistingChangeBalanceTransaction(db, model, gameId, transactionId, bet, gameCode);

            if (foundBetRecord != null)
            {
                childResponse = foundBetRecord;
                return true;
            }
            else
            {
                if (amount > (model.Chip + model.Cash))
                {
                    ErrorResult errorResponse = new ErrorResult
                    {
                        CODE = changeBalanceFailed,
                        RESULT = resultFailed
                    };
                    childResponse = errorResponse;
                    return false;
                }
                else
                {
                    if (amount > model.Chip)
                    {
                        model.Cash = model.Cash - (amount - model.Chip);
                        model.Chip = 0;
                    }
                    else
                    {
                        model.Chip = model.Chip - amount;
                    }

                    return true;
                }
            }
        }

        private bool UpdateWinBalance(BingoConn db, Models.SeamlessWallet model, decimal amount, int gameId, string transactionId, string gameCode, ref dynamic childResponse)
        {
            var foundBetRecord = FormExistingChangeBalanceTransaction(db, model, gameId, transactionId, win, gameCode);
            if(foundBetRecord != null)
            {
                childResponse = foundBetRecord;
                return true;
            }
            else
            {
                model.Cash = model.Cash + amount;
                return true;
            }

        }

        private bool UpdateCancelBetBalance(BingoConn db, Models.SeamlessWallet model, decimal amount, int gameId, string transactionId, string gameCode ,ref dynamic childResponse)
        {
            var foundBetRecord = FormExistingChangeBalanceTransaction(db, model, gameId, transactionId, cancelBet, gameCode);
            if (foundBetRecord != null)
            {
                childResponse = foundBetRecord;
                return true;
            }
            else
            {
                model.Cash = model.Cash + amount;
                return true;
            }
        }

        private long InsertIntoVivoGameRecord(BingoConn db, string userId, decimal Amount, string TransactionID, string TrnType, string TrnDescription,
           int roundId, int gameId, string History, bool isRoundFinished)
        {
            long returnId = 0;
            try
            {
                Models.VivoGameRecord model = new VivoGameRecord()
                {
                    PlayerId = userId,
                    Amount = Amount,
                    TransactionId = TransactionID,
                    TrnType = TrnType,
                    TrnDescription = TrnDescription,
                    RoundId = roundId.ToString(),
                    GameId = gameId.ToString(),
                    History = History,
                    RoundFinished = isRoundFinished.ToString(),
                    CreateDate = DateTime.Now,
                    ModifiedDate = DateTime.Now

                };
                db.VivoGameRecords.Add(model);
                db.SaveChanges();
                returnId = model.fId;

                return returnId;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private long InsertIntoGameRecord(BingoConn db, string userId, decimal Amount, string TransactionID, string TrnType, string TrnDescription,
         string roundId, string innerGameCode, string gameId, string History, bool isRoundFinished)
        {
            try
            {
                SqlConnection scon = new SqlConnection(BingoConn1);
                string sID = Guid.NewGuid().ToString();
                DateTime dtNow = DateTime.Now;

                string query = $"INSERT INTO dbo.{innerGameCode}GameRecord(PlayerId,Amount,TransactionId,TrnType,TrnDescription,RoundId,GameId,History,RoundFinished,ModifiedDate,CreateDate) VALUES ";
                query += "(@PlayerId,@Amount,@TransactionId,@TrnType,@TrnDescription,@RoundId,@GameId,@History,@RoundFinished,@ModifiedDate,@CreateDate)SELECT CAST(scope_identity() AS int)";
                SqlCommand cmd = new SqlCommand(query, scon);
                cmd.Parameters.AddWithValue("@id", sID);
                cmd.Parameters.AddWithValue("@PlayerId", userId);
                cmd.Parameters.AddWithValue("@Amount", Amount);
                cmd.Parameters.AddWithValue("@TransactionID", TransactionID);
                cmd.Parameters.AddWithValue("@TrnType", TrnType);
                cmd.Parameters.AddWithValue("@TrnDescription", TrnDescription);
                cmd.Parameters.AddWithValue("@RoundId", roundId);
                cmd.Parameters.AddWithValue("@GameId", gameId);
                if(History != null) 
                { 
                    cmd.Parameters.AddWithValue("@History", History);
                }
                else
                {
                    cmd.Parameters.AddWithValue("@History", "");
                }   
                cmd.Parameters.AddWithValue("@RoundFinished", isRoundFinished);
                cmd.Parameters.AddWithValue("@ModifiedDate", dtNow);
                cmd.Parameters.AddWithValue("@CreateDate", dtNow);
                scon.Open();
                int newID = (int)cmd.ExecuteScalar();
                scon.Close();

                return newID;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private List<VivoGameRecord> SelectGameRecord(string TransactionId, string TrnType, string gameId, string gameCode="")
        {
            try
            {
                List<VivoGameRecord> gameRecords = new List<VivoGameRecord>();
                SqlConnection scon = new SqlConnection(BingoConn1);
                string sID = Guid.NewGuid().ToString();
                DateTime dtNow = DateTime.Now;

                string query = $"SELECT * FROM dbo.{gameCode}GameRecord WHERE GameId = @gameId AND TransactionId = @transactionId AND TrnType = @trnType ";
                SqlCommand cmd = new SqlCommand(query, scon);
                scon.Open();
                cmd.Parameters.AddWithValue("@gameId", gameId);
                cmd.Parameters.AddWithValue("@transactionId", TransactionId);
                cmd.Parameters.AddWithValue("@trnType", TrnType);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            // iterate your results here
                            gameRecords.Add(new VivoGameRecord()
                            {
                                fId = (long)reader["fId"],
                                PlayerId = reader["PlayerId"].ToString(),
                                Amount = (decimal)reader["Amount"],
                                TransactionId = reader["TransactionId"].ToString(),
                                TrnType = reader["TrnType"].ToString(),
                                TrnDescription = reader["TrnDescription"].ToString(),
                                RoundId = reader["RoundId"].ToString(),
                                GameId = reader["GameId"].ToString(),
                                History = reader["History"].ToString(),
                                RoundFinished = reader["RoundFinished"].ToString(),
                                CreateDate = (DateTime)reader["CreateDate"],
                                ModifiedDate = (DateTime)reader["ModifiedDate"]
                            });
                        }
                    }


                }
                scon.Close();   

                return gameRecords;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private List<VivoGameRecord> SelectGameRecordStatus(string TransactionId, string PlayerId, string gameCode)
        {
            try
            {
                List<VivoGameRecord> gameRecords = new List<VivoGameRecord>();
                SqlConnection scon = new SqlConnection(BingoConn1);
                string sID = Guid.NewGuid().ToString();
                DateTime dtNow = DateTime.Now;

                string query = $"SELECT * FROM dbo.{gameCode}GameRecord WHERE PlayerId = @playerId AND TransactionId = @transactionId ";
                SqlCommand cmd = new SqlCommand(query, scon);
                scon.Open();
                cmd.Parameters.AddWithValue("@playerId", PlayerId);
                cmd.Parameters.AddWithValue("@transactionId", TransactionId);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            // iterate your results here
                            gameRecords.Add(new VivoGameRecord()
                            {
                                fId = (long)reader["fId"],
                                PlayerId = reader["PlayerId"].ToString(),
                                Amount = (decimal)reader["Amount"],
                                TransactionId = reader["TransactionId"].ToString(),
                                TrnType = reader["TrnType"].ToString(),
                                TrnDescription = reader["TrnDescription"].ToString(),
                                RoundId = reader["RoundId"].ToString(),
                                GameId = reader["GameId"].ToString(),
                                History = reader["History"].ToString(),
                                RoundFinished = reader["RoundFinished"].ToString(),
                                CreateDate = (DateTime)reader["CreateDate"],
                                ModifiedDate = (DateTime)reader["ModifiedDate"]
                            });
                        }
                    }


                }
                scon.Close();

                return gameRecords;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        private dynamic FormExistingChangeBalanceTransaction(BingoConn db, Models.SeamlessWallet model, int gameId, string transactionId, string trnType, string gameCode = "")
        {
            switch(gameCode)
            {
                case "VIVO":
                    var foundBetRecord = db.VivoGameRecords.Where(x => x.GameId == gameId.ToString()
                    && x.TransactionId == transactionId && x.TrnType == trnType).ToList();
                    if (!foundBetRecord.Any()) { return null; }
                    foundBetRecord.OrderByDescending(x => x.fId);
                    dynamic response = new ChangeBalanceChildResponse()
                    {
                        RESULT = resultOk,
                        ECSYSTEMTRANSACTIONID = foundBetRecord[foundBetRecord.Count() - 1].fId,
                        BALANCE = (model.Cash + model.Chip).ToString("F")
                    };
                    return response;
                case arrowEdgeGameCode:
                    var foundArrowEdgeBetRecord = SelectGameRecord(transactionId,trnType,gameId.ToString(), arrowEdgeGameCode);
                    if (!foundArrowEdgeBetRecord.Any()) { return null; }
                    foundArrowEdgeBetRecord.OrderByDescending(x => x.fId);
                    dynamic foundArrowEdgeBetResponse = new ChangeBalanceChildResponse()
                    {
                        RESULT = resultOk,
                        ECSYSTEMTRANSACTIONID = foundArrowEdgeBetRecord[foundArrowEdgeBetRecord.Count() - 1].fId,
                        BALANCE = (model.Cash + model.Chip).ToString("F")
                    };
                    return foundArrowEdgeBetResponse;
                case rocketManGameCode:
                    var foundRocketManBetRecord = SelectGameRecord(transactionId, trnType,gameId.ToString(), rocketManGameCode);
                    if (!foundRocketManBetRecord.Any()) { return null; }
                    foundRocketManBetRecord.OrderByDescending(x => x.fId);
                    dynamic foundRocketManBetResponse = new ChangeBalanceChildResponse()
                    {
                        RESULT = resultOk,
                        ECSYSTEMTRANSACTIONID = foundRocketManBetRecord[foundRocketManBetRecord.Count() - 1].fId,
                        BALANCE = (model.Cash + model.Chip).ToString("F")
                    };
                    return foundRocketManBetResponse;
                default:
                    return null;
            }

        }
        #endregion

        [HttpGet]
        public HttpResponseMessage Authenticate(string token, string hash)
        {
            bool passFlag = true;
            dynamic childResponse = null;
            Common _common = new Common();
            AuthenticateRootResponse baseResponse = new AuthenticateRootResponse();
            AuthenticateResponse response = new AuthenticateResponse();
            ErrorResult errorResponse = new ErrorResult();
            AuthenticateRequest request = new AuthenticateRequest();
            AuthenticateChildResponse successResponse = new AuthenticateChildResponse();
            string code = "";
            string innerGameCode = string.Empty;
            string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            try
            {
                request = new AuthenticateRequest()
                {
                    TOKEN = token,
                    HASH = hash
                };

                passFlag = VerifyMD5Signature(request, hash);
                using (BingoConn db = new BingoConn())
                {
                    var foundRecord = db.SeamlessWallets.SingleOrDefault(
                        x => (x.Game == rocketManGameCode || x.Game == gameCode || x.Game == arrowEdgeGameCode)
                    && x.Token == token);

                    if (foundRecord != null)
                    {
                        passFlag = true;
                        successResponse = new AuthenticateChildResponse()
                        {
                            RESULT = resultOk,
                            USERID = String.IsNullOrEmpty(foundRecord.Username) ? "" : foundRecord.Username,
                            CURRENCY = String.IsNullOrEmpty(foundRecord.Currency) ? "" : foundRecord.Currency,
                            GAMESESSIONID = string.Empty, 
                            FIRSTNAME = string.Empty,
                            LASTNAME = string.Empty,
                            EMAIL = string.Empty,
                            BALANCE = (foundRecord.Cash + foundRecord.Chip).ToString("F")
                        };
                        //Expired token by update token to empty.
                        //foundRecord.Token = null;
                        innerGameCode = foundRecord.Game;
                        childResponse = successResponse;
                        code = resultOk;
                    }
                    else
                    {
                        passFlag = false;
                        errorResponse = new ErrorResult()
                        {
                            RESULT = resultFailed,
                            CODE = authenticateFailed
                        };
                        childResponse = errorResponse;
                        code = authenticateFailed;
                    }


                    //baseResponse = new AuthenticateRootResponse()
                    //{
                    //    REQUEST = request,
                    //    RESPONSE = childResponse
                    //};

                    //response = new AuthenticateResponse()
                    //{
                    //    VGSSYSTEM = baseResponse
                    //};
                    //string responseContent = ConvertClassToXMLObject<AuthenticateResponse>(response);

                    db.SaveChanges();
                    //return new HttpResponseMessage()
                    //{
                    //    StatusCode = HttpStatusCode.OK,
                    //    Content = new StringContent(responseContent, Encoding.UTF8, "application/xml")
                    //};
                }


            }
            catch (Exception ex)
            {
               
                p_ErrorMessage = ex.Message.ToString();
                _common.LogError($"{dateTime}:Authenticate:EXCEPTION " + string.Format("{0}", innerGameCode) + " " + ex.Message.ToString(), filePrefix);

                dynamic res = new ExpandoObject();
                errorResponse = new ErrorResult()
                {
                    RESULT = resultFailed,
                    CODE = invalidHash
                };
                childResponse = errorResponse;
                code = invalidHash;
                _common.InsertLogWithGameId($"{innerGameCode}Authenticate", innerGameCode, "TrackerId", dateTime, "Exception", ex.Message.ToString(), "Game", innerGameCode, "Token", token, "Code", code);
            }

            baseResponse = new AuthenticateRootResponse()
            {
                TIME = DateTime.Now.ToString("dd MMMM yyyy HH:mm:ss"),
                REQUEST = request,
                RESPONSE = childResponse
            };

            response = new AuthenticateResponse()
            {
                VGSSYSTEM = baseResponse
            };
            string responseContent = ConvertClassToXMLObject<AuthenticateResponse>(response);

            _common.InsertLogWithGameId($"{innerGameCode}Authenticate", innerGameCode, "TrackerId", dateTime, "Response", responseContent, "Game", innerGameCode, "Token", token,"Code" , code);
            return new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent,Encoding.UTF8)
            };

        }

        [HttpGet]
        public HttpResponseMessage ChangeBalance(string userId, decimal Amount, string TransactionID, string TrnType, string TrnDescription,
           int roundId, int gameId, string History, string isRoundFinished, string hash)
        {
            bool isRoundFinishedConvert;
            bool passFlag = false;
            Common _common = new Common();
            dynamic childResponse = null;
            string innerGameCode = string.Empty;
            ErrorResult error = new ErrorResult();
            ChangeBalanceRootResponse baseResponse = new ChangeBalanceRootResponse();
            ChangeBalanceResposne response = new ChangeBalanceResposne();
            ErrorResult errorResponse = new ErrorResult();
            ChangeBalanceRequest request = new ChangeBalanceRequest();
            string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string code = resultOk;

            try
            {
                if (isRoundFinished.Equals("1") || isRoundFinished.Equals("0"))
                {
                    isRoundFinishedConvert = Convert.ToBoolean(Convert.ToInt16(isRoundFinished));
                }
                else {
                    isRoundFinishedConvert = Convert.ToBoolean(isRoundFinished);
                }
                request = new ChangeBalanceRequest()
                {
                    USERID = userId,
                    AMOUNT = Amount.ToString(),
                    TRANSACTIONID = TransactionID,
                    TRNTYPE = TrnType,
                    TRNDESCRIPTION = TrnDescription,
                    ROUNDID = roundId,
                    GAMEID = gameId,
                    HISTORY = History,
                    ISROUNDFINISHED = isRoundFinishedConvert,
                    HASH = hash
                };

                passFlag = VerifyMD5Signature(request, hash);
                using (BingoConn db = new BingoConn())
                {
                    var foundRecord = db.SeamlessWallets.SingleOrDefault(
                         x => (x.Game == gameCode || x.Game == rocketManGameCode ||  x.Game == arrowEdgeGameCode)
                        && x.Username == userId);

                    if(foundRecord != null)
                    {
                        innerGameCode = foundRecord.Game;
                    }

                    switch (TrnType)
                    {
                        case bet:
                            passFlag = UpdateBetBalance(db, foundRecord, Amount, gameId, TransactionID, innerGameCode, ref childResponse);
                            break;
                        case win:
                            passFlag = UpdateWinBalance(db, foundRecord, Amount, gameId, TransactionID, innerGameCode, ref childResponse);
                            break;
                        case lose:
                            break;
                        case cancelBet:
                            passFlag = UpdateCancelBetBalance(db, foundRecord, Amount, gameId, TransactionID, innerGameCode, ref childResponse);
                            break;
                        default:
                            break;
                    }


                    if (passFlag == true && childResponse == null)
                    {
                        long transactionID = 0;
                        if (innerGameCode == gameCode)
                        {
                             transactionID = InsertIntoVivoGameRecord(db, userId, Amount, TransactionID, TrnType, TrnDescription, roundId, gameId, History, isRoundFinishedConvert);
                        }
                        else
                        {
                             transactionID = InsertIntoGameRecord(db, userId, Amount, TransactionID, TrnType, TrnDescription, roundId.ToString(), innerGameCode, gameId.ToString(), History, isRoundFinishedConvert);
                        }
                        ChangeBalanceChildResponse successResposne = new ChangeBalanceChildResponse()
                        {
                            RESULT = resultOk,
                            ECSYSTEMTRANSACTIONID = transactionID,
                            BALANCE = (foundRecord.Cash + foundRecord.Chip).ToString("F")
                        };
                        childResponse = successResposne;
                        code = resultOk;
                    }

                    db.SaveChanges();

                }
            }
            catch (Exception ex)
            {
                p_ErrorMessage = ex.Message.ToString();
                _common.LogError($"{dateTime}:ChangeBalance:EXCEPTION " + string.Format("{0}", innerGameCode) + " " + ex.Message.ToString(), filePrefix);

                dynamic res = new ExpandoObject();
                errorResponse = new ErrorResult()
                {
                    RESULT = resultFailed,
                    CODE = invalidHash
                };
                childResponse = errorResponse;
                code = invalidHash;
                _common.InsertLogWithGameId($"{innerGameCode}ChangeBalance", innerGameCode, "TrackerId", dateTime, "Exception", ex.Message.ToString(),
                    "Game", innerGameCode, "TransactionId", TransactionID, "Code", code, "RoundId" , roundId, "GameId", gameId, "TrnType", TrnType,"TrnDescription", TrnDescription,
                    "UserId",userId);
            }

            baseResponse = new ChangeBalanceRootResponse()
            {
                REQUEST = request,
                RESPONSE = childResponse
            };

            response = new ChangeBalanceResposne()
            {
                VGSSYSTEM = baseResponse
            };
            string responseContent = ConvertClassToXMLObject<AuthenticateResponse>(response);

            _common.InsertLogWithGameId($"{innerGameCode}ChangeBalance", innerGameCode, "TrackerId", dateTime,
                    "Game", innerGameCode, "TransactionId", TransactionID, "Code", code, "RoundId", roundId, "GameId", gameId, "TrnType", TrnType, "TrnDescription", TrnDescription,
                    "UserId", userId);
            return new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/xml")
            };
        }

        [HttpGet]
        public HttpResponseMessage RequestStatus(string userid, string casinoTransactionId, string hash)
        {
            bool passFlag = false;
            dynamic childResponse = null;
            Common _common = new Common();
            StatusRootResponse baseResponse = new StatusRootResponse();
            StatusResposne response = new StatusResposne();
            ErrorResult errorResponse = new ErrorResult();
            StatusRequest request = new StatusRequest();
            VivoGameRecord foundRecord = new VivoGameRecord();
            StatusChildResponse successResposne = new StatusChildResponse();
            string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string code = "";
            string innerGameCode = string.Empty;

            try
            {
                request = new StatusRequest()
                {
                    USERID = userid,
                    CASIONTRANSCATIONID = casinoTransactionId,
                    HASH = hash
                };

                passFlag = VerifyMD5Signature(request, hash);
                using (BingoConn db = new BingoConn())
                {
                    var foundPlayerRecord = db.SeamlessWallets.SingleOrDefault(
                         x => (x.Game == gameCode || x.Game == rocketManGameCode || x.Game == arrowEdgeGameCode)
                        && x.Username == userid);

                    if (foundPlayerRecord != null)
                    {
                        innerGameCode = foundPlayerRecord.Game;
                        
                    }
                    switch(innerGameCode)
                    {
                        case "VIVO":
                             foundRecord = db.VivoGameRecords.Where(x => x.TransactionId == casinoTransactionId
                                && x.PlayerId == userid).OrderByDescending(x => x.fId).FirstOrDefault();
                            break;
                        case arrowEdgeGameCode:
                            foundRecord = SelectGameRecordStatus(casinoTransactionId, userid , arrowEdgeGameCode).OrderByDescending(x => x.fId).FirstOrDefault();
                            break;
                        case rocketManGameCode:
                            foundRecord = SelectGameRecordStatus(casinoTransactionId, userid, rocketManGameCode).OrderByDescending(x => x.fId).FirstOrDefault();
                            break;

                    }

                    if (foundRecord != null)
                    {
                        passFlag = true;
                        successResposne = new StatusChildResponse()
                        {
                            RESULT = resultOk,
                            ECSYSTEMTRANSACTIONID = foundRecord.fId
                        };
                        childResponse = successResposne;
                        code = resultOk;
                        innerGameCode = foundRecord.GameId;
                    }
                    else
                    {
                        passFlag = false;
                        errorResponse = new ErrorResult()
                        {
                            RESULT = resultFailed,
                            CODE = statusFailed
                        };
                        childResponse = errorResponse;
                        code = statusFailed;
                    }


                }
            }
            catch (Exception ex)
            {

                p_ErrorMessage = ex.Message.ToString();
                childResponse = errorResponse;
                _common.LogError($"{dateTime}:RequestStatus:EXCEPTION " + string.Format("{0}", innerGameCode) + " " + ex.Message.ToString(), filePrefix);

                dynamic res = new ExpandoObject();
                errorResponse = new ErrorResult()
                {
                    RESULT = resultFailed,
                    CODE = invalidHash
                };
                childResponse = errorResponse;
                code = invalidHash;
                _common.InsertLogWithGameId($"{innerGameCode}RequestStatus", innerGameCode, "TrackerId", dateTime, "Exception", ex.Message.ToString(), "Game", gameCode, "UserID", userid, "Code", code);
            }

            baseResponse = new StatusRootResponse()
            {
                REQUEST = request,
                RESPONSE = childResponse
            };

            response = new StatusResposne()
            {
                VGSSYSTEM = baseResponse
            };

            string responseContent = ConvertClassToXMLObject<GetBalanceRootResponse>(response);
            _common.InsertLogWithGameId($"{innerGameCode}RequestStatus", innerGameCode, "TrackerId", dateTime, "Response", responseContent, "Game", innerGameCode, "CasionTransactionId", casinoTransactionId, "Code", code);
            return new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/xml")
            };
        }

        [HttpGet]
        public HttpResponseMessage GetBalance(string userid, string hash)
        {
            bool passFlag = false;
            dynamic childResponse = null;
            Common _common = new Common();
            GetBalanceRootResponse baseResponse = new GetBalanceRootResponse();
            GetBalanceResponse response = new GetBalanceResponse();
            ErrorResult errorResponse = new ErrorResult();
            GetBalanceRequest request = new GetBalanceRequest();
            GetBalanceChildResponse successResponse = new GetBalanceChildResponse();
            string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string innerGameCode = string.Empty;
            string code = "";
            try
            {
                request = new GetBalanceRequest()
                {
                    USERID = userid,
                    HASH = hash
                };

                passFlag = VerifyMD5Signature(request, hash);
                using (BingoConn db = new BingoConn())
                {
                    var foundRecord = db.SeamlessWallets.SingleOrDefault(
                        x => (x.Game == gameCode || x.Game == rocketManGameCode || x.Game == arrowEdgeGameCode)
                    && x.Username == userid);

                    if (foundRecord != null)
                    {
                        passFlag = true;
                        successResponse = new GetBalanceChildResponse()
                        {
                            RESULT = resultOk,
                            BALANCE = (foundRecord.Cash + foundRecord.Chip).ToString("F")
                        };
                        childResponse = successResponse;
                        code = resultOk;
                        innerGameCode = foundRecord.Game;
                    }
                    else
                    {
                        passFlag = false;
                        errorResponse = new ErrorResult()
                        {
                            RESULT = resultFailed,
                            CODE = getBalanecFailed
                        };
                        childResponse = errorResponse;
                        code = getBalanecFailed;
                    }
                }
            }
            catch (Exception ex)
            {
                p_ErrorMessage = ex.Message.ToString();
                _common.LogError($"{dateTime}:GetBalance:EXCEPTION " + string.Format("{0}", innerGameCode) + " " + ex.Message.ToString(), filePrefix);

                dynamic res = new ExpandoObject();
                errorResponse = new ErrorResult()
                {
                    RESULT = resultFailed,
                    CODE = invalidHash
                };
                childResponse = errorResponse;
                code = invalidHash;
                _common.InsertLogWithGameId($"{innerGameCode}GetBalance", innerGameCode, "TrackerId", dateTime, "Exception", ex.Message.ToString(), "Game", innerGameCode, "UserID", userid, "Code", code);
            }

            baseResponse = new GetBalanceRootResponse()
            {
                REQUEST = request,
                RESPONSE = childResponse
            };

            response = new GetBalanceResponse()
            {
                VGSSYSTEM = baseResponse
            };
            string responseContent = ConvertClassToXMLObject<GetBalanceRootResponse>(response);
            _common.InsertLogWithGameId($"{innerGameCode}GetBalance", innerGameCode, "TrackerId", dateTime, "Response", responseContent, "Game", innerGameCode, "UserID", userid,"Code", code);
            return new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/xml")
            };
        }
    }
}
