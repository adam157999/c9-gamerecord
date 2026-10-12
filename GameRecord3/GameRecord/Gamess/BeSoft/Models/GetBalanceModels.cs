using System.Collections.Generic;

namespace GameRecord.Gamess.BeSoft.Models
{
    public class GetBalanceRequest
    {
        public string Cert { get; set; } // Security code
        public string Extension1 { get; set; } // Agent ID (agent id to login ARS)
        public string Users { get; set; } // A series of usernames split by comma
    }

    public class GetBalanceUser
    {
        public string User { get; set; } // Username
        public string CurScore { get; set; } // Current score
    }

    public class GetBalanceResponse
    {
        public string Extension1 { get; set; } // Agent or admin
        public int Status { get; set; } // Status code
        public string TotalScore { get; set; } // Total score
        public List<GetBalanceUser> Users { get; set; } // List of user balances
    }
}
