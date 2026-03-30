using UnityEngine;

namespace DGame
{
    public partial class DebuggerDriver
    {
        private class InputSummaryInformationWindow : ScrollableDebuggerWindowBase
        {
            protected override void OnDrawScrollableWindow()
            {
                DrawSectionTitle("Device Input");
                BeginPanel();
                {
                    DrawItem("Back Button Leaves App", Input.backButtonLeavesApp.ToString(), "返回按钮是否退出应用");
                    DrawItem("Device Orientation", Input.deviceOrientation.ToString(), "设备物理方向");
                    DrawItem("Compensate Sensors", Input.compensateSensors.ToString(), "是否补偿传感器数据");
                }
                EndPanel();

                DrawSectionTitle("Mouse Input");
                BeginPanel();
                {
                    ResetRowIndex();
                    DrawItem("Mouse Present", Input.mousePresent.ToString(), "是否存在鼠标设备");
                    DrawItem("Mouse Position", Input.mousePosition.ToString(), "当前鼠标位置（屏幕坐标）");
                    DrawItem("Mouse Scroll Delta", Input.mouseScrollDelta.ToString(), "鼠标滚轮滚动增量");
                }
                EndPanel();

                DrawSectionTitle("Keyboard Input");
                BeginPanel();
                {
                    ResetRowIndex();
                    Color32 keyColor = Input.anyKey ? DebuggerStyles.SuccessColor : DebuggerStyles.TextColor;
                    DrawItemColored("Any Key", Input.anyKey.ToString(), keyColor);

                    Color32 keyDownColor = Input.anyKeyDown ? DebuggerStyles.PrimaryColor : DebuggerStyles.TextColor;
                    DrawItemColored("Any Key Down", Input.anyKeyDown.ToString(), keyDownColor);

                    DrawItem("Input String", string.IsNullOrEmpty(Input.inputString) ? "(none)" : Input.inputString, "当前输入的字符序列");
                }
                EndPanel();

                DrawSectionTitle("IME (Input Method)");
                BeginPanel();
                {
                    ResetRowIndex();
                    Color32 imeColor = Input.imeIsSelected ? DebuggerStyles.PrimaryColor : DebuggerStyles.TextColor;
                    DrawItemColored("IME Is Selected", Input.imeIsSelected.ToString(), imeColor);
                    DrawItem("IME Composition Mode", Input.imeCompositionMode.ToString(), "输入法组合模式");
                    DrawItem("Composition Cursor Position", Input.compositionCursorPos.ToString(), "输入法组合光标位置");
                    DrawItem("Composition String", string.IsNullOrEmpty(Input.compositionString) ? "(none)" : Input.compositionString, "输入法组合字符串");
                }
                EndPanel();
            }
        }
    }
}