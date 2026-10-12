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
using GameRecord.Models;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;

namespace PlayStar
{
    public class PlayStar : IDisposable
    {
        #region Declaration
        private string apiUrl { get; set; }
        private string hostId { get; set; }
        private string secretKey { get; set; }
        private string currency { get; set; }
        private int timeOutSec { get; set; }

        public CreatePlayerRes CreatePlayerRes { get; set; }
        public GetBalanceRes GetBalanceRes { get; set; }
        public DepositRes DepositRes { get; set; }
        public WithdrawRes WithdrawRes { get; set; }
        public WithdrawAllRes WithdrawAllRes { get; set; }
        public GetGameListRes GetGameListRes { get; set; }
        public GameLoginRes GameLoginRes { get; set; }

        public string Request { get; set; }
        public string Parameters { get; set; }
        public string Response { get; set; }
        public string ErrorMessage { get; set; }
        public string ExternalIP { get; set; }

        public PlayStar(string apiUrl, string hostId, string currency, int timeOutSec = 30000)
        {
            this.apiUrl = apiUrl;
            this.hostId = hostId;
            this.currency = currency;
            this.timeOutSec = timeOutSec;
        }
        #endregion

        #region General Functions
        public double GetTimeUtc0()
        {
            //return datetime as milisecond in int
            DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Local);
            long unixTime = (long)(DateTime.Now - new DateTime(1970, 1, 1)).TotalSeconds;
            return unixTime;
            //return ((DateTime.UtcNow.Ticks - new DateTime(1970, 1, 1).Ticks) / TimeSpan.TicksPerMillisecond).ToString();
            //return DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
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

        public IPAddress GetExternalIPAddress()
        {
            IPHostEntry myIPHostEntry = Dns.GetHostEntry(Dns.GetHostName());

            foreach (IPAddress myIPAddress in myIPHostEntry.AddressList)
            {
                byte[] ipBytes = myIPAddress.GetAddressBytes();

                if (myIPAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    if (!IsPrivateIP(myIPAddress))
                        return myIPAddress;
                }
            }

            return null;
        }

        public bool IsPrivateIP(IPAddress myIPAddress)
        {
            if (myIPAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                byte[] ipBytes = myIPAddress.GetAddressBytes();

                // 10.0.0.0/24 
                if (ipBytes[0] == 10)
                    return true;
                // 172.16.0.0/16
                else if (ipBytes[0] == 172 && ipBytes[1] == 16)
                    return true;
                // 192.168.0.0/16
                else if (ipBytes[0] == 192 && ipBytes[1] == 168)
                    return true;
                // 169.254.0.0/16
                else if (ipBytes[0] == 169 && ipBytes[1] == 254)
                    return true;
            }

            return false;
        }
        #endregion

