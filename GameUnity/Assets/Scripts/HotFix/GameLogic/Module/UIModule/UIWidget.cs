using System.Collections.Generic;
using DGame;
using UnityEngine;

namespace GameLogic
{
    public abstract class UIWidget : UIBase
    {
        public override GameObject gameObject { get; protected set; }

        private Transform m_transform;

        /// <summary>
        /// 矩阵位置组件
        /// </summary>
        public override RectTransform rectTransform => m_transform as RectTransform;

        /// <summary>
        /// 位置组件
        /// </summary>
        public override Transform transform => m_transform;

        public override UIType Type => UIType.Widget;

        /// <summary>
        /// 窗口组件名称
        /// </summary>
        public string WidgetName { get; protected set; } = string.Empty;

        /// <summary>
        /// 所属窗口
        /// </summary>
        public UIWindow ParentWindow
        {
            get
            {
                var parentUI = m_parent;

                while (parentUI != null)
                {
                    if (parentUI.Type == UIType.Window)
                    {
                        return parentUI as UIWindow;
                    }
                    parentUI = parentUI.Parent;
                }
                return null;
            }
        }

        /// <summary>
        /// 组件可见性
        /// </summary>
        protected override bool Visible
        {
            get => gameObject != null && gameObject.activeSelf;
            set
            {
                if (gameObject == null || gameObject.activeSelf == value)
                {
                    return;
                }

                gameObject.SetActive(value);
                if (value)
                {
                    OnVisible();
                }
                else
                {
                    OnHidden();
                }
            }
        }

        /// <summary>
        /// 内部更新方法
        /// </summary>
        /// <returns>是否需要继续更新</returns>
        public bool InternalUpdate()
        {
            if (!IsPrepared || IsDestroyed || !Visible)
            {
                return false;
            }

            List<UIWidget> listNextUpdateChild = null;

            if (ChildList != null && ChildList.Count > 0)
            {
                listNextUpdateChild = m_updateChildList;
                var updateListDirty = m_updateListDirty;
                List<UIWidget> childList;
                if (m_updateListDirty)
                {
                    if (listNextUpdateChild == null)
                    {
                        listNextUpdateChild = new List<UIWidget>();
                        m_updateChildList = listNextUpdateChild;
                    }
                    else
                    {
                        listNextUpdateChild.Clear();
                    }
                    childList = ChildList;
                }
                else
                {
                    childList = listNextUpdateChild;
                }

                for (int i = 0; i < childList.Count; i++)
                {
                    var uiWidget = childList[i];

                    if (uiWidget == null)
                    {
                        continue;
                    }
                    var childNeedUpdate = uiWidget.InternalUpdate();

                    if (updateListDirty && childNeedUpdate)
                    {
                        listNextUpdateChild.Add(uiWidget);
                    }
                }

                if (updateListDirty)
                {
                    m_updateListDirty = false;
                }
            }

            bool needUpdate = false;

            if (listNextUpdateChild is null || listNextUpdateChild.Count <= 0)
            {
                m_hasOverrideUpdate = true;
                OnUpdate();
                needUpdate = m_hasOverrideUpdate;
            }
            else
            {
                OnUpdate();
                needUpdate = true;
            }

            return needUpdate;
        }

        #region Create

        /// <summary>
        /// 通过资源路径创建UIWidget
        /// </summary>
        /// <param name="resPath">资源路径</param>
        /// <param name="parentUI">父UI</param>
        /// <param name="parentTrans">父节点</param>
        /// <param name="visible">是否可见</param>
        /// <returns>是否创建成功</returns>
        public bool CreateByPath(string resPath, UIBase parentUI, Transform parentTrans = null, bool visible = true)
        {
            GameObject goInstance = UIModule.ResourceLoader.LoadGameObject(resPath, parentTrans);
            if (goInstance == null)
            {
                return false;
            }

            if (!Create(parentUI, goInstance, visible))
            {
                return false;
            }

            goInstance.transform.localScale = Vector3.one;
            goInstance.transform.localPosition = Vector3.zero;
            return true;
        }

