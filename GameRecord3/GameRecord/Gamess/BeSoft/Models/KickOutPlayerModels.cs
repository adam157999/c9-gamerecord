namespace GameRecord.Gamess.BeSoft.Models
{
    public class KickOutPlayerRequest
    {
        public string Extension1 { get; set; } // Security code
        public string Cert { get; set; } // Agent account
        public string User { get; set; } // The player's account to be kicked off (optional)
    }

    public class KickOutPlayerResponse
    {
        public int Status { get; set; } // Status code for success
        public int? Erro { get; set; } // Error code for failure (nullable)
    }
}
