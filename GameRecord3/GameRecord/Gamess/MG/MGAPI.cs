using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GameRecord.Gamess.MicroGaming
{
    public class MGAPI : IDisposable
    {
        private string API_URL { get; set; }
        private string Token_URL { get; set; }

        private string Agent_Code_ID { get; set; }

        private string API_Secret_Key { get; set; }

        private string APITOKEN { get; set; }

        public string finalUrl { get; set; }
        public string Request { get; set; }
        public string Parameters { get; set; }
        public string Response { get; set; }
        public string ErrorMessage { get; set; }


        public TokenResponse TokenRes { get; set; }
        public List<GetGamesResponse> GetGamesRes { get; set; }
        public GetGameDetailsResponse GetGameDetailsRes { get; set; }
        public GetImageTemplateResponse GetImageTemplateRes { get; set; }
        public GetBetsResponse GetBetsRes { get; set; }

        public GetIncompleteBetsResponse GetIncompleteBetsRes { get; set; }

        public GetPlayerIncompleteBetsResponse GetPlayerIncompleteBetsRes { get; set; }

        public GetLiveDealerBetsResponse GetLiveDealerBetsRes { get; set; }
        public GetBettingProfilesResponse GetBettingProfilesRes { get; set; }
        public GetPlayerBettingProfilesResponse GetPlayerBettingProfilesRes { get; set; }
        public ErrorResponse ErrorRes { get; set; }
        public PostBettingProfileResponse PostBettingProfileRes { get; set; }

        public DeleteBettingProfileResponse DeleteBettingProfileRes { get; set; }
        public GetGameCategoriesResponse GetGameCategoriesRes { get; set; }

        public GetPlayerGameCategoriesResponse GetPlayerGameCategoriesRes { get; set; }
        public PostPlayerGameCategoryResponse PostPlayerGameCategoryRes { get; set; }

        public DeletePlayerGameCategoryResponse DeletePlayerGameCategoryRes { get; set; }

        public GetPlayerResponse GetPlayerRes { get; set; }

        public PatchPlayerResponse PatchPlayerRes { get; set; }

        public PostPlayerResponse PostPlayerRes { get; set; }

        public PlayerProductInfoResponse PlayerProductInfoRes { get; set; }


        public PlayerAllProductsResponse PlayerAllProductsRes { get; set; }

        public PostPlayerProductBetVisualizersAsyncResponse PostPlayerProductBetVisualizersAsyncRes { get; set; }

        public PostPlayerBetVisualizersAsyncResponse PostPlayerBetVisualizersAsyncRes { get; set; }

        public PostPlayerSessionResponse PostPlayerSessionRes { get; set; }

        public WalletTransactionsResponse WalletTransactionsRes { get; set; }

        public PostWalletTransactionsAsyncResponse PostWalletTransactionsAsyncRes { get; set; }


        public GetWalletTransactionsAsyncResponse GetWalletTransactionsAsyncRes { get; set; }


        public List<PostReportLedgerAsyncResponse> PostReportLedgerAsyncRes { get; set; }


        public List<PostReportFinancialAsyncResponse> PostReportFinancialAsyncRes { get; set; }

        public List<PostReportgameAnalysisAsyncResponse> PostReportgameAnalysisAsyncRes { get; set; }


        public List<PostReportplayerGameAnalysisAsyncResponse> PostReportplayerGameAnalysisAsyncRes { get; set; }

        public List<PostReportprogressiveGamePlayAsyncResponse> PostReportprogressiveGamePlayAsyncRes { get; set; }

        public List<PostReportprogressiveWinsAsyncResponse> PostReportprogressiveWinsAsyncRes { get; set; }

        public List<PostReportfortuneRewardsAsyncResponse> PostReportfortuneRewardsAsyncRes { get; set; }

        public List<PostReportliveDealerPromotionsAsyncResponse> PostReportliveDealerPromotionsAsyncRes { get; set; }

        public List<PostReporttournamentWinsAsyncResponse> PostReporttournamentWinsAsyncRes { get; set; }

        public PostdemoSessionsAsyncResponse PostdemoSessionsAsyncRes { get; set; }


        public PostoffersAsyncResponse PostoffersAsyncRes { get; set; }

        public GetoffersAsyncResponse GetoffersAsyncRes { get; set; }

        public MGAPI(string api_url, string token_url, string agent_code_id, string api_secret_key)
        {
            this.API_URL = api_url;
            this.Token_URL = token_url;
            this.Agent_Code_ID = agent_code_id;
            this.API_Secret_Key = api_secret_key;
        }

        private async Task<HttpResponseMessage> GetTokenAsync(string url, Dictionary<string, dynamic> bodyContent = null)
        {
            try
            {
                finalUrl = url;


                Dictionary<string, dynamic> tempBodyContent;
                if (bodyContent != null)
                {
                    tempBodyContent = bodyContent;
                }
                else
                {
                    tempBodyContent = new Dictionary<string, dynamic>();
                }


                Dictionary<string, string> stringBodyContent = new Dictionary<string, string>();
                foreach (KeyValuePair<string, dynamic> kvp in tempBodyContent)
                {
                    string value;
                    if (kvp.Value != null)
                    {
                        value = kvp.Value.ToString();
                    }
                    else
                    {
                        value = string.Empty;
                    }
                    stringBodyContent.Add(kvp.Key, value);
                }

                var formContent = new FormUrlEncodedContent(stringBodyContent);
                Parameters = await formContent.ReadAsStringAsync(); // Convert bodyContent to a string for logging/debugging

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(url),
                    Content = formContent
                };
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var handler = new HttpClientHandler();
                using (HttpClient client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) })
                {
                    return await client.SendAsync(request);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to get token: " + ex.Message);
            }
        }

        private async Task<HttpResponseMessage> GetApiAsync(string url, Dictionary<string, dynamic> bodyContent = null)
        {
            try
            {
                finalUrl = url;


                // Convert Dictionary<string, dynamic> to URL query parameters
                if (bodyContent != null && bodyContent.Count > 0)
                {
                    List<string> queryList = new List<string>();
                    foreach (var kvp in bodyContent)
                    {
                        queryList.Add($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value.ToString())}");
                    }
                    string queryString = string.Join("&", queryList);
                    url += "?" + queryString;
                }

                Parameters = url; // Store for debugging/logging

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(url)
                };
                request.Headers.Add("Authorization", "Bearer " + this.APITOKEN);

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var handler = new HttpClientHandler();
                using (HttpClient client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) })
                {
                    return await client.SendAsync(request);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                throw;
            }
        }


        private async Task<HttpResponseMessage> PostApiAsync(string url, Dictionary<string, dynamic> bodyContent = null)
        {
            try
            {
                finalUrl = url;


                Dictionary<string, dynamic> tempBodyContent;
                if (bodyContent != null)
                {
                    tempBodyContent = bodyContent;
                }
                else
                {
                    tempBodyContent = new Dictionary<string, dynamic>();
                }


                Dictionary<string, string> stringBodyContent = new Dictionary<string, string>();
                foreach (KeyValuePair<string, dynamic> kvp in tempBodyContent)
                {
                    string value;
                    if (kvp.Value != null)
                    {
                        value = kvp.Value.ToString();
                    }
                    else
                    {
                        value = string.Empty;
                    }
                    stringBodyContent.Add(kvp.Key, value);
                }



                var formContent = new FormUrlEncodedContent(stringBodyContent);
                Parameters = await formContent.ReadAsStringAsync(); // Convert bodyContent to a string for logging/debugging


                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(url),
                    Content = formContent
                };

                request.Headers.Add("Authorization", "Bearer " + this.APITOKEN);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var handler = new HttpClientHandler();
                using (HttpClient client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) })
                {
                    return await client.SendAsync(request);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                throw;
            }
        }

        private async Task<HttpResponseMessage> DeleteApiAsync(string url, Dictionary<string, dynamic> bodyContent = null)
        {
            try
            {
                finalUrl = url;


                Dictionary<string, dynamic> tempBodyContent;
                if (bodyContent != null)
                {
                    tempBodyContent = bodyContent;
                }
                else
                {
                    tempBodyContent = new Dictionary<string, dynamic>();
                }


                Dictionary<string, string> stringBodyContent = new Dictionary<string, string>();
                foreach (KeyValuePair<string, dynamic> kvp in tempBodyContent)
                {
                    string value;
                    if (kvp.Value != null)
                    {
                        value = kvp.Value.ToString();
                    }
                    else
                    {
                        value = string.Empty;
                    }
                    stringBodyContent.Add(kvp.Key, value);
                }



                var formContent = new FormUrlEncodedContent(stringBodyContent);
                Parameters = await formContent.ReadAsStringAsync(); // Convert bodyContent to a string for logging/debugging


                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Delete,
                    RequestUri = new Uri(url),
                    Content = formContent
                };

                request.Headers.Add("Authorization", "Bearer " + this.APITOKEN);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var handler = new HttpClientHandler();
                using (HttpClient client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) })
                {
                    return await client.SendAsync(request);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                throw;
            }
        }

        private async Task<HttpResponseMessage> PatchApiAsync(string url, Dictionary<string, dynamic> bodyContent = null)
        {
            try
            {
                finalUrl = url;


                Dictionary<string, dynamic> tempBodyContent;
                if (bodyContent != null)
                {
                    tempBodyContent = bodyContent;
                }
                else
                {
                    tempBodyContent = new Dictionary<string, dynamic>();
                }


                Dictionary<string, string> stringBodyContent = new Dictionary<string, string>();
                foreach (KeyValuePair<string, dynamic> kvp in tempBodyContent)
                {
                    string value;
                    if (kvp.Value != null)
                    {
                        value = kvp.Value.ToString();
                    }
                    else
                    {
                        value = string.Empty;
                    }
                    stringBodyContent.Add(kvp.Key, value);
                }



                var formContent = new FormUrlEncodedContent(stringBodyContent);
                Parameters = await formContent.ReadAsStringAsync(); // Convert bodyContent to a string for logging/debugging


                var request = new HttpRequestMessage
                {
                    Method = new HttpMethod("PATCH"),
                    RequestUri = new Uri(url),
                    Content = formContent
                };

                request.Headers.Add("Authorization", "Bearer " + this.APITOKEN);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var handler = new HttpClientHandler();
                using (HttpClient client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) })
                {
                    return await client.SendAsync(request);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                throw;
            }
        }

        private async Task<HttpResponseMessage> PatchApiInJsonAsync(string url, Dictionary<string, dynamic> bodyContent = null)
        {
            try
            {
                finalUrl = url;
                var jsonContent = JsonConvert.SerializeObject(bodyContent, Formatting.None);
                Parameters = jsonContent;

                var request = new HttpRequestMessage
                {
                    Method = new HttpMethod("PATCH"),
                    RequestUri = new Uri(url),
                    Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
                };
                request.Headers.Add("Authorization", "Bearer " + this.APITOKEN);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var handler = new HttpClientHandler();
                using (HttpClient client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) })
                {
                    return await client.SendAsync(request);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                throw;
            }
        }

        private async Task<HttpResponseMessage> PostApiInJsonAsync(string url, Dictionary<string, dynamic> bodyContent = null)
        {
            try
            {
                finalUrl = url;
                var jsonContent = JsonConvert.SerializeObject(bodyContent, Formatting.None);
                Parameters = jsonContent;

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(url),
                    Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
                };
                request.Headers.Add("Authorization", "Bearer " + this.APITOKEN);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var handler = new HttpClientHandler();
                using (HttpClient client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) })
                {
                    return await client.SendAsync(request);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                throw;
            }
        }


        private async Task<string> ReadResponseMessageAsync(HttpResponseMessage httpResponseMessage)
        {
            return await httpResponseMessage.Content.ReadAsStringAsync();
        }




        public async Task<bool> GetAPITokenAsync(string grant_type)
        {
            try
            {

                Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                geek.Add("client_id", this.Agent_Code_ID);
                geek.Add("client_secret", this.API_Secret_Key);
                geek.Add("grant_type", grant_type);



                HttpResponseMessage response = await GetTokenAsync(this.Token_URL + "/connect/token", geek);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await ReadResponseMessageAsync(response);
                    Response = responseContent;
                    TokenRes = JsonConvert.DeserializeObject<TokenResponse>(responseContent);
                    this.APITOKEN = TokenRes.AccessToken;
                    return true;
                }
                else
                {
                    ErrorMessage = response.ReasonPhrase;
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return false;
            }
        }


        //HeartBeat 实时系统监测
        public async Task<bool> HeartbeatAsync()
        {
            try
            {
                if (await GetAPITokenAsync("client_credentials"))
                {

                    Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();

                    HttpResponseMessage response = await GetApiAsync(this.API_URL + "/heartbeat", geek);
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        Response = responseContent;
                        return true;
                    }
                    else
                    {
                        ErrorMessage = response.ReasonPhrase;
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

        //Games 遊戲
        public async Task<bool> GetGamesAsync(string fromReleaseDateUtc = null, string toReleaseDateUtc = null, string channelCode = null)
        {
            try
            {
                if (await GetAPITokenAsync("client_credentials"))
                {

                    Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                    if (!string.IsNullOrEmpty(fromReleaseDateUtc)) geek["fromReleaseDateUtc"] = fromReleaseDateUtc;
                    if (!string.IsNullOrEmpty(toReleaseDateUtc)) geek["toReleaseDateUtc"] = toReleaseDateUtc;
                    if (!string.IsNullOrEmpty(channelCode)) geek["channelCode"] = channelCode;


                    string endpoint = $"{this.API_URL}/agents/{this.Agent_Code_ID}/games";
                    HttpResponseMessage response = await GetApiAsync(endpoint, geek);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        Response = responseContent;
                        GetGamesRes = JsonConvert.DeserializeObject<List<GetGamesResponse>>(responseContent);
                        return true;
                    }
                    else
                    {
                        ErrorMessage = response.ReasonPhrase;
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

        public async Task<bool> GetGameDetailsAsync(string gameCode)
        {
            try
            {
                if (await GetAPITokenAsync("client_credentials"))
                {
                    Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();

                    string endpoint = $"{this.API_URL}/agents/{this.Agent_Code_ID}/games/{gameCode}";
                    HttpResponseMessage response = await GetApiAsync(endpoint, geek);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        Response = responseContent;
                        return true;
                    }
                    else
                    {
                        ErrorMessage = response.ReasonPhrase;
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

        //Game Assets 遊戲素材
        public async Task<bool> GetImageTemplateAsync()
        {
            try
            {
                if (await GetAPITokenAsync("client_credentials"))
                {
                    Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();

                    string endpoint = $"{this.API_URL}/agents/{this.Agent_Code_ID}/assets/imageTemplate";
                    HttpResponseMessage response = await GetApiAsync(endpoint, geek);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        Response = responseContent;
                        return true;
                    }
                    else
                    {
                        ErrorMessage = response.ReasonPhrase;
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


        //Bets 投注
        public async Task<bool> GetBetsAsync(string agentCode, int limit, string startingAfter = null, bool? Channel = null)
        {
            try
            {
                if (await GetAPITokenAsync("client_credentials"))
                {

                    Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>
                        {
                            { "limit", limit }
                        };

                    if (!string.IsNullOrEmpty(startingAfter)) geek["startingAfter"] = startingAfter;
                    if (Channel.HasValue)
                        geek["channel"] = Channel.Value ? "true" : "false";


                    string endpoint = $"{this.API_URL}/agents/{this.Agent_Code_ID}/bets";
                    HttpResponseMessage response = await GetApiAsync(endpoint, geek);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        Response = responseContent;
                        GetBetsRes = JsonConvert.DeserializeObject<GetBetsResponse>(responseContent);
                        return true;
                    }
                    else
                    {
                        ErrorMessage = response.ReasonPhrase;
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


        public async Task<bool> GetIncompleteBetsAsync(string playerId, string productId)
        {
            try
            {
                if (await GetAPITokenAsync("client_credentials"))
                {
                    Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();

                    string endpoint = $"{this.API_URL}/agents/{this.Agent_Code_ID}/players/{playerId}/products/{productId}/incompleteBets";
                    HttpResponseMessage response = await GetApiAsync(endpoint, geek);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        Response = responseContent;
                        GetIncompleteBetsRes = JsonConvert.DeserializeObject<GetIncompleteBetsResponse>(responseContent);
                        return true;
                    }
                    else
                    {
                        ErrorMessage = response.ReasonPhrase;
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


        public async Task<bool> GetPlayerIncompleteBetsAsync(string playerId)
        {
            try
            {
                if (await GetAPITokenAsync("client_credentials"))
                {
                    Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();

                    string endpoint = $"{this.API_URL}/agents/{this.Agent_Code_ID}/players/{playerId}/incompleteBets";
                    HttpResponseMessage response = await GetApiAsync(endpoint, geek);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        Response = responseContent;
                        GetPlayerIncompleteBetsRes = JsonConvert.DeserializeObject<GetPlayerIncompleteBetsResponse>(responseContent);
                        return true;
                    }
                    else
                    {
                        ErrorMessage = response.ReasonPhrase;
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


        public async Task<bool> GetLiveDealerBetsAsync(int? limit = null, string startingAfter = null)
        {
            try
            {
                if (await GetAPITokenAsync("client_credentials"))
                {
                    Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();

                    if (limit.HasValue)
                        geek["limit"] = limit;

                    if (!string.IsNullOrEmpty(startingAfter)) geek["startingAfter"] = startingAfter;


                    string endpoint = $"{this.API_URL}/agents/{this.Agent_Code_ID}/liveDealerBets";
                    HttpResponseMessage response = await GetApiAsync(endpoint, geek);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        Response = responseContent;
                        GetLiveDealerBetsRes = JsonConvert.DeserializeObject<GetLiveDealerBetsResponse>(responseContent);
                        return true;
                    }
                    else
                    {
                        ErrorMessage = response.ReasonPhrase;
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

        //Betting Profiles 投注配置
        public async Task<bool> GetBettingProfilesAsync(string productId)
        {
            try
            {
                if (await GetAPITokenAsync("client_credentials"))
                {
                    Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();

                    string endpoint = $"{this.API_URL}/agents/{this.Agent_Code_ID}/products/{productId}/bettingProfiles";
                    HttpResponseMessage response = await GetApiAsync(endpoint, geek);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        Response = responseContent;
                        GetBettingProfilesRes = JsonConvert.DeserializeObject<GetBettingProfilesResponse>(responseContent);
                        return true;
                    }
                    else
                    {
                        ErrorMessage = response.ReasonPhrase;
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


        public async Task<bool> GetPlayerBettingProfilesAsync(string productId, string playerId)
        {
            try
            {
                if (await GetAPITokenAsync("client_credentials"))
                {
                    Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();

                    string endpoint = $"{this.API_URL}/agents/{this.Agent_Code_ID}/products/{productId}/players/{playerId}/bettingProfiles";
                    HttpResponseMessage response = await GetApiAsync(endpoint, geek);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        Response = responseContent;
                        GetPlayerBettingProfilesRes = JsonConvert.DeserializeObject<GetPlayerBettingProfilesResponse>(responseContent);
                        return true;
                    }
                    else
                    {
                        ErrorMessage = response.ReasonPhrase;
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

        public async Task<bool> PostPlayerBettingProfilesAsync(string productId, string playerId, int bettingProfileId)
        {
            try
            {
                if (await GetAPITokenAsync("client_credentials"))
                {
                    Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                    geek.Add("bettingProfileId", bettingProfileId);

                    string endpoint = $"{this.API_URL}/agents/{this.Agent_Code_ID}/products/{productId}/players/{playerId}/bettingProfiles";
                    HttpResponseMessage response = await PostApiAsync(endpoint, geek);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        Response = responseContent;
                        PostBettingProfileRes = JsonConvert.DeserializeObject<PostBettingProfileResponse>(responseContent);
                        return true;
                    }
                    else
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.error.message, ErrorRes.error.messagezhCHS);
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

        public async Task<bool> DeletePlayerBettingProfilesAsync(string productId, string playerId, int bettingProfileId)
        {
            try
            {
                if (await GetAPITokenAsync("client_credentials"))
                {
                    Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();


                    string endpoint = $"{this.API_URL}/agents/{this.Agent_Code_ID}/products/{productId}/players/{playerId}/bettingProfiles/{bettingProfileId}";
                    HttpResponseMessage response = await DeleteApiAsync(endpoint, geek);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        Response = responseContent;
                        DeleteBettingProfileRes = JsonConvert.DeserializeObject<DeleteBettingProfileResponse>(responseContent);
                        return true;
                    }
                    else
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.error.message, ErrorRes.error.messagezhCHS);
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

        //Game Categories 游戏类别
        public async Task<bool> GetGameCategoriesAsync()
        {
            try
            {
                if (await GetAPITokenAsync("client_credentials"))
                {
                    string endpoint = $"{this.API_URL}/agents/{this.Agent_Code_ID}/gameCategories";
                    HttpResponseMessage response = await GetApiAsync(endpoint);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        Response = responseContent;
                        if (responseContent.Trim() == "[]")
                        {
                            GetGameCategoriesRes = new GetGameCategoriesResponse { gameCategories = new List<GameCategory>() };
                        }
                        else
                        {
                            GetGameCategoriesRes = JsonConvert.DeserializeObject<GetGameCategoriesResponse>(responseContent);
                        }

                        return true;
                    }
                    else
                    {
                        ErrorMessage = response.ReasonPhrase;
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


        public async Task<bool> GetPlayerGameCategoriesAsync(string playerId)
        {
            try
            {
                if (await GetAPITokenAsync("client_credentials"))
                {
                    string endpoint = $"{this.API_URL}/agents/{this.Agent_Code_ID}/players/{playerId}/gameCategories";
                    Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                    HttpResponseMessage response = await GetApiAsync(endpoint, geek);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        Response = responseContent;

                        // Handle empty array response
                        if (responseContent.Trim() == "[]")
                        {
                            GetPlayerGameCategoriesRes = new GetPlayerGameCategoriesResponse { gameCategories = new List<GetPlayerGameCategoriesGameCategory>() };
                        }
                        else
                        {
                            GetPlayerGameCategoriesRes = JsonConvert.DeserializeObject<GetPlayerGameCategoriesResponse>(responseContent);
                        }

                        return true;
                    }
                    else
                    {
                        ErrorMessage = response.ReasonPhrase;
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


        public async Task<bool> PostPlayerGameCategoryAsync(string playerId, string productId, string gameCategoryCode)
        {
            try
            {
                if (await GetAPITokenAsync("client_credentials"))
                {
                    Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                    geek.Add("gameCategoryCode", gameCategoryCode);


                    string endpoint = $"{this.API_URL}/agents/{this.Agent_Code_ID}/players/{playerId}/products/{productId}/gameCategories";
                    HttpResponseMessage response = await PostApiAsync(endpoint, geek);

                    if (response.StatusCode == HttpStatusCode.Created)
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        Response = responseContent;
                        PostPlayerGameCategoryRes = JsonConvert.DeserializeObject<PostPlayerGameCategoryResponse>(responseContent);
                        return true;
                    }
                    else
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.error.message, ErrorRes.error.messagezhCHS);
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

        public async Task<bool> DeletePlayerGameCategoryAsync(string playerId, string productId, string gameCategoryCode)
        {
            try
            {
                if (await GetAPITokenAsync("client_credentials"))
                {



                    string endpoint = $"{this.API_URL}/agents/{this.Agent_Code_ID}/players/{playerId}/products/{productId}/gameCategories/{gameCategoryCode}";
                    HttpResponseMessage response = await DeleteApiAsync(endpoint);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        Response = responseContent;
                        DeletePlayerGameCategoryRes = JsonConvert.DeserializeObject<DeletePlayerGameCategoryResponse>(responseContent);
                        return true;
                    }
                    else
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.error.message, ErrorRes.error.messagezhCHS);
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

        //Players 玩家

        public async Task<bool> GetPlayerAsync(string playerId, bool? properties = null)
        {
            try
            {
                if (await GetAPITokenAsync("client_credentials"))
                {

                    Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                    if (properties.HasValue)
                    {
                        geek.Add("properties", "balance");
                    }



                    string endpoint = $"{this.API_URL}/agents/{this.Agent_Code_ID}/players/{playerId}";
                    HttpResponseMessage response = await GetApiAsync(endpoint, geek);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        Response = responseContent;

                        // Handle empty response
                        if (string.IsNullOrWhiteSpace(responseContent) || responseContent.Trim() == "{}")
                        {
                            GetPlayerRes = new GetPlayerResponse();
                        }
                        else
                        {
                            GetPlayerRes = JsonConvert.DeserializeObject<GetPlayerResponse>(responseContent);
                        }

                        return true;
                    }
                    else
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.error.message, ErrorRes.error.messagezhCHS);
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

        public async Task<bool> PatchPlayerAsync(string playerId, bool isLock)
        {
            try
            {
                if (await GetAPITokenAsync("client_credentials"))
                {

                    Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                    if (isLock)
                    {
                        geek.Add("isLock", "true");
                    }
                    else
                    {
                        geek.Add("isLock", "false");
                    }

                    string endpoint = $"{this.API_URL}/agents/{this.Agent_Code_ID}/players/{playerId}";
                    HttpResponseMessage response = await PatchApiAsync(endpoint, geek);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        Response = responseContent;

                        // Handle empty response
                        if (string.IsNullOrWhiteSpace(responseContent) || responseContent.Trim() == "{}")
                        {
                            PatchPlayerRes = new PatchPlayerResponse();
                        }
                        else
                        {
                            PatchPlayerRes = JsonConvert.DeserializeObject<PatchPlayerResponse>(responseContent);
                        }

                        return true;
                    }
                    else
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.error.message, ErrorRes.error.messagezhCHS);
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


        public async Task<bool> PostPlayerAsync(string playerId)
        {
            try
            {
                if (await GetAPITokenAsync("client_credentials"))
                {
                    Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                    geek.Add("playerId", playerId);


                    string endpoint = $"{this.API_URL}/agents/{this.Agent_Code_ID}/players";
                    HttpResponseMessage response = await PostApiAsync(endpoint, geek);

                    if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        Response = responseContent;
                        PostPlayerRes = JsonConvert.DeserializeObject<PostPlayerResponse>(responseContent);
                        return true;
                    }
                    else
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.error.message, ErrorRes.error.messagezhCHS);
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


        public async Task<bool> GetPlayerProductAsync(string playerId, string productId, bool? balance = null)
        {
            try
            {
                if (await GetAPITokenAsync("client_credentials"))
                {
                    Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                    if (balance.HasValue)
                    {
                        geek.Add("properties", "balance");
                    }

                    string endpoint = $"{this.API_URL}/agents/{this.Agent_Code_ID}/players/{playerId}/products/{productId}";
                    HttpResponseMessage response = await GetApiAsync(endpoint, geek);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        Response = responseContent;

                        // Handle empty response
                        if (string.IsNullOrWhiteSpace(responseContent) || responseContent.Trim() == "{}")
                        {
                            PlayerProductInfoRes = new PlayerProductInfoResponse();
                        }
                        else
                        {
                            PlayerProductInfoRes = JsonConvert.DeserializeObject<PlayerProductInfoResponse>(responseContent);
                        }

                        return true;
                    }
                    else
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.error.message, ErrorRes.error.messagezhCHS);
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

        public async Task<bool> GetPlayerAllProductAsync(string playerId)
        {
            try
            {
                if (await GetAPITokenAsync("client_credentials"))
                {
                    string endpoint = $"{this.API_URL}/agents/{this.Agent_Code_ID}/players/{playerId}/products";
                    HttpResponseMessage response = await GetApiAsync(endpoint);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        Response = responseContent;

                        // Handle empty response
                        if (string.IsNullOrWhiteSpace(responseContent) || responseContent.Trim() == "{}")
                        {
                            PlayerAllProductsRes = new PlayerAllProductsResponse();
                        }
                        else
                        {
                            PlayerAllProductsRes = JsonConvert.DeserializeObject<PlayerAllProductsResponse>(responseContent);
                        }

                        return true;
                    }
                    else
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.error.message, ErrorRes.error.messagezhCHS);
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


        public async Task<bool> PostPlayerProductBetVisualizersAsync(string playerId, string productId, int utcOffset, string langCode, string betUid = null, string game = null, string fromDate = null, string toDate = null)
        {
            try
            {
                if (await GetAPITokenAsync("client_credentials"))
                {
                    Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                    geek.Add("utcOffset ", utcOffset);
                    geek.Add("langCode  ", langCode);

                    if (!string.IsNullOrEmpty(betUid)) geek["betUid"] = betUid;
                    if (!string.IsNullOrEmpty(game)) geek["game"] = game;
                    if (!string.IsNullOrEmpty(fromDate)) geek["fromDate"] = fromDate;
                    if (!string.IsNullOrEmpty(toDate)) geek["toDate"] = toDate;

                    string endpoint = $"{this.API_URL}/agents/{this.Agent_Code_ID}/players/{playerId}/products/{productId}/betVisualizers";
                    HttpResponseMessage response = await PostApiAsync(endpoint, geek);

                    if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        Response = responseContent;
                        PostPlayerProductBetVisualizersAsyncRes = JsonConvert.DeserializeObject<PostPlayerProductBetVisualizersAsyncResponse>(responseContent);
                        return true;
                    }
                    else
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.error.message, ErrorRes.error.messagezhCHS);
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

        public async Task<bool> PostPlayerBetVisualizersAsync(string playerId, int utcOffset, string langCode, string betUid = null, string game = null, string fromDate = null, string toDate = null)
        {
            try
            {
                if (await GetAPITokenAsync("client_credentials"))
                {
                    Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                    geek.Add("utcOffset ", utcOffset);
                    geek.Add("langCode  ", langCode);

                    if (!string.IsNullOrEmpty(betUid)) geek["betUid"] = betUid;
                    if (!string.IsNullOrEmpty(game)) geek["game"] = game;
                    if (!string.IsNullOrEmpty(fromDate)) geek["fromDate"] = fromDate;
                    if (!string.IsNullOrEmpty(toDate)) geek["toDate"] = toDate;

                    string endpoint = $"{this.API_URL}/agents/{this.Agent_Code_ID}/players/{playerId}/betVisualizers";
                    HttpResponseMessage response = await PostApiAsync(endpoint, geek);

                    if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        Response = responseContent;
                        PostPlayerBetVisualizersAsyncRes = JsonConvert.DeserializeObject<PostPlayerBetVisualizersAsyncResponse>(responseContent);
                        return true;
                    }
                    else
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.error.message, ErrorRes.error.messagezhCHS);
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

        //Sessions 游戏启动
        public async Task<bool> PostPlayerSessionAsync(string playerId, string contentCode, string platform, string langCode, string game = null, string contentType = null, string homeUrl = null, string bankUrl = null)
        {
            try
            {
                if (await GetAPITokenAsync("client_credentials"))
                {
                    Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                    geek.Add("contentCode", contentCode);
                    geek.Add("platform", platform);
                    geek.Add("langCode", langCode);

                    if (!string.IsNullOrEmpty(game)) geek["game"] = game;
                    if (!string.IsNullOrEmpty(contentType)) geek["contentType"] = contentType;
                    if (!string.IsNullOrEmpty(homeUrl)) geek["homeUrl"] = homeUrl;
                    if (!string.IsNullOrEmpty(bankUrl)) geek["bankUrl"] = bankUrl;

                    string endpoint = $"{this.API_URL}/agents/{this.Agent_Code_ID}/players/{playerId}/sessions";
                    HttpResponseMessage response = await PostApiAsync(endpoint, geek);

                    if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        Response = responseContent;
                        PostPlayerSessionRes = JsonConvert.DeserializeObject<PostPlayerSessionResponse>(responseContent);
                        return true;
                    }
                    else
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.error.message, ErrorRes.error.messagezhCHS);
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

        //Transactions 交易
        public async Task<bool> WalletTransactionsAsync(string idempotencyKey)
        {
            try
            {
                if (await GetAPITokenAsync("client_credentials"))
                {

                    Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                    geek.Add("idempotencyKey", "idempotencyKey");


                    string endpoint = $"{this.API_URL}/agents/{this.Agent_Code_ID}/WalletTransactions";
                    HttpResponseMessage response = await GetApiAsync(endpoint, geek);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        Response = responseContent;

                        // Handle empty response
                        if (string.IsNullOrWhiteSpace(responseContent) || responseContent.Trim() == "{}")
                        {
                            WalletTransactionsRes = new WalletTransactionsResponse();
                        }
                        else
                        {
                            WalletTransactionsRes = JsonConvert.DeserializeObject<WalletTransactionsResponse>(responseContent);
                        }

                        return true;
                    }
                    else
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.error.message, ErrorRes.error.messagezhCHS);
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

        public async Task<bool> PostWalletTransactionsAsync(string playerId, string type, decimal? amount = null, string idempotencyKey = null, string externalTransactionId = null)
        {
            try
            {
                if (await GetAPITokenAsync("client_credentials"))
                {

                    Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                    if (!string.IsNullOrEmpty(playerId)) geek["playerId"] = playerId;
                    if (!string.IsNullOrEmpty(type)) geek["type"] = type;
                    if (amount.HasValue) geek["amount"] = amount.Value;
                    if (!string.IsNullOrEmpty(idempotencyKey)) geek["idempotencyKey"] = idempotencyKey;
                    if (!string.IsNullOrEmpty(externalTransactionId)) geek["externalTransactionId"] = externalTransactionId;

                    string endpoint = $"{this.API_URL}/agents/{this.Agent_Code_ID}/WalletTransactions";
                    HttpResponseMessage response = await PostApiAsync(endpoint, geek);

                    if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        Response = responseContent;
                        PostWalletTransactionsAsyncRes = JsonConvert.DeserializeObject<PostWalletTransactionsAsyncResponse>(responseContent);
                        return true;
                    }
                    else
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.error.message, ErrorRes.error.messagezhCHS);
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

        public async Task<bool> GetWalletTransactionsAsync(string walletTransactionId)
        {
            try
            {
                if (await GetAPITokenAsync("client_credentials"))
                {


                    string endpoint = $"{this.API_URL}/agents/{this.Agent_Code_ID}/WalletTransactions/{walletTransactionId}";
                    HttpResponseMessage response = await GetApiAsync(endpoint);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        Response = responseContent;

                        // Handle empty response
                        if (string.IsNullOrWhiteSpace(responseContent) || responseContent.Trim() == "{}")
                        {
                            GetWalletTransactionsAsyncRes = new GetWalletTransactionsAsyncResponse();
                        }
                        else
                        {
                            GetWalletTransactionsAsyncRes = JsonConvert.DeserializeObject<GetWalletTransactionsAsyncResponse>(responseContent);
                        }

                        return true;
                    }
                    else
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.error.message, ErrorRes.error.messagezhCHS);
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


        //Reports 报表
        public async Task<bool> PostReportLedgerAsync(DateTime fromDate,
                                                        DateTime toDate,
                                                        int utcOffset,
                                                        string currency,
                                                        string playerId,
                                                        List<string> products,
                                                        string externalTransactionId,
                                                        List<string> transactionTypes,
                                                        List<string> transactionStatuses,
                                                        string endingBefore,
                                                        int limit)
        {
            try
            {
                if (await GetAPITokenAsync("client_credentials"))
                {
                    Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>
                            {
                                { "fromDate", fromDate },
                                { "toDate", toDate },
                                { "utcOffset", utcOffset },
                                { "currency", currency },
                                { "playerId", playerId },
                                { "products", products },
                                { "externalTransactionId", externalTransactionId },
                                { "transactionTypes", transactionTypes},
                                { "transactionStatuses", transactionStatuses },
                                { "endingBefore", endingBefore },
                                { "limit", limit }
                            };

                    string endpoint = $"{this.API_URL}/agents/{this.Agent_Code_ID}/reports/ledger";
                    HttpResponseMessage response = await PostApiInJsonAsync(endpoint, geek);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        Response = responseContent;
                        // Handle empty response
                        if (string.IsNullOrWhiteSpace(responseContent) || responseContent.Trim() == "[]")
                        {
                            PostReportLedgerAsyncRes = new List<PostReportLedgerAsyncResponse>();
                        }
                        else
                        {
                            PostReportLedgerAsyncRes = JsonConvert.DeserializeObject<List<PostReportLedgerAsyncResponse>>(responseContent);


                        }
                        return true;
                    }
                    else
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.error.message, ErrorRes.error.messagezhCHS);
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


        public async Task<bool> PostReportFinancialAsync(DateTime fromDate,
                                                        DateTime toDate,
                                                        string timeAggregation,
                                                        int utcOffset,
                                                        string currency,
                                                        string playerId = null,
                                                        List<string> products = null
                                                         )
        {
            try
            {
                if (await GetAPITokenAsync("client_credentials"))
                {
                    Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>
                            {
                                { "fromDate", fromDate },
                                { "toDate", toDate },
                                { "timeAggregation", timeAggregation },
                                { "utcOffset", utcOffset },
                                { "currency", currency }
                            };

                    string endpoint = $"{this.API_URL}/agents/{this.Agent_Code_ID}/reports/financial";
                    HttpResponseMessage response = await PostApiInJsonAsync(endpoint, geek);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        Response = responseContent;
                        // Handle empty response
                        if (string.IsNullOrWhiteSpace(responseContent) || responseContent.Trim() == "[]")
                        {
                            PostReportFinancialAsyncRes = new List<PostReportFinancialAsyncResponse>();
                        }
                        else
                        {
                            PostReportFinancialAsyncRes = JsonConvert.DeserializeObject<List<PostReportFinancialAsyncResponse>>(responseContent);
                        }
                        return true;
                    }
                    else
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.error.message, ErrorRes.error.messagezhCHS);
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


        public async Task<bool> PostReportgameAnalysisAsync(DateTime fromDate,
                                                      DateTime toDate,
                                                      string timeAggregation,
                                                      int utcOffset,
                                                      string currency,
                                                      string playerId,
                                                      List<string> products,
                                                      List<string> channels,
                                                      List<string> platforms,
                                                      List<string> games
                                                       )
        {
            try
            {
                if (await GetAPITokenAsync("client_credentials"))
                {
                    Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>
                            {
                                { "fromDate", fromDate },
                                { "toDate", toDate },
                                { "timeAggregation", timeAggregation },
                                { "utcOffset", utcOffset },
                                { "currency", currency },
                                { "playerId", playerId },
                                { "products", products },
                                { "channels", channels },
                                { "platforms", platforms },
                                { "games", games }
                            };

                    string endpoint = $"{this.API_URL}/agents/{this.Agent_Code_ID}/reports/gameAnalysis";
                    HttpResponseMessage response = await PostApiInJsonAsync(endpoint, geek);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        Response = responseContent;
                        // Handle empty response
                        if (string.IsNullOrWhiteSpace(responseContent) || responseContent.Trim() == "[]")
                        {
                            PostReportgameAnalysisAsyncRes = new List<PostReportgameAnalysisAsyncResponse>();
                        }
                        else
                        {
                            PostReportgameAnalysisAsyncRes = JsonConvert.DeserializeObject<List<PostReportgameAnalysisAsyncResponse>>(responseContent);
                        }
                        return true;
                    }
                    else
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.error.message, ErrorRes.error.messagezhCHS);
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

        public async Task<bool> PostReportplayerGameAnalysisAsync(DateTime fromDate,
                                                     DateTime toDate,
                                                     string timeAggregation,
                                                     int utcOffset,
                                                     string playerId = null,
                                                     List<string> products = null,
                                                     List<string> channels = null,
                                                     List<string> platforms = null,
                                                     List<string> games = null
                                                      )
        {
            try
            {
                if (await GetAPITokenAsync("client_credentials"))
                {
                    Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>
                            {
                                { "fromDate", fromDate },
                                { "toDate", toDate },
                                { "timeAggregation", timeAggregation },
                                { "utcOffset", utcOffset }

                            };

                    string endpoint = $"{this.API_URL}/agents/{this.Agent_Code_ID}/reports/playerGameAnalysis";
                    HttpResponseMessage response = await PostApiInJsonAsync(endpoint, geek);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        Response = responseContent;
                        // Handle empty response
                        if (string.IsNullOrWhiteSpace(responseContent) || responseContent.Trim() == "[]")
                        {
                            PostReportplayerGameAnalysisAsyncRes = new List<PostReportplayerGameAnalysisAsyncResponse>();
                        }
                        else
                        {
                            PostReportplayerGameAnalysisAsyncRes = JsonConvert.DeserializeObject<List<PostReportplayerGameAnalysisAsyncResponse>>(responseContent);

                        }
                        return true;
                    }
                    else
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.error.message, ErrorRes.error.messagezhCHS);
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

        public async Task<bool> PostReportprogressiveGamePlayAsync(DateTime fromDate,
                                                    DateTime toDate,
                                                    string timeAggregation,
                                                    int utcOffset,
                                                    string playerId,
                                                    string currency,
                                                    List<string> products,
                                                    List<string> channels,
                                                    List<string> platforms,
                                                    List<string> games
                                                     )
        {
            try
            {
                if (await GetAPITokenAsync("client_credentials"))
                {
                    Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>
                            {
                                { "fromDate", fromDate },
                                { "toDate", toDate },
                                { "timeAggregation", timeAggregation },
                                { "utcOffset", utcOffset },
                                { "currency", currency },
                                { "playerId", playerId },
                                { "products", products },
                                { "platforms", platforms },
                                { "games", games }
                            };

                    string endpoint = $"{this.API_URL}/agents/{this.Agent_Code_ID}/reports/progressiveGamePlay";
                    HttpResponseMessage response = await PostApiInJsonAsync(endpoint, geek);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        Response = responseContent;
                        // Handle empty response
                        if (string.IsNullOrWhiteSpace(responseContent) || responseContent.Trim() == "[]")
                        {
                            PostReportprogressiveGamePlayAsyncRes = new List<PostReportprogressiveGamePlayAsyncResponse>();
                        }
                        else
                        {
                            PostReportprogressiveGamePlayAsyncRes = JsonConvert.DeserializeObject<List<PostReportprogressiveGamePlayAsyncResponse>>(responseContent);
                        }
                        return true;
                    }
                    else
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.error.message, ErrorRes.error.messagezhCHS);
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


        public async Task<bool> PostReportprogressiveWinsAsync(DateTime fromDate,
                                                  DateTime toDate,
                                                  int utcOffset,
                                                  string currency,
                                                  string playerId,
                                                  List<string> products,
                                                  List<string> jackpotTypes,
                                                  string jackpotNumber
                                                   )
        {
            try
            {
                if (await GetAPITokenAsync("client_credentials"))
                {
                    Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>
                            {
                                { "fromDate", fromDate },
                                { "toDate", toDate },
                                { "utcOffset", utcOffset },
                                { "currency", currency },
                                { "playerId", playerId },
                                { "products", products },
                                { "jackpotTypes", jackpotTypes },
                                { "jackpotNumber", jackpotNumber }
                            };

                    string endpoint = $"{this.API_URL}/agents/{this.Agent_Code_ID}/reports/progressiveWins";
                    HttpResponseMessage response = await PostApiInJsonAsync(endpoint, geek);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        Response = responseContent;
                        // Handle empty response
                        if (string.IsNullOrWhiteSpace(responseContent) || responseContent.Trim() == "[]")
                        {
                            PostReportprogressiveWinsAsyncRes = new List<PostReportprogressiveWinsAsyncResponse>();
                        }
                        else
                        {
                            PostReportprogressiveWinsAsyncRes = JsonConvert.DeserializeObject<List<PostReportprogressiveWinsAsyncResponse>>(responseContent);
                        }
                        return true;
                    }
                    else
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.error.message, ErrorRes.error.messagezhCHS);
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


        public async Task<bool> PostReportfortuneRewardsAsync(DateTime fromDate,
                                                 DateTime toDate,
                                                 int utcOffset,
                                                 List<string> rewardTypes
                                                  )
        {
            try
            {
                if (await GetAPITokenAsync("client_credentials"))
                {
                    Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>
                            {
                                { "fromDate", fromDate },
                                { "toDate", toDate },
                                { "utcOffset", utcOffset },
                                { "rewardTypes", rewardTypes }
                            };

                    string endpoint = $"{this.API_URL}/agents/{this.Agent_Code_ID}/reports/fortuneRewards";
                    HttpResponseMessage response = await PostApiInJsonAsync(endpoint, geek);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        Response = responseContent;
                        // Handle empty response
                        if (string.IsNullOrWhiteSpace(responseContent) || responseContent.Trim() == "[]")
                        {
                            PostReportfortuneRewardsAsyncRes = new List<PostReportfortuneRewardsAsyncResponse>();
                        }
                        else
                        {
                            PostReportfortuneRewardsAsyncRes = JsonConvert.DeserializeObject<List<PostReportfortuneRewardsAsyncResponse>>(responseContent);
                        }
                        return true;
                    }
                    else
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.error.message, ErrorRes.error.messagezhCHS);
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



        public async Task<bool> PostReportliveDealerPromotionsAsync(DateTime fromDate,
                                                 DateTime toDate,
                                                 int utcOffset,
                                                 string currency,
                                                 string playerId,
                                                 List<string> promotionTypes
                                                  )
        {
            try
            {
                if (await GetAPITokenAsync("client_credentials"))
                {
                    Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>
                            {
                                { "fromDate", fromDate },
                                { "toDate", toDate },
                                { "utcOffset", utcOffset },
                                { "currency", currency },
                                { "playerId", playerId },
                                { "promotionTypes", promotionTypes }
                            };

                    string endpoint = $"{this.API_URL}/agents/{this.Agent_Code_ID}/reports/liveDealerPromotions";
                    HttpResponseMessage response = await PostApiInJsonAsync(endpoint, geek);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        Response = responseContent;
                        // Handle empty response
                        if (string.IsNullOrWhiteSpace(responseContent) || responseContent.Trim() == "[]")
                        {
                            PostReportliveDealerPromotionsAsyncRes = new List<PostReportliveDealerPromotionsAsyncResponse>();
                        }
                        else
                        {
                            PostReportliveDealerPromotionsAsyncRes = JsonConvert.DeserializeObject<List<PostReportliveDealerPromotionsAsyncResponse>>(responseContent);
                        }
                        return true;
                    }
                    else
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.error.message, ErrorRes.error.messagezhCHS);
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

        public async Task<bool> PostReporttournamentWinsAsync(DateTime fromDate,
                                                 DateTime toDate,
                                                 int utcOffset,
                                                 string playerId,
                                                 List<string> tournaments
                                                  )
        {
            try
            {
                if (await GetAPITokenAsync("client_credentials"))
                {
                    Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>
                            {
                                { "fromDate", fromDate },
                                { "toDate", toDate },
                                { "utcOffset", utcOffset },
                                { "playerId", playerId },
                                { "tournaments", tournaments }
                            };

                    string endpoint = $"{this.API_URL}/agents/{this.Agent_Code_ID}/reports/tournamentWins";
                    HttpResponseMessage response = await PostApiInJsonAsync(endpoint, geek);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        Response = responseContent;
                        // Handle empty response
                        if (string.IsNullOrWhiteSpace(responseContent) || responseContent.Trim() == "[]")
                        {
                            PostReporttournamentWinsAsyncRes = new List<PostReporttournamentWinsAsyncResponse>();
                        }
                        else
                        {
                            PostReporttournamentWinsAsyncRes = JsonConvert.DeserializeObject<List<PostReporttournamentWinsAsyncResponse>>(responseContent);
                        }
                        return true;
                    }
                    else
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.error.message, ErrorRes.error.messagezhCHS);
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

        //DemoSessions 演示模式会话

        public async Task<bool> PostdemoSessionsAsync(string contentCode, string platform, string langCode, string playerId = null)
        {
            try
            {
                if (await GetAPITokenAsync("client_credentials"))
                {

                    Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                    geek.Add("contentCode", contentCode);
                    geek.Add("platform", platform);
                    geek.Add("langCode", langCode);
                    if (!string.IsNullOrEmpty(playerId)) geek["playerId"] = playerId;

                    string endpoint = $"{this.API_URL}/agents/{this.Agent_Code_ID}/demoSessions";
                    HttpResponseMessage response = await PostApiAsync(endpoint, geek);

                    if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        Response = responseContent;
                        PostdemoSessionsAsyncRes = JsonConvert.DeserializeObject<PostdemoSessionsAsyncResponse>(responseContent);
                        return true;
                    }
                    else
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.error.message, ErrorRes.error.messagezhCHS);
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

        //Free Games - Offer Management 免费游戏 - 优惠管理
        public async Task<bool> PostoffersAsync(string offerName, List<Dictionary<string, dynamic>> offerGames, int durationAvailableInDaysAfterAwarded, int numberOfRounds, string offerAvailableFromDateUTC, string offerAvailableToDateUTC)
        {
            try
            {
                if (await GetAPITokenAsync("client_credentials"))
                {
                    Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>
                                                    {
                                                        { "offerName", offerName },
                                                        { "offerGames",  offerGames },
                                                        { "durationAvailableInDaysAfterAwarded", durationAvailableInDaysAfterAwarded },
                                                        { "numberOfRounds", numberOfRounds},
                                                        { "offerAvailableFromDateUTC", offerAvailableFromDateUTC },
                                                        { "offerAvailableToDateUTC", offerAvailableFromDateUTC }
                                                    };


                    string endpoint = $"{this.API_URL}/agents/{this.Agent_Code_ID}/offers";
                    HttpResponseMessage response = await PostApiInJsonAsync(endpoint, geek);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        Response = responseContent;
                        PostoffersAsyncRes = JsonConvert.DeserializeObject<PostoffersAsyncResponse>(responseContent);
                        return true;
                    }
                    else
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.error.message, ErrorRes.error.messagezhCHS);
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


        public async Task<bool> GetoffersAsync(string offerGuidStartsWith, string offerNameContains, string availableFromDateUTC, string availableToDateUTC, string offerStatus, int page, int perPage)
        {
            try
            {
                if (await GetAPITokenAsync("client_credentials"))
                {

                    Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();
                    geek.Add("offerGuidStartsWith", offerGuidStartsWith);
                    geek.Add("offerGuidStartsWith", offerGuidStartsWith);
                    geek.Add("offerNameContains", offerNameContains);
                    geek.Add("availableFromDateUTC", availableFromDateUTC);
                    geek.Add("availableToDateUTC", availableToDateUTC);
                    geek.Add("offerStatus", offerStatus);
                    geek.Add("page", page);
                    geek.Add("perPage", perPage);



                    string endpoint = $"{this.API_URL}/agents/{this.Agent_Code_ID}/offers";
                    HttpResponseMessage response = await GetApiAsync(endpoint, geek);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        Response = responseContent;


                        // Handle empty response
                        if (string.IsNullOrWhiteSpace(responseContent) || responseContent.Trim() == "{}")
                        {
                            GetoffersAsyncRes = new GetoffersAsyncResponse();
                        }
                        else
                        {
                            GetoffersAsyncRes = JsonConvert.DeserializeObject<GetoffersAsyncResponse>(responseContent);
                        }



                        return true;
                    }
                    else
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.error.message, ErrorRes.error.messagezhCHS);
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


        public async Task<bool> PatchoffersAsync(string offerGuid, List<Dictionary<string, dynamic>> offerGames, string offerName, string offerAvailableFromDateUTC, string offerAvailableToDateUTC, int durationAvailableInDaysAfterAwarded, int numberOfRounds)
        {
            try
            {
                if (await GetAPITokenAsync("client_credentials"))
                {

                    Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>
                                                    {
                                                        { "offerName", offerName },
                                                        { "offerGames",  offerGames },
                                                        { "offerAvailableFromDateUTC", offerAvailableFromDateUTC },
                                                        { "offerAvailableToDateUTC", offerAvailableFromDateUTC },
                                                        { "durationAvailableInDaysAfterAwarded", durationAvailableInDaysAfterAwarded },
                                                        { "numberOfRounds", numberOfRounds }
                                                    };

                    string endpoint = $"{this.API_URL}/agents/{this.Agent_Code_ID}/offers/{offerGuid}";
                    HttpResponseMessage response = await PatchApiInJsonAsync(endpoint, geek);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        Response = responseContent;

                        // Handle empty response
                        if (string.IsNullOrWhiteSpace(responseContent) || responseContent.Trim() == "{}")
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
                        string responseContent = await ReadResponseMessageAsync(response);
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.error.message, ErrorRes.error.messagezhCHS);
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

        public async Task<bool> PatchoffersCloseAsync(string offerGuid)
        {
            try
            {
                if (await GetAPITokenAsync("client_credentials"))
                {

                    Dictionary<string, dynamic> geek = new Dictionary<string, dynamic>();


                    string endpoint = $"{this.API_URL}/agents/{this.Agent_Code_ID}/offers/{offerGuid}/close";
                    HttpResponseMessage response = await PatchApiInJsonAsync(endpoint, geek);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string responseContent = await ReadResponseMessageAsync(response);
                        Response = responseContent;

                        // Handle empty response
                        if (string.IsNullOrWhiteSpace(responseContent) || responseContent.Trim() == "{}")
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
                        string responseContent = await ReadResponseMessageAsync(response);
                        ErrorRes = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                        ErrorMessage = response.ReasonPhrase + "," + string.Join(" | ", ErrorRes.error.message, ErrorRes.error.messagezhCHS);
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


        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public class TokenResponse
        {
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }

            [JsonProperty("token_type")]
            public string TokenType { get; set; }

            [JsonProperty("expires_in")]
            public int ExpiresIn { get; set; }

            [JsonProperty("refresh_token")]
            public string RefreshToken { get; set; }
        }

        public class BetSetting
        {
            public string bettingProfileName { get; set; }
            public object maxBet { get; set; }
            public double minBet { get; set; }
        }

        public class Language
        {
            public string code { get; set; }
            public string level { get; set; }
        }

        public class Platform
        {
            public string platform { get; set; }
            public List<BetSetting> betSettings { get; set; }
        }

        public class GetGamesResponse
        {
            public string gameName { get; set; }
            public string gameCode { get; set; }
            public List<TranslatedGameName> translatedGameName { get; set; }
            public string channelCode { get; set; }
            public string channelName { get; set; }
            public string gameCategoryCode { get; set; }
            public string gameCategoryName { get; set; }
            public string gameSubcategoryCode { get; set; }
            public string gameSubcategoryName { get; set; }
            public DateTime? releaseDateUTC { get; set; }
            public string platform { get; set; }
            public List<Platform> platforms { get; set; }
            public bool isDemoEnabled { get; set; }
            public List<Language> languages { get; set; }
            public double? rtp { get; set; }
            public List<string> gameFeatures { get; set; }
            public bool isFreeGamesSupported { get; set; }
            public string gameAssetsUrl { get; set; }
            public string volatility { get; set; }
            public string linesWays { get; set; }
            public string reels { get; set; }
            public string orientationSupport { get; set; }
            public string tags { get; set; }
        }

        public class TranslatedGameName
        {
            public string code { get; set; }
            public string value { get; set; }
        }


        public class GetGameDetailsResponse
        {
            public string gameName { get; set; }
            public string gameCode { get; set; }
            public List<GetGameDetailsTranslatedGameName> translatedGameName { get; set; }
            public string channelCode { get; set; }
            public string channelName { get; set; }
            public string gameCategoryCode { get; set; }
            public string gameCategoryName { get; set; }
            public string gameSubcategoryCode { get; set; }
            public string gameSubcategoryName { get; set; }
            public DateTime? releaseDateUTC { get; set; }
            public string platform { get; set; }
            public List<GetGameDetailsPlatform> platforms { get; set; }
            public bool isDemoEnabled { get; set; }
            public List<GetGameDetailsLanguage> languages { get; set; }
            public double rtp { get; set; }
            public List<string> gameFeatures { get; set; }
            public bool isFreeGamesSupported { get; set; }
            public string gameAssetsUrl { get; set; }
            public string volatility { get; set; }
            public string linesWays { get; set; }
            public string reels { get; set; }
            public string orientationSupport { get; set; }
            public string tags { get; set; }
        }

        public class GetGameDetailsTranslatedGameName
        {
            public string code { get; set; }
            public string value { get; set; }
        }

        public class GetGameDetailsPlatform
        {
            public string platform { get; set; }
            public List<BetSetting> betSettings { get; set; }
        }

        public class GetGameDetailsBetSetting
        {
            public string bettingProfileName { get; set; }
            public object maxBet { get; set; }
            public double minBet { get; set; }
        }

        public class GetGameDetailsLanguage
        {
            public string code { get; set; }
            public string level { get; set; }
        }


        public class GetImageTemplateResponse
        {
            public string gameCode { get; set; }
            public string templateType { get; set; }
            public string imageUrl { get; set; }
        }

        public class GetBetsResponse
        {
            public List<Bet> Bets { get; set; }
        }

        public class Bet
        {
            public string betId { get; set; }
            public string playerId { get; set; }
            public double amount { get; set; }
            public string currency { get; set; }
            public string gameCode { get; set; }
            public string betStatus { get; set; }
            public DateTime betTimeUtc { get; set; }
        }

        public class GetIncompleteBetsResponse
        {
            public List<IncompleteBet> IncompleteBets { get; set; }
        }

        public class IncompleteBet
        {
            public string betId { get; set; }
            public string playerId { get; set; }
            public string productId { get; set; }
            public double amount { get; set; }
            public string currency { get; set; }
            public string gameCode { get; set; }
            public string status { get; set; }
            public DateTime betTimeUtc { get; set; }
        }

        public class GetPlayerIncompleteBetsResponse
        {
            public List<GetPlayerIncompleteBet> IncompleteBets { get; set; }
        }

        public class Metadata
        {
            public string Description { get; set; }
        }

        public class OpenBet
        {
            public string playerId { get; set; }
            public string productId { get; set; }
            public string gameCode { get; set; }
            public string platform { get; set; }
            public double betAmount { get; set; }
            public double payoutAmount { get; set; }
            public DateTime createdDateUtc { get; set; }
            public DateTime estimatedSettlementTimeUtc { get; set; }
            public string externalTransactionId { get; set; }
            public Metadata metadata { get; set; }
        }

        public class GetPlayerIncompleteBet
        {
            public string product { get; set; }
            public string status { get; set; }
            public List<OpenBet> openBets { get; set; }
        }

        public class GetLiveDealerBetsResponse
        {
            public List<LiveDealerBet> LiveDealerBets { get; set; }
        }

        public class LiveDealerBet
        {
            public string betId { get; set; }
            public string playerId { get; set; }
            public double amount { get; set; }
            public string currency { get; set; }
            public string gameCode { get; set; }
            public string betStatus { get; set; }
            public DateTime betTimeUtc { get; set; }
            public string dealerName { get; set; }
            public string tableId { get; set; }
        }

        public class GetBettingProfilesResponse
        {
            public List<BettingProfile> BettingProfiles { get; set; }
        }

        public class BettingProfile
        {
            public string profileId { get; set; }
            public string profileName { get; set; }
            public double minBet { get; set; }
            public double maxBet { get; set; }
            public string currency { get; set; }
        }


        public class GetPlayerBettingProfilesResponse
        {
            public List<GetPlayerBettingProfile> BettingProfiles { get; set; }
        }

        public class GetPlayerBettingProfile
        {
            public string profileId { get; set; }
            public string profileName { get; set; }
            public double minBet { get; set; }
            public double maxBet { get; set; }
            public string currency { get; set; }
        }

        public class Error
        {
            public string code { get; set; }
            public string message { get; set; }

            [JsonProperty("message-zh-CHS")]
            public string messagezhCHS { get; set; }
        }

        public class ErrorResponse
        {
            public Error error { get; set; }
        }

        public class PostBettingProfileResponse
        {
            public int bettingProfileId { get; set; }
            public string gameType { get; set; }
            public string bettingProfileName { get; set; }
            public string bettingProfileDescription { get; set; }
            public string currency { get; set; }
        }

        public class DeleteBettingProfileResponse
        {
            public int bettingProfileId { get; set; }
            public string gameType { get; set; }
            public string bettingProfileName { get; set; }
            public string bettingProfileDescription { get; set; }
            public string currency { get; set; }
        }


        public class GameCategory
        {
            public string productId { get; set; }
            public string gameCategoryCode { get; set; }
            public string gameCategoryDescription { get; set; }
        }

        public class GetGameCategoriesResponse
        {
            public List<GameCategory> gameCategories { get; set; }
        }


        public class GetPlayerGameCategoriesResponse
        {
            public List<GetPlayerGameCategoriesGameCategory> gameCategories { get; set; }
        }

        public class GetPlayerGameCategoriesGameCategory
        {
            public string productId { get; set; }
            public string gameCategoryCode { get; set; }
            public string gameCategoryDescription { get; set; }

        }


        public class PostPlayerGameCategoryResponse
        {
            public string productId { get; set; }
            public string gameCategoryCode { get; set; }
            public string gameCategoryDescription { get; set; }
            public string uri { get; set; }
        }


        public class DeletePlayerGameCategoryResponse
        {
            public string productId { get; set; }
            public string gameCategoryCode { get; set; }
            public string gameCategoryDescription { get; set; }
        }


        public class GetPlayerResponse
        {
            public bool isLocked { get; set; }
            public string playerId { get; set; }
            public DateTime createdDateUTC { get; set; }
            public Balance balance { get; set; }
            public string uri { get; set; }
        }

        public class Balance
        {
            public double total { get; set; }
            public bool isPartial { get; set; }
            public BalanceDetails details { get; set; }
        }

        public class BalanceDetails
        {
            public WalletBalance walletBalance { get; set; }
            public ProductsBalance productsBalance { get; set; }
        }

        public class WalletBalance
        {
            public double total { get; set; }
        }

        public class ProductsBalance
        {
            public double total { get; set; }
            public List<ProductBalance> details { get; set; }
        }

        public class ProductBalance
        {
            public string productId { get; set; }
            public double balance { get; set; }
        }


        public class PatchPlayerResponse
        {
            public string playerId { get; set; }
            public bool isLocked { get; set; }
            public DateTime createdDateUTC { get; set; }
            public string uri { get; set; }
        }


        public class PostPlayerResponse
        {
            public string playerId { get; set; }
            public bool isLocked { get; set; }
            public DateTime createdDateUTC { get; set; }
            public string uri { get; set; }
        }

        public class PlayerProductInfoResponse
        {
            public string productId { get; set; }
            public bool isLocked { get; set; }
            public double balance { get; set; }
            public string uri { get; set; }
        }


        public class ProductInfo
        {
            public string productId { get; set; }
            public bool isLocked { get; set; }
            public string uri { get; set; }
        }

        public class PlayerAllProductsResponse
        {
            public List<ProductInfo> products { get; set; }
        }

        public class PostPlayerProductBetVisualizersAsyncResponse
        {
            public string url { get; set; }
        }

        public class ProductUrl
        {
            public string product { get; set; }
            public string url { get; set; }
        }

        public class PostPlayerBetVisualizersAsyncResponse
        {
            public List<ProductUrl> productUrls { get; set; }
        }

        public class PostPlayerSessionResponse
        {
            public string gameURL { get; set; }
            public string url { get; set; }
        }

        public class WalletTransactionsResponse
        {
            public string id { get; set; }
            public string status { get; set; }
            public string type { get; set; }
            public string playerId { get; set; }
            public double amount { get; set; }
            public string externalTransactionId { get; set; }
            public string idempotencyKey { get; set; }
            public DateTime createdDateUTC { get; set; }
            public List<TransactionDetail> details { get; set; }
            public string uri { get; set; }
        }

        public class TransactionDetail
        {
            public string product { get; set; }
            public string status { get; set; }
            public double amount { get; set; }
        }


        public class PWTTransactionDetail
        {
            public string Product { get; set; }
            public string Status { get; set; }
            public decimal Amount { get; set; }
        }

        public class PostWalletTransactionsAsyncResponse
        {
            public string Id { get; set; }
            public string Status { get; set; }
            public string Type { get; set; }
            public string PlayerId { get; set; }
            public decimal Amount { get; set; }
            public string ExternalTransactionId { get; set; }
            public string IdempotencyKey { get; set; }
            public DateTime CreatedDateUTC { get; set; }
            public List<PWTTransactionDetail> Details { get; set; }
            public string Uri { get; set; }

        }

        public class GWTTransactionDetail
        {
            public string Product { get; set; }
            public string Status { get; set; }
            public decimal Amount { get; set; }
        }

        public class GetWalletTransactionsAsyncResponse
        {
            public string Id { get; set; }
            public string Status { get; set; }
            public string Type { get; set; }
            public string PlayerId { get; set; }
            public decimal Amount { get; set; }
            public string ExternalTransactionId { get; set; }
            public string IdempotencyKey { get; set; }
            public DateTime CreatedDateUTC { get; set; }
            public List<GWTTransactionDetail> Details { get; set; }
            public string Uri { get; set; }
        }


        public class PostReportLedgerAsyncResponse
        {
            public DateTime TransactionTime { get; set; }
            public string TransactionStatus { get; set; }
            public string ProductCode { get; set; }
            public string Product { get; set; }
            public string AgentCode { get; set; }
            public string Agent { get; set; }
            public string PlayerId { get; set; }
            public string TransactionType { get; set; }
            public string ExternalTransactionId { get; set; }
            public string TransactionId { get; set; }
            public decimal Amount { get; set; }
        }




        public class PostReportFinancialAsyncResponse
        {
            public DateTime Time { get; set; }
            public string ProductCode { get; set; }
            public string ProductDescription { get; set; }
            public string AgentCode { get; set; }
            public string AgentDescription { get; set; }
            public decimal Income { get; set; }
            public decimal IncomePercentage { get; set; }
            public decimal Payout { get; set; }
            public decimal PayoutPercentage { get; set; }
            public decimal GrossWin { get; set; }
            public decimal GrossWinPercentage { get; set; }
            public int NumOfBets { get; set; }
            public decimal Margin { get; set; }
            public decimal DepositAmount { get; set; }

            [JsonPropertyName("withdrawalAMount")] // Fixing incorrect JSON field capitalization
            public decimal WithdrawalAmount { get; set; }

            public decimal NetCash { get; set; }
            public int NumOfDeposits { get; set; }
            public int NumOfWithdrawals { get; set; }
        }




        public class PostReportgameAnalysisAsyncResponse
        {
            public DateTime time { get; set; }
            public string productCode { get; set; }
            public string productDescription { get; set; }
            public string agentCode { get; set; }
            public string agentDescription { get; set; }
            public string gameCode { get; set; }
            public string gameName { get; set; }
            public string platform { get; set; }
            public int income { get; set; }
            public int incomePercentage { get; set; }
            public int payout { get; set; }
            public int payoutPercentage { get; set; }
            public int grossWin { get; set; }
            public int grossWinPercentage { get; set; }
            public int numOfActivePlayers { get; set; }
            public int numOfBets { get; set; }
            public int numOfBetsPercentage { get; set; }
            public int numOfPayouts { get; set; }
            public int numOfPayoutsPercentage { get; set; }
            public int margin { get; set; }
            public int maxBet { get; set; }
            public int maxPayout { get; set; }
            public int avgBet { get; set; }
            public int avgPayout { get; set; }
        }




        // Root myDeserializedClass = JsonConvert.DeserializeObject<List<Root>>(myJsonResponse);
        public class PostReportplayerGameAnalysisAsyncResponse
        {
            public string productCode { get; set; }
            public string productDescription { get; set; }
            public DateTime time { get; set; }
            public string playerId { get; set; }
            public string channel { get; set; }
            public string gameName { get; set; }
            public string gameCode { get; set; }
            public string platform { get; set; }
            public double income { get; set; }
            public double payout { get; set; }
            public double grossWin { get; set; }
            public int numOfBets { get; set; }
            public int numOfPayouts { get; set; }
            public double margin { get; set; }
            public double maxBet { get; set; }
            public double maxPayout { get; set; }
            public double avgBet { get; set; }
            public double avgPayout { get; set; }
            public double progressiveBetAmount { get; set; }
        }





        public class PostReportprogressiveGamePlayAsyncResponse
        {
            public DateTime time { get; set; }
            public string productCode { get; set; }
            public string productDescription { get; set; }
            public string agentCode { get; set; }
            public string agentDescription { get; set; }
            public string gameCode { get; set; }
            public string gameName { get; set; }
            public string platform { get; set; }
            public int income { get; set; }
            public int numOfBets { get; set; }
            public int PCA { get; set; }
        }




        public class PostReportprogressiveWinsAsyncResponse
        {
            public string productPlayerId { get; set; }
            public string productCode { get; set; }
            public string productDescription { get; set; }
            public DateTime jackpotDate { get; set; }
            public string currency { get; set; }
            public string agentCode { get; set; }
            public string agentDescription { get; set; }
            public string playerId { get; set; }
            public string gameCode { get; set; }
            public string gameName { get; set; }
            public string platform { get; set; }
            public int jackpotAmount { get; set; }
            public int jackpotType { get; set; }
            public string jackpotNumber { get; set; }
            public string transactionId { get; set; }
            public string betUID { get; set; }
        }




        public class PostReportfortuneRewardsAsyncResponse
        {
            public string campaignName { get; set; }
            public string playerId { get; set; }
            public DateTime creditDate { get; set; }
            public string transactionId { get; set; }
            public int rewardAmount { get; set; }
            public int payout { get; set; }
            public string referenceTransactionId { get; set; }
            public string rewardType { get; set; }
        }





        public class PostReportliveDealerPromotionsAsyncResponse
        {
            public string externalTransactionId { get; set; }
            public DateTime transactionTime { get; set; }
            public string currency { get; set; }
            public int winAmount { get; set; }
            public string playerId { get; set; }
            public string providerPlayerId { get; set; }
            public string agentCode { get; set; }
            public string agentDescription { get; set; }
            public string promotionType { get; set; }
            public string promotionDescription { get; set; }
        }




        public class PostReporttournamentWinsAsyncResponse
        {
            public int tournamentPeriodId { get; set; }
            public int tournamentId { get; set; }
            public string tournamentName { get; set; }
            public DateTime periodStartDate { get; set; }
            public DateTime periodEndDate { get; set; }
            public DateTime creditDate { get; set; }
            public string currency { get; set; }
            public string playerId { get; set; }
            public string aliasName { get; set; }
            public int rank { get; set; }
            public int winAmount { get; set; }
            public string winType { get; set; }
            public string prizeType { get; set; }
        }



        public class PostdemoSessionsAsyncResponse
        {
            public string url { get; set; }
            public string gameURL { get; set; }
        }


        public class PostoffersAsyncResponse
        {
            public string offerGuid { get; set; }
            public bool reuse { get; set; }
        }


        public class FreeGameOfferUsage
        {
            public int numberOfAssignedPlayers { get; set; }
            public int numberOfInstances { get; set; }
        }

        public class OfferGame
        {
            public int nearestCostPerBet { get; set; }
            public string gameCode { get; set; }
        }

        public class GetoffersAsyncResponse
        {
            public FreeGameOfferUsage freeGameOfferUsage { get; set; }
            public string offerStatus { get; set; }
            public DateTime offerAvailableFromDateUTC { get; set; }
            public DateTime offerAvailableToDateUTC { get; set; }
            public string offerGuid { get; set; }
            public DateTime offerCreateDateUTC { get; set; }
            public List<OfferGame> offerGames { get; set; }
            public string offerName { get; set; }
            public int durationAvailableInDaysAfterAwarded { get; set; }
            public int numberOfRounds { get; set; }
        }

    }

    public class Test
    {
        public static async Task Main(string[] args)
        {
            MGAPI api = new MGAPI(
               "https://api-powerasia.k2net.io/api/v1",
               "https://sts-powerasia.k2net.io",
               "zf1363tmyrag",
               "c7a238043f12452d862913ddf51d31"
           );

            Console.WriteLine("Starting API Tests...");

            //            // Authenticate and get token
            //            if (await api.GetAPITokenAsync("client_credentials"))
            //            {
            //                Console.WriteLine("GetAPITokenAsync successful!");
            //            }
            //            else
            //            {
            //                Console.WriteLine($"GetAPITokenAsync failed: {api.ErrorMessage}");
            //                return;
            //            }

            //            // Call API methods below:

            //            // Heartbeat
            //            if (await api.HeartbeatAsync())
            //            {
            //                Console.WriteLine("Heartbeat successful!");
            //            }
            //            else
            //            {
            //                Console.WriteLine($"Heartbeat failed: {api.ErrorMessage}");
            //            }

            //// Get Games
            //if (await api.GetGamesAsync())
            //{
            //    Console.WriteLine("GetGamesAsync successful!");
            //}
            //else
            //{
            //    Console.WriteLine($"GetGamesAsync failed: {api.ErrorMessage}");
            //}

            //            // Get Game Details
            //            string gameCode = "SAMPLE_GAME_CODE";
            //            if (await api.GetGameDetailsAsync(gameCode))
            //            {
            //                Console.WriteLine("GetGameDetailsAsync successful!");
            //            }
            //            else
            //            {
            //                Console.WriteLine($"GetGameDetailsAsync failed: {api.ErrorMessage}");
            //            }

            //            // Get Image Template
            //            if (await api.GetImageTemplateAsync())
            //            {
            //                Console.WriteLine("GetImageTemplateAsync successful!");
            //            }
            //            else
            //            {
            //                Console.WriteLine($"GetImageTemplateAsync failed: {api.ErrorMessage}");
            //            }


            //            // Get Bets
            //            string agentCode = "AGENT001"; // Replace with actual agent code
            //            int limit = 100; // Set a limit (1-20000)
            //            string startingAfter = null; // Optional, can be null
            //            bool? channel = true; // Optional, can be null or true/false

            //            if (await api.GetBetsAsync(agentCode, limit, startingAfter, channel))
            //            {
            //                Console.WriteLine("GetBetsAsync successful!");
            //            }
            //            else
            //            {
            //                Console.WriteLine($"GetBetsAsync failed: {api.ErrorMessage}");
            //            }

            //            // Get Incomplete Bets
            //            string playerId = "PLAYER123";
            //            string productId = "PROD001";
            //            if (await api.GetIncompleteBetsAsync(playerId, productId))
            //            {
            //                Console.WriteLine("GetIncompleteBetsAsync successful!");
            //            }
            //            else
            //            {
            //                Console.WriteLine($"GetIncompleteBetsAsync failed: {api.ErrorMessage}");
            //            }

            //            // Get Player Incomplete Bets
            //            if (await api.GetPlayerIncompleteBetsAsync(playerId))
            //            {
            //                Console.WriteLine("GetPlayerIncompleteBetsAsync successful!");
            //            }
            //            else
            //            {
            //                Console.WriteLine($"GetPlayerIncompleteBetsAsync failed: {api.ErrorMessage}");
            //            }

            //            // Get Live Dealer Bets
            //            if (await api.GetLiveDealerBetsAsync())
            //            {
            //                Console.WriteLine("GetLiveDealerBetsAsync successful!");
            //            }
            //            else
            //            {
            //                Console.WriteLine($"GetLiveDealerBetsAsync failed: {api.ErrorMessage}");
            //            }

            //            // Get Betting Profiles
            //            if (await api.GetBettingProfilesAsync(productId))
            //            {
            //                Console.WriteLine("GetBettingProfilesAsync successful!");
            //            }
            //            else
            //            {
            //                Console.WriteLine($"GetBettingProfilesAsync failed: {api.ErrorMessage}");
            //            }

            //            // Get Player Betting Profiles
            //            if (await api.GetPlayerBettingProfilesAsync(productId, playerId))
            //            {
            //                Console.WriteLine("GetPlayerBettingProfilesAsync successful!");
            //            }
            //            else
            //            {
            //                Console.WriteLine($"GetPlayerBettingProfilesAsync failed: {api.ErrorMessage}");
            //            }

            //            // Post Player Betting Profile
            //            int bettingProfileId = 101;
            //            if (await api.PostPlayerBettingProfilesAsync(productId, playerId, bettingProfileId))
            //            {
            //                Console.WriteLine("PostPlayerBettingProfilesAsync successful!");
            //            }
            //            else
            //            {
            //                Console.WriteLine($"PostPlayerBettingProfilesAsync failed: {api.ErrorMessage}");
            //            }

            //            // Delete Player Betting Profile
            //            if (await api.DeletePlayerBettingProfilesAsync(productId, playerId, bettingProfileId))
            //            {
            //                Console.WriteLine("DeletePlayerBettingProfilesAsync successful!");
            //            }
            //            else
            //            {
            //                Console.WriteLine($"DeletePlayerBettingProfilesAsync failed: {api.ErrorMessage}");
            //            }


            //            // Get Game Categories
            //            if (await api.GetGameCategoriesAsync())
            //            {
            //                Console.WriteLine("GetGameCategoriesAsync successful!");
            //            }
            //            else
            //            {
            //                Console.WriteLine($"GetGameCategoriesAsync failed: {api.ErrorMessage}");
            //            }

            //            // Get Player Game Categories
            //            if (await api.GetPlayerGameCategoriesAsync(playerId))
            //            {
            //                Console.WriteLine("GetPlayerGameCategoriesAsync successful!");
            //            }
            //            else
            //            {
            //                Console.WriteLine($"GetPlayerGameCategoriesAsync failed: {api.ErrorMessage}");
            //            }

            //            // Post Player Game Category
            //            string gameCategoryCode = "SLOTS"; // Example category
            //            if (await api.PostPlayerGameCategoryAsync(playerId, productId, gameCategoryCode))
            //            {
            //                Console.WriteLine("PostPlayerGameCategoryAsync successful!");
            //            }
            //            else
            //            {
            //                Console.WriteLine($"PostPlayerGameCategoryAsync failed: {api.ErrorMessage}");
            //            }

            //            // Delete Player Game Category
            //            if (await api.DeletePlayerGameCategoryAsync(playerId, productId, gameCategoryCode))
            //            {
            //                Console.WriteLine("DeletePlayerGameCategoryAsync successful!");
            //            }
            //            else
            //            {
            //                Console.WriteLine($"DeletePlayerGameCategoryAsync failed: {api.ErrorMessage}");
            //            }

            // Get Player
            //if (await api.GetPlayerAsync("mitjenny",true))
            //{
            //    Console.WriteLine("GetPlayerAsync successful!");
            //}
            //else
            //{
            //    Console.WriteLine($"GetPlayerAsync failed: {api.ErrorMessage}");
            //}


            //            if (await api.PatchPlayerAsync(playerId, true))
            //            {
            //                Console.WriteLine("PatchPlayerAsync successful!");
            //            }
            //            else
            //            {
            //                Console.WriteLine($"PatchPlayerAsync failed: {api.ErrorMessage}");
            //            }


            //            if (await api.PostPlayerAsync("NEWPLAYER001"))
            //            {
            //                Console.WriteLine("PostPlayerAsync successful!");
            //            }
            //            else
            //            {
            //                Console.WriteLine($"PostPlayerAsync failed: {api.ErrorMessage}");
            //            }

            //            // Get Player Product Info
            //            if (await api.GetPlayerProductAsync("NEWPLAYER001","productid"))
            //            {
            //                Console.WriteLine("GetPlayerProductInfoAsync successful!");
            //            }
            //            else
            //            {
            //                Console.WriteLine($"GetPlayerProductInfoAsync failed: {api.ErrorMessage}");
            //            }

            //            // Get Player All Products
            //            if (await api.GetPlayerAllProductAsync(playerId))
            //            {
            //                Console.WriteLine("GetPlayerAllProductsAsync successful!");
            //            }
            //            else
            //            {
            //                Console.WriteLine($"GetPlayerAllProductsAsync failed: {api.ErrorMessage}");
            //            }




            //             playerId = "PLAYER123";
            //             productId = "PROD001";
            //            int utcOffset = 8;  
            //            string langCode = "en-US"; 
            //            string betUid = null; 
            //            string game = "Blackjack";  
            //            string fromDate = "2024-03-01T00:00:00Z"; 
            //            string toDate = "2024-03-02T23:59:59Z";  

            //            // Call the API function
            //            if (await api.PostPlayerProductBetVisualizersAsync(playerId, productId, utcOffset, langCode, betUid, game, fromDate, toDate))
            //            {
            //                Console.WriteLine("PostPlayerProductBetVisualizersAsync successful!");
            //            }
            //            else
            //            {
            //                Console.WriteLine($"PostPlayerProductBetVisualizersAsync failed: {api.ErrorMessage}");
            //            }


            //             playerId = "PLAYER123";
            //             utcOffset = 8;
            //             langCode = "en-US";
            //             betUid = null;
            //             game = "Roulette";
            //             fromDate = "2024-03-01T00:00:00Z";
            //             toDate = "2024-03-02T23:59:59Z";

            //            if (await api.PostPlayerBetVisualizersAsync(playerId, utcOffset, langCode, betUid, game, fromDate, toDate))
            //            {
            //                Console.WriteLine("PostPlayerBetVisualizersAsync successful!");
            //            }
            //            else
            //            {
            //                Console.WriteLine($"PostPlayerBetVisualizersAsync failed: {api.ErrorMessage}");
            //            }


            //if (await api.PostPlayerSessionAsync("mitjenny", "SMG_icePopParty", "Desktop", "EN"))
            //{
            //    Console.WriteLine("PostPlayerSessionAsync successful!");
            //}
            //else
            //{
            //    Console.WriteLine($"PostPlayerSessionAsync failed: {api.ErrorMessage}");
            //}

            //            string idempotencyKey = "123e4567-e89b-12d3-a456-426614174000";

            //            if (await api.WalletTransactionsAsync(idempotencyKey))
            //            {
            //                Console.WriteLine("WalletTransactionsAsync successful!");
            //            }
            //            else
            //            {
            //                Console.WriteLine($"WalletTransactionsAsync failed: {api.ErrorMessage}");
            //            }

            //string playerId = "mitjenny";
            //string type = "deposit";
            //decimal? amount = 100.50m;
            //string idempotencyKey = "123e4567-e89b-12d3-a456-426614174000";
            //string externalTransactionId = "TXN789456";

            //if (await api.PostWalletTransactionsAsync(playerId, type, amount, idempotencyKey, externalTransactionId))
            //{
            //    Console.WriteLine("PostWalletTransactionsAsync successful!");
            //}
            //else
            //{
            //    Console.WriteLine($"PostWalletTransactionsAsync failed: {api.ErrorMessage}");
            //}

            //            string walletTransactionId = "WTX123456";

            //            if (await api.GetWalletTransactionsAsync(walletTransactionId))
            //            {
            //                Console.WriteLine("GetWalletTransactionsAsync successful!");
            //            }
            //            else
            //            {
            //                Console.WriteLine($"GetWalletTransactionsAsync failed: {api.ErrorMessage}");
            //            }

            //            DateTime fromDatedt = new DateTime(2024, 03, 01, 00, 00, 00, DateTimeKind.Utc);
            //            DateTime  toDatedt = new DateTime(2024, 03, 02, 23, 59, 59, DateTimeKind.Utc);
            //             utcOffset = 8;
            //            string currency = "USD";
            //             playerId = "PLAYER123";
            //            List<string> products = new List<string> { "PROD001", "PROD002" };
            //             externalTransactionId = "TXN789456";
            //            List<string> transactionTypes = new List<string> { "deposit", "withdrawal" };
            //            List<string> transactionStatuses = new List<string> { "completed", "pending" };
            //            string endingBefore = null;
            //             limit = 100;

            //            if (await api.PostReportLedgerAsync(fromDatedt, toDatedt, utcOffset, currency, playerId, products, externalTransactionId, transactionTypes, transactionStatuses, endingBefore, limit))
            //            {
            //                Console.WriteLine("PostReportLedgerAsync successful!");
            //            }
            //            else
            //            {
            //                Console.WriteLine($"PostReportLedgerAsync failed: {api.ErrorMessage}");
            //            }


            //DateTime fromDatedt = new DateTime(2025, 3, 4).Date;
            //DateTime toDatedt = new DateTime(2025, 3, 5).Date;
            //string timeAggregation = "daily";
            //int utcOffset = 8;
            //string currency = "MYR";
            //string playerId = "mitjenny";
            //List<string> productsA = new List<string> { "PROD1", "PROD2" };
            //List<string> channels = new List<string> { "Desktop", "Mobile" };
            //List<string> platforms = new List<string> { "Windows", "iOS" };
            //List<string> games = new List<string> { "Blackjack", "Poker" };
            //List<string> jackpotTypes = new List<string> { "Mega", "Minor" };
            //string jackpotNumber = "JACKPOT001";

            // Post Report Financial
            //if (await api.PostReportFinancialAsync(fromDatedt, toDatedt, timeAggregation, utcOffset, currency))
            //{
            //    Console.WriteLine("PostReportFinancialAsync successful!");
            //}
            //else
            //{
            //    Console.WriteLine($"PostReportFinancialAsync failed: {api.ErrorMessage}");
            //}

            //            // Post Report Game Analysis
            //            if (await api.PostReportgameAnalysisAsync(fromDatedt, toDatedt, timeAggregation, utcOffset, currency, playerId, products, channels, platforms, games))
            //            {
            //                Console.WriteLine("PostReportgameAnalysisAsync successful!");
            //            }
            //            else
            //            {
            //                Console.WriteLine($"PostReportgameAnalysisAsync failed: {api.ErrorMessage}");
            //            }

            //// Post Report Player Game Analysis
            //if (await api.PostReportplayerGameAnalysisAsync(fromDatedt, toDatedt, timeAggregation, utcOffset))
            //{
            //    Console.WriteLine("PostReportplayerGameAnalysisAsync successful!");
            //}
            //else
            //{
            //    Console.WriteLine($"PostReportplayerGameAnalysisAsync failed: {api.ErrorMessage}");
            //}

            //            // Post Report Progressive Game Play
            //            if (await api.PostReportprogressiveGamePlayAsync(fromDatedt, toDatedt, timeAggregation, utcOffset, playerId, currency, products, channels, platforms, games))
            //            {
            //                Console.WriteLine("PostReportprogressiveGamePlayAsync successful!");
            //            }
            //            else
            //            {
            //                Console.WriteLine($"PostReportprogressiveGamePlayAsync failed: {api.ErrorMessage}");
            //            }

            //            // Post Report Progressive Wins
            //            if (await api.PostReportprogressiveWinsAsync(fromDatedt, toDatedt, utcOffset, currency, playerId, products, jackpotTypes, jackpotNumber))
            //            {
            //                Console.WriteLine("PostReportprogressiveWinsAsync successful!");
            //            }
            //            else
            //            {
            //                Console.WriteLine($"PostReportprogressiveWinsAsync failed: {api.ErrorMessage}");
            //            }

            //              fromDatedt = new DateTime(2024, 3, 1);
            //              toDatedt = new DateTime(2024, 3, 31);
            //              utcOffset = 8;
            //              currency = "USD";
            //              playerId = "PLAYER123";
            //            List<string> rewardTypes = new List<string> { "Bonus", "Cashback" };
            //            List<string> promotionTypes = new List<string> { "VIP", "Seasonal" };
            //            List<string> tournaments = new List<string> { "Spring Championship", "Grand Finale" };

            //            // Post Report Fortune Rewards
            //            if (await api.PostReportfortuneRewardsAsync(fromDatedt, toDatedt, utcOffset, rewardTypes))
            //            {
            //                Console.WriteLine("PostReportfortuneRewardsAsync successful!");
            //            }
            //            else
            //            {
            //                Console.WriteLine($"PostReportfortuneRewardsAsync failed: {api.ErrorMessage}");
            //            }

            //            // Post Report Live Dealer Promotions
            //            if (await api.PostReportliveDealerPromotionsAsync(fromDatedt, toDatedt, utcOffset, currency, playerId, promotionTypes))
            //            {
            //                Console.WriteLine("PostReportliveDealerPromotionsAsync successful!");
            //            }
            //            else
            //            {
            //                Console.WriteLine($"PostReportliveDealerPromotionsAsync failed: {api.ErrorMessage}");
            //            }

            //            // Post Report Tournament Wins
            //            if (await api.PostReporttournamentWinsAsync(fromDatedt, toDatedt, utcOffset, playerId, tournaments))
            //            {
            //                Console.WriteLine("PostReporttournamentWinsAsync successful!");
            //            }
            //            else
            //            {
            //                Console.WriteLine($"PostReporttournamentWinsAsync failed: {api.ErrorMessage}");
            //            }

            //            // Post Demo Sessions
            //              contentCode = "DEMO123";
            //              platform = "Mobile";
            //              langCode = "en-US";
            //            string demoPlayerId = "PLAYER123";

            //            if (await api.PostdemoSessionsAsync(contentCode, platform, langCode, demoPlayerId))
            //            {
            //                Console.WriteLine("PostdemoSessionsAsync successful!");
            //            }
            //            else
            //            {
            //                Console.WriteLine($"PostdemoSessionsAsync failed: {api.ErrorMessage}");
            //            }

            //            // Post Offers
            //            string offerName = "New Year Bonus";
            //            List<Dictionary<string, dynamic>> offerGames = new List<Dictionary<string, dynamic>>
            //{
            //    new Dictionary<string, dynamic> { { "gameCode", "Blackjack" }, { "betMultiplier", 2 } },
            //    new Dictionary<string, dynamic> { { "gameCode", "Roulette" }, { "betMultiplier", 1.5 } }
            //};
            //            int durationAvailableInDaysAfterAwarded = 7;
            //            int numberOfRounds = 10;
            //            string offerAvailableFromDateUTC = "2024-04-01T00:00:00Z";
            //            string offerAvailableToDateUTC = "2024-04-30T23:59:59Z";

            //            if (await api.PostoffersAsync(offerName, offerGames, durationAvailableInDaysAfterAwarded, numberOfRounds, offerAvailableFromDateUTC, offerAvailableToDateUTC))
            //            {
            //                Console.WriteLine("PostoffersAsync successful!");
            //            }
            //            else
            //            {
            //                Console.WriteLine($"PostoffersAsync failed: {api.ErrorMessage}");
            //            }

            //            // Get Offers
            //            string offerGuidStartsWith = "OFFER123";
            //            string offerNameContains = "Bonus";
            //            string availableFromDateUTC = "2024-04-01T00:00:00Z";
            //            string availableToDateUTC = "2024-04-30T23:59:59Z";
            //            string offerStatus = "Active";
            //            int page = 1;
            //            int perPage = 20;

            //            if (await api.GetoffersAsync(offerGuidStartsWith, offerNameContains, availableFromDateUTC, availableToDateUTC, offerStatus, page, perPage))
            //            {
            //                Console.WriteLine("GetoffersAsync successful!");
            //            }
            //            else
            //            {
            //                Console.WriteLine($"GetoffersAsync failed: {api.ErrorMessage}");
            //            }

            //            // Patch Offers
            //            string offerGuid = "OFFER123456";
            //            string patchedOfferName = "Updated Bonus";
            //            string patchedOfferAvailableFromDateUTC = "2024-04-01T00:00:00Z";
            //            string patchedOfferAvailableToDateUTC = "2024-05-01T23:59:59Z";
            //            int patchedDurationAvailableInDaysAfterAwarded = 10;
            //            int patchedNumberOfRounds = 15;

            //            if (await api.PatchoffersAsync(offerGuid, offerGames, patchedOfferName, patchedOfferAvailableFromDateUTC, patchedOfferAvailableToDateUTC, patchedDurationAvailableInDaysAfterAwarded, patchedNumberOfRounds))
            //            {
            //                Console.WriteLine("PatchoffersAsync successful!");
            //            }
            //            else
            //            {
            //                Console.WriteLine($"PatchoffersAsync failed: {api.ErrorMessage}");
            //            }

            //            // Patch Offers Close
            //            if (await api.PatchoffersCloseAsync(offerGuid))
            //            {
            //                Console.WriteLine("PatchoffersCloseAsync successful!");
            //            }
            //            else
            //            {
            //                Console.WriteLine($"PatchoffersCloseAsync failed: {api.ErrorMessage}");
            //            }



            Console.WriteLine("API Tests Completed.");
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}
