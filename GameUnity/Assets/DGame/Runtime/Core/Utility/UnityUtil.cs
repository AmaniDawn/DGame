using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine.Internal;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngineInternal;

namespace DGame
{
    public static partial class Utility
    {
        public static class UnityUtil
        {
            private static IMonoDriver m_monoDriver;

            #region 控制协程Coroutine

            public static GameCoroutine StartCoroutine(string name, IEnumerator routine, MonoBehaviour bindBehaviour)
            {
                if (bindBehaviour == null)
                {
                    DLogger.Error("StartCoroutine {0} failed, bindBehaviour is null", name);
                    return null;
                }

                var behaviour = bindBehaviour;
                return StartCoroutine(behaviour, name, routine);
            }

            public static GameCoroutine StartCoroutine(string name, IEnumerator routine, GameObject bindGo)
            {
                if (bindGo == null)
                {
                    DLogger.Error("StartCoroutine {0} failed, BindGo is null", name);
                    return null;
                }

                var behaviour = GetDefaultBehaviour(bindGo);
                return StartCoroutine(behaviour, name, routine);
            }

            public static GameCoroutine StartGlobalCoroutine(string name, IEnumerator routine)
            {
                var coroutine = StartCoroutine(routine);
                var gameCoroutine = new GameCoroutine();
                gameCoroutine.Coroutine = coroutine;
                gameCoroutine.Name = name;
                gameCoroutine.BindBehaviour = null;
                return gameCoroutine;
            }

            public static void StopCoroutine(GameCoroutine coroutine)
            {
                if (coroutine.Coroutine != null)
                {
                    var behaviour = coroutine.BindBehaviour;
                    if (behaviour != null)
                    {
                        behaviour.StopCoroutine(coroutine.Coroutine);
                    }

                    coroutine.Coroutine = null;
                    coroutine.BindBehaviour = null;
                }
            }

            private static GameCoroutine StartCoroutine(MonoBehaviour behaviour, string name, IEnumerator routine)
            {
                var coroutine = behaviour.StartCoroutine(routine);
                var gameCoroutine = new GameCoroutine
                {
                    Coroutine = coroutine,
                    Name = name,
                    BindBehaviour = behaviour
                };
                return gameCoroutine;
            }

            private static GameCoroutineAgent GetDefaultBehaviour(GameObject bindGameObject)
            {
                if (bindGameObject != null)
                {
                    if (bindGameObject.TryGetComponent(out GameCoroutineAgent coroutineBehaviour))
                    {
                        return coroutineBehaviour;
                    }

                    return bindGameObject.AddComponent<GameCoroutineAgent>();
                }

                return null;
            }


            public static Coroutine StartCoroutine(string methodName)
            {
                if (string.IsNullOrEmpty(methodName))
                {
                    return null;
                }

                _MakeEntity();
                return m_monoDriver.StartCoroutine(methodName);
            }

            public static Coroutine StartCoroutine(IEnumerator routine)
            {
                if (routine == null)
                {
                    return null;
                }

                _MakeEntity();
                return m_monoDriver.StartCoroutine(routine);
            }

            public static Coroutine StartCoroutine(string methodName, [DefaultValue("null")] object value)
            {
                if (string.IsNullOrEmpty(methodName))
                {
                    return null;
                }

                _MakeEntity();
                return m_monoDriver.StartCoroutine(methodName, value);
            }

            public static void StopCoroutine(string methodName)
            {
                if (string.IsNullOrEmpty(methodName))
                {
                    return;
                }

                _MakeEntity();
                m_monoDriver.StopCoroutine(methodName);
            }

            public static void StopCoroutine(IEnumerator routine)
            {
                if (routine == null)
                {
                    return;
                }

                _MakeEntity();
                m_monoDriver.StopCoroutine(routine);
            }

            public static void StopCoroutine(Coroutine routine)
            {
                if (routine == null)
                {
                    return;
                }

                _MakeEntity();
                m_monoDriver.StopCoroutine(routine);
                routine = null;
            }

            public static void StopAllCoroutines()
            {
                _MakeEntity();
                m_monoDriver.StopAllCoroutines();
            }

            #endregion

            #region 注入UnityUpdate/FixedUpdate/LateUpdate

            /// <summary>
            /// 为给外部提供的 添加帧更新事件。
            /// </summary>
            /// <param name="fun"></param>
            public static void AddUpdateListener(Action fun)
            {
                _MakeEntity();
                AddUpdateListenerImp(fun).Forget();
            }

