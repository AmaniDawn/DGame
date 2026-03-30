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

using Fantasy.Entitas;
using GameProto;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 单位显示组件。
    /// 负责创建显示根节点 DisplayRoot，并持有模型容器与挂点缓存。
    /// </summary>
    public sealed class UnitDisplayComponent : Entity
    {
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
        /// 初始化显示组件，并在渲染单位根节点下创建 DisplayRoot。
        /// </summary>
        /// <param name="owner">所属渲染单位。</param>
        public void Init(RenderUnit owner)
        {
            if (owner == null || owner.UnitRootTransform == null)
            {
                return;
            }

            if (DisplayRoot == null)
            {
                DisplayRoot = new GameObject("DisplayRoot");
            }

            var displayTransform = DisplayRoot.transform;
            displayTransform.SetParent(owner.UnitRootTransform, false);
            displayTransform.localPosition = Vector3.zero;
            displayTransform.localRotation = Quaternion.identity;
            displayTransform.localScale = Vector3.one;

            UnitModel ??= new UnitModel(this);
            UnitModel.Init(displayTransform);
        }

        /// <summary>
        /// 刷新主模型。
        /// 这是显示组件对外暴露的主模型切换入口。
        /// </summary>
        /// <param name="modelId">模型 ID。</param>
        /// <returns>刷新成功返回 true。</returns>
        public bool RefreshMainModel(int modelId)
        {
            if (UnitModel == null)
            {
                return false;
            }

            return UnitModel.RefreshMainModel(modelId);
        }

        /// <summary>
        /// 获取当前主模型对象。
        /// </summary>
        /// <returns>主模型对象；未加载时返回 null。</returns>
        public GameObject GetMainModelGo()
        {
            return UnitModel?.GetModelGo();
        }

        /// <summary>
        /// 从当前挂点缓存中获取指定挂点。
        /// </summary>
        /// <param name="dummyName">挂点名称。</param>
        /// <returns>挂点 Transform；不存在时返回 null。</returns>
        public Transform GetDummyPoint(string dummyName)
        {
            return UnitDummy.GetDummyPoint(dummyName);
        }

        /// <summary>
        /// 设置整个显示层显隐。
        /// </summary>
        /// <param name="active">是否可见。</param>
        public void SetActive(bool active)
        {
            if (DisplayRoot != null)
            {
                DisplayRoot.SetActive(active);
            }
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
            if (DisplayRoot != null)
            {
                Object.Destroy(DisplayRoot);
                DisplayRoot = null;
            }
        }
    }
}