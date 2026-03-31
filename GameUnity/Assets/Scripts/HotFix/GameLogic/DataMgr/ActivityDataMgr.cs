using System.Collections.Generic;
using Fantasy;
using GameProto;

namespace GameLogic
{
    public class ActivityDataMgr : DataCenterModule<ActivityDataMgr>
    {
        private Dictionary<ActivityType, CSActivityOpenEntry> m_openActivityList = new Dictionary<ActivityType, CSActivityOpenEntry>();

        #region Override

        public override void OnRoleLogout()
        {
            m_openActivityList.Clear();
        }

        #endregion
        
        #region check

        public CSActivityOpenEntry GetActivityOpenEntry(ActivityType activityType)
            => m_openActivityList.TryGetValue(activityType, out var entry) ? entry : null;

        /// <summary>
        /// 检测活动是否开启
        /// </summary>
        /// <param name="activityType"></param>
        /// <param name="showTips"></param>
        /// <returns></returns>
        public bool CheckActivityOpen(int activityType, bool showTips = false)
            => CheckActivityOpen((ActivityType)activityType, showTips);

        /// <summary>
        /// 检测活动是否开启
        /// </summary>
        /// <param name="activityType"></param>
        /// <param name="showTips"></param>
        /// <returns></returns>
        public bool CheckActivityOpen(ActivityType activityType, bool showTips = false)
        {
            bool isOpen = false;
            if (m_openActivityList.TryGetValue(activityType, out var openEntry))
            {
                isOpen = openEntry.OpenTime > 0
                         && Utility.TimeUtil.GetServerSeconds() >= openEntry.OpenTime
                         && Utility.TimeUtil.GetServerSeconds() <= openEntry.EndTime;
            }
            if (!isOpen && showTips)
            {
                GameModule.UIModule.ShowTipsUI(G.R("活动尚未开启"));
            }
            return isOpen;
        }
        
        /// <summary>
        /// 检测活动是否在延迟消失时间
        /// </summary>
        /// <param name="activityType"></param>
        /// <returns></returns>
        public bool CheckActivityDelayDisappear(ActivityType activityType)
        {
            if (m_openActivityList.TryGetValue(activityType, out var openEntry))
            {
                if (Utility.TimeUtil.GetServerSeconds() > openEntry.EndTime && openEntry.DelayTime > 0)
                {
                    return Utility.TimeUtil.GetServerSeconds() <= openEntry.DelayTime;
                }
            }
            return false;
        }

        /// <summary>
        /// 检测活动是否在开启且延迟消失
        /// </summary>
        /// <param name="activityType"></param>
        /// <param name="showTips"></param>
        /// <returns></returns>
        public bool CheckActivityOpenAndDelay(ActivityType activityType, bool showTips = false)
        {
            bool isOpen = false;
            if (m_openActivityList.TryGetValue(activityType, out var openEntry))
            {
                isOpen = openEntry.OpenTime > 0
                         && Utility.TimeUtil.GetServerSeconds() >= openEntry.OpenTime
                         && Utility.TimeUtil.GetServerSeconds() <= openEntry.EndTime + openEntry.DelayTime;
            }
            if (!isOpen && showTips)
            {
                GameModule.UIModule.ShowTipsUI(G.R("活动尚未开启"));
            }
            return isOpen;
        }

        #endregion
    }
}