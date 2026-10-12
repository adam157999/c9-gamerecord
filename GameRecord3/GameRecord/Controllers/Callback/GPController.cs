using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Serialization;

namespace GameRecord.Controllers.Callback
{
    public class GPController : Controller
    {
        public Common _common = new Common();
        public string BingoConn = System.Configuration.ConfigurationManager.ConnectionStrings["BingoConn"].ToString();
        public string gameID = "GP";

        // GET: GP
        public ContentResult ValidateTicket(string ticket)
        {
            FailedResponse f = new FailedResponse();
            SuccessResponse s = new SuccessResponse();
            XmlDocument xmldoc = new XmlDocument();
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add("", "");
            StringWriterUtf8 myWriter = new StringWriterUtf8();

            XmlSerializer xs;

            f.error_code = "03";
            f.error_msg = "Authentication Failed";

            if (ticket != "")
            {
                
                DataRow dr = CheckToken(ticket);
                if (dr != null)
                {
                    s.error_code = "0";
                    s.cust_id = dr["cust_id"].ToString();
                    s.cust_name = dr["cust_name"].ToString();
                    s.language = dr["language"].ToString();
                    s.country = dr["country"].ToString();
                    s.test_cust = "false";
                    s.ip = dr["ip"].ToString();
                    s.currency_code = dr["currency_code"].ToString();
                    s.date_of_birth = DateTime.Parse(dr["date_of_birth"].ToString()).ToString("dd-MM-yyyy");

                    xs = new XmlSerializer(s.GetType());
                    xs.Serialize(myWriter, s, ns);
                    xmldoc.LoadXml(myWriter.ToString());
                }
                else
                {
                    xs = new XmlSerializer(f.GetType());
                    xs.Serialize(myWriter, f, ns);
                    xmldoc.LoadXml(myWriter.ToString());
                }
            }
            else
            {
                xs = new XmlSerializer(f.GetType());
                xs.Serialize(myWriter, f, ns);
                xmldoc.LoadXml(myWriter.ToString());
            }

            return Content(xmldoc.OuterXml);
        }

        public DataRow CheckToken(string token)
        {
            DataRow dr = null;
            try
            {
                using (SqlConnection con = new SqlConnection(BingoConn))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("sp_GameToken_Validate", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@GameID", gameID));
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
                throw ex;
            }

            return dr;
        }

        public class StringWriterUtf8 : System.IO.StringWriter
        {
            public override Encoding Encoding
            {
                get
                {
                    return Encoding.UTF8;
                }
            }
        }

        [Serializable, XmlRoot("resp")]
        public class SuccessResponse
        {
            [XmlElement("error_code")]
            public string error_code { get; set; }

            [XmlElement("cust_id")]
            public string cust_id { get; set; }

            [XmlElement("cust_name")]
            public string cust_name { get; set; }

            [XmlElement("currency_code")]
            public string currency_code { get; set; }

            [XmlElement("language")]
            public string language { get; set; }

            [XmlElement("test_cust")]
            public string test_cust { get; set; }

            [XmlElement("country")]
            public string country { get; set; }

            [XmlElement("date_of_birth")]
            public string date_of_birth { get; set; }

            [XmlElement("ip")]
            public string ip { get; set; }
        }

        [Serializable, XmlRoot("resp")]
        public class FailedResponse
        {
            [XmlElement("error_code")]
            public string error_code { get; set; }
            [XmlElement("error_msg")]
            public string error_msg { get; set; }
        }
    }
}