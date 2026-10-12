namespace GameRecord.Gamess.OnlyPlay.Models
{
    public class CreateAccountResponse
    {
        public int ErrorCode { get; set; } // Error code
        public string Message { get; set; } // Response message
        public string Username { get; set; } // Created username
    }
}