        /// <summary>
        /// 创建UIWidget
        /// </summary>
        /// <param name="parentUI">父UI</param>
        /// <param name="widgetPrefab">组件预制体</param>
        /// <param name="visible">是否可见</param>
        /// <returns>是否创建成功</returns>
        public bool Create(UIBase parentUI, GameObject widgetPrefab, bool visible = true)
        {
            return CreateImp(parentUI, widgetPrefab, visible);
        }

        /// <summary>
        /// 通过预制体创建UIWidget
        /// </summary>
        /// <param name="parentUI">父UI</param>
        /// <param name="goPrefab">预制体对象</param>
        /// <param name="parentTrans">父节点</param>
        /// <param name="visible">是否可见</param>
        /// <returns>是否创建成功</returns>
        public bool CreateByPrefab(UIBase parentUI, GameObject goPrefab, Transform parentTrans, bool visible = true)
        {
            if (parentTrans == null)
            {
                parentTrans = parentUI.transform;
            }

            return CreateImp(parentUI, Object.Instantiate(goPrefab, parentTrans), visible);
        }

        private bool CreateImp(UIBase parentUI, GameObject widgetPrefab, bool visible = true)
        {
            if (!CreateBase(widgetPrefab))
            {
                return false;
            }

            ResetChildCanvas(parentUI);
            m_parent = parentUI;
            Parent.AddChild(this);
            if (gameObject.GetComponent<Canvas>())
            {
                (Parent as UIWindow)?.MakeChildCanvasDirty();
            }
            ScriptGenerator();
            BindMemberProperty();
            RegisterEvent();
            OnCreate();
            OnRefresh();
            IsPrepared = true;
            if (visible)
            {
                Show(true);
            }
            else
            {
                gameObject?.SetActive(false);
            }

            return true;
        }

        private bool CreateBase(GameObject go)
        {
            if (go == null)
            {
                return false;
            }

            WidgetName = GetType().Name;
            gameObject = go;
            m_transform = gameObject?.transform;
            DLogger.Assert(rectTransform != null, $"{go.name} UI元素必须具有 RectTransform");
            return true;
        }

        protected void ResetChildCanvas(UIBase parentUI)
        {
            if (parentUI == null || parentUI.gameObject == null)
            {
                return;
            }

            Canvas parentCanvas = parentUI.gameObject.GetComponentInParent<Canvas>();

            if (parentCanvas == null)
            {
                return;
            }

            if (gameObject != null)
            {
                var listCanvas = gameObject.GetComponentsInChildren<Canvas>(true);

                for (int i = 0; i < listCanvas.Length; i++)
                {
                    var childCanvas = listCanvas[i];
                    childCanvas.sortingOrder = parentCanvas.sortingOrder + childCanvas.sortingOrder % UIModule.EACH_WINDOW_DEEP;
                }
            }
        }

        #endregion

        #region Destroy

        private void InternalDestroyWidget()
        {
            RemoveAllUIEvents();

            for (int i = ChildList.Count - 1; i >= 0; i--)
            {
                ChildList[i]?.Destroy();
            }

            if (gameObject != null)
            {
                if (gameObject.GetComponent<Canvas>())
                {
                    (Parent as UIWindow)?.MakeChildCanvasDirty();
                }
                Object.Destroy(gameObject);
                gameObject = null;
            }
            IsDestroyed = true;
        }

        /// <summary>
        /// 销毁UIWidget
        /// </summary>
        public void Destroy()
        {
            if (IsDestroyed)
            {
                return;
            }
            Parent?.RemoveChild(this);
            OnDestroy();
            InternalDestroyWidget();
        }

        #endregion

        /// <summary>
        /// 显示/隐藏UIWidget
        /// </summary>
        /// <param name="visible">是否可见</param>
        public void Show(bool visible)
        {
            Visible = visible;
        }
    }
}