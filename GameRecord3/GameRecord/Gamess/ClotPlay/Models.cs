using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ClotPlay
{
    // Enums
    public enum GameOrientation
    {
        portrait,
        landscape
    }

    // Base API Response
    public class ApiResponse<T>
    {
        public int code { get; set; }
        public T data { get; set; }
        public string message { get; set; }
        public object debug { get; set; }
        public PaginationResponse pagination { get; set; } // Optional - only present for paginated endpoints
    }

    // Player/Member Models
    public class Player
    {
        public int id { get; set; }
        public string name { get; set; }
        public decimal balance { get; set; }
        public string created_at { get; set; }
        public string currency { get; set; }
        public decimal demo_balance { get; set; }
        public bool isDemo { get; set; }
        public bool isSuspended { get; set; }
        public string token { get; set; }
        public string operator_name { get; set; }
        public string operator_member_token { get; set; }
        public string agent_code { get; set; }
        public string agent_name { get; set; }
        public bool is_free_spins_settle { get; set; }
    }

    // Request Models
    public class LaunchGameRequest
    {
        public string player_unique_id { get; set; }
        public string player_name { get; set; }
        public string player_currency { get; set; }
        public string game_id { get; set; }
        public bool is_demo { get; set; }
        public string language { get; set; }
        public string callback_url { get; set; }

        [JsonConverter(typeof(StringEnumConverter), true)]
        [JsonProperty("orientation")]
        public GameOrientation? orientation { get; set; }

        public string agent_code { get; set; }
    }

    public class CreateMemberRequest
    {
        public string player_unique_id { get; set; }
        public string player_name { get; set; }
        public string player_currency { get; set; }
        public string agent_code { get; set; }
    }

    public class MemberDetailsRequest
    {
        public string player_unique_id { get; set; }
        public string player_currency { get; set; }
        public string agent_code { get; set; }
    }

    public class MemberLogoutRequest
    {
        public string player_unique_id { get; set; }
        public string player_currency { get; set; }
        public string agent_code { get; set; }
    }

    public class WalletDepositRequest
    {
        public string player_unique_id { get; set; }
        public string player_currency { get; set; }
        public string transfer_id { get; set; }
        public decimal amount { get; set; }
        public string agent_code { get; set; }
    }

    public class WalletWithdrawRequest
    {
        public string player_unique_id { get; set; }
        public string player_currency { get; set; }
        public string transfer_id { get; set; }
        public decimal amount { get; set; }
        public string agent_code { get; set; }
    }

    public class WalletAdjustRequest
    {
        public string player_unique_id { get; set; }
        public string player_currency { get; set; }
        public string transfer_id { get; set; }
        public decimal amount { get; set; }
        public string agent_code { get; set; }
    }

    public class TransactionDetailsRequest
    {
        public string transfer_id { get; set; }
        public string agent_code { get; set; }
    }

    // New model for GET transaction details endpoint
    public class TransactionDetailsGetRequest
    {
        public string updated_at_from { get; set; }
        public string updated_at_to { get; set; }
        public string created_at_from { get; set; }
        public string created_at_to { get; set; }
        public int? perPage { get; set; }
        public int? page { get; set; }
        public string agent_code { get; set; }
        public string transaction_id { get; set; }
        public string game_round_id { get; set; }
    }

    public class BetDetailsRequest
    {
        public string bet_id { get; set; }
    }

    public class DailyReportRequest
    {
        public string date_from { get; set; }
        public string date_to { get; set; }
        public string agent_code { get; set; }
    }

    public class CancelledTransactionsRequest
    {
        public string cancelled_at_from { get; set; }
        public string cancelled_at_to { get; set; }
        public int? perPage { get; set; }
        public int? page { get; set; }
        public string agent_code { get; set; }
        public string transaction_id { get; set; }
        public string game_round_id { get; set; }
    }

    // Response Models
    public class LaunchGameResponse
    {
        public string game_url { get; set; }
        public Player player { get; set; }
    }

    public class CreateMemberResponse
    {
        public Player player { get; set; }
    }

    public class WalletBalanceResponse
    {
        public string balance { get; set; }
    }

    public class WalletTransactionResponse
    {
        public int status { get; set; }
        public int id { get; set; }
        public string transfer_id { get; set; }
        public string created_at { get; set; }
        public string name { get; set; }
        public string currency { get; set; }
        public decimal balance_before { get; set; }
        public decimal amount { get; set; }
        public decimal balance { get; set; }
        public string action { get; set; }
        public string operator_name { get; set; }
        public string player_unique_id { get; set; }
        public string operator_member_token { get; set; }
        public string agent_code { get; set; }
        public string agent_name { get; set; }
    }

    public class Game
    {
        public string category { get; set; }
        public string created_at { get; set; }
        public string description { get; set; }
        public int isMaintenance { get; set; }
        public string name { get; set; }
        public string name_cn { get; set; }
        public string slug { get; set; }
        public string updated_at { get; set; }
        public int isNew { get; set; }
        public int isPopular { get; set; }
        public Dictionary<string, GameThumbnails> thumbnails { get; set; }
    }

    public class GameThumbnails
    {
        public string portrait { get; set; }
        public string square { get; set; }
    }

    public class BetDetailsResponse
    {
        public string history_url { get; set; }
    }

    public class TransactionListResponse
    {
        public List<WalletTransactionResponse> transactions { get; set; }
    }

    // Note: GetTransactionDetailsResponse is no longer needed
    // We now use ApiResponse<List<GameTransaction>> with the pagination property

    // Game transaction model based on API documentation
    public class GameTransaction
    {
        public int id { get; set; }
        public string operator_name { get; set; }
        public string agent_name { get; set; }
        public string agent_code { get; set; }
        public string player_unique_id { get; set; }
        public int game_id { get; set; }
        public string game_slug { get; set; }
        public decimal bet_amount { get; set; }
        public int multiplier { get; set; }
        public int bet_line { get; set; }
        public decimal total_bet_amount { get; set; }
        public bool isAutoPlay { get; set; }
        public bool isBonusTransaction { get; set; }
        public bool is100Spin { get; set; }
        public bool isFreeSpin { get; set; }
        public bool isFreeBet { get; set; }
        public string currency { get; set; }
        public string currency_symbol { get; set; }
        public decimal balance_before { get; set; }
        public decimal winning { get; set; }
        public decimal balance_after { get; set; }
        public object result { get; set; }
        public object bonusDetails { get; set; }
        public decimal current_turnover { get; set; }
        public bool isVirtualTransaction { get; set; }
        public bool awardedInGame { get; set; }
        public bool isBuy { get; set; }
        public string created_at { get; set; }
        public string updated_at { get; set; }
        public object extra { get; set; }
        public bool isDemo { get; set; }
        public string game_round_id { get; set; }
    }

    public class CancelledTransactionsResponse
    {
        public List<CancelledTransactionItem> data { get; set; }
        public PaginationResponse pagination { get; set; }
    }

    public class CancelledTransactionItem
    {
        public int id { get; set; }
        public string bet_amount { get; set; }
        public int multiplier { get; set; }
        public int bet_line { get; set; }
        public string total_bet_amount { get; set; }
        public int isAutoPlay { get; set; }
        public object isBonusTransaction { get; set; }
        public object is100Spin { get; set; }
        public int isFreeSpin { get; set; }
        public int isFreeBet { get; set; }
        public string currency { get; set; }
        public string balance_before { get; set; }
        public string winning { get; set; }
        public string balance_after { get; set; }
        public object result { get; set; }
        public object bonusDetails { get; set; }
        public string current_turnover { get; set; }
        public bool isVirtualTransaction { get; set; }
        public int awardedInGame { get; set; }
        public string created_at { get; set; }
        public string updated_at { get; set; }
        public string cancelled_at { get; set; }
        public object extra { get; set; }
        public int isDemo { get; set; }
        public string game_round_id { get; set; }
        public string operator_name { get; set; }
        public string agent_name { get; set; }
        public string agent_code { get; set; }
        public string player_unique_id { get; set; }
        public int game_id { get; set; }
        public string game_slug { get; set; }
        public string currency_symbol { get; set; }
        public bool isBuy { get; set; }
    }

    public class DailyReportResponse
    {
        public string agent_code { get; set; }
        public string date_at_daily { get; set; }
        public string currency { get; set; }
        public decimal total_turnover { get; set; }
        public decimal total_winning { get; set; }
        public decimal total_win_loss { get; set; }
        public int spin_count { get; set; }
        public int unique_players { get; set; }
    }

    public class PaginationResponse
    {
        public int current_page { get; set; }
        public int last_page { get; set; }
        public int per_page { get; set; }
        public int total { get; set; }
        public int count { get; set; }
    }



    public class SupportedLanguage
    {
        public string code { get; set; }
        public string lcid { get; set; }
        public string label { get; set; }
    }

    public class SupportedLanguagesResponse
    {
        public List<SupportedLanguage> languages { get; set; }
    }

    public class SupportedCurrency
    {
        public string code { get; set; }
        public string label { get; set; }
        public string symbol { get; set; }
        public string language { get; set; }
    }

    public class SupportedCurrenciesResponse
    {
        public List<SupportedCurrency> currencies { get; set; }
    }
}