            private static async UniTaskVoid AddUpdateListenerImp(Action fun)
            {
                await UniTask.Yield( /*PlayerLoopTiming.LastPreUpdate*/);
                m_monoDriver.AddUpdateListener(fun);
            }

            /// <summary>
            /// 为给外部提供的 添加物理帧更新事件。
            /// </summary>
            /// <param name="fun"></param>
            public static void AddFixedUpdateListener(Action fun)
            {
                _MakeEntity();
                AddFixedUpdateListenerImp(fun).Forget();
            }

            private static async UniTaskVoid AddFixedUpdateListenerImp(Action fun)
            {
                await UniTask.Yield(PlayerLoopTiming.LastEarlyUpdate);
                m_monoDriver.AddFixedUpdateListener(fun);
            }

            /// <summary>
            /// 为给外部提供的 添加Late帧更新事件。
            /// </summary>
            /// <param name="fun"></param>
            public static void AddLateUpdateListener(Action fun)
            {
                _MakeEntity();
                AddLateUpdateListenerImp(fun).Forget();
            }

            private static async UniTaskVoid AddLateUpdateListenerImp(Action fun)
            {
                await UniTask.Yield( /*PlayerLoopTiming.LastPreLateUpdate*/);
                m_monoDriver.AddLateUpdateListener(fun);
            }

            /// <summary>
            /// 移除帧更新事件。
            /// </summary>
            /// <param name="fun"></param>
            public static void RemoveUpdateListener(Action fun)
            {
                _MakeEntity();
                m_monoDriver.RemoveUpdateListener(fun);
            }

            /// <summary>
            /// 移除物理帧更新事件。
            /// </summary>
            /// <param name="fun"></param>
            public static void RemoveFixedUpdateListener(Action fun)
            {
                _MakeEntity();
                m_monoDriver.RemoveFixedUpdateListener(fun);
            }

            /// <summary>
            /// 移除Late帧更新事件。
            /// </summary>
            /// <param name="fun"></param>
            public static void RemoveLateUpdateListener(Action fun)
            {
                _MakeEntity();
                m_monoDriver.RemoveLateUpdateListener(fun);
            }

            #endregion

            #region Unity Events 注入

            /// <summary>
            /// 为给外部提供的Destroy注册事件。
            /// </summary>
            /// <param name="fun"></param>
            public static void AddDestroyListener(Action fun)
            {
                _MakeEntity();
                m_monoDriver.AddDestroyListener(fun);
            }

            /// <summary>
            /// 为给外部提供的Destroy反注册事件。
            /// </summary>
            /// <param name="fun"></param>
            public static void RemoveDestroyListener(Action fun)
            {
                _MakeEntity();
                m_monoDriver.RemoveDestroyListener(fun);
            }

            /// <summary>
            /// 为给外部提供的OnDrawGizmos注册事件。
            /// </summary>
            /// <param name="fun"></param>
            public static void AddOnDrawGizmosListener(Action fun)
            {
                _MakeEntity();
                m_monoDriver.AddOnDrawGizmosListener(fun);
            }

            /// <summary>
            /// 为给外部提供的OnDrawGizmos反注册事件。
            /// </summary>
            /// <param name="fun"></param>
            public static void RemoveOnDrawGizmosListener(Action fun)
            {
                _MakeEntity();
                m_monoDriver.RemoveOnDrawGizmosListener(fun);
            }

            /// <summary>
            /// 为给外部提供的OnApplicationPause注册事件。
            /// </summary>
            /// <param name="fun"></param>
            public static void AddOnApplicationPauseListener(Action<bool> fun)
            {
                _MakeEntity();
                m_monoDriver.AddOnApplicationPauseListener(fun);
            }

            /// <summary>
            /// 为给外部提供的OnApplicationPause反注册事件。
            /// </summary>
            /// <param name="fun"></param>
            public static void RemoveOnApplicationPauseListener(Action<bool> fun)
            {
                _MakeEntity();
                m_monoDriver.RemoveOnApplicationPauseListener(fun);
            }

            #endregion

            private static void _MakeEntity()
            {
                if (m_monoDriver != null)
                {
                    return;
                }

                m_monoDriver = ModuleSystem.GetModule<IMonoDriver>();
            }

            #region FindObjectOfType

            public static T FindObjectOfType<T>() where T : UnityEngine.Object
                =>
#if UNITY_6000_0_OR_NEWER
                UnityEngine.Object.FindFirstObjectByType<T>();
#else
                UnityEngine.Object.FindObjectOfType<T>();
#endif

