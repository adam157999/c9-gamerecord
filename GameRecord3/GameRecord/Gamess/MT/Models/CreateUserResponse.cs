namespace GameRecord.Gamess.MT.Models
{
    public class CreateUserResponse : BaseResponse
    {
        public CreateUserData Data { get; set; }
    }

    public class CreateUserData
    {
        public int Result { get; set; }
    }
}