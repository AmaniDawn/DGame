using UnityEngine;

namespace GameLogic
{
    public static partial class Utility
    {
        /// <summary>
        /// 提供 Transform 相关的辅助方法。
        /// </summary>
        public class TransformUtil
        {
            /// <summary>
            /// 将指定 <see cref="Transform"/> 的本地位置、旋转和缩放重置为默认值。
            /// </summary>
            /// <param name="trans">需要重置的 Transform 对象。</param>
            public static void ResetLocalPosScaleRot(Transform trans)
            {
                trans.localPosition = Vector3.zero;
                trans.localRotation = Quaternion.identity;
                trans.localScale = Vector3.one;
            }

            /// <summary>
            /// 将指定 <see cref="Transform"/> 的本地位置和本地缩放重置为默认值。
            /// </summary>
            /// <param name="trans">需要重置的 Transform 对象。</param>
            public static void ResetLocalPosScale(Transform trans)
            {
                trans.localPosition = Vector3.zero;
                trans.localScale = Vector3.one;
            }

            /// <summary>
            /// 设置指定 <see cref="Transform"/> 的本地缩放。
            /// </summary>
            /// <param name="trans">目标 Transform 对象。</param>
            /// <param name="x">本地缩放的 X 分量。</param>
            /// <param name="y">本地缩放的 Y 分量。</param>
            /// <param name="z">本地缩放的 Z 分量。</param>
            public static void SetLocalScale(Transform trans, float x, float y, float z)
            {
                trans.localScale = new Vector3(x, y, z);
            }

            /// <summary>
            /// 设置指定 <see cref="GameObject"/> 的本地缩放。
            /// </summary>
            /// <param name="go">目标 GameObject 对象。</param>
            /// <param name="x">本地缩放的 X 分量。</param>
            /// <param name="y">本地缩放的 Y 分量。</param>
            /// <param name="z">本地缩放的 Z 分量。</param>
            public static void SetLocalScale(GameObject go, float x, float y, float z)
            {
                go.transform.localScale = new Vector3(x, y, z);
            }

            /// <summary>
            /// 设置指定 <see cref="Transform"/> 的本地位置。
            /// </summary>
            /// <param name="trans">目标 Transform 对象。</param>
            /// <param name="x">本地位置的 X 分量。</param>
            /// <param name="y">本地位置的 Y 分量。</param>
            /// <param name="z">本地位置的 Z 分量。</param>
            public static void SetLocalPosition(Transform trans, float x, float y, float z)
            {
                trans.localPosition = new Vector3(x, y, z);
            }

            /// <summary>
            /// 设置指定 <see cref="Transform"/> 的世界坐标位置。
            /// </summary>
            /// <param name="trans">目标 Transform 对象。</param>
            /// <param name="x">世界坐标位置的 X 分量。</param>
            /// <param name="y">世界坐标位置的 Y 分量。</param>
            /// <param name="z">世界坐标位置的 Z 分量。</param>
            public static void SetPosition(Transform trans, float x, float y, float z)
            {
                trans.position = new Vector3(x, y, z);
            }

            /// <summary>
            /// 将指定 <see cref="Transform"/> 的世界坐标位置设置为源 <see cref="Transform"/> 的位置。
            /// </summary>
            /// <param name="trans">目标 Transform 对象。</param>
            /// <param name="src">源 Transform 对象。</param>
            public static void SetPosition(Transform trans, Transform src)
            {
                trans.position = src.position;
            }

            /// <summary>
            /// 设置指定 <see cref="Transform"/> 的 forward 方向。
            /// </summary>
            /// <param name="trans">目标 Transform 对象。</param>
            /// <param name="x">forward 向量的 X 分量。</param>
            /// <param name="y">forward 向量的 Y 分量。</param>
            /// <param name="z">forward 向量的 Z 分量。</param>
            public static void SetForward(Transform trans, float x, float y, float z)
            {
                trans.forward = new Vector3(x, y, z);
            }

            /// <summary>
            /// 将指定 <see cref="Transform"/> 的世界旋转设置为源 <see cref="Transform"/> 的旋转。
            /// </summary>
            /// <param name="trans">目标 Transform 对象。</param>
            /// <param name="srcTrans">源 Transform 对象。</param>
            public static void SetRotation(Transform trans, Transform srcTrans)
            {
                trans.rotation = srcTrans.rotation;
            }

            /// <summary>
            /// 将指定 <see cref="Transform"/> 的本地旋转设置为源 <see cref="Transform"/> 的本地旋转。
            /// </summary>
            /// <param name="trans">目标 Transform 对象。</param>
            /// <param name="srcTrans">源 Transform 对象。</param>
            public static void SetLocalRotation(Transform trans, Transform srcTrans)
            {
                trans.localRotation = srcTrans.localRotation;
            }

            /// <summary>
            /// 将指定 <see cref="Transform"/> 的本地旋转重置为默认值。
            /// </summary>
            /// <param name="tran">需要重置本地旋转的 Transform 对象。</param>
            public static void ResetLocalRotation(Transform tran)
            {
                tran.localRotation = Quaternion.identity;
            }
        }
    }

