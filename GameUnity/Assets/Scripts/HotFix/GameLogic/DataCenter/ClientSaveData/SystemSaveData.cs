namespace GameLogic
{
    [ClientSaveData("SystemSaveData")]
    public class SystemSaveData : BaseClientSaveData
    {
        /// <summary>
        /// 系统保存数据类型枚举
        /// </summary>
        public enum SaveType
        {
            Max,
        }

        /// <summary>
        /// 系统设置参数数组
        /// </summary>
        public int[] SettingParams { get; private set; } = new int[(int)SaveType.Max];

        /// <summary>
        /// 构造系统保存数据
        /// </summary>
        public SystemSaveData()
        {
        }

        /// <summary>
        /// 获取系统保存数据实例
        /// </summary>
        public static SystemSaveData Get => BaseClientSaveData.Get<SystemSaveData>();
    }
}