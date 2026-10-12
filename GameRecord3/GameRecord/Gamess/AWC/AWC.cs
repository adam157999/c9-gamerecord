using Newtonsoft.Json;
using RestSharp;
using System;
using AWC.Models;
using System.Linq;
using AESexy;

namespace GameRecord.Gamess.AWC
{
    public class AWC : IDisposable
    {
        private string apiUrl;
        private string cert;
        private string agentId;
        private int timeOut;
        private string successCode = "0000";

        public string ErrorMessage { get; private set; }
        public LoginResponse LoginResponse { get; private set; }
        public CreateMemberResponse CreateMemberResponse { get; private set; }
        public LogoutResponse LogoutResponse { get; private set; }
        public GetBalanceResponse GetBalanceResponse { get; private set; }
        public WithdrawResponse WithdrawResponse { get; private set; }
        public DepositeResponse DepositeResponse { get; private set; }
        public CheckTransferOperationResponse CheckTransferOperationResponse { get; private set; }

        public AWC(string apiUrl, string cert, string agentId, int timeOut)
        {
            this.apiUrl = apiUrl;
            this.cert = cert;
            this.agentId = agentId;
            this.timeOut = timeOut;
        }

        private IRestResponse ExecutePostRequest(string endpoint, object requestModel)
        {
            var client = new RestClient(apiUrl + endpoint);
            var request = new RestRequest(Method.POST);
            request.AddHeader("content-type", "application/x-www-form-urlencoded");

            // Convert requestModel to query string format
            var parameters = string.Join("&", requestModel.GetType().GetProperties().Select(p => $"{char.ToLowerInvariant(p.Name[0])}" +
            $"{p.Name.Substring(1)}={Uri.EscapeDataString(p.GetValue(requestModel)?.ToString() ?? string.Empty)}"));

            request.AddParameter("application/x-www-form-urlencoded", parameters, ParameterType.RequestBody);
            request.Timeout = timeOut * 1000;
            return client.Execute(request);
        }

        public bool Login(LoginRequest loginRequest)
        {
            try
            {
                loginRequest.Cert = cert;
                loginRequest.AgentId = agentId;
                var response = ExecutePostRequest("/wallet/login", loginRequest);
                LoginResponse = JsonConvert.DeserializeObject<LoginResponse>(response.Content);
                if (LoginResponse?.Status == successCode)
                {
                    return true;
                }
                else
                {
                    ErrorMessage = LoginResponse?.Desc;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool CreateMember(CreateMemberRequest createMemberRequest)
        {
            try
            {
                createMemberRequest.Cert = cert;
                createMemberRequest.AgentId = agentId;
                var response = ExecutePostRequest("wallet/createMember", createMemberRequest);
                CreateMemberResponse = JsonConvert.DeserializeObject<CreateMemberResponse>(response.Content);
                if (CreateMemberResponse?.Status == successCode)
                {
                    return true;
                }
                else
                {
                    ErrorMessage = CreateMemberResponse?.Desc;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool DoLoginAndLaunchGame(LoginGameRequest request)
        {
            try
            {
                request.Cert = cert;
                request.AgentId = agentId;
                var response = ExecutePostRequest("wallet/login", request);
                LoginResponse = JsonConvert.DeserializeObject<LoginResponse>(response.Content);
                if (LoginResponse?.Status == successCode)
                {
                    return true;
                }
                else
                {
                    ErrorMessage = LoginResponse?.Desc;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool Logout(LogoutRequest logoutRequest)
        {
            try
            {
                logoutRequest.Cert = cert;
                logoutRequest.AgentId = agentId;
                var response = ExecutePostRequest("wallet/logout", logoutRequest);
                LogoutResponse = JsonConvert.DeserializeObject<LogoutResponse>(response.Content);
                if (LogoutResponse?.Status == successCode)
                {
                    return true;
                }
                else
                {
                    ErrorMessage = LogoutResponse.Status;
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
                getBalanceRequest.Cert = cert;
                getBalanceRequest.AgentId = agentId;
                var response = ExecutePostRequest("wallet/getBalance", getBalanceRequest);
                GetBalanceResponse = JsonConvert.DeserializeObject<GetBalanceResponse>(response.Content);
                if (GetBalanceResponse?.Status == successCode)
                {
                    return true;
                }
                else
                {
                    ErrorMessage = GetBalanceResponse.Status;
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
                withdrawRequest.Cert = cert;
                withdrawRequest.AgentId = agentId;
                var response = ExecutePostRequest("wallet/withdraw", withdrawRequest);
                WithdrawResponse = JsonConvert.DeserializeObject<WithdrawResponse>(response.Content);
                if (WithdrawResponse?.Status == successCode)
                {
                    return true;
                }
                else
                {
                    ErrorMessage = WithdrawResponse.TxCode;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool Deposite(DepositeRequest depositeRequest)
        {
            try
            {
                depositeRequest.Cert = cert;
                depositeRequest.AgentId = agentId;
                var response = ExecutePostRequest("wallet/deposit", depositeRequest);
                DepositeResponse = JsonConvert.DeserializeObject<DepositeResponse>(response.Content);
                if (DepositeResponse?.Status == successCode)
                {
                    return true;
                }
                else
                {
                    ErrorMessage = DepositeResponse.TxCode;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool CheckTransferOperation(CheckTransferOperationRequest request)
        {
            try
            {
                request.Cert = cert;
                request.AgentId = agentId;
                var response = ExecutePostRequest("/wallet/checkTransferOperation", request);
                CheckTransferOperationResponse = JsonConvert.DeserializeObject<CheckTransferOperationResponse>(response.Content);
                if (CheckTransferOperationResponse?.Status == successCode)
                {
                    return true;
                }
                else
                {
                    ErrorMessage = "Check Transfer Operation Failed";
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