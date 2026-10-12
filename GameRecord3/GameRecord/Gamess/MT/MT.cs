using GameRecord.Gamess.MT.Models;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Threading;

namespace GameRecord.Gamess.MT
{
    public class MT : IDisposable
    {
        private string apiUrl;
        private string clientId;
        private string clientSecret;
        private string desKey;
        private string desIV;
        private int timeOut;
        private string successCode = "00000";

        public string ErrorMessage { get; private set; }
        public CreateUserResponse CreateUserResponse { get; private set; }
        public GetBalanceResponse GetBalanceResponse { get; private set; }
        public WithdrawResponse WithdrawResponse { get; private set; }
        public DepositResponse DepositResponse { get; private set; }
        public GetURLTokenResponse GetURLTokenResponse { get; private set; }

        public KickOutResponse KickOutResponse { get; private set; }

        public MT(string apiUrl, string clientId, string clientSecret, string desKey, string desIV, string systemCode, string webId, int timeOut)
        {
            this.apiUrl = apiUrl;
            this.clientId = clientId;
            this.clientSecret = clientSecret;
            this.desKey = desKey;
            this.desIV = desIV;
            this.timeOut = timeOut;
        }

        private IRestResponse ExecutePostRequest(string endpoint, object requestModel)
        {
            var client = new RestClient(apiUrl + endpoint);
            var request = new RestRequest(Method.POST);
            string timestamp = GetUnixTimestamp();
            // Adding default headers
            request.AddHeader("APICI", clientId); // Connection Endpoint ID for the Request
            request.AddHeader("APISI", GenerateSignature(requestModel, timestamp)); // Signature for the Request
            request.AddHeader("APITS", timestamp); // Request Timestamp, formatted as Unix Timestamp
            // Encrypt the request model
            string encryptedData = EncryptData(JsonConvert.SerializeObject(requestModel));
            request.AlwaysMultipartFormData = true;
            // Adding encrypted data as form-data
            request.AddParameter("msg", encryptedData, ParameterType.GetOrPost);

            request.Timeout = timeOut * 1000;
            return client.Execute(request);
        }

        private string GenerateSignature(object requestModel, string timestamp)
        {
            // Convert request model to JSON
            string jsonData = JsonConvert.SerializeObject(requestModel);

            // Encrypt the JSON data
            string encryptedData = EncryptData(jsonData);

            // Concatenate timestamp, client secret, client ID, and encrypted data

            string signatureData = timestamp + clientSecret + clientId + encryptedData;

            // Generate MD5 hash
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(signatureData);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert hash to hexadecimal string
                return string.Concat(hashBytes.Select(b => b.ToString("x2")));
            }
        }

        private string EncryptData(string data)
        {
            // DES-CBC encryption
            using (var des = System.Security.Cryptography.DES.Create())
            {
                // Ensure the key and IV are valid sizes for DES
                byte[] keyBytes = System.Text.Encoding.UTF8.GetBytes(this.desKey);
                byte[] ivBytes = System.Text.Encoding.UTF8.GetBytes(this.desIV);

                if (keyBytes.Length != 8 || ivBytes.Length != 8)
                {
                    throw new ArgumentException("DES key and IV must be exactly 8 bytes long.");
                }

                des.Key = keyBytes;
                des.IV = ivBytes;
                des.Mode = System.Security.Cryptography.CipherMode.CBC;
                des.Padding = System.Security.Cryptography.PaddingMode.PKCS7;

                var dataBytes = System.Text.Encoding.UTF8.GetBytes(data);
                using (var encryptor = des.CreateEncryptor())
                {
                    var encryptedBytes = encryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length);
                    return Convert.ToBase64String(encryptedBytes);
                }
            }
        }

        private string GetUnixTimestamp()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        }

        public bool CreateUser(CreateUserRequest createUserRequest)
        {
            try
            {
                var response = ExecutePostRequest("Player/CreateUser", createUserRequest);
                CreateUserResponse = JsonConvert.DeserializeObject<CreateUserResponse>(response.Content);
                if (CreateUserResponse?.code == successCode)
                {
                    return true;
                }
                else
                {
                    ErrorMessage = CreateUserResponse?.message;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool GetBalance(GetBalanceRequest getBalanceRequest)
        {
            try
            {
                var response = ExecutePostRequest("Player/GetBalance", getBalanceRequest);
                GetBalanceResponse = JsonConvert.DeserializeObject<GetBalanceResponse>(response.Content);
                if (GetBalanceResponse?.code == successCode)
                {
                    return true;
                }
                else
                {
                    ErrorMessage = GetBalanceResponse?.message;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool Withdraw(WithdrawRequest withdrawRequest)
        {
            try
            {
                var response = ExecutePostRequest("Player/Withdraw", withdrawRequest);
                WithdrawResponse = JsonConvert.DeserializeObject<WithdrawResponse>(response.Content);
                if (WithdrawResponse?.code == successCode)
                {
                    return true;
                }
                else
                {
                    ErrorMessage = WithdrawResponse?.message;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool Deposit(DepositRequest depositRequest)
        {
            try
            {
                var response = ExecutePostRequest("Player/Deposit", depositRequest);
                DepositResponse = JsonConvert.DeserializeObject<DepositResponse>(response.Content);
                if (DepositResponse?.code == successCode)
                {
                    return true;
                }
                else
                {
                    ErrorMessage = DepositResponse?.message;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool GetGameURLToken(GetURLTokenRequest getURLTokenRequest)
        {
            try
            {
                var response = ExecutePostRequest("Player/GetURLToken", getURLTokenRequest);
                GetURLTokenResponse = JsonConvert.DeserializeObject<GetURLTokenResponse>(response.Content);
                if (GetURLTokenResponse?.code == successCode)
                {
                    return true;
                }
                else
                {
                    ErrorMessage = GetURLTokenResponse?.message;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool KickOutPlayer(kickOutRequest kickOutRequest)
        {
            try
            {
                var response = ExecutePostRequest("Player/Kickout", kickOutRequest);
                KickOutResponse = JsonConvert.DeserializeObject<KickOutResponse>(response.Content);
                if (KickOutResponse?.code == successCode)
                {
                    return true;
                }
                else
                {
                    ErrorMessage = GetURLTokenResponse?.message;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public void Dispose()
        {
            // Dispose resources if needed
        }
    }
}