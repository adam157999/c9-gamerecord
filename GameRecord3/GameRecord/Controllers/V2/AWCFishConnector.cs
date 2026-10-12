using GameRecord.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using vm = GameRecord.Controllers.GameAPIController;
using AWC.Models; // Added to resolve missing types
using GameRecord.Gamess;
using System.Collections.Generic;

namespace GameRecord.Controllers
{
    public class AWCFishConnector : BaseConnector
    {
        private string gameID = "FASTSPINFISH";
        private string gamePlatform = "FASTSPIN";
        private string apiUrl = "";
        private string cert = "";
        private string agentId = "";
        private int timeout = 1;
        private GameMapping gameMap;
        private Gamess.AWC.AWC api = null; // Fully qualified the AWC class

        public AWCFishConnector(string playerID = "", string currencyID = "", string ipAddr = "", GameEnvironment environment = GameEnvironment.PROD) : base(playerID, currencyID, ipAddr, environment)
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
                    apiUrl = jObj["FASTSPINFISHApiUrl"].ToString();
                    cert = jObj["FASTSPINFISHCert"].ToString();
                    agentId = jObj["FASTSPINFISHAgentId"].ToString();
                    timeout = int.Parse(jObj["FASTSPINFISHTimeOutSec"].ToString());

                    api = new Gamess.AWC.AWC(apiUrl, cert, agentId, timeout);
                }
            }
        }

        public override vm.CheckAndCreateAccountVM CreatePlayer(string loginID, string loginPass, string fullName, string mcUID)
        {
            var res = new vm.CheckAndCreateAccountVM();

            try
            {
                var createMemberRequest = new CreateMemberRequest
                {
                    UserId = loginID,
                    Currency = currencyID,
                    Language = "en"
                };

                if (api.CreateMember(createMemberRequest))
                {
                    InsertLog(gameID, "CreateUser", "request", JsonConvert.SerializeObject(createMemberRequest), "response", JsonConvert.SerializeObject(api.CreateMemberResponse));

                    InsertGameMapping(gameID, loginID, loginPass, mcUID);

                    res.PlayerID = playerID;
                    res.GamePlayerID = loginID;
                    res.Game = gameID;
                    res.Message = "Success";
                    res.Code = 0;
                }
                else
                {
                    InsertLog(gameID, "CreateUser", "request", JsonConvert.SerializeObject(createMemberRequest), "response", JsonConvert.SerializeObject(api.ErrorMessage));

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
            var depositRequest = new DepositeRequest
            {
                UserId = gameMap.GamePlayerID,
                TxCode = transactionID,
                TransferAmount = amount
            };

            if (api.Deposite(depositRequest))
            {
                int transferOpertaion = -1;
                while (transferOpertaion != 1 && transferOpertaion != 0 && transferOpertaion != 1017)
                {
                    transferOpertaion = TransactionCheck(api.DepositeResponse.TxCode, ref error);
                    if (transferOpertaion == 2)
                    {
                        System.Threading.Thread.Sleep(3000); // Wait for 3 seconds before retrying
                    }
                }

                if (transferOpertaion == 1)
                {
                    InsertLog(gameID, "Deposit", "request", JsonConvert.SerializeObject(depositRequest), "response", JsonConvert.SerializeObject(api.DepositeResponse));
                    return true;
                }
                else if (transferOpertaion == 0)
                {
                    InsertLog(gameID, "Deposit", "request", JsonConvert.SerializeObject(depositRequest), "response", JsonConvert.SerializeObject(error));
                    return false;
                }
                else
                {
                    InsertLog(gameID, "Deposit", "request", JsonConvert.SerializeObject(depositRequest), "response", JsonConvert.SerializeObject(error));
                    return false;
                }
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
            var withdrawRequest = new WithdrawRequest
            {
                UserId = gameMap.GamePlayerID,
                TxCode = transactionID,
                TransferAmount = amount
            };

            if (api.Withdraw(withdrawRequest))
            {
                int transferOpertaion = -1;
                while (transferOpertaion != 1 && transferOpertaion != 0 && transferOpertaion != 1017)
                {
                    transferOpertaion = TransactionCheck(api.WithdrawResponse.TxCode, ref error);
                    if (transferOpertaion == 2)
                    {
                        System.Threading.Thread.Sleep(3000); // Wait for 3 seconds before retrying
                    }
                }

                if (transferOpertaion == 1)
                {
                    InsertLog(gameID, "Withdraw", "request", JsonConvert.SerializeObject(withdrawRequest), "response", JsonConvert.SerializeObject(api.WithdrawResponse));
                    return true;
                }
                else if (transferOpertaion == 0)
                {
                    InsertLog(gameID, "Withdraw", "request", JsonConvert.SerializeObject(withdrawRequest), "response", JsonConvert.SerializeObject(error));
                    return false;
                }
                else
                {
                    InsertLog(gameID, "Withdraw", "request", JsonConvert.SerializeObject(withdrawRequest), "response", JsonConvert.SerializeObject(error));
                    return false;
                }
            }
            else
            {
                InsertLog(gameID, "Withdraw", "request", JsonConvert.SerializeObject(withdrawRequest), "response", JsonConvert.SerializeObject(api.ErrorMessage));
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
                var balanceRequest = new GetBalanceRequest
                {
                    UserIds = gameMap.GamePlayerID
                };

                if (api.GetBalance(balanceRequest))
                {
                    InsertLog(gameID, "GetBalance", "request", JsonConvert.SerializeObject(balanceRequest), "response", JsonConvert.SerializeObject(api.GetBalanceResponse));

                    res.PlayerID = playerID;
                    if (decimal.TryParse(api.GetBalanceResponse.Results.FirstOrDefault()?.Balance, out var balance))
                    {
                        res.Balance = balance;
                    }
                    else
                    {
                        res.Balance = null;
                    }
                    res.Game = gameID;
                    res.Message = "Successfully retrieved balance.";
                    res.Code = 0;
                }
                else
                {
                    InsertLog(gameID, "GetBalance", "request", JsonConvert.SerializeObject(balanceRequest), "response", JsonConvert.SerializeObject(api.ErrorMessage));
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
            var balanceRequest = new GetBalanceRequest
            {
                UserIds = gameMap.GamePlayerID
            };

            if (api.GetBalance(balanceRequest))
            {
                InsertLog(gameID, "GetGameBalance", "request", JsonConvert.SerializeObject(balanceRequest), "response", JsonConvert.SerializeObject(api.GetBalanceResponse));
                
                if(api.GetBalanceResponse.Results.Count  == 0)
                {
                    return 0;
                }
                if (decimal.TryParse(api.GetBalanceResponse.Results.FirstOrDefault()?.Balance, out var balance))
                {
                    return balance;
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

        public override vm.GetGameUrlVM GetGameUrl(string langId, bool isMobile = false)
        {
            var res = new vm.GetGameUrlVM();

            try
            {
                var loginRequest = new LoginGameRequest
                {
                    UserId = gameMap.GamePlayerID,
                    Language = langId,
                    IsMobileLogin = isMobile,
                    Platform = gamePlatform,
                    GameForbidden = "{\"FASTSPIN\": {\"SLOT\":[\"ALL\"]}}",
                    GameType = "FH"
                };

                if (api.DoLoginAndLaunchGame(loginRequest))
                {
                    InsertLog(gameID, "GetGameUrl", "request", JsonConvert.SerializeObject(loginRequest), "response", JsonConvert.SerializeObject(api.LoginResponse));

                    res.PlayerID = playerID;
                    res.Url = api.LoginResponse.Url;
                    res.Game = gameID;
                    res.Message = "Success";
                    res.Code = 0;
                }
                else
                {
                    InsertLog(gameID, "GetGameUrl", "request", JsonConvert.SerializeObject(loginRequest), "response", JsonConvert.SerializeObject(api.ErrorMessage));

                    res.PlayerID = playerID;
                    res.Url = null;
                    res.Game = gameID;
                    res.Message = api.ErrorMessage;
                    res.Code = 2;
                }
            }
            catch (Exception ex)
            {
                _common.LogError("GetGameUrl" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

                var err = new Error
                {
                    ErrorCode = "999",
                    ErrorDesc = ex.Message.ToString()
                };

                InsertLog(gameID, "GetGameUrl", "Error", JsonConvert.SerializeObject(err));

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
            var logoutRequest = new LogoutRequest
            {
                UserIds = gamePlayerId
            };

            if (api.Logout(logoutRequest))
            {
                InsertLog(gameID, "KickPlayer", "request", JsonConvert.SerializeObject(logoutRequest), "response", JsonConvert.SerializeObject(api.LogoutResponse));
                return true;
            }
            else
            {

                InsertLog(gameID, "KickPlayer", "request", JsonConvert.SerializeObject(logoutRequest), "response", JsonConvert.SerializeObject(api.ErrorMessage));
                error = new Error
                {
                    ErrorCode = "998",
                    ErrorDesc = api.ErrorMessage
                };
                return false;
            }
        }

        public override string RetryDeposit(decimal amount, string trackerId, ref Error error)
        {
            throw new NotImplementedException();
        }

        public override string RetryWithdraw(decimal amount, string trackerId, ref Error error)
        {
            throw new NotImplementedException();
        }

        public List<JDBFishGame> GetGameList(string lang = "en-us")
        {
            // Implement logic to fetch game list for AWCFish
            return new List<JDBFishGame>();
        }

        public vm.GetGameUrlVM GetFishGameUrl(string langId, string gType, string mType, bool isMobile = false)
        {
            // Implement logic to fetch game URL for AWCFish
            return new vm.GetGameUrlVM();
        }

        private int TransactionCheck(string transactionID, ref Error error)
        {
            var checkTransferOperationRequest = new CheckTransferOperationRequest
            {
                TxCode = transactionID
            };
            if (api.CheckTransferOperation(checkTransferOperationRequest))
            {
                InsertLog(gameID, "TransactionCheck", "request", JsonConvert.SerializeObject(checkTransferOperationRequest), "response", JsonConvert.SerializeObject(api.CheckTransferOperationResponse));
                if (api.CheckTransferOperationResponse != null)
                {
                    var result = api.CheckTransferOperationResponse.TxStatus;
                    switch (result)
                    {
                        case 1: // Assuming "1" indicates success
                            return result;

                        case 0:
                            error = new Error
                            {
                                ErrorCode = "0",
                                ErrorDesc = "Transaction failed"
                            };
                            return result;

                        case 2:
                            error = new Error
                            {
                                ErrorCode = "2",
                                ErrorDesc = "Transaction Processing"
                            };
                            return result;

                        case 1017:
                            error = new Error
                            {
                                ErrorCode = "1017",
                                ErrorDesc = "Transaction Not Exists"
                            };
                            return result;

                        default:
                            error = new Error
                            {
                                ErrorCode = "-998",
                                ErrorDesc = "Unknown transaction status"
                            };
                            return result;
                    }
                }
                else
                {
                    error = new Error
                    {
                        ErrorCode = "-997",
                        ErrorDesc = "No results found for the transaction."
                    };
                    return -1;
                }
            }
            else
            {
                InsertLog(gameID, "TransactionCheck", "request", JsonConvert.SerializeObject(checkTransferOperationRequest), "response", JsonConvert.SerializeObject(api.ErrorMessage));
                error = new Error
                {
                    ErrorCode = "-999",
                    ErrorDesc = api.ErrorMessage
                };
                return -1;
            }
        }
    }
}