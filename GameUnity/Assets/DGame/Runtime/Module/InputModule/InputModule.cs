using System;
using System.Collections.Generic;
using UnityEngine;

namespace DGame
{
    internal sealed partial class InputModule : Module, IInputModule, IUpdateModule
    {
        private const string SAVE_KEY = "INPUT_MODULE_REBIND_KEY_MAP";

        public override int Priority => 999;

        private readonly List<InputCommand> m_inputCommands = new List<InputCommand>(32);
        private readonly List<InputCommand> m_toDelete = new List<InputCommand>(32);
        // key -> oldKey value -> newKey
        private Dictionary<KeyCode, KeyCode> m_keyRebuildMap;

        private bool CanInput { get; set; } = true;

        public override void OnCreate()
        {
            m_keyRebuildMap = LoadRebindKeyMap();
        }

        public override void OnDestroy()
        {
            m_inputCommands.Clear();
            m_keyRebuildMap.Clear();
            m_toDelete.Clear();
        }

        public void RegisterInputCommand(InputType inputType, KeyCode keyCode, Action<KeyCode> action)
        {
            for (int i = 0; i < m_inputCommands.Count; i++)
            {
                var inputCommand = m_inputCommands[i];
                if (inputCommand.keyCode == keyCode && inputCommand.inputType == inputType)
                {
                    inputCommand.keyCodeAction += action;
                    return;
                }
            }
            m_inputCommands.Add(new InputCommand(this, inputType, keyCode, action));
        }

        public void RegisterInputCommand(InputType inputType, int mouseID, Action<int> action)
        {
            for (int i = 0; i < m_inputCommands.Count; i++)
            {
                var inputCommand = m_inputCommands[i];
                if (inputCommand.mouseID == mouseID && inputCommand.inputType == inputType)
                {
                    inputCommand.mouseAction += action;
                    return;
                }
            }
            m_inputCommands.Add(new InputCommand(inputType, mouseID, action));
        }

        public void RegisterInputCommand(InputType inputType, HotKeyType hotKeyType, Action<float> action)
        {
            for (int i = 0; i < m_inputCommands.Count; i++)
            {
                var inputCommand = m_inputCommands[i];
                if (inputCommand.hotKeyName == hotKeyType && inputCommand.inputType == inputType)
                {
                    inputCommand.hotKeyAction += action;
                    return;
                }
            }
            m_inputCommands.Add(new InputCommand(inputType, hotKeyType, action));
        }

        public void RemoveInputCommand(InputType inputType, KeyCode keyCode, Action<KeyCode> action)
        {
            for (int i = 0; i < m_inputCommands.Count; i++)
            {
                var inputCommand = m_inputCommands[i];
                if (inputCommand.keyCode == keyCode && inputCommand.inputType == inputType)
                {
                    inputCommand.keyCodeAction -= action;

                    if (inputCommand.keyCodeAction == null)
                    {
                        m_toDelete.Add(inputCommand);
                    }

                    return;
                }
            }
        }

        public void RemoveInputCommand(InputType inputType, int mouseID, Action<int> action)
        {
            for (int i = 0; i < m_inputCommands.Count; i++)
            {
                var inputCommand = m_inputCommands[i];
                if (inputCommand.mouseID == mouseID && inputCommand.inputType == inputType)
                {
                    inputCommand.mouseAction -= action;
                    if (inputCommand.mouseAction == null)
                    {
                        m_toDelete.Add(inputCommand);
                    }

                    return;
                }
            }
        }

        public void RemoveInputCommand(InputType inputType, HotKeyType hotKeyType, Action<float> action)
        {
            for (int i = 0; i < m_inputCommands.Count; i++)
            {
                var inputCommand = m_inputCommands[i];
                if (inputCommand.hotKeyName == hotKeyType && inputCommand.inputType == inputType)
                {
                    inputCommand.hotKeyAction -= action;
                    if (inputCommand.hotKeyAction == null)
                    {
                        m_toDelete.Add(inputCommand);
                    }
                    return;
                }
            }
        }

        public void RebindInputCommand(KeyCode oldKeyCode, KeyCode newKeyCode)
        {
            if (oldKeyCode == newKeyCode)
            {
                m_keyRebuildMap.Remove(oldKeyCode);
                return;
            }
            m_keyRebuildMap[oldKeyCode] = newKeyCode;
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            if (m_toDelete.Count > 0)
            {
                for (int i = 0; i < m_toDelete.Count; i++)
                {
                    m_inputCommands.Remove(m_toDelete[i]);
                }
                m_toDelete.Clear();
            }

            if (!CanInput || m_inputCommands.Count <= 0) return;
            for (int i = 0; i < m_inputCommands.Count; i++)
            {
                m_inputCommands[i].Execute();
            }
        }

        public bool TryGetRebindKeyCode(KeyCode oldKeyCode, out KeyCode newKeyCode)
        {
            if (m_keyRebuildMap == null)
            {
                newKeyCode = oldKeyCode;
                return false;
            }

            bool isRebind = m_keyRebuildMap.TryGetValue(oldKeyCode, out newKeyCode);

            if (!isRebind)
            {
                newKeyCode = oldKeyCode;
            }
            return isRebind;
        }

        public void EnableInput() => CanInput = true;

        public void DisableInput() => CanInput = false;

        public void SaveRebindKeyMap()
        {
            if (m_keyRebuildMap == null)
            {
                return;
            }
            string jsonStr = Utility.JsonUtil.ToJson(m_keyRebuildMap);
            Utility.PlayerPrefsUtil.SetString(SAVE_KEY, jsonStr);
        }

        private Dictionary<KeyCode, KeyCode> LoadRebindKeyMap()
        {
            string jsonStr = Utility.PlayerPrefsUtil.GetString(SAVE_KEY);
            return Utility.JsonUtil.ToObject<Dictionary<KeyCode, KeyCode>>(jsonStr)
                   ?? new Dictionary<KeyCode, KeyCode>(32);
        }
    }
}