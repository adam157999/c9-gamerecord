namespace GameRecord.Gamess.MT.Models
{
    public class GetURLTokenResponse : BaseResponse
    {
        public GameLaunchData Data { get; set; }
    }

    public class GameLaunchData
    {
        public string url { get; set; }
        public string token { get; set; }
    }
}