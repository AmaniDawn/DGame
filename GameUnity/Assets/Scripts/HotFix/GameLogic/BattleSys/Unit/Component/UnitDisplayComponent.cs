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
    public sealed class UnitDisplayComponent : Entity
    {
        public UnitModel UnitModel { get; private set; }

        public bool IsValid => UnitModel != null;

        public ModelConfig MainModelCfg => UnitModel?.MainModelCfg;

        /// <summary>
        /// 模型生成到Hierarchy上时，挂载的节点
        /// </summary>
        public GameObject DisplayRoot { get; private set; }

        public DummyPointCache UnitDummy { get; private set; } = new DummyPointCache();

    }
}