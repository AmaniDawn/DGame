using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DGame;
using Fantasy.Entitas.Interface;
using GameBattle;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 单位显示组件销毁系统。
    /// </summary>
    public sealed class UnitDisplayComponentDestroySystem : DestroySystem<UnitDisplayComponent>
    {
        /// <summary>
        /// 销毁单位显示组件。
        /// </summary>
        /// <param name="self">单位显示组件实例。</param>
        protected override void Destroy(UnitDisplayComponent self)
        {
            self?.Destroy();
        }
    }
    
    public static class UnitDisplayComponentSystem
    {
        #region 生命周期与初始化

        /// <summary>
        /// 初始化显示组件，并在渲染单位根节点下创建 DisplayRoot。
        /// </summary>
        /// <param name="self">单位显示组件。</param>
        /// <param name="ct">模型初始化的取消令牌。</param>
        public static async UniTaskVoid InitAsync(this UnitDisplayComponent self, CancellationToken ct = default)
        {
            try
            {
                self.OwnerUnit = self.Parent as RenderUnit;
                if (self.OwnerUnit == null || self.OwnerUnit.UnitRootTransform == null)
                {
                    return;
                }

                if (self.DisplayRoot == null)
                {
                    self.DisplayRoot = new GameObject(UnitHelper.DisplayRootName);
                }

                var displayTransform = self.DisplayRoot.transform;
                displayTransform.SetParent(self.OwnerUnit.UnitRootTransform, false);
                displayTransform.ResetLocalPosScaleRot();
                self.SubscribeRenderScoped<UnitModelCreatedEvent>(self.OnUnitModelCreated);
                self.ApplySorting();

                self.UnitModel ??= new UnitModel(self);
                var isSuccess = await self.RefreshMainModelAsync(self.OwnerUnit.GetModelID(), ct);
                if (!isSuccess)
                {
                    self.Clear();
                }
            }
            catch (OperationCanceledException)
            {
                self.Clear();
            }
            catch (Exception e)
            {
                DLogger.Error($"UnitDisplayComponent init failed: {e}");
                self.Clear();
            }
        }

        /// <summary>
        /// 销毁显示组件持有的运行时显示对象。
        /// </summary>
        /// <param name="self">单位显示组件。</param>
        public static void Destroy(this UnitDisplayComponent self)
        {
            self.Clear();
        }

        /// <summary>
        /// 清理显示组件相关资源。
        /// <remarks>包括模型容器、挂点缓存和显示根节点。</remarks>
        /// </summary>
        /// <param name="self">单位显示组件。</param>
        public static void Clear(this UnitDisplayComponent self)
        {
            self.UnitModel?.Destroy();
            self.UnitModel = null;
            self.UnitDummy?.Clear();
            self.CachedSortingGroup = null;
            if (self.DisplayRoot != null)
            {
                UnityEngine.Object.Destroy(self.DisplayRoot);
                self.DisplayRoot = null;
            }
        }

        #endregion

        #region 模型与挂点访问

        /// <summary>
        /// 刷新主模型。
        /// 这是显示组件对外暴露的主模型切换入口。
        /// </summary>
        /// <param name="self">单位显示组件。</param>
        /// <param name="modelId">模型 ID。</param>
        /// <param name="ct">模型刷新时使用的取消令牌。</param>
        /// <returns>刷新成功返回 true。</returns>
        public static async UniTask<bool> RefreshMainModelAsync(this UnitDisplayComponent self, int modelId,
            CancellationToken ct = default)
        {
            if (self.UnitModel == null)
            {
                return false;
            }

            return await self.UnitModel.RefreshMainModelAsync(modelId, ct);
        }

        /// <summary>
        /// 刷新主模型。
        /// 这是显示组件对外暴露的主模型切换入口。
        /// </summary>
        /// <param name="self">单位显示组件。</param>
        /// <param name="weaponModelId">武器模型 ID。</param>
        /// <param name="ct">模型刷新时使用的取消令牌。</param>
        /// <returns>刷新成功返回 true。</returns>
        public static async UniTask<bool> RefreshWeaponModelAsync(this UnitDisplayComponent self,
            int weaponModelId, CancellationToken ct = default)
        {
            if (self.UnitModel == null)
            {
                return false;
            }
            return await self.UnitModel.RefreshWeaponModelAsync(weaponModelId, ct);
        }

        /// <summary>
        /// 获取当前主模型对象。
        /// </summary>
        /// <param name="self">单位显示组件。</param>
        /// <returns>主模型对象；未加载时返回 null。</returns>
        public static GameObject GetMainModelGo(this UnitDisplayComponent self) => self?.UnitModel?.GetMainModelGo();

        /// <summary>
        /// 获取当前武器模型对象。
        /// </summary>
        /// <param name="self">单位显示组件。</param>
        /// <returns>武器模型对象；未加载时返回 null。</returns>
        public static GameObject GetWeaponModelGo(this UnitDisplayComponent self) => self?.UnitModel?.GetWeaponModelGo();

        /// <summary>
        /// 从当前挂点缓存中获取指定挂点。
        /// </summary>
        /// <param name="self">单位显示组件。</param>
        /// <param name="dummyName">挂点名称。</param>
        /// <returns>挂点 Transform；不存在时返回 null。</returns>
        public static Transform GetDummyPoint(this UnitDisplayComponent self, string dummyName)
            => self?.UnitDummy?.GetDummyPoint(dummyName);

        /// <summary>
        /// 从当前挂点缓存中获取指定类型的挂点。
        /// </summary>
        /// <param name="self">单位显示组件。</param>
        /// <param name="dummyType">挂点类型。</param>
        /// <returns>挂点 Transform；不存在时返回 null。</returns>
        public static Transform GetDummyPoint(this UnitDisplayComponent self, DummyPointType dummyType)
            => self?.UnitDummy?.GetDummyPoint(dummyType);

        /// <summary>
        /// 尝试按挂点名称获取挂点。
        /// </summary>
        /// <param name="self">单位显示组件。</param>
        /// <param name="dummyName">挂点名称。</param>
        /// <param name="point">输出挂点。</param>
        /// <returns>找到时返回 true。</returns>
        public static bool TryGetDummyPoint(this UnitDisplayComponent self, string dummyName, out Transform point)
            => self.UnitDummy.TryGetDummyPoint(dummyName, out point);

        /// <summary>
        /// 尝试按挂点类型获取挂点。
        /// </summary>
        /// <param name="self">单位显示组件。</param>
        /// <param name="pointType">挂点类型。</param>
        /// <param name="point">输出挂点。</param>
        /// <returns>找到时返回 true。</returns>
        public static bool TryGetDummyPoint(this UnitDisplayComponent self, DummyPointType pointType, out Transform point)
            => self.UnitDummy.TryGetDummyPoint(pointType, out point);

        #endregion

        #region 显示与排序控制

        /// <summary>
        /// 设置整个显示层显隐。
        /// </summary>
        /// <param name="self">单位显示组件。</param>
        /// <param name="active">是否可见。</param>
        public static void SetActive(this UnitDisplayComponent self, bool active) => self.DisplayRoot?.SetActive(active);

        /// <summary>
        /// 立即刷新当前显示层的 SortingGroup 配置。
        /// <remarks>适合在模型重建、切换显示层级或外部修改排序参数后调用。</remarks>
        /// </summary>
        /// <param name="self">单位显示组件。</param>
        public static void RefreshSorting(this UnitDisplayComponent self) => self.ApplySorting();

        /// <summary>
        /// 将当前缓存的排序配置应用到显示根节点上的 SortingGroup。
        /// </summary>
        /// <param name="self">单位显示组件。</param>
        public static void ApplySorting(this UnitDisplayComponent self)
        {
            var sortingGroup = self.SortingGroup;
            if (sortingGroup == null)
            {
                return;
            }

            sortingGroup.sortingLayerID = self.CachedSortingLayerId;
            sortingGroup.sortingOrder = self.CachedSortingOrder;
        }

        #endregion

        #region 内部事件响应

        /// <summary>
        /// 模型创建完成后的回调。
        /// <remarks>新模型实例挂入显示层后重新应用当前排序配置，保证后创建的表现对象也受同一排序入口控制。</remarks>
        /// </summary>
        /// <param name="self">单位显示组件。</param>
        /// <param name="eventData">模型创建事件。</param>
        private static void OnUnitModelCreated(this UnitDisplayComponent self, UnitModelCreatedEvent eventData)
        {
            self.ApplySorting();
        }

        #endregion

        /// <summary>
        /// 注册一个跟随当前渲染单位默认作用域自动释放的渲染单位事件监听。
        /// <para>
        /// <see cref="UnitDisplayComponent"/> 作为 <see cref="RenderUnit"/> 的内部显示组件使用，
        /// 因此事件所属者和默认释放作用域都直接复用当前 <see cref="RenderUnit"/>。
        /// </para>
        /// </summary>
        /// <typeparam name="T">渲染单位事件类型。</typeparam>
        /// <param name="self">渲染单位实例。</param>
        /// <param name="handler">事件回调。</param>
        public static void SubscribeRenderScoped<T>(this UnitDisplayComponent self, Action<T> handler)
            where T : struct, IUnitEvent
            => self?.OwnerUnit?.SubscribeRenderScoped(handler);

        /// <summary>
        /// 注册渲染单位事件监听。
        /// </summary>
        /// <typeparam name="T">渲染单位事件类型。</typeparam>
        /// <param name="self">渲染单位实例。</param>
        /// <param name="owner">监听所属者。</param>
        /// <param name="handler">事件回调。</param>
        public static void SubscribeRender<T>(this UnitDisplayComponent self, object owner, Action<T> handler)
            where T : struct, IUnitEvent
            => self?.OwnerUnit?.SubscribeRender(owner, handler);

        /// <summary>
        /// 取消渲染单位事件监听。
        /// </summary>
        /// <typeparam name="T">渲染单位事件类型。</typeparam>
        /// <param name="self">渲染单位实例。</param>
        /// <param name="handler">事件回调。</param>
        public static void UnsubscribeRender<T>(this UnitDisplayComponent self, Action<T> handler)
            where T : struct, IUnitEvent
            => self?.OwnerUnit?.UnsubscribeRender(handler);

        /// <summary>
        /// 移除指定所属者注册的全部渲染单位事件监听。
        /// </summary>
        /// <param name="self">渲染单位实例。</param>
        /// <param name="owner">监听所属者。</param>
        public static void RemoveAllRenderSubscriptions(this UnitDisplayComponent self, object owner)
            => self?.OwnerUnit?.RemoveAllRenderSubscriptions(owner);

        /// <summary>
        /// 发布渲染单位事件。
        /// </summary>
        /// <typeparam name="T">渲染单位事件类型。</typeparam>
        /// <param name="self">渲染单位实例。</param>
        /// <param name="eventData">事件数据。</param>
        public static void PublishRender<T>(this UnitDisplayComponent self, T eventData)
            where T : struct, IUnitEvent
            => self?.OwnerUnit?.PublishRender(eventData);
    }
}