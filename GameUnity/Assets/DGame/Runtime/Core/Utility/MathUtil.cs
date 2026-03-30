using System.Collections.Generic;
using UnityEngine;

namespace DGame
{
    public static partial class Utility
    {
        public static class MathUtil
        {
            private static readonly Dictionary<Camera, (Plane[] planes, int frame)> m_frustumCache = new();
            private static readonly Dictionary<Transform, Renderer> m_rendererCache = new();
            private static Camera m_mainCamera;

            static MathUtil()
            {
                CheckMainCamera();
            }

            #region 角度和弧度互转

            /// <summary>
            /// 角度转弧度 1°=0.017453弧度
            /// </summary>
            /// <param name="deg">角度</param>
            /// <returns></returns>
            public static float Deg2Rad(float deg) => deg * Mathf.Deg2Rad;

            /// <summary>
            /// 弧度转角度 1弧度=57.29578°
            /// </summary>
            /// <param name="rad">弧度</param>
            /// <returns></returns>
            public static float Rad2Deg(float rad) => rad * Mathf.Rad2Deg;

            #endregion

            #region 距离判断

            /// <summary>
            /// 获取 XZ 平面上 两点的距离
            /// </summary>
            /// <param name="a">a点</param>
            /// <param name="b">b点</param>
            /// <returns></returns>
            public static float DistanceXZ(Vector3 a, Vector3 b)
            {
                float dx = a.x - b.x;
                float dz = a.z - b.z;
                return Mathf.Sqrt(dx * dx + dz * dz);
            }

            /// <summary>
            /// 获取 XZ 平面上 两点的距离
            /// </summary>
            /// <param name="a">a点</param>
            /// <param name="b">b点</param>
            /// <returns></returns>
            public static float DistanceXZSqr(Vector3 a, Vector3 b)
            {
                float dx = a.x - b.x;
                float dz = a.z - b.z;
                return dx * dx + dz * dz;
            }

            /// <summary>
            /// 检查XZ平面上是否在指定范围内（使用平方距离优化）
            /// </summary>
            public static bool CheckIsInRangeXZ(Vector3 a, Vector3 b, float range)
            {
                return DistanceXZSqr(a, b) <= range * range;
            }

            /// <summary>
            /// 获取 XY 平面上 两点的距离
            /// </summary>
            /// <param name="a">a点</param>
            /// <param name="b">b点</param>
            /// <returns></returns>
            public static float DistanceXY(Vector3 a, Vector3 b)
            {
                float dx = a.x - b.x;
                float dy = a.y - b.y;
                return Mathf.Sqrt(dx * dx + dy * dy);
            }

            /// <summary>
            /// 获取 XY 平面上 两点的距离
            /// </summary>
            /// <param name="a">a点</param>
            /// <param name="b">b点</param>
            /// <returns></returns>
            public static float DistanceXYSqr(Vector3 a, Vector3 b)
            {
                float dx = a.x - b.x;
                float dy = a.y - b.y;
                return dx * dx + dy * dy;
            }

            /// <summary>
            /// 判断 XY 平面上的两点距离 是否小于等于目标距离
            /// </summary>
            /// <param name="a">a点</param>
            /// <param name="b">b点</param>
            /// <param name="range">目标距离</param>
            /// <returns></returns>
            public static bool CheckIsInRangeXY(Vector3 a, Vector3 b, float range)
            {
                return DistanceXYSqr(a, b) <= range * range;
            }

            #endregion

            #region 位置判断

            private static void CheckMainCamera()
            {
                if (m_mainCamera == null)
                {
                    m_mainCamera = Camera.main;
                }
            }

            /// <summary>
            /// 判断世界坐标的某个点 是否在屏幕可见范围外
            /// </summary>
            /// <param name="pos">世界坐标的点</param>
            /// <returns>可见范围内-false 可见范围外-true</returns>
            public static bool CheckWorldPosOutOfScreen(Vector3 pos)
            {
                CheckMainCamera();
                if (Camera.main != null)
                {
                    Vector3 viewportPos = Camera.main.WorldToViewportPoint(pos);

                    // 视口坐标：x 和 y 在 [0,1] 范围内表示在屏幕内
                    // z<0 表示在相机后面(近平面) z>1在远平面后
                    return viewportPos.x < 0 || viewportPos.x > 1 ||
                           viewportPos.y < 0 || viewportPos.y > 1 ||
                           viewportPos.z < 0 || viewportPos.z > 1;
                }

                return true;
            }

