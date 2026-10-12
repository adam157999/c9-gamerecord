using Antlr.Runtime;
using GameRecord.Gamess;
using GameRecord.Gamess.ClotPlay;
using GameRecord.Gamess.ClotPlay.Models;
using GameRecord.Gamess.Constant;
using GameRecord.Gamess.EvoGaming;
using GameRecord.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using static GameRecord.Gamess.SAGAPI.SAGAMINGAPI;
using vm = GameRecord.Controllers.GameAPIController;

namespace GameRecord.Controllers
{
    public class ClotPlayConnector : BaseConnector
    {
        private string gameID = "CLOTPLAY";
        private string apiUrl = "";
        private string operatorToken = "";
        private string agentCode = "";
        private int timeout = 30;
        private GameMapping gameMap;
        private Gamess.ClotPlay.ClotPlay api = null;
        private string lobbyUrl = HttpContext.Current.Request.Url.AbsoluteUri.Replace(HttpContext.Current.Request.Url.PathAndQuery, "/") + "ClotPlay/Lobby";


        public ClotPlayConnector(string playerID = "", string currencyID = "", string ipAddr = "", GameEnvironment environment = GameEnvironment.PROD) : base(playerID, currencyID, ipAddr, environment)
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
                    apiUrl = jObj["ClotPlayApiUrl"].ToString();
                    operatorToken = jObj["ClotPlayOperatorToken"].ToString();
                    agentCode = jObj["ClotPlayAgentCode"].ToString();
                    timeout = int.Parse(jObj["ClotPlayTimeOutSec"].ToString());

