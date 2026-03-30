using UnityEngine;
using UnityEngine.Events;

namespace DGame
{
    public static partial class Utility
    {
        public static class PhysicsUtil
        {
            /// <summary>
            /// 进行盒装范围检测
            /// </summary>
            /// <typeparam name="T">想要获取的信息类型 可以填写 Collider GameObject 以及对象上依附的组件类型</typeparam>
            /// <param name="center">盒装中心点</param>
            /// <param name="results">碰撞体结果组</param>
            /// <param name="rotation">盒子的角度</param>
            /// <param name="halfExtents">长宽高的一半</param>
            /// <param name="layerMask">层级筛选</param>
            /// <param name="callBack">回调函数 </param>
            public static bool TryOverlapBox3D<T>(Vector3 center, Vector3 halfExtents, Collider[] results, Quaternion rotation, int layerMask, UnityAction<T> callBack = null) where T : class
            {
                int count = Physics.OverlapBoxNonAlloc(center, halfExtents, results, rotation, layerMask, QueryTriggerInteraction.Collide);
                if (count <= 0)
                {
                    return false;
                }
                return InternalTryOverlap3D(results, callBack, count);
            }

            /// <summary>
            /// 进行球体范围检测
            /// </summary>
            /// <typeparam name="T">想要获取的信息类型 可以填写 Collider GameObject 以及对象上依附的组件类型</typeparam>
            /// <param name="center">球体的中心点</param>
            /// <param name="radius">球体的半径</param>
            /// <param name="results">碰撞体结果组</param>
            /// <param name="layerMask">层级筛选</param>
            /// <param name="callBack">回调函数</param>
            public static bool TryOverlapSphere3D<T>(Vector3 center, float radius, Collider[] results, int layerMask, UnityAction<T> callBack = null) where T : class
            {
                var count = Physics.OverlapSphereNonAlloc(center, radius, results, layerMask, QueryTriggerInteraction.Collide);
                if (count <= 0)
                {
                    return false;
                }
                return InternalTryOverlap3D(results, callBack, count);
            }

            /// <summary>
            /// 进行椭圆体范围检测
            /// </summary>
            /// <typeparam name="T">想要获取的信息类型 可以填写 Collider GameObject 以及对象上依附的组件类型</typeparam>
            /// <param name="point0">第一个点</param>
            /// <param name="point1">第二个点</param>
            /// <param name="radius">半径</param>
            /// <param name="results">碰撞体结果组</param>
            /// <param name="layerMask">层级筛选</param>
            /// <param name="callBack">回调函数</param>
            public static bool TryOverlapCapsule3D<T>(Vector3 point0, Vector3 point1, float radius, Collider[] results, int layerMask, UnityAction<T> callBack = null) where T : class
            {
                var count = Physics.OverlapCapsuleNonAlloc(point0, point1, radius, results, layerMask, QueryTriggerInteraction.Collide);
                if (count <= 0)
                {
                    return false;
                }
                return InternalTryOverlap3D(results, callBack, count);
            }

            private static bool InternalTryOverlap3D<T>(Collider[] results, UnityAction<T> callBack, int count) where T : class
            {
                bool isCollider = typeof(T) == typeof(Collider);
                bool isGameObject = typeof(T) == typeof(GameObject);
                for (int i = 0; i < count; i++)
                {
                    Collider collider = results[i];
                    if (collider == null)
                    {
                        continue;
                    }
                    T item = null;

                    if (isCollider)
                    {
                        item = collider as T;
                    }
                    else if (isGameObject)
                    {
                        item = collider.gameObject as T;
                    }
                    else
                    {
                        if (collider.TryGetComponent<T>(out T component))
                        {
                            item = component;
                        }
                    }

                    if (item != null)
                    {
                        callBack?.Invoke(item);
                    }
                }
                return true;
            }

            /// <summary>
            /// 进行盒装范围检测
            /// </summary>
            /// <typeparam name="T">想要获取的信息类型 可以填写 Collider GameObject 以及对象上依附的组件类型</typeparam>
            /// <param name="center">盒装中心点</param>
            /// <param name="angle">盒子的角度</param>
            /// <param name="size">长宽高的一半</param>
            /// <param name="results"></param>
            /// <param name="callBack">回调函数 </param>
            /// <param name="filter2D">筛选条件</param>
#if UNITY_6000_0_OR_NEWER