            /// <summary>
            /// 判断世界物体是否超出相机的远近平面
            /// </summary>
            /// <param name="worldPos">世界物体坐标</param>
            /// <param name="camera">相对相机</param>
            /// <returns></returns>
            public static bool CheckWorldObjectOutOfCameraView(Vector3 worldPos, Camera camera)
            {
                if (camera == null)
                {
                    return true;
                }

                Vector3 toCamera = worldPos - camera.transform.position;
                float sqrDistance = toCamera.sqrMagnitude;
                float nearSqr = camera.nearClipPlane * camera.nearClipPlane;
                float farSqr = camera.farClipPlane * camera.farClipPlane;

                if (sqrDistance < nearSqr || sqrDistance > farSqr)
                {
                    return true;
                }
                return false;
            }

            /// <summary>
            /// 判断世界物体是否在相机的视锥体内
            /// </summary>
            /// <param name="objTransform">世界物体</param>
            /// <param name="camera">相对相机</param>
            /// <returns></returns>
            public static bool CheckWorldObjectInCameraFrustum(Transform objTransform, Camera camera)
            {
                if (objTransform == null || camera == null)
                {
                    return false;
                }

                if (CheckWorldPosOutOfScreen(objTransform.position))
                {
                    return false;
                }

                // if (Time.frameCount % 600 == 0) // 每10秒（假设60FPS）
                // {
                //     var expiredCameras = m_frustumCache
                //         .Where(kvp => Time.frameCount - kvp.Value.frame > 300) // 5秒未使用
                //         .Select(kvp => kvp.Key)
                //         .ToList();
                //
                //     foreach (var c in expiredCameras)
                //     {
                //         m_frustumCache.Remove(camera);
                //     }
                // }

                // 获取或计算视锥体平面
                if (!m_frustumCache.TryGetValue(camera, out var cache) || cache.frame != Time.frameCount)
                {
                    cache.planes = GeometryUtility.CalculateFrustumPlanes(camera);
                    cache.frame = Time.frameCount;
                    m_frustumCache[camera] = cache;
                }
                // 获取 Renderer（带缓存）
                if (!m_rendererCache.TryGetValue(objTransform, out var renderer))
                {
                    renderer = objTransform.GetComponent<Renderer>();
                    m_rendererCache[objTransform] = renderer;

                    // 定期清理缓存（避免内存泄漏）
                    if (m_rendererCache.Count > 1000)
                    {
                        m_rendererCache.Clear();
                    }
                }

                return renderer != null && GeometryUtility.TestPlanesAABB(cache.planes, renderer.bounds);
            }

            public static void Clear()
            {
                m_frustumCache?.Clear();
                m_rendererCache?.Clear();
            }

            /// <summary>
            /// 判断点 B 是否在点 A 的扇形范围内 XZ 平面
            /// </summary>
            /// <param name="a">a点</param>
            /// <param name="forward">a点面朝向</param>
            /// <param name="b">b点</param>
            /// <param name="radius">扇形半径</param>
            /// <param name="angle">扇形角度</param>
            /// <returns>范围内-true 范围外-false</returns>
            public static bool CheckBIsInSectorRangeOfAAboutXZ(Vector3 a, Vector3 forward, Vector3 b, float radius, float angle)
            {
                // 计算XZ平面向量
                // Vector3 toB = new Vector3(b.x - a.x, 0, b.z - a.z);
                var toB_x = b.x - a.x;
                var toB_z = b.z - a.z;
                // 快速距离检查（平方距离）
                float sqrDistance = toB_x * toB_x + toB_z * toB_z;
                float radiusSqr = radius * radius;
                if (sqrDistance > radiusSqr)
                {
                    return false;
                }
                if (sqrDistance < 0.0001f)
                {
                    return true;
                }
                // Vector3 forwardXZ = new Vector3(forward.x, 0, forward.z);
                float forwardSqrLength = forward.x * forward.x + forward.z * forward.z;
                if (forwardSqrLength < 0.0001f)
                {
                    return false;
                }
                // 使用点积检查角度 dot = A · B = |A| * |B| * cosθ => cosθ = (A · B) / (|A| * |B|)
                float dot = forward.x * toB_x + forward.z * toB_z;
                float cosHalfAngle = Mathf.Cos(angle * 0.5f * Mathf.Deg2Rad);
                // |A| * |B|
                float productLength = Mathf.Sqrt(forwardSqrLength * sqrDistance);
                // (A · B) / (|A| * |B|) ≥ cos(半角) => A · B ≥ cos(半角) * |A| * |B|
                return dot >= cosHalfAngle * productLength;
                // a.y = 0;
                // forward.y = 0;
                // b.y = 0;
                // return CheckIsInRangeXZ(a, b, radius) && Vector3.Angle(forward, b - a) <= angle / 2f;
            }

