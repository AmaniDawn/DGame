using Fantasy.Entitas.Interface;

namespace GameLogic
{
    /// <summary>
    /// 渲染层单位销毁触发系统。
    /// </summary>
    public sealed class RenderUnitDestroySystem : DestroySystem<RenderUnit>
    {
        protected override void Destroy(RenderUnit self)
        {
            self.Destroy();
        }
    }

    /// <summary>
    /// 渲染层单位扩展系统入口。
    /// </summary>
    public static class RenderUnitSystem
    {
        /// <summary>
        /// 获取当前绑定逻辑单位的标识。
        /// </summary>
        /// <returns>逻辑单位存在时返回其标识，否则返回 0。</returns>
        public static ulong GetPlayerID(this RenderUnit self)
        {
            if (self.LogicUnit != null)
            {
                return self.LogicUnit.UnitID;
            }

            return 0;
        }
        
        /// <summary>
        /// 获取在编辑器中展示的游戏对象名称。
        /// </summary>
        /// <returns>编辑器下返回包含单位信息的名称，运行时返回通用名称。</returns>
        public static string GetGameObjectName(this RenderUnit self)
        {
            if (DGame.Utility.PlatformUtil.IsEditorPlatform())
            {
                return $"[{self.UnitID}][{self.LogicUnit.UnitType}][{self.UnitName}]";
            }

            return "RenderUnit";
        }
        
        /// <summary>
        /// 判断两个渲染单位是否表示同一个运行时实例。
        /// </summary>
        /// <param name="self">渲染单位。</param>
        /// <param name="other">待比较的渲染单位。</param>
        /// <returns>是同一个有效运行时实例时返回 <see langword="true"/>。</returns>
        public static bool IsSameUnit(this RenderUnit self, RenderUnit other)
            => other != null && self.RuntimeId != 0 && other.RuntimeId != 0 && self.RuntimeId == other.RuntimeId;
    }
}