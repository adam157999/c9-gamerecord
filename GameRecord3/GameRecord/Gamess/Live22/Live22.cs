using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using RestSharp;
using System.Net.Http;
using System.Xml.Linq;
using System.Dynamic;
using System.Web;

namespace GameRecord.Gamess.Live22
{
    public class Live22 : IDisposable
    {
        #region Declaration
        private string apiUrl { get; set; }
        private string operatorId { get; set; }
        private string operatorPrefix { get; set; }
        private string secretKey { get; set; }
        private string currency { get; set; }
        private int timeOutSec { get; set; }

        public CreatePlayerRes CreatePlayerRes { get; set; }
        public CheckBalanceRes CheckBalanceRes { get; set; }
        public DepositRes DepositRes { get; set; }
        public WithdrawRes WithdrawRes { get; set; }
        public GetGameListRes GetGameListRes { get; set; }
        public GameLoginRes GameLoginRes { get; set; }
        public PullLogRes PullLogRes { get; set; }
        public FlagLogRes FlagLogRes { get; set; }

        public string Request { get; set; }
        public string Parameters { get; set; }
        public string Response { get; set; }
        public string ErrorMessage { get; set; }

        public Live22(string apiUrl, string operatorId, string operatorPrefix, string secretKey, string currency, int timeOutSec = 30000)
        {
            this.apiUrl = apiUrl;
            this.operatorId = operatorId;
            this.operatorPrefix = operatorPrefix;
            this.secretKey = secretKey;
            this.currency = currency;
            this.timeOutSec = timeOutSec;
        }
        #endregion