            public static bool CheckBIsInSectorRangeOfAAboutXY(Vector3 a, Vector3 forward, Vector3 b, float radius, float angle)
            {
                // 计算XZ平面向量
                // Vector3 toB = new Vector3(b.x - a.x, b.y - a.y, 0);
                var toB_x = b.x - a.x;
                var toB_y = b.y - a.y;
                // 快速距离检查（平方距离）
                float sqrDistance = toB_x * toB_x + toB_y * toB_y;
                float radiusSqr = radius * radius;
                if (sqrDistance > radiusSqr)
                {
                    return false;
                }
                if (sqrDistance < 0.0001f)
                {
                    return true;
                }
                // Vector3 forwardXZ = new Vector3(forward.x, 0, forward.z);
                float forwardSqrLength = forward.x * forward.x + forward.y * forward.y;
                if (forwardSqrLength < 0.0001f)
                {
                    return false;
                }
                // 使用点积检查角度 dot = A · B = |A| * |B| * cosθ => cosθ = (A · B) / (|A| * |B|)
                float dot = forward.x * toB_x + forward.y * toB_y;
                float cosHalfAngle = Mathf.Cos(angle * 0.5f * Mathf.Deg2Rad);
                // |A| * |B|
                float productLength = Mathf.Sqrt(forwardSqrLength * sqrDistance);
                // (A · B) / (|A| * |B|) ≥ cos(半角) => A · B ≥ cos(半角) * |A| * |B|
                return dot >= cosHalfAngle * productLength;
                // a.y = 0;
                // forward.y = 0;
                // b.y = 0;
                // return CheckIsInRangeXZ(a, b, radius) && Vector3.Angle(forward, b - a) <= angle / 2f;
            }


            /// <summary>
            /// 判断点 B 是否在点 A 的扇形范围内 XZ 平面
            /// </summary>
            /// <param name="a">a点</param>
            /// <param name="forward">a点面朝向</param>
            /// <param name="b">b点</param>
            /// <param name="radius">扇形半径</param>
            /// <param name="leftAngle">扇形角度</param>
            /// <param name="rightAngle">扇形角度</param>
            /// <returns>范围内-true 范围外-false</returns>
            public static bool CheckBIsInSectorRangeOfAAboutXZ(Vector3 a, Vector3 forward, Vector3 b, float radius, float leftAngle, float rightAngle)
            {
                // 计算XZ平面向量
                // Vector3 toB = new Vector3(b.x - a.x, 0, b.z - a.z);
                var toB_x = b.x - a.x;
                var toB_z = b.z - a.z;
                // 快速距离检查（平方距离）
                float sqrDistance = toB_x * toB_x + toB_z * toB_z;
                float radiusSqr = radius * radius;
                if (sqrDistance > radiusSqr)
                {
                    return false;
                }
                if (sqrDistance < 0.0001f)
                {
                    return true;
                }
                // Vector3 forwardXZ = new Vector3(forward.x, 0, forward.z);
                float forwardSqrLength = forward.x * forward.x + forward.z * forward.z;
                if (forwardSqrLength < 0.0001f)
                {
                    return false;
                }
                // 使用点积检查角度 dot = A · B = |A| * |B| * cosθ => cosθ = (A · B) / (|A| * |B|)
                float dot = forward.x * toB_x + forward.z * toB_z;
                // 2D叉积
                float crossY = forward.x * toB_z - forward.z * toB_x;
                bool isOnRightSide = crossY < 0;

                if (isOnRightSide)
                {
                    float cosRightAngle = Mathf.Cos(rightAngle * Mathf.Deg2Rad);
                    // |A| * |B|
                    float productLength = Mathf.Sqrt(forwardSqrLength * sqrDistance);
                    // (A · B) / (|A| * |B|) ≥ cos(半角) => A · B ≥ cos(半角) * |A| * |B|
                    return dot >= cosRightAngle * productLength;
                }
                else
                {
                    float cosLeftAngle = Mathf.Cos(leftAngle * Mathf.Deg2Rad);
                    // |A| * |B|
                    float productLength = Mathf.Sqrt(forwardSqrLength * sqrDistance);
                    // (A · B) / (|A| * |B|) ≥ cos(半角) => A · B ≥ cos(半角) * |A| * |B|
                    return dot >= cosLeftAngle * productLength;
                }
                // a.y = 0;
                // forward.y = 0;
                // b.y = 0;
                // return CheckIsInRangeXZ(a, b, radius) && Vector3.Angle(forward, b - a) <= angle / 2f;
            }

