using System;
using System.Collections.Generic;

namespace GameRecord.Gamess.OctoPlay.Models
{
    public class GameListRequest
    {
        public string BrandId { get; set; } // Brand ID
    }

    public class GameInfo
    {
        public List<string> BlockedCountries { get; set; } // Blocked countries
        public string GameCategory { get; set; } // Game category
        public string GameId { get; set; } // Game ID
        public string GameName { get; set; } // Game name
        public bool IsDemoModeDisabled { get; set; } // Demo mode disabled
        public bool IsFreespinsEnabled { get; set; } // Freespins enabled
        public bool IsWalletModeDisabled { get; set; } // Wallet mode disabled
        public List<string> Platform { get; set; } // Supported platforms
        public string ProviderName { get; set; } // Provider name
        public string ReleaseDate { get; set; } // Release date
        public string Rtp { get; set; } // RTP
        public List<string> SupportedCurrencies { get; set; } // Supported currencies
        public List<string> SupportedLocales { get; set; } // Supported locales
        public string ThumbnailUrl { get; set; } // Thumbnail URL
    }

    public class GameListResponse
    {
        public List<GameInfo> Games { get; set; } // List of games
    }
}
