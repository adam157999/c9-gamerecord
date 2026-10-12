using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ClotPlay
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

        /// <summary>
        /// Generate curl command for debugging purposes
        /// </summary>
        private string GenerateCurlCommand(string method, string url, string jsonData = null)
        {
            var curl = new StringBuilder();
            curl.Append("curl -X ").Append(method.ToUpper());
            curl.Append(" \"").Append(url).Append("\"");
            curl.Append(" -H \"Authorization: ").Append(_operatorToken).Append("\"");
            curl.Append(" -H \"Content-Type: application/json\"");

            if (!string.IsNullOrEmpty(jsonData))
            {
                // Escape quotes in JSON data for curl
                var escapedJson = jsonData.Replace("\"", "\\\"");
                curl.Append(" -d \"").Append(escapedJson).Append("\"");
            }

            return curl.ToString();
        }

        /// <summary>
        /// Print debug information to console
        /// </summary>
        private void PrintDebugInfo(string method, string url, string requestData, string responseData, TimeSpan elapsed)
        {
            if (!_debugMode) return;

            Console.WriteLine("\n" + new string('=', 80));
            Console.WriteLine($"[DEBUG] ClotPlay API Call - {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            Console.WriteLine(new string('=', 80));

            // Print curl command
            Console.WriteLine("[CURL COMMAND]");
            Console.WriteLine(GenerateCurlCommand(method, url, requestData));
            Console.WriteLine();

            // Print request details
            Console.WriteLine("[REQUEST]");
            Console.WriteLine($"Method: {method.ToUpper()}");
            Console.WriteLine($"URL: {url}");
            Console.WriteLine($"Headers:");
            Console.WriteLine($"  Authorization: {_operatorToken}");
            Console.WriteLine($"  Content-Type: application/json");

            if (!string.IsNullOrEmpty(requestData))
            {
                Console.WriteLine($"Body:");
                Console.WriteLine(FormatJson(requestData));
            }
            else
            {
                Console.WriteLine("Body: (empty)");
            }
            Console.WriteLine();

            // Print response details with color coding
            Console.WriteLine("[RESPONSE]");
            Console.WriteLine($"Elapsed Time: {elapsed.TotalMilliseconds:F2} ms");

            // Determine if response is success or error based on content
            bool isSuccess = IsSuccessResponse(responseData);

            if (isSuccess)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Response Body: (SUCCESS)");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Response Body: (ERROR)");
                Console.ResetColor();
            }

            Console.WriteLine(FormatJson(responseData));
            Console.WriteLine(new string('=', 80) + "\n");
        }

        /// <summary>
        /// Format JSON for better readability in debug output
        /// </summary>
        private string FormatJson(string json)
        {
            try
            {
                var parsed = JsonConvert.DeserializeObject(json);
                return JsonConvert.SerializeObject(parsed, Formatting.Indented);
            }
            catch
            {
                return json; // Return original if parsing fails
            }
        }

        /// <summary>
        /// Determine if the API response indicates success or error
        /// </summary>
        private bool IsSuccessResponse(string responseData)
        {
            try
            {
                // Try to parse as API response and check code field
                dynamic response = JsonConvert.DeserializeObject(responseData);
                if (response?.code != null)
                {
                    return response.code == 0; // ClotPlay API uses code 0 for success
                }

                // If no code field, check for common error indicators
                string lowerResponse = responseData?.ToLower() ?? "";
                return !lowerResponse.Contains("error") &&
                       !lowerResponse.Contains("not found") &&
                       !lowerResponse.Contains("exception") &&
                       !lowerResponse.Contains("<!doctype html"); // HTML error pages
            }
            catch
            {
                // If parsing fails, assume it's an error
                return false;
            }
        }

        public async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object requestData = null)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
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

                var response = await _httpClient.PostAsync(url, content);
                responseContent = await response.Content.ReadAsStringAsync();

                stopwatch.Stop();

                // Print debug info if enabled
                PrintDebugInfo("POST", url, requestJson, responseContent, stopwatch.Elapsed);

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"API call failed with status {response.StatusCode}: {responseContent}");
                }

                var apiResponse = JsonConvert.DeserializeObject<ApiResponse<T>>(responseContent);
                return apiResponse;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // Print debug info even for failed requests
                if (_debugMode && url != null)
                {
                    PrintDebugInfo("POST", url, requestJson, responseContent ?? ex.Message, stopwatch.Elapsed);
                }

                throw new Exception($"Error calling API endpoint {endpoint}: {ex.Message}", ex);
            }
        }

        public async Task<ApiResponse<T>> GetAsync<T>(string endpoint)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            string url = null;
            string responseContent = null;

            try
            {
                url = $"{_baseUrl}/{endpoint.TrimStart('/')}";
                var response = await _httpClient.GetAsync(url);
                responseContent = await response.Content.ReadAsStringAsync();

                stopwatch.Stop();

                // Print debug info if enabled
                PrintDebugInfo("GET", url, null, responseContent, stopwatch.Elapsed);

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"API call failed with status {response.StatusCode}: {responseContent}");
                }

                var apiResponse = JsonConvert.DeserializeObject<ApiResponse<T>>(responseContent);
                return apiResponse;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // Print debug info even for failed requests
                if (_debugMode && url != null)
                {
                    PrintDebugInfo("GET", url, null, responseContent ?? ex.Message, stopwatch.Elapsed);
                }

                throw new Exception($"Error calling API endpoint {endpoint}: {ex.Message}", ex);
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}