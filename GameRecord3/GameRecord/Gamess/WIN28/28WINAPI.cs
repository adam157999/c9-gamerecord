using GameRecord.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Specialized;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using static GameRecord.Gamess.SAGAPI.SAGAMINGAPI;

namespace GameRecord.Gamess.WIN28
{
    public class WIN28 : IDisposable
    {
        #region Properties Declaration

        private string API_Url;
        private string API_apiUser;
        private string API_apiPass;
        private string API_user;
        private string API_pass;
        private int API_TimeOutSec;


        public string finalUrl { get; set; }


        public WIN28(string Url, string ApiUser, string ApiPass, string User, string Pass, int TimeOutSec = 120)
        {

            API_Url = Url.EndsWith("/") ? Url : Url + "/";
            API_apiUser = ApiUser;
            API_apiPass = ApiPass;
            API_user = User;
            API_pass = Pass;
            API_TimeOutSec = TimeOutSec;
        }


        private string P_Parameters;
        private string P_Response;
        private string p_ErrorMessage;
        private string p_Request;
        private string p_TargetApiUrl;
        private bool p_ParseError = false;


        public string Parameters => P_Parameters;
        public string Response => P_Response;
        public string ErrorMessage => p_ErrorMessage;
        public string Request => p_Request;
        public string TargetApiUrl => p_TargetApiUrl;
        public bool ParseError => p_ParseError;


        public CreatePlayerResponse CreatePlayerResponse { get; private set; }
        public GetProfileResponse GetProfileResponse { get; private set; }
        public DepositResponse DepositResponse { get; private set; }
        public WithdrawResponse WithdrawResponse { get; private set; }
        public BetLoginResponse BetLoginResponse { get; private set; }
        public BetResponse BetResponse { get; private set; }
        public GetBetListResponse GetBetListResponse { get; private set; }
        public GetBetListByPageResponse GetBetListByPageResponse { get; private set; }
        public GetWinLossResponse GetWinLossResponse { get; private set; }
        public GetWinningNumberResponse GetWinningNumberResponse { get; private set; }
        public UpdatePasswordStatusResponse UpdatePasswordStatusResponse { get; private set; }


        private CreatePlayerResponse p_CreatePlayerResponse;
        private GetProfileResponse p_GetProfileResponse;
        private DepositResponse p_DepositResponse;
        private WithdrawResponse p_WithdrawResponse;
        private BetLoginResponse p_BetLoginResponse;
        private BetResponse p_BetResponse;
        private GetBetListResponse p_GetBetListResponse;
        private GetBetListByPageResponse p_GetBetListByPageResponse;
        private GetWinLossResponse p_GetWinLossResponse;
        private GetWinningNumberResponse p_GetWinningNumberResponse;
        private UpdatePasswordStatusResponse p_UpdatePasswordStatusResponse;



        private void UpdateResponseProperties()
        {

            this.CreatePlayerResponse = p_CreatePlayerResponse;
            this.GetProfileResponse = p_GetProfileResponse;
            this.DepositResponse = p_DepositResponse;
            this.WithdrawResponse = p_WithdrawResponse;
            this.BetLoginResponse = p_BetLoginResponse;
            this.BetResponse = p_BetResponse;
            this.GetBetListResponse = p_GetBetListResponse;
            this.GetBetListByPageResponse = p_GetBetListByPageResponse;
            this.GetWinLossResponse = p_GetWinLossResponse;
            this.GetWinningNumberResponse = p_GetWinningNumberResponse;
            this.UpdatePasswordStatusResponse = p_UpdatePasswordStatusResponse;
        }


        #endregion

