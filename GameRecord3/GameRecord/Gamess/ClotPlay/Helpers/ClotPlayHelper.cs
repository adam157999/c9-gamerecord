using GameRecord.Gamess.ClotPlay.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GameRecord.Gamess.ClotPlay.Helpers
{
    /// <summary>
    /// Validation helper class for ClotPlay SDK based on API documentation
    /// </summary>
    public static class ValidationHelper
    {
        // Supported currencies from API documentation
        private static readonly HashSet<string> SupportedCurrencies = new HashSet<string>
        {
            "USD", "MYR", "PHP", "CNY", "JPY", "THB", "IDR", "IDR2", "VND", "VND2",
            "KHR", "KHR2", "SGD", "INR", "INR2", "BND", "TWD", "EUR", "GBP", "ZAR",
            "VEF", "UAH", "TRY", "SEK", "RUB", "RON", "PLN", "PEN", "NZD", "NOK",
            "NGN", "MXN", "MOP", "MAD", "KRW", "ISK", "HUF", "HRK", "HKD", "GEL",
            "DKK", "CZK", "CLP", "CHF", "CAD", "BRL", "BGN", "AUD", "ARS", "COP",
            "UBC", "MBC", "SCR"
        };

        // Supported languages from API documentation
        private static readonly HashSet<string> SupportedLanguages = new HashSet<string>
        {
            "en", "zh-cn", "th", "vn", "jp", "ind", "hi", "en-us"
        };

        // Valid orientation values (case-insensitive)
        private static readonly HashSet<string> ValidOrientations = new HashSet<string>
        {
            "portrait", "landscape"
        };

        // Valid transaction action values (case-insensitive)
        private static readonly HashSet<string> ValidTransactionActions = new HashSet<string>
        {
            "deposit", "withdraw", "adjust"
        };

        /// <summary>
        /// Validates operator token - must be exactly 16 characters
        /// </summary>
        public static ValidationResult ValidateOperatorToken(string operatorToken)
        {
            if (string.IsNullOrEmpty(operatorToken))
                return ValidationResult.Error("Operator token is required");

            if (operatorToken.Length != 16)
                return ValidationResult.Error("Operator token must be exactly 16 characters");

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates player unique ID - limited to 24 characters maximum, without any prefix
        /// </summary>
        public static ValidationResult ValidatePlayerUniqueId(string playerUniqueId)
        {
            if (string.IsNullOrEmpty(playerUniqueId))
                return ValidationResult.Error("player_unique_id is required");

            if (playerUniqueId.Length > 24)
                return ValidationResult.Error("player_unique_id must be limited to 24 characters maximum");

            // Check for common prefixes that should be avoided
            if (playerUniqueId.StartsWith("op_") || playerUniqueId.StartsWith("usr_") ||
                playerUniqueId.StartsWith("player_") || playerUniqueId.Contains("prefix"))
                return ValidationResult.Warning("player_unique_id should not contain prefixes");

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates player name - required field for display purposes
        /// </summary>
        public static ValidationResult ValidatePlayerName(string playerName)
        {
            if (string.IsNullOrEmpty(playerName))
                return ValidationResult.Error("player_name is required");

            if (playerName.Length > 100)
                return ValidationResult.Error("player_name is too long (maximum 100 characters)");

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates player currency - must be from supported currencies list
        /// </summary>
        public static ValidationResult ValidatePlayerCurrency(string playerCurrency)
        {
            if (string.IsNullOrEmpty(playerCurrency))
                return ValidationResult.Error("player_currency is required");

            if (!SupportedCurrencies.Contains(playerCurrency.ToUpper()))
                return ValidationResult.Error($"Currency '{playerCurrency}' is not supported. Please refer to supported currencies list from /MOGI/currencies endpoint");

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates game ID - required field, should be valid game slug
        /// </summary>
        public static ValidationResult ValidateGameId(string gameId)
        {
            if (string.IsNullOrEmpty(gameId))
                return ValidationResult.Error("game_id is required");

            // Game ID should be a valid slug format (lowercase, hyphens allowed)
            if (!Regex.IsMatch(gameId, @"^[a-z0-9]+(?:-[a-z0-9]+)*$"))
                return ValidationResult.Error("game_id must be a valid slug format (lowercase letters, numbers, and hyphens only)");

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates language code - optional field, must be from supported languages
        /// </summary>
        public static ValidationResult ValidateLanguage(string language)
        {
            if (string.IsNullOrEmpty(language))
                return ValidationResult.Success(); // Language is optional

            if (!SupportedLanguages.Contains(language.ToLower()))
                return ValidationResult.Error($"Language '{language}' is not supported. Supported languages: {string.Join(", ", SupportedLanguages)}");

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates orientation - optional field, valid values are "portrait" and "landscape"
        /// Any other value will result in error code 1000
        /// </summary>
        public static ValidationResult ValidateOrientation(string orientation)
        {
            if (string.IsNullOrEmpty(orientation))
                return ValidationResult.Success(); // Orientation is optional

            if (!ValidOrientations.Contains(orientation.ToLower()))
                return ValidationResult.Error($"Orientation '{orientation}' is not valid. Valid values are: portrait, landscape. Any other value will result in error code 1000");

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates callback URL - optional field, must be a valid URL
        /// </summary>
        public static ValidationResult ValidateCallbackUrl(string callbackUrl)
        {
            if (string.IsNullOrEmpty(callbackUrl))
                return ValidationResult.Success(); // Callback URL is optional

            if (!Uri.TryCreate(callbackUrl, UriKind.Absolute, out Uri uri))
                return ValidationResult.Error("callback_url must be a valid URL");

            if (uri.Scheme != "http" && uri.Scheme != "https")
                return ValidationResult.Error("callback_url must use HTTP or HTTPS protocol");

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates transfer ID - required field for wallet operations, should be unique
        /// </summary>
        public static ValidationResult ValidateTransferId(string transferId)
        {
            if (string.IsNullOrEmpty(transferId))
                return ValidationResult.Error("transfer_id is required");

            if (transferId.Length > 50)
                return ValidationResult.Error("transfer_id is too long (maximum 50 characters)");

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates amount for wallet operations - negative values not allowed, minimum 0.01
        /// </summary>
        public static ValidationResult ValidateAmount(decimal amount)
        {
            if (amount < 0)
                return ValidationResult.Error("Amount field: negative values are not allowed");

            if (amount < 0.01m)
                return ValidationResult.Error("Amount field: minimum amount that can be passed is 0.01");

            if (amount > 999999999)
                return ValidationResult.Error("Amount is too large");

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates agent code - optional field for operators with agent system
        /// </summary>
        public static ValidationResult ValidateAgentCode(string agentCode)
        {
            if (string.IsNullOrEmpty(agentCode))
                return ValidationResult.Success(); // Agent code is optional

            if (agentCode.Length > 50)
                return ValidationResult.Error("agent_code is too long (maximum 50 characters)");

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates date format for reports - must be YYYY-MM-DD format
        /// </summary>
        public static ValidationResult ValidateDate(string date)
        {
            if (string.IsNullOrEmpty(date))
                return ValidationResult.Error("Date is required");

            if (!DateTime.TryParseExact(date, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
                return ValidationResult.Error("Date must be in YYYY-MM-DD format");

            if (parsedDate > DateTime.Now.Date)
                return ValidationResult.Error("Date cannot be in the future");

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates datetime format for transaction queries
        /// </summary>
        public static ValidationResult ValidateDateTime(string dateTime)
        {
            if (string.IsNullOrEmpty(dateTime))
                return ValidationResult.Error("DateTime is required");

            if (!DateTime.TryParseExact(dateTime, "yyyy-MM-dd HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out DateTime parsedDateTime))
                return ValidationResult.Error("DateTime must be in YYYY-MM-DD HH:mm:ss format");

            if (parsedDateTime > DateTime.Now)
                return ValidationResult.Error("DateTime cannot be in the future");

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates pagination parameters
        /// </summary>
        public static ValidationResult ValidatePagination(int page, int perPage)
        {
            if (page < 1)
                return ValidationResult.Error("Page number must be greater than 0");

            if (perPage < 1)
                return ValidationResult.Error("Per page count must be greater than 0");

            if (perPage > 1000)
                return ValidationResult.Error("Per page count is too large (maximum 1000)");

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates bet ID for bet details
        /// </summary>
        public static ValidationResult ValidateBetId(string betId)
        {
            if (string.IsNullOrEmpty(betId))
                return ValidationResult.Error("bet_id is required");

            if (!long.TryParse(betId, out long _))
                return ValidationResult.Error("bet_id must be a valid numeric value");

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates transaction ID
        /// </summary>
        public static ValidationResult ValidateTransactionId(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId))
                return ValidationResult.Success(); // Optional field

            if (!long.TryParse(transactionId, out long _))
                return ValidationResult.Error("transaction_id must be a valid numeric value");

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates game round ID
        /// </summary>
        public static ValidationResult ValidateGameRoundId(string gameRoundId)
        {
            if (string.IsNullOrEmpty(gameRoundId))
                return ValidationResult.Success(); // Optional field

            if (gameRoundId.Length > 50)
                return ValidationResult.Error("game_round_id is too long (maximum 50 characters)");

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates transaction action - must be one of the supported transaction types
        /// </summary>
        public static ValidationResult ValidateTransactionAction(string action)
        {
            if (string.IsNullOrEmpty(action))
                return ValidationResult.Error("Transaction action is required");

            if (!ValidTransactionActions.Contains(action.ToLower()))
                return ValidationResult.Error($"Transaction action '{action}' is not valid. Valid values are: {string.Join(", ", ValidTransactionActions)}");

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates date range for reports (maximum 7 days for daily reports, 30 minutes for transaction details)
        /// </summary>
        public static ValidationResult ValidateDateRange(DateTime fromDate, DateTime toDate, bool isDailyReport = false)
        {
            if (fromDate > toDate)
                return ValidationResult.Error("From date cannot be later than to date");

            var timeSpan = toDate - fromDate;

            if (isDailyReport)
            {
                if (timeSpan.TotalDays > 7)
                    return ValidationResult.Error("Daily report: maximum 7 days search range limit");
            }
            else
            {
                if (timeSpan.TotalMinutes > 30)
                    return ValidationResult.Error("Transaction details: maximum 30 minutes search range limit");
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates LaunchGameRequest
        /// </summary>
        public static ValidationResult ValidateLaunchGameRequest(LaunchGameRequest request)
        {
            var results = new List<ValidationResult>
            {
                ValidatePlayerUniqueId(request.player_unique_id),
                ValidatePlayerName(request.player_name),
                ValidatePlayerCurrency(request.player_currency),
                ValidateGameId(request.game_id),
                ValidateLanguage(request.language),
                ValidateCallbackUrl(request.callback_url),
                ValidateOrientation(request.orientation?.ToString()),
                ValidateAgentCode(request.agent_code)
            };

            var errors = results.Where(r => !r.IsValid).ToList();
            if (errors.Any())
                return ValidationResult.Error(string.Join("; ", errors.Select(e => e.ErrorMessage)));

            var warnings = results.Where(r => r.IsValid && r.IsWarning).ToList();
            if (warnings.Any())
                return ValidationResult.Warning(string.Join("; ", warnings.Select(w => w.ErrorMessage)));

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates CreateMemberRequest
        /// </summary>
        public static ValidationResult ValidateCreateMemberRequest(CreateMemberRequest request)
        {
            var results = new List<ValidationResult>
            {
                ValidatePlayerUniqueId(request.player_unique_id),
                ValidatePlayerName(request.player_name),
                ValidatePlayerCurrency(request.player_currency),
                ValidateAgentCode(request.agent_code)
            };

            var errors = results.Where(r => !r.IsValid).ToList();
            if (errors.Any())
                return ValidationResult.Error(string.Join("; ", errors.Select(e => e.ErrorMessage)));

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates WalletDepositRequest
        /// </summary>
        public static ValidationResult ValidateWalletDepositRequest(WalletDepositRequest request)
        {
            var results = new List<ValidationResult>
            {
                ValidatePlayerUniqueId(request.player_unique_id),
                ValidatePlayerCurrency(request.player_currency),
                ValidateTransferId(request.transfer_id),
                ValidateAmount(request.amount),
                ValidateAgentCode(request.agent_code)
            };

            var errors = results.Where(r => !r.IsValid).ToList();
            if (errors.Any())
                return ValidationResult.Error(string.Join("; ", errors.Select(e => e.ErrorMessage)));

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates WalletWithdrawRequest
        /// </summary>
        public static ValidationResult ValidateWalletWithdrawRequest(WalletWithdrawRequest request)
        {
            var results = new List<ValidationResult>
            {
                ValidatePlayerUniqueId(request.player_unique_id),
                ValidatePlayerCurrency(request.player_currency),
                ValidateTransferId(request.transfer_id),
                ValidateAmount(request.amount),
                ValidateAgentCode(request.agent_code)
            };

            var errors = results.Where(r => !r.IsValid).ToList();
            if (errors.Any())
                return ValidationResult.Error(string.Join("; ", errors.Select(e => e.ErrorMessage)));

            return ValidationResult.Success();
        }
    }

    /// <summary>
    /// Validation result class
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; private set; }
        public string ErrorMessage { get; private set; }
        public bool IsWarning { get; private set; }

        private ValidationResult(bool isValid, string errorMessage, bool isWarning = false)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
            IsWarning = isWarning;
        }

        public static ValidationResult Success() => new ValidationResult(true, null);
        public static ValidationResult Error(string message) => new ValidationResult(false, message);
        public static ValidationResult Warning(string message) => new ValidationResult(true, message, true);

        public override string ToString()
        {
            if (IsValid && !IsWarning) return "Valid";
            if (IsWarning) return $"Warning: {ErrorMessage}";
            return $"Error: {ErrorMessage}";
        }
    }

    /// <summary>
    /// Generic validation result class that can return a value
    /// </summary>
    /// <typeparam name="T">The type of the value to return</typeparam>
    public class ValidationResult<T>
    {
        public bool IsValid { get; private set; }
        public string ErrorMessage { get; private set; }
        public bool IsWarning { get; private set; }
        public T Value { get; private set; }

        private ValidationResult(bool isValid, string errorMessage, T value = default(T), bool isWarning = false)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
            Value = value;
            IsWarning = isWarning;
        }

        public static ValidationResult<T> Success(T value) => new ValidationResult<T>(true, null, value);
        public static ValidationResult<T> Error(string message) => new ValidationResult<T>(false, message);
        public static ValidationResult<T> Warning(string message, T value) => new ValidationResult<T>(true, message, value, true);

        public override string ToString()
        {
            if (IsValid && !IsWarning) return $"Valid: {Value}";
            if (IsWarning) return $"Warning: {ErrorMessage}, Value: {Value}";
            return $"Error: {ErrorMessage}";
        }
    }

    /// <summary>
    /// Custom exception for validation errors
    /// </summary>
    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
        public ValidationException(string message, Exception innerException) : base(message, innerException) { }
    }
}