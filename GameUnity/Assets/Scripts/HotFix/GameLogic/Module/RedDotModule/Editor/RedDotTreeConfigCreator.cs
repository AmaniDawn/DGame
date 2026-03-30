#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 红点树配置创建向导
    /// </summary>
    public static class RedDotTreeConfigCreator
    {
        [MenuItem("DGame Tools/RedDot/Create Sample Config")]
        public static void CreateSampleConfig()
        {
            // 创建配置资源
            var config = ScriptableObject.CreateInstance<RedDotTreeConfig>();

            // 添加示例树结构
            var main = new RedDotNodeConfig("Main") { description = "主界面" };
            config.RootNodes.Add(main);

            // 邮件系统
            var mail = new RedDotNodeConfig("Mail") { description = "邮件系统" };
            mail.children.Add(new RedDotNodeConfig("System") { description = "系统邮件" });
            mail.children.Add(new RedDotNodeConfig("Player") { description = "玩家邮件" });
            mail.children.Add(new RedDotNodeConfig("Guild") { description = "公会邮件" });
            main.children.Add(mail);

            // 背包系统
            var bag = new RedDotNodeConfig("Bag") { description = "背包系统" };
            bag.children.Add(new RedDotNodeConfig("Equipment") { description = "装备", type = RedDotType.New });
            bag.children.Add(new RedDotNodeConfig("Item") { description = "道具", type = RedDotType.Number });
            bag.children.Add(new RedDotNodeConfig("Material") { description = "材料" });
            main.children.Add(bag);

            // 任务系统
            var quest = new RedDotNodeConfig("Quest") { description = "任务系统" };
            quest.children.Add(new RedDotNodeConfig("Main") { description = "主线任务" });
            quest.children.Add(new RedDotNodeConfig("Daily") { description = "日常任务", type = RedDotType.Number });
            quest.children.Add(new RedDotNodeConfig("Weekly") { description = "周常任务", type = RedDotType.Number });
            main.children.Add(quest);

            // 活动系统
            var activity = new RedDotNodeConfig("Activity") { description = "活动系统" };

            var sevenDay = new RedDotNodeConfig("SevenDay") { description = "七日活动" };
            sevenDay.children.Add(new RedDotNodeConfig("Task") { description = "七日活动任务" });
            sevenDay.children.Add(new RedDotNodeConfig("Reward") { description = "七日活动奖励" });
            activity.children.Add(sevenDay);

            var signin = new RedDotNodeConfig("SignIn") { description = "签到活动" };
            signin.children.Add(new RedDotNodeConfig("Daily") { description = "每日签到" });
            signin.children.Add(new RedDotNodeConfig("Cumulative") { description = "累计签到" });
            activity.children.Add(signin);

            main.children.Add(activity);

            // 社交系统
            var social = new RedDotNodeConfig("Social") { description = "社交系统" };
            config.RootNodes.Add(social);

            var friends = new RedDotNodeConfig("Friends") { description = "好友" };
            friends.children.Add(new RedDotNodeConfig("Request") { description = "好友申请", type = RedDotType.Number });
            friends.children.Add(new RedDotNodeConfig("Recommend") { description = "推荐好友" });
            social.children.Add(friends);

            var chat = new RedDotNodeConfig("Chat") { description = "聊天" };
            chat.children.Add(new RedDotNodeConfig("World") { description = "世界频道", type = RedDotType.Number });
            chat.children.Add(new RedDotNodeConfig("Guild") { description = "公会频道", type = RedDotType.Number });
            chat.children.Add(new RedDotNodeConfig("Private") { description = "私聊", type = RedDotType.Number });
            social.children.Add(chat);

            // 刷新路径
            config.RefreshPaths();

            // 保存资源
            string path = "Assets/Scripts/HotFix/GameLogic/Module/RedDotModule/Config/RedDotTreeConfig.asset";
            string directory = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();

            // 选中并打开编辑器
            Selection.activeObject = config;
            RedDotTreeEditorWindow.ShowWindow();

            Debug.Log($"[RedDot] Sample config created at: {path}");
        }
    }
}

#endif