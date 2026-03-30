using System.Globalization;
using UnityEngine;

namespace DGame
{
    public partial class DebuggerDriver
    {
        private class ObjectPoolInformationWindow : ScrollableDebuggerWindowBase
        {
            private IObjectPoolModule m_objectPool = null;

            public override void Initialize(params object[] args)
            {
                m_objectPool = ModuleSystem.GetModule<IObjectPoolModule>();
                if (m_objectPool == null)
                {
                    DLogger.Fatal("Object pool component is invalid.");
                }
            }

            protected override void OnDrawScrollableWindow()
            {
                DrawSectionTitle("Object Pool Overview");
                BeginPanel();
                {
                    int poolCount = m_objectPool.Count;
                    Color32 countColor = poolCount > 0 ? DebuggerStyles.PrimaryColor : DebuggerStyles.TextColor;
                    DrawItemColored("Total Object Pools", poolCount.ToString(), countColor);
                }
                EndPanel();

                BaseObjectPool[] objectPools = m_objectPool.GetAllObjectPools(true);

                if (objectPools.Length == 0)
                {
                    DrawSectionTitle("Pool Details");
                    BeginPanel();
                    {
                        DrawInfoMessage("No object pools created yet...");
                    }
                    EndPanel();
                }
                else
                {
                    for (int i = 0; i < objectPools.Length; i++)
                    {
                        DrawObjectPool(objectPools[i], i);
                    }
                }
            }

            private void DrawObjectPool(BaseObjectPool objectPool, int index)
            {
                DrawSectionTitle(Utility.StringUtil.Format("Pool #{0}: {1}", index + 1, objectPool.Name));
                BeginPanel();
                {
                    ResetRowIndex();
                    DrawItem("Full Name", objectPool.FullName, "对象池完整名称");
                    DrawItem("Object Type", objectPool.ObjectType.Name, "对象池类型");

                    GUILayout.Space(4);

                    // 容量信息
                    int usedCount = objectPool.Count;
                    int capacity = objectPool.Capacity;
                    float usagePercent = capacity > 0 ? (float)usedCount / capacity : 0f;
                    Color32 usageColor = usagePercent > 0.8f ? DebuggerStyles.WarningColor :
                                         usagePercent > 0.5f ? DebuggerStyles.PrimaryColor : DebuggerStyles.SuccessColor;

                    DrawItemColored("Usage", Utility.StringUtil.Format("{0} / {1} ({2:P0})", usedCount, capacity, usagePercent), usageColor);
                    DrawItem("Can Release Count", objectPool.CanReleaseCount.ToString(), "可以释放的对象数量");

                    GUILayout.Space(4);

                    DrawItem("Auto Release Interval", Utility.StringUtil.Format("{0} s", objectPool.AutoReleaseInterval.ToString(CultureInfo.InvariantCulture)), "自动释放间隔");
                    DrawItem("Expire Time", Utility.StringUtil.Format("{0} s", objectPool.ExpireTime.ToString(CultureInfo.InvariantCulture)), "对象过期时间");
                    DrawItem("Priority", objectPool.Priority.ToString(), "对象池优先级");
                    DrawItem("Allow Multi Spawn", objectPool.AllowMultiSpawn.ToString(), "是否允许多次获取");
                }
                EndPanel();

                // 对象详情表格
                PoolObjectInfo[] objectInfos = objectPool.GetAllPoolObjectInfos();
                if (objectInfos.Length > 0)
                {
                    BeginPanel();
                    {
                        // 表格头
                        DrawTableHeader(
                            ("Name", 0),
                            ("Locked", 50f),
                            (objectPool.AllowMultiSpawn ? "Count" : "In Use", 50f),
                            ("Flag", 40f),
                            ("Priority", 55f),
                            ("Last Use", 130f)
                        );

                        // 数据行
                        for (int i = 0; i < objectInfos.Length; i++)
                        {
                            string nameText = string.IsNullOrEmpty(objectInfos[i].Name) ? "(unnamed)" : objectInfos[i].Name;
                            string statusText = objectPool.AllowMultiSpawn
                                ? objectInfos[i].SpawnCount.ToString()
                                : objectInfos[i].IsUsing.ToString();

                            // 如果对象正在使用，用特殊颜色标记
                            bool isActive = objectPool.AllowMultiSpawn
                                ? objectInfos[i].SpawnCount > 0
                                : objectInfos[i].IsUsing;

                            if (isActive)
                            {
                                GUILayout.BeginHorizontal(DebuggerStyles.PrimaryBoxStyle);
                                {
                                    GUILayout.Label(DebuggerStyles.ColorText(nameText, DebuggerStyles.PrimaryColor), DebuggerStyles.RichLabelStyle);
                                    GUILayout.Label(DebuggerStyles.ColorText(objectInfos[i].Locked.ToString(), DebuggerStyles.PrimaryColor), DebuggerStyles.RichLabelStyle, GUILayout.Width(50f));
                                    GUILayout.Label(DebuggerStyles.ColorText(statusText, DebuggerStyles.PrimaryColor), DebuggerStyles.RichLabelStyle, GUILayout.Width(50f));
                                    GUILayout.Label(DebuggerStyles.ColorText(objectInfos[i].CustomCanReleaseFlag.ToString(), DebuggerStyles.PrimaryColor), DebuggerStyles.RichLabelStyle, GUILayout.Width(40f));
                                    GUILayout.Label(DebuggerStyles.ColorText(objectInfos[i].Priority.ToString(), DebuggerStyles.PrimaryColor), DebuggerStyles.RichLabelStyle, GUILayout.Width(55f));
                                    GUILayout.Label(DebuggerStyles.ColorText(objectInfos[i].LastUseTime.ToLocalTime().ToString("MM-dd HH:mm:ss"), DebuggerStyles.PrimaryColor), DebuggerStyles.RichLabelStyle, GUILayout.Width(130f));
                                }
                                GUILayout.EndHorizontal();
                            }
                            else
                            {
                                DrawTableRow(i % 2 == 1,
                                    (nameText, 0),
                                    (objectInfos[i].Locked.ToString(), 50f),
                                    (statusText, 50f),
                                    (objectInfos[i].CustomCanReleaseFlag.ToString(), 40f),
                                    (objectInfos[i].Priority.ToString(), 55f),
                                    (objectInfos[i].LastUseTime.ToLocalTime().ToString("MM-dd HH:mm:ss"), 130f)
                                );
                            }
                        }
                    }
                    EndPanel();
                }
            }
        }
    }
}