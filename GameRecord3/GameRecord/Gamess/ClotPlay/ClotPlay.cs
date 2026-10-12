using AESexy;
using GameRecord.Gamess.ClotPlay.Helpers;
using GameRecord.Gamess.ClotPlay.Models;
using Habanero.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ValidationException = GameRecord.Gamess.ClotPlay.Helpers.ValidationException;

namespace GameRecord.Gamess.ClotPlay
{
    public class ClotPlay : IDisposable
    {
        #region Declaration
        private readonly string _baseUrl;
        private readonly string _operatorToken;
        private readonly string _agentCode;
        private readonly int _timeOutSec;
        private readonly bool _debugMode;
        private readonly ApiHttpClient _httpClient;

        public string Parameters { get; private set; }
        public string Response { get; private set; }
        public string ErrorMessage { get; private set; }
        public string Request { get; private set; }
        public string TargetApiUrl { get; private set; }
        public bool ParseError { get; private set; } = false;

        // Response properties following Pragmatic pattern
        public LaunchGameResponse LaunchGameResponse { get; private set; }
        public CreateMemberResponse CreateMemberResponse { get; private set; }
        public GetMemberDetailResponse PlayerResponse { get; private set; }
        public WalletBalanceResponse WalletBalanceResponse { get; private set; }
        public object LogoutPlayerResponse { get; private set; }
        public WalletTransactionResponse WalletTransactionResponse { get; private set; }
        public TransactionListResponse TransactionListResponse { get; private set; }
        public List<DailyReportResponse> DailyReportResponse { get; private set; }
        public List<Game> GameListResponse { get; private set; }
        public BetDetailsResponse BetDetailsResponse { get; private set; }
        public List<SupportedLanguage> SupportedLanguagesResponse { get; private set; }
        public List<SupportedCurrency> SupportedCurrenciesResponse { get; private set; }

        #endregion

        public ClotPlay(string baseUrl, string operatorToken, string agentCode = null, int timeOutSec = 30, bool debugMode = false)
        {
            // Validate constructor parameters
            if (string.IsNullOrEmpty(baseUrl))
                throw new ArgumentException("Base URL is required", nameof(baseUrl));

            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri uri))
                throw new ArgumentException("Base URL must be a valid URL", nameof(baseUrl));

            var tokenValidation = ValidationHelper.ValidateOperatorToken(operatorToken);
            if (!tokenValidation.IsValid)
                throw new ArgumentException(tokenValidation.ErrorMessage, nameof(operatorToken));

            if (!string.IsNullOrEmpty(agentCode))
            {
                var agentValidation = ValidationHelper.ValidateAgentCode(agentCode);
                if (!agentValidation.IsValid)
                    throw new ArgumentException(agentValidation.ErrorMessage, nameof(agentCode));
            }

            if (timeOutSec <= 0)
                throw new ArgumentException("Timeout must be greater than 0", nameof(timeOutSec));

