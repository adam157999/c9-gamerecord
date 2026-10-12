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
using System.Web;

namespace Kiss918H5
{
    public class Kiss918H5 : IDisposable
    {
        #region Declaration
        private string ApiUrl { get; set; }
        private string LobbyUrl { get; set; }
        private string SecretKey { get; set; }
        private string Auth { get; set; }
        private string Agent { get; set; }
        private string AreaId { get; set; }
        private string Tel { get; set; }
        private int TimeOutSec { get; set; }
        private string IP { get; set; }

        public string Url { get; set; }
        public string Request { get; set; }
        public string Response { get; set; }
        public string ErrorMessage { get; set; }

        public RandomUserNameRes RandomUserNameRes;
        public AddUserRes AddUserRes;
        public GetUserInfoRes GetUserInfoRes;
        public SetScoreRes SetScoreRes;
        public GameLogRes GameLogRes;
        public LaunchH5LobbyRes LaunchH5LobbyRes;

        public Kiss918H5(string ApiUrl, string LobbyUrl, string SecretKey, string Auth, string Agent, string AreaId, string Tel, int TimeOutSec = 30000, string IP = null)
        {
            int iOccurence = IP.Split(':').Length - 1;
            if (iOccurence == 1)
                IP = IP.Substring(0, IP.IndexOf(":"));

            this.ApiUrl = ApiUrl;
            this.LobbyUrl = LobbyUrl;
            this.SecretKey = SecretKey;
            this.Auth = Auth;
            this.Agent = Agent;
            this.AreaId = AreaId;
            this.Tel = Tel;
            this.TimeOutSec = TimeOutSec;
            this.IP = IP;
        }
        #endregion

        #region General Functions
        private long GetTime()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private string Sign(string input)
        {
            input = input?.ToLower();

            //using (MD5 md5 = MD5.Create())
            //{
            //    byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            //    byte[] hashBytes = md5.ComputeHash(inputBytes);

            //    StringBuilder sb = new StringBuilder();
            //    for (int i = 0; i < hashBytes.Length; i++)
            //        sb.Append(hashBytes[i].ToString("X2"));
            //    return sb.ToString();
            //}

            StringBuilder hash = new StringBuilder();
            MD5CryptoServiceProvider md5provider = new MD5CryptoServiceProvider();
            byte[] bytes = md5provider.ComputeHash(new UTF8Encoding().GetBytes(input));
            for (int i = 0; i < bytes.Length; i++)
                hash.Append(bytes[i].ToString("x2"));

            string result = hash.ToString();
            return result;
        }
        #endregion

        #region Api Functions
        public bool RandomUserName()
        {
            try
            {
                var time = GetTime();

                Url = ApiUrl + "ashx/account/account.ashx";
                Url += "?action=" + "RandomUserName";
                Url += "&userName=" + Agent;
                Url += "&UserAreaId=" + AreaId;
                Url += "&time=" + time;
                Url += "&authcode=" + Auth;
                Url += "&sign=" + Sign(Auth + Agent + time + SecretKey);

                var client = new RestClient(Url);
                var r = new RestRequest(Method.GET);
                IRestResponse response = client.Execute(r);
                Response = response.Content;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    RandomUserNameRes = JsonConvert.DeserializeObject<RandomUserNameRes>(response.Content);
                    if (RandomUserNameRes.success)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = RandomUserNameRes.code + ": " + RandomUserNameRes.msg;
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

        public bool AddUser(string userName, string PassWd)
        {
            try
            {
                var time = GetTime();

                Url = ApiUrl + "ashx/account/account.ashx";
                Url += "?action=" + "addUser";
                Url += "&agent=" + Agent;
                Url += "&PassWd=" + PassWd;
                Url += "&pwdtype=" + "0";
                Url += "&userName=" + userName;
                Url += "&Name=" + userName;
                Url += "&Tel=" + Tel;
                Url += "&Memo=" + null;
                Url += "&UserType=" + "1";
                Url += "&time=" + time;
                Url += "&authcode=" + Auth;
                Url += "&sign=" + Sign(Auth + userName + time + SecretKey);

                var client = new RestClient(Url);
                var r = new RestRequest(Method.GET);
                IRestResponse response = client.Execute(r);
                Response = response.Content;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    AddUserRes = JsonConvert.DeserializeObject<AddUserRes>(response.Content);
                    if (AddUserRes.success)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = AddUserRes.code + ": " + AddUserRes.msg;
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

        public bool GetUserInfo(string userName)
        {
            try
            {
                var time = GetTime();

                Url = ApiUrl + "ashx/account/account.ashx";
                Url += "?action=" + "getUserInfo";
                Url += "&userName=" + userName;
                Url += "&time=" + time;
                Url += "&authcode=" + Auth;
                Url += "&sign=" + Sign(Auth + userName + time + SecretKey);

                var client = new RestClient(Url);
                var r = new RestRequest(Method.GET);
                IRestResponse response = client.Execute(r);
                Response = response.Content;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    GetUserInfoRes = JsonConvert.DeserializeObject<GetUserInfoRes>(response.Content);
                    if (GetUserInfoRes.success)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = GetUserInfoRes.code + ": " + GetUserInfoRes.msg;
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

        public bool SetScore(string userName, double scoreNum)
        {
            try
            {
                var time = GetTime();

                Url = ApiUrl + "ashx/account/setScore.ashx";
                Url += "?action=" + "setServerScore";
                Url += "&orderid=" + Guid.NewGuid().ToString();
                Url += "&scoreNum=" + scoreNum;
                Url += "&userName=" + userName;
                Url += "&ActionUser=" + "ActionUser";
                Url += "&ActionIp=" + IP;
                Url += "&time=" + time;
                Url += "&authcode=" + Auth;
                Url += "&sign=" + Sign(Auth + userName + time + SecretKey);

                var client = new RestClient(Url);
                var r = new RestRequest(Method.GET);
                IRestResponse response = client.Execute(r);
                Response = response.Content;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    SetScoreRes = JsonConvert.DeserializeObject<SetScoreRes>(response.Content);
                    if (SetScoreRes.success)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = SetScoreRes.code + ": " + SetScoreRes.msg;
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

        public bool GameLog(string sDate, string eDate, string userName = null, int pageIndex = 1, int pageSize = 1000)
        {
            try
            {
                var time = GetTime();

                Url = ApiUrl + "ashx/GameLog.ashx?";
                Url += "?pageIndex=" + pageIndex;
                Url += "&pageSize=" + pageSize;
                Url += "&userName=" + userName;
                Url += "&sDate=" + sDate;
                Url += "&eDate=" + eDate;
                Url += "&time=" + time;
                Url += "&authcode=" + Auth;
                Url += "&sign=" + Sign(Auth + userName + time + SecretKey);

                var client = new RestClient(Url);
                var r = new RestRequest(Method.GET);
                IRestResponse response = client.Execute(r);
                Response = response.Content;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    GameLogRes = JsonConvert.DeserializeObject<GameLogRes>(response.Content);
                    if (GameLogRes.success)
                    {
                        return true;
                    }
                    else
                    {
                        ErrorMessage = GameLogRes.msg;
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

        public bool LaunchH5Lobby(string userName, string lang)
        {
            try
            {
                var time = GetTime();

                Url = ApiUrl + "ashx/launchH5Lobby";
                Url += "?userName=" + userName;
                Url += "&time=" + time;
                Url += "&authcode=" + Auth;
                Url += "&sign=" + Sign(Auth + userName + time + SecretKey);

                var client = new RestClient(Url);
                var r = new RestRequest(Method.GET);
                IRestResponse response = client.Execute(r);
                Response = response.Content;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    LaunchH5LobbyRes = JsonConvert.DeserializeObject<LaunchH5LobbyRes>(response.Content);
                    if (!string.IsNullOrWhiteSpace(LaunchH5LobbyRes.lobbyToken))
                    {
                        //LaunchH5LobbyRes.launchGameUrl = LobbyUrl + "lang=" + lang + "&combine=1&token=" + HttpUtility.UrlEncode(LaunchH5LobbyRes.lobbyToken);
                        LaunchH5LobbyRes.launchGameUrl = LobbyUrl + "lang=" + lang + "&combine=1&token=" + LaunchH5LobbyRes.lobbyToken;
                        return true;
                    }
                    else
                    {
                        ErrorMessage = LaunchH5LobbyRes.code + ": " + LaunchH5LobbyRes.msg;
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
    public class RandomUserNameRes
    {
        public int code { get; set; }
        public string msg { get; set; }
        public bool success { get; set; }
        public string account { get; set; }
    }

    public class AddUserRes
    {
        public int code { get; set; }
        public string msg { get; set; }
        public bool success { get; set; }
        public int type { get; set; }
    }

    public class GetUserInfoRes
    {
        public int code { get; set; }
        public string msg { get; set; }

        public string Account { get; set; }
        public string Agent { get; set; }
        public string Name { get; set; }
        public decimal MoneyNum { get; set; }
        public decimal ScoreNum { get; set; }
        public bool success { get; set; }
    }

    public class SetScoreRes
    {
        public string acc { get; set; }
        public int code { get; set; }
        public decimal money { get; set; }
        public string msg { get; set; }
        public bool success { get; set; }
    }

    public class GameLogRes
    {
        public bool success { get; set; }
        public string msg { get; set; }
        public List<GameLogRes_Result> results { get; set; }
    }

    public class GameLogRes_Result
    {
        public decimal BeginBlance { get; set; }
        public long ClassID { get; set; }
        public DateTime CreateTime { get; set; }
        public decimal EndBlance { get; set; }
        public long GameID { get; set; }
        public string GameName { get; set; }
        public long LineNum { get; set; }
        public long RoundNO { get; set; }
        public long Rownum { get; set; }
        public long TableID { get; set; }
        public decimal Win { get; set; }
        public decimal bet { get; set; }
        public long id { get; set; }
    }

    public class LaunchH5LobbyRes
    {
        public int code { get; set; }
        public string msg { get; set; }
        public bool success { get; set; }
        public string account { get; set; }
        public int type { get; set; }

        public string lobbyToken { get; set; }

        public string launchGameUrl { get; set; }
    }
    #endregion
}