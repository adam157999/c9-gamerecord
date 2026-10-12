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

namespace GameRecord.Gamess.WCasino
{
    public class WCasino : IDisposable
    {
        #region Declaration
        private string apiurl { get; set; }
        private string appid { get; set; }
        private string key { get; set; }
        private string qids { get; set; }
        private string currency { get; set; }
        private int timeOutSec { get; set; }

        public GetGameListRes GetGameListRes { get; set; }
        public LoginRes LoginRes { get; set; }
        public KickRes KickRes { get; set; }
        public UserBalanceRes UserBalanceRes { get; set; }
        public DepositWithdrawRes DepositWithdrawRes { get; set; }
        public BetsDetailRes BetsDetailRes { get; set; }
        public BetLimitInterfaceRes BetLimitInterfaceRes { get; set; }
        public BetLimitSetRes BetLimitSetRes { get; set; }

        public string Request { get; set; }
        public string Parameters { get; set; }
        public string Response { get; set; }
        public string ErrorMessage { get; set; }

        public WCasino(string apiurl, string appid, string key, string qids, string currency, int timeOutSec = 30000)
        {
            this.apiurl = apiurl;
            this.appid = appid;
            this.key = key;
            this.qids = qids;
            this.currency = currency;
            this.timeOutSec = timeOutSec;
        }
        #endregion

        #region General Functions
        private string GetSignature(SortedDictionary<string, string> geek, string secretKey)
        {
            List<string> list = new List<string>();
            foreach (KeyValuePair<string, string> kvp in geek)
            {
                string key = kvp.Key.ToLower();
                string value = kvp.Value;
                list.Add(key + "=" + value);
            }
            string raw_data = string.Join("&", list.ToArray()) + "&key=" + secretKey;
            using (MD5 md5 = MD5.Create())
            {
                byte[] rawBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(raw_data));
                return BitConverter.ToString(rawBytes).Replace("-", "").ToLower();
            }
        }
        #endregion