        #region Api Functions
        public bool CreatePlayer(string memberId)
        {
            try
            {
                var client = new RestClient(apiUrl + "/funds/createplayer");
                //client.Timeout = timeOutSec;
                var r = new RestRequest(Method.GET);
                r.AddParameter("host_id", hostId);
                r.AddParameter("member_id", memberId);
                r.AddHeader("Content-Type", "application/json");

                Request = client.BuildUri(r).ToString();
                //ExternalIP = GetExternalIPAddress().ToString();
                IRestResponse response = client.Execute(r);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Response = response.Content;
                    CreatePlayerRes = JsonConvert.DeserializeObject<CreatePlayerRes>(response.Content);

                    if (CreatePlayerRes.status_code == 0)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = CreatePlayerRes.message;
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

        public bool GetBalance(string memberId)
        {
            try
            {
                var client = new RestClient(apiUrl + "/funds/getbalance");
               // client.Timeout = timeOutSec;
                var r = new RestRequest(Method.GET);
                r.AddParameter("host_id", hostId);
                r.AddParameter("member_id", memberId);
                r.AddHeader("Content-Type", "application/json");

                Request = client.BuildUri(r).ToString();
                //ExternalIP = GetExternalIPAddress().ToString();
                IRestResponse response = client.Execute(r);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Response = response.Content;
                    GetBalanceRes = JsonConvert.DeserializeObject<GetBalanceRes>(response.Content);

                    if (GetBalanceRes.status_code == 0)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = GetBalanceRes.message;
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

        public bool Deposit(string memberId, long amount)
        {
            try
            {
                double datetime = GetTimeUtc0();

                var client = new RestClient(apiUrl + "/funds/deposit");
                var r = new RestRequest(Method.GET);
                r.AddParameter("host_id", hostId);
                r.AddParameter("member_id", memberId);
                r.AddParameter("txn_id", datetime);
                r.AddParameter("amount", amount);
                r.AddHeader("Content-Type", "application/json");

                Request = client.BuildUri(r).ToString();
                //ExternalIP = GetExternalIPAddress().ToString();
                IRestResponse response = client.Execute(r);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Response = response.Content;
                    DepositRes = JsonConvert.DeserializeObject<DepositRes>(response.Content);

                    if (DepositRes.status_code == 0)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = DepositRes.message;
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

        public bool Withdraw(string memberId, long amount)
        {
            try
            {
                double datetime = GetTimeUtc0();

                var client = new RestClient(apiUrl + "/funds/withdraw");
               // client.Timeout = timeOutSec;
                var r = new RestRequest(Method.GET);
                r.AddParameter("host_id", hostId);
                r.AddParameter("member_id", memberId);
                r.AddParameter("txn_id", datetime);
                r.AddParameter("amount", amount);
                r.AddHeader("Content-Type", "application/json");

                Request = client.BuildUri(r).ToString();
                //ExternalIP = GetExternalIPAddress().ToString();
                IRestResponse response = client.Execute(r);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Response = response.Content;
                    WithdrawRes = JsonConvert.DeserializeObject<WithdrawRes>(response.Content);

                    if (WithdrawRes.status_code == 0)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = WithdrawRes.message;
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

        public bool WithdrawAll(string memberId)
        {
            try
            {
                double datetime = GetTimeUtc0();

                var client = new RestClient(apiUrl + "/funds/withdrawall");
               // client.Timeout = timeOutSec;
                var r = new RestRequest(Method.GET);
                r.AddParameter("host_id", hostId);
                r.AddParameter("member_id", memberId);
                r.AddParameter("txn_id", datetime);
                r.AddHeader("Content-Type", "application/json");

                Request = client.BuildUri(r).ToString();
                //ExternalIP = GetExternalIPAddress().ToString();
                IRestResponse response = client.Execute(r);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Response = response.Content;
                    WithdrawAllRes = JsonConvert.DeserializeObject<WithdrawAllRes>(response.Content);

                    if (WithdrawAllRes.status_code == 0)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = WithdrawAllRes.message;
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
                double datetime = GetTimeUtc0();

                var client = new RestClient(apiUrl + "/feed/gamelist");
                var r = new RestRequest(Method.GET);
                r.AddParameter("host_id", hostId);
                r.AddHeader("Content-Type", "application/json");

                Request = client.BuildUri(r).ToString();
                //ExternalIP = GetExternalIPAddress().ToString();
                IRestResponse response = client.Execute(r);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Response = response.Content;
                    GetGameListRes = JsonConvert.DeserializeObject<GetGameListRes>(response.Content);

                    if (GetGameListRes.ListGames.Count > 0)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = "Status Not 200";
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

        public bool GetGameUrl(string gameId, string lang, string accessToken)
        {
            try
            {
                var client = new RestClient(apiUrl + "/launch");
                var r = new RestRequest(Method.GET);
                r.AddParameter("host_id", hostId);
                r.AddParameter("game_id", gameId);
                r.AddParameter("access_token", accessToken);
                r.AddParameter("lang", lang);

                //ExternalIP = GetExternalIPAddress().ToString();

                GameLoginRes.url = client.BuildUri(r).ToString();
                return true;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                return false;
            }
        }

        //public bool PullLog()
        //{
        //    try
        //    {
        //        string datetime = GetTimeUtc0();

        //        var lPair = new List<PairVM>
        //        {
        //            new PairVM { Key = "OperatorId", Value = operatorId },
        //            new PairVM { Key = "RequestDateTime", Value = datetime },
        //            new PairVM { Key = "Signature", Value = MD5Hash("PullLog" + datetime + operatorId + secretKey) },
        //        };

        //        var client = new RestClient(apiUrl + "PullLog");
        //       // client.Timeout = timeOutSec;
        //        var r = new RestRequest(Method.POST);
        //        r.AddHeader("Content-Type", "application/json");
        //        dynamic o = new ExpandoObject();
        //        foreach (var P in lPair)
        //            ((IDictionary<string, object>)o).Add(P.Key, P.Value);
        //        var body = JsonConvert.SerializeObject(o);
        //        r.AddParameter("application/json", body, ParameterType.RequestBody);

        //        Request = client.BuildUri(r).ToString();
        //        Parameters = body;
        //        IRestResponse response = client.Execute(r);

        //        if (response.StatusCode == HttpStatusCode.OK)
        //        {
        //            Response = response.Content;
        //            PullLogRes = JsonConvert.DeserializeObject<PullLogRes>(response.Content);

        //            if (PullLogRes.Status == 200)
        //            {
        //                return true;
        //            }
        //            else
        //            {
        //                ErrorMessage = PullLogRes.Description;
        //                return false;
        //            }
        //        }
        //        else
        //        {
        //            ErrorMessage = response.StatusDescription;
        //            return false;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        ErrorMessage = ex.Message.ToString();
        //        return false;
        //    }
        //}

        //public bool FlagLog(List<long> Ids)
        //{
        //    try
        //    {
        //        string datetime = GetTimeUtc0();

        //        var lPair = new List<ObjectPairVM>
        //        {
        //            new ObjectPairVM { Key = "OperatorId", Object = operatorId },
        //            new ObjectPairVM { Key = "RequestDateTime", Object = datetime },
        //            new ObjectPairVM { Key = "Signature", Object = MD5Hash("FlagLog" + datetime + operatorId + secretKey) },
        //            new ObjectPairVM { Key = "TransactionIds", Object = Ids },
        //        };

        //        var client = new RestClient(apiUrl + "FlagLog");
        //       // client.Timeout = timeOutSec;
        //        var r = new RestRequest(Method.POST);
        //        r.AddHeader("Content-Type", "application/json");
        //        dynamic o = new ExpandoObject();
        //        foreach (var P in lPair)
        //            ((IDictionary<string, object>)o).Add(P.Key, P.Object);
        //        var body = JsonConvert.SerializeObject(o);
        //        r.AddParameter("application/json", body, ParameterType.RequestBody);

        //        Request = client.BuildUri(r).ToString();
        //        Parameters = body;
        //        IRestResponse response = client.Execute(r);

        //        if (response.StatusCode == HttpStatusCode.OK)
        //        {
        //            Response = response.Content;
        //            FlagLogRes = JsonConvert.DeserializeObject<FlagLogRes>(response.Content);

        //            if (FlagLogRes.Status == 200)
        //            {
        //                return true;
        //            }
        //            else
        //            {
        //                ErrorMessage = FlagLogRes.Description;
        //                return false;
        //            }
        //        }
        //        else
        //        {
        //            ErrorMessage = response.StatusDescription;
        //            return false;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        ErrorMessage = ex.Message.ToString();
        //        return false;
        //    }
        //}
        #endregion

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }

    #region models
    public class BaseRequest
    {
        public string host_id { get; set; }
    }

    public class BaseResponse
    {
        public int status_code { get; set; }
        public long balance { get; set; }
        public string message { get; set; }
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

    public class AuthenticateReq
    {
        public string accesss_token { get; set; }
    }

    public class AuthenticateRes
    {
        public int status_code { get; set; }
        public string member_id { get; set; }
        public string member_name { get; set; }
        public long balance { get; set; }
        public int type { get; set; }
    }

    public class CreatePlayerReq : BaseRequest
    {
        public string member_id { get; set; }
    }


    public class CreatePlayerRes : BaseResponse
    {
    }

    public class GetBalanceReq : BaseRequest
    {
        public string member_id { get; set; }
    }

    public class GetBalanceRes : BaseResponse
    {
    }

    public class DepositReq : BaseRequest
    {
        public string member_id { get; set; }
        public  long txn_id { get; set; }
        public long amount { get; set; }

    }

    public class DepositRes : BaseResponse
    {
    }

    public class WithdrawReq : BaseRequest
    {
        public string member_id { get; set; }
        public long txn_id { get; set; }
        public long amount { get; set; }

    }

    public class WithdrawRes : BaseResponse
    {
    }

    public class WithdrawAllReq : BaseRequest
    {
        public string member_id { get; set; }
        public long txn_id { get; set; }

    }

    public class WithdrawAllRes : BaseResponse
    {
    }

    public class GetGameListReq : BaseRequest
    {
    }

    public class GetGameListRes
    {
        public List<GetGameDetail> ListGames { get; set; }
    }   

    public class GetGameDetail
    {
        public string game_id { get; set; }
        public string type { get; set; }
        public string subgame_id { get; set; }
        public string group_id { get; set; }
        public string game_name { get; set; }
        public string jackpot_group { get; set; }
        public string category { get; set; }
        public string volatility { get; set; }
        public string feature { get; set; }
        public string commission_type { get; set; }
        public string min_bet { get; set; }
        public string max_bet { get; set; }
        public string mark { get; set; }
    }

    public class GameLoginReq : BaseRequest
    {
        public string game_id { get; set; }
        public string access_token { get; set; }
        public string lang { get; set; }
    }

    public class GameLoginRes
    {
        public string url { get; set; }
    }

    #endregion
}