using GameRecord.Gamess.BTI.Models;
using RestSharp;
using System;
using System.IO;
using System.Xml.Serialization;

namespace GameRecord.Gamess.BTI
{
    public class BTI : IDisposable
    {
        private string apiUrl;
        private string agentUserName;
        private string agentPassword;
        private int timeOut;
        public string ErrorMessage { get; private set; }
        public CreateUserNewResponse CreateUserNewResponse { get; private set; }
        public GetBalanceResponse GetBalanceResponse { get; private set; }
        public GetCustomerAuthTokenResponse GetCustomerAuthTokenResponse { get; private set; }
        public GetCustomerByMerchantCodeResponse GetCustomerByMerchantCodeResponse { get; private set; }
        public DepositResponse DepositResponse { get; private set; }
        public WithdrawResponse WithdrawResponse { get; private set; }
        public ErrorResponse ErrorResponse { get; private set; }

        public BTI(string apiUrl, string agentUserName, string agentPassword, int timeOut)
        {
            this.apiUrl = apiUrl;
            this.agentUserName = agentUserName;
            this.agentPassword = agentPassword;
            this.timeOut = timeOut;
        }

        private IRestResponse ExecutePostRequest(string endpoint, object requestModel)
        {
            var client = new RestClient(apiUrl + endpoint);
            client.Timeout = timeOut * 1000;
            var request = new RestRequest(Method.POST);
            request.AddHeader("content-type", "application/x-www-form-urlencoded");

            // Add each property as a form parameter
            var properties = requestModel.GetType().GetProperties();
            foreach (var prop in properties)
            {
                var value = prop.GetValue(requestModel);
                if (value != null)
                {
                    request.AddParameter(prop.Name, value.ToString(), ParameterType.GetOrPost);
                }
            }

            return client.Execute(request);
        }

        public bool CreateUserNew(CreateUserNewRequest requestModel)
        {
            try
            {
                requestModel.AgentUserName = agentUserName;
                requestModel.AgentPassword = agentPassword;

                var response = ExecutePostRequest("/CreateUserNew", requestModel);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    try
                    {
                        var xmlSerializer = new XmlSerializer(typeof(CreateUserNewResponse));
                        using (var stringReader = new StringReader(response.Content))
                        {
                            CreateUserNewResponse = (CreateUserNewResponse)xmlSerializer.Deserialize(stringReader);
                        }
                        if (CreateUserNewResponse.ErrorCode == "NoError")
                        {
                            return true;
                        }
                        else
                        {
                            ErrorMessage = $"ErrorCode: {CreateUserNewResponse.ErrorCode}";
                            return false;
                        }
                    }
                    catch (Exception xmlEx)
                    {
                        ErrorMessage = $"XML Deserialization Error: {xmlEx.Message}. Response Content: {response.Content}";
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = response.ErrorMessage ?? $"Request failed with status code: {response.StatusCode}. Response: {response.Content}";
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool GetBalance(GetBalanceRequest requestModel)
        {
            try
            {
                requestModel.AgentUserName = agentUserName;
                requestModel.AgentPassword = agentPassword;

                var response = ExecutePostRequest("/GetBalance", requestModel);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    try
                    {
                        var xmlSerializer = new XmlSerializer(typeof(GetBalanceResponse));
                        using (var stringReader = new StringReader(response.Content))
                        {
                            GetBalanceResponse = (GetBalanceResponse)xmlSerializer.Deserialize(stringReader);
                        }
                        if (GetBalanceResponse.ErrorCode == "NoError")
                        {
                            return true;
                        }
                        else
                        {
                            ErrorMessage = $"ErrorCode: {GetBalanceResponse.ErrorCode}";
                            return false;
                        }
                    }
                    catch (Exception xmlEx)
                    {
                        ErrorMessage = $"XML Deserialization Error: {xmlEx.Message}. Response Content: {response.Content}";
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = response.ErrorMessage ?? $"Request failed with status code: {response.StatusCode}. Response: {response.Content}";
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool Deposit(DepositRequest requestModel)
        {
            try
            {
                requestModel.AgentUserName = agentUserName;
                requestModel.AgentPassword = agentPassword;

                var response = ExecutePostRequest("/TransferToWHL", requestModel);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    try
                    {
                        var xmlSerializer = new XmlSerializer(typeof(DepositResponse));
                        using (var stringReader = new StringReader(response.Content))
                        {
                            DepositResponse = (DepositResponse)xmlSerializer.Deserialize(stringReader);
                        }
                        if (DepositResponse.ErrorCode == "NoError")
                        {
                            return true;
                        }
                        else
                        {
                            ErrorMessage = $"ErrorCode: {DepositResponse.ErrorCode}";
                            return false;
                        }
                    }
                    catch (Exception xmlEx)
                    {
                        ErrorMessage = $"XML Deserialization Error: {xmlEx.Message}. Response Content: {response.Content}";
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = response.ErrorMessage ?? $"Request failed with status code: {response.StatusCode}. Response: {response.Content}";
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool Withdraw(WithdrawRequest requestModel)
        {
            try
            {
                requestModel.AgentUserName = agentUserName;
                requestModel.AgentPassword = agentPassword;

                var response = ExecutePostRequest("/TransferFromWHL", requestModel);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    try
                    {
                        var xmlSerializer = new XmlSerializer(typeof(WithdrawResponse));
                        using (var stringReader = new StringReader(response.Content))
                        {
                            WithdrawResponse = (WithdrawResponse)xmlSerializer.Deserialize(stringReader);
                        }
                        if (WithdrawResponse.ErrorCode == "NoError")
                        {
                            return true;
                        }
                        else
                        {
                            ErrorMessage = $"ErrorCode: {WithdrawResponse.ErrorCode}";
                            return false;
                        }
                    }
                    catch (Exception xmlEx)
                    {
                        ErrorMessage = $"XML Deserialization Error: {xmlEx.Message}. Response Content: {response.Content}";
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = response.ErrorMessage ?? $"Request failed with status code: {response.StatusCode}. Response: {response.Content}";
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool GetCustomerAuthToken(GetCustomerAuthTokenRequest requestModel)
        {
            try
            {
                requestModel.AgentUserName = agentUserName;
                requestModel.AgentPassword = agentPassword;

                var response = ExecutePostRequest("/GetCustomerAuthToken", requestModel);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    try
                    {
                        var xmlSerializer = new XmlSerializer(typeof(GetCustomerAuthTokenResponse));
                        using (var stringReader = new StringReader(response.Content))
                        {
                            GetCustomerAuthTokenResponse = (GetCustomerAuthTokenResponse)xmlSerializer.Deserialize(stringReader);
                        }
                        if (GetCustomerAuthTokenResponse.ErrorCode == "NoError")
                        {
                            return true;
                        }
                        else
                        {
                            ErrorMessage = $"ErrorCode: {GetCustomerAuthTokenResponse.ErrorCode}";
                            return false;
                        }
                    }
                    catch (Exception xmlEx)
                    {
                        ErrorMessage = $"XML Deserialization Error: {xmlEx.Message}. Response Content: {response.Content}";
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = response.ErrorMessage ?? $"Request failed with status code: {response.StatusCode}. Response: {response.Content}";
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
        }
    }
}
