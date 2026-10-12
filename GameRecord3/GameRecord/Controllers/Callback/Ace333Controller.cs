using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Http;

namespace GameRecord.Controllers.Callback
{
    public class Ace333Controller : ApiController
    {
        public Common _common = new Common();
        public string BingoConn = System.Configuration.ConfigurationManager.ConnectionStrings["BingoConn1"].ToString();
        private string GAME_ID = "ACE333";
        private string API_SERVICE_TOKEN { get; set; }


        public Ace333Controller()
        {
            var apiConfig = CatchData.GetApiKeys(GAME_ID.ToUpper().Trim(), BaseConnector.GameEnvironment.PROD.ToString());

            if (apiConfig != null)
            {
                JObject jObj = JObject.Parse(apiConfig.ApiConfig);
                API_SERVICE_TOKEN = jObj["Ace333OperatorServiceToken"].ToString();
            }
        }

        [Route("api/callback/ace333/authenticate")]
        [HttpPost]
        [Ace333Header("token")]
        public HttpResponseMessage Authenticate([FromBody] AuthenticatePayload payload)
        {
            AuthenticateResponse s = new AuthenticateResponse();
            try
            {
                string token = Request.Headers.GetValues("token").FirstOrDefault();

                if (token == API_SERVICE_TOKEN)
                {
                    AuthenticateResponse response = GetPlayerBalance(payload.userName, GAME_ID);
                    if (response != null)
                    {
                        s.error = "0";
                        s.balance = response.balance;
                        s.playerID = response.playerID;
                        s.httpStatusCode = HttpStatusCode.OK;
                    }
                    else
                    {
                        s.error = "1";
                        s.description = "Invalid Username";
                        s.httpStatusCode = HttpStatusCode.Unauthorized;
                    }

                    _common.InsertLog("Authenticate", "Authenticate", payload, "Exception", null, "Game", GAME_ID, "PlayerID", payload.userName);
                }
                else
                {
                    s.error = "1";
                    s.description = "Token validation fail";
                    s.httpStatusCode = HttpStatusCode.BadRequest;
                }
            }
            catch (Exception ex)
            {
                s.error = "1";
                s.description = ex.Message;
                s.httpStatusCode = HttpStatusCode.InternalServerError;
            }

            return new HttpResponseMessage()
            {
                StatusCode = s.httpStatusCode,
                Content = new StringContent(JsonConvert.SerializeObject(s), System.Text.Encoding.UTF8, "application/json")
            }; 
        }

        //private bool VerifyToken(string receivedHash, string payload)
        //{
        //    string calculatedHash = "";
        //    using (MD5 md5 = MD5.Create())
        //    {
        //        byte[] rawBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(payload));
        //        calculatedHash = BitConverter.ToString(rawBytes).Replace("-", "").ToUpper();
        //    }

        //    return receivedHash == calculatedHash;
        //}

        private AuthenticateResponse GetPlayerBalance(string userName, string gameId)
        {
            AuthenticateResponse response = null;

            try
            {
                using (SqlConnection con = new SqlConnection(BingoConn))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("sp_Ace333GamePlayerBalance_Get", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@username", userName));
                    cmd.Parameters.Add(new SqlParameter("@gameId", gameId));

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        response = new AuthenticateResponse()
                        {
                            balance = dt.Rows[0]["Cash"] != DBNull.Value ? (double)dt.Rows[0]["Cash"] : 0,
                            playerID = dt.Rows[0]["GamePlayerID"] != DBNull.Value ? int.Parse(dt.Rows[0]["GamePlayerID"].ToString()) : 0
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _common.InsertLog("Authenticate", "GetPlayerBalance", userName, "Exception", ex.Message.ToString(), "Game", "Ace333", "response", response);
                throw ex;
            }

            return response;
        }

        public class AuthenticateResponse
        {
            public double balance { get; set; } = 0;
            public int playerID { get; set; } = 0;
            public string error { get; set; } = "0";
            public string description { get; set; }
            [JsonIgnore]
            public HttpStatusCode httpStatusCode { get; set; }
        }

        public class AuthenticatePayload
        {
            public string userName { get; set; }
            public string password { get; set; }
        }
    }
}