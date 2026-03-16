// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Fantasy
{
	/// <summary>
	/// 活动类型的枚举
	/// </summary>
	public enum ActivityType
	{
		/// <summary>
		/// 无
		/// </summary>
		ACTIVITY_TYPE_NONE = 0
	}

	/// <summary>
	/// 帧命令类型
	/// </summary>
	public enum FrameCmdType
	{
		FRAME_CMD_GM = 0
	}

	/// <summary>
	/// 系统开放类型
	/// </summary>
	public enum FuncType
	{
		/// <summary>
		/// 异常
		/// </summary>
		FUNC_TYPE_NONE = 0,
		/// <summary>
		/// 商城
		/// </summary>
		FUNC_TYPE_SHOP = 1,
		/// <summary>
		/// 角色
		/// </summary>
		FUNC_TYPE_ACTOR = 2,
		/// <summary>
		/// 主界面
		/// </summary>
		FUNC_TYPE_MAIN = 3,
		/// <summary>
		/// 公会
		/// </summary>
		FUNC_TYPE_LEAGUE = 4,
		/// <summary>
		/// 玩法
		/// </summary>
		FUNC_TYPE_GAMEPLAY = 5,
		/// <summary>
		/// 家园
		/// </summary>
		FUNC_TYPE_HOME = 6
	}


}