            #endregion

            #region 自定义组件事件管理

            /// <summary>
            /// 添加自定义事件监听器到指定控件的 EventTrigger 上。
            /// </summary>
            /// <param name="control">要添加监听器的控件</param>
            /// <param name="type">事件类型</param>
            /// <param name="action">回调函数</param>
            public static void AddCustomEventListener(UIBehaviour control, EventTriggerType type, UnityAction<BaseEventData> action)
            {
                // 找到控件身上的EventTrigger组件
                EventTrigger trigger = AddMonoBehaviour<EventTrigger>(control);
                // 创建一个 EventTrigger.Entry 条目并设置事件类型和回调函数
                EventTrigger.Entry entry = new EventTrigger.Entry
                {
                    eventID = type
                };
                if (entry.callback == null)
                {
                    entry.callback = new EventTrigger.TriggerEvent();
                }
                entry.callback.AddListener(action);
                // 将条目添加到 EventTrigger 的触发器列表中
                trigger.triggers?.Add(entry);
            }

            /// <summary>
            /// 移除自定义事件监听器到指定控件的 EventTrigger 上。
            /// </summary>
            /// <param name="control">要添加监听器的控件</param>
            /// <param name="type">事件类型</param>
            /// <param name="action">回调函数</param>
            public static void RemoveCustomEventListener(UIBehaviour control, EventTriggerType type, UnityAction<BaseEventData> action)
            {
                // 找到控件身上的EventTrigger组件
                EventTrigger trigger = control.GetComponent<EventTrigger>();
                if (trigger?.triggers != null)
                {
                    EventTrigger.Entry entry;
                    for (int i = 0; i < trigger.triggers.Count; i++)
                    {
                        entry = trigger.triggers[i];
                        if (entry?.callback == null)
                        {
                            continue;
                        }
                        if (entry.eventID == type && entry.callback.GetPersistentMethodName(0) == action.Method.Name)
                        {
                            // 移除匹配的条目
                            trigger.triggers.RemoveAt(i);
                            break;
                        }
                    }
                    trigger.triggers.RemoveAll(e => e?.callback == null || e.callback?.GetPersistentEventCount() == 0);
                }
            }

            #endregion

            #region AddComponent

