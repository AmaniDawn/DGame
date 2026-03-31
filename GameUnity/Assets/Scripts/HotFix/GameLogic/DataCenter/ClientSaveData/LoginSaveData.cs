namespace GameLogic
{
    [ClientSaveData("LoginSaveData")]
    public class LoginSaveData : BaseClientSaveData
    {
        public int IsFirstOpenUserPrivacy = 1;
        public int SetAgreeUserPrivacy;
    }
}