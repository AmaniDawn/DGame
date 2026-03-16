using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DGame;
using UnityEngine;
#if ENABLE_OBFUZ
using Obfuz;
#endif

namespace GameLogic
{
    /// <summary>
    /// UI基类
    /// </summary>
#if ENABLE_OBFUZ
    [ObfuzIgnore(ObfuzScope.TypeName, ApplyToChildTypes = true)]
#endif
    public abstract class UIBase
    {
        protected UIBase m_parent = null;

        /// <summary>
        /// UI父节点
        /// </summary>
        public UIBase Parent => m_parent;

        protected System.Object[] m_userDatas;

        /// <summary>
        /// 自定义数据
        /// </summary>
        public System.Object UserData => m_userDatas != null && m_userDatas.Length >= 1 ? m_userDatas[0] : null;

        /// <summary>
        /// 自定义数据集
        /// </summary>
        public System.Object[] UserDatas => m_userDatas;

        /// <summary>
        /// 窗口的实例资源对象
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public virtual GameObject gameObject { get; protected set; }

        /// <summary>
        /// 窗口的位置组件
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public virtual Transform transform { get; protected set; }

        /// <summary>
        /// 窗口的矩阵位置组件
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public virtual RectTransform rectTransform { get; protected set; }

        /// <summary>
        /// UI类型
        /// </summary>
        public virtual UIType Type => UIType.None;

        /// <summary>
        /// 资源是否准备完毕
        /// </summary>
        public bool IsPrepared { get; protected set; }

        /// <summary>
        /// 资源是否被销毁
        /// </summary>
        internal bool IsDestroyed { get; set; }

        protected virtual bool Visible { get; set; }

        /// <summary>
        /// UI子组件列表
        /// </summary>
        internal readonly List<UIWidget> ChildList = new List<UIWidget>();

        /// <summary>
        /// 存在Update更新的UI子组件列表
        /// </summary>
        protected List<UIWidget> m_updateChildList = null;

        /// <summary>
        /// 是否重建更新列表
        /// </summary>
        protected bool m_updateListDirty = true;

        /// <summary>
        /// 是否标记脏排序
        /// </summary>
        protected bool m_isSortingOrderDirty = false;

        /// <summary>
        /// 代码自动生成绑定
        /// </summary>
        protected virtual void ScriptGenerator()
        {
        }

        /// <summary>
        /// 绑定UI成员元素
        /// </summary>
        protected virtual void BindMemberProperty()
        {
        }

        /// <summary>
        /// 注册事件
        /// </summary>
        protected virtual void RegisterEvent()
        {
        }

        /// <summary>
        /// 窗口创建
        /// </summary>
        protected virtual void OnCreate()
        {
        }

        /// <summary>
        /// 窗口刷新。
        /// </summary>
        protected virtual void OnRefresh()
        {
        }

        /// <summary>
        /// 是否需要Update
        /// </summary>
        protected bool m_hasOverrideUpdate = true;

        /// <summary>
        /// 窗口更新
        /// </summary>
        protected virtual void OnUpdate() => m_hasOverrideUpdate = false;

        /// <summary>
        /// 窗口销毁
        /// </summary>
        protected virtual void OnDestroy()
        {
        }

        /// <summary>
        /// 当触发窗口的层级排序。
        /// </summary>
        protected void _OnSortingOrderChange()
        {
            m_isSortingOrderDirty = false;

            if (ChildList != null)
            {
                for (int i = 0; i < ChildList.Count; i++)
                {
                    ChildList[i]?.OnSortingOrderChange();
                }
            }

            OnSortingOrderChange();
        }

        /// <summary>
        /// 触发窗口的层级排序
        /// </summary>
        protected virtual void OnSortingOrderChange()
        {
        }

        /// <summary>
        /// 当因为全屏遮挡触发或者窗口可见性触发窗口的显隐
        /// </summary>
        protected virtual void OnVisible()
        {
        }

        /// <summary>
        /// 界面不可见的时候调用
        /// 当被上层全屏界面覆盖后，也会触发一次隐藏
        /// </summary>
        protected virtual void OnHidden()
        {
        }

