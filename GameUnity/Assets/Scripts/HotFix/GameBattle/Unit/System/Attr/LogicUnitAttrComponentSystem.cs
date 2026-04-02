using System;
using System.Collections.Generic;
using DGame;
using Fantasy.Entitas.Interface;

namespace GameBattle
{
    /// <summary>
    /// 逻辑单位属性组件销毁系统。
    /// </summary>
    public sealed class LogicUnitAttrComponentDestroySystem : DestroySystem<LogicUnitAttrComponent>
    {
        protected override void Destroy(LogicUnitAttrComponent self)
        {
            self.Clear();
        }
    }

    /// <summary>
    /// 逻辑单位属性组件扩展方法。
    /// </summary>
    public static class LogicUnitAttrComponentSystem
    {
        /// <summary>
        /// 获取逻辑单位当前攻击力。
        /// </summary>
        public static int GetAtk(this LogicUnit self) => self?.Attr?.FinalAttr.Atk ?? 0;

        /// <summary>
        /// 获取逻辑单位当前生命值。
        /// </summary>
        public static int GetCurrentHp(this LogicUnit self) => self?.Attr?.CurrentHp ?? 0;

        /// <summary>
        /// 获取逻辑单位当前最大生命值。
        /// </summary>
        public static int GetMaxHp(this LogicUnit self) => self?.Attr?.FinalAttr.MaxHp ?? 0;

        /// <summary>
        /// 获取逻辑单位当前移动速度。
        /// </summary>
        public static FixedPoint64 GetMoveSpeed(this LogicUnit self)
        {
            if (self?.Attr == null)
            {
                return FixedPoint64.Zero;
            }

            return self.Attr.FinalAttr.MoveSpeed;
        }

        /// <summary>
        /// 通过逻辑单位入口造成伤害。
        /// </summary>
        public static int TakeDamage(this LogicUnit self, int damage) => self?.Attr?.TakeDamage(damage) ?? 0;

        /// <summary>
        /// 通过逻辑单位入口回复生命值。
        /// </summary>
        public static int Heal(this LogicUnit self, int healValue) => self?.Attr?.Heal(healValue) ?? 0;

        /// <summary>
        /// 使用基础属性初始化属性组件。
        /// </summary>
        public static void InitBaseAttr(this LogicUnitAttrComponent self, LogicUnitAttrData baseAttr, bool resetCurrentHp = true)
        {
            if (self == null)
            {
                return;
            }

            ReleaseModifiers(self.Modifiers);
            self.BaseAttr.CopyFrom(baseAttr);
            self.RuntimeBaseAttr.Clear();
            self.Dirty = true;
            self.RefreshFinalAttr(resetCurrentHp);
        }

        /// <summary>
        /// 刷新最终属性。
        /// </summary>
        public static void RefreshFinalAttr(this LogicUnitAttrComponent self, bool resetCurrentHp = false)
        {
            if (self == null)
            {
                return;
            }

            var baseAtk = new FixedPoint64(self.BaseAttr.Atk + self.RuntimeBaseAttr.Atk);
            var baseMaxHp = new FixedPoint64(self.BaseAttr.MaxHp + self.RuntimeBaseAttr.MaxHp);
            var baseMoveSpeed = self.BaseAttr.MoveSpeed + self.RuntimeBaseAttr.MoveSpeed;

            FixedPoint64 atkFlat = FixedPoint64.Zero;
            FixedPoint64 atkRatio = FixedPoint64.Zero;
            FixedPoint64 hpFlat = FixedPoint64.Zero;
            FixedPoint64 hpRatio = FixedPoint64.Zero;
            FixedPoint64 maxHpFlat = FixedPoint64.Zero;
            FixedPoint64 maxHpRatio = FixedPoint64.Zero;
            FixedPoint64 moveSpeedFlat = FixedPoint64.Zero;
            FixedPoint64 moveSpeedRatio = FixedPoint64.Zero;

            var modifiers = self.Modifiers;
            for (int i = 0; i < modifiers.Count; i++)
            {
                var modifier = modifiers[i];
                if (modifier == null)
                {
                    continue;
                }

                AccumulateModifier(modifier, ref atkFlat, ref atkRatio, ref hpFlat, ref hpRatio, ref maxHpFlat, ref maxHpRatio, ref moveSpeedFlat, ref moveSpeedRatio);
            }

            var oldMaxHp = self.FinalAttr.MaxHp;

            self.FinalAttr.Atk = CalculateIntAttr(baseAtk, atkFlat, atkRatio, 0);
            self.FinalAttr.MaxHp = CalculateIntAttr(baseMaxHp, maxHpFlat, maxHpRatio, 1);
            self.FinalAttr.MoveSpeed = CalculateFixedAttr(baseMoveSpeed, moveSpeedFlat, moveSpeedRatio, FixedPoint64.Zero);

            if (resetCurrentHp || oldMaxHp <= 0)
            {
                self.BaseCurrentHp = self.FinalAttr.MaxHp;
            }
            else if (self.BaseCurrentHp > self.FinalAttr.MaxHp)
            {
                self.BaseCurrentHp = self.FinalAttr.MaxHp;
            }
            else if (self.BaseCurrentHp < 0)
            {
                self.BaseCurrentHp = 0;
            }

            var hpModifierValue = CalculateCurrentHpModifierValue(self.FinalAttr.MaxHp, hpFlat, hpRatio);
            self.CurrentHp = Math.Clamp(self.BaseCurrentHp + hpModifierValue, 0, self.FinalAttr.MaxHp);
            self.Dirty = false;
            self.SyncSnapshot();
        }

