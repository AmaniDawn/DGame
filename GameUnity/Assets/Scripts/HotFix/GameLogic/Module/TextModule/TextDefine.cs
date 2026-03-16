namespace GameLogic
{
	/// <summary>
	/// 文本枚举
	/// </summary>
	public enum TextDefine
	{
		#region 格式化时间戳

		ID_LABEL_DAY_HOUR_MIN_TEXT = 10000001, // {0}天{1}时{2}分
		ID_LABEL_DAY_HOUR_MIN_SEC_TEXT, // {0}天 {1}：{2}：{3}
		ID_LABEL_COMMON_HOUR_MIN_TEXT, // {0}小时{1}分钟
		ID_LABEL_CHINESE_DAY_HOUR_MIN_SEC_TEXT, // {0}天{1}时{2}分{3}秒
		ID_LABEL_CHINESE_HOUR_MIN_SEC_TEXT, // {0}时{1}分{2}秒
		ID_LABEL_CHINESE_MIN_SEC_TXT_WITH_FENGE, // {0}分{1}{2}秒
		ID_LABEL_CHINESE_MIN_SEC_TXT_WITHOUT_FENGE, // {0}分{2}秒

		#endregion

		#region 大写数字

		ID_LABEL_ZERO = 10000008, //零
		ID_LABEL_ONE, //一
		ID_LABEL_TWO, //二
		ID_LABEL_THREE, //三
		ID_LABEL_FOUR, //四
		ID_LABEL_FIVE, //五
		ID_LABEL_SIX, //六
		ID_LABEL_SEVEN, //七
		ID_LABEL_EIGHT, //八
		ID_LABEL_NINE, //九
		ID_LABEL_TEN, //十

		#endregion

		#region 时间差格式

		ID_LABEL_TIME_SPAN_SECOND = 10000019, //刚刚
		ID_LABEL_TIME_SPAN_MINUTE, //{0}分前
		ID_LABEL_TIME_SPAN_HOUR, //{0}时{1}分前
		ID_LABEL_TIME_SPAN_DAY, //{0}天前
		ID_LABEL_TIME_SPAN_HOUR_WITHOUT_MIN, //{0}小时前

		#endregion

		ID_LABEL_START_GAME = 10000024,
		ID_LABEL_QUIT_GAME,

		#region G.R自动生成

		// 此代码块的文本ID为自动生成 请不要手动修改
		// AutoBuildStart
		// AutoBuildEnd

		#endregion

		#region Prefab自动生成

		// 此代码块的文本ID为自动生成 请不要手动修改
		// PrefabAutoBuildStart
		// PrefabAutoBuildEnd

		#endregion
	}
}