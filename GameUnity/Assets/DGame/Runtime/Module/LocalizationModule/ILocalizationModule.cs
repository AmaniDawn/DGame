namespace DGame
{
    public interface ILocalizationModule
    {
        /// <summary>
        /// 获取或设置本地化语言
        /// </summary>
        Language CurrentLanguage { get; }

        /// <summary>
        /// 获取系统语言
        /// </summary>
        Language SystemLanguage { get; }

        /// <summary>
        /// 设置本地化辅助器
        /// </summary>
        /// <param name="localizationHelper"></param>
        void SetLocalizationHelper(ILocalizationHelper localizationHelper);

        /// <summary>
        /// 检查是否存在语言
        /// </summary>
        /// <param name="language">语言</param>
        /// <returns></returns>
        bool ContainsLanguage(Language language);

        /// <summary>
        /// 检查是否存在语言
        /// </summary>
        /// <param name="language">语言</param>
        /// <returns></returns>
        bool ContainsLanguage(int language);

        /// <summary>
        /// 设置当前语言
        /// </summary>
        /// <param name="language">语言</param>
        /// <returns></returns>
        bool SetLanguage(Language language);

        /// <summary>
        /// 设置当前语言
        /// </summary>
        /// <param name="language">语言</param>
        /// <returns></returns>
        bool SetLanguage(int language);
    }
}