            public static bool TryOverlapBox2DNonAlloc<T>(Vector2 center, Vector2 size, float angle, ContactFilter2D filter2D, Collider2D[] results, UnityAction<T> callBack = null) where T : class
            {
                var count = Physics2D.OverlapBox(center, size, angle, filter2D, results);
                if (count <= 0)
                {
                    return false;
                }
                return InternalTryOverlap2DNonAlloc(count, results, callBack);
            }
#else
            public static bool TryOverlapBox2DNonAlloc<T>(Vector2 center, Vector2 size, float angle, Collider2D[] results, int filter2D, UnityAction<T> callBack = null) where T : class
            {
                var count = Physics2D.OverlapBoxNonAlloc(center, size, angle, results, filter2D);
                if (count <= 0)
                {
                    return false;
                }
                return InternalTryOverlap2DNonAlloc(count, results, callBack);
            }
#endif

            /// <summary>
            /// 进行球体范围检测
            /// </summary>
            /// <typeparam name="T">想要获取的信息类型 可以填写 Collider GameObject 以及对象上依附的组件类型</typeparam>
            /// <param name="center">球体的中心点</param>
            /// <param name="radius">球体的半径</param>
            /// <param name="results"></param>
            /// <param name="filter2D">筛选条件</param>
            /// <param name="callBack">回调函数</param>
#if UNITY_6000_0_OR_NEWER
            public static bool TryOverlapCircle2DNonAlloc<T>(Vector2 center, float radius, ContactFilter2D filter2D, Collider2D[] results, UnityAction<T> callBack = null) where T : class
            {
                var count = Physics2D.OverlapCircle(center, radius, filter2D, results);
                if (count <= 0)
                {
                    return false;
                }
                return InternalTryOverlap2DNonAlloc(count, results, callBack);
            }
#else
            public static bool TryOverlapCircle2DNonAlloc<T>(Vector2 center, float radius, Collider2D[] results, int filter2D, UnityAction<T> callBack = null) where T : class
            {
                var count = Physics2D.OverlapCircleNonAlloc(center, radius, results, filter2D);
                if (count <= 0)
                {
                    return false;
                }
                return InternalTryOverlap2DNonAlloc(count, results, callBack);
            }
#endif

            /// <summary>
            /// 进行区域范围检测
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="point0">左上角的点</param>
            /// <param name="point1">右下角的点</param>
            /// <param name="results"></param>
            /// <param name="filter2D">筛选条件</param>
            /// <param name="callBack"></param>
            /// <returns></returns>
#if UNITY_6000_0_OR_NEWER
            public static bool TryOverlapCircle2DNonAlloc<T>(Vector2 point0, Vector2 point1, ContactFilter2D filter2D, Collider2D[] results, UnityAction<T> callBack = null) where T : class
            {
                var count = Physics2D.OverlapArea(point0, point1, filter2D, results);
                if (count <= 0)
                {
                    return false;
                }
                return InternalTryOverlap2DNonAlloc(count, results, callBack);
            }
#else
            public static bool TryOverlapArea2DNonAlloc<T>(Vector2 point0, Vector2 point1, Collider2D[] results, int filter2D, UnityAction<T> callBack = null) where T : class
            {
                var count = Physics2D.OverlapAreaNonAlloc(point0, point1, results, filter2D);
                if (count <= 0)
                {
                    return false;
                }

                return InternalTryOverlap2DNonAlloc(count, results, callBack);
            }
#endif

            /// <summary>
            /// 进行点检测
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="point0">点</param>
            /// <param name="results"></param>
            /// <param name="filter2D">筛选条件</param>
            /// <param name="callBack"></param>
            /// <returns></returns>
#if UNITY_6000_0_OR_NEWER
            public static bool TryOverlapCircle2DNonAlloc<T>(Vector2 point0, ContactFilter2D filter2D, Collider2D[] results, UnityAction<T> callBack = null) where T : class
            {
                var count = Physics2D.OverlapPoint(point0, filter2D, results);
                if (count <= 0)
                {
                    return false;
                }
                return InternalTryOverlap2DNonAlloc(count, results, callBack);
            }
#else
            public static bool TryOverlapPoint2DNonAlloc<T>(Vector2 point0, Collider2D[] results, int filter2D, UnityAction<T> callBack = null) where T : class
            {
                var count = Physics2D.OverlapPointNonAlloc(point0, results, filter2D);
                if (count <= 0)
                {
                    return false;
                }
                return InternalTryOverlap2DNonAlloc(count, results, callBack);
            }
#endif

