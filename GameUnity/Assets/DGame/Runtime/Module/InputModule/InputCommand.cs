using System;
using UnityEngine;

namespace DGame
{
    internal sealed partial class InputModule
    {
        public class InputCommand
        {
            public readonly KeyCode keyCode;
            public readonly int mouseID;
            public readonly HotKeyType hotKeyName;
            public readonly InputType inputType;
            public Action<KeyCode> keyCodeAction;
            public Action<int> mouseAction;
            public Action<float> hotKeyAction;
            private readonly IInputModule m_inputModule;

            public InputCommand(IInputModule inputModule, InputType inputType, KeyCode keyCode, Action<KeyCode> keyCodeAction)
            {
                m_inputModule = inputModule;
                this.keyCode = keyCode;
                this.keyCodeAction = keyCodeAction;
                this.inputType = inputType;
                this.mouseID = -1;
                this.hotKeyName = HotKeyType.None;
                this.hotKeyAction = null;
                this.mouseAction = null;
            }

            public InputCommand(InputType inputType, int mouseID, Action<int> mouseAction)
            {
                this.keyCode = default;
                this.keyCodeAction = null;
                this.inputType = inputType;
                this.mouseID = mouseID;
                this.hotKeyName = HotKeyType.None;
                this.hotKeyAction = null;
                this.mouseAction = mouseAction;
            }

            public InputCommand(InputType inputType, HotKeyType hotKeyType, Action<float> hotKeyAction)
            {
                this.keyCode = default;
                this.keyCodeAction = null;
                this.inputType = inputType;
                this.mouseID = -1;
                this.hotKeyName = hotKeyType;
                this.hotKeyAction = hotKeyAction;
                this.mouseAction = null;
            }

            private string GetHotKeyName()
                => hotKeyName switch
                {
                    HotKeyType.Horizontal => nameof(HotKeyType.Horizontal),
                    HotKeyType.Vertical => nameof(HotKeyType.Vertical),
                    _ => hotKeyName.ToString()
                };

            public void Execute()
            {
                if (mouseID != -1)
                {
                    switch (inputType)
                    {
                        case InputType.Down:
                            if (Input.GetMouseButtonDown(mouseID))
                            {
                                mouseAction?.Invoke(mouseID);
                            }

                            return;

                        case InputType.Up:
                            if (Input.GetMouseButtonUp(mouseID))
                            {
                                mouseAction?.Invoke(mouseID);
                            }

                            return;

                        case InputType.Stay:
                            if (Input.GetMouseButton(mouseID))
                            {
                                mouseAction?.Invoke(mouseID);
                            }

                            return;
                    }
                }
                else if (hotKeyName != HotKeyType.None)
                {
                    switch (inputType)
                    {
                        case InputType.Axis:
                            hotKeyAction?.Invoke(Input.GetAxis(GetHotKeyName()));
                            return;

                        case InputType.AxisRaw:
                            hotKeyAction?.Invoke(Input.GetAxisRaw(GetHotKeyName()));
                            return;
                    }
                }
                else
                {
                    m_inputModule.TryGetRebindKeyCode(keyCode, out var rebindKeyCode);
                    switch (inputType)
                    {
                        case InputType.Down:
                            if (Input.GetKeyDown(rebindKeyCode))
                            {
                                keyCodeAction?.Invoke(rebindKeyCode);
                            }

                            return;

                        case InputType.Up:
                            if (Input.GetKeyUp(rebindKeyCode))
                            {
                                keyCodeAction?.Invoke(rebindKeyCode);
                            }

                            return;

                        case InputType.Stay:
                            if (Input.GetKey(rebindKeyCode))
                            {
                                keyCodeAction?.Invoke(rebindKeyCode);
                            }

                            return;
                    }
                }
            }
        }
    }
}