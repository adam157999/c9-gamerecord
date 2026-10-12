using System;

namespace GameRecord.Gamess.OctoPlay.Models
{
    public class GameLaunchRequest
    {
        public string BrandId { get; set; } // Brand ID
        public string Country { get; set; } // Country code
        public string Currency { get; set; } // Currency
        public string GameId { get; set; } // Game ID
        public string Ip { get; set; } // IP address
        public string LobbyUrl { get; set; } // Lobby URL
        public string Locale { get; set; } // Locale
        public string Mode { get; set; } // Mode (wallet)
        public string Platform { get; set; } // Platform (web)
        public string PlayerId { get; set; } // Player ID
        public string SessionId { get; set; } // Session ID
    }

    public class GameLaunchResponse
    {
        public string Url { get; set; } // Game launch URL
    }
}
