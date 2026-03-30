using System;
using System.Diagnostics;
using UnityEngine;

namespace DGame
{
    internal partial class MonoDriver
    {
        private class MainMonoBehaviour : MonoBehaviour
        {
            private event Action OnUpdateEvent;
            private event Action OnFixedUpdateEvent;
            private event Action OnLateUpdateEvent;
            private event Action OnDestroyEvent;
            private event Action OnDrawGizmosEvent;
            private event Action OnDrawGizmosSelectedEvent;
            private event Action<bool> OnApplicationPauseEvent;

            private void Update()
            {
                OnUpdateEvent?.Invoke();
            }

            private void FixedUpdate()
            {
                OnFixedUpdateEvent?.Invoke();
            }

            private void LateUpdate()
            {
                OnLateUpdateEvent?.Invoke();
            }

            private void OnDestroy()
            {
                OnDestroyEvent?.Invoke();
            }

            [Conditional("UNITY_EDITOR")]
            private void OnDrawGizmos()
            {
                OnDrawGizmosEvent?.Invoke();
            }

            [Conditional("UNITY_EDITOR")]
            private void OnDrawGizmosSelected()
            {
                OnDrawGizmosSelectedEvent?.Invoke();
            }

            private void OnApplicationPause(bool pauseStatus)
            {
                OnApplicationPauseEvent?.Invoke(pauseStatus);
            }

            #region 事件添加删除操作

            public void AddUpdateListener(Action action)
            {
                OnUpdateEvent += action;
            }

            public void RemoveUpdateListener(Action action)
            {
                OnUpdateEvent -= action;
            }

            public void AddFixedUpdateListener(Action action)
            {
                OnFixedUpdateEvent += action;
            }

            public void RemoveFixedUpdateListener(Action action)
            {
                OnFixedUpdateEvent -= action;
            }

            public void AddLateUpdateListener(Action action)
            {
                OnLateUpdateEvent += action;
            }

            public void RemoveLateUpdateListener(Action action)
            {
                OnLateUpdateEvent -= action;
            }

            public void AddDestroyListener(Action action)
            {
                OnDestroyEvent += action;
            }

            public void RemoveDestroyListener(Action action)
            {
                OnDestroyEvent -= action;
            }

            [Conditional("UNITY_EDITOR")]
            public void AddOnDrawGizmosListener(Action action)
            {
                OnDrawGizmosEvent += action;
            }

            [Conditional("UNITY_EDITOR")]
            public void RemoveOnDrawGizmosListener(Action action)
            {
                OnDrawGizmosEvent -= action;
            }

            [Conditional("UNITY_EDITOR")]
            public void AddOnDrawGizmosSelectedListener(Action action)
            {
                OnDrawGizmosSelectedEvent += action;
            }

            [Conditional("UNITY_EDITOR")]
            public void RemoveOnDrawGizmosSelectedListener(Action action)
            {
                OnDrawGizmosSelectedEvent -= action;
            }

            public void AddOnApplicationPauseListener(Action<bool> action)
            {
                OnApplicationPauseEvent += action;
            }

            public void RemoveOnApplicationPauseListener(Action<bool> action)
            {
                OnApplicationPauseEvent -= action;
            }

            #endregion

            public void Destroy()
            {
                OnUpdateEvent = null;
                OnFixedUpdateEvent = null;
                OnLateUpdateEvent = null;
                OnDestroyEvent = null;
                OnDrawGizmosEvent = null;
                OnDrawGizmosSelectedEvent = null;
                OnApplicationPauseEvent = null;
            }
        }
    }
}