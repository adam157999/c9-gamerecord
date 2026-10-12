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

namespace GameRecord.Games.VBoss
{
    public class VBoss : IDisposable
    {
        #region Declaration
        private string apiUrl { get; set; }
        private string apiUser { get; set; }
        private string apiPass { get; set; }
        private string user { get; set; }
        private string pass { get; set; }
        private string currency { get; set; }
        private int timeOutSec { get; set; }

        public CreatePlayerRes CreatePlayerRes { get; set; }
        public GetProfileRes GetProfileRes { get; set; }
        public DepositRes DepositRes { get; set; }
        public WithdrawRes WithdrawRes { get; set; }
        public BetLoginRes BetLoginRes { get; set; }
        public List<BetHistoryRes> lBetHistoryRes { get; set; }
        public WinningNumberRes WinningNumberRes { get; set; }

        public string Request { get; set; }
        public string Response { get; set; }
        public string ErrorMessage { get; set; }
        public string Request1 { get; set; }
        public string Response1 { get; set; }
        public string ErrorMessage1 { get; set; }

        public VBoss(string apiUrl, string apiUser, string apiPass, string user, string pass, string currency, int timeOutSec = 30)
        {
            this.apiUrl = apiUrl;
            this.apiUser = apiUser;
            this.apiPass = apiPass;
            this.user = user;
            this.pass = pass;
            this.currency = currency;
            this.timeOutSec = timeOutSec;
        }
        #endregion

        #region Api Functions
        public bool CreatePlayer(string loginId, string loginPass, string fullName)
        {
            try
            {
                var client = new RestClient(apiUrl);
                client.Timeout = timeOutSec;
                //var r = new RestRequest(Method.GET);
                var r = new RestRequest("createPlayer", Method.GET);
                r.AddParameter("apiUser", apiUser);
                r.AddParameter("apiPass", apiPass);
                r.AddParameter("user", user);
                r.AddParameter("pass", pass);
                r.AddParameter("loginId", loginId);
                r.AddParameter("loginPass", loginPass);
                r.AddParameter("fullName", fullName);
                r.AddParameter("currency", currency);

                Request = client.BuildUri(r).ToString();
                IRestResponse response = client.Execute(r);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Response = response.Content;
                    CreatePlayerRes = JsonConvert.DeserializeObject<CreatePlayerRes>(response.Content);
                    if (CreatePlayerRes.status == "1")
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = CreatePlayerRes.errorMessage;
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = (response.StatusDescription + "\n" + response.ErrorMessage)?.Trim();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                return false;
            }
        }

        public bool GetProfile(string loginId)
        {
            try
            {
                var client = new RestClient(apiUrl + "getProfile");
                client.Timeout = timeOutSec;
                var r = new RestRequest(Method.GET);
                r.AddParameter("apiUser", apiUser);
                r.AddParameter("apiPass", apiPass);
                r.AddParameter("user", user);
                r.AddParameter("pass", pass);
                r.AddParameter("loginId", loginId);

                Request = client.BuildUri(r).ToString();
                IRestResponse response = client.Execute(r);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Response = response.Content;
                    GetProfileRes = JsonConvert.DeserializeObject<GetProfileRes>(response.Content);
                    if (GetProfileRes.status == "A")
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = "Error: Status " + GetProfileRes.status;
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = (response.StatusDescription + "\n" + response.ErrorMessage)?.Trim();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                return false;
            }
        }

        public bool Deposit(string loginId, decimal amount)
        {
            try
            {
                var client = new RestClient(apiUrl + "deposit");
                client.Timeout = timeOutSec;
                var r = new RestRequest(Method.GET);
                r.AddParameter("apiUser", apiUser);
                r.AddParameter("apiPass", apiPass);
                r.AddParameter("user", user);
                r.AddParameter("pass", pass);
                r.AddParameter("loginId", loginId);
                r.AddParameter("amount", amount);

                Request = client.BuildUri(r).ToString();
                IRestResponse response = client.Execute(r);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Response = response.Content;
                    DepositRes = JsonConvert.DeserializeObject<DepositRes>(response.Content);
                    if (DepositRes.status == "1")
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = DepositRes.errorMessage;
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = (response.StatusDescription + "\n" + response.ErrorMessage)?.Trim();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                return false;
            }
        }