        /// <summary>
        /// 添加属性修正项。
        /// </summary>
        public static void AddModifier(this LogicUnitAttrComponent self, LogicUnitAttrModifier modifier, bool refresh = true)
        {
            if (self == null || modifier == null)
            {
                return;
            }

            self.Modifiers.Add(modifier);
            self.Dirty = true;
            if (refresh)
            {
                self.RefreshFinalAttr();
            }
        }

        /// <summary>
        /// 以便捷参数形式创建并添加一条属性修正项。
        /// <remarks>
        /// 该重载会自动从内存池申请 <see cref="LogicUnitAttrModifier"/>，
        /// 并交由属性组件统一持有与释放，调用方无需手动回收。
        /// </remarks>
        /// </summary>
        /// <param name="self">逻辑单位属性组件。</param>
        /// <param name="sourceId">修正来源标识，用于后续按来源移除。</param>
        /// <param name="attrType">目标属性类型。</param>
        /// <param name="modifyMode">修正模式。</param>
        /// <param name="value">修正值。</param>
        /// <param name="refresh">是否在添加后立即刷新最终属性。</param>
        public static void AddModifier(this LogicUnitAttrComponent self, ulong sourceId, UnitAttrType attrType,
            UnitAttrModifyMode modifyMode, FixedPoint64 value, bool refresh = true)
        {
            var modifier = LogicUnitAttrModifier.Create(sourceId, attrType, modifyMode, value);
            self.AddModifier(modifier, refresh);
        }

        /// <summary>
        /// 移除指定来源的全部属性修正项。
        /// </summary>
        public static int RemoveModifiersBySource(this LogicUnitAttrComponent self, ulong sourceId, bool refresh = true)
        {
            if (self == null)
            {
                return 0;
            }

            var removedCount = 0;
            var modifiers = self.Modifiers;
            for (int i = modifiers.Count - 1; i >= 0; i--)
            {
                var modifier = modifiers[i];
                if (modifier == null || modifier.SourceId != sourceId)
                {
                    continue;
                }

                modifiers.RemoveAt(i);
                MemoryObject.Release(modifier);
                removedCount++;
            }

            if (removedCount <= 0)
            {
                return 0;
            }

            self.Dirty = true;
            if (refresh)
            {
                self.RefreshFinalAttr();
            }

            return removedCount;
        }

        /// <summary>
        /// 造成伤害并扣除当前生命值。
        /// </summary>
        public static int TakeDamage(this LogicUnitAttrComponent self, int damage)
        {
            if (self == null || damage <= 0)
            {
                return 0;
            }

            var oldHp = self.CurrentHp;
            var newHp = Math.Clamp(oldHp - damage, 0, self.FinalAttr.MaxHp);

            if (newHp == oldHp)
            {
                return 0;
            }

            var hpModifierValue = oldHp - self.BaseCurrentHp;
            self.BaseCurrentHp = Math.Clamp(newHp - hpModifierValue, 0, self.FinalAttr.MaxHp);
            self.CurrentHp = newHp;
            self.Owner?.MarkHp(newHp);
            return oldHp - newHp;
        }

        /// <summary>
        /// 为当前单位回复生命值。
        /// </summary>
        public static int Heal(this LogicUnitAttrComponent self, int healValue)
        {
            if (self == null || healValue <= 0)
            {
                return 0;
            }

            var oldHp = self.CurrentHp;
            var maxHp = self.FinalAttr.MaxHp;
            var newHp = oldHp + healValue;
            if (newHp > maxHp)
            {
                newHp = maxHp;
            }

            if (newHp == oldHp)
            {
                return 0;
            }

            var hpModifierValue = oldHp - self.BaseCurrentHp;
            self.BaseCurrentHp = Math.Clamp(newHp - hpModifierValue, 0, maxHp);
            self.CurrentHp = newHp;
            self.Owner?.MarkHp(newHp);
            return newHp - oldHp;
        }