        internal void AddChild(UIWidget child)
        {
            if (ChildList != null)
            {
                ChildList.Add(child);
                MarkUpdateDirty();
            }
        }

        internal void RemoveChild(UIWidget child)
        {
            if (ChildList != null)
            {
                if (ChildList.Remove(child))
                {
                    MarkUpdateDirty();
                }
            }
        }

        private void MarkUpdateDirty()
        {
            m_updateListDirty = true;
            Parent?.MarkUpdateDirty();
        }

        #region FindChildComponent

        protected Transform FindChild(string path)
            => FindChildImp(rectTransform, path);

        private Transform FindChild(Transform trans, string path)
            => FindChildImp(trans, path);

        /// <summary>
        /// 查找子组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="path">子节点路径</param>
        /// <returns>找到的组件</returns>
        public T FindChildComponent<T>(string path) where T : Component
            => FindChildComponentImp<T>(rectTransform, path);

        protected T FindChildComponent<T>(Transform trans, string path) where T : Component
            => FindChildComponentImp<T>(trans, path);

        private static Transform FindChildImp(Transform trans, string path) => trans.Find(path);

        private static T FindChildComponentImp<T>(Transform trans, string path) where T : Component
            => trans.Find(path)?.gameObject.GetComponent<T>();

        #endregion

        #region UIEvent

        private GameEventDriver m_eventDriver;

        protected GameEventDriver EventDriver =>
            m_eventDriver == null ? m_eventDriver = MemoryPool.Spawn<GameEventDriver>() : m_eventDriver;

        /// <summary>
        /// 添加UI事件
        /// </summary>
        /// <param name="eventID">事件ID</param>
        /// <param name="handler">事件处理函数</param>
        public void AddUIEvent(int eventID, Action handler)
            => EventDriver.AddUIEvent(eventID, handler);

        /// <summary>
        /// 添加UI事件（带1个参数）
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="eventID">事件ID</param>
        /// <param name="handler">事件处理函数</param>
        public void AddUIEvent<T>(int eventID, Action<T> handler)
            => EventDriver.AddUIEvent(eventID, handler);

        /// <summary>
        /// 添加UI事件（带2个参数）
        /// </summary>
        /// <typeparam name="T1">第1个参数类型</typeparam>
        /// <typeparam name="T2">第2个参数类型</typeparam>
        /// <param name="eventID">事件ID</param>
        /// <param name="handler">事件处理函数</param>
        public void AddUIEvent<T1, T2>(int eventID, Action<T1, T2> handler)
            => EventDriver.AddUIEvent(eventID, handler);

        /// <summary>
        /// 添加UI事件（带3个参数）
        /// </summary>
        /// <typeparam name="T1">第1个参数类型</typeparam>
        /// <typeparam name="T2">第2个参数类型</typeparam>
        /// <typeparam name="T3">第3个参数类型</typeparam>
        /// <param name="eventID">事件ID</param>
        /// <param name="handler">事件处理函数</param>
        public void AddUIEvent<T1, T2, T3>(int eventID, Action<T1, T2, T3> handler)
            => EventDriver.AddUIEvent(eventID, handler);

        /// <summary>
        /// 添加UI事件（带4个参数）
        /// </summary>
        /// <typeparam name="T1">第1个参数类型</typeparam>
        /// <typeparam name="T2">第2个参数类型</typeparam>
        /// <typeparam name="T3">第3个参数类型</typeparam>
        /// <typeparam name="T4">第4个参数类型</typeparam>
        /// <param name="eventID">事件ID</param>
        /// <param name="handler">事件处理函数</param>
        public void AddUIEvent<T1, T2, T3, T4>(int eventID, Action<T1, T2, T3, T4> handler)
            => EventDriver.AddUIEvent(eventID, handler);

