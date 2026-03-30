using UnityEngine;

namespace DGame
{
    public partial class DebuggerDriver
    {
        private class QualityInformationWindow : ScrollableDebuggerWindowBase
        {
            private bool m_applyExpensiveChanges = false;

            protected override void OnDrawScrollableWindow()
            {
                DrawSectionTitle("Quality Level");
                BeginPanel();
                {
                    // 切换图形质量
                    int currentQualityLevel = QualitySettings.GetQualityLevel();

                    DrawItem("Current Quality Level", QualitySettings.names[currentQualityLevel], "当前图形质量等级");

                    GUILayout.Space(8);

                    // 图形质量切换是否立即生效
                    m_applyExpensiveChanges = GUILayout.Toggle(m_applyExpensiveChanges,
                        "  Apply expensive changes on quality level change.", DebuggerStyles.ToggleStyle);

                    GUILayout.Space(8);

                    // 质量等级选择网格
                    int newQualityLevel = GUILayout.SelectionGrid(currentQualityLevel, QualitySettings.names, 3,
                        DebuggerStyles.ToolbarButtonStyle, GUILayout.Height(DebuggerStyles.TabHeight * Mathf.CeilToInt(QualitySettings.names.Length / 3f)));

                    if (newQualityLevel != currentQualityLevel)
                    {
                        QualitySettings.SetQualityLevel(newQualityLevel, m_applyExpensiveChanges);
                    }
                }
                EndPanel();

                DrawSectionTitle("Rendering Information");
                BeginPanel();
                {
                    ResetRowIndex();
                    DrawItem("Active Color Space", QualitySettings.activeColorSpace.ToString(), "当前的颜色空间（Gamma/Linear）");
                    DrawItem("Desired Color Space", QualitySettings.desiredColorSpace.ToString(), "期望的颜色空间（Gamma/Linear）");
                    DrawItem("Max Queued Frames", QualitySettings.maxQueuedFrames.ToString(), "最大排队帧数");
                    DrawItem("Pixel Light Count", QualitySettings.pixelLightCount.ToString(), "像素光数量");
#if UNITY_2022_1_OR_NEWER
                    DrawItem("Master Texture Limit", QualitySettings.globalTextureMipmapLimit.ToString(), "全局纹理Mipmap限制");
#else
                    DrawItem("Master Texture Limit", QualitySettings.masterTextureLimit.ToString(), "全局纹理Mipmap限制");
#endif
                    DrawItem("Anisotropic Filtering", QualitySettings.anisotropicFiltering.ToString(), "各向异性过滤");
                    DrawItem("Anti Aliasing", QualitySettings.antiAliasing.ToString(), "抗锯齿级别");
#if UNITY_5_5_OR_NEWER
                    DrawItem("Soft Particles", QualitySettings.softParticles.ToString(), "软粒子");
#endif
                    DrawItem("Soft Vegetation", QualitySettings.softVegetation.ToString(), "软植被");
                    DrawItem("Realtime Reflection Probes", QualitySettings.realtimeReflectionProbes.ToString(), "实时反射探针");
                    DrawItem("Billboards Face Camera Position", QualitySettings.billboardsFaceCameraPosition.ToString(), "广告牌是否面向相机位置");
                }
                EndPanel();

#if UNITY_2018_2_OR_NEWER
                DrawSectionTitle("Texture Streaming");
                BeginPanel();
                {
                    ResetRowIndex();
                    DrawItem("Texture Streaming Enabled", QualitySettings.streamingMipmapsActive.ToString(), "纹理流式加载是否启用");
                    DrawItem("Texture Streaming Add All Cameras", QualitySettings.streamingMipmapsAddAllCameras.ToString(), "是否为所有相机启用纹理流式加载");
                    DrawItem("Texture Streaming Memory Budget", QualitySettings.streamingMipmapsMemoryBudget.ToString(), "纹理流式加载内存预算（MB）");
                    DrawItem("Texture Streaming Renderers Per Frame", QualitySettings.streamingMipmapsRenderersPerFrame.ToString(), "每帧处理的渲染器数量");
                    DrawItem("Texture Streaming Max Level Reduction", QualitySettings.streamingMipmapsMaxLevelReduction.ToString(), "最大mip级别减少量");
                    DrawItem("Texture Streaming Max File IO Requests", QualitySettings.streamingMipmapsMaxFileIORequests.ToString(), "最大文件IO请求数量");
                }
                EndPanel();
#endif

                DrawSectionTitle("Shadows Information");
                BeginPanel();
                {
                    ResetRowIndex();
#if UNITY_2017_1_OR_NEWER
                    DrawItem("Shadowmask Mode", QualitySettings.shadowmaskMode.ToString(), "阴影遮罩模式");
#endif
#if UNITY_5_5_OR_NEWER
                    DrawItem("Shadow Quality", QualitySettings.shadows.ToString(), "阴影质量级别");
#endif
#if UNITY_5_4_OR_NEWER
                    DrawItem("Shadow Resolution", QualitySettings.shadowResolution.ToString(), "阴影分辨率");
#endif
                    DrawItem("Shadow Projection", QualitySettings.shadowProjection.ToString(), "阴影投影方式");
                    DrawItem("Shadow Distance", QualitySettings.shadowDistance.ToString(), "阴影渲染距离");
                    DrawItem("Shadow Near Plane Offset", QualitySettings.shadowNearPlaneOffset.ToString(), "阴影近平面偏移");
                    DrawItem("Shadow Cascades", QualitySettings.shadowCascades.ToString(), "阴影级联数量");
                    DrawItem("Shadow Cascade 2 Split", QualitySettings.shadowCascade2Split.ToString(), "2级级联分割比例");
                    DrawItem("Shadow Cascade 4 Split", QualitySettings.shadowCascade4Split.ToString(), "4级级联分割比例");
                }
                EndPanel();

                DrawSectionTitle("Other Information");
                BeginPanel();
                {
                    ResetRowIndex();
#if UNITY_2019_1_OR_NEWER
                    DrawItem("Skin Weights", QualitySettings.skinWeights.ToString(), "蒙皮权重数量");
#endif
                    DrawItem("VSync Count", QualitySettings.vSyncCount.ToString(), "垂直同步");
                    DrawItem("LOD Bias", QualitySettings.lodBias.ToString(), "LOD偏差");
                    DrawItem("Maximum LOD Level", QualitySettings.maximumLODLevel.ToString(), "最大LOD级别");
                    DrawItem("Particle Raycast Budget", QualitySettings.particleRaycastBudget.ToString(), "粒子射线检测预算");
                    DrawItem("Async Upload Time Slice", Utility.StringUtil.Format("{0} ms", QualitySettings.asyncUploadTimeSlice), "异步上传时间片");
                    DrawItem("Async Upload Buffer Size", Utility.StringUtil.Format("{0} MB", QualitySettings.asyncUploadBufferSize), "异步上传缓冲区大小（MB）");
#if UNITY_2018_3_OR_NEWER
                    DrawItem("Async Upload Persistent Buffer", QualitySettings.asyncUploadPersistentBuffer.ToString(), "异步上传持久化缓冲区");
#endif
                }
                EndPanel();
            }
        }
    }
}