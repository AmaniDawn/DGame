// =============================================================================
// AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
// Generated from: Assets/Scripts/HotFix/GameLogic/Module/RedDotModule/Config/RedDotTreeConfig.asset
// =============================================================================

namespace GameLogic
{
	public static class RedDotPathDefine
	{

		/// <summary>
		/// 主界面
		/// </summary>
		public static class Main
		{
			public const int Id = 1;
			public const string Path = "Main";
			public static readonly string[] Segments = { "Main" };

			/// <summary>
			/// 邮件系统
			/// </summary>
			public static class Mail
			{
				public const int Id = 2;
				public const string Path = "Main/Mail";
				public static readonly string[] Segments = { "Main", "Mail" };

				/// <summary>
				/// 系统邮件
				/// </summary>
				public const int System = 3;
				public const string SystemPath = "Main/Mail/System";
				public static readonly string[] SystemSegments = { "Main", "Mail", "System" };

				/// <summary>
				/// 玩家邮件
				/// </summary>
				public const int Player = 4;
				public const string PlayerPath = "Main/Mail/Player";
				public static readonly string[] PlayerSegments = { "Main", "Mail", "Player" };

				/// <summary>
				/// 公会邮件
				/// </summary>
				public const int Guild = 5;
				public const string GuildPath = "Main/Mail/Guild";
				public static readonly string[] GuildSegments = { "Main", "Mail", "Guild" };
			}

			/// <summary>
			/// 背包系统
			/// </summary>
			public static class Bag
			{
				public const int Id = 6;
				public const string Path = "Main/Bag";
				public static readonly string[] Segments = { "Main", "Bag" };

				/// <summary>
				/// 装备
				/// </summary>
				public const int Equipment = 7;
				public const string EquipmentPath = "Main/Bag/Equipment";
				public static readonly string[] EquipmentSegments = { "Main", "Bag", "Equipment" };

				/// <summary>
				/// 道具
				/// </summary>
				public const int Item = 8;
				public const string ItemPath = "Main/Bag/Item";
				public static readonly string[] ItemSegments = { "Main", "Bag", "Item" };

				/// <summary>
				/// 材料
				/// </summary>
				public const int Material = 9;
				public const string MaterialPath = "Main/Bag/Material";
				public static readonly string[] MaterialSegments = { "Main", "Bag", "Material" };
			}

			/// <summary>
			/// 任务系统
			/// </summary>
			public static class Quest
			{
				public const int Id = 10;
				public const string Path = "Main/Quest";
				public static readonly string[] Segments = { "Main", "Quest" };

				/// <summary>
				/// 主线任务
				/// </summary>
				public const int Main = 11;
				public const string MainPath = "Main/Quest/Main";
				public static readonly string[] MainSegments = { "Main", "Quest", "Main" };

				/// <summary>
				/// 日常任务
				/// </summary>
				public const int Daily = 12;
				public const string DailyPath = "Main/Quest/Daily";
				public static readonly string[] DailySegments = { "Main", "Quest", "Daily" };

				/// <summary>
				/// 周常任务
				/// </summary>
				public const int Weekly = 13;
				public const string WeeklyPath = "Main/Quest/Weekly";
				public static readonly string[] WeeklySegments = { "Main", "Quest", "Weekly" };
			}

			/// <summary>
			/// 活动系统
			/// </summary>
			public static class Activity
			{
				public const int Id = 14;
				public const string Path = "Main/Activity";
				public static readonly string[] Segments = { "Main", "Activity" };

				/// <summary>
				/// 七日活动
				/// </summary>
				public static class SevenDay
				{
					public const int Id = 15;
					public const string Path = "Main/Activity/SevenDay";
					public static readonly string[] Segments = { "Main", "Activity", "SevenDay" };

					/// <summary>
					/// 七日活动任务
					/// </summary>
					public const int Task = 16;
					public const string TaskPath = "Main/Activity/SevenDay/Task";
					public static readonly string[] TaskSegments = { "Main", "Activity", "SevenDay", "Task" };

					/// <summary>
					/// 七日活动奖励
					/// </summary>
					public const int Reward = 17;
					public const string RewardPath = "Main/Activity/SevenDay/Reward";
					public static readonly string[] RewardSegments = { "Main", "Activity", "SevenDay", "Reward" };
				}