            [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
            public static Component AddMonoBehaviour(Type type, GameObject go)
            {
                if (!go.TryGetComponent(type, out var comp))
                {
                    comp = go.AddComponent(type);
                }

                return comp;
            }

            public static T AddMonoBehaviour<T>(Component comp) where T : Component
            {
                if (!comp.TryGetComponent<T>(out var ret))
                {
                    ret = comp.AddComponent<T>();
                }
                return ret;
            }

            public static T AddMonoBehaviour<T>(GameObject go) where T : Component
            {
                if (!go.TryGetComponent<T>(out var comp))
                {
                    comp = go.AddComponent<T>();
                }
                return comp;
            }

            [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
            public static void RmvMonoBehaviour(Type type, GameObject go)
            {
                if (go.TryGetComponent(type, out var comp))
                {
                    UnityEngine.Object.Destroy(comp);
                }
            }

            public static void RmvMonoBehaviour<T>(GameObject go) where T : Component
            {
                if (go.TryGetComponent<T>(out var comp))
                {
                    UnityEngine.Object.Destroy(comp);
                }
            }

            public static void RmvMonoBehaviour<T>(Component go) where T : Component
            {
                if (go.TryGetComponent<T>(out var comp))
                {
#if UNITY_EDITOR
                    string assetPath = UnityEditor.AssetDatabase.GetAssetPath(comp);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        Debug.LogError($"试图销毁 Asset: {assetPath}");
                        return;
                    }
#endif
                    UnityEngine.Object.DestroyImmediate(comp);
                }
            }

            #endregion

            #region Utils

            public static Transform FindChild(Transform transform, string path)
            {
                var findTrans = transform.Find(path);
                if (findTrans != null)
                {
                    return findTrans;
                }

                return null;
            }

            /// <summary>
            /// 根据名字找到子节点，主要用于dummy接口
            /// </summary>
            /// <param name="transform"></param>
            /// <param name="name"></param>
            /// <returns></returns>
            public static Transform FindChildByName(Transform transform, string name)
            {
                if (transform == null)
                {
                    return null;
                }

                for (int i = 0; i < transform.childCount; i++)
                {
                    var childTrans = transform.GetChild(i);
                    if (childTrans.name == name)
                    {
                        return childTrans;
                    }

                    var find = FindChildByName(childTrans, name);
                    if (find != null)
                    {
                        return find;
                    }
                }

                return null;
            }

            [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
            public static Component FindChildComponent(Type type, Transform transform, string path)
            {
                var findTrans = transform.Find(path);
                if (findTrans != null)
                {
                    return findTrans.gameObject.TryGetComponent(type, out var comp) ? comp : null;
                }

                return null;
            }

            public static T FindChildComponent<T>(Transform transform, string path) where T : Component
            {
                var findTrans = transform.Find(path);
                if (findTrans != null)
                {
                    return findTrans.gameObject.TryGetComponent<T>(out var comp) ? comp : null;
                }

                return null;
            }

            public static void SetLayer(GameObject go, int layer)
            {
                if (go == null)
                {
                    return;
                }
                SetLayer(go.transform, layer);
            }

            public static void SetLayer(Transform trans, int layer)
            {
                if (trans == null)
                {
                    return;
                }
                var allTrans = trans.GetComponentsInChildren<Transform>();

                for (int i = 0; i < allTrans.Length; i++)
                {
                    allTrans[i].gameObject.layer = layer;
                }
            }

            public static int RandomRangeInt(int min, int max)
                => UnityEngine.Random.Range(min, max);

            public static float RandomRangeFloat(float min, float max)
                => UnityEngine.Random.Range(min, max);

            public static Vector2 RandomInsideCircle(float radius)
                => UnityEngine.Random.insideUnitCircle * radius;

            [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
            public static Array CreateUnityArray(Type type, int length)
                => length >= 0 ? Array.CreateInstance(type, length) : null;

            public static T[] CreateUnityArray<T>(int length)
                => length >= 0 ? new T[length] : null;

            public static GameObject Instantiate(GameObject go)
            => go == null ? null : UnityEngine.Object.Instantiate(go);

            public static T Instantiate<T>(T go) where T : UnityEngine.Object
                => go == null ? null : UnityEngine.Object.Instantiate(go);

            public static bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance, int layerMask)
                => Physics.Raycast(ray, out hitInfo, maxDistance, layerMask);

            public static List<string> GetRegexMatchGroups(string pattern,string input)
            {
                if (string.IsNullOrEmpty(pattern) || string.IsNullOrEmpty(input))
                {
                    return new List<string>();
                }
                var match = Regex.Match(input, pattern);
                var results = new List<string>(match.Groups.Count);
                foreach (Group group in match.Groups)
                {
                    results.Add(group.Value);
                }
                return results;
            }

            public static void SetMaterialVector3(Material mat, int nameId, Vector3 val)
            {
                mat.SetVector(nameId, val);
            }

            public static bool TryGetTouchByFingerId(int fingerId, out Touch findTouch)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var touch = Input.GetTouch(i);
                    if (touch.fingerId == fingerId)
                    {
                        findTouch = touch;
                        return true;
                    }
                }

                findTouch = default;
                return false;
            }

            public static int GetHashCodeByString(string str)
                => str.GetHashCode();

            #endregion

            #region ResolutionHelper

            private static Resolution[] s_resolutions;

            public static Resolution[] GetResolutions() => s_resolutions != null ? s_resolutions : s_resolutions = Screen.resolutions;

            public static void SetScreenResolution(int width, int height, bool fullscreen)
            {
                Screen.SetResolution(width, height, fullscreen);
            }

            public static void SetScreenResolutionWithMode(int width, int height, FullScreenMode mode)
            {
                Screen.SetResolution(width, height, mode);
            }

            public static void SetScreenResolution(int index, bool fullscreen)
            {
                if (index < 0 || index >= Screen.resolutions.Length)
                {
                    return;
                }
                var resolution = Screen.resolutions[index];
                Screen.SetResolution(resolution.width, resolution.height, fullscreen);
            }

            public static void SetScreenResolutionWithMode(int index, FullScreenMode mode)
            {
                if (index < 0 || index >= Screen.resolutions.Length)
                {
                    return;
                }
                var resolution = Screen.resolutions[index];
                Screen.SetResolution(resolution.width, resolution.height, mode);
            }

            #endregion
        }

        public class GameCoroutine
        {
            public string Name;
            public Coroutine Coroutine;
            public MonoBehaviour BindBehaviour;
        }

        class GameCoroutineAgent : MonoBehaviour
        {
        }
    }
}