                    api = new Gamess.ClotPlay.ClotPlay(apiUrl, operatorToken, agentCode, timeout);
                }
            }
        }

        public override vm.CheckAndCreateAccountVM CreatePlayer(string loginID, string loginPass, string fullName, string mcUID)
        {
            var res = new vm.CheckAndCreateAccountVM();

            try
            {
                var createMemberRequest = new Gamess.ClotPlay.Models.CreateMemberRequest
                {
                    player_unique_id = loginID,
                    player_name = fullName,
                    player_currency = currencyID,
                    agent_code = agentCode
                };

                if (api.CreateMember(loginID, fullName, currencyID))
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
            var depositRequest = new Gamess.ClotPlay.Models.WalletDepositRequest
            {
                player_unique_id = gameMap.GamePlayerID,
                player_currency = currencyID,
                amount = amount
            };

            transactionID = Utils.SubStringTrackerId(transactionID)+DateTime.Now.ToString("dd:HH:mm:ss");

            if (api.Deposit(gameMap.GamePlayerID, currencyID, transactionID, amount))
            {
                InsertLog(gameID, "Deposit", "request", JsonConvert.SerializeObject(depositRequest), "response", JsonConvert.SerializeObject(api.WalletTransactionResponse));
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
            var withdrawRequest = new Gamess.ClotPlay.Models.WalletWithdrawRequest
            {
                player_unique_id = gameMap.GamePlayerID,
                player_currency = currencyID,
                amount = amount
            };

            transactionID = Utils.SubStringTrackerId(transactionID) + DateTime.Now.ToString("dd:HH:mm:ss");

            if (api.Withdraw(gameMap.GamePlayerID, currencyID, transactionID, amount))
            {
                InsertLog(gameID, "Withdraw", "request", JsonConvert.SerializeObject(withdrawRequest), "response", JsonConvert.SerializeObject(api.WalletTransactionResponse));
                return true;
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
                if (api.GetMemberDetails(gameMap.GamePlayerID, currencyID))
                {
                    InsertLog(gameID, "GetBalance", "request", playerID, "response", JsonConvert.SerializeObject(api.WalletBalanceResponse));

                    res.PlayerID = playerID;
                    res.Balance = api.PlayerResponse.player.balance;
                    res.Game = gameID;
                    res.Message = "Successfully retrieved balance.";
                    res.Code = 0;
                }
                else
                {
                    InsertLog(gameID, "GetBalance", "request", playerID, "response", JsonConvert.SerializeObject(api.ErrorMessage));
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
            if (api.GetMemberDetails(gameMap.GamePlayerID, currencyID))
            {
                InsertLog(gameID, "GetGameBalance", "request", playerID, "response", JsonConvert.SerializeObject(api.WalletBalanceResponse));
                return api.PlayerResponse.player.balance;
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
                if (gameMap != null)
                {
                    string url = string.Format("{0}?playerid={1}&currency={2}&ip={3}&lang={4}&mobile={5}", lobbyUrl, playerID, "MYR", Utils.GetClientIp(), langId, isMobile.ToString().ToLower());
                    InsertLog(gameID, "GetGameUrl", "playerid", playerID, "currency", "MYR", "ip", Utils.GetClientIp(), "lang", langId, "url", url);
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
            var logoutRequest = new Gamess.ClotPlay.Models.MemberLogoutRequest
            {
                player_unique_id = gamePlayerId,
                player_currency = currencyID,
                agent_code = agentCode
            };

            if (api.Logout(gamePlayerId, currencyID, agentCode))
            {
                InsertLog(gameID, "KickPlayer", "request", JsonConvert.SerializeObject(logoutRequest), "response", JsonConvert.SerializeObject(api.PlayerResponse));
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

        public vm.GetGameUrlVM GetClotPlayGameUrl(string langId, string gameCode, bool IsMobile = false)
        {
            var res = new vm.GetGameUrlVM();

            try
            {
                if (gameMap != null)
                {
                    var sessionId = new Guid().ToString();
                    langId = GetGameLanguage(gameID, langId);
                    var apiCall = api.LaunchGame(gameMap.GamePlayerID, gameMap.PlayerID, gameMap.Currency, gameCode, false, langId, null, GameOrientation.landscape.ToString());
                    InsertLog(gameID, "UserAuth", "request", api.Request, "response", api.Response);

                    request = api.Request;
                    response = api.Response;

                    if (apiCall)
                    {
                        res.PlayerID = playerID;
                        res.Url = api.LaunchGameResponse.game_url;
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
                _common.LogError("UserAuth" + " " + DateTime.Now.ToString() + " " + string.Format("{0}", playerID) + " " + ex.Message.ToString(), gameID);

                var err = new Error
                {
                    ErrorCode = "999",
                    ErrorDesc = ex.Message.ToString()
                };

                InsertLog(gameID, "UserAuth_Exception", "Error", JsonConvert.SerializeObject(err));

                res.PlayerID = playerID;
                res.Url = null;
                res.Game = gameID;
                res.Message = ex.Message.ToString();
                res.Code = 99;
            }

            return res;
        }

        public override string RetryDeposit(decimal amount, string trackerId, ref Error error)
        {
            throw new NotImplementedException();
        }

        public override string RetryWithdraw(decimal amount, string trackerId, ref Error error)
        {
            throw new NotImplementedException();
        }

        public List<SupportedLanguage> GetLanauges()
        {
            var apiCall = api.GetLanguage();
            request = api.Request;
            response = api.Response;

            return null;
        }


        public List<ClotPlayLobbyGame> GetGameList()
        {
            api = new Gamess.ClotPlay.ClotPlay(apiUrl, operatorToken, agentCode, timeout);

            var apiCall = api.GetGameList();
            request = api.Request;
            response = api.Response;

            var data = api.GameListResponse
                .Select(x =>
                    new ClotPlayLobbyGame
                    {
                        Slug = x.slug,
                        Name = x.name,
                        NameCn = x.name_cn,
                        Category = x.category,
                        Description = x.description,
                        ThumbnailsCn = x.thumbnails.ContainsKey("cn") ? x.thumbnails["cn"].square.ToString() : x.thumbnails["square"].square.ToString(),
                        ThumbnailsEn = x.thumbnails.ContainsKey("en") ? x.thumbnails["en"].square.ToString() : x.thumbnails["square"].square.ToString(),
                        AllSeq = 1,
                        IsAll = true,
                        IsMaintenance = x.isMaintenance,
                        CreatedAt = DateTime.TryParse(x.created_at, out var createdAt) ? (DateTime?)createdAt : null,
                        UpdatedAt = DateTime.TryParse(x.updated_at, out var updatedAt) ? (DateTime?)updatedAt : null,
                        IsNewInGame = x.isNew,
                        IsPopularInGame = x.isPopular,
                        CreatedDate = DateTime.Now,
                        ModifiedDate = DateTime.Now
                    })
                .ToList();

            if (apiCall)
            {
                return data;
            }
            else
            {
                return null;
            }
        }
    }
}