        /// <summary>
        /// 添加UI事件（带5个参数）
        /// </summary>
        /// <typeparam name="T1">第1个参数类型</typeparam>
        /// <typeparam name="T2">第2个参数类型</typeparam>
        /// <typeparam name="T3">第3个参数类型</typeparam>
        /// <typeparam name="T4">第4个参数类型</typeparam>
        /// <typeparam name="T5">第5个参数类型</typeparam>
        /// <param name="eventID">事件ID</param>
        /// <param name="handler">事件处理函数</param>
        public void AddUIEvent<T1, T2, T3, T4, T5>(int eventID, Action<T1, T2, T3, T4, T5> handler)
            => EventDriver.AddUIEvent(eventID, handler);

        /// <summary>
        /// 添加UI事件（带6个参数）
        /// </summary>
        /// <typeparam name="T1">第1个参数类型</typeparam>
        /// <typeparam name="T2">第2个参数类型</typeparam>
        /// <typeparam name="T3">第3个参数类型</typeparam>
        /// <typeparam name="T4">第4个参数类型</typeparam>
        /// <typeparam name="T5">第5个参数类型</typeparam>
        /// <typeparam name="T6">第6个参数类型</typeparam>
        /// <param name="eventID">事件ID</param>
        /// <param name="handler">事件处理函数</param>
        public void AddUIEvent<T1, T2, T3, T4, T5, T6>(int eventID, Action<T1, T2, T3, T4, T5, T6> handler)
            => EventDriver.AddUIEvent(eventID, handler);

        protected void RemoveAllUIEvents()
        {
            if (m_eventDriver != null)
            {
                MemoryPool.Release(m_eventDriver);
                m_eventDriver = null;
            }
        }

        #endregion

        #region Create UIWidget

        /// <summary>
        /// 创建UIWidget 通过父UI位置节点
        /// <remarks>资源实例已存在父物体所以不需要异步创建</remarks>
        /// </summary>
        /// <param name="goPath">父UI位置节点</param>
        /// <param name="visible">是否可见</param>
        /// <typeparam name="T">UIWidget</typeparam>
        /// <returns></returns>
        public T CreateWidget<T>(string goPath, bool visible = true) where T : UIWidget, new()
        {
            var goRootTrans = FindChild(goPath);

            if (goRootTrans != null)
            {
                return CreateWidget<T>(goRootTrans.gameObject, visible);
            }

            return null;
        }

        /// <summary>
        /// 创建UIWidget 通过父UI位置节点
        /// <remarks>资源实例已存在父物体所以不需要异步创建</remarks>
        /// </summary>
        /// <param name="parentTrans">父UI位置</param>
        /// <param name="goPath">父UI位置节点</param>
        /// <param name="visible">是否可见</param>
        /// <typeparam name="T">UIWidget</typeparam>
        /// <returns></returns>
        public T CreateWidget<T>(Transform parentTrans, string goPath, bool visible = true) where T : UIWidget, new()
        {
            var goRootTrans = FindChild(parentTrans, goPath);

            if (goRootTrans != null)
            {
                return CreateWidget<T>(goRootTrans.gameObject, visible);
            }

            return null;
        }


        /// <summary>
        /// 创建UIWidget 通过父UI位置节点
        /// <remarks>资源实例已存在父物体所以不需要异步创建</remarks>
        /// </summary>
        /// <param name="goRoot">游戏物体</param>
        /// <param name="visible">是否可见</param>
        /// <typeparam name="T">UIWidget</typeparam>
        /// <returns></returns>
        public T CreateWidget<T>(GameObject goRoot, bool visible = true) where T : UIWidget, new()
        {
            if (goRoot == null)
            {
                return null;
            }

            var widget = new T();

            if (widget.Create(this, goRoot, visible))
            {
                return widget;
            }

            return null;
        }

        /// <summary>
        /// 通过资源路径创建UIWidget
        /// </summary>
        /// <typeparam name="T">UIWidget类型</typeparam>
        /// <param name="parentTrans">父节点</param>
        /// <param name="assetLocation">资源路径</param>
        /// <param name="visible">是否可见</param>
        /// <returns>UIWidget实例</returns>
        public T CreateWidgetByPath<T>(Transform parentTrans, string assetLocation, bool visible = true)
            where T : UIWidget, new()
        {
            GameObject goInst = UIModule.ResourceLoader.LoadGameObject(assetLocation, parentTrans);
            return CreateWidget<T>(goInst, visible);
        }