				/// <summary>
				/// 签到活动
				/// </summary>
				public static class SignIn
				{
					public const int Id = 18;
					public const string Path = "Main/Activity/SignIn";
					public static readonly string[] Segments = { "Main", "Activity", "SignIn" };

					/// <summary>
					/// 每日签到
					/// </summary>
					public const int Daily = 19;
					public const string DailyPath = "Main/Activity/SignIn/Daily";
					public static readonly string[] DailySegments = { "Main", "Activity", "SignIn", "Daily" };

					/// <summary>
					/// 累计签到
					/// </summary>
					public const int Cumulative = 20;
					public const string CumulativePath = "Main/Activity/SignIn/Cumulative";
					public static readonly string[] CumulativeSegments = { "Main", "Activity", "SignIn", "Cumulative" };
				}
			}
		}

		/// <summary>
		/// 社交系统
		/// </summary>
		public static class Social
		{
			public const int Id = 21;
			public const string Path = "Social";
			public static readonly string[] Segments = { "Social" };

			/// <summary>
			/// 好友
			/// </summary>
			public static class Friends
			{
				public const int Id = 22;
				public const string Path = "Social/Friends";
				public static readonly string[] Segments = { "Social", "Friends" };

				/// <summary>
				/// 好友申请
				/// </summary>
				public const int Request = 23;
				public const string RequestPath = "Social/Friends/Request";
				public static readonly string[] RequestSegments = { "Social", "Friends", "Request" };

				/// <summary>
				/// 推荐好友
				/// </summary>
				public const int Recommend = 24;
				public const string RecommendPath = "Social/Friends/Recommend";
				public static readonly string[] RecommendSegments = { "Social", "Friends", "Recommend" };
			}

			/// <summary>
			/// 聊天
			/// </summary>
			public static class Chat
			{
				public const int Id = 25;
				public const string Path = "Social/Chat";
				public static readonly string[] Segments = { "Social", "Chat" };

				/// <summary>
				/// 世界频道
				/// </summary>
				public const int World = 26;
				public const string WorldPath = "Social/Chat/World";
				public static readonly string[] WorldSegments = { "Social", "Chat", "World" };

				/// <summary>
				/// 公会频道
				/// </summary>
				public const int Guild = 27;
				public const string GuildPath = "Social/Chat/Guild";
				public static readonly string[] GuildSegments = { "Social", "Chat", "Guild" };

				/// <summary>
				/// 私聊
				/// </summary>
				public const int Private = 28;
				public const string PrivatePath = "Social/Chat/Private";
				public static readonly string[] PrivateSegments = { "Social", "Chat", "Private" };
			}
		}

