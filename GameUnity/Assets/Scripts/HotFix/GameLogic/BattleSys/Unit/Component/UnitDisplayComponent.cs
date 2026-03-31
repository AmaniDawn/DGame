/*
 * Hierarchy层次结构
 * - UnitRoot (RenderUnit 根节点，战斗内是 HeroActorRoot 的子节点，UI是UIActorModel 生成的 ActorModelTrans 节点)
 *      - DisplayRoot （代表 UnitDisplayComponent 组件的节点）
 *          - UnitModelRoot（代表 UnitModel 类对象的节点）
 *              - MainModel（主模型）
 *                  - OtherModel（其他部位模型）
 *              - OtherModel（其他部位模型）
 *          - DebugTextInfo（HP）
 */

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DGame;
using Fantasy.Entitas;
using GameBattle;
using GameProto;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace GameLogic
{
    /// <summary>
    /// 单位显示组件。
    /// 负责创建显示根节点 DisplayRoot，并持有模型容器、挂点缓存与显示排序控制。
    /// </summary>
    public sealed class UnitDisplayComponent : Entity
    {
        private SortingGroup m_sortingGroup;
        private int m_sortingOrder;
        private int m_sortingLayerId;

        public RenderUnit OwnerUnit { get; private set; }
        
        /// <summary>
        /// 当前显示组件关联的模型容器。
        /// </summary>
        public UnitModel UnitModel { get; private set; }

        /// <summary>
        /// 当前显示组件是否已经完成模型系统初始化。
        /// </summary>
        public bool IsValid => UnitModel != null;

        /// <summary>
        /// 当前主模型配置。
        /// </summary>
        public ModelConfig MainModelCfg => UnitModel?.MainModelCfg;

        /// <summary>
        /// 显示根节点。
        /// 模型最终不直接挂在 UnitRoot，而是先挂到 DisplayRoot 下，便于后续做整体偏移、镜像、排序等处理。
        /// </summary>
        public GameObject DisplayRoot { get; private set; }

        /// <summary>
        /// 当前单位的挂点缓存。
        /// 一般会在主模型加载完成后刷新。
        /// </summary>
        public DummyPointCache UnitDummy { get; private set; } = new DummyPointCache();

        /// <summary>
        /// 显示根节点 Transform。
        /// </summary>
        public Transform DisplayRootTransform => DisplayRoot != null ? DisplayRoot.transform : null;

        /// <summary>
        /// 当前显示根节点上的 SortingGroup。
        /// <remarks>SortingGroup 挂在 DisplayRoot 上，保证模型重建后排序配置仍然持续生效。</remarks>
        /// </summary>
        public SortingGroup SortingGroup
        {
            get
            {
                if (DisplayRoot == null)
                {
                    return null;
                }
                m_sortingGroup = DGame.Utility.UnityUtil.AddMonoBehaviour<SortingGroup>(DisplayRoot);
                return m_sortingGroup;
            }
        }

        /// <summary>
        /// 当前显示层使用的 SortingOrder。
        /// <remarks>即使模型尚未创建，也会先缓存数值，并在模型创建完成后自动重新应用。</remarks>
        /// </summary>
        public int SortingOrder
        {
            get => m_sortingOrder;
            set
            {
                if (m_sortingOrder == value && m_sortingGroup != null)
                {
                    return;
                }

                m_sortingOrder = value;
                ApplySorting();
            }
        }

        /// <summary>
        /// 当前显示层使用的 SortingLayerId。
        /// <remarks>即使模型尚未创建，也会先缓存数值，并在模型创建完成后自动重新应用。</remarks>
        /// </summary>
        public int SortingLayerId
        {
            get => m_sortingLayerId;
            set
            {
                if (m_sortingLayerId == value && m_sortingGroup != null)
                {
                    return;
                }

                m_sortingLayerId = value;
                ApplySorting();
            }
        }

        /// <summary>
        /// 初始化显示组件，并在渲染单位根节点下创建 DisplayRoot。
        /// </summary>
        /// <param name="ct">模型初始化的取消令牌。</param>
        public async UniTaskVoid InitAsync(CancellationToken ct = default)
        {
            try
            {
                OwnerUnit = Parent as RenderUnit;
                if (OwnerUnit == null || OwnerUnit.UnitRootTransform == null)
                {
                    return;
                }

                if (DisplayRoot == null)
                {
                    DisplayRoot = new GameObject(UnitHelper.DisplayRootName);
                }

                var displayTransform = DisplayRoot.transform;
                displayTransform.SetParent(OwnerUnit.UnitRootTransform, false);
                displayTransform.ResetLocalPosScaleRot();
                this.SubscribeRenderScoped<UnitModelCreatedEvent>(OnUnitModelCreated);
                ApplySorting();

                UnitModel ??= new UnitModel(this);
                var isSuccess = await RefreshMainModelAsync(OwnerUnit.GetModelID(), ct);
                if (!isSuccess)
                {
                    Clear();
                }
            }
            catch (OperationCanceledException)
            {
                Clear();
            }
            catch (Exception e)
            {
                DLogger.Error($"UnitDisplayComponent init failed: {e}");
                Clear();
            }
        }

        /// <summary>
        /// 刷新主模型。
        /// 这是显示组件对外暴露的主模型切换入口。
        /// </summary>
        /// <param name="modelId">模型 ID。</param>
        /// <param name="ct">模型刷新时使用的取消令牌。</param>
        /// <returns>刷新成功返回 true。</returns>
        public async UniTask<bool> RefreshMainModelAsync(int modelId, CancellationToken ct = default)
        {
            if (UnitModel == null)
            {
                return false;
            }

            return await UnitModel.RefreshMainModelAsync(modelId, ct);
        }

        /// <summary>
        /// 获取当前主模型对象。
        /// </summary>
        /// <returns>主模型对象；未加载时返回 null。</returns>
        public GameObject GetMainModelGo() => UnitModel?.GetMainModelGo();

        /// <summary>
        /// 获取当前武器模型对象。
        /// </summary>
        /// <returns>武器模型对象；未加载时返回 null。</returns>
        public GameObject GetWeaponModelGo() => UnitModel?.GetWeaponModelGo();

        /// <summary>
        /// 从当前挂点缓存中获取指定挂点。
        /// </summary>
        /// <param name="dummyName">挂点名称。</param>
        /// <returns>挂点 Transform；不存在时返回 null。</returns>
        public Transform GetDummyPoint(string dummyName) => UnitDummy.GetDummyPoint(dummyName);

        /// <summary>
        /// 从当前挂点缓存中获取指定类型的挂点。
        /// </summary>
        /// <param name="dummyType">挂点类型。</param>
        /// <returns>挂点 Transform；不存在时返回 null。</returns>
        public Transform GetDummyPoint(DummyPointType dummyType) => UnitDummy.GetDummyPoint(dummyType);

        /// <summary>
        /// 尝试按挂点名称获取挂点。
        /// </summary>
        /// <param name="dummyName">挂点名称。</param>
        /// <param name="point">输出挂点。</param>
        /// <returns>找到时返回 true。</returns>
        public bool TryGetDummyPoint(string dummyName, out Transform point)
            => UnitDummy.TryGetDummyPoint(dummyName, out point);

        /// <summary>
        /// 尝试按挂点类型获取挂点。
        /// </summary>
        /// <param name="pointType">挂点类型。</param>
        /// <param name="point">输出挂点。</param>
        /// <returns>找到时返回 true。</returns>
        public bool TryGetDummyPoint(DummyPointType pointType, out Transform point)
            => UnitDummy.TryGetDummyPoint(pointType, out point);

        /// <summary>
        /// 设置整个显示层显隐。
        /// </summary>
        /// <param name="active">是否可见。</param>
        public void SetActive(bool active) => DisplayRoot?.SetActive(active);

        /// <summary>
        /// 立即刷新当前显示层的 SortingGroup 配置。
        /// <remarks>适合在模型重建、切换显示层级或外部修改排序参数后调用。</remarks>
        /// </summary>
        public void RefreshSorting()
        {
            ApplySorting();
        }

        /// <summary>
        /// 销毁显示组件持有的运行时显示对象。
        /// </summary>
        public void Destroy()
        {
            Clear();
        }

        /// <summary>
        /// 模型创建完成后的回调。
        /// <remarks>新模型实例挂入显示层后重新应用当前排序配置，保证后创建的表现对象也受同一排序入口控制。</remarks>
        /// </summary>
        /// <param name="eventData">模型创建事件。</param>
        private void OnUnitModelCreated(UnitModelCreatedEvent eventData)
        {
            ApplySorting();
        }
        
        /// <summary>
        /// 将当前缓存的排序配置应用到显示根节点上的 SortingGroup。
        /// </summary>
        private void ApplySorting()
        {
            var sortingGroup = SortingGroup;
            if (sortingGroup == null)
            {
                return;
            }

            sortingGroup.sortingLayerID = m_sortingLayerId;
            sortingGroup.sortingOrder = m_sortingOrder;
        }
        
        /// <summary>
        /// 清理显示组件相关资源。
        /// 包括模型容器、挂点缓存和显示根节点。
        /// </summary>
        public void Clear()
        {
            UnitModel?.Destroy();
            UnitModel = null;
            UnitDummy.Clear();
            m_sortingGroup = null;
            if (DisplayRoot != null)
            {
                Object.Destroy(DisplayRoot);
                DisplayRoot = null;
            }
        }
    }
}