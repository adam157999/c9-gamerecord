namespace GameRecord.Gamess.MT.Models
{
    public class KickOutResponse : BaseResponse
    {
        public KickOutData Data { get; set; }
    }

    public class KickOutData
    {
        public int user_id { get; set; }
        public int result { get; set; } // 1: Success, 0: Failure
    }
}