        /// <summary>
        /// 异步通过资源路径创建UIWidget
        /// </summary>
        /// <typeparam name="T">UIWidget类型</typeparam>
        /// <param name="parentTrans">父节点</param>
        /// <param name="assetLocation">资源路径</param>
        /// <param name="visible">是否可见</param>
        /// <returns>UIWidget实例</returns>
        public async UniTask<T> CreateWidgetByPathAsync<T>(Transform parentTrans, string assetLocation,
            bool visible = true)
            where T : UIWidget, new()
        {
            if (gameObject == null)
            {
                return null;
            }

            GameObject goInst = await UIModule.ResourceLoader.LoadGameObjectAsync(assetLocation, parentTrans,
                gameObject.GetCancellationTokenOnDestroy());
            return CreateWidget<T>(goInst, visible);
        }


        /// <summary>
        /// 通过预制体创建UIWidget
        /// </summary>
        /// <typeparam name="T">UIWidget类型</typeparam>
        /// <param name="goPrefab">预制体对象</param>
        /// <param name="parentTrans">父节点</param>
        /// <param name="visible">是否可见</param>
        /// <returns>UIWidget实例</returns>
        public T CreateWidgetByPrefab<T>(GameObject goPrefab, Transform parentTrans = null, bool visible = true)
            where T : UIWidget, new()
        {
            var widget = new T();

            if (!widget.CreateByPrefab(this, goPrefab, parentTrans, visible))
            {
                return null;
            }

            return widget;
        }

        /// <summary>
        /// 通过类型名称创建UIWidget
        /// </summary>
        /// <typeparam name="T">UIWidget类型</typeparam>
        /// <param name="parentTrans">父节点</param>
        /// <param name="visible">是否可见</param>
        /// <returns>UIWidget实例</returns>
        public T CreateWidgetByType<T>(Transform parentTrans, bool visible = true)
            where T : UIWidget, new()
            => CreateWidgetByPath<T>(parentTrans, typeof(T).Name, visible);

        /// <summary>
        /// 异步通过类型名称创建UIWidget
        /// </summary>
        /// <typeparam name="T">UIWidget类型</typeparam>
        /// <param name="parentTrans">父节点</param>
        /// <param name="visible">是否可见</param>
        /// <returns>UIWidget实例</returns>
        public async UniTask<T> CreateWidgetByTypeAsync<T>(Transform parentTrans, bool visible = true)
            where T : UIWidget, new()
            => await CreateWidgetByPathAsync<T>(parentTrans, typeof(T).Name, visible);

        /// <summary>
        /// 调整列表项数量
        /// </summary>
        /// <typeparam name="T">UIWidget类型</typeparam>
        /// <param name="itemList">项目列表</param>
        /// <param name="count">目标数量</param>
        /// <param name="parentTrans">父节点</param>
        /// <param name="prefab">预制体</param>
        /// <param name="assetLocation">资源路径</param>
        public void AdjustItemNum<T>(List<T> itemList, int count, Transform parentTrans, GameObject prefab = null,
            string assetLocation = "") where T : UIWidget, new()
        {
            if (itemList == null)
            {
                DLogger.Error($"itemList is null, please check GameObject: {gameObject.name}.{parentTrans.name}!");
                return;
            }

            if (itemList.Count < count)
            {
                int needNCnt = count - itemList.Count;

                for (int i = 0; i < needNCnt; i++)
                {
                    T tempItem = prefab != null
                        ? CreateWidgetByPrefab<T>(prefab, parentTrans)
                        : CreateWidgetByType<T>(parentTrans);
                    itemList.Add(tempItem);
                }
            }
            else if (itemList.Count > count)
            {
                RemoveUnUseItem(itemList, count);
            }
        }