        /// <summary>
        /// 注册所有红点节点到 RedDotModule（零 GC）
        /// </summary>
        public static void RegisterAll()
        {
        	var mgr = RedDotModule.Instance;

            // 主界面
            mgr.Register(Main.Id, Main.Path, Main.Segments, RedDotType.Dot, RedDotAggregateStrategy.Or);

            // 邮件系统
            mgr.Register(Main.Mail.Id, Main.Mail.Path, Main.Mail.Segments, RedDotType.Dot, RedDotAggregateStrategy.Or);

            // 系统邮件
            mgr.Register(Main.Mail.System, Main.Mail.SystemPath, Main.Mail.SystemSegments, RedDotType.Dot, RedDotAggregateStrategy.Or);
            // 玩家邮件
            mgr.Register(Main.Mail.Player, Main.Mail.PlayerPath, Main.Mail.PlayerSegments, RedDotType.Dot, RedDotAggregateStrategy.Or);
            // 公会邮件
            mgr.Register(Main.Mail.Guild, Main.Mail.GuildPath, Main.Mail.GuildSegments, RedDotType.Dot, RedDotAggregateStrategy.Or);
            // 背包系统
            mgr.Register(Main.Bag.Id, Main.Bag.Path, Main.Bag.Segments, RedDotType.Dot, RedDotAggregateStrategy.Or);

            // 装备
            mgr.Register(Main.Bag.Equipment, Main.Bag.EquipmentPath, Main.Bag.EquipmentSegments, RedDotType.New, RedDotAggregateStrategy.Or);
            // 道具
            mgr.Register(Main.Bag.Item, Main.Bag.ItemPath, Main.Bag.ItemSegments, RedDotType.Number, RedDotAggregateStrategy.Or);
            // 材料
            mgr.Register(Main.Bag.Material, Main.Bag.MaterialPath, Main.Bag.MaterialSegments, RedDotType.Dot, RedDotAggregateStrategy.Or);
            // 任务系统
            mgr.Register(Main.Quest.Id, Main.Quest.Path, Main.Quest.Segments, RedDotType.Dot, RedDotAggregateStrategy.Or);

            // 主线任务
            mgr.Register(Main.Quest.Main, Main.Quest.MainPath, Main.Quest.MainSegments, RedDotType.Dot, RedDotAggregateStrategy.Or);
            // 日常任务
            mgr.Register(Main.Quest.Daily, Main.Quest.DailyPath, Main.Quest.DailySegments, RedDotType.Number, RedDotAggregateStrategy.Or);
            // 周常任务
            mgr.Register(Main.Quest.Weekly, Main.Quest.WeeklyPath, Main.Quest.WeeklySegments, RedDotType.Number, RedDotAggregateStrategy.Or);
            // 活动系统
            mgr.Register(Main.Activity.Id, Main.Activity.Path, Main.Activity.Segments, RedDotType.Dot, RedDotAggregateStrategy.Or);

            // 七日活动
            mgr.Register(Main.Activity.SevenDay.Id, Main.Activity.SevenDay.Path, Main.Activity.SevenDay.Segments, RedDotType.Dot, RedDotAggregateStrategy.Or);

            // 七日活动任务
            mgr.Register(Main.Activity.SevenDay.Task, Main.Activity.SevenDay.TaskPath, Main.Activity.SevenDay.TaskSegments, RedDotType.Dot, RedDotAggregateStrategy.Or);
            // 七日活动奖励
            mgr.Register(Main.Activity.SevenDay.Reward, Main.Activity.SevenDay.RewardPath, Main.Activity.SevenDay.RewardSegments, RedDotType.Dot, RedDotAggregateStrategy.Or);
            // 签到活动
            mgr.Register(Main.Activity.SignIn.Id, Main.Activity.SignIn.Path, Main.Activity.SignIn.Segments, RedDotType.Dot, RedDotAggregateStrategy.Or);

            // 每日签到
            mgr.Register(Main.Activity.SignIn.Daily, Main.Activity.SignIn.DailyPath, Main.Activity.SignIn.DailySegments, RedDotType.Dot, RedDotAggregateStrategy.Or);
            // 累计签到
            mgr.Register(Main.Activity.SignIn.Cumulative, Main.Activity.SignIn.CumulativePath, Main.Activity.SignIn.CumulativeSegments, RedDotType.Dot, RedDotAggregateStrategy.Or);
            // 社交系统
            mgr.Register(Social.Id, Social.Path, Social.Segments, RedDotType.Dot, RedDotAggregateStrategy.Or);

            // 好友
            mgr.Register(Social.Friends.Id, Social.Friends.Path, Social.Friends.Segments, RedDotType.Dot, RedDotAggregateStrategy.Or);

            // 好友申请
            mgr.Register(Social.Friends.Request, Social.Friends.RequestPath, Social.Friends.RequestSegments, RedDotType.Number, RedDotAggregateStrategy.Or);
            // 推荐好友
            mgr.Register(Social.Friends.Recommend, Social.Friends.RecommendPath, Social.Friends.RecommendSegments, RedDotType.Dot, RedDotAggregateStrategy.Or);
            // 聊天
            mgr.Register(Social.Chat.Id, Social.Chat.Path, Social.Chat.Segments, RedDotType.Dot, RedDotAggregateStrategy.Or);

            // 世界频道
            mgr.Register(Social.Chat.World, Social.Chat.WorldPath, Social.Chat.WorldSegments, RedDotType.Number, RedDotAggregateStrategy.Or);
            // 公会频道
            mgr.Register(Social.Chat.Guild, Social.Chat.GuildPath, Social.Chat.GuildSegments, RedDotType.Number, RedDotAggregateStrategy.Or);
            // 私聊
            mgr.Register(Social.Chat.Private, Social.Chat.PrivatePath, Social.Chat.PrivateSegments, RedDotType.Number, RedDotAggregateStrategy.Or);
        }
	}
}