    public static class TransformExtensions
    {
        /// <summary>
        /// 将指定 <see cref="Transform"/> 的本地位置、旋转和缩放重置为默认值。
        /// </summary>
        /// <param name="trans">需要重置的 Transform 对象。</param>
        public static void ResetLocalPosScaleRot(this Transform trans)
            => Utility.TransformUtil.ResetLocalPosScaleRot(trans);

        /// <summary>
        /// 将指定 <see cref="Transform"/> 的本地位置和本地缩放重置为默认值。
        /// </summary>
        /// <param name="trans">需要重置的 Transform 对象。</param>
        public static void ResetLocalPosScale(this Transform trans)
            => Utility.TransformUtil.ResetLocalPosScale(trans);

        /// <summary>
        /// 设置指定 <see cref="Transform"/> 的本地缩放。
        /// </summary>
        /// <param name="trans">目标 Transform 对象。</param>
        /// <param name="x">本地缩放的 X 分量。</param>
        /// <param name="y">本地缩放的 Y 分量。</param>
        /// <param name="z">本地缩放的 Z 分量。</param>
        public static void SetLocalScale(this Transform trans, float x, float y, float z)
            => Utility.TransformUtil.SetLocalScale(trans, x, y, z);

        /// <summary>
        /// 设置指定 <see cref="GameObject"/> 的本地缩放。
        /// </summary>
        /// <param name="go">目标 GameObject 对象。</param>
        /// <param name="x">本地缩放的 X 分量。</param>
        /// <param name="y">本地缩放的 Y 分量。</param>
        /// <param name="z">本地缩放的 Z 分量。</param>
        public static void SetLocalScale(GameObject go, float x, float y, float z)
            => Utility.TransformUtil.SetLocalScale(go, x, y, z);

        /// <summary>
        /// 设置指定 <see cref="Transform"/> 的本地位置。
        /// </summary>
        /// <param name="trans">目标 Transform 对象。</param>
        /// <param name="x">本地位置的 X 分量。</param>
        /// <param name="y">本地位置的 Y 分量。</param>
        /// <param name="z">本地位置的 Z 分量。</param>
        public static void SetLocalPosition(Transform trans, float x, float y, float z)
            => Utility.TransformUtil.SetLocalPosition(trans, x, y, z);

        /// <summary>
        /// 设置指定 <see cref="Transform"/> 的世界坐标位置。
        /// </summary>
        /// <param name="trans">目标 Transform 对象。</param>
        /// <param name="x">世界坐标位置的 X 分量。</param>
        /// <param name="y">世界坐标位置的 Y 分量。</param>
        /// <param name="z">世界坐标位置的 Z 分量。</param>
        public static void SetPosition(Transform trans, float x, float y, float z)
            => Utility.TransformUtil.SetPosition(trans, x, y, z);

        /// <summary>
        /// 将指定 <see cref="Transform"/> 的世界坐标位置设置为源 <see cref="Transform"/> 的位置。
        /// </summary>
        /// <param name="trans">目标 Transform 对象。</param>
        /// <param name="src">源 Transform 对象。</param>
        public static void SetPosition(Transform trans, Transform src)
            => Utility.TransformUtil.SetPosition(trans, src);

        /// <summary>
        /// 设置指定 <see cref="Transform"/> 的 forward 方向。
        /// </summary>
        /// <param name="trans">目标 Transform 对象。</param>
        /// <param name="x">forward 向量的 X 分量。</param>
        /// <param name="y">forward 向量的 Y 分量。</param>
        /// <param name="z">forward 向量的 Z 分量。</param>
        public static void SetForward(Transform trans, float x, float y, float z)
            => Utility.TransformUtil.SetForward(trans, x, y, z);

        /// <summary>
        /// 将指定 <see cref="Transform"/> 的世界旋转设置为源 <see cref="Transform"/> 的旋转。
        /// </summary>
        /// <param name="trans">目标 Transform 对象。</param>
        /// <param name="srcTrans">源 Transform 对象。</param>
        public static void SetRotation(Transform trans, Transform srcTrans)
            => Utility.TransformUtil.SetRotation(trans, srcTrans);

        /// <summary>
        /// 将指定 <see cref="Transform"/> 的本地旋转设置为源 <see cref="Transform"/> 的本地旋转。
        /// </summary>
        /// <param name="trans">目标 Transform 对象。</param>
        /// <param name="srcTrans">源 Transform 对象。</param>
        public static void SetLocalRotation(Transform trans, Transform srcTrans)
            => Utility.TransformUtil.SetLocalRotation(trans, srcTrans);

        /// <summary>
        /// 将指定 <see cref="Transform"/> 的本地旋转重置为默认值。
        /// </summary>
        /// <param name="trans">需要重置本地旋转的 Transform 对象。</param>
        public static void ResetLocalRotation(this Transform trans)
            => Utility.TransformUtil.ResetLocalRotation(trans);
    }
}