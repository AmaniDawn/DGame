using UnityEngine;
using UnityEngine.Profiling;

namespace DGame
{
    public partial class DebuggerDriver
    {
        private class ProfilerInformationWindow : ScrollableDebuggerWindowBase
        {
            protected override void OnDrawScrollableWindow()
            {
                DrawSectionTitle("Profiler Status");
                BeginPanel();
                {
                    DrawItem("Supported", Profiler.supported.ToString(), "性能分析器是否在当前平台支持");
                    DrawItem("Enabled", Profiler.enabled.ToString(), "性能分析器是否已启用");
                    DrawItem("Enable Binary Log",
                        Profiler.enableBinaryLog ? Utility.StringUtil.Format("True, {0}", Profiler.logFile) : "False", "是否启用二进制日志记录");
#if UNITY_2019_3_OR_NEWER
                    DrawItem("Enable Allocation Callstacks", Profiler.enableAllocationCallstacks.ToString(), "是否启用分配调用栈跟踪");
#endif
#if UNITY_2018_3_OR_NEWER
                    DrawItem("Area Count", Profiler.areaCount.ToString(), "性能分析区域数量");
#endif
                }
                EndPanel();

                DrawSectionTitle("Memory Usage");
                BeginPanel();
                {
                    ResetRowIndex();
#if UNITY_2018_3_OR_NEWER
                    long maxUsed = (long)Profiler.maxUsedMemory;
                    DrawItemColored("Max Used Memory", GetByteLengthString(maxUsed), GetMemoryColor(maxUsed, 512 * 1024 * 1024L));
#endif
                    long monoUsed = Profiler.GetMonoUsedSizeLong();
                    DrawItemColored("Mono Used Size", GetByteLengthString(monoUsed), GetMemoryColor(monoUsed, 256 * 1024 * 1024L));

                    long monoHeap = Profiler.GetMonoHeapSizeLong();
                    DrawItem("Mono Heap Size", GetByteLengthString(monoHeap), "Mono虚拟机堆的总大小");

                    long usedHeap = (long)Profiler.usedHeapSizeLong;
                    DrawItem("Used Heap Size", GetByteLengthString(usedHeap), "整体堆内存的使用量");
                }
                EndPanel();

                DrawSectionTitle("Allocated Memory");
                BeginPanel();
                {
                    ResetRowIndex();
                    long totalAllocated = Profiler.GetTotalAllocatedMemoryLong();
                    DrawItem("Total Allocated Memory", GetByteLengthString(totalAllocated), "应用程序当前分配的总内存");

                    long totalReserved = Profiler.GetTotalReservedMemoryLong();
                    DrawItem("Total Reserved Memory", GetByteLengthString(totalReserved), "操作系统为应用程序保留的总内存");

                    long totalUnused = Profiler.GetTotalUnusedReservedMemoryLong();
                    DrawItem("Total Unused Reserved Memory", GetByteLengthString(totalUnused), "已保留但未使用的内存");

#if UNITY_2018_1_OR_NEWER
                    long graphicsAllocated = Profiler.GetAllocatedMemoryForGraphicsDriver();
                    DrawItem("Allocated Memory For Graphics", GetByteLengthString(graphicsAllocated), "图形驱动程序分配的内存");
#endif

#if UNITY_5_5_OR_NEWER
                    long tempAllocatorSize = (long)Profiler.GetTempAllocatorSize();
                    DrawItem("Temp Allocator Size", GetByteLengthString(tempAllocatorSize), "临时分配器的大小");
#endif
                }
                EndPanel();

                DrawSectionTitle("Other");
                BeginPanel();
                {
                    ResetRowIndex();
                    DrawItem("Marshal Cached HGlobal Size", GetByteLengthString(Utility.Marshal.CachedHGlobalSize), "封送处理缓存的非托管内存大小");
                }
                EndPanel();
            }

            private Color32 GetMemoryColor(long bytes, long warningThreshold)
            {
                if (bytes > warningThreshold * 2)
                {
                    return DebuggerStyles.ErrorColor;
                }
                else if (bytes > warningThreshold)
                {
                    return DebuggerStyles.WarningColor;
                }
                return DebuggerStyles.TextColor;
            }
        }
    }
}
