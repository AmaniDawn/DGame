namespace GameLogic
{
    [ClientSaveData("LoginSaveData")]
    public class LoginSaveData : BaseClientSaveData
    {
        public int IsFirstOpenUserPrivacy = 1;
        public int SetAgreeUserPrivacy;

        /// <summary>
        /// 获取系统保存数据实例
        /// </summary>
        public static LoginSaveData Get => BaseClientSaveData.Get<LoginSaveData>();
    }
}