        #region Api Functions
        public bool GetGameList(string username)
        {
            try
            {
                SortedDictionary<string, string> geek = new SortedDictionary<string, string>();
                geek.Add("appid", appid);
                geek.Add("username", username);
                var signature = GetSignature(geek, key);

                var list = new List<string>();
                foreach (var g in geek)
                    list.Add(g.Key.ToLower() + "=" + HttpUtility.UrlEncode(g.Value));
                list.Add("sign=" + signature);
                var query = "?" + string.Join("&", list.ToArray());

                var client = new RestClient(apiurl + "/api/data/getgames" + query);
                client.Timeout = -1;
                var r = new RestRequest(Method.GET);
                r.AddHeader("Content-Type", "application/json");

                Request = client.BuildUri(r).ToString();
                Parameters = query;
                IRestResponse response = client.Execute(r);
                Response = response.Content;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    GetGameListRes = JsonConvert.DeserializeObject<GetGameListRes>(response.Content);
                    if (GetGameListRes.result == 0)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = "Error Code: " + GetGameListRes.result;
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

        public bool Login(string username, int clienttype = 2, int language = 1)
        {
            try
            {
                SortedDictionary<string, string> geek = new SortedDictionary<string, string>();
                geek.Add("appid", appid);
                geek.Add("clienttype", clienttype.ToString());
                geek.Add("iscreate", "1");
                geek.Add("language", language.ToString());
                geek.Add("username", username);
                geek.Add("nickname", username);
                geek.Add("currency", currency);
                var signature = GetSignature(geek, key);

                var list = new List<string>();
                foreach (var g in geek)
                    list.Add(g.Key.ToLower() + "=" + HttpUtility.UrlEncode(g.Value));
                list.Add("sign=" + signature);
                var query = "?" + string.Join("&", list.ToArray());

                var client = new RestClient(apiurl + "/api/login" + query);
                client.Timeout = -1;
                var r = new RestRequest(Method.POST);
                r.AddHeader("Content-Type", "application/json");

                Request = client.BuildUri(r).ToString();
                Parameters = query;
                IRestResponse response = client.Execute(r);
                Response = response.Content;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    LoginRes = JsonConvert.DeserializeObject<LoginRes>(response.Content);
                    if (LoginRes.result == 0)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = LoginRes.desc;
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

        public bool Kick(string username)
        {
            try
            {
                SortedDictionary<string, string> geek = new SortedDictionary<string, string>();
                geek.Add("appid", appid);
                geek.Add("username", username);
                var signature = GetSignature(geek, key);

                var list = new List<string>();
                foreach (var g in geek)
                    list.Add(g.Key.ToLower() + "=" + HttpUtility.UrlEncode(g.Value));
                list.Add("sign=" + signature);
                var query = "?" + string.Join("&", list.ToArray());

                var client = new RestClient(apiurl + "/api/kickout" + query);
                client.Timeout = -1;
                var r = new RestRequest(Method.POST);
                r.AddHeader("Content-Type", "application/json");

                Request = client.BuildUri(r).ToString();
                Parameters = query;
                IRestResponse response = client.Execute(r);
                Response = response.Content;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    KickRes = JsonConvert.DeserializeObject<KickRes>(response.Content);
                    if (KickRes.result == 0)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = KickRes.desc;
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

        public bool GetUserBalance(string username)
        {
            try
            {
                SortedDictionary<string, string> geek = new SortedDictionary<string, string>();
                geek.Add("appid", appid);
                geek.Add("username", username);
                var signature = GetSignature(geek, key);

                var list = new List<string>();
                foreach (var g in geek)
                    list.Add(g.Key.ToLower() + "=" + HttpUtility.UrlEncode(g.Value));
                list.Add("sign=" + signature);
                var query = "?" + string.Join("&", list.ToArray());

                var client = new RestClient(apiurl + "/api/user/balance" + query);
                client.Timeout = -1;
                var r = new RestRequest(Method.GET);
                r.AddHeader("Content-Type", "application/json");

                Request = client.BuildUri(r).ToString();
                Parameters = query;
                IRestResponse response = client.Execute(r);
                Response = response.Content;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    UserBalanceRes = JsonConvert.DeserializeObject<UserBalanceRes>(response.Content);
                    if (UserBalanceRes.result == 0)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = "Error Code: " + UserBalanceRes.result;
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

        public bool DepositAndWithdraw(string username, decimal amount) //+ve=deposit, -ve=withdraw
        {
            try
            {
                SortedDictionary<string, string> geek = new SortedDictionary<string, string>();
                geek.Add("appid", appid);
                geek.Add("username", username);
                geek.Add("amount", amount.ToString());
                var signature = GetSignature(geek, key);

                var list = new List<string>();
                foreach (var g in geek)
                    list.Add(g.Key.ToLower() + "=" + HttpUtility.UrlEncode(g.Value));
                list.Add("sign=" + signature);
                var query = "?" + string.Join("&", list.ToArray());

                var client = new RestClient(apiurl + "/api/user/dw" + query);
                client.Timeout = -1;
                var r = new RestRequest(Method.POST);
                r.AddHeader("Content-Type", "application/json");

                Request = client.BuildUri(r).ToString();
                Parameters = query;
                IRestResponse response = client.Execute(r);
                Response = response.Content;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    DepositWithdrawRes = JsonConvert.DeserializeObject<DepositWithdrawRes>(response.Content);
                    if (DepositWithdrawRes.result == 0)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = DepositWithdrawRes.desc;
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

        public bool GetBetsDetail(int begintime, int endtime, int size = 2000, int page = 0) //Timestamp，second unit
        {
            try
            {
                SortedDictionary<string, string> geek = new SortedDictionary<string, string>();
                geek.Add("appid", appid);
                geek.Add("begintime", begintime.ToString());
                geek.Add("endtime", endtime.ToString());
                geek.Add("index", page.ToString());
                geek.Add("size", size.ToString());
                var signature = GetSignature(geek, key);

                var list = new List<string>();
                foreach (var g in geek)
                    list.Add(g.Key.ToLower() + "=" + HttpUtility.UrlEncode(g.Value));
                list.Add("sign=" + signature);
                var query = "?" + string.Join("&", list.ToArray());

                var client = new RestClient(apiurl + "/api/record/bets/detail" + query);
                client.Timeout = -1;
                var r = new RestRequest(Method.GET);
                r.AddHeader("Content-Type", "application/json");

                Request = client.BuildUri(r).ToString();
                Parameters = query;
                IRestResponse response = client.Execute(r);
                Response = response.Content;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    BetsDetailRes = JsonConvert.DeserializeObject<BetsDetailRes>(response.Content);
                    if (BetsDetailRes.result == 0)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = "Error code: " + BetsDetailRes.result;
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

        public bool BetLimitInterface()
        {
            try
            {
                SortedDictionary<string, string> geek = new SortedDictionary<string, string>();
                geek.Add("appid", appid);
                var signature = GetSignature(geek, key);

                var list = new List<string>();
                foreach (var g in geek)
                    list.Add(g.Key.ToLower() + "=" + HttpUtility.UrlEncode(g.Value));
                list.Add("sign=" + signature);
                var query = "?" + string.Join("&", list.ToArray());

                var client = new RestClient(apiurl + "/api/quota/list" + query);
                client.Timeout = -1;
                var r = new RestRequest(Method.GET);
                r.AddHeader("Content-Type", "application/json");

                Request = client.BuildUri(r).ToString();
                Parameters = query;
                IRestResponse response = client.Execute(r);
                Response = response.Content;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    BetLimitInterfaceRes = JsonConvert.DeserializeObject<BetLimitInterfaceRes>(response.Content);
                    if (BetLimitInterfaceRes.result == 0)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = BetLimitInterfaceRes.desc;
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

        public bool BetLimitSet(string username)
        {
            try
            {
                SortedDictionary<string, string> geek = new SortedDictionary<string, string>();
                geek.Add("appid", appid);
                geek.Add("username", username);
                geek.Add("qids", qids);
                var signature = GetSignature(geek, key);

                var list = new List<string>();
                foreach (var g in geek)
                    list.Add(g.Key.ToLower() + "=" + HttpUtility.UrlEncode(g.Value));
                list.Add("sign=" + signature);
                var query = "?" + string.Join("&", list.ToArray());

                var client = new RestClient(apiurl + "/api/quota/set" + query);
                client.Timeout = -1;
                var r = new RestRequest(Method.POST);
                r.AddHeader("Content-Type", "application/json");

                Request = client.BuildUri(r).ToString();
                Parameters = query;
                IRestResponse response = client.Execute(r);
                Response = response.Content;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    BetLimitSetRes = JsonConvert.DeserializeObject<BetLimitSetRes>(response.Content);
                    if (BetLimitSetRes.result == 0)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = BetLimitSetRes.desc;
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

    #region models
    public class ErrorRes
    {
        public int result { get; set; }
        public string desc { get; set; }
    }

    public class GetGameListRes
    {
        public int result { get; set; }
        public int arraysize { get; set; }
        public List<GetGameListRes_Row> array { get; set; }
    }

    public class GetGameListRes_Row
    {
        public long id { get; set; }
        public int gametype { get; set; }
        public string title { get; set; }
        public int state { get; set; }
        public int betstate { get; set; }
        public int rank { get; set; }
        public int updatetime { get; set; }
    }

    public class LoginRes
    {
        public int result { get; set; }
        public string desc { get; set; }
        public string openurl { get; set; }
    }

    public class KickRes
    {
        public int result { get; set; }
        public string desc { get; set; }
    }

    public class UserBalanceRes
    {
        public int result { get; set; }
        public decimal balance { get; set; }
        public bool isonline { get; set; }
        public bool isplaying { get; set; }
    }

    public class DepositWithdrawRes
    {
        public int result { get; set; }
        public string desc { get; set; }
        public long? orderno { get; set; }
        public decimal balance { get; set; }
    }

    public class BetsDetailRes
    {
        public int result { get; set; }
        public int arraysize { get; set; }
        public List<BetsDetailRes_Bet> array { get; set; }
    }

    public class BetsDetailRes_Bet
    {
        public long id { get; set; }
        public long serialnumber { get; set; }
        public long userid { get; set; }
        public string username { get; set; }
        public long gameid { get; set; }
        public long gameroundid { get; set; }
        public long gameroundno { get; set; }
        public string gameresult { get; set; }
        public int state { get; set; }
        public int bettype { get; set; }
        public string betpoint { get; set; }
        public decimal betamount { get; set; }
        public decimal winlost { get; set; }
        public decimal commamount { get; set; }
        public string commratio { get; set; }
        public decimal balance { get; set; }
        public decimal betodds { get; set; }
        public DateTime updatetime { get; set; }
        public DateTime createtime { get; set; }
    }

    public class BetLimitInterfaceRes
    {
        public int result { get; set; }
        public string desc { get; set; }
        public List<BetLimitInterfaceRes_Quota> array { get; set; }
        public int arraysize { get; set; }
    }

    public class BetLimitInterfaceRes_Quota
    {
        public int ID { get; set; }
        public string title { get; set; }
        public decimal up { get; set; }
        public decimal down { get; set; }
    }

    public class BetLimitSetRes
    {
        public int result { get; set; }
        public string desc { get; set; }
    }
    #endregion
}