        /// <summary>
        /// 异步调整列表项数量
        /// </summary>
        /// <typeparam name="T">UIWidget类型</typeparam>
        /// <param name="itemList">项目列表</param>
        /// <param name="count">目标数量</param>
        /// <param name="parentTrans">父节点</param>
        /// <param name="prefab">预制体</param>
        /// <param name="assetLocation">资源路径</param>
        /// <param name="maxNumPerFrame">每帧最大处理数量</param>
        /// <param name="updateAction">更新回调</param>
        public void AsyncAdjustItemNum<T>(List<T> itemList, int count, Transform parentTrans, GameObject prefab = null,
            string assetLocation = "", int maxNumPerFrame = 5, Action<T, int> updateAction = null)
            where T : UIWidget, new()
            => AsyncAdjustItemNumInternal(itemList, count, parentTrans, maxNumPerFrame, updateAction, prefab,
                assetLocation).Forget();

        /// <summary>
        /// 异步等待调整列表项数量完成
        /// </summary>
        /// <typeparam name="T">UIWidget类型</typeparam>
        /// <param name="itemList">项目列表</param>
        /// <param name="count">目标数量</param>
        /// <param name="parentTrans">父节点</param>
        /// <param name="prefab">预制体</param>
        /// <param name="assetLocation">资源路径</param>
        /// <param name="maxNumPerFrame">每帧最大处理数量</param>
        /// <param name="updateAction">更新回调</param>
        /// <returns>异步任务</returns>
        public async UniTask AsyncAwaitAdjustItemNum<T>(List<T> itemList, int count, Transform parentTrans,
            GameObject prefab = null, string assetLocation = "", int maxNumPerFrame = 5,
            Action<T, int> updateAction = null)
            where T : UIWidget, new()
            => await AsyncAdjustItemNumInternal(itemList, count, parentTrans, maxNumPerFrame, updateAction, prefab,
                assetLocation);

        private async UniTask AsyncAdjustItemNumInternal<T>(List<T> itemList, int count, Transform parentTrans,
            int maxCntPerFrame, Action<T, int> updateAction, GameObject prefab, string assetLocation)
            where T : UIWidget, new()
        {
            if (itemList == null)
            {
                DLogger.Error($"itemList is null, please check GameObject: {gameObject.name}.{parentTrans.name}!");
                return;
            }

            int createCnt = 0;

            for (int i = 0; i < count; i++)
            {
                T tempT = null;

                if (i < itemList.Count)
                {
                    tempT = itemList[i];
                }
                else
                {
                    if (prefab != null)
                    {
                        tempT = CreateWidgetByPrefab<T>(prefab, parentTrans);
                    }
                    else
                    {
                        tempT = await CreateWidgetByPathAsync<T>(parentTrans, assetLocation);
                    }

                    itemList.Add(tempT);
                }

                int index = i;

                if (updateAction != null)
                {
                    updateAction(tempT, index);
                }

                createCnt++;

                if (createCnt >= maxCntPerFrame)
                {
                    createCnt = 0;
                    await UniTask.Yield();
                }
            }

            if (itemList.Count > count)
            {
                RemoveUnUseItem(itemList, count);
            }
        }

        private void RemoveUnUseItem<T>(List<T> itemList, int count) where T : UIWidget
        {
            for (int i = itemList.Count - 1; i >= count; i--)
            {
                var item = itemList[i];
                itemList.RemoveAt(i);
                item.Destroy();
            }
        }

        #endregion

        #region 红点相关

        /// <summary>
        /// 异步创建红点项
        /// </summary>
        /// <param name="redDotNodeID">红点节点ID</param>
        /// <param name="parent">父节点</param>
        /// <returns>红点项实例</returns>
        public async UniTask<RedDotItem> CreateRedDotAsync(int redDotNodeID, Transform parent)
        {
            var item = await CreateWidgetByTypeAsync<RedDotItem>(parent);
            item?.Init(redDotNodeID);
            return item;
        }

        /// <summary>
        /// 创建红点项
        /// </summary>
        /// <param name="redDotNodeID">红点节点ID</param>
        /// <param name="parent">父节点</param>
        /// <returns>红点项实例</returns>
        public RedDotItem CreateRedDot(int redDotNodeID, Transform parent)
        {
            var item = CreateWidgetByType<RedDotItem>(parent);
            item?.Init(redDotNodeID);
            return item;
        }

        #endregion
    }
}