             public static bool CheckBIsInSectorRangeOfAAboutXY(Vector3 a, Vector3 forward, Vector3 b, float radius, float leftAngle, float rightAngle)
            {
                // 计算XZ平面向量
                // Vector3 toB = new Vector3(b.x - a.x, b.y - a.y, 0);
                var toB_x = b.x - a.x;
                var toB_y = b.y - a.y;
                // 快速距离检查（平方距离）
                float sqrDistance = toB_x * toB_x + toB_y * toB_y;
                float radiusSqr = radius * radius;
                if (sqrDistance > radiusSqr)
                {
                    return false;
                }
                if (sqrDistance < 0.0001f)
                {
                    return true;
                }
                // Vector3 forwardXZ = new Vector3(forward.x, 0, forward.z);
                float forwardSqrLength = forward.x * forward.x + forward.y * forward.y;
                if (forwardSqrLength < 0.0001f)
                {
                    return false;
                }
                // 使用点积检查角度 dot = A · B = |A| * |B| * cosθ => cosθ = (A · B) / (|A| * |B|)
                float dot = forward.x * toB_x + forward.y * toB_y;
                // 2D叉积
                float crossY = forward.x * toB_y - forward.y * toB_x;
                bool isOnRightSide = crossY < 0;

                if (isOnRightSide)
                {
                    float cosRightAngle = Mathf.Cos(rightAngle * Mathf.Deg2Rad);
                    // |A| * |B|
                    float productLength = Mathf.Sqrt(forwardSqrLength * sqrDistance);
                    // (A · B) / (|A| * |B|) ≥ cos(半角) => A · B ≥ cos(半角) * |A| * |B|
                    return dot >= cosRightAngle * productLength;
                }
                else
                {
                    float cosLeftAngle = Mathf.Cos(leftAngle * Mathf.Deg2Rad);
                    // |A| * |B|
                    float productLength = Mathf.Sqrt(forwardSqrLength * sqrDistance);
                    // (A · B) / (|A| * |B|) ≥ cos(半角) => A · B ≥ cos(半角) * |A| * |B|
                    return dot >= cosLeftAngle * productLength;
                }
                // a.y = 0;
                // forward.y = 0;
                // b.y = 0;
                // return CheckIsInRangeXZ(a, b, radius) && Vector3.Angle(forward, b - a) <= angle / 2f;
            }

            #endregion

            #region 坐标转换

            /// <summary>
            /// 世界坐标转UI窗口相对坐标
            /// </summary>
            /// <param name="parentCanvas">UI父物体</param>
            /// <param name="worldPos">世界坐标</param>
            /// <param name="uiCamera">UI相机</param>
            /// <param name="offset">偏移位置</param>
            /// <returns>相对于UI父物体的坐标</returns>
            public static Vector3 WorldPointToUILocalPoint(RectTransform parentCanvas, Vector3 worldPos, Camera uiCamera, Vector3 offset = default)
            {
                CheckMainCamera();

                if (m_mainCamera == null || parentCanvas == null || uiCamera == null)
                {
                    return Vector3.zero;
                }
                var screenPoint = m_mainCamera.WorldToScreenPoint(worldPos + offset);
                // 将屏幕坐标转换为指定 Canvas 下的局部坐标
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parentCanvas, screenPoint, uiCamera, out var uiLocalPoint);
                return uiLocalPoint;
            }

            /// <summary>
            /// 获得UI物体跟随世界物体移动的近大远小缩放值
            /// </summary>
            /// <param name="worldPos">世界物体坐标</param>
            /// <param name="originalDistance">世界物体相对于主相机的初始距离</param>
            /// <param name="targetCamera">目标相机</param>
            /// <returns>近大远小的Scale缩放值</returns>
            public static float UIObjectFollowWorldObjectFactor(Vector3 worldPos, float originalDistance, Camera targetCamera = null)
            {
                if (targetCamera == null)
                {
                    CheckMainCamera();
                    targetCamera = m_mainCamera;
                }
                if (targetCamera == null)
                {
                    return 1f;
                }
                float currentDistance = MathUtil.DistanceXZ(targetCamera.transform.position, worldPos);
                if (currentDistance <= Mathf.Epsilon)
                {
                    return 1f;
                }
                return originalDistance / currentDistance;
            }

            /// <summary>
            /// 获得UI物体跟随世界物体移动的近大远小缩放值
            /// </summary>
            /// <param name="worldPos">世界物体坐标</param>
            /// <param name="originalDistance">世界物体相对于主相机的初始距离</param>
            /// <param name="maxScale">最大缩放</param>
            /// <param name="targetCamera">目标相机</param>
            /// <param name="minScale">最小缩放</param>
            /// <returns>近大远小的Scale缩放值</returns>
            public static float UIObjectFollowWorldObjectFactor(Vector3 worldPos, float originalDistance,
                float minScale = 0.1f, float maxScale = 10f, Camera targetCamera = null)
            {
                if (targetCamera == null)
                {
                    CheckMainCamera();
                    targetCamera = m_mainCamera;
                }

                if (targetCamera == null)
                {
                    return 1f;
                }

                if (minScale <= 0f)
                {
                    minScale = 0.1f;
                }

                float currentDistance = MathUtil.DistanceXZ(targetCamera.transform.position, worldPos);

                if (currentDistance <= Mathf.Epsilon)
                {
                    return 1f;
                }

                float scale = originalDistance / currentDistance;
                return Mathf.Clamp(scale, minScale, maxScale);
            }

            /// <summary>
            /// UI物体跟随世界物体移动
            /// </summary>
            /// <param name="uiObj">UI物体</param>
            /// <param name="canvas">UI物体的父级Canvas</param>
            /// <param name="targetObjPos">跟随的世界物体坐标值</param>
            /// <param name="uiCamera">UI相机</param>
            /// <param name="originalDistance">世界物体相对于主相机的初始距离</param>
            /// <param name="offset">偏移位置</param>
            /// <param name="followZoom">近大远小开关</param>
            /// <returns>是否成功</returns>
            public static bool UIObjectFollowWorldObject(RectTransform uiObj, RectTransform canvas,
                Vector3 targetObjPos, Camera uiCamera, float originalDistance, Vector3 offset = default,
                bool followZoom = false)
            {
                if (uiObj == null || canvas == null || uiCamera == null)
                {
                    return false;
                }

                // 计算跟随目标坐标
                var uiPos = WorldPointToUILocalPoint(canvas, targetObjPos, uiCamera, offset);

                if (followZoom)
                {
                    float zoomFactor = UIObjectFollowWorldObjectFactor(targetObjPos, originalDistance, uiCamera);
                    uiObj.localScale = Vector3.one * zoomFactor;
                }

                uiObj.localPosition = uiPos;

                return true;
            }

            /// <summary>
            /// UI物体跟随世界物体移动
            /// </summary>
            /// <param name="uiObj">UI物体</param>
            /// <param name="canvas">UI物体的父级Canvas</param>
            /// <param name="targetObjPos">跟随的世界物体坐标值</param>
            /// <param name="uiCamera">UI相机</param>
            /// <param name="originalDistance">世界物体相对于主相机的初始距离</param>
            /// <param name="offset">偏移位置</param>
            /// <param name="followZoom">近大远小开关</param>
            /// <param name="minScale">最小缩放</param>
            /// <param name="maxScale">最大缩放</param>
            /// <returns>是否成功</returns>
            public static bool UIObjectFollowWorldObject(RectTransform uiObj, RectTransform canvas,
                Vector3 targetObjPos, Camera uiCamera, float originalDistance, Vector3 offset = default,
                bool followZoom = false, float minScale = 0.1f, float maxScale = 10f)
            {
                if (uiObj == null || canvas == null || uiCamera == null)
                {
                    return false;
                }

                // 计算跟随目标坐标
                var uiPos = WorldPointToUILocalPoint(canvas, targetObjPos, uiCamera, offset);

                if (followZoom)
                {
                    if (minScale <= 0f)
                    {
                        minScale = 0.1f;
                    }
                    float zoomFactor = UIObjectFollowWorldObjectFactor(targetObjPos, originalDistance, minScale, maxScale, uiCamera);
                    uiObj.localScale = Vector3.one * zoomFactor;
                }

                uiObj.localPosition = uiPos;

                return true;
            }

            public static bool TryGetMouseDownUIPos(RectTransform uiRoot, Camera uiCamera, out Vector2 screenPos)
            {
                screenPos = Vector2.zero;

                if (uiRoot == null || uiCamera == null)
                {
                    return false;
                }
                Vector3 mousePos = Vector3.zero;

#if UNITY_EDITOR || PLATFORM_STANDALONE_WIN
                if (Input.GetMouseButton(0))
                {
                    mousePos = Input.mousePosition;
                }
                else
                {
                    return false;
                }
#else
                if (Input.touchCount > 0)
                {
                    mousePos = Input.GetTouch(0).position;
                }
                else
                {
                    return false;
                }
#endif
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(uiRoot, mousePos, uiCamera, out var localPoint))
                {
                    screenPos = uiRoot.transform.TransformPoint(localPoint);
                    return true;
                }
                return false;
            }

            #endregion
        }
    }
}