namespace GameLogic
{
    [ClientSaveData("QuickAuthSaveData")]
    public class QuickAuthSaveData : BaseClientSaveData
    {
        public string Uid;
        public string Pwd;
        public string Token;
    }
}