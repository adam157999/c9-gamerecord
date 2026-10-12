using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Ekor
{
    public class Ekor : IDisposable
    {
        #region Declaration
        private string ApiUrl { get; set; }
        private string Sn { get; set; }
        private string ApiSecretKey { get; set; }
        private int TimeOutSec { get; set; }
        private string SuccessCode = "S100";

        public string Parameters { get; set; }
        public string Response { get; set; }
        public string ErrorMessage { get; set; }
        public string Request { get; set; }
        public string TargetApiUrl { get; set; }
        public bool ParseError { get; set; } = false;

        public LoginResponse LoginResponse;
        public GetBalanceResponse GetBalanceResponse;
        public TransferBalanceResponse TransferBalanceResponse;
        public GetRecord2Response GetRecord2Response;

        public Ekor(string ApiUrl, string Sn, string ApiSecretKey, int TimeOutSec = 30)
        {
            this.ApiUrl = ApiUrl;
            this.Sn = Sn;
            this.ApiSecretKey = ApiSecretKey;
            this.TimeOutSec = TimeOutSec;
        }

        private readonly Encoding Charset = Encoding.UTF8;
        #endregion

        #region General Functions
        private bool ConvertJsonToClassObject<T>(string JsonObj, ref T TargetClass)
        {
            try
            {
                TargetClass = JsonConvert.DeserializeObject<T>(JsonObj);
                return true;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        private string MD5Hashing(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        private bool PostJSON<T>(BaseRequest req, ref T resp)
        {
            try
            {
                //Attached brand id and api key for each call
                req.SN = Sn;
                req.APISecretKey = ApiSecretKey;
                Parameters = req.GetParamter();
                req.Signature = MD5Hashing(Parameters);
                Request = JsonConvert.SerializeObject(req);
                byte[] body = Encoding.ASCII.GetBytes(Request);

                TargetApiUrl = ApiUrl + "/" + req.Address;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(TargetApiUrl);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.ContentLength = body.Length;
                request.Timeout = TimeOutSec * 1000;

                using (Stream os = request.GetRequestStream())
                    os.Write(body, 0, body.Length);

                HttpWebResponse objResponse = (HttpWebResponse)request.GetResponse();
                resp = default(T);

                if ((int)objResponse.StatusCode == 200)
                {
                    StreamReader StreamReader;
                    StreamReader = new StreamReader(objResponse.GetResponseStream(), Charset);
                    Response = StreamReader.ReadToEnd().Trim();

                    if (ConvertJsonToClassObject<T>(Response, ref resp))
                        return true;

                    return false;
                }
                else
                {
                    ErrorMessage = objResponse.StatusDescription;
                    return false;
                }
            }
            catch (WebException ex1)
            {
                HttpWebResponse errorResponse = (HttpWebResponse)ex1.Response;
                StreamReader StreamReader;
                StreamReader = new StreamReader(errorResponse.GetResponseStream(), Charset);
                Response = StreamReader.ReadToEnd().Trim();
                if (ConvertJsonToClassObject<T>(Response, ref resp))
                {
                    ErrorMessage = "";
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }
        #endregion

        #region Api Functions
        public bool Login(string ID, string LoginId)
        {
            LoginRequest req = new LoginRequest()
            {
                ID = ID,
                LoginId = LoginId,
            };

            try
            {
                if (PostJSON<LoginResponse>(req, ref LoginResponse))
                {
                    if (LoginResponse != null && LoginResponse.Code.Equals(SuccessCode))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool GetBalance(string Id, string LoginId)
        {
            GetBalanceRequest req = new GetBalanceRequest()
            {
                ID = Id,
                LoginId = LoginId,
            };

            try
            {
                if (PostJSON<GetBalanceResponse>(req, ref GetBalanceResponse))
                {
                    if (GetBalanceResponse != null && GetBalanceResponse.Code.Equals(SuccessCode))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool TransferBalance(string ID, string LoginId, decimal AddReward, decimal AddBalance)
        {
            TransferBalanceRequest req = new TransferBalanceRequest()
            {
                ID = ID,
                LoginId = LoginId,
                AddReward = AddReward,
                AddBalance = AddBalance,
            };

            try
            {
                if (PostJSON<TransferBalanceResponse>(req, ref TransferBalanceResponse))
                {
                    if (TransferBalanceResponse != null && TransferBalanceResponse.Code.Equals(SuccessCode))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool GetRecord2(string ID, string GE_CreationDate)
        {
            GetRecord2Request req = new GetRecord2Request()
            {
                ID = ID,
                GE_CreationDate = GE_CreationDate,
            };

            try
            {
                if (PostJSON<GetRecord2Response>(req, ref GetRecord2Response))
                {
                    if (GetRecord2Response != null && GetRecord2Response.Code.Equals(SuccessCode))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
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
    public abstract class BaseRequest
    {
        public string ID { get; set; }
        public string Method { get; set; }
        public string SN { get; set; }
        public string Signature { get; set; }
        internal string Address { get; set; }
        internal string APISecretKey { get; set; }
        public abstract string GetParamter();
    }

    public class BaseResposne
    {
        public string Code { get; set; }
    }

    public class LoginRequest : BaseRequest
    {
        public LoginRequest()
        {
            Address = "Ekor/API";
            Method = "LOGIN";
        }

        public string LoginId { get; set; }

        public override string GetParamter()
        {
            return ID + Method + SN + LoginId + APISecretKey;
        }
    }

    public class LoginResponse : BaseResposne
    {
        public string Body { get; set; }
    }

    public class GetBalanceRequest : BaseRequest
    {
        public GetBalanceRequest()
        {
            Address = "Ekor/API";
            Method = "GETBALANCE";
        }

        public string LoginId { get; set; }

        public override string GetParamter()
        {
            return ID + Method + SN + LoginId + APISecretKey;
        }
    }

    public class GetBalanceResponse : BaseResposne
    {
        public GetBalanceResponse_Body Body { get; set; }
    }

    public class GetBalanceResponse_Body
    {
        public int Id { get; set; }
        public string PlayerId { get; set; }
        public decimal Reward { get; set; }
        public decimal Balance { get; set; }

        public DateTime CreationDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
    }

    public class TransferBalanceRequest : BaseRequest
    {
        public TransferBalanceRequest()
        {
            Address = "Ekor/API";
            Method = "TRANSFERBALANCE";
        }

        public string LoginId { get; set; }
        public decimal AddReward { get; set; }
        public decimal AddBalance { get; set; }

        public override string GetParamter()
        {
            return ID + Method + SN + LoginId + APISecretKey;
        }
    }

    public class TransferBalanceResponse : BaseResposne
    {
        public decimal Balance { get; set; }
    }

    public class GetRecord2Request : BaseRequest
    {
        public GetRecord2Request()
        {
            Address = "Ekor/API";
            Method = "GETRECORD2";
        }

        public string GE_CreationDate { get; set; }

        public override string GetParamter()
        {
            return ID + Method + SN + APISecretKey;
        }
    }

    public class GetRecord2Response : BaseResposne
    {
        public List<GetRecord2Response_Item> Body { get; set; }
    }

    public class GetRecord2Response_Item
    {
        public long Id { get; set; }
        public long OrderId { get; set; }
        public string GameId { get; set; }
        public string UserId { get; set; }
        public string PlayerId { get; set; }
        public string BetCurrency { get; set; }
        public decimal BetExchangeRate { get; set; }
        public string InputRef { get; set; }
        public string OutputRef { get; set; }
        public decimal BetAmount { get; set; }
        public decimal BetAmountReward { get; set; }
        public decimal WinAmount { get; set; }
        public string BetPlatform { get; set; }
        public DateTime? FirstDrawDate { get; set; }
        public DateTime? LastDrawDate { get; set; }
        public int Status { get; set; }
        public DateTime? VoidDate { get; set; }
        public DateTime? ActionDate { get; set; }
        public string ActionBy { get; set; }
        public DateTime CreationDate { get; set; }
    }
    #endregion
}
