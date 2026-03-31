using DGame;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 战斗表现层使用的定点与 Unity 常用数学类型转换工具。
    /// </summary>
    public static class BattleHelper
    {
        /// <summary>
        /// 将定点三维向量转换为 Unity 的 <see cref="Vector3"/>。
        /// </summary>
        /// <param name="value">定点三维向量。</param>
        /// <returns>转换后的 Unity 三维向量。</returns>
        public static Vector3 FpToVector(FixedPointVector3 value)
            => value.ToVector3();

        /// <summary>
        /// 将 Unity 的 <see cref="Vector3"/> 转换为定点三维向量。
        /// </summary>
        /// <param name="value">Unity 三维向量。</param>
        /// <returns>转换后的定点三维向量。</returns>
        public static FixedPointVector3 VectorToFp(Vector3 value)
            => new FixedPointVector3(value.x, value.y, value.z);

        /// <summary>
        /// 将定点四元数转换为 Unity 的 <see cref="Quaternion"/>。
        /// </summary>
        /// <param name="value">定点四元数。</param>
        /// <returns>转换后的 Unity 四元数。</returns>
        public static Quaternion FpToQuaternion(FixedPointQuaternion value)
            => value.ToQuaternion();

        /// <summary>
        /// 将 Unity 的 <see cref="Quaternion"/> 转换为定点四元数。
        /// </summary>
        /// <param name="value">Unity 四元数。</param>
        /// <returns>转换后的定点四元数。</returns>
        public static FixedPointQuaternion QuaternionToFp(Quaternion value)
            => new FixedPointQuaternion(value.x, value.y, value.z, value.w);

        /// <summary>
        /// 将定点二维向量转换为 Unity 的 <see cref="Vector2"/>。
        /// </summary>
        /// <param name="value">定点二维向量。</param>
        /// <returns>转换后的 Unity 二维向量。</returns>
        public static Vector2 FpToVector(FixedPointVector2 value)
            => new Vector2(value.x.AsFloat(), value.y.AsFloat());

        /// <summary>
        /// 将 Unity 的 <see cref="Vector2"/> 转换为定点二维向量。
        /// </summary>
        /// <param name="value">Unity 二维向量。</param>
        /// <returns>转换后的定点二维向量。</returns>
        public static FixedPointVector2 VectorToFp(Vector2 value)
            => new FixedPointVector2(value.x, value.y);
    }
}