        /// <summary>
        /// 获取当前生命值百分比。
        /// </summary>
        public static FixedPoint64 GetHpPercent(this LogicUnitAttrComponent self)
        {
            if (self == null || self.FinalAttr.MaxHp <= 0)
            {
                return FixedPoint64.Zero;
            }

            return new FixedPoint64(self.CurrentHp) / new FixedPoint64(self.FinalAttr.MaxHp);
        }

        /// <summary>
        /// 同步属性快照到状态同步组件。
        /// </summary>
        public static void SyncSnapshot(this LogicUnitAttrComponent self)
        {
            var owner = self?.Owner;
            if (owner == null || owner.StateSync == null)
            {
                return;
            }

            owner.MarkAttrSnapshot(self.FinalAttr.Atk, self.CurrentHp, self.FinalAttr.MaxHp, self.FinalAttr.MoveSpeed);
        }

        /// <summary>
        /// 清空属性组件内部状态。
        /// </summary>
        public static void Clear(this LogicUnitAttrComponent self)
        {
            if (self == null)
            {
                return;
            }

            self.ClearAttrStorages();
            ReleaseModifiers(self.Modifiers);
            self.BaseCurrentHp = 0;
            self.CurrentHp = 0;
            self.Dirty = true;
            self.Owner = null;
            self.ReleaseAttrStorages();
        }

        /// <summary>
        /// 将单条修正项累计到对应属性槽位。
        /// </summary>
        private static void AccumulateModifier(LogicUnitAttrModifier modifier,
            ref FixedPoint64 atkFlat, ref FixedPoint64 atkRatio,
            ref FixedPoint64 hpFlat, ref FixedPoint64 hpRatio,
            ref FixedPoint64 maxHpFlat, ref FixedPoint64 maxHpRatio,
            ref FixedPoint64 moveSpeedFlat, ref FixedPoint64 moveSpeedRatio)
        {
            switch (modifier.AttrType)
            {
                case UnitAttrType.Atk:
                    AddModifierValue(modifier, ref atkFlat, ref atkRatio);
                    break;
                case UnitAttrType.Hp:
                    AddModifierValue(modifier, ref hpFlat, ref hpRatio);
                    break;
                case UnitAttrType.MaxHp:
                    AddModifierValue(modifier, ref maxHpFlat, ref maxHpRatio);
                    break;
                case UnitAttrType.MoveSpeed:
                    AddModifierValue(modifier, ref moveSpeedFlat, ref moveSpeedRatio);
                    break;
            }
        }

        private static void AddModifierValue(LogicUnitAttrModifier modifier, ref FixedPoint64 flat, ref FixedPoint64 ratio)
        {
            if (modifier.ModifyMode == UnitAttrModifyMode.Ratio)
            {
                ratio += modifier.Value;
                return;
            }

            flat += modifier.Value;
        }

        private static int CalculateIntAttr(FixedPoint64 baseValue, FixedPoint64 flatValue, FixedPoint64 ratioValue, int minValue)
        {
            var finalValue = (baseValue + flatValue) * (FixedPoint64.One + ratioValue);
            var intValue = finalValue.AsInt();
            return intValue < minValue ? minValue : intValue;
        }

        private static FixedPoint64 CalculateFixedAttr(FixedPoint64 baseValue, FixedPoint64 flatValue, FixedPoint64 ratioValue, FixedPoint64 minValue)
        {
            var finalValue = (baseValue + flatValue) * (FixedPoint64.One + ratioValue);
            return finalValue < minValue ? minValue : finalValue;
        }

        /// <summary>
        /// 基于当前最大生命值和 Hp 修正累计结果计算生命修正值。
        /// </summary>
        private static int CalculateCurrentHpModifierValue(int maxHp, FixedPoint64 flatValue, FixedPoint64 ratioValue)
        {
            if (maxHp <= 0)
            {
                return 0;
            }

            var value = flatValue + new FixedPoint64(maxHp) * ratioValue;
            return value.AsInt();
        }

        private static void ReleaseModifiers(List<LogicUnitAttrModifier> modifiers)
        {
            for (int i = 0; i < modifiers.Count; i++)
            {
                var modifier = modifiers[i];
                if (modifier == null)
                {
                    continue;
                }

                MemoryObject.Release(modifier);
            }

            modifiers.Clear();
        }
    }
}