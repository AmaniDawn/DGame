using UnityEngine;

namespace DGame
{
    public enum DebuggerActiveWindowType : byte
    {
        /// <summary>
        /// 总是打开
        /// </summary>
        [InspectorName("总是打开")]
        AlwaysOpen = 0,

        /// <summary>
        /// 仅在开发模式时打开
        /// </summary>
        [InspectorName("仅在开发模式时打开")]
        OnlyOpenWhenDevelopment = 1,

        /// <summary>
        /// 仅在编辑器中打开
        /// </summary>
        [InspectorName("仅在编辑器中打开")]
        OnlyOpenInEditor = 2,

        /// <summary>
        /// 总是关闭
        /// </summary>
        [InspectorName("总是关闭")]
        AlwaysClose = 3,
    }
}