namespace GameLogic
{
    public enum ModelType
    {
        /// <summary> 普通模态 </summary>
        NormalType,

        /// <summary> 透明模态 </summary>
        TransparentType,

        /// <summary> 普通状态透明度的75% </summary>
        NormalType75,

        /// <summary> 浅色普通状态且有关闭功能 </summary>
        UndertintHaveClose,

        /// <summary> 普通状态且有关闭功能 </summary>
        NormalHaveClose,

        /// <summary> 透明状态且有关闭功能 </summary>
        TransparentHaveClose,

        /// <summary> 非模态 </summary>
        NoneType,
    }
}