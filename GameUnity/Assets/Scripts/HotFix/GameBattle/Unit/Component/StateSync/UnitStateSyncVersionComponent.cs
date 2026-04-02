using Fantasy.Entitas;

namespace GameBattle
{
    /// <summary>
    /// 单位状态同步版本记录组件。
    /// </summary>
    public sealed class UnitStateSyncVersionComponent : Entity
    {
        /// <summary>
        /// 获取或设置上次已同步的变换版本号。
        /// </summary>
        public uint LastTransformVersion { get; set; }

        /// <summary>
        /// 获取或设置上次已同步的状态版本号。
        /// </summary>
        public uint LastStateVersion { get; set; }

        /// <summary>
        /// 获取或设置上次已同步的属性版本号。
        /// </summary>
        public uint LastAttrVersion { get; set; }

        /// <summary>
        /// 清空已记录的版本号。
        /// </summary>
        public void Clear()
        {
            LastTransformVersion = 0;
            LastStateVersion = 0;
            LastAttrVersion = 0;
        }
    }
}