        #region General Functions
        public bool DeSerializeFromXMLString<T>(string xmlString, ref T TargetClass, string ElementName = "response")
        {
            try
            {
                XmlRootAttribute xRoot = new XmlRootAttribute
                {
                    ElementName = ElementName,
                    IsNullable = true
                };

                XmlSerializer ser = new XmlSerializer(typeof(T), xRoot);
                using (StringReader stringReader = new StringReader(xmlString))
                {
                    using (XmlReader xRdr = XmlReader.Create(stringReader))
                    {

                        TargetClass = (T)ser.Deserialize(xRdr);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                p_ParseError = true;
                p_ErrorMessage = ex.Message + ex.StackTrace;
                TargetClass = default(T);
                return false;
            }
        }

        private bool POSTData<T>(WIN28BaseRequest req, ref T resp) where T : class
        {
            WebResponse wresp = null;
            HttpWebRequest wr = null;

            try
            {
                string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
                byte[] boundarybytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

                string Url = API_Url + req.GetPage();



                p_TargetApiUrl = Url;
                finalUrl = Url;
                wr = (HttpWebRequest)WebRequest.Create(Url);
                wr.ContentType = "multipart/form-data; boundary=" + boundary;
                wr.Method = "POST";
                wr.KeepAlive = true;
                wr.Credentials = System.Net.CredentialCache.DefaultCredentials;
                wr.Timeout = API_TimeOutSec * 1000;

                p_Request = "";
                StringBuilder requestLogBuilder = new StringBuilder();
                NameValueCollection nvc = req.GetParam();


                using (Stream rs = wr.GetRequestStream())
                {
                    string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";

                    foreach (string key in nvc.Keys)
                    {

                        requestLogBuilder.AppendFormat("&{0}={1}", key, nvc[key]);

                        rs.Write(boundarybytes, 0, boundarybytes.Length);
                        string formitem = string.Format(formdataTemplate, key, nvc[key]);
                        byte[] formitembytes = Encoding.UTF8.GetBytes(formitem);
                        rs.Write(formitembytes, 0, formitembytes.Length);
                    }

                    byte[] trailer = Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
                    rs.Write(trailer, 0, trailer.Length);
                }


                if (requestLogBuilder.Length > 0)
                {
                    p_Request = Url + " form-data:" + requestLogBuilder.ToString().Substring(1); // Remove leading '&'
                }
                else
                {
                    p_Request = Url + " form-data: (No parameters)";
                }
                P_Parameters = requestLogBuilder.ToString().Substring(1);

                wresp = wr.GetResponse();
                string responseString;

                using (Stream stream2 = wresp.GetResponseStream())
                {
                    if (stream2 == null)
                    {
                        p_ErrorMessage = "Received null response stream.";
                        return false;
                    }
                    using (StreamReader reader2 = new StreamReader(stream2))
                    {
                        responseString = reader2.ReadToEnd().Trim();
                    }
                }

                P_Response = responseString;
                T oResponse = default(T);


                if (DeSerializeFromXMLString<T>(P_Response, ref oResponse))
                {
                    resp = oResponse;

                    dynamic baseResp = resp;
                    if (baseResp != null && baseResp.errorCode == 0)
                    {
                        return true;
                    }
                    else
                    {
                        p_ErrorMessage = baseResp?.desc ?? "Unknown error or null response object.";
                        return false;
                    }
                }
                else
                {

                    return false;
                }
            }
            catch (WebException webEx)
            {

                p_ErrorMessage = $"WebException: {webEx.Message}";
                if (webEx.Response != null)
                {
                    try
                    {
                        using (StreamReader errorReader = new StreamReader(webEx.Response.GetResponseStream()))
                        {
                            p_ErrorMessage += $"\nResponse Body: {errorReader.ReadToEnd()}";
                        }
                    }
                    catch { }
                    webEx.Response.Close();
                }
                if (wresp != null) { wresp.Close(); wresp = null; }

                P_Response = p_ErrorMessage;
                InsertLog("WIN28", "Exception", "request", "URL=" + finalUrl + ", " + P_Parameters, "response", P_Response);


                return false;
            }
            catch (Exception ex)
            {

                p_ErrorMessage = $"General Exception: {ex.Message}\nStackTrace: {ex.StackTrace}";
                P_Response = p_ErrorMessage;
                InsertLog("WIN28", "Exception", "request", "URL=" + finalUrl + ", " + P_Parameters, "response", P_Response);
                if (wresp != null) { wresp.Close(); wresp = null; }
                return false;
            }
            finally
            {

                if (wresp != null && !(wresp is IDisposable))
                {

                }
            }
        }


        private string ToStringBase64(string text)
        {
            byte[] byt = Encoding.UTF8.GetBytes(text);
            return Convert.ToBase64String(byt);
        }
        #endregion

        #region API Functions
        public string BingoConn = System.Configuration.ConfigurationManager.ConnectionStrings["BingoConn1"].ToString();
        public bool InsertLog(string GameId, string sType, string sField1 = "", object sValue1 = null, string sField2 = "", object sValue2 = null, string sField3 = "", object sValue3 = null
                                                            , string sField4 = "", object sValue4 = null, string sField5 = "", object sValue5 = null, string sField6 = "", object sValue6 = null
                                                            , string sField7 = "", object sValue7 = null, string sField8 = "", object sValue8 = null, string sField9 = "", object sValue9 = null
                                                            , string sField10 = "", object sValue10 = null)
        {
            try
            {
                string sID = Guid.NewGuid().ToString();
                DateTime dtNow = DateTime.Now;

                using (SqlConnection con = new SqlConnection(BingoConn))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("sp_GameTracker_IUD", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Type", "0");
                    cmd.Parameters.AddWithValue("@Id", sID);
                    cmd.Parameters.AddWithValue("@GameID", GameId);
                    cmd.Parameters.AddWithValue("@TrackerType", sType);
                    cmd.Parameters.AddWithValue("@Field1", sField1);
                    cmd.Parameters.AddWithValue("@Value1", sValue1 ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Field2", sField2);
                    cmd.Parameters.AddWithValue("@Value2", sValue2 ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Field3", sField3);
                    cmd.Parameters.AddWithValue("@Value3", sValue3 ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Field4", sField4);
                    cmd.Parameters.AddWithValue("@Value4", sValue4 ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Field5", sField5);
                    cmd.Parameters.AddWithValue("@Value5", sValue5 ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Field6", sField6);
                    cmd.Parameters.AddWithValue("@Value6", sValue6 ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Field7", sField7);
                    cmd.Parameters.AddWithValue("@Value7", sValue7 ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Field8", sField8);
                    cmd.Parameters.AddWithValue("@Value8", sValue8 ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Field9", sField9);
                    cmd.Parameters.AddWithValue("@Value9", sValue9 ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Field10", sField10);
                    cmd.Parameters.AddWithValue("@Value10", sValue10 ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CreatedDate", dtNow);
                    cmd.Parameters.AddWithValue("@Deleted", '0');

                    cmd.ExecuteNonQuery();
                }

                return true;
            }
            catch
            {
                return false;
            }
        }


        private bool ExecuteApiCall<TRequest, TResponse>(TRequest request, ref TResponse responseField)
            where TRequest : WIN28BaseRequest
            where TResponse : class
        {
            p_ErrorMessage = null;
            p_ParseError = false;
            TResponse tempResponse = default(TResponse);

            try
            {

                if (POSTData<TResponse>(request, ref tempResponse))
                {
                    responseField = tempResponse;
                    UpdateResponseProperties();
                    return true;
                }
                else
                {

                    responseField = default(TResponse);
                    UpdateResponseProperties();




                    return false;
                }
            }
            catch (Exception ex)
            {
                p_ErrorMessage = ex.Message;
                responseField = default(TResponse);
                UpdateResponseProperties();
                return false;
            }
        }


        public bool RegisterUser(string username, string password, string fullName)
        {
            CreatePlayerRequest req = new CreatePlayerRequest
            {
                apiUser = API_apiUser,
                apiPass = API_apiPass,
                user = API_user,
                pass = API_pass,
                loginID = username,
                loginPass = password,
                fullName = fullName
            };
            return ExecuteApiCall(req, ref p_CreatePlayerResponse);
        }

        public bool GetProfile(string loginID)
        {
            GetProfileRequest req = new GetProfileRequest
            {
                apiUser = API_apiUser,
                apiPass = API_apiPass,
                user = API_user,
                pass = API_pass,
                loginID = loginID
            };
            return ExecuteApiCall(req, ref p_GetProfileResponse);
        }

        public bool Deposit(string loginID, decimal amount)
        {
            DepositRequest req = new DepositRequest
            {
                apiUser = API_apiUser,
                apiPass = API_apiPass,
                user = API_user,
                pass = API_pass,
                loginID = loginID,
                amount = amount
            };
            return ExecuteApiCall(req, ref p_DepositResponse);
        }

        public bool Withdraw(string loginID, decimal amount)
        {
            WithdrawRequest req = new WithdrawRequest
            {
                apiUser = API_apiUser,
                apiPass = API_apiPass,
                user = API_user,
                pass = API_pass,
                loginID = loginID,
                amount = amount
            };
            return ExecuteApiCall(req, ref p_WithdrawResponse);
        }

        public bool BetLogin(string user, string password, bool mobile = false) // Default parameter
        {


            BetLoginRequest req = new BetLoginRequest
            {
                apiUser = API_apiUser,
                apiPass = API_apiPass,
                user = user,
                pass = password
            };
            return ExecuteApiCall(req, ref p_BetLoginResponse);
        }

        public bool GetBetList(DateTime dateFrom, DateTime dateTo, string loginID = "") // Default parameter
        {
            GetBetListRequest req = new GetBetListRequest
            {
                apiUser = API_apiUser,
                apiPass = API_apiPass,
                user = API_user,
                pass = API_pass,
                loginID = loginID,
                dateFrom = dateFrom,
                dateTo = dateTo
            };
            return ExecuteApiCall(req, ref p_GetBetListResponse);
        }

        public bool GetBetListByPage(DateTime dateFrom, DateTime dateTo, int page = 1, string loginID = "") // Default parameters
        {
            GetBetListByPageRequest req = new GetBetListByPageRequest
            {
                apiUser = API_apiUser,
                apiPass = API_apiPass,
                user = API_user,
                pass = API_pass,
                loginID = loginID,
                dateFrom = dateFrom,
                dateTo = dateTo,
                page = page
            };
            return ExecuteApiCall(req, ref p_GetBetListByPageResponse);
        }

        public bool GetWinLoss(DateTime dateFrom, DateTime dateTo)
        {
            GetWinLossRequest req = new GetWinLossRequest
            {
                apiUser = API_apiUser,
                apiPass = API_apiPass,
                user = API_user,
                pass = API_pass,
                dateFrom = dateFrom,
                dateTo = dateTo
            };
            return ExecuteApiCall(req, ref p_GetWinLossResponse);
        }

        public bool GetWinningNumber(DateTime dateFrom, DateTime dateTo, string loginID = "") // Default parameter
        {
            GetWinningNumberRequest req = new GetWinningNumberRequest
            {
                apiUser = API_apiUser,
                apiPass = API_apiPass,
                user = API_user,
                pass = API_pass,
                loginID = loginID,
                dateFrom = dateFrom,
                dateTo = dateTo
            };
            return ExecuteApiCall(req, ref p_GetWinningNumberResponse);
        }
        public bool UpdatePasswordStatus(string user, string newPassword = "", string newStatus = "") // Default parameters
        {
            UpdatePasswordStatusRequest req = new UpdatePasswordStatusRequest
            {
                apiUser = API_apiUser,
                apiPass = API_apiPass,
                user = API_user,
                pass = API_pass,
                loginID = user,
                newpass = newPassword,
                newStatus = newStatus
            };
            return ExecuteApiCall(req, ref p_UpdatePasswordStatusResponse);
        }


        #endregion



        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {

                }


                API_Url = null;
                API_apiUser = null;
                API_apiPass = null;
                API_user = null;
                API_pass = null;
                P_Parameters = null;
                P_Response = null;
                p_ErrorMessage = null;
                p_Request = null;
                p_TargetApiUrl = null;


                disposedValue = true;
            }
        }



        public void Dispose()
        {

            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }


    public abstract class WIN28BaseRequest
    {
        public string apiUser;
        public string apiPass;
        public string user;
        public string pass;
        public string loginID;

        public abstract string GetPage();
        public abstract NameValueCollection GetParam();
    }

    public class BaseResponse
    {
        public int errorCode = -1;
        public string desc = "invalid xml";
    }

    public class BetResponse : BaseResponse
    {
        public string url;
    }


    public class BetLoginRequest : WIN28BaseRequest
    {
        public bool mobile = false;

        public override string GetPage()
        {
            return "betLogin.aspx";
        }

        public override NameValueCollection GetParam()
        {
            NameValueCollection nvc = new NameValueCollection();
            nvc.Add("apiUser", apiUser);
            nvc.Add("apiPass", apiPass);
            nvc.Add("user", user);
            nvc.Add("pass", pass);
            return nvc;
        }
    }

    public class BetLoginResponse : BaseResponse
    {
        public string sessionID;
        public string tokenCode;
    }


    public class CreatePlayerRequest : WIN28BaseRequest
    {
        public string loginPass;
        public string fullName;

        public override string GetPage()
        {
            return "createPlayer.aspx";
        }

        public override NameValueCollection GetParam()
        {
            NameValueCollection nvc = new NameValueCollection();
            nvc.Add("apiUser", apiUser);
            nvc.Add("apiPass", apiPass);
            nvc.Add("user", user);
            nvc.Add("pass", pass);
            nvc.Add("loginID", loginID);
            nvc.Add("loginPass", loginPass);
            nvc.Add("fullName", fullName);
            return nvc;
        }
    }

    public class CreatePlayerResponse : BaseResponse
    {
    }


    public class DepositRequest : WIN28BaseRequest
    {
        public decimal amount;

        public override string GetPage()
        {
            return "deposit.aspx";
        }

        public override NameValueCollection GetParam()
        {
            NameValueCollection nvc = new NameValueCollection();
            nvc.Add("apiUser", apiUser);
            nvc.Add("apiPass", apiPass);
            nvc.Add("user", user);
            nvc.Add("pass", pass);
            nvc.Add("loginID", loginID);
            nvc.Add("amount", amount.ToString());
            return nvc;
        }
    }

    public class DepositResponse : BaseResponse
    {
    }



    public class GetBetListByPageRequest : WIN28BaseRequest
    {
        public DateTime dateFrom;
        public DateTime dateTo;
        public int page;

        public override string GetPage()
        {
            return "betlistPage.aspx";
        }

        public override NameValueCollection GetParam()
        {
            NameValueCollection nvc = new NameValueCollection();
            nvc.Add("apiUser", apiUser);
            nvc.Add("apiPass", apiPass);
            nvc.Add("user", user);
            nvc.Add("pass", pass);
            nvc.Add("loginID", loginID);
            nvc.Add("dateFrom", dateFrom.ToString("yyyy-MM-dd"));
            nvc.Add("dateTo", dateTo.ToString("yyyy-MM-dd"));
            nvc.Add("page", page.ToString());
            return nvc;
        }
    }

    public class GetBetListByPageResponse : BaseResponse
    {
        [System.Xml.Serialization.XmlElementAttribute("betData")]
        public BetData[] betData;
    }



    public class GetBetListRequest : WIN28BaseRequest
    {
        public DateTime dateFrom;
        public DateTime dateTo;

        public override string GetPage()
        {
            return "betlist.aspx";
        }

        public override NameValueCollection GetParam()
        {
            NameValueCollection nvc = new NameValueCollection();
            nvc.Add("apiUser", apiUser);
            nvc.Add("apiPass", apiPass);
            nvc.Add("user", user);
            nvc.Add("pass", pass);
            nvc.Add("loginID", loginID);
            nvc.Add("dateFrom", dateFrom.ToString("yyyy-MM-dd"));
            nvc.Add("dateTo", dateTo.ToString("yyyy-MM-dd"));
            return nvc;
        }
    }

    public class GetBetListResponse : BaseResponse
    {
        [System.Xml.Serialization.XmlElementAttribute("betData")]
        public BetData[] betData;
    }

    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public class BetData
    {
        public string betID;
        public string loginID;
        public string dateTime;
        public string drawType;
        public string drawDate;
        public string betNumber;
        public string betPosition;
        public decimal bet;
    }



    public class GetProfileRequest : WIN28BaseRequest
    {
        public override string GetPage()
        {
            return "getProfile.aspx";
        }

        public override NameValueCollection GetParam()
        {
            NameValueCollection nvc = new NameValueCollection();
            nvc.Add("apiUser", apiUser);
            nvc.Add("apiPass", apiPass);
            nvc.Add("user", user);
            nvc.Add("pass", pass);
            nvc.Add("loginID", loginID);
            return nvc;
        }
    }

    public class GetProfileResponse : BaseResponse
    {
        public string loginID;
        public string fullName;
        public string status;
        public string balance;
        public string outstanding;
        public string currency;
        public string priceBig;
        public string prizeBig1;
        public string prizeBig2;
        public string prizeBig3;
        public string prizeBig4;
        public string prizeBig5;
        public string priceSmall;
        public string prizeSmall1;
        public string prizeSmall2;
        public string prizeSmall3;
        public string price4a;
        public string prize4a;
        public string priceA;
        public string prizeA;
        public string priceAbc;
        public string prizeAbc1;
        public string prizeAbc2;
        public string prizeAbc3;
        public string price5D;
        public string prize5D1;
        public string prize5D2;
        public string prize5D3;
        public string prize5D4;
        public string prize5D5;
        public string prize5D6;
        public string price6D;
        public string prize6D1;
        public string prize6D2;
        public string prize6D3;
        public string prize6D4;
        public string prize6D5;
        public ibox ibox24;
        public ibox ibox12;
        public ibox ibox6;
        public ibox ibox4;
        public string GDpriceBig;
        public string GDprizeBig1;
        public string GDprizeBig2;
        public string GDprizeBig3;
        public string GDprizeBig4;
        public string GDprizeBig5;
        public string GDpriceSmall;
        public string GDprizeSmall1;
        public string GDprizeSmall2;
        public string GDprizeSmall3;
        public string GDprice4a;
        public string GDprize4a;
        public string GDpriceA;
        public string GDprizeA;
        public string GDpriceAbc;
        public string GDprizeAbc1;
        public string GDprizeAbc2;
        public string GDprizeAbc3;
        public string GDprice6D;
        public string GDprize6D1;
        public string GDprize6D2;
        public string GDprize6D3;
        public string GDprize6D4;
        public string GDprize6D5;
        public GDibox GDibox24;
        public GDibox GDibox12;
        public GDibox GDibox6;
        public GDibox GDibox4;
    }

    public class ibox
    {
        public string prizeBig1;
        public string prizeBig2;
        public string prizeBig3;
        public string prizeBig4;
        public string prizeBig5;
        public string prizeSmall1;
        public string prizeSmall2;
        public string prizeSmall3;
        public string prize4a;
    }


    public class GDibox
    {
        public string GDprizeBig1;
        public string GDprizeBig2;
        public string GDprizeBig3;
        public string GDprizeBig4;
        public string GDprizeBig5;
        public string GDprizeSmall1;
        public string GDprizeSmall2;
        public string GDprizeSmall3;
        public string GDprize4a;
    }



    public class GetWinLossRequest : WIN28BaseRequest
    {
        public DateTime dateFrom;
        public DateTime dateTo;

        public override string GetPage()
        {
            return "winLoss.aspx";
        }

        public override NameValueCollection GetParam()
        {
            NameValueCollection nvc = new NameValueCollection();
            nvc.Add("apiUser", apiUser);
            nvc.Add("apiPass", apiPass);
            nvc.Add("user", user);
            nvc.Add("pass", pass);
            nvc.Add("dateFrom", dateFrom.ToString("yyyy-MM-dd"));
            nvc.Add("dateTo", dateTo.ToString("yyyy-MM-dd"));
            return nvc;
        }
    }

    public class GetWinLossResponse : BaseResponse
    {
        public WinLoss winLoss;
    }

    public class WinLoss
    {
        public string loginName;
        public decimal turnover;
        public decimal commission;
        public decimal strike;
        public decimal total;
    }



    public class GetWinningNumberRequest : WIN28BaseRequest
    {
        public DateTime dateFrom;
        public DateTime dateTo;

        public override string GetPage()
        {
            return "winNumber.aspx";
        }

        public override NameValueCollection GetParam()
        {
            NameValueCollection nvc = new NameValueCollection();
            nvc.Add("apiUser", apiUser);
            nvc.Add("apiPass", apiPass);
            nvc.Add("user", user);
            nvc.Add("pass", pass);
            nvc.Add("loginID", loginID);
            nvc.Add("dateFrom", dateFrom.ToString("yyyy-MM-dd"));
            nvc.Add("dateTo", dateTo.ToString("yyyy-MM-dd"));
            return nvc;
        }
    }

    public class GetWinningNumberResponse : BaseResponse
    {
        [System.Xml.Serialization.XmlElementAttribute("betData")]
        public WinningBetData[] betData;
    }

    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public class WinningBetData
    {
        public string betID;
        public string loginID;
        public string dateTime;
        public string drawType;
        public string drawDate;
        public string betNumber;
        public string strikePosition;
        public decimal bet;
        public decimal strike;
    }


    public class UpdatePasswordStatusRequest : WIN28BaseRequest
    {
        public string newpass;
        public string newStatus;

        public override string GetPage()
        {
            return "changePasswordStatus.aspx";
        }

        public override NameValueCollection GetParam()
        {
            NameValueCollection nvc = new NameValueCollection();
            nvc.Add("apiUser", apiUser);
            nvc.Add("apiPass", apiPass);
            nvc.Add("user", user);
            nvc.Add("pass", pass);
            nvc.Add("loginID", loginID);
            nvc.Add("newpass", newpass);
            nvc.Add("newStatus", newStatus);
            return nvc;
        }
    }

    public class UpdatePasswordStatusResponse : BaseResponse
    {
    }



    public class WithdrawRequest : WIN28BaseRequest
    {
        public decimal amount;

        public override string GetPage()
        {
            return "withdraw.aspx";
        }

        public override NameValueCollection GetParam()
        {
            NameValueCollection nvc = new NameValueCollection();
            nvc.Add("apiUser", apiUser);
            nvc.Add("apiPass", apiPass);
            nvc.Add("user", user);
            nvc.Add("pass", pass);
            nvc.Add("loginID", loginID);
            nvc.Add("amount", amount.ToString());
            return nvc;
        }
    }

    public class WithdrawResponse : BaseResponse
    {
    }



    public class Test
    {
        public static void Main() // Can also be Main(string[] args)
        {
            // Use using statement for IDisposable objects
            using (WIN28 win28 = new WIN28("http://api.coba8.com",
                                           "ar112",
                                           "xR1SV3vac",
                                           "ar112",
                                           "998822",
                                           30)) // Timeout is optional
            {
                //Console.WriteLine("Attempting deposit...");
                //if (win28.Deposit("mitjennyd", 100))
                //{
                //    Console.WriteLine("Deposit successful!");
                //    // Access response details if needed (assuming DepositResponse has relevant fields)
                //    Console.WriteLine($"Transaction ID (example): {win28.DepositResponse?.errorCode}"); // Just an example
                //}
                //else
                //{
                //    Console.WriteLine($"Deposit failed: {win28.ErrorMessage}");
                //    if (win28.ParseError)
                //    {
                //        Console.WriteLine("Response parsing failed.");
                //        Console.WriteLine($"Raw Response: {win28.Response}");
                //    }
                //}

                //Console.WriteLine("\nAttempting GetProfile...");
                //if (win28.GetProfile("mitjennyd"))
                //{
                //    Console.WriteLine("GetProfile successful!");


                //}
                //else
                //{
                //    Console.WriteLine($"GetProfile failed: {win28.ErrorMessage}");
                //    if (win28.ParseError)
                //    {
                //        Console.WriteLine("Response parsing failed.");
                //        Console.WriteLine($"Raw Response: {win28.Response}");
                //    }
                //}


                //Console.WriteLine("\nAttempting BetLogin...");
                //if (win28.BetLogin("mitjennyd", "A123123", true)) // Mobile = true
                //{
                //    Console.WriteLine("BetLogin successful!");
                //    Console.WriteLine($"Token: {win28.BetLoginResponse?.tokenCode}"); // Access response data
                //    Console.WriteLine($"SessionID: {win28.BetLoginResponse?.sessionID}");

                //    // Construct URLs based on response
                //    bool isMobile = true; // Based on the parameter passed to BetLogin
                //    string url;
                //if (isMobile)
                //{
                //    url = $"http://api.coba8.com/wap/main.aspx?user=mitjennyd&tokenCode={win28.BetLoginResponse.tokenCode}&sessionID={win28.BetLoginResponse.sessionID}";
                //}
                //else
                //{
                //    url = $"http://api.coba8.com/bet.aspx?user=mitjennyd&tokenCode={win28.BetLoginResponse.tokenCode}&sessionID={win28.BetLoginResponse.sessionID}";
                //}
                //    Console.WriteLine($"Login URL: {url}");
                //}
                //else
                //{
                //    Console.WriteLine($"BetLogin failed: {win28.ErrorMessage}");
                //    if (win28.ParseError)
                //    {
                //        Console.WriteLine("Response parsing failed.");
                //        Console.WriteLine($"Raw Response: {win28.Response}");
                //    }
                //}



            } // win28.Dispose() is automatically called here

            Console.WriteLine("\nPress any key to exit.");
            Console.ReadKey();
        }
    }
}