            _baseUrl = baseUrl;
            _operatorToken = operatorToken;
            _agentCode = agentCode;
            _timeOutSec = timeOutSec;
            _debugMode = debugMode;
            _httpClient = new ApiHttpClient(baseUrl, operatorToken, debugMode);
        }

        #region Member Operations

        /// <summary>
        /// Launch a game for a player
        /// </summary>
        public async Task<ApiResponse<LaunchGameResponse>> LaunchGameAsync(LaunchGameRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Validate request using comprehensive validation
            var validation = ValidationHelper.ValidateLaunchGameRequest(request);
            if (!validation.IsValid)
                throw new ValidationException(validation.ErrorMessage);

            if (!string.IsNullOrEmpty(_agentCode))
                request.agent_code = _agentCode;

            return await _httpClient.PostAsync<LaunchGameResponse>("MOGI/launch-game", request);
        }

        /// <summary>
        /// Create a new member
        /// </summary>
        public async Task<ApiResponse<CreateMemberResponse>> CreateMemberAsync(CreateMemberRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Validate request using comprehensive validation
            var validation = ValidationHelper.ValidateCreateMemberRequest(request);
            if (!validation.IsValid)
                throw new ValidationException(validation.ErrorMessage);

            if (!string.IsNullOrEmpty(_agentCode))
                request.agent_code = _agentCode;
            return await _httpClient.PostAsync<CreateMemberResponse>("MOGI/create-member", request);
        }

        /// <summary>
        /// Get member details
        /// </summary>
        public async Task<ApiResponse<GetMemberDetailResponse>> GetMemberDetailsAsync(MemberDetailsRequest request)
        {
            if (!string.IsNullOrEmpty(_agentCode))
                request.agent_code = _agentCode;
            return await _httpClient.PostAsync<GetMemberDetailResponse>("MOGI/member-details", request);
        }

        /// <summary>
        /// Logout a member
        /// </summary>
        public async Task<ApiResponse<object>> LogoutMemberAsync(MemberLogoutRequest request)
        {
            if (!string.IsNullOrEmpty(_agentCode))
                request.agent_code = _agentCode;
            return await _httpClient.PostAsync<object>("MOGI/member-logout", request);
        }

        /// <summary>
        /// Clear member feature
        /// </summary>
        public async Task<ApiResponse<object>> ClearMemberFeatureAsync(MemberDetailsRequest request)
        {
            if (!string.IsNullOrEmpty(_agentCode))
                request.agent_code = _agentCode;
            return await _httpClient.PostAsync<object>("MOGI/member-clear-feature", request);
        }

        #endregion

        #region Wallet Operations

        /// <summary>
        /// Deposit funds to player wallet
        /// </summary>
        public async Task<ApiResponse<WalletBalanceResponse>> DepositAsync(WalletDepositRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Validate request using comprehensive validation
            var validation = ValidationHelper.ValidateWalletDepositRequest(request);
            if (!validation.IsValid)
                throw new ValidationException(validation.ErrorMessage);

            if (!string.IsNullOrEmpty(_agentCode))
                request.agent_code = _agentCode;
            return await _httpClient.PostAsync<WalletBalanceResponse>("MOGI/wallet/deposit", request);
        }

        /// <summary>
        /// Withdraw funds from player wallet
        /// </summary>
        public async Task<ApiResponse<WalletBalanceResponse>> WithdrawAsync(WalletWithdrawRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Validate request using comprehensive validation
            var validation = ValidationHelper.ValidateWalletWithdrawRequest(request);
            if (!validation.IsValid)
                throw new ValidationException(validation.ErrorMessage);

            if (!string.IsNullOrEmpty(_agentCode))
                request.agent_code = _agentCode;
            return await _httpClient.PostAsync<WalletBalanceResponse>("MOGI/wallet/withdraw", request);
        }

        /// <summary>
        /// Adjust player wallet balance
        /// </summary>
        public async Task<ApiResponse<WalletTransactionResponse>> AdjustWalletAsync(WalletAdjustRequest request)
        {
            if (!string.IsNullOrEmpty(_agentCode))
                request.agent_code = _agentCode;
            return await _httpClient.PostAsync<WalletTransactionResponse>("MOGI/wallet/adjustment", request);
        }

        /// <summary>
        /// Get wallet transaction details
        /// </summary>
        public async Task<ApiResponse<TransactionListResponse>> GetWalletTransactionDetailsAsync(TransactionDetailsRequest request)
        {
            if (!string.IsNullOrEmpty(_agentCode))
                request.agent_code = _agentCode;
            return await _httpClient.PostAsync<TransactionListResponse>("MOGI/wallet/transaction-details", request);
        }

        #endregion

        #region Reporting

        /// <summary>
        /// Get transaction details
        /// </summary>
        public async Task<ApiResponse<List<GameTransaction>>> GetTransactionDetailsAsync(TransactionDetailsGetRequest request)
        {
            // Build query string for GET request
            var queryParams = new List<string>();

            if (!string.IsNullOrEmpty(request.updated_at_from))
                queryParams.Add($"updated_at_from={Uri.EscapeDataString(request.updated_at_from)}");

            if (!string.IsNullOrEmpty(request.updated_at_to))
                queryParams.Add($"updated_at_to={Uri.EscapeDataString(request.updated_at_to)}");

            if (!string.IsNullOrEmpty(request.created_at_from))
                queryParams.Add($"created_at_from={Uri.EscapeDataString(request.created_at_from)}");

            if (!string.IsNullOrEmpty(request.created_at_to))
                queryParams.Add($"created_at_to={Uri.EscapeDataString(request.created_at_to)}");

            if (request.perPage.HasValue)
                queryParams.Add($"perPage={request.perPage.Value}");

            if (request.page.HasValue)
                queryParams.Add($"page={request.page.Value}");

            if (!string.IsNullOrEmpty(request.transaction_id))
                queryParams.Add($"transaction_id={Uri.EscapeDataString(request.transaction_id)}");

            if (!string.IsNullOrEmpty(request.game_round_id))
                queryParams.Add($"game_round_id={Uri.EscapeDataString(request.game_round_id)}");

            if (!string.IsNullOrEmpty(_agentCode))
                queryParams.Add($"agent_code={Uri.EscapeDataString(_agentCode)}");
            else if (!string.IsNullOrEmpty(request.agent_code))
                queryParams.Add($"agent_code={Uri.EscapeDataString(request.agent_code)}");

            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var endpoint = $"MOGI/get-transaction-details{queryString}";

            return await _httpClient.GetAsync<List<GameTransaction>>(endpoint);
        }

        /// <summary>
        /// Get cancelled transactions
        /// </summary>
        public async Task<ApiResponse<CancelledTransactionsResponse>> GetCancelledTransactionsAsync(CancelledTransactionsRequest request)
        {
            // Build query string for GET request
            var queryParams = new List<string>();

            if (!string.IsNullOrEmpty(request.cancelled_at_from))
                queryParams.Add($"cancelled_at_from={Uri.EscapeDataString(request.cancelled_at_from)}");

            if (!string.IsNullOrEmpty(request.cancelled_at_to))
                queryParams.Add($"cancelled_at_to={Uri.EscapeDataString(request.cancelled_at_to)}");

            if (request.perPage.HasValue)
                queryParams.Add($"perPage={request.perPage.Value}");

            if (request.page.HasValue)
                queryParams.Add($"page={request.page.Value}");

            if (!string.IsNullOrEmpty(request.transaction_id))
                queryParams.Add($"transaction_id={Uri.EscapeDataString(request.transaction_id)}");

            if (!string.IsNullOrEmpty(request.game_round_id))
                queryParams.Add($"game_round_id={Uri.EscapeDataString(request.game_round_id)}");

            if (!string.IsNullOrEmpty(_agentCode))
                queryParams.Add($"agent_code={Uri.EscapeDataString(_agentCode)}");
            else if (!string.IsNullOrEmpty(request.agent_code))
                queryParams.Add($"agent_code={Uri.EscapeDataString(request.agent_code)}");

            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var endpoint = $"MOGI/get-cancelled-transactions{queryString}";

            return await _httpClient.GetAsync<CancelledTransactionsResponse>(endpoint);
        }

        /// <summary>
        /// Get daily report
        /// </summary>
        public async Task<ApiResponse<List<DailyReportResponse>>> GetDailyReportAsync(DailyReportRequest request)
        {
            // Build query string for GET request
            var queryParams = new List<string>();

            if (!string.IsNullOrEmpty(request.date_from))
                queryParams.Add($"date_from={Uri.EscapeDataString(request.date_from)}");

            if (!string.IsNullOrEmpty(request.date_to))
                queryParams.Add($"date_to={Uri.EscapeDataString(request.date_to)}");

            if (!string.IsNullOrEmpty(_agentCode))
                queryParams.Add($"agent_code={Uri.EscapeDataString(_agentCode)}");
            else if (!string.IsNullOrEmpty(request.agent_code))
                queryParams.Add($"agent_code={Uri.EscapeDataString(request.agent_code)}");

            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var endpoint = $"MOGI/get-daily-report{queryString}";

            return await _httpClient.GetAsync<List<DailyReportResponse>>(endpoint);
        }

        #endregion

        #region Game Operations

        /// <summary>
        /// Get list of available games
        /// </summary>
        public async Task<ApiResponse<List<Game>>> GetGameListAsync()
        {
            return await _httpClient.GetAsync<List<Game>>("MOGI/get-game-list");
        }

        /// <summary>
        /// Get bet details
        /// </summary>
        public async Task<ApiResponse<BetDetailsResponse>> GetBetDetailsAsync(BetDetailsRequest request)
        {
            return await _httpClient.PostAsync<BetDetailsResponse>("MOGI/bet-details", request);
        }

        #endregion

        #region Utility Operations

        /// <summary>
        /// Get supported languages
        /// </summary>
        public async Task<ApiResponse<List<SupportedLanguage>>> GetSupportedLanguagesAsync()
        {
            var response = await _httpClient.GetAsync<SupportedLanguagesResponse>("MOGI/languages");

            // Transform the response to match the expected return type
            return new ApiResponse<List<SupportedLanguage>>
            {
                code = response.code,
                data = response.data?.languages ?? new List<SupportedLanguage>(),
                message = response.message,
                debug = response.debug
            };
        }

        /// <summary>
        /// Get supported currencies
        /// </summary>
        public async Task<ApiResponse<List<SupportedCurrency>>> GetSupportedCurrenciesAsync()
        {
            var response = await _httpClient.GetAsync<SupportedCurrenciesResponse>("MOGI/currencies");

            // Transform the response to match the expected return type
            return new ApiResponse<List<SupportedCurrency>>
            {
                code = response.code,
                data = response.data?.currencies ?? new List<SupportedCurrency>(),
                message = response.message,
                debug = response.debug
            };
        }



        #endregion

        #region Agent Operations (for operators with agent system)

        /// <summary>
        /// Launch a game for a player (Agent version)
        /// </summary>
        public async Task<ApiResponse<LaunchGameResponse>> LaunchGameAgentAsync(LaunchGameRequest request)
        {
            if (string.IsNullOrEmpty(_agentCode))
                throw new ArgumentException("AgentCode must be provided in SDK constructor for agent operations");

            if (!string.IsNullOrEmpty(_agentCode))
                request.agent_code = _agentCode;

            return await _httpClient.PostAsync<LaunchGameResponse>("MOGI/launch-game", request);
        }

        /// <summary>
        /// Create a new member (Agent version)
        /// </summary>
        public async Task<ApiResponse<CreateMemberResponse>> CreateMemberAgentAsync(CreateMemberRequest request)
        {
            if (string.IsNullOrEmpty(_agentCode))
                throw new ArgumentException("AgentCode must be provided in SDK constructor for agent operations");

            if (!string.IsNullOrEmpty(_agentCode))
                request.agent_code = _agentCode;

            return await _httpClient.PostAsync<CreateMemberResponse>("MOGI/create-member", request);
        }

        /// <summary>
        /// Get member details (Agent version)
        /// </summary>
        public async Task<ApiResponse<Player>> GetMemberDetailsAgentAsync(MemberDetailsRequest request)
        {
            if (string.IsNullOrEmpty(_agentCode))
                throw new ArgumentException("AgentCode must be provided in SDK constructor for agent operations");

            if (!string.IsNullOrEmpty(_agentCode))
                request.agent_code = _agentCode;

            return await _httpClient.PostAsync<Player>("MOGI/member-details", request);
        }

        /// <summary>
        /// Logout a member (Agent version)
        /// </summary>
        public async Task<ApiResponse<object>> LogoutMemberAgentAsync(MemberLogoutRequest request)
        {
            if (string.IsNullOrEmpty(_agentCode))
                throw new ArgumentException("AgentCode must be provided in SDK constructor for agent operations");

            if (!string.IsNullOrEmpty(_agentCode))
                request.agent_code = _agentCode;

            return await _httpClient.PostAsync<object>("MOGI/member-logout", request);
        }

        /// <summary>
        /// Clear member feature (Agent version)
        /// </summary>
        public async Task<ApiResponse<object>> ClearMemberFeatureAgentAsync(MemberDetailsRequest request)
        {
            if (string.IsNullOrEmpty(_agentCode))
                throw new ArgumentException("AgentCode must be provided in SDK constructor for agent operations");

            if (!string.IsNullOrEmpty(_agentCode))
                request.agent_code = _agentCode;

            return await _httpClient.PostAsync<object>("MOGI/member-clear-feature", request);
        }

        /// <summary>
        /// Deposit funds to player wallet (Agent version)
        /// </summary>
        public async Task<ApiResponse<WalletBalanceResponse>> DepositAgentAsync(WalletDepositRequest request)
        {
            if (string.IsNullOrEmpty(_agentCode))
                throw new ArgumentException("AgentCode must be provided in SDK constructor for agent operations");

            if (!string.IsNullOrEmpty(_agentCode))
                request.agent_code = _agentCode;

            return await _httpClient.PostAsync<WalletBalanceResponse>("MOGI/wallet/deposit", request);
        }

        /// <summary>
        /// Withdraw funds from player wallet (Agent version)
        /// </summary>
        public async Task<ApiResponse<WalletBalanceResponse>> WithdrawAgentAsync(WalletWithdrawRequest request)
        {
            if (string.IsNullOrEmpty(_agentCode))
                throw new ArgumentException("AgentCode must be provided in SDK constructor for agent operations");

            if (!string.IsNullOrEmpty(_agentCode))
                request.agent_code = _agentCode;

            return await _httpClient.PostAsync<WalletBalanceResponse>("MOGI/wallet/withdraw", request);
        }

        /// <summary>
        /// Adjust player wallet balance (Agent version)
        /// </summary>
        public async Task<ApiResponse<WalletTransactionResponse>> AdjustWalletAgentAsync(WalletAdjustRequest request)
        {
            if (string.IsNullOrEmpty(_agentCode))
                throw new ArgumentException("AgentCode must be provided in SDK constructor for agent operations");

            if (!string.IsNullOrEmpty(_agentCode))
                request.agent_code = _agentCode;

            return await _httpClient.PostAsync<WalletTransactionResponse>("MOGI/wallet/adjust", request);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Create a LaunchGameRequest with common parameters
        /// </summary>
        public static LaunchGameRequest CreateLaunchGameRequest(
            string playerUniqueId,
            string playerName,
            string playerCurrency,
            string gameId,
             GameOrientation orientation,
            bool isDemo = false,
            string language = "en-us",
            string callbackUrl = null
           )
        {
            // Validate parameters before creating request
            var playerIdValidation = ValidationHelper.ValidatePlayerUniqueId(playerUniqueId);
            if (!playerIdValidation.IsValid)
                throw new ValidationException($"Invalid player_unique_id: {playerIdValidation.ErrorMessage}");

            var playerNameValidation = ValidationHelper.ValidatePlayerName(playerName);
            if (!playerNameValidation.IsValid)
                throw new ValidationException($"Invalid player_name: {playerNameValidation.ErrorMessage}");

            var currencyValidation = ValidationHelper.ValidatePlayerCurrency(playerCurrency);
            if (!currencyValidation.IsValid)
                throw new ValidationException($"Invalid player_currency: {currencyValidation.ErrorMessage}");

            var gameIdValidation = ValidationHelper.ValidateGameId(gameId);
            if (!gameIdValidation.IsValid)
                throw new ValidationException($"Invalid game_id: {gameIdValidation.ErrorMessage}");

            var languageValidation = ValidationHelper.ValidateLanguage(language);
            if (!languageValidation.IsValid)
                throw new ValidationException($"Invalid language: {languageValidation.ErrorMessage}");

            var callbackValidation = ValidationHelper.ValidateCallbackUrl(callbackUrl);
            if (!callbackValidation.IsValid)
                throw new ValidationException($"Invalid callback_url: {callbackValidation.ErrorMessage}");

            return new LaunchGameRequest
            {
                player_unique_id = playerUniqueId,
                player_name = playerName,
                player_currency = playerCurrency,
                game_id = gameId,
                is_demo = isDemo,
                language = language,
                callback_url = callbackUrl,
                orientation = orientation
            };
        }

        /// <summary>
        /// Create a CreateMemberRequest with common parameters
        /// </summary>
        public static CreateMemberRequest CreateMemberRequest(
            string playerUniqueId,
            string playerName,
            string playerCurrency)
        {
            // Validate parameters before creating request
            var playerIdValidation = ValidationHelper.ValidatePlayerUniqueId(playerUniqueId);
            if (!playerIdValidation.IsValid)
                throw new ValidationException($"Invalid player_unique_id: {playerIdValidation.ErrorMessage}");

            var playerNameValidation = ValidationHelper.ValidatePlayerName(playerName);
            if (!playerNameValidation.IsValid)
                throw new ValidationException($"Invalid player_name: {playerNameValidation.ErrorMessage}");

            var currencyValidation = ValidationHelper.ValidatePlayerCurrency(playerCurrency);
            if (!currencyValidation.IsValid)
                throw new ValidationException($"Invalid player_currency: {currencyValidation.ErrorMessage}");

            return new CreateMemberRequest
            {
                player_unique_id = playerUniqueId,
                player_name = playerName,
                player_currency = playerCurrency
            };
        }

        /// <summary>
        /// Create a WalletDepositRequest with common parameters
        /// </summary>
        public static WalletDepositRequest CreateDepositRequest(
            string playerUniqueId,
            string playerCurrency,
            string transferId,
            decimal amount)
        {
            // Validate parameters before creating request
            var playerIdValidation = ValidationHelper.ValidatePlayerUniqueId(playerUniqueId);
            if (!playerIdValidation.IsValid)
                throw new ValidationException($"Invalid player_unique_id: {playerIdValidation.ErrorMessage}");

            var currencyValidation = ValidationHelper.ValidatePlayerCurrency(playerCurrency);
            if (!currencyValidation.IsValid)
                throw new ValidationException($"Invalid player_currency: {currencyValidation.ErrorMessage}");

            var transferIdValidation = ValidationHelper.ValidateTransferId(transferId);
            if (!transferIdValidation.IsValid)
                throw new ValidationException($"Invalid transfer_id: {transferIdValidation.ErrorMessage}");

            var amountValidation = ValidationHelper.ValidateAmount(amount);
            if (!amountValidation.IsValid)
                throw new ValidationException($"Invalid amount: {amountValidation.ErrorMessage}");

            return new WalletDepositRequest
            {
                player_unique_id = playerUniqueId,
                player_currency = playerCurrency,
                transfer_id = transferId,
                amount = amount
            };
        }

        /// <summary>
        /// Create a WalletWithdrawRequest with common parameters
        /// </summary>
        public static WalletWithdrawRequest CreateWithdrawRequest(
            string playerUniqueId,
            string playerCurrency,
            string transferId,
            decimal amount)
        {
            // Validate parameters before creating request
            var playerIdValidation = ValidationHelper.ValidatePlayerUniqueId(playerUniqueId);
            if (!playerIdValidation.IsValid)
                throw new ValidationException($"Invalid player_unique_id: {playerIdValidation.ErrorMessage}");

            var currencyValidation = ValidationHelper.ValidatePlayerCurrency(playerCurrency);
            if (!currencyValidation.IsValid)
                throw new ValidationException($"Invalid player_currency: {currencyValidation.ErrorMessage}");

            var transferIdValidation = ValidationHelper.ValidateTransferId(transferId);
            if (!transferIdValidation.IsValid)
                throw new ValidationException($"Invalid transfer_id: {transferIdValidation.ErrorMessage}");

            var amountValidation = ValidationHelper.ValidateAmount(amount);
            if (!amountValidation.IsValid)
                throw new ValidationException($"Invalid amount: {amountValidation.ErrorMessage}");

            return new WalletWithdrawRequest
            {
                player_unique_id = playerUniqueId,
                player_currency = playerCurrency,
                transfer_id = transferId,
                amount = amount
            };
        }

        /// <summary>
        /// Create a DailyReportRequest with date range validation
        /// </summary>
        public static DailyReportRequest CreateDailyReportRequest(
            string dateFrom,
            string dateTo,
            string agentCode = null)
        {
            // Validate date format (expecting YYYY-MM-DD)
            if (string.IsNullOrEmpty(dateFrom))
                throw new ArgumentException("date_from is required", nameof(dateFrom));

            if (string.IsNullOrEmpty(dateTo))
                throw new ArgumentException("date_to is required", nameof(dateTo));

            if (!DateTime.TryParse(dateFrom, out DateTime fromDate))
                throw new ArgumentException("date_from must be in valid date format (YYYY-MM-DD)", nameof(dateFrom));

            if (!DateTime.TryParse(dateTo, out DateTime toDate))
                throw new ArgumentException("date_to must be in valid date format (YYYY-MM-DD)", nameof(dateTo));

            if (fromDate > toDate)
                throw new ArgumentException("date_from cannot be later than date_to");

            // Check for 7-day limit as mentioned in the API documentation
            if ((toDate - fromDate).TotalDays > 7)
                throw new ArgumentException("Date range cannot exceed 7 days");

            return new DailyReportRequest
            {
                date_from = dateFrom,
                date_to = dateTo,
                agent_code = agentCode
            };
        }

        /// <summary>
        /// Create a CancelledTransactionsRequest with date range validation
        /// </summary>
        public static CancelledTransactionsRequest CreateCancelledTransactionsRequest(
            string cancelledAtFrom,
            string cancelledAtTo,
            int? perPage = null,
            int? page = null,
            string agentCode = null,
            string transactionId = null,
            string gameRoundId = null)
        {
            // Validate date format (expecting YYYY-MM-DD HH:mm:ss)
            if (string.IsNullOrEmpty(cancelledAtFrom))
                throw new ArgumentException("cancelled_at_from is required", nameof(cancelledAtFrom));

            if (string.IsNullOrEmpty(cancelledAtTo))
                throw new ArgumentException("cancelled_at_to is required", nameof(cancelledAtTo));

            if (!DateTime.TryParse(cancelledAtFrom, out DateTime fromDate))
                throw new ArgumentException("cancelled_at_from must be in valid datetime format (YYYY-MM-DD HH:mm:ss)", nameof(cancelledAtFrom));

            if (!DateTime.TryParse(cancelledAtTo, out DateTime toDate))
                throw new ArgumentException("cancelled_at_to must be in valid datetime format (YYYY-MM-DD HH:mm:ss)", nameof(cancelledAtTo));

            if (fromDate > toDate)
                throw new ArgumentException("cancelled_at_from cannot be later than cancelled_at_to");

            // Check for 24-hour limit as mentioned in the API documentation
            if ((toDate - fromDate).TotalHours > 24)
                throw new ArgumentException("Date range cannot exceed 24 hours");

            // Validate pagination parameters
            if (perPage.HasValue && perPage.Value <= 0)
                throw new ArgumentException("perPage must be greater than 0", nameof(perPage));

            if (page.HasValue && page.Value <= 0)
                throw new ArgumentException("page must be greater than 0", nameof(page));

            return new CancelledTransactionsRequest
            {
                cancelled_at_from = cancelledAtFrom,
                cancelled_at_to = cancelledAtTo,
                perPage = perPage,
                page = page,
                agent_code = agentCode,
                transaction_id = transactionId,
                game_round_id = gameRoundId
            };
        }

        /// <summary>
        /// Create a TransactionDetailsGetRequest with date range validation
        /// </summary>
        public static TransactionDetailsGetRequest CreateTransactionDetailsRequest(
            string updatedAtFrom = null,
            string updatedAtTo = null,
            string createdAtFrom = null,
            string createdAtTo = null,
            int? perPage = null,
            int? page = null,
            string agentCode = null,
            string transactionId = null,
            string gameRoundId = null)
        {
            // At least one date range must be provided
            if (string.IsNullOrEmpty(updatedAtFrom) && string.IsNullOrEmpty(createdAtFrom))
                throw new ArgumentException("Either updated_at_from or created_at_from must be provided");

            if (string.IsNullOrEmpty(updatedAtTo) && string.IsNullOrEmpty(createdAtTo))
                throw new ArgumentException("Either updated_at_to or created_at_to must be provided");

            // Validate updated_at date range if provided
            if (!string.IsNullOrEmpty(updatedAtFrom) && !string.IsNullOrEmpty(updatedAtTo))
            {
                if (!DateTime.TryParse(updatedAtFrom, out DateTime updatedFromDate))
                    throw new ArgumentException("updated_at_from must be in valid datetime format (YYYY-MM-DD HH:mm:ss)", nameof(updatedAtFrom));

                if (!DateTime.TryParse(updatedAtTo, out DateTime updatedToDate))
                    throw new ArgumentException("updated_at_to must be in valid datetime format (YYYY-MM-DD HH:mm:ss)", nameof(updatedAtTo));

                if (updatedFromDate > updatedToDate)
                    throw new ArgumentException("updated_at_from cannot be later than updated_at_to");

                // Check for 30-minute limit as mentioned in the API documentation
                if ((updatedToDate - updatedFromDate).TotalMinutes > 30)
                    throw new ArgumentException("Updated date range cannot exceed 30 minutes");
            }

            // Validate created_at date range if provided
            if (!string.IsNullOrEmpty(createdAtFrom) && !string.IsNullOrEmpty(createdAtTo))
            {
                if (!DateTime.TryParse(createdAtFrom, out DateTime createdFromDate))
                    throw new ArgumentException("created_at_from must be in valid datetime format (YYYY-MM-DD HH:mm:ss)", nameof(createdAtFrom));

                if (!DateTime.TryParse(createdAtTo, out DateTime createdToDate))
                    throw new ArgumentException("created_at_to must be in valid datetime format (YYYY-MM-DD HH:mm:ss)", nameof(createdAtTo));

                if (createdFromDate > createdToDate)
                    throw new ArgumentException("created_at_from cannot be later than created_at_to");

                // Check for 30-minute limit as mentioned in the API documentation
                if ((createdToDate - createdFromDate).TotalMinutes > 30)
                    throw new ArgumentException("Created date range cannot exceed 30 minutes");
            }

            // Validate pagination parameters
            if (perPage.HasValue && perPage.Value <= 0)
                throw new ArgumentException("perPage must be greater than 0", nameof(perPage));

            if (page.HasValue && page.Value <= 0)
                throw new ArgumentException("page must be greater than 0", nameof(page));

            return new TransactionDetailsGetRequest
            {
                updated_at_from = updatedAtFrom,
                updated_at_to = updatedAtTo,
                created_at_from = createdAtFrom,
                created_at_to = createdAtTo,
                perPage = perPage,
                page = page,
                agent_code = agentCode,
                transaction_id = transactionId,
                game_round_id = gameRoundId
            };
        }
        #endregion

        #region Synchronous Methods (following Pragmatic pattern)

        /// <summary>
        /// Launch a game for a player (synchronous)
        /// </summary>
        public bool LaunchGame(string playerUniqueId, string playerName, string playerCurrency, string gameId,
            bool isDemo = false, string language = "en-us", string callbackUrl = null, string orientation = null)
        {
            try
            {
                GameOrientation? gameOrientation = null;
                if (!string.IsNullOrEmpty(orientation))
                {
                    if (Enum.TryParse<GameOrientation>(orientation, true, out GameOrientation parsedOrientation))
                    {
                        gameOrientation = parsedOrientation;
                    }
                }

                var request = new LaunchGameRequest
                {
                    player_unique_id = playerUniqueId,
                    player_name = playerName,
                    player_currency = playerCurrency,
                    game_id = gameId,
                    is_demo = isDemo,
                    language = language,
                    callback_url = "https://example.com",
                    orientation = gameOrientation.Value
                };

                var response = Task.Run(() => LaunchGameAsync(request)).Result;
                Response = response.ToString();

                if (response != null && response.code == 0)
                {
                    LaunchGameResponse = response.data;
                    return true;
                }
                else
                {
                    ErrorMessage = response?.message ?? "Unknown error";
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.InnerException.Message;
                return false;
            }
        }

        /// <summary>
        /// Create a new member (synchronous)
        /// </summary>
        public bool CreateMember(string playerUniqueId, string playerName, string playerCurrency)
        {
            try
            {
                var request = new CreateMemberRequest
                {
                    player_unique_id = playerUniqueId,
                    player_name = playerName,
                    player_currency = playerCurrency
                };

                var response = Task.Run(() => CreateMemberAsync(request)).Result;
                Response = response.ToString();

                if (response != null && response.code == 0)
                {
                    CreateMemberResponse = response.data;
                    return true;
                }
                else
                {
                    ErrorMessage = response?.message ?? "Unknown error";
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.InnerException.Message;
                return false;
            }
        }

        /// <summary>
        /// Get member details (synchronous)
        /// </summary>
        public bool GetMemberDetails(string playerUniqueId, string playerCurrency)
        {
            try
            {
                var request = new MemberDetailsRequest
                {
                    player_unique_id = playerUniqueId,
                    player_currency = playerCurrency
                };

                var response = Task.Run(() => GetMemberDetailsAsync(request)).Result;
                Response = response.ToString();

                if (response != null && response.code == 0)
                {
                    PlayerResponse = response.data;
                    return true;
                }
                else
                {
                    ErrorMessage = response?.message ?? "Unknown error";
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.InnerException.Message;
                return false;
            }
        }

        /// <summary>
        /// Deposit funds to player wallet (synchronous)
        /// </summary>
        public bool Deposit(string playerUniqueId, string playerCurrency, string transferId, decimal amount)
        {
            try
            {
                var request = new WalletDepositRequest
                {
                    player_unique_id = playerUniqueId,
                    player_currency = playerCurrency,
                    transfer_id = transferId,
                    amount = amount
                };

                var response = Task.Run(() => DepositAsync(request)).Result;
                Response = response.ToString();

                if (response != null && response.code == 0)
                {
                    WalletBalanceResponse = response.data;
                    return true;
                }
                else
                {
                    ErrorMessage = response?.message ?? "Unknown error";
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.InnerException.Message;
                return false;
            }
        }

        /// <summary>
        /// Withdraw funds from player wallet (synchronous)
        /// </summary>
        public bool Withdraw(string playerUniqueId, string playerCurrency, string transferId, decimal amount)
        {
            try
            {
                var request = new WalletWithdrawRequest
                {
                    player_unique_id = playerUniqueId,
                    player_currency = playerCurrency,
                    transfer_id = transferId,
                    amount = amount
                };

                var response = Task.Run(() => WithdrawAsync(request)).Result;
                Response = response.ToString();

                if (response != null && response.code == 0)
                {
                    WalletBalanceResponse = response.data;
                    return true;
                }
                else
                {
                    ErrorMessage = response?.message ?? "Unknown error";
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.InnerException.Message;
                return false;
            }
        }

        public bool Logout(string playerUniqueId, string playerCurrency, string agentCode)
        {
            try
            {
                var request = new MemberLogoutRequest
                {
                    player_unique_id = playerUniqueId,
                    player_currency = playerCurrency,
                    agent_code = agentCode
                };

                var response = Task.Run(() => LogoutMemberAsync(request)).Result;
                Response = response.ToString();

                if (response != null && response.code == 0)
                {
                    LogoutPlayerResponse = response.data;
                    return true;
                }
                else
                {
                    ErrorMessage = response?.message ?? "Unknown error";
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.InnerException.Message;
                return false;
            }
        }

        /// <summary>
        /// Get list of available games (synchronous)
        /// </summary>
        public bool GetGameList()
        {
            try
            {
                var response = Task.Run(() => GetGameListAsync()).Result;
                Response = response.ToString();

                if (response != null && response.code == 0)
                {
                    GameListResponse = response.data;
                    return true;
                }
                else
                {
                    ErrorMessage = response?.message ?? "Unknown error";
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.InnerException.Message;
                return false;
            }
        }

        public bool GetLanguage()
        {
            try
            {
                var response = Task.Run(() => GetSupportedLanguagesAsync()).Result;
                Response = response.ToString();

                if (response != null && response.code == 0)
                {
                    SupportedLanguagesResponse = response.data;
                    return true;
                }
                else
                {
                    ErrorMessage = response?.message ?? "Unknown error";
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.InnerException.Message;
                return false;
            }
        }

        /// <summary>
        /// Get transaction details (synchronous)
        /// </summary>
        public bool GetTransactionDetails(string updatedAtFrom, string updatedAtTo, int? perPage = null, int? page = null)
        {
            try
            {
                var request = new TransactionDetailsGetRequest
                {
                    updated_at_from = updatedAtFrom,
                    updated_at_to = updatedAtTo,
                    perPage = perPage,
                    page = page
                };

                var response = Task.Run(() => GetTransactionDetailsAsync(request)).Result;
                Response = response.ToString();

                if (response != null && response.code == 0)
                {
                    // Note: The response structure is different for this endpoint
                    // TransactionListResponse = response.data; // This would need to be adapted
                    return true;
                }
                else
                {
                    ErrorMessage = response?.message ?? "Unknown error";
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.InnerException.Message;
                return false;
            }
        }

        #endregion

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}