            private static bool InternalTryOverlap2DNonAlloc<T>(int count, Collider2D[] results, UnityAction<T> callBack)
                where T : class
            {
                bool isCollider = typeof(T) == typeof(Collider);
                bool isGameObject = typeof(T) == typeof(GameObject);
                for (int i = 0; i < count; i++)
                {
                    Collider2D collider = results[i];
                    if (collider == null)
                    {
                        continue;
                    }
                    T item = null;

                    if (isCollider)
                    {
                        item = collider as T;
                    }
                    else if (isGameObject)
                    {
                        item = collider.gameObject as T;
                    }
                    else
                    {
                        if (collider.TryGetComponent<T>(out T component))
                        {
                            item = component;
                        }
                    }

                    if (item != null)
                    {
                        callBack?.Invoke(item);
                    }
                }
                return true;
            }

            /// <summary>
            /// 进行盒装范围检测
            /// </summary>
            /// <typeparam name="T">想要获取的信息类型 可以填写 Collider GameObject 以及对象上依附的组件类型</typeparam>
            /// <param name="center">盒装中心点</param>
            /// <param name="angle">盒子的角度</param>
            /// <param name="size">长宽高的一半</param>
            /// <param name="layerMask">层级筛选</param>
            /// <param name="callBack">回调函数 </param>
            public static bool TryOverlapBox2D<T>(Vector2 center, Vector2 size, float angle, int layerMask, UnityAction<T> callBack = null) where T : class
            {
                var coll = Physics2D.OverlapBox(center, size, angle, layerMask);
                return InternalTryOverlap2D(coll, callBack);
            }

            /// <summary>
            /// 进行球体范围检测
            /// </summary>
            /// <typeparam name="T">想要获取的信息类型 可以填写 Collider GameObject 以及对象上依附的组件类型</typeparam>
            /// <param name="center">球体的中心点</param>
            /// <param name="radius">球体的半径</param>
            /// <param name="layerMask">层级筛选</param>
            /// <param name="callBack">回调函数</param>
            public static bool TryOverlapCircle2D<T>(Vector2 center, float radius, int layerMask, UnityAction<T> callBack = null) where T : class
            {
                var coll = Physics2D.OverlapCircle(center, radius, layerMask);
                return InternalTryOverlap2D(coll, callBack);
            }

            /// <summary>
            /// 进行点检测
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="point0">点</param>
            /// <param name="layerMask">层级筛选</param>
            /// <param name="callBack"></param>
            /// <returns></returns>
            public static bool TryOverlapPoint2D<T>(Vector2 point0, int layerMask, UnityAction<T> callBack = null) where T : class
            {
                Collider2D coll = Physics2D.OverlapPoint(point0, layerMask);
                return InternalTryOverlap2D(coll, callBack);
            }

            /// <summary>
            /// 进行区域范围检测
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="point0">左上角的点</param>
            /// <param name="point1">右下角的点</param>
            /// <param name="layerMask">层级筛选</param>
            /// <param name="callBack"></param>
            /// <returns></returns>
            public static bool TryOverlapArea2D<T>(Vector2 point0, Vector2 point1, int layerMask, UnityAction<T> callBack = null) where T : class
            {
                Collider2D coll = Physics2D.OverlapArea(point0, point1, layerMask);
                return InternalTryOverlap2D(coll, callBack);
            }

            private static bool InternalTryOverlap2D<T>(Collider2D result, UnityAction<T> callBack)
                where T : class
            {
                if (result == null)
                {
                    return false;
                }
                bool isCollider = typeof(T) == typeof(Collider);
                bool isGameObject = typeof(T) == typeof(GameObject);
                T item = null;

                if (isCollider)
                {
                    item = result as T;
                }
                else if (isGameObject)
                {
                    item = result.gameObject as T;
                }
                else
                {
                    if (result.TryGetComponent<T>(out T component))
                    {
                        item = component;
                    }
                }

                if (item != null)
                {
                    callBack?.Invoke(item);
                }
                return true;
            }
        }
    }
}