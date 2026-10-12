using Newtonsoft.Json;
using RestSharp;
using System;
using System.Linq;
using GameRecord.Gamess.BeSoft.Models;

namespace GameRecord.Gamess.BeSoft
{
    public class BeSoft : IDisposable
    {
        private string apiUrl;
        private string cert;
        private string agentId;
        private int timeOut;
        private string successCode = "1";

        public string ErrorMessage { get; private set; }
        public GetKeyResponse GetKeyResponse { get; private set; }
        public GetBalanceResponse GetBalanceResponse { get; private set; }
        public WithdrawResponse WithdrawResponse { get; private set; }
        public KickOutPlayerResponse KickOutPlayerResponse { get; private set; }
        public GetGameListResponse GetGameListResponse { get; private set; }

        public BeSoft(string apiUrl, string cert, string agentId, int timeOut)
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

            // Convert model properties to lower camel case for the first character
            var parameters = string.Join("&", requestModel.GetType().GetProperties().Select(p => $"{char.ToLowerInvariant(p.Name[0])}{p.Name.Substring(1)}={Uri.EscapeDataString(p.GetValue(requestModel)?.ToString() ?? string.Empty)}"));
            request.AddParameter("application/x-www-form-urlencoded", parameters, ParameterType.RequestBody);
            request.Timeout = timeOut * 1000;
            return client.Execute(request);
        }

        public bool GetKey(GetKeyRequest getKeyRequest)
        {
            try
            {
                getKeyRequest.Cert = cert;
                getKeyRequest.Extension1 = agentId;
                var response = ExecutePostRequest("api/game/getKey", getKeyRequest);
                GetKeyResponse = JsonConvert.DeserializeObject<GetKeyResponse>(response.Content);
                if (GetKeyResponse?.Status == 1)
                {
                    return true;
                }
                else
                {
                    ErrorMessage = "GetKey failed";
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
                getBalanceRequest.Extension1 = agentId;
                var response = ExecutePostRequest("api/game/getbalance", getBalanceRequest);
                GetBalanceResponse = JsonConvert.DeserializeObject<GetBalanceResponse>(response.Content);
                if (GetBalanceResponse?.Status == 1)
                {
                    return true;
                }
                else
                {
                    ErrorMessage = "GetBalance failed";
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
                withdrawRequest.Extension1 = agentId;
                var response = ExecutePostRequest("api/game/withdraw", withdrawRequest);
                WithdrawResponse = JsonConvert.DeserializeObject<WithdrawResponse>(response.Content);
                if (WithdrawResponse?.Status == 1)
                {
                    return true;
                }
                else
                {
                    ErrorMessage = "Withdraw failed";
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool GetGameList(GetGameListRequest getGameListRequest)
        {
            try
            {
                var response = ExecutePostRequest("api/game/subgamelist", getGameListRequest);
                GetGameListResponse = JsonConvert.DeserializeObject<GetGameListResponse>(response.Content);
                if (GetGameListResponse != null)
                {
                    return true;
                }
                else
                {
                    ErrorMessage = "Get game list failed";
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }

        public bool KickOutPlayer(KickOutPlayerRequest kickOutPlayerRequest)
        {
            try
            {
                kickOutPlayerRequest.Extension1 = agentId;
                kickOutPlayerRequest.Cert = cert;
                var response = ExecutePostRequest("api/game/kickOutPlayer", kickOutPlayerRequest);
                KickOutPlayerResponse = JsonConvert.DeserializeObject<KickOutPlayerResponse>(response.Content);
                if (KickOutPlayerResponse?.Status == 1)
                {
                    return true;
                }
                else
                {
                    ErrorMessage = KickOutPlayerResponse?.Erro?.ToString() ?? "KickOutPlayer failed";
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