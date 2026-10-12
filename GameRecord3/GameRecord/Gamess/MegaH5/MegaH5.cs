using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MegaH5
{
    public class MegaH5 : IDisposable
    {
        #region Declaration
        private string ApiUrl { get; set; }
        private string OperatorId { get; set; }
        private string SecretKey { get; set; }
        private string Currency { get; set; }
        private int TimeOutSec { get; set; }

        public string Url { get; set; }
        public string Request { get; set; }
        public string Response { get; set; }
        public string ErrorMessage { get; set; }

        public CreatePlayerRes CreatePlayerRes;
        public CheckBalanceRes CheckBalanceRes;
        public DepositRes DepositRes;
        public WithdrawRes WithdrawRes;
        public PullLogRes PullLogRes;
        public FlagLogRes FlagLogRes;
        public GameLoginRes GameLoginRes;

        public MegaH5(string ApiUrl, string OperatorId, string SecretKey, string Currency, int TimeOutSec = 30000)
        {
            this.ApiUrl = ApiUrl;
            this.OperatorId = OperatorId;
            this.SecretKey = SecretKey;
            this.Currency = Currency;
            this.TimeOutSec = TimeOutSec;
        }
        #endregion

        #region General Functions
        private string GetRequestDateTime()
        {
            return DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private string GetSignature(string stringToHash)
        {
            // Create an instance of the MD5 class
            MD5 md5 = MD5.Create();
            // Compute the hash as a byte array
            byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(stringToHash));
            // Convert the byte array to a hexadecimal string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("x2")); // "x2" formats each byte as a two-digit hexadecimal number
            }
            string hash = sb.ToString();

            return hash;
        }
        #endregion

        #region Api Functions
        public bool CreatePlayer(string PlayerId)
        {
            try
            {
                var RequestDateTime = GetRequestDateTime();

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("OperatorId", OperatorId);
                geek.Add("RequestDateTime", RequestDateTime);
                geek.Add("Signature", GetSignature("CreatePlayer" + RequestDateTime + OperatorId + SecretKey + PlayerId));
                geek.Add("PlayerId", PlayerId);

                Url = ApiUrl + "CreatePlayer";
                var client = new RestClient(Url);
                var r = new RestRequest(Method.POST);
                r.AddHeader("Content-Type", "application/json");
                r.AddJsonBody(geek);

                Request = JsonConvert.SerializeObject(geek);
                IRestResponse response = client.Execute(r);
                Response = response.Content;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    CreatePlayerRes = JsonConvert.DeserializeObject<CreatePlayerRes>(response.Content);
                    if (CreatePlayerRes.Status == 200)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = CreatePlayerRes.Status + ": " + CreatePlayerRes.Description;
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = response.StatusCode + ": " + response.StatusDescription;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                return false;
            }
        }

        public bool CheckBalance(string PlayerId)
        {
            try
            {
                var RequestDateTime = GetRequestDateTime();

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("OperatorId", OperatorId);
                geek.Add("RequestDateTime", RequestDateTime);
                geek.Add("Signature", GetSignature("CheckBalance" + RequestDateTime + OperatorId + SecretKey + PlayerId));
                geek.Add("PlayerId", PlayerId);

                Url = ApiUrl + "CheckBalance";
                var client = new RestClient(Url);
                var r = new RestRequest(Method.POST);
                r.AddHeader("Content-Type", "application/json");
                r.AddJsonBody(geek);

                Request = JsonConvert.SerializeObject(geek);
                IRestResponse response = client.Execute(r);
                Response = response.Content;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    CheckBalanceRes = JsonConvert.DeserializeObject<CheckBalanceRes>(response.Content);
                    if (CheckBalanceRes.Status == 200)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = CheckBalanceRes.Status + ": " + CheckBalanceRes.Description;
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = response.StatusCode + ": " + response.StatusDescription;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                return false;
            }
        }

        public bool Deposit(string PlayerId, decimal Amount)
        {
            try
            {
                var RequestDateTime = GetRequestDateTime();

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("OperatorId", OperatorId);
                geek.Add("RequestDateTime", RequestDateTime);
                geek.Add("Signature", GetSignature("Deposit" + RequestDateTime + OperatorId + SecretKey + PlayerId));
                geek.Add("PlayerId", PlayerId);
                geek.Add("Amount", Amount.ToString());
                geek.Add("ReferenceId", Guid.NewGuid().ToString());

                Url = ApiUrl + "Deposit";
                var client = new RestClient(Url);
                var r = new RestRequest(Method.POST);
                r.AddHeader("Content-Type", "application/json");
                r.AddJsonBody(geek);

                Request = JsonConvert.SerializeObject(geek);
                IRestResponse response = client.Execute(r);
                Response = response.Content;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    DepositRes = JsonConvert.DeserializeObject<DepositRes>(response.Content);
                    if (DepositRes.Status == 200)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = DepositRes.Status + ": " + DepositRes.Description;
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = response.StatusCode + ": " + response.StatusDescription;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                return false;
            }
        }

        public bool Withdraw(string PlayerId, decimal Amount)
        {
            try
            {
                var RequestDateTime = GetRequestDateTime();

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("OperatorId", OperatorId);
                geek.Add("RequestDateTime", RequestDateTime);
                geek.Add("Signature", GetSignature("Withdraw" + RequestDateTime + OperatorId + SecretKey + PlayerId));
                geek.Add("PlayerId", PlayerId);
                geek.Add("Amount", Amount.ToString());
                geek.Add("ReferenceId", Guid.NewGuid().ToString());

                Url = ApiUrl + "Withdraw";
                var client = new RestClient(Url);
                var r = new RestRequest(Method.POST);
                r.AddHeader("Content-Type", "application/json");
                r.AddJsonBody(geek);

                Request = JsonConvert.SerializeObject(geek);
                IRestResponse response = client.Execute(r);
                Response = response.Content;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    WithdrawRes = JsonConvert.DeserializeObject<WithdrawRes>(response.Content);
                    if (WithdrawRes.Status == 200)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = WithdrawRes.Status + ": " + WithdrawRes.Description;
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = response.StatusCode + ": " + response.StatusDescription;
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
                var RequestDateTime = GetRequestDateTime();

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("OperatorId", OperatorId);
                geek.Add("RequestDateTime", RequestDateTime);
                geek.Add("Signature", GetSignature("PullLog" + RequestDateTime + OperatorId + SecretKey));

                Url = ApiUrl + "PullLog";
                var client = new RestClient(Url);
                var r = new RestRequest(Method.POST);
                r.AddHeader("Content-Type", "application/json");
                r.AddJsonBody(geek);

                Request = JsonConvert.SerializeObject(geek);
                IRestResponse response = client.Execute(r);
                Response = response.Content;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    PullLogRes = JsonConvert.DeserializeObject<PullLogRes>(response.Content);
                    if (PullLogRes.Status == 200)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = PullLogRes.Status + ": " + PullLogRes.Description;
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = response.StatusCode + ": " + response.StatusDescription;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                return false;
            }
        }

        public bool FlagLog(List<long> TransactionIds)
        {
            try
            {
                var RequestDateTime = GetRequestDateTime();

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("OperatorId", OperatorId);
                geek.Add("RequestDateTime", RequestDateTime);
                geek.Add("Signature", GetSignature("FlagLog" + RequestDateTime + OperatorId + SecretKey));
                geek.Add("TransactionIds", TransactionIds);

                Url = ApiUrl + "FlagLog";
                var client = new RestClient(Url);
                var r = new RestRequest(Method.POST);
                r.AddHeader("Content-Type", "application/json");
                r.AddJsonBody(geek);

                Request = JsonConvert.SerializeObject(geek);
                IRestResponse response = client.Execute(r);
                Response = response.Content;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    FlagLogRes = JsonConvert.DeserializeObject<FlagLogRes>(response.Content);
                    if (FlagLogRes.Status == 200)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = FlagLogRes.Status + ": " + FlagLogRes.Description;
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = response.StatusCode + ": " + response.StatusDescription;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                return false;
            }
        }

        public bool GameLogin(string PlayerId, string Ip, string Lang)
        {
            try
            {
                int iOccurence = Ip.Split(':').Length - 1;
                if (iOccurence == 1)
                    Ip = Ip.Substring(0, Ip.IndexOf(":"));

                var RequestDateTime = GetRequestDateTime();

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("OperatorId", OperatorId);
                geek.Add("RequestDateTime", RequestDateTime);
                geek.Add("Signature", GetSignature("GameLogin" + RequestDateTime + OperatorId + SecretKey + PlayerId));
                geek.Add("PlayerId", PlayerId);
                geek.Add("Ip", Ip);
                geek.Add("GameCode", "0");
                //geek.Add("GameCode", null);
                //geek.Add("GameCode", 0);
                geek.Add("Currency", Currency);
                geek.Add("Lang", Lang);

                Url = ApiUrl + "GameLogin";
                var client = new RestClient(Url);
                var r = new RestRequest(Method.POST);
                r.AddHeader("Content-Type", "application/json");
                r.AddJsonBody(geek);

                Request = JsonConvert.SerializeObject(geek);
                IRestResponse response = client.Execute(r);
                Response = response.Content;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    GameLoginRes = JsonConvert.DeserializeObject<GameLoginRes>(response.Content);
                    if (GameLoginRes.Status == 200)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = GameLoginRes.Status + ": " + GameLoginRes.Description;
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = response.StatusCode + ": " + response.StatusDescription;
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

    #region models
    public class CreatePlayerRes
    {
        public int Status { get; set; }
        public string Description { get; set; }
    }

    public class CheckBalanceRes
    {
        public int Status { get; set; }
        public string Description { get; set; }
        public decimal CurrentBalance { get; set; }
    }

    public class DepositRes
    {
        public int Status { get; set; }
        public string Description { get; set; }
        public decimal CurrentBalance { get; set; }
    }

    public class WithdrawRes
    {
        public int Status { get; set; }
        public string Description { get; set; }
        public decimal CurrentBalance { get; set; }
    }

    public class PullLogRes
    {
        public int Status { get; set; }
        public string Description { get; set; }
        public List<PullLogRes_Log> Logs { get; set; }
    }

    public class PullLogRes_Log
    {
        public long TranId { get; set; }
        public string PlayerId { get; set; }
        public decimal? Turnover { get; set; }
        public decimal? ValidTurnover { get; set; }
        public decimal? Payout { get; set; }
        public decimal? WinLose { get; set; }
        public DateTime? TranDt { get; set; }
        public int? TranType { get; set; }
        public string GameCode { get; set; }
        public int? GameType { get; set; }
        public string GameName { get; set; }
    }

    public class FlagLogRes
    {
        public int Status { get; set; }
        public string Description { get; set; }
    }

    public class GameLoginRes
    {
        public int Status { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
    }
    #endregion
}