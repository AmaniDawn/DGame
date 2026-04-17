using GameProto;

namespace GameLogic
{
    public interface ILocalizationHelper
    {
        /// <summary>
        /// 获取或设置本地化语言
        /// </summary>
        LocalAreaType CurrentLanguage { get; set; }

        /// <summary>
        /// 获取系统语言
        /// </summary>
        LocalAreaType SystemLanguage { get; }

        /// <summary>
        /// 检查是否存在语言
        /// </summary>
        /// <param name="language">语言</param>
        /// <returns></returns>
        bool ContainsLanguage(LocalAreaType language);

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
        bool SetLanguage(LocalAreaType language);

        /// <summary>
        /// 设置当前语言
        /// </summary>
        /// <param name="language">语言</param>
        /// <returns></returns>
        bool SetLanguage(int language);
    }
}