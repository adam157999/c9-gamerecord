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
using System.Globalization;

namespace GameRecord.Gamess.AdvantPlay
{
    public class AdvantPlay : IDisposable
    {
        Common _common = new Common();

        #region Declaration
        private string apiUrl { get; set; }
        private string webUrl { get; set; }
        private string prefix { get; set; }
        private string secretKey { get; set; }
        private string currency { get; set; }
        private int timeOutSec { get; set; }

        public GetGameListRes GetGameListRes { get; set; }
        public CreatePlayerRes CreatePlayerRes { get; set; }
        public GetPlayerTokenRes GetPlayerTokenRes { get; set; }
        public GetBalanceRes GetBalanceRes { get; set; }
        public TransferInRes TransferInRes { get; set; }
        public TransferOutRes TransferOutRes { get; set; }
        public GetBatchHistoryRes GetBatchHistoryRes { get; set; }

        public string Request { get; set; }
        public string Parameters { get; set; }
        public string Response { get; set; }
        public string ErrorMessage { get; set; }
        public string Url { get; set; }

        public AdvantPlay(string apiUrl, string webUrl, string prefix, string secretKey, string currency, int timeOutSec = 30000)
        {
            this.apiUrl = apiUrl;
            this.webUrl = webUrl;
            this.prefix = prefix;
            this.secretKey = secretKey;
            this.currency = currency;
            this.timeOutSec = timeOutSec;
        }
        #endregion

        #region General Functions
        private string GetSignature(Dictionary<string, dynamic> geek, string secretKey)
        {
            string s = secretKey + JsonConvert.SerializeObject(geek);
            using (MD5 md5 = MD5.Create())
            {
                byte[] rawBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(s));
                return BitConverter.ToString(rawBytes).Replace("-", "").ToLower();
            }
        }

        private string GenerateToken()
        {
            return Guid.NewGuid().ToString().Replace("-", "");
        }
        #endregion