        public bool Withdraw(string loginId, decimal amount)
        {
            try
            {
                var client = new RestClient(apiUrl + "withdraw");
                client.Timeout = timeOutSec;
                var r = new RestRequest(Method.GET);
                r.AddParameter("apiUser", apiUser);
                r.AddParameter("apiPass", apiPass);
                r.AddParameter("user", user);
                r.AddParameter("pass", pass);
                r.AddParameter("loginId", loginId);
                r.AddParameter("amount", amount);

                Request = client.BuildUri(r).ToString();
                IRestResponse response = client.Execute(r);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Response = response.Content;
                    WithdrawRes = JsonConvert.DeserializeObject<WithdrawRes>(response.Content);
                    if (WithdrawRes.status == "1")
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = WithdrawRes.errorMessage;
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = (response.StatusDescription + "\n" + response.ErrorMessage)?.Trim();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                return false;
            }
        }

        public bool BetLogin(string loginId, string loginPass, string targetLocale)
        {
            try
            {
                var client = new RestClient(apiUrl + "betLogin");
                client.Timeout = timeOutSec;
                var r = new RestRequest(Method.GET);
                r.AddParameter("apiUser", apiUser);
                r.AddParameter("apiPass", apiPass);
                r.AddParameter("user", user);
                r.AddParameter("pass", pass);
                r.AddParameter("loginId", loginId);
                r.AddParameter("loginPass", loginPass);

                Request = client.BuildUri(r).ToString();
                IRestResponse response = client.Execute(r);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Response = response.Content;
                    BetLoginRes = JsonConvert.DeserializeObject<BetLoginRes>(response.Content);
                    if (!string.IsNullOrWhiteSpace(BetLoginRes.sessionId)
                        && !string.IsNullOrWhiteSpace(BetLoginRes.tokenCode))
                    {
                        BetLoginRes.url = apiUrl + "betLobby"
                            + "?loginId=" + loginId
                            + "&sessionId=" + HttpUtility.UrlEncode(BetLoginRes.sessionId)
                            + "&tokenCode=" + HttpUtility.UrlEncode(BetLoginRes.tokenCode)
                            + "&targetLocale=" + targetLocale
                            + "&view=mobile";

                        return true;
                    }
                    else
                    {
                        ErrorMessage = BetLoginRes.errorMessage;
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = (response.StatusDescription + "\n" + response.ErrorMessage)?.Trim();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                return false;
            }
        }

        public bool BetHistory(string loginId, string fromDate, string toDate, int pageNo = 0)
        {
            try
            {
                var client = new RestClient(apiUrl + "betList");
                client.Timeout = timeOutSec;
                var r = new RestRequest(Method.GET);
                r.AddParameter("apiUser", apiUser);
                r.AddParameter("apiPass", apiPass);
                r.AddParameter("user", user);
                r.AddParameter("pass", pass);
                r.AddParameter("loginId", loginId);
                r.AddParameter("fromDate", fromDate);
                r.AddParameter("toDate", toDate);
                if (pageNo != 0)
                    r.AddParameter("pageNo", pageNo);

                Request = client.BuildUri(r).ToString();
                IRestResponse response = client.Execute(r);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Response = response.Content;

                    //{"errorMessage":"Member not found!","status":"0"}
                    if (response.Content.Contains("\"status\":\"0\""))
                    {
                        BaseResponse BaseResponse = JsonConvert.DeserializeObject<BaseResponse>(response.Content);
                        ErrorMessage = (BaseResponse.status + " " + BaseResponse.errorMessage)?.Trim();
                        return false;
                    }
                    else
                    {
                        lBetHistoryRes = JsonConvert.DeserializeObject<List<BetHistoryRes>>(response.Content);
                        return true;
                    }
                }
                else
                {
                    ErrorMessage = (response.StatusDescription + " " + response.ErrorMessage)?.Trim();
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.ToString();
                return false;
            }
        }

        public bool WinningNumber(string loginId, string fromDate, string toDate)
        {
            try
            {
                var client = new RestClient(apiUrl + "winNumber");
                client.Timeout = timeOutSec;
                var r = new RestRequest(Method.GET);
                r.AddParameter("apiUser", apiUser);
                r.AddParameter("apiPass", apiPass);
                r.AddParameter("user", user);
                r.AddParameter("pass", pass);
                r.AddParameter("loginId", loginId);
                r.AddParameter("fromDate", fromDate);
                r.AddParameter("toDate", toDate);

                Request = client.BuildUri(r).ToString();
                IRestResponse response = client.Execute(r);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Response = response.Content;
                    WinningNumberRes = JsonConvert.DeserializeObject<WinningNumberRes>(response.Content);
                    return true;
                }
                else
                {
                    ErrorMessage = (response.StatusDescription + "\n" + response.ErrorMessage)?.Trim();
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
    public class BaseRequest
    {
        public string serialNo { get; set; }
        public string merchantCode { get; set; }
        internal string Method { get; set; }
    }

    //public class BaseResponse
    //{
    //    public string serialNo { get; set; }
    //    public int code { get; set; }
    //    public string msg { get; set; }
    //    public string merchantCode { get; set; }
    //}

    public class BaseResponse
    {
        public string errorMessage { get; set; }
        public string status { get; set; }
    }

    public class PairVM
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }


    public class CreatePlayerRes
    {
        public string errorMessage { get; set; }
        public string message { get; set; }
        public string status { get; set; }
    }

    public class GetProfileRes
    {
        public decimal balance { get; set; }
        public string currency { get; set; }
        public string fullName { get; set; }
        public string loginId { get; set; }
        public decimal outstanding { get; set; }
        public string status { get; set; }
    }

    public class DepositRes
    {
        public string errorMessage { get; set; }
        public string message { get; set; }
        public string status { get; set; }
    }

    public class WithdrawRes
    {
        public string errorMessage { get; set; }
        public string message { get; set; }
        public string status { get; set; }
    }

    public class BetLoginRes
    {
        public string errorMessage { get; set; }
        public string message { get; set; }
        public string status { get; set; }

        public string sessionId { get; set; }
        public string tokenCode { get; set; }

        public string url { get; set; }
    }

    public class BetLobbyRes
    {
        public string errorMessage { get; set; }
        public string message { get; set; }
        public string status { get; set; }

        public string sessionId { get; set; }
        public string tokenCode { get; set; }

        public string url { get; set; }
    }

    public class BetHistoryRes
    {
        public string content { get; set; }
        public string currency { get; set; }
        public string dateCreated { get; set; }
        public long id { get; set; }
        public int pageNo { get; set; }
        public string receiptContent { get; set; }
        public string receiptNo { get; set; }
        public decimal totalBet { get; set; }
        public string username { get; set; }
        public bool voided { get; set; }
    }

    public class WinningNumberRes
    {
        public string status { get; set; }
        public List<WinningNumberRes_WinningDetail> winningDetails { get; set; }
    }

    public class WinningNumberRes_WinningDetail
    {
        public decimal betBig { get; set; }
        public string betNumber { get; set; }
        public int betNumberId { get; set; }
        public int betOrderId { get; set; }
        public string currency { get; set; }
        public decimal currencyRate { get; set; }
        public DateTime drawDate { get; set; }
        public string drawType { get; set; }
        public string packageName { get; set; }
        public int pageNo { get; set; }
        public int prize { get; set; }
        public string refId { get; set; }
        public decimal prizeBig { get; set; }
        public decimal strikeBig { get; set; }
    }
    #endregion
}