        #region General Functions
        public string GetTimeUtc0()
        {
            return DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public string MD5Hash(string input)
        {
            StringBuilder hash = new StringBuilder();
            MD5CryptoServiceProvider md5provider = new MD5CryptoServiceProvider();
            byte[] bytes = md5provider.ComputeHash(new UTF8Encoding().GetBytes(input));

            for (int i = 0; i < bytes.Length; i++)
            {
                hash.Append(bytes[i].ToString("x2"));
            }
            return hash.ToString();
        }
        #endregion

        #region Api Functions
        public bool CreatePlayer(string playerId)
        {
            try
            {
                string datetime = GetTimeUtc0();

                var lPair = new List<PairVM>
                {
                    new PairVM { Key = "OperatorId", Value = operatorId },
                    new PairVM { Key = "RequestDateTime", Value = datetime },
                    new PairVM { Key = "Signature", Value = MD5Hash("CreatePlayer" + datetime + operatorId + secretKey + playerId) },
                    new PairVM { Key = "PlayerId", Value = playerId },
                };

                var client = new RestClient(apiUrl + "CreatePlayer");
                client.Timeout = -1;
                var r = new RestRequest(Method.POST);
                r.AddHeader("Content-Type", "application/json");
                dynamic o = new ExpandoObject();
                foreach (var P in lPair)
                    ((IDictionary<string, object>)o).Add(P.Key, P.Value);
                var body = JsonConvert.SerializeObject(o);
                r.AddParameter("application/json", body, ParameterType.RequestBody);

                Request = client.BuildUri(r).ToString();
                Parameters = body;
                IRestResponse response = client.Execute(r);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Response = response.Content;
                    CreatePlayerRes = JsonConvert.DeserializeObject<CreatePlayerRes>(response.Content);

                    if (CreatePlayerRes.Status == 200)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = CreatePlayerRes.Description;
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = response.StatusDescription;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                return false;
            }
        }

        public bool CheckBalance(string playerId)
        {
            try
            {
                string datetime = GetTimeUtc0();

                var lPair = new List<PairVM>
                {
                    new PairVM { Key = "OperatorId", Value = operatorId },
                    new PairVM { Key = "RequestDateTime", Value = datetime },
                    new PairVM { Key = "Signature", Value = MD5Hash("CheckBalance" + datetime + operatorId + secretKey + playerId) },
                    new PairVM { Key = "PlayerId", Value = playerId },
                };

                var client = new RestClient(apiUrl + "CheckBalance");
                client.Timeout = timeOutSec;
                var r = new RestRequest(Method.POST);
                r.AddHeader("Content-Type", "application/json");
                dynamic o = new ExpandoObject();
                foreach (var P in lPair)
                    ((IDictionary<string, object>)o).Add(P.Key, P.Value);
                var body = JsonConvert.SerializeObject(o);
                r.AddParameter("application/json", body, ParameterType.RequestBody);

                Request = client.BuildUri(r).ToString();
                Parameters = body;
                IRestResponse response = client.Execute(r);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Response = response.Content;
                    CheckBalanceRes = JsonConvert.DeserializeObject<CheckBalanceRes>(response.Content);

                    if (CheckBalanceRes.Status == 200)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = CheckBalanceRes.Description;
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = response.StatusDescription;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                return false;
            }
        }

        public bool Deposit(string playerId, decimal amount)
        {
            try
            {
                string datetime = GetTimeUtc0();

                var lPair = new List<PairVM>
                {
                    new PairVM { Key = "OperatorId", Value = operatorId },
                    new PairVM { Key = "RequestDateTime", Value = datetime },
                    new PairVM { Key = "Signature", Value = MD5Hash("Deposit" + datetime + operatorId + secretKey + playerId) },
                    new PairVM { Key = "PlayerId", Value = playerId },
                    new PairVM { Key = "Amount", Value = amount.ToString() },
                    new PairVM { Key = "ReferenceId", Value = Guid.NewGuid().ToString() },
                };

                var client = new RestClient(apiUrl + "Deposit");
                client.Timeout = timeOutSec;
                var r = new RestRequest(Method.POST);
                r.AddHeader("Content-Type", "application/json");
                dynamic o = new ExpandoObject();
                foreach (var P in lPair)
                    ((IDictionary<string, object>)o).Add(P.Key, P.Value);
                var body = JsonConvert.SerializeObject(o);
                r.AddParameter("application/json", body, ParameterType.RequestBody);

                Request = client.BuildUri(r).ToString();
                Parameters = body;
                IRestResponse response = client.Execute(r);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Response = response.Content;
                    DepositRes = JsonConvert.DeserializeObject<DepositRes>(response.Content);

                    if (DepositRes.Status == 200)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = DepositRes.Description;
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = response.StatusDescription;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                return false;
            }
        }

        public bool Withdraw(string playerId, decimal amount)
        {
            try
            {
                string datetime = GetTimeUtc0();

                var lPair = new List<PairVM>
                {
                    new PairVM { Key = "OperatorId", Value = operatorId },
                    new PairVM { Key = "RequestDateTime", Value = datetime },
                    new PairVM { Key = "Signature", Value = MD5Hash("Withdraw" + datetime + operatorId + secretKey + playerId) },
                    new PairVM { Key = "PlayerId", Value = playerId },
                    new PairVM { Key = "Amount", Value = amount.ToString() },
                    new PairVM { Key = "ReferenceId", Value = Guid.NewGuid().ToString() },
                };

                var client = new RestClient(apiUrl + "Withdraw");
                client.Timeout = timeOutSec;
                var r = new RestRequest(Method.POST);
                r.AddHeader("Content-Type", "application/json");
                dynamic o = new ExpandoObject();
                foreach (var P in lPair)
                    ((IDictionary<string, object>)o).Add(P.Key, P.Value);
                var body = JsonConvert.SerializeObject(o);
                r.AddParameter("application/json", body, ParameterType.RequestBody);

                Request = client.BuildUri(r).ToString();
                Parameters = body;
                IRestResponse response = client.Execute(r);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Response = response.Content;
                    WithdrawRes = JsonConvert.DeserializeObject<WithdrawRes>(response.Content);

                    if (WithdrawRes.Status == 200)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = WithdrawRes.Description;
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = response.StatusDescription;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                return false;
            }
        }

        public bool GetGameList()
        {
            try
            {
                string datetime = GetTimeUtc0();

                var lPair = new List<PairVM>
                {
                    new PairVM { Key = "OperatorId", Value = operatorId },
                    new PairVM { Key = "RequestDateTime", Value = datetime },
                    new PairVM { Key = "Signature", Value = MD5Hash("GetGameList" + datetime + operatorId + secretKey) },
                };

                var client = new RestClient(apiUrl + "GetGameList");
                client.Timeout = timeOutSec;
                var r = new RestRequest(Method.POST);
                r.AddHeader("Content-Type", "application/json");
                dynamic o = new ExpandoObject();
                foreach (var P in lPair)
                    ((IDictionary<string, object>)o).Add(P.Key, P.Value);
                var body = JsonConvert.SerializeObject(o);
                r.AddParameter("application/json", body, ParameterType.RequestBody);

                Request = client.BuildUri(r).ToString();
                Parameters = body;
                IRestResponse response = client.Execute(r);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Response = response.Content;
                    GetGameListRes = JsonConvert.DeserializeObject<GetGameListRes>(response.Content);

                    if (GetGameListRes.Status == 200)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = GetGameListRes.Description;
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = response.StatusDescription;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                return false;
            }
        }

        public bool GameLogin(string playerId, string ip, string lang, string gameCode)
        {
            try
            {
                string datetime = GetTimeUtc0();

                var lPair = new List<PairVM>
                {
                    new PairVM { Key = "OperatorId", Value = operatorId },
                    new PairVM { Key = "RequestDateTime", Value = datetime },
                    new PairVM { Key = "Signature", Value = MD5Hash("GameLogin" + datetime + operatorId + secretKey + playerId) },
                    new PairVM { Key = "PlayerId", Value = playerId },
                    new PairVM { Key = "Ip", Value = ip },
                    new PairVM { Key = "GameCode", Value = gameCode },
                    new PairVM { Key = "Currency", Value = currency },
                    new PairVM { Key = "Lang", Value = lang },
                };

                var client = new RestClient(apiUrl + "GameLogin");
                client.Timeout = timeOutSec;
                var r = new RestRequest(Method.POST);
                r.AddHeader("Content-Type", "application/json");
                dynamic o = new ExpandoObject();
                foreach (var P in lPair)
                    ((IDictionary<string, object>)o).Add(P.Key, P.Value);
                var body = JsonConvert.SerializeObject(o);
                r.AddParameter("application/json", body, ParameterType.RequestBody);

                Request = client.BuildUri(r).ToString();
                Parameters = body;
                IRestResponse response = client.Execute(r);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Response = response.Content;
                    GameLoginRes = JsonConvert.DeserializeObject<GameLoginRes>(response.Content);

                    if (GameLoginRes.Status == 200)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = GameLoginRes.Description;
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = response.StatusDescription;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                return false;
            }
        }

        public bool PullLog()
        {
            try
            {
                string datetime = GetTimeUtc0();

                var lPair = new List<PairVM>
                {
                    new PairVM { Key = "OperatorId", Value = operatorId },
                    new PairVM { Key = "RequestDateTime", Value = datetime },
                    new PairVM { Key = "Signature", Value = MD5Hash("PullLog" + datetime + operatorId + secretKey) },
                };

                var client = new RestClient(apiUrl + "PullLog");
                client.Timeout = timeOutSec;
                var r = new RestRequest(Method.POST);
                r.AddHeader("Content-Type", "application/json");
                dynamic o = new ExpandoObject();
                foreach (var P in lPair)
                    ((IDictionary<string, object>)o).Add(P.Key, P.Value);
                var body = JsonConvert.SerializeObject(o);
                r.AddParameter("application/json", body, ParameterType.RequestBody);

                Request = client.BuildUri(r).ToString();
                Parameters = body;
                IRestResponse response = client.Execute(r);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Response = response.Content;
                    PullLogRes = JsonConvert.DeserializeObject<PullLogRes>(response.Content);

                    if (PullLogRes.Status == 200)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = PullLogRes.Description;
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = response.StatusDescription;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                return false;
            }
        }

        public bool FlagLog(List<long> Ids)
        {
            try
            {
                string datetime = GetTimeUtc0();

                var lPair = new List<ObjectPairVM>
                {
                    new ObjectPairVM { Key = "OperatorId", Object = operatorId },
                    new ObjectPairVM { Key = "RequestDateTime", Object = datetime },
                    new ObjectPairVM { Key = "Signature", Object = MD5Hash("FlagLog" + datetime + operatorId + secretKey) },
                    new ObjectPairVM { Key = "TransactionIds", Object = Ids },
                };

                var client = new RestClient(apiUrl + "FlagLog");
                client.Timeout = timeOutSec;
                var r = new RestRequest(Method.POST);
                r.AddHeader("Content-Type", "application/json");
                dynamic o = new ExpandoObject();
                foreach (var P in lPair)
                    ((IDictionary<string, object>)o).Add(P.Key, P.Object);
                var body = JsonConvert.SerializeObject(o);
                r.AddParameter("application/json", body, ParameterType.RequestBody);

                Request = client.BuildUri(r).ToString();
                Parameters = body;
                IRestResponse response = client.Execute(r);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Response = response.Content;
                    FlagLogRes = JsonConvert.DeserializeObject<FlagLogRes>(response.Content);

                    if (FlagLogRes.Status == 200)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = FlagLogRes.Description;
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = response.StatusDescription;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                return false;
            }
        }
        #endregion

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }

