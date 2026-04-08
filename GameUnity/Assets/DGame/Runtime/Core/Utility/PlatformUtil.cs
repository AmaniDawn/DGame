namespace DGame
{
    public partial class Utility
    {
        public static class PlatformUtil
        {
            /// <summary>
            /// 退出游戏
            /// </summary>
            public static void Quit()
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else

#if !UNITY_WEBGL
                UnityEngine.Application.Quit();
#endif

#endif
            }

            /// <summary>
            /// PC平台或编辑器模式
            /// </summary>
            /// <returns></returns>
            public static bool IsPcOrEditorPlatform() => IsPcPlatform() || IsEditorPlatform();

            /// <summary>
            /// PC平台
            /// </summary>
            /// <returns></returns>
            public static bool IsPcPlatform() =>
#if UNITY_STANDALONE_WIN
                true;
#else
                false;
#endif

            /// <summary>
            /// 编辑器模式
            /// </summary>
            /// <returns></returns>
            public static bool IsEditorPlatform() =>
#if UNITY_EDITOR
                true;
#else
                false;
#endif

            /// <summary>
            /// Android平台
            /// </summary>
            /// <returns></returns>
            public static bool IsAndroidPlatform() =>
#if !UNITY_EDITOR && UNITY_ANDROID
                true;
#else
                false;
#endif

            /// <summary>
            /// IOS平台
            /// </summary>
            /// <returns></returns>
            public static bool IsIosPlatform() =>
#if !UNITY_EDITOR && UNITY_IPHONE
                true;
#else
                false;
#endif

            /// <summary>
            /// WebGL平台
            /// </summary>
            /// <returns></returns>
            public static bool IsWebGLPlatform() =>
#if UNITY_WEBGL
                true;
#else
                false;
#endif

            /// <summary>
            /// WebGL 模式
            /// </summary>
            /// <returns></returns>
            public static bool IsWebGLMode() =>
#if UNITY_INSTANT
                true;
#else
                IsWebGLPlatform();
#endif

            /// <summary>
            /// 小包版本
            /// </summary>
            /// <returns></returns>
            public static bool IsSmallPkg() =>
#if UNITY_INSTANT && !UNITY_WEBGL
                true;
#else
                false;
#endif

            /// <summary>
            /// 即时游戏
            /// </summary>
            /// <returns></returns>
            public static bool IsInstantGame() =>
#if UNITY_INSTANT
                true;
#else
                false;
#endif

            /// <summary>
            /// 设备振动
            /// </summary>
            public static void Vibrate()
            {
#if !UNITY_EDITOR
#if UNITY_ANDROID || UNITY_IPHONE
                UnityEngine.Handheld.Vibrate();
#elif UNITY_WEBGL
#endif
#endif
            }
        }
    }
}