using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace GameRecord.Gamess.SpadeGaming
{
    public class SpadeGamingFish : IDisposable
    {
        private string P_Parameters;

        private string P_Response;

        private string p_ErrorMessage;

        private string p_Request;

        private string p_TargetApiUrl;

        private bool p_ParseError = false;

        private DepositResponse p_DepositResponse;

        private WithdrawResponse p_WithdrawResponse;

        private GetAcctInfoResponse p_GetAcctInfoResponse;

        private GetBetHistoryResponse p_GetBetHistoryResponse;

        private GetGameUrlResponse p_GetGameUrlResponse;

        private KickAcctResponse p_KickAcctResponse;

        private readonly Encoding Charset = Encoding.UTF8;

        private readonly string AcceptEncoding = "Accept-Encoding";

        private readonly string AcceptLanguage = "Accept-Language";

        private readonly string ContentEncoding = "Content-Encoding";

        private readonly string Api = "API";

        private readonly string DataType = "DataType";

        private readonly string Disgest = "Digest";

        private string P_apiUrl { get; set; }

        private string P_merchantCode { get; set; }

        private string P_secretKey { get; set; }

        private string P_currency { get; set; }

        private int P_timeOutSec { get; set; }

        public string Parameters => P_Parameters;

        public string Response => P_Response;

        public string ErrorMessage => p_ErrorMessage;

        public string Request => p_Request;

        public string TargetApiUrl => p_TargetApiUrl;

        public bool ParseError => p_ParseError;

        public DepositResponse DepositResponses => p_DepositResponse;

        public WithdrawResponse WithdrawResponses => p_WithdrawResponse;

        public GetAcctInfoResponse GetAcctInfoResponses => p_GetAcctInfoResponse;

        public GetGameUrlResponse GetGameUrlResponses => p_GetGameUrlResponse;
        public GetBetHistoryResponse GetBetHistoryResponses => p_GetBetHistoryResponse;

        public KickAcctResponse KickAcctResponses => p_KickAcctResponse;

        public SpadeGamingFish(string apiUrl, string merchantCode, string currency, int timeOutSec = 30)
        {
            P_apiUrl = apiUrl + ((apiUrl.Substring(apiUrl.Length - 1) != "/") ? "/" : "");
            P_merchantCode = merchantCode;
            P_currency = currency;
            P_timeOutSec = timeOutSec;
        }

        private bool ConvertJsonToClassObject<T>(string JsonObj, ref T TargetClass)
        {
            try
            {
                TargetClass = JsonConvert.DeserializeObject<T>(JsonObj);
                return true;
            }
            catch (Exception ex)
            {
                p_ErrorMessage = ex.Message;
                return false;
            }
        }

        private bool PostJSON<T>(BaseRequest req, ref T resp)
        {
            try
            {
                req.serialNo = Guid.NewGuid().ToString();
                req.merchantCode = P_merchantCode;
                p_Request = JsonConvert.SerializeObject(req);
                string s = JsonConvert.SerializeObject(req);
                byte[] bytes = Charset.GetBytes(s);
                string value = ComputeHash(bytes);
                p_TargetApiUrl = P_apiUrl;
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(p_TargetApiUrl);
                httpWebRequest.Method = "POST";
                httpWebRequest.ContentLength = bytes.Length;
                httpWebRequest.Headers.Add(Api, req.Method);
                httpWebRequest.Headers.Add(DataType, "JSON");
                httpWebRequest.Headers.Add(AcceptEncoding, "gzip");
                httpWebRequest.AutomaticDecompression = DecompressionMethods.GZip;
                httpWebRequest.Headers.Add(Disgest, value);
                using (Stream stream = httpWebRequest.GetRequestStream())
                {
                    stream.Write(bytes, 0, bytes.Length);
                }

                HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                resp = default(T);
                if (httpWebResponse.StatusCode == HttpStatusCode.OK)
                {
                    StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream(), Charset);
                    P_Response = streamReader.ReadToEnd().Trim();
                    if (ConvertJsonToClassObject(P_Response, ref resp))
                    {
                        return true;
                    }

                    return false;
                }

                p_ErrorMessage = httpWebResponse.StatusDescription;
                return false;
            }
            catch (WebException ex)
            {
                HttpWebResponse httpWebResponse2 = (HttpWebResponse)ex.Response;
                StreamReader streamReader2 = new StreamReader(httpWebResponse2.GetResponseStream(), Charset);
                P_Response = streamReader2.ReadToEnd().Trim();
                if (ConvertJsonToClassObject(P_Response, ref resp))
                {
                    p_ErrorMessage = "";
                    return true;
                }

                return false;
            }
            catch (Exception ex2)
            {
                p_ErrorMessage = ex2.Message;
                return false;
            }
        }

        public string ComputeHash(byte[] body)
        {
            HashAlgorithm hashAlgorithm = new MD5CryptoServiceProvider();
            byte[] array = hashAlgorithm.ComputeHash(body);
            return BitConverter.ToString(array).Replace("-", string.Empty);
        }

        public bool Deposit(string acctId, decimal amount)
        {
            DepositRequest req = new DepositRequest
            {
                acctId = acctId,
                currency = P_currency,
                amount = amount
            };
            try
            {
                if (PostJSON(req, ref p_DepositResponse))
                {
                    if (p_DepositResponse != null && p_DepositResponse.code == 0 && Convert.ToString(p_DepositResponse.msg).Trim().ToUpper() == "SUCCESS")
                    {
                        return true;
                    }

                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                p_ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool Withdraw(string acctId, decimal amount)
        {
            WithdrawRequest req = new WithdrawRequest
            {
                acctId = acctId,
                currency = P_currency,
                amount = amount
            };
            try
            {
                if (PostJSON(req, ref p_WithdrawResponse))
                {
                    if (p_WithdrawResponse != null && p_WithdrawResponse.code == 0 && Convert.ToString(p_WithdrawResponse.msg).Trim().ToUpper() == "SUCCESS")
                    {
                        return true;
                    }

                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                p_ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool GetAcctInfo(string acctId)
        {
            GetAcctInfoRequest req = new GetAcctInfoRequest
            {
                acctId = acctId
            };
            try
            {
                if (PostJSON(req, ref p_GetAcctInfoResponse))
                {
                    if (p_GetAcctInfoResponse != null && p_GetAcctInfoResponse.code == 0 && Convert.ToString(p_GetAcctInfoResponse.msg).Trim().ToUpper() == "SUCCESS" && p_GetAcctInfoResponse.list != null)
                    {
                        return true;
                    }

                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                p_ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool GetBetHistory(DateTime beginDate, DateTime endDate, int pageIndex)
        {
            GetBetHistoryRequest req = new GetBetHistoryRequest
            {
                beginDate = beginDate.ToString("yyyyMMddTHHmmss"),
                endDate = endDate.ToString("yyyyMMddTHHmmss"),
                pageIndex = pageIndex
            };
            try
            {
                if (PostJSON(req, ref p_GetBetHistoryResponse))
                {
                    if (p_GetBetHistoryResponse != null && p_GetBetHistoryResponse.code == 0 && Convert.ToString(p_GetBetHistoryResponse.msg).Trim().ToUpper() == "SUCCESS")
                    {
                        return true;
                    }

                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                p_ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool KickAcct(string acctId)
        {
            KickAcctRequest req = new KickAcctRequest
            {
                acctId = acctId
            };
            try
            {
                if (PostJSON(req, ref p_KickAcctResponse))
                {
                    if (p_KickAcctResponse != null && p_KickAcctResponse.code == 0 && Convert.ToString(p_KickAcctResponse.msg).Trim().ToUpper() == "SUCCESS")
                    {
                        return true;
                    }

                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                p_ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool GetGameUrl(AcctInfo acctInfo, bool isMobile, string lang, string token, string gameCode, string ipAddress)
        {
            GetGameUrlRequest req = new GetGameUrlRequest
            {
                acctInfo = acctInfo,
                language = lang,
                token = token,
                game = gameCode,
                acctIp = ipAddress,
                mobile = isMobile
            };

            try
            {
                if (PostJSON(req, ref p_GetGameUrlResponse))
                {
                    if (p_GetGameUrlResponse != null && p_GetGameUrlResponse.code == 0 && Convert.ToString(p_GetGameUrlResponse.msg).Trim().ToUpper() == "SUCCESS")
                    {
                        return true;
                    }
                    return false;
                }
                return false;
            }
            catch (Exception ex)
            {
                p_ErrorMessage = ex.Message;
                return false;
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        #region Model Classes

        public class BaseRequest
        {
            public string serialNo { get; set; }

            public string merchantCode { get; set; }

            internal string Method { get; set; }
        }

        public class BaseResponse
        {
            public string serialNo { get; set; }

            public int code { get; set; }

            public string msg { get; set; }

            public string merchantCode { get; set; }
        }

        public class GetBetHistoryRequest : BaseRequest
        {
            public string beginDate { get; set; }

            public string endDate { get; set; }

            public int pageIndex { get; set; }

            public GetBetHistoryRequest()
            {
                base.Method = "getBetHistory";
            }
        }

        public class KickAcctRequest : BaseRequest
        {
            public string acctId { get; set; }

            public KickAcctRequest()
            {
                base.Method = "kickAcct";
            }
        }

        public class GetAcctInfoRequest : BaseRequest
        {
            public string acctId { get; set; }

            public GetAcctInfoRequest()
            {
                base.Method = "getAcctInfo";
            }
        }
        public class WithdrawRequest : BaseRequest
        {
            public string acctId { get; set; }

            public string currency { get; set; }

            public decimal amount { get; set; }

            public WithdrawRequest()
            {
                base.Method = "withdraw";
            }
        }

        public class GetGameUrlRequest : BaseRequest
        {
            public AcctInfo acctInfo { get; set; }
            public string language { get; set; }
            public string token { get; set; }
            public string game { get; set; }
            public string acctIp { get; set; }
            public bool mobile { get; set; }

            public GetGameUrlRequest()
            {
                base.Method = "getAuthorize";
            }
        }

        public class WithdrawResponse : BaseResponse
        {
            public string transactionId { get; set; }

            public decimal afterBalance { get; set; }
        }

        public class DepositRequest : BaseRequest
        {
            public string acctId { get; set; }

            public string currency { get; set; }

            public decimal amount { get; set; }

            public DepositRequest()
            {
                base.Method = "deposit";
            }
        }

        public class DepositResponse : BaseResponse
        {
            public string transactionId { get; set; }

            public decimal afterBalance { get; set; }
        }

        public class GetAcctInfoResponse : BaseResponse
        {
            public int resultCount { get; set; }

            public int pageCount { get; set; }

            public List<AcctInfo> list { get; set; }
        }

        public class GetBetHistoryResponse : BaseResponse
        {
            public int resultCount { get; set; }

            public int pageCount { get; set; }

            public List<BetInfo> list { get; set; }
        }

        public class KickAcctResponse : BaseResponse
        {
            public List<string> list { get; set; }
        }

        public class GetGameUrlResponse : BaseResponse
        {
            public string token { get; set; }
            public string gameUrl { get; set; }
        }

        public class BetInfo
        {
            public string ticketId { get; set; }

            public string acctId { get; set; }

            public string ticketTime { get; set; }

            public string categoryId { get; set; }

            public string gameCode { get; set; }

            public string currency { get; set; }

            public decimal betAmount { get; set; }

            public string result { get; set; }

            public decimal winLoss { get; set; }

            public decimal jackpotAmount { get; set; }

            public string betIp { get; set; }

            public long? luckyDrawId { get; set; }

            public bool? completed { get; set; }

            public long roundId { get; set; }

            public int sequence { get; set; }

            public string channel { get; set; }

            public decimal balance { get; set; }

            public decimal jpWin { get; set; }
        }

        public class AcctInfo
        {
            public string acctId { get; set; }

            public string userName { get; set; }

            public string currency { get; set; }

            public decimal balance { get; set; }

        }

        #endregion
    }
}