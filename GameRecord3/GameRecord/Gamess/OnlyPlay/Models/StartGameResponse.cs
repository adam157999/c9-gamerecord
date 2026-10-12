namespace GameRecord.Gamess.OnlyPlay.Models
{
    public class StartGameResponse
    {
        public int ErrorCode { get; set; } // Error code
        public string Message { get; set; } // Response message
        public string Url { get; set; } // Game URL
    }
}