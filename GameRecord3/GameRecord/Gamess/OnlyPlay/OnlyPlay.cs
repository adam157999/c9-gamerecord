using GameRecord.Gamess.OnlyPlay.Models;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Linq;

namespace GameRecord.Gamess.OnlyPlay
{
    public class OnlyPlay : IDisposable
    {
        private string apiUrl;
        private string operatorName;
        private string secretKey;
        private int timeOut;
        private string successCode = "0";

        public string ErrorMessage { get; private set; }
        public CreateAccountResponse CreateAccountResponse { get; private set; }
        public GetBalanceResponse GetBalanceResponse { get; private set; }
        public DepositResponse DepositResponse { get; private set; }
        public WithdrawalResponse WithdrawalResponse { get; private set; }
        public GetEgamesListResponse GetEgamesListResponse { get; private set; }
        public StartGameResponse StartGameResponse { get; private set; }

        public OnlyPlay(string apiUrl, string operatorName, string secretKey, int timeOut)
        {
            this.apiUrl = apiUrl;
            this.operatorName = operatorName;
            this.secretKey = secretKey;
            this.timeOut = timeOut;
        }

        private string GenerateToken(object requestModel)
        {
            // Convert request model to query string format
            var parameters = string.Join("&", requestModel.GetType().GetProperties().Select(p => $"{p.Name}={Uri.EscapeDataString(p.GetValue(requestModel)?.ToString() ?? string.Empty)}"));

            // Concatenate parameters with the secret key
            string dataToEncrypt = secretKey + parameters;

            // Generate MD5 hash
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(dataToEncrypt);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert hash to hexadecimal string
                return string.Concat(hashBytes.Select(b => b.ToString("x2")));
            }
        }

        private IRestResponse ExecutePostRequest(string endpoint, object requestModel)
        {
            // Generate token
            string tokenValue = GenerateToken(requestModel);

            //// Convert request model to query string format
            //var parameters = string.Join("&", requestModel.GetType().GetProperties().Select(p => $"{p.Name}={Uri.EscapeDataString(p.GetValue(requestModel)?.ToString() ?? string.Empty)}"));

            //// Append token to the parameters
            //parameters += $"&Token={tokenValue}";

            var client = new RestClient(apiUrl + endpoint);
            var request = new RestRequest(Method.POST);

            request.Timeout = timeOut * 1000;

            foreach (var prop in requestModel.GetType().GetProperties())
            {
                var value = prop.GetValue(requestModel)?.ToString() ?? string.Empty;
                request.AddParameter(prop.Name, value);
            }

            request.AddParameter("Token", tokenValue);
            request.AlwaysMultipartFormData = false;
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");

            return client.Execute(request);
        }

        public bool CreateAccount(CreateAccountRequest createAccountRequest)
        {
            try
            {
                var response = ExecutePostRequest("/api/CreateAccount", createAccountRequest);
                CreateAccountResponse = JsonConvert.DeserializeObject<CreateAccountResponse>(response.Content);
                if (CreateAccountResponse?.ErrorCode == int.Parse(successCode))
                {
                    return true;
                }
                else
                {
                    ErrorMessage = CreateAccountResponse?.Message;
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
                var response = ExecutePostRequest("/api/GetBalance", getBalanceRequest);
                GetBalanceResponse = JsonConvert.DeserializeObject<GetBalanceResponse>(response.Content);
                if (GetBalanceResponse?.ErrorCode == int.Parse(successCode))
                {
                    return true;
                }
                else
                {
                    ErrorMessage = GetBalanceResponse?.Message;
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
                var response = ExecutePostRequest("/api/Deposit", depositRequest);
                DepositResponse = JsonConvert.DeserializeObject<DepositResponse>(response.Content);
                if (DepositResponse?.ErrorCode == int.Parse(successCode))
                {
                    return true;
                }
                else
                {
                    ErrorMessage = DepositResponse?.Message;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool Withdraw(WithdrawalRequest withdrawalRequest)
        {
            try
            {
                var response = ExecutePostRequest("/api/Withdrawal", withdrawalRequest);
                WithdrawalResponse = JsonConvert.DeserializeObject<WithdrawalResponse>(response.Content);
                if (WithdrawalResponse?.ErrorCode == int.Parse(successCode))
                {
                    return true;
                }
                else
                {
                    ErrorMessage = WithdrawalResponse?.Message;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool GetEgamesList(GetEgamesListRequest getEgamesListRequest)
        {
            try
            {
                var response = ExecutePostRequest("/api/GetEgamesList", getEgamesListRequest);
                GetEgamesListResponse = JsonConvert.DeserializeObject<GetEgamesListResponse>(response.Content);
                if (GetEgamesListResponse?.ErrorCode == int.Parse(successCode))
                {
                    return true;
                }
                else
                {
                    ErrorMessage = GetEgamesListResponse?.Message;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool StartGame(StartGameRequest startGameRequest)
        {
            try
            {
                var response = ExecutePostRequest("/api/StartGame", startGameRequest);
                StartGameResponse = JsonConvert.DeserializeObject<StartGameResponse>(response.Content);
                if (StartGameResponse?.ErrorCode == int.Parse(successCode))
                {
                    return true;
                }
                else
                {
                    ErrorMessage = StartGameResponse?.Message;
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