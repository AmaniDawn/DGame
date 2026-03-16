namespace GameLogic
{
    public static class ClientSaveDataHelper
    {
        /// <summary>
        /// 获取系统设置值
        /// </summary>
        /// <param name="saveData">系统保存数据对象</param>
        /// <param name="saveType">保存类型</param>
        /// <returns>对应的设置值</returns>
        public static int GetSystemSettingVal(this SystemSaveData saveData, SystemSaveData.SaveType saveType)
            => saveData.SettingParams[(int)saveType];

        /// <summary>
        /// 保存系统设置值到本地存储
        /// </summary>
        /// <param name="saveData">系统保存数据对象</param>
        /// <param name="saveType">保存类型</param>
        /// <param name="value">要保存的值</param>
        public static void SaveSystemSettingVal(this SystemSaveData saveData, SystemSaveData.SaveType saveType,
            int value)
        {
            saveData?.SetSystemSettingVal(saveType, value);
            saveData?.Save();
        }

        /// <summary>
        /// 设置系统设置值
        /// </summary>
        /// <param name="saveData">系统保存数据对象</param>
        /// <param name="saveType">保存类型</param>
        /// <param name="value">要设置的值</param>
        public static void SetSystemSettingVal(this SystemSaveData saveData, SystemSaveData.SaveType saveType,
            int value)
            => saveData.SettingParams[(int)saveType] = value;
    }
}