        #region Api Functions
        public bool GetGameList()
        {
            try
            {
                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Timestamp", _common.GetTimestamp_ISO8601_Microsecond());
                geek.Add("Seq", Guid.NewGuid().ToString());
                geek.Add("SiteCode", "default");
                geek.Add("Size", "150x150");
                var signature = GetSignature(geek, secretKey);

                var requestUrl = apiUrl + "/" + prefix + "/GetGameList";
                var client = new RestClient(requestUrl);
                var r = new RestRequest(Method.POST);
                r.AddHeader("Ap-Hash", signature);
                r.AddHeader("Content-Type", "application/json");
                r.AddJsonBody(geek);

                Request = requestUrl;
                Parameters = JsonConvert.SerializeObject(geek);
                IRestResponse response = client.Execute(r);
                Response = response.Content;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    GetGameListRes = JsonConvert.DeserializeObject<GetGameListRes>(response.Content);
                    if (GetGameListRes.ErrorCode == 0)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = GetGameListRes.ErrorCode + ": " + GetGameListRes.ErrorDescription;
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

        public bool CreatePlayer(string PlayerId)
        {
            try
            {
                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Timestamp", _common.GetTimestamp_ISO8601_Microsecond());
                geek.Add("Seq", Guid.NewGuid().ToString());
                geek.Add("SiteCode", "default");
                geek.Add("PlayerId", PlayerId);
                geek.Add("PlayerName", PlayerId);
                geek.Add("Currency", currency);
                var signature = GetSignature(geek, secretKey);

                var requestUrl = apiUrl + "/" + prefix + "/CreatePlayer";
                var client = new RestClient(requestUrl);
                var r = new RestRequest(Method.POST);
                r.AddHeader("Ap-Hash", signature);
                r.AddHeader("Content-Type", "application/json");
                r.AddJsonBody(geek);

                Request = requestUrl;
                Parameters = JsonConvert.SerializeObject(geek);
                IRestResponse response = client.Execute(r);
                Response = response.Content;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    CreatePlayerRes = JsonConvert.DeserializeObject<CreatePlayerRes>(response.Content);
                    if (CreatePlayerRes.ErrorCode == 0)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = CreatePlayerRes.ErrorCode + ": " + CreatePlayerRes.ErrorDescription;
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

        public bool GetPlayerToken(string PlayerId, string GameCode, string LangCode, string BackUrl)
        {
            try
            {
                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Timestamp", _common.GetTimestamp_ISO8601_Microsecond());
                geek.Add("Seq", Guid.NewGuid().ToString());
                geek.Add("SiteCode", "default");
                geek.Add("PlayerId", PlayerId);
                var signature = GetSignature(geek, secretKey);

                var requestUrl = apiUrl + "/" + prefix + "/GetPlayerToken";
                var client = new RestClient(requestUrl);
                var r = new RestRequest(Method.POST);
                r.AddHeader("Ap-Hash", signature);
                r.AddHeader("Content-Type", "application/json");
                r.AddJsonBody(geek);

                Request = requestUrl;
                Parameters = JsonConvert.SerializeObject(geek);
                IRestResponse response = client.Execute(r);
                Response = response.Content;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    GetPlayerTokenRes = JsonConvert.DeserializeObject<GetPlayerTokenRes>(response.Content);
                    if (GetPlayerTokenRes.ErrorCode == 0)
                    {
                        //GetPlayerTokenRes.Url = HttpUtility.UrlEncode(webUrl + "/LaunchGame"
                        //    + "?Token=" + GetPlayerTokenRes.Token
                        //    + "&SiteCode=default"
                        //    + "&GameCode=" + GameCode
                        //    + "&LangCode=" + LangCode
                        //    + "&BackUrl=" + BackUrl);
                        GetPlayerTokenRes.Url = webUrl + "/" + prefix + "/LaunchGame"
                            + "?Token=" + HttpUtility.UrlEncode(GetPlayerTokenRes.Token)
                            + "&SiteCode=default"
                            + "&GameCode=" + GameCode
                            + "&LangCode=" + LangCode
                            + "&BackUrl=" + HttpUtility.UrlEncode(BackUrl);

                        Url = GetPlayerTokenRes.Url;

                        return true;
                    }
                    else
                    {
                        ErrorMessage = GetPlayerTokenRes.ErrorCode + ": " + GetPlayerTokenRes.ErrorDescription;
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

        public bool GetBalance(string PlayerId)
        {
            try
            {
                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Timestamp", _common.GetTimestamp_ISO8601_Microsecond());
                geek.Add("Seq", Guid.NewGuid().ToString());
                geek.Add("OPToken", GenerateToken());
                geek.Add("SiteCode", "default");
                geek.Add("PlayerId", PlayerId);
                var signature = GetSignature(geek, secretKey);

                var requestUrl = apiUrl + "/" + prefix + "/GetBalance";
                var client = new RestClient(requestUrl);
                var r = new RestRequest(Method.POST);
                r.AddHeader("Ap-Hash", signature);
                r.AddHeader("Content-Type", "application/json");
                r.AddJsonBody(geek);

                Request = requestUrl;
                Parameters = JsonConvert.SerializeObject(geek);
                IRestResponse response = client.Execute(r);
                Response = response.Content;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    GetBalanceRes = JsonConvert.DeserializeObject<GetBalanceRes>(response.Content);
                    if (GetBalanceRes.ErrorCode == 0)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = GetBalanceRes.ErrorCode + ": " + GetBalanceRes.ErrorDescription;
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

        public bool TransferIn(string PlayerId, decimal Amount)
        {
            try
            {
                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Timestamp", _common.GetTimestamp_ISO8601_Microsecond());
                geek.Add("Seq", Guid.NewGuid().ToString());
                geek.Add("SiteCode", "default");
                geek.Add("PlayerId", PlayerId);
                geek.Add("OPTransferID", _common.GenerateUniqueNumber().ToString());
                geek.Add("Currency", currency);
                geek.Add("Amount", Amount.ToString());
                var signature = GetSignature(geek, secretKey);

                var requestUrl = apiUrl + "/" + prefix + "/TransferIn";
                var client = new RestClient(requestUrl);
                var r = new RestRequest(Method.POST);
                r.AddHeader("Ap-Hash", signature);
                r.AddHeader("Content-Type", "application/json");
                r.AddJsonBody(geek);

                Request = requestUrl;
                Parameters = JsonConvert.SerializeObject(geek);
                IRestResponse response = client.Execute(r);
                Response = response.Content;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    TransferInRes = JsonConvert.DeserializeObject<TransferInRes>(response.Content);
                    if (TransferInRes.ErrorCode == 0)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = TransferInRes.ErrorCode + ": " + TransferInRes.ErrorDescription;
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

        public bool TransferOut(string PlayerId, decimal Amount)
        {
            try
            {
                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Timestamp", _common.GetTimestamp_ISO8601_Microsecond());
                geek.Add("Seq", Guid.NewGuid().ToString());
                geek.Add("SiteCode", "default");
                geek.Add("PlayerId", PlayerId);
                geek.Add("OPTransferID", _common.GenerateUniqueNumber().ToString());
                geek.Add("Currency", currency);
                geek.Add("Amount", Amount.ToString());
                var signature = GetSignature(geek, secretKey);

                var requestUrl = apiUrl + "/" + prefix + "/TransferOut";
                var client = new RestClient(requestUrl);
                var r = new RestRequest(Method.POST);
                r.AddHeader("Ap-Hash", signature);
                r.AddHeader("Content-Type", "application/json");
                r.AddJsonBody(geek);

                Request = requestUrl;
                Parameters = JsonConvert.SerializeObject(geek);
                IRestResponse response = client.Execute(r);
                Response = response.Content;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    TransferOutRes = JsonConvert.DeserializeObject<TransferOutRes>(response.Content);
                    if (TransferOutRes.ErrorCode == 0)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = TransferOutRes.ErrorCode + ": " + TransferOutRes.ErrorDescription;
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

        public bool GetBatchHistory(string DateFrom, string DateTo, int Page = 1, int PageSize = 1000)
        {
            try
            {
                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("Timestamp", _common.GetTimestamp_ISO8601_Microsecond());
                geek.Add("Seq", Guid.NewGuid().ToString());
                geek.Add("SiteCode", "default");
                geek.Add("DateFrom", DateFrom);
                geek.Add("DateTo", DateTo);
                geek.Add("Page", Page);
                geek.Add("PageSize", PageSize);
                var signature = GetSignature(geek, secretKey);

                var requestUrl = apiUrl + "/" + prefix + "/GetBatchHistory";
                var client = new RestClient(requestUrl);
                var r = new RestRequest(Method.POST);
                r.AddHeader("Ap-Hash", signature);
                r.AddHeader("Content-Type", "application/json");
                r.AddJsonBody(geek);

                Request = requestUrl;
                Parameters = JsonConvert.SerializeObject(geek);
                IRestResponse response = client.Execute(r);
                Response = response.Content;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    GetBatchHistoryRes = JsonConvert.DeserializeObject<GetBatchHistoryRes>(response.Content);
                    if (GetBatchHistoryRes.ErrorCode == 0)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = GetBatchHistoryRes.ErrorCode + ": " + GetBatchHistoryRes.ErrorDescription;
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
    public class GetGameListRes
    {
        public string Timestamp { get; set; }
        public string Seq { get; set; }
        public int ErrorCode { get; set; }
        public string ErrorDescription { get; set; }

        public List<GetGameListRes_Game> Games { get; set; }
    }

    public class GetGameListRes_Game
    {
        public int Id { get; set; }
        public string GameCode { get; set; }
        public List<GetGameListRes_Game_Icon> IconList { get; set; }
    }

    public class GetGameListRes_Game_Icon
    {
        public string Language { get; set; }
        public string Name { get; set; }
        public GetGameListRes_Game_Icon_Url Url { get; set; }
    }

    public class GetGameListRes_Game_Icon_Url
    {
        [JsonProperty("150x150")]
        public GetGameListRes_Game_Icon_Urlx x { get; set; }
    }

    public class GetGameListRes_Game_Icon_Urlx
    {
        public string bkg { get; set; }
    }

    public class CreatePlayerRes
    {
        public string Timestamp { get; set; }
        public string Seq { get; set; }
        public int ErrorCode { get; set; }
        public string ErrorDescription { get; set; }
        public string Token { get; set; }
    }

    public class GetPlayerTokenRes
    {
        public string Timestamp { get; set; }
        public string Seq { get; set; }
        public int ErrorCode { get; set; }
        public string ErrorDescription { get; set; }
        public string Token { get; set; }

        //custom
        public string Url { get; set; }
    }

    public class GetBalanceRes
    {
        public string Timestamp { get; set; }
        public string Seq { get; set; }
        public int ErrorCode { get; set; }
        public string ErrorDescription { get; set; }
        public decimal Balance { get; set; }
    }

    public class TransferInRes
    {
        public string Timestamp { get; set; }
        public string Seq { get; set; }
        public int ErrorCode { get; set; }
        public string ErrorDescription { get; set; }

        public string OPTransferID { get; set; }
        public decimal BalanceBefore { get; set; }
        public decimal BalanceAfter { get; set; }
        public decimal Amount { get; set; }
    }

    public class TransferOutRes
    {
        public string Timestamp { get; set; }
        public string Seq { get; set; }
        public int ErrorCode { get; set; }
        public string ErrorDescription { get; set; }

        public string OPTransferID { get; set; }
        public decimal BalanceBefore { get; set; }
        public decimal BalanceAfter { get; set; }
        public decimal Amount { get; set; }
    }

    public class GetBatchHistoryRes
    {
        public string Timestamp { get; set; }
        public string Seq { get; set; }
        public int ErrorCode { get; set; }
        public string ErrorDescription { get; set; }

        public GetBatchHistoryRes_Info Info { get; set; }
        public List<GetBatchHistoryRes_Result> Results { get; set; }
    }

    public class GetBatchHistoryRes_Info
    {
        public int page { get; set; }
        public int pagesize { get; set; }
        public int records { get; set; }
    }

    public class GetBatchHistoryRes_Result
    {
        public string BrandCode { get; set; }
        public string SiteCode { get; set; }
        public string GameCode { get; set; }
        public string GameRoundId { get; set; }
        public string PlayerId { get; set; }
        public string Currency { get; set; }
        public string StatusCode { get; set; }
        public DateTime BetTime { get; set; }
        public DateTime SettleTime { get; set; }
        public decimal TotalStake { get; set; }
        public decimal TotalWin { get; set; }
    }
    #endregion
}