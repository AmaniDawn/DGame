using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Internal;

namespace DGame
{
    public interface IMonoDriver
    {
        #region 协程控制相关

        public Coroutine StartCoroutine(string methodName);

        public Coroutine StartCoroutine(IEnumerator routine);

        public Coroutine StartCoroutine(string methodName, [DefaultValue("null")] object value);

        public void StopCoroutine(string methodName);

        public void StopCoroutine(IEnumerator routine);

        public void StopCoroutine(Coroutine routine);

        public void StopAllCoroutines();

        #endregion

        #region 注入 Unity Update/FixedUpdate/LateUpdate 事件

        public void AddUpdateListener(Action action);

        public void RemoveUpdateListener(Action action);

        public void AddFixedUpdateListener(Action action);

        public void RemoveFixedUpdateListener(Action action);

        public void AddLateUpdateListener(Action action);

        public void RemoveLateUpdateListener(Action action);

        #endregion

        #region Unity 生命周期事件注入

        public void AddDestroyListener(Action action);

        public void RemoveDestroyListener(Action action);

        public void AddOnDrawGizmosListener(Action action);

        public void RemoveOnDrawGizmosListener(Action action);

        public void AddOnDrawGizmosSelectedListener(Action action);

        public void RemoveOnDrawGizmosSelectedListener(Action action);

        public void AddOnApplicationPauseListener(Action<bool> action);

        public void RemoveOnApplicationPauseListener(Action<bool>  action);

        #endregion
    }
}