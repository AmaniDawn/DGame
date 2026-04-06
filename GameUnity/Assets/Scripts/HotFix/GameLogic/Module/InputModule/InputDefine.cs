#if ENABLE_INPUT_SYSTEM

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameLogic
{
    /// <summary>
    /// 轴输入状态枚举
    /// </summary>
    public enum InputAxisType
    {
        /// <summary>
        /// 前后移动轴
        /// </summary>
        MoveForward,

        /// <summary>
        /// 左右移动轴
        /// </summary>
        MoveRight,

        /// <summary>
        /// 视角上下轴
        /// </summary>
        LookUp,

        /// <summary>
        /// 视角左右轴
        /// </summary>
        LookRight,

        /// <summary>
        /// 镜头拉近
        /// </summary>
        ZoomUp,

        /// <summary>
        /// 镜头拉远
        /// </summary>
        ZoomDown,
    }

    /// <summary>
    /// 命令类型
    /// </summary>
    public enum InputCommandType
    {
        /// <summary>
        /// 无
        /// </summary>
        None,

        /// <summary>
        /// 技能
        /// </summary>
        Skill,

        /// <summary>
        /// 跳跃
        /// </summary>
        Jump,

        /// <summary>
        /// 冲刺
        /// </summary>
        Sprint,
    }

    /// <summary>
    /// 按键状态
    /// </summary>
    public enum InputState
    {
        /// <summary>
        /// 按下
        /// </summary>
        Started,

        /// <summary>
        /// 按住
        /// </summary>
        Performed,

        /// <summary>
        /// 抬起
        /// </summary>
        Canceled,
    }

    /// <summary>
    /// 输入事件结构体
    /// <remarks>表示原始输入事件，例如按下、持续、释放</remarks>
    /// </summary>
    public struct InputEvent
    {
        /// <summary>
        /// 输入动作类型
        /// </summary>
        public InputEventType ActionType;

        /// <summary>
        /// 输入按键状态
        /// </summary>
        public InputState InputState;

        /// <summary>
        /// 输入发生时间
        /// </summary>
        public double EventTime;

        public InputEvent(InputEventType actionType, InputState inputState, double eventTime)
        {
            ActionType = actionType;
            InputState = inputState;
            EventTime = eventTime;
        }
    }

    /// <summary>
    /// 最终玩法命令结构体
    /// <remarks>表示最下游的具体执行命令，例如跳跃、冲刺、释放技能</remarks>
    /// </summary>
    public struct GameplayCommand
    {
        /// <summary>
        /// 命令类型
        /// </summary>
        public InputCommandType CommandType;

        /// <summary>
        /// 整数值 传递如技能ID等
        /// </summary>
        public int IntValue;

        public GameplayCommand(InputCommandType commandType, int intValue = 0)
        {
            CommandType = commandType;
            IntValue = intValue;
        }
    }

    /// <summary>
    /// 输入上下文命令结构体
    /// <remarks>表示根据输入事件和当前上下文解析出的中间命令，包含输入来源与最终玩法命令</remarks>
    /// </summary>
    public struct InputContextCommand
    {
        /// <summary>
        /// 原始输入动作类型
        /// </summary>
        public InputEventType ActionType;

        /// <summary>
        /// 原始输入状态
        /// </summary>
        public InputState InputState;

        /// <summary>
        /// 原始输入时间
        /// </summary>
        public double EventTime;

        /// <summary>
        /// 原始输入命令
        /// </summary>
        public GameplayCommand Command;

        /// <summary>
        /// 命令索引
        /// </summary>
        public int Index;

        public InputContextCommand(InputEventType actionType, InputState inputState, GameplayCommand command,
            double eventTime, int index)
        {
            ActionType = actionType;
            InputState = inputState;
            EventTime = eventTime;
            Command = command;
            Index = index;
        }
    }

    /// <summary>
    /// 输入缓存结构体
    /// <remarks>用于临时存储输入事件</remarks>
    /// </summary>
    public struct InputCache
    {
        public InputCache(InputEventType actionType, InputState inputState, double eventTime, double createTime,
            double accumulatedTime)
        {
            ActionType = actionType;
            InputState = inputState;
            EventTime = eventTime;
            CreateTime = createTime;
            AccumulatedTime = accumulatedTime;
        }

        /// <summary>
        /// 缓存的动作类型
        /// </summary>
        public InputEventType ActionType;

        /// <summary>
        /// 缓存的状态
        /// </summary>
        public InputState InputState;

        /// <summary>
        /// 原始事件时间
        /// </summary>
        public double EventTime;

        /// <summary>
        /// 缓存创建时间
        /// </summary>
        public double CreateTime;

        /// <summary>
        /// 缓存累计时间
        /// </summary>
        public double AccumulatedTime;
    }

    public struct AxisCache
    {
        /// <summary>
        /// 最后非零值
        /// </summary>
        public float LasNonZeroValue;

        /// <summary>
        /// 累计时间
        /// </summary>
        public float AccumulateTime;

        public AxisCache(float lasNonZeroValue, float accumulateTime)
        {
            LasNonZeroValue = lasNonZeroValue;
            AccumulateTime = accumulateTime;
        }
    }

    /// <summary>
    /// 输入绑定信息。
    /// 用于设置界面列出动作的当前键位、分组和组合绑定状态。
    /// </summary>
    public struct InputBindingInfo
    {
        /// <summary>
        /// 绑定所属的动作名称。
        /// </summary>
        public string ActionName;

        /// <summary>
        /// 该绑定在动作内的索引。
        /// <remarks>重绑定和重置时使用此索引定位绑定。</remarks>
        /// </summary>
        public int BindingIndex;

        /// <summary>
        /// 绑定的唯一 ID。
        /// </summary>
        public string BindingId;

        /// <summary>
        /// 当前用于 UI 展示的人类可读名称。
        /// </summary>
        public string DisplayName;

        /// <summary>
        /// 当前生效的控制路径。
        /// <remarks>若存在 override，则这里是 override 后的路径。</remarks>
        /// </summary>
        public string EffectivePath;

        /// <summary>
        /// 绑定所属的控制方案分组。
        /// <remarks>例如 Keyboard&amp;Mouse 或 Gamepad。</remarks>
        /// </summary>
        public string Groups;

        /// <summary>
        /// 是否为组合绑定本身。
        /// <remarks>例如 Dpad、2DVector。</remarks>
        /// </summary>
        public bool IsComposite;

        /// <summary>
        /// 是否为组合绑定的组成项。
        /// <remarks>例如 up、down、left、right。</remarks>
        /// </summary>
        public bool IsPartOfComposite;

        public InputBindingInfo(string actionName, int bindingIndex, string bindingId, string displayName,
            string effectivePath, string groups, bool isComposite, bool isPartOfComposite)
        {
            ActionName = actionName;
            BindingIndex = bindingIndex;
            BindingId = bindingId;
            DisplayName = displayName;
            EffectivePath = effectivePath;
            Groups = groups;
            IsComposite = isComposite;
            IsPartOfComposite = isPartOfComposite;
        }
    }

    public sealed partial class InputDefine
    {
        private const string BindingOverridesSaveKey = "GAME_LOGIC_INPUT_BINDING_OVERRIDES"; // 按键覆盖保存键

        private static GameInputActions m_inputActions; // Unity输入系统实例
        private static InputActionRebindingExtensions.RebindingOperation m_rebindingOperation; // 当前交互式改键操作
        private static bool m_rebindActionWasEnabled; // 改键开始前对应动作是否处于启用状态

        /// <summary>
        /// 当前是否存在正在进行中的交互式改键操作。
        /// </summary>
        public static bool IsRebinding => m_rebindingOperation != null;

        /// <summary>
        /// 初始化新输入系统实例，并在创建时自动加载本地保存的按键覆盖。
        /// </summary>
        public static void Initialize()
        {
            if (m_inputActions != null)
            {
                return;
            }

            m_inputActions = new GameInputActions();
            LoadBindingOverrides();
            Enable();

            RegisterInputActions();

            #region 移动输入处理

            m_inputActions.GamePlay.Move.performed += ctx =>
            {
                GameModule.Input.ReceiveInputAxis(InputAxisType.MoveForward, ctx.ReadValue<Vector2>().y);
                GameModule.Input.ReceiveInputAxis(InputAxisType.MoveRight, ctx.ReadValue<Vector2>().x);
            };
            m_inputActions.GamePlay.Move.canceled += _ =>
            {
                GameModule.Input.ReceiveInputAxis(InputAxisType.MoveForward, 0);
                GameModule.Input.ReceiveInputAxis(InputAxisType.MoveRight, 0);
            };

            #endregion

            #region 视角输入处理

            m_inputActions.GamePlay.Look.performed += ctx =>
            {
                GameModule.Input.ReceiveInputAxis(InputAxisType.LookUp, ctx.ReadValue<Vector2>().y);
                GameModule.Input.ReceiveInputAxis(InputAxisType.LookRight, ctx.ReadValue<Vector2>().x);
            };
            m_inputActions.GamePlay.Look.canceled += _ =>
            {
                GameModule.Input.ReceiveInputAxis(InputAxisType.LookUp, 0);
                GameModule.Input.ReceiveInputAxis(InputAxisType.LookRight, 0);
            };

            #endregion

            #region 视角输入处理

            m_inputActions.GamePlay.Zoom.performed += ctx =>
            {
                GameModule.Input.ReceiveInputAxis(InputAxisType.ZoomUp, ctx.ReadValue<Vector2>().y);
                GameModule.Input.ReceiveInputAxis(InputAxisType.ZoomDown, ctx.ReadValue<Vector2>().x);
            };
            m_inputActions.GamePlay.Zoom.canceled += _ =>
            {
                GameModule.Input.ReceiveInputAxis(InputAxisType.ZoomUp, 0);
                GameModule.Input.ReceiveInputAxis(InputAxisType.ZoomDown, 0);
            };

            #endregion
        }

        /// <summary>
        /// 启用输入系统
        /// </summary>
        public static void Enable() => m_inputActions?.Enable();

        /// <summary>
        /// 禁用输入系统
        /// </summary>
        public static void Disable() => m_inputActions?.Disable();

        /// <summary>
        /// 释放输入系统实例，并清理当前正在进行的改键状态。
        /// </summary>
        public static void Dispose()
        {
            CancelInteractiveRebind();
            Disable();
            m_inputActions?.Dispose();
            m_inputActions = null;
        }

        /// <summary>
        /// 获取指定动作的绑定信息列表。
        /// </summary>
        /// <param name="actionName">输入动作名称</param>
        /// <param name="includeComposite">是否包含组合绑定本身</param>
        /// <returns>绑定信息列表。若动作不存在则返回空列表</returns>
        public static List<InputBindingInfo> GetActionBindings(string actionName, bool includeComposite = false)
        {
            List<InputBindingInfo> result = new List<InputBindingInfo>();
            InputAction action = FindAction(actionName);
            if (action == null)
            {
                return result;
            }

            for (int i = 0; i < action.bindings.Count; i++)
            {
                InputBinding binding = action.bindings[i];
                if (!includeComposite && binding.isComposite)
                {
                    continue;
                }

                result.Add(new InputBindingInfo(
                    action.name,
                    i,
                    binding.id.ToString(),
                    GetBindingDisplayString(action, i),
                    binding.effectivePath ?? binding.path ?? string.Empty,
                    binding.groups ?? string.Empty,
                    binding.isComposite,
                    binding.isPartOfComposite));
            }

            return result;
        }

        /// <summary>
        /// 获取指定绑定的人类可读显示文本。
        /// </summary>
        /// <param name="actionName">输入动作名称</param>
        /// <param name="bindingIndex">动作内的绑定索引</param>
        /// <returns>用于界面展示的按键名称</returns>
        public static string GetBindingDisplayString(string actionName, int bindingIndex)
        {
            InputAction action = FindAction(actionName);
            return GetBindingDisplayString(action, bindingIndex);
        }

        /// <summary>
        /// 开始交互式改键。
        /// </summary>
        /// <param name="actionName">输入动作名称</param>
        /// <param name="bindingIndex">动作内的绑定索引</param>
        /// <param name="onComplete">改键成功完成时触发</param>
        /// <param name="onCancel">改键被取消时触发</param>
        /// <param name="excludeMouse">是否排除鼠标输入</param>
        /// <returns>是否成功开始改键</returns>
        public static bool StartInteractiveRebind(string actionName, int bindingIndex, Action onComplete = null,
            Action onCancel = null, bool excludeMouse = true)
        {
            if (IsRebinding)
            {
                return false;
            }

            InputAction action = FindAction(actionName);
            if (!IsValidBindingIndex(action, bindingIndex))
            {
                return false;
            }

            InputBinding binding = action.bindings[bindingIndex];
            if (binding.isComposite)
            {
                return false;
            }

            m_rebindActionWasEnabled = action.enabled;
            action.Disable();

            var operation = action.PerformInteractiveRebinding(bindingIndex)
                .OnMatchWaitForAnother(0.1f)
                .OnCancel(_ =>
                {
                    CompleteRebind(false);
                    onCancel?.Invoke();
                })
                .OnComplete(_ =>
                {
                    CompleteRebind(true);
                    onComplete?.Invoke();
                });

            if (excludeMouse)
            {
                operation.WithControlsExcluding("<Mouse>");
            }

            m_rebindingOperation = operation;
            m_rebindingOperation.Start();
            return true;
        }

        /// <summary>
        /// 取消当前正在进行中的交互式改键。
        /// </summary>
        public static void CancelInteractiveRebind()
        {
            if (!IsRebinding)
            {
                return;
            }

            m_rebindingOperation.Cancel();
        }

        /// <summary>
        /// 重置指定绑定的 override，并立即保存到本地。
        /// </summary>
        /// <param name="actionName">输入动作名称</param>
        /// <param name="bindingIndex">动作内的绑定索引</param>
        public static void ResetBindingOverride(string actionName, int bindingIndex)
        {
            InputAction action = FindAction(actionName);
            if (!IsValidBindingIndex(action, bindingIndex))
            {
                return;
            }

            action.RemoveBindingOverride(bindingIndex);
            SaveBindingOverrides();
        }

        /// <summary>
        /// 清空所有动作的绑定 override，并立即保存到本地。
        /// </summary>
        public static void ResetAllBindingOverrides()
        {
            if (m_inputActions == null)
            {
                return;
            }

            m_inputActions.asset.RemoveAllBindingOverrides();
            SaveBindingOverrides();
        }

        private static void SaveBindingOverrides()
        {
            if (m_inputActions == null)
            {
                return;
            }

            string overridesJson = m_inputActions.asset.SaveBindingOverridesAsJson();
            DGame.Utility.PlayerPrefsUtil.SetString(BindingOverridesSaveKey, overridesJson ?? string.Empty);
        }

        private static void LoadBindingOverrides()
        {
            if (m_inputActions == null)
            {
                return;
            }

            string overridesJson = DGame.Utility.PlayerPrefsUtil.GetString(BindingOverridesSaveKey, string.Empty);
            if (string.IsNullOrEmpty(overridesJson))
            {
                return;
            }

            m_inputActions.asset.LoadBindingOverridesFromJson(overridesJson);
        }

        private static void CompleteRebind(bool saveOverride)
        {
            InputAction action = m_rebindingOperation?.action;
            m_rebindingOperation?.Dispose();
            m_rebindingOperation = null;

            if (saveOverride)
            {
                SaveBindingOverrides();
            }

            if (m_rebindActionWasEnabled)
            {
                action?.Enable();
            }

            m_rebindActionWasEnabled = false;
        }

        private static InputAction FindAction(string actionName)
        {
            if (m_inputActions == null || string.IsNullOrEmpty(actionName))
            {
                return null;
            }

            return m_inputActions.FindAction(actionName);
        }

        private static bool IsValidBindingIndex(InputAction action, int bindingIndex)
        {
            return action != null && bindingIndex >= 0 && bindingIndex < action.bindings.Count;
        }

        private static string GetBindingDisplayString(InputAction action, int bindingIndex)
        {
            if (!IsValidBindingIndex(action, bindingIndex))
            {
                return string.Empty;
            }

            InputBinding binding = action.bindings[bindingIndex];
            if (binding.isComposite)
            {
                return binding.name;
            }

            string displayString = action.GetBindingDisplayString(bindingIndex);
            return string.IsNullOrEmpty(displayString)
                ? InputControlPath.ToHumanReadableString(
                    binding.effectivePath,
                    InputControlPath.HumanReadableStringOptions.OmitDevice)
                : displayString;
        }
    }
}

#endif