/* -------------------------------------------------------------------------
 * Hierarchy层次结构
 * - UnitRoot (RenderUnit 根节点，战斗内是 HeroActorRoot 的子节点，UI是UIActorModel 生成的 ActorModelTrans 节点)
 *      - DisplayRoot （代表 UnitDisplayComponent 组件的节点）
 *          - UnitModelRoot（代表 UnitModel 类对象的节点）
 *              - MainModel（主模型）
 *                  - OtherModel（其他部位模型）
 *              - OtherModel（其他部位模型）
 *          - DebugTextInfo（HP）
 * -------------------------------------------------------------------------
 */

using Fantasy.Entitas;
using GameProto;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameLogic
{
    /// <summary>
    /// 单位显示组件。
    /// <remarks>负责创建显示根节点 DisplayRoot，并持有模型容器、挂点缓存与显示排序控制。</remarks>
    /// </summary>
    public sealed class UnitDisplayComponent : Entity
    {
        #region 基础显示状态

        public RenderUnit OwnerUnit { get; set; }
        
        /// <summary>
        /// 当前显示组件关联的模型容器。
        /// </summary>
        public UnitModel UnitModel { get; set; }

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
        public GameObject DisplayRoot { get; set; }

        /// <summary>
        /// 当前单位的挂点缓存。
        /// 一般会在主模型加载完成后刷新。
        /// </summary>
        public DummyPointCache UnitDummy { get; set; } = new DummyPointCache();

        #endregion

        #region 排序与显示访问器

        /// <summary>
        /// 当前显示根节点上的 SortingGroup 缓存。
        /// </summary>
        public SortingGroup CachedSortingGroup { get; set; }

        /// <summary>
        /// 当前显示层使用的 SortingOrder 缓存值。
        /// </summary>
        public int CachedSortingOrder { get; set; }

        /// <summary>
        /// 当前显示层使用的 SortingLayerId 缓存值。
        /// </summary>
        public int CachedSortingLayerId { get; set; }

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
                CachedSortingGroup = DGame.Utility.UnityUtil.AddMonoBehaviour<SortingGroup>(DisplayRoot);
                return CachedSortingGroup;
            }
        }

        /// <summary>
        /// 当前显示层使用的 SortingOrder。
        /// <remarks>即使模型尚未创建，也会先缓存数值，并在模型创建完成后自动重新应用。</remarks>
        /// </summary>
        public int SortingOrder
        {
            get => CachedSortingOrder;
            set
            {
                if (CachedSortingOrder == value && CachedSortingGroup != null)
                {
                    return;
                }

                CachedSortingOrder = value;
                this.ApplySorting();
            }
        }

        /// <summary>
        /// 当前显示层使用的 SortingLayerId。
        /// <remarks>即使模型尚未创建，也会先缓存数值，并在模型创建完成后自动重新应用。</remarks>
        /// </summary>
        public int SortingLayerId
        {
            get => CachedSortingLayerId;
            set
            {
                if (CachedSortingLayerId == value && CachedSortingGroup != null)
                {
                    return;
                }

                CachedSortingLayerId = value;
                this.ApplySorting();
            }
        }

        #endregion
    }
}