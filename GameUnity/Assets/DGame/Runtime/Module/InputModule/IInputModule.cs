using System;
using UnityEngine;

namespace DGame
{
    public interface IInputModule
    {
        void RegisterInputCommand(InputType inputType, KeyCode keyCode, Action<KeyCode> action);

        void RegisterInputCommand(InputType inputType, int mouseID, Action<int> action);

        void RegisterInputCommand(InputType inputType, HotKeyType hotKeyType, Action<float> action);

        void RemoveInputCommand(InputType inputType, KeyCode keyCode, Action<KeyCode> action);

        void RemoveInputCommand(InputType inputType, int mouseID, Action<int> action);

        void RemoveInputCommand(InputType inputType, HotKeyType hotKeyType, Action<float> action);

        void RebindInputCommand(KeyCode oldKeyCode, KeyCode newKeyCode);

        bool TryGetRebindKeyCode(KeyCode oldKeyCode, out KeyCode newKeyCode);

        void SaveRebindKeyMap();
    }
}