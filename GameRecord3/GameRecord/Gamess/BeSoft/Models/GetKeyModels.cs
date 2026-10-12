namespace GameRecord.Gamess.BeSoft.Models
{
    public class GetKeyRequest
    {
        public string Cert { get; set; } // Security code

        public string User { get; set; } // Unique user ID (only allow lower case a-z and 0-9)

        public string Extension1 { get; set; } // Agent ID (Can be used for login, each proxy account can only be used for a single operator)
    }

    public class GetKeyResponse
    {
        public int Status { get; set; } // Status code

        public string Key { get; set; } // Generated key

        public string ReturnUrl { get; set; } // Return URL with the key
    }
}