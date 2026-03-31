using DGame;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 定点类型与 Unity 常用数学类型之间的扩展转换方法。
    /// </summary>
    public static class FixedPointExtension
    {
        /// <summary>
        /// 将 Unity 的 <see cref="Vector3"/> 转换为定点三维向量。
        /// </summary>
        /// <param name="value">Unity 三维向量。</param>
        /// <returns>转换后的定点三维向量。</returns>
        public static FixedPointVector3 ToFpVector(this Vector3 value)
            => BattleHelper.VectorToFp(value);

        /// <summary>
        /// 将定点四元数转换为 Unity 的 <see cref="Quaternion"/>。
        /// </summary>
        /// <param name="value">定点四元数。</param>
        /// <returns>转换后的 Unity 四元数。</returns>
        public static Quaternion ToQuaternion(this FixedPointQuaternion value)
            => BattleHelper.FpToQuaternion(value);

        /// <summary>
        /// 将 Unity 的 <see cref="Quaternion"/> 转换为定点四元数。
        /// </summary>
        /// <param name="value">Unity 四元数。</param>
        /// <returns>转换后的定点四元数。</returns>
        public static FixedPointQuaternion ToFpQuaternion(this Quaternion value)
            => BattleHelper.QuaternionToFp(value);

        /// <summary>
        /// 将定点二维向量转换为 Unity 的 <see cref="Vector2"/>。
        /// </summary>
        /// <param name="value">定点二维向量。</param>
        /// <returns>转换后的 Unity 二维向量。</returns>
        public static Vector2 ToVector2(this FixedPointVector2 value)
            => BattleHelper.FpToVector(value);

        /// <summary>
        /// 将 Unity 的 <see cref="Vector2"/> 转换为定点二维向量。
        /// </summary>
        /// <param name="value">Unity 二维向量。</param>
        /// <returns>转换后的定点二维向量。</returns>
        public static FixedPointVector2 ToFpVector(this Vector2 value)
            => BattleHelper.VectorToFp(value);
    }
}