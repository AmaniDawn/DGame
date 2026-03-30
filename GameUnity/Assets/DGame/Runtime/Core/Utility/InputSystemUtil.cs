using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DGame
{
    public static partial class Utility
    {
        public static class InputSystemUtil
        {
        // #region 新输入系统改键功能
        //
        // /// <summary>
        // /// 对对应的按键进行改建
        // /// </summary>
        // /// <param name="inputAction">键盘映射类</param>
        // /// <param name="actionName">事件名</param>
        // /// <param name="controlIndex"></param>
        // /// <param name="keyIndex"></param>
        // /// <param name="callback"></param>
        // /// <param name="isExclude"></param>
        // public static void RebindKeyAction(PlayerInputAction inputAction, string actionName, int controlIndex,
        //     int keyIndex, UnityAction callback, bool isExclude = true)
        // {
        //     if (!CheckHaveBindingActionByActionName(inputAction, actionName)) return;
        //     // 临时存储
        //     InputAction tempAction = inputAction.FindAction(actionName);
        //     // 得到对应的键的索引
        //     int tempIndex = tempAction.GetBindingIndexForControl(tempAction.controls[controlIndex]) + keyIndex;
        //
        //     // 改建前一定要先禁用键盘监听
        //     tempAction.Disable();
        //
        //     if (isExclude)
        //     {
        //         // 执行互动重绑定，为指定的键创建重绑定操作
        //         tempAction.PerformInteractiveRebinding(tempIndex)
        //             .WithControlsExcluding("Mouse") // 排除掉名为 "Mouse" 的控件
        //             .OnMatchWaitForAnother(.1f) // 如果有匹配的控件，等待另一个键的输入（最多 0.1 秒）来进行绑定
        //             .OnComplete(operation => // 当重绑定操作完成时，执行以下操作：
        //             {
        //                 // 改建完成后保存到本地
        //                 string actionMapJson = inputAction.asset.SaveBindingOverridesAsJson();
        //                 PlayerPrefs.SetString("ActionMap", actionMapJson);
        //
        //                 callback?.Invoke();
        //                 tempAction.Enable();
        //                 operation.Dispose();
        //             }).Start();
        //     }
        //     else
        //     {
        //         tempAction.PerformInteractiveRebinding(tempIndex)
        //             .OnMatchWaitForAnother(.1f)
        //             .OnComplete(operation =>
        //             {
        //                 // 改建完成后保存到本地
        //                 string actionMapJson = inputAction.asset.SaveBindingOverridesAsJson();
        //                 PlayerPrefs.SetString("ActionMap", actionMapJson);
        //
        //                 callback?.Invoke();
        //                 tempAction.Enable();
        //                 operation.Dispose(); // 释放操作对象，以确保资源被正确释放
        //             }).Start(); // 启动互动重绑定操作，此时开始等待用户的输入
        //     }
        // }
        //
        // /// <summary>
        // /// 得到对应映射对应的绑定按键的名字
        // /// </summary>
        // /// <param name="actionName">键盘映射名</param>
        // /// <param name="index">当前映射的按键索引</param>
        // /// <returns></returns>
        // public static string GetBindingNameOfAction(PlayerInputAction inputAction, string actionName,
        //     int controlIndex, int keyIndex)
        // {
        //     if (!CheckHaveBindingActionByActionName(inputAction, actionName)) return null;
        //     // 临时存储
        //     InputAction tempAction = inputAction.FindAction(actionName);
        //     // 得到对应的键的索引
        //     int tempIndex = tempAction.GetBindingIndexForControl(tempAction.controls[controlIndex]) + keyIndex;
        //     // 通过索引找到对应的键，并把路径改成人类易读字符返回
        //     return InputControlPath.ToHumanReadableString(
        //         tempAction.bindings[tempIndex].effectivePath,
        //         InputControlPath.HumanReadableStringOptions.OmitDevice);
        // }
        //
        // /// <summary>
        // /// 查找键盘映射中是否存在对应名字的事件
        // /// </summary>
        // /// <param name="actionName"></param>
        // /// <returns></returns>
        // private static bool CheckHaveBindingActionByActionName(PlayerInputAction inputAction, string actionName)
        // {
        //     if (inputAction.asset[actionName].bindings.Count > 0)
        //     {
        //         return true;
        //     }
        //
        //     return false;
        // }
        //
        // #endregion
        }
    }
}