using GameRecord.Gamess.OctoPlay.Models;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Web.Services.Description;

namespace GameRecord.Gamess.OctoPlay
{
    public class OctoPlay : IDisposable
    {
        private string apiUrl;
        private string privateKey;
        private string publicKey;
        private string brandId;
        private int timeOut;
        public string ErrorMessage { get; private set; }
        public CreateWalletResponse CreateWalletResponse { get; private set; }
        public FetchWalletResponse FetchWalletResponse { get; private set; }
        public TransferWalletResponse TransferWalletResponse { get; private set; }
        public GameListResponse GameListResponse { get; private set; }
        public GameLaunchResponse GameLaunchResponse { get; private set; }
        public ErrorResponse ErrorResponse { get; private set; }

        public OctoPlay(string apiUrl, string privateKey, string publicKey, string brandId, int timeOut)
        {
            this.apiUrl = apiUrl;
            this.publicKey = publicKey;
            this.privateKey = privateKey;
            this.brandId = brandId;
            this.timeOut = timeOut;
        }

        private IRestResponse ExecutePostRequest(string endpoint, object requestModel)
        {
            var client = new RestClient(apiUrl + endpoint);
            var request = new RestRequest(Method.POST);
            request.AddHeader("content-type", "application/json");

            var jsonSettings = new JsonSerializerSettings
            {
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
            };
            var requestBody = JsonConvert.SerializeObject(requestModel, jsonSettings);

            var md5Hash = GenerateMD5Hash(requestBody);
            var toSign = $"{HttpMethod.Post}\n{apiUrl + endpoint}\n{md5Hash}";

            Console.WriteLine("toSign: " + toSign);

            var generatedSignature = GenerateHMACSHA256(toSign, privateKey);

            request.AddHeader("Signature", generatedSignature);
            request.AddHeader("Public-Key", publicKey ?? string.Empty);
            request.AddParameter("application/json", requestBody, ParameterType.RequestBody);
            request.Timeout = timeOut * 1000;

            return client.Execute(request);
        }

        private string GenerateMD5Hash(string input)    
        {
            using (var md5 = MD5.Create())
            {
                var inputBytes = Encoding.UTF8.GetBytes(input);
                var hashBytes = md5.ComputeHash(inputBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToUpper();
            }
        }

        private string GenerateHMACSHA256(string input, string key)
        {
            // Convert the secret key and message to bytes
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] messageBytes = Encoding.UTF8.GetBytes(input);

            // Compute the HMAC SHA256 hash
            using (var hmac = new HMACSHA256(keyBytes))
            {
                byte[] hashBytes = hmac.ComputeHash(messageBytes);

                // Convert to lowercase hexadecimal string (same as cryptoJS.enc.Hex)
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        public bool CreateWallet(CreateWalletRequest createWalletRequest)
        {
            try
            {
                var response = ExecutePostRequest("wallet/create", createWalletRequest);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    CreateWalletResponse = JsonConvert.DeserializeObject<CreateWalletResponse>(response.Content);
                    ErrorResponse = null;
                    return true;
                }
                else
                {
                    ErrorResponse = JsonConvert.DeserializeObject<ErrorResponse>(response.Content);
                    CreateWalletResponse = null;
                    ErrorMessage = ErrorResponse?.Description;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool FetchWallet(FetchWalletRequest fetchWalletRequest)
        {
            try
            {
                var response = ExecutePostRequest("wallet/get-balance", fetchWalletRequest);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    FetchWalletResponse = JsonConvert.DeserializeObject<FetchWalletResponse>(response.Content);
                    ErrorResponse = null;
                    return true;
                }
                else
                {
                    ErrorResponse = JsonConvert.DeserializeObject<ErrorResponse>(response.Content);
                    FetchWalletResponse = null;
                    ErrorMessage = ErrorResponse?.Description;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool TransferWallet(TransferWalletRequest transferWalletRequest)
        {
            try
            {
                var response = ExecutePostRequest("wallet/transfer", transferWalletRequest);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    TransferWalletResponse = JsonConvert.DeserializeObject<TransferWalletResponse>(response.Content);
                    ErrorResponse = null;
                    return true;
                }
                else
                {
                    ErrorResponse = JsonConvert.DeserializeObject<ErrorResponse>(response.Content);
                    TransferWalletResponse = null;
                    ErrorMessage = ErrorResponse?.Description;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool GetGameList(GameListRequest gameListRequest)
        {
            try
            {
                var response = ExecutePostRequest("game/list", gameListRequest);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    GameListResponse = JsonConvert.DeserializeObject<GameListResponse>(response.Content);
                    ErrorResponse = null;
                    return true;
                }
                else
                {
                    ErrorResponse = JsonConvert.DeserializeObject<ErrorResponse>(response.Content);
                    GameListResponse = null;
                    ErrorMessage = ErrorResponse?.Description;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool GameLaunch(GameLaunchRequest gameLaunchRequest)
        {
            try
            {
                var response = ExecutePostRequest("game/url", gameLaunchRequest);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    GameLaunchResponse = JsonConvert.DeserializeObject<GameLaunchResponse>(response.Content);
                    ErrorResponse = null;
                    return true;
                }
                else
                {
                    ErrorResponse = JsonConvert.DeserializeObject<ErrorResponse>(response.Content);
                    GameLaunchResponse = null;
                    ErrorMessage = ErrorResponse?.Description;
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