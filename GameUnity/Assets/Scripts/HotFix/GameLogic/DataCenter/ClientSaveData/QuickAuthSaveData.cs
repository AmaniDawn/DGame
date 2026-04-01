namespace GameLogic
{
    [ClientSaveData("QuickAuthSaveData")]
    public class QuickAuthSaveData : BaseClientSaveData
    {
        public string Uid;
        public string Pwd;
        public string Token;

        /// <summary>
        /// 获取系统保存数据实例
        /// </summary>
        public static QuickAuthSaveData Get => BaseClientSaveData.Get<QuickAuthSaveData>();
    }
}