    //models
    public class BaseRequest
    {
        public string serialNo { get; set; }
        public string merchantCode { get; set; }
        internal string Method { get; set; }
    }

    public class BaseResponse
    {
        public string serialNo { get; set; }
        public int code { get; set; }
        public string msg { get; set; }
        public string merchantCode { get; set; }
    }

    public class PairVM
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public class ObjectPairVM
    {
        public string Key { get; set; }
        public object Object { get; set; }
    }


    public class CreatePlayerRes
    {
        public int Status { get; set; }
        public string Description { get; set; }
        public string ResponseDateTime { get; set; }
    }

    public class CheckBalanceRes
    {
        public int Status { get; set; }
        public string Description { get; set; }
        public string ResponseDateTime { get; set; }
        public decimal CurrentBalance { get; set; }
        public string Currency { get; set; }
    }

    public class DepositRes
    {
        public int Status { get; set; }
        public string Description { get; set; }
        public string ResponseDateTime { get; set; }
        public decimal CurrentBalance { get; set; }
        public string Currency { get; set; }
    }

    public class WithdrawRes
    {
        public int Status { get; set; }
        public string Description { get; set; }
        public string ResponseDateTime { get; set; }
        public decimal CurrentBalance { get; set; }
        public string Currency { get; set; }
    }

    public class GetGameListRes
    {
        public int Status { get; set; }
        public string Description { get; set; }
        public string ResponseDateTime { get; set; }
        public List<GetGameListRes_Game> Game { get; set; }
    }

    public class GetGameListRes_Game
    {
        public string GameCode { get; set; }
        public int GameType { get; set; }
        public string GameName { get; set; }
        public string ImageUrl { get; set; }
        public bool HasDemo { get; set; }
    }

    public class GameLoginRes
    {
        public int Status { get; set; }
        public string Description { get; set; }
        public string ResponseDateTime { get; set; }
        public string Url { get; set; }
    }

    //refer 

    public class PullLogRes
    {
        public int Status { get; set; }
        public string Description { get; set; }
        public string ResponseDateTime { get; set; }
        public List<PullLogRes_Log> Logs { get; set; }
    }

    public class PullLogRes_Log
    {
        public long TranId { get; set; }
        public string PlayerId { get; set; }
        public decimal Turnover { get; set; }
        public decimal ValidTurnover { get; set; }
        public decimal Payout { get; set; }
        public decimal WinLose { get; set; }
        public DateTime TranDt { get; set; }
        public int TranType { get; set; }
        public string GameCode { get; set; }
        public int GameType { get; set; }
        public string GameName { get; set; }
    }

    public class FlagLogRes
    {
        public int Status { get; set; }
        public string Description { get; set; }
        public string ResponseDateTime { get; set; }
    }
}