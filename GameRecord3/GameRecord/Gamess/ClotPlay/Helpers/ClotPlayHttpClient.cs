using GameRecord.Gamess.ClotPlay.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GameRecord.Gamess.ClotPlay.Helpers
{
    public class ApiHttpClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _operatorToken;
        private readonly bool _debugMode;

        public ApiHttpClient(string baseUrl, string operatorToken, bool debugMode = false)
        {
            _baseUrl = baseUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(baseUrl));
            _operatorToken = operatorToken ?? throw new ArgumentNullException(nameof(operatorToken));
            _debugMode = debugMode;

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", _operatorToken);
        }

        public async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object requestData = null)
        {
            string url = null;
            string requestJson = null;
            string responseContent = null;

            try
            {
                url = $"{_baseUrl}/{endpoint.TrimStart('/')}";
                requestJson = requestData != null ? JsonConvert.SerializeObject(requestData) : null;

                var content = requestData != null
                    ? new StringContent(requestJson, Encoding.UTF8, "application/json")
                    : null;

                var response = await _httpClient.PostAsync(url, content).ConfigureAwait(false);
                responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"API call failed with status {response.StatusCode}: {responseContent}");
                }

                var apiResponse = JsonConvert.DeserializeObject<ApiResponse<T>>(responseContent);
                return apiResponse;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error calling API endpoint {endpoint}: {ex.Message}", ex);
            }
        }

        public async Task<ApiResponse<T>> GetAsync<T>(string endpoint)
        {
            string url = null;
            string responseContent = null;

            try
            {
                url = $"{_baseUrl}/{endpoint.TrimStart('/')}";
                var response = await _httpClient.GetAsync(url);
                responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"API call failed with status {response.StatusCode}: {responseContent}");
                }

                var apiResponse = JsonConvert.DeserializeObject<ApiResponse<T>>(responseContent);
                return apiResponse;
            }
            catch (Exception ex)
            {

                throw new Exception($"Error calling API endpoint {endpoint}: {ex.Message}", ex);
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}