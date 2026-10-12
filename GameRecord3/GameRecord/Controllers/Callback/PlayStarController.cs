using Newtonsoft.Json;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace GameRecord.Controllers.Callback
{
    public class PlayStarController : ApiController
    {
        public Common _common = new Common();
        public string BingoConn = System.Configuration.ConfigurationManager.ConnectionStrings["BingoConn1"].ToString();
        public string gameID = "PLAYSTAR";

        // GET: GP
        [Route("api/callback/playstar/authenticate")]
        [HttpGet]
        public HttpResponseMessage Get(string access_token)
        {
            AuthenticateResponse s = new AuthenticateResponse();

            if (!String.IsNullOrEmpty(access_token))
            {

                DataRow dr = CheckToken(access_token);
                if (dr != null)
                {
                    s.status_code = 0;
                    s.member_id = dr["GamePlayerID"].ToString();
                    s.member_name = dr["GamePlayerID"].ToString();
                }
                else
                {
                    s.status_code = 1;
                    s.member_id = "";
                    s.member_name = "";
                }
            }
            else
            {
                s.status_code = 1;
                s.member_id = "";
                s.member_name = "";
            }

            _common.InsertLog(gameID, "Authenticate", "AccessToken", access_token, "PlayerID", s.member_name);

            return new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(s), System.Text.Encoding.UTF8, "application/json")
            };
        }

        public DataRow CheckToken(string token)
        {
            DataRow dr = null;
            try
            {
                using (SqlConnection con = new SqlConnection(BingoConn))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("sp_PlayStarGameToken_Get", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@Token", token));

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    if (dt != null && dt.Rows.Count > 0)
                    {
                        dr = dt.Rows[0];
                    }
                }
            }
            catch (Exception ex)
            {
                _common.InsertLog(gameID, "Authenticate", "AccessToken", token, "Exception", ex.Message.ToString());
                throw ex;
            }

            return dr;
        }

        public class AuthenticateResponse
        {
            public int status_code { get; set; }
            public string member_id { get; set; }
            public string member_name { get; set; }
            public long balance { get; set; } = 0;
            public int type { get; set; } = 0;
        }
    }
}
