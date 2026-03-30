using UnityEngine;

namespace DGame
{
    public partial class DebuggerDriver
    {
        private class GraphicsInformationWindow : ScrollableDebuggerWindowBase
        {
            protected override void OnDrawScrollableWindow()
            {
                DrawSectionTitle("Graphics Device");
                BeginPanel();
                {
                    DrawItem("Device ID", SystemInfo.graphicsDeviceID.ToString(), "图形设备ID");
                    DrawItem("Device Name", SystemInfo.graphicsDeviceName, "显卡型号名称");
                    DrawItem("Device Vendor ID", SystemInfo.graphicsDeviceVendorID.ToString(), "显卡厂商ID");
                    DrawItem("Device Vendor", SystemInfo.graphicsDeviceVendor, "显卡厂商名称");
                    DrawItem("Device Type", SystemInfo.graphicsDeviceType.ToString(), "设备类型（DirectX/OpenGL等）");
                    DrawItem("Device Version", SystemInfo.graphicsDeviceVersion, "图形API版本");
                    DrawItem("Memory Size", Utility.StringUtil.Format("{0} MB", SystemInfo.graphicsMemorySize), "显存大小（MB）");
                    DrawItem("Multi Threaded", SystemInfo.graphicsMultiThreaded.ToString(), "是否支持多线程渲染");
#if UNITY_2019_3_OR_NEWER
                    DrawItem("Rendering Threading Mode", SystemInfo.renderingThreadingMode.ToString(), "渲染线程模式");
#endif
                }
                EndPanel();

                DrawSectionTitle("Shader & Rendering");
                BeginPanel();
                {
                    ResetRowIndex();
#if UNITY_2020_1_OR_NEWER
                    DrawItem("HRD Display Support Flags", SystemInfo.hdrDisplaySupportFlags.ToString(), "HDR显示支持标志");
#endif
                    DrawItem("Shader Level", GetShaderLevelString(SystemInfo.graphicsShaderLevel), "Shader模型级别");
                    DrawItem("Global Maximum LOD", Shader.globalMaximumLOD.ToString(), "全局细节级别");
#if UNITY_5_6_OR_NEWER
                    DrawItem("Global Render Pipeline", Shader.globalRenderPipeline, "全局渲染管线");
#endif
#if UNITY_2020_2_OR_NEWER
                    DrawItem("Min OpenGLES Version", Graphics.minOpenGLESVersion.ToString(), "最小OpenGL ES版本");
#endif
#if UNITY_5_5_OR_NEWER
                    DrawItem("Active Tier", Graphics.activeTier.ToString(), "图形性能等级");
#endif
#if UNITY_2017_2_OR_NEWER
                    DrawItem("Active Color Gamut", Graphics.activeColorGamut.ToString(), "活动色域");
#endif
                }
                EndPanel();

                DrawSectionTitle("Texture Capabilities");
                BeginPanel();
                {
                    ResetRowIndex();
                    DrawItem("NPOT Support", SystemInfo.npotSupport.ToString(), "非2次幂纹理支持");
                    DrawItem("Max Texture Size", SystemInfo.maxTextureSize.ToString(), "最大纹理尺寸");
#if UNITY_5_6_OR_NEWER
                    DrawItem("Max Cubemap Size", SystemInfo.maxCubemapSize.ToString(), "最大立方体贴图尺寸");
                    DrawItem("Graphics UV Starts At Top", SystemInfo.graphicsUVStartsAtTop.ToString(), "UV坐标起点位置");
#endif
#if UNITY_5_5_OR_NEWER
                    DrawItem("Uses Reversed ZBuffer", SystemInfo.usesReversedZBuffer.ToString(), "是否使用反向Z缓冲");
#endif
#if UNITY_5_4_OR_NEWER
                    DrawItem("Copy Texture Support", SystemInfo.copyTextureSupport.ToString(), "纹理拷贝支持");
#endif
                }
                EndPanel();

                DrawSectionTitle("Render Target");
                BeginPanel();
                {
                    ResetRowIndex();
                    DrawItem("Supported Render Target Count", SystemInfo.supportedRenderTargetCount.ToString(), "支持的渲染目标数量");
#if UNITY_2019_3_OR_NEWER
                    DrawItem("Supported Random Write Target Count", SystemInfo.supportedRandomWriteTargetCount.ToString(), "随机写入目标数量");
#endif
#if UNITY_2019_2_OR_NEWER
                    DrawItem("Preserve Frame Buffer Alpha", Graphics.preserveFramebufferAlpha.ToString(), "保留帧缓冲Alpha");
#endif
                }
                EndPanel();

#if UNITY_2019_3_OR_NEWER
                DrawSectionTitle("Compute Shader");
                BeginPanel();
                {
                    ResetRowIndex();
                    DrawItem("Max Compute Buffer Inputs Compute", SystemInfo.maxComputeBufferInputsCompute.ToString(), "计算着色器阶段的最大计算缓冲区数量");
                    DrawItem("Max Compute Buffer Inputs Fragment", SystemInfo.maxComputeBufferInputsFragment.ToString(), "片段着色器阶段的最大计算缓冲区数量");
                    DrawItem("Max Compute Buffer Inputs Vertex", SystemInfo.maxComputeBufferInputsVertex.ToString(), "顶点着色器阶段的最大计算缓冲区数量");
                    DrawItem("Max Compute Work Group Size", SystemInfo.maxComputeWorkGroupSize.ToString(), "计算工作组的总线程数上限");
                    DrawItem("Max Compute Work Group Size X", SystemInfo.maxComputeWorkGroupSizeX.ToString(), "计算工作组X维度的最大线程数");
                    DrawItem("Max Compute Work Group Size Y", SystemInfo.maxComputeWorkGroupSizeY.ToString(), "计算工作组Y维度的最大线程数");
                    DrawItem("Max Compute Work Group Size Z", SystemInfo.maxComputeWorkGroupSizeZ.ToString(), "计算工作组Z维度的最大线程数");
                }
                EndPanel();
#endif

                DrawSectionTitle("Feature Support");
                BeginPanel();
                {
                    ResetRowIndex();
                    DrawItem("Supports Sparse Textures", SystemInfo.supportsSparseTextures.ToString(), "稀疏纹理支持");
                    DrawItem("Supports 3D Textures", SystemInfo.supports3DTextures.ToString(), "3D体积纹理支持");
                    DrawItem("Supports Shadows", SystemInfo.supportsShadows.ToString(), "阴影映射支持");
                    DrawItem("Supports Raw Shadow Depth Sampling", SystemInfo.supportsRawShadowDepthSampling.ToString(), "原始阴影深度采样");
                    DrawItem("Supports Compute Shader", SystemInfo.supportsComputeShaders.ToString(), "计算着色器支持");
                    DrawItem("Supports Instancing", SystemInfo.supportsInstancing.ToString(), "GPU实例化支持");
#if UNITY_5_4_OR_NEWER
                    DrawItem("Supports 2D Array Textures", SystemInfo.supports2DArrayTextures.ToString(), "2D纹理数组支持");
                    DrawItem("Supports Motion Vectors", SystemInfo.supportsMotionVectors.ToString(), "运动向量支持");
#endif
#if UNITY_5_5_OR_NEWER
                    DrawItem("Supports Cubemap Array Textures", SystemInfo.supportsCubemapArrayTextures.ToString(), "立方体贴图数组支持");
#endif
#if UNITY_5_6_OR_NEWER
                    DrawItem("Supports 3D Render Textures", SystemInfo.supports3DRenderTextures.ToString(), "3D渲染纹理支持");
#endif
#if UNITY_2017_3_OR_NEWER
                    DrawItem("Supports Async Compute", SystemInfo.supportsAsyncCompute.ToString(), "异步计算支持");
                    DrawItem("Supports Multi-sampled Textures", SystemInfo.supportsMultisampledTextures.ToString(), "多重采样纹理支持");
#endif
#if UNITY_2018_1_OR_NEWER
                    DrawItem("Supports Async GPU Readback", SystemInfo.supportsAsyncGPUReadback.ToString(), "异步GPU回读支持");
                    DrawItem("Supports 32bits Index Buffer", SystemInfo.supports32bitsIndexBuffer.ToString(), "32位索引缓冲区支持");
#endif
#if UNITY_2018_2_OR_NEWER
                    DrawItem("Supports Mip Streaming", SystemInfo.supportsMipStreaming.ToString(), "Mipmap流式加载支持");
#endif
#if UNITY_2019_3_OR_NEWER
                    DrawItem("Supports Geometry Shaders", SystemInfo.supportsGeometryShaders.ToString(), "几何着色器支持");
                    DrawItem("Supports Ray Tracing", SystemInfo.supportsRayTracing.ToString(), "光线追踪支持");
                    DrawItem("Supports Tessellation Shaders", SystemInfo.supportsTessellationShaders.ToString(), "曲面细分着色器支持");
#endif
                }
                EndPanel();
            }

            private string GetShaderLevelString(int shaderLevel)
            {
                return Utility.StringUtil.Format("Shader Model {0}.{1}", shaderLevel / 10, shaderLevel % 10);
            }
        }
    }
}
