using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DGame;
using GameProto;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// UI帧动画播放状态。
    /// </summary>
    public enum UIFrameAnimState
    {
        /// <summary>
        /// 待机状态。
        /// </summary>
        Idle,
        /// <summary>
        /// 移动状态。
        /// </summary>
        Move,
        /// <summary>
        /// 死亡状态。
        /// </summary>
        Death,
        /// <summary>
        /// 技能状态。
        /// </summary>
        Skill,
        /// <summary>
        /// 受击状态。
        /// </summary>
        Hurt,
        /// <summary>
        /// 状态数量上限。
        /// </summary>
        Max
    }

    /// <summary>
    /// UI帧动画播放代理。
    /// </summary>
    public sealed class UIFrameAnimatorAgent : MemoryObject
    {
        #region 字段

        /// <summary>
        /// 基础帧间隔，默认一秒八帧。
        /// </summary>
        private const float FRAME_INTERVAL = 0.125f; // 1秒8帧

        /// <summary>
        /// 普通模型基础播放速度。
        /// </summary>
        private const float NORMAL_BASE_SPEED = 1.5f; // 1秒12帧

        /// <summary>
        /// 精英模型基础播放速度。
        /// </summary>
        private const float ELITE_BASE_SPEED = 1.5f; // 1秒12帧

        /// <summary>
        /// 当前使用的帧动画资源池。
        /// </summary>
        private FrameSpritePool m_frameSpritePool;

        /// <summary>
        /// UI显示用的图片组件。
        /// </summary>
        private Image m_image;

        /// <summary>
        /// 是否已经完成资源初始化。
        /// </summary>
        private bool m_isInit;

        /// <summary>
        /// 当前播放的UI帧动画状态。
        /// </summary>
        private UIFrameAnimState m_curFrameAnimName = UIFrameAnimState.Idle;

        /// <summary>
        /// 初始化完成前缓存的目标UI帧动画状态。
        /// </summary>
        private UIFrameAnimState m_changeFrameAnimName = UIFrameAnimState.Idle;
        // private UIFrameAnimState m_deathFrameAnimName = UIFrameAnimState.Death;

        /// <summary>
        /// 各状态对应的帧动画片段缓存。
        /// </summary>
        private FrameClip[] m_animClips = new FrameClip[(int)UIFrameAnimState.Max];

        /// <summary>
        /// 当前帧动画配置资源地址。
        /// </summary>
        private string m_curCfgLocation;

        /// <summary>
        /// 是否已经绑定显示用的图片组件。
        /// </summary>
        private bool m_isBindDisplayImage;

        /// <summary>
        /// 死亡动画播放速度。
        /// </summary>
        private float m_deathSpeed = 1.0f;

        /// <summary>
        /// UI模型显示缩放。
        /// </summary>
        private Vector3 m_uiModelScale;

        /// <summary>
        /// 是否已经设置首帧。
        /// </summary>
        private bool m_isSetFirstFrame;

        /// <summary>
        /// 是否使用非缩放时间驱动。
        /// </summary>
        private bool m_isUnscaledTime;

        /// <summary>
        /// 上一次推进动画帧时的时间戳。
        /// </summary>
        private float m_preFrameTime;

        /// <summary>
        /// 动画速度缩放系数。
        /// </summary>
        private float m_speedScale = 1.0f;

        /// <summary>
        /// 当前基础播放速度。
        /// </summary>
        private float m_curBaseSpeed;

        /// <summary>
        /// 是否已经释放或销毁。
        /// </summary>
        private bool m_isDestroy;

        /// <summary>
        /// 是否已经请求开始播放。
        /// </summary>
        private bool m_isStarted;

        /// <summary>
        /// 当前代理在调度列表中的索引；-1表示未注册。
        /// </summary>
        internal int UpdateIndex { get; set; } = -1;

        /// <summary>
        /// 当前代理是否由非缩放时间调度。
        /// </summary>
        internal bool IsUnscaledTime => m_isUnscaledTime;

        /// <summary>
        /// 是否有效（未销毁、已初始化、已绑定Image）
        /// </summary>
        public bool IsValid => !m_isDestroy && m_isInit && m_image != null;

        private int m_initVersion;
        private CancellationTokenSource m_initCts;

        #endregion

        /// <summary>
        /// 创建帧动画代理实例
        /// </summary>
        public static UIFrameAnimatorAgent Create()
        {
            var agent = MemoryObject.Spawn<UIFrameAnimatorAgent>();
            agent.m_isDestroy = false;
            return agent;
        }

        /// <summary>
        /// 初始化帧动画代理，异步加载帧动画资源
        /// </summary>
        /// <param name="modelConfig">模型配置</param>
        public async UniTask Init(ModelConfig modelConfig)
        {
            ResetForReinit();
            m_initCts?.Cancel();
            m_initCts?.Dispose();
            m_initCts = new CancellationTokenSource();
            int version = ++m_initVersion;
            var ct = m_initCts.Token;

            if (modelConfig == null || string.IsNullOrEmpty(modelConfig.FrameCfgLocation))
            {
                DLogger.Error($"请检查模型配置表，模型配置表为空");
                return;
            }
            m_curCfgLocation = modelConfig.FrameCfgLocation;
            try
            {
                var frameSpritePool = await FrameSpriteMgr.Instance.GetFrameSpritePool(m_curCfgLocation, ct);
                if (ct.IsCancellationRequested || version != m_initVersion || m_isDestroy)
                {
                    return;
                }

                if (frameSpritePool == null)
                {
                    DLogger.Error($"没有找到帧动画配置文件: {m_curCfgLocation}");
                    return;
                }

                m_frameSpritePool = frameSpritePool;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            m_uiModelScale = modelConfig.UIScale > 0 ? new Vector3(modelConfig.UIScale, modelConfig.UIScale, modelConfig.UIScale) : Vector3.one;
            m_deathSpeed = modelConfig.DeathFrameSpeed > 0 ? modelConfig.DeathFrameSpeed : 1;
            m_curBaseSpeed = NORMAL_BASE_SPEED;
            m_isBindDisplayImage = false;

            if (m_isDestroy)
            {
                return;
            }

            m_animClips[(int)UIFrameAnimState.Idle] = new FrameClip(FrameAnimName.idle,
                m_frameSpritePool.GetSprites(FrameAnimName.idle), IsLoopAnim(UIFrameAnimState.Idle));
            m_animClips[(int)UIFrameAnimState.Move] = new FrameClip(FrameAnimName.run,
                m_frameSpritePool.GetSprites(FrameAnimName.run), IsLoopAnim(UIFrameAnimState.Move));
            m_animClips[(int)UIFrameAnimState.Death] = new FrameClip(FrameAnimName.death,
                m_frameSpritePool.GetSprites(FrameAnimName.death), IsLoopAnim(UIFrameAnimState.Death));
            m_animClips[(int)UIFrameAnimState.Skill] = new FrameClip(FrameAnimName.skill,
                m_frameSpritePool.GetSprites(FrameAnimName.skill), IsLoopAnim(UIFrameAnimState.Skill));
            m_animClips[(int)UIFrameAnimState.Hurt] = new FrameClip(FrameAnimName.hurt,
                m_frameSpritePool.GetSprites(FrameAnimName.hurt), IsLoopAnim(UIFrameAnimState.Hurt));
            m_isInit = true;
            SetFirstFrame();
        }

        /// <summary>
        /// 重复初始化前退出调度，清理旧片段和显示状态；保留异步加载的取消与版本校验。
        /// </summary>
        private void ResetForReinit()
        {
            if (FrameSpriteMgr.IsValid)
            {
                FrameSpriteMgr.Instance.UnregisterAnimator(this);
            }

            m_isInit = false;
            m_isStarted = false;
            m_isSetFirstFrame = false;
            m_isBindDisplayImage = false;
            m_frameSpritePool = null;
            if (m_image != null)
            {
                m_image.sprite = null;
            }
            m_curFrameAnimName = UIFrameAnimState.Idle;
            m_changeFrameAnimName = UIFrameAnimState.Idle;

            for (int i = 0; i < m_animClips.Length; i++)
            {
                m_animClips[i]?.OnDestroy();
                m_animClips[i] = null;
            }
        }

        /// <summary>
        /// 设置是否使用不受时间缩放影响的时间
        /// </summary>
        /// <param name="isUnscaledTime">true=使用UnscaledTime，false=使用普通Time</param>
        public void SetUnscaledTime(bool isUnscaledTime)
        {
            if (m_isUnscaledTime == isUnscaledTime)
            {
                return;
            }

            bool isRegistered = UpdateIndex >= 0;
            if (isRegistered && FrameSpriteMgr.IsValid)
            {
                FrameSpriteMgr.Instance.UnregisterAnimator(this);
            }

            m_isUnscaledTime = isUnscaledTime;

            if (m_isStarted && IsValid)
            {
                m_preFrameTime = isUnscaledTime ? GameTime.UnscaledTime : GameTime.Time;
            }

            if (isRegistered && FrameSpriteMgr.IsValid)
            {
                FrameSpriteMgr.Instance.RegisterAnimator(this);
            }
        }

        /// <summary>
        /// 绑定显示用的Image组件
        /// </summary>
        /// <param name="image">Image组件</param>
        public void BindDisplayRender(Image image)
        {
            if (m_isBindDisplayImage)
            {
                return;
            }
            m_isBindDisplayImage = true;
            m_image = image;
            SetFirstFrame();
        }

        /// <summary>
        /// 在初始化和绑定图片组件都满足后设置首帧图片。
        /// </summary>
        private void SetFirstFrame()
        {
            if (!m_isInit)
            {
                if (m_image != null)
                {
                    m_image.sprite = null;
                }
                return;
            }

            if (m_isSetFirstFrame || m_image == null)
            {
                return;
            }

            
            m_curFrameAnimName = m_changeFrameAnimName;
            var curClip = m_animClips[(int)m_curFrameAnimName];

            if (curClip == null)
            {
                DLogger.Warning($"没找到动画Clip: {m_curFrameAnimName}");
                return;
            }
            SetImageSize();
            SetSprite(curClip.GetNext());
            m_preFrameTime = m_isUnscaledTime ? GameTime.UnscaledTime : GameTime.Time;
            m_isSetFirstFrame = true;
        }

        /// <summary>
        /// 设置当前帧图片到UI图片组件。
        /// </summary>
        /// <param name="sprite">待显示的帧图片。</param>
        private void SetSprite(Sprite sprite)
        {
            if (m_isDestroy || !m_isInit || m_image == null || sprite == null)
            {
                return;
            }

            m_image.sprite = sprite;
        }

        /// <summary>
        /// 开始播放帧动画
        /// </summary>
        public void StartAnim()
        {
            if (!IsValid)
            {
                return;
            }

            m_isStarted = true;
            FrameSpriteMgr.Instance.RegisterAnimator(this);
            m_preFrameTime = m_isUnscaledTime ? GameTime.UnscaledTime : GameTime.Time;
        }

        /// <summary>
        /// 调度器驱动的UI帧动画更新。
        /// </summary>
        /// <param name="currentTime">当前时间域下的时间戳。</param>
        /// <returns>true表示仍需继续调度；false表示已无效或播放结束。</returns>
        internal bool Tick(float currentTime)
        {
            if (!IsValid)
            {
                return false;
            }

            var curClip = m_animClips[(int)m_curFrameAnimName];

            if (curClip == null)
            {
                return false;
            }

            if (curClip.IsStop())
            {
                return false;
            }

            var deltaTime = currentTime - m_preFrameTime;

            if (deltaTime * GetSpeed() > FRAME_INTERVAL)
            {
                SetSprite(curClip.GetNext());
                SetImageSize();
                m_preFrameTime = currentTime;
            }

            return true;
        }

        /// <summary>
        /// 设置或还原图片组件所在节点的UI模型缩放。
        /// </summary>
        /// <param name="revert">是否还原为默认缩放。</param>
        private void SetImageSize(bool revert = false)
        {
            if (m_image == null)
            {
                return;
            }

            m_image.transform.localScale = revert ? Vector3.one : m_uiModelScale;
        }

        /// <summary>
        /// 获取当前动画播放速度
        /// </summary>
        /// <returns>当前动画播放速度。</returns>
        public float GetSpeed()
        {
            if (m_curFrameAnimName == UIFrameAnimState.Move)
            {
                return m_curBaseSpeed;
            }

            if (m_curFrameAnimName == UIFrameAnimState.Death)
            {
                return m_deathSpeed;
            }

            return m_speedScale * m_curBaseSpeed;
        }

        /// <summary>
        /// 切换动画状态
        /// </summary>
        /// <param name="animName">目标动画状态</param>
        public void SwitchAnim(UIFrameAnimState animName)
        {
            if (!IsValid)
            {
                m_changeFrameAnimName = animName;
                return;
            }

            var oldAnimName = m_curFrameAnimName;
            if (animName != oldAnimName)
            {
                m_curFrameAnimName = animName;
                var oldClip = m_animClips[(int)oldAnimName];
                oldClip?.Leave();

                if (m_isStarted)
                {
                    FrameSpriteMgr.Instance.RegisterAnimator(this);
                }
            }
        }
        
        /// <summary>
        /// 重播动画
        /// </summary>
        /// <param name="animName"></param>
        public void ReplayAnim(UIFrameAnimState animName)
        {
            if (!IsValid)
            {
                m_changeFrameAnimName = animName;
                return;
            }
            
            if (animName != m_curFrameAnimName)
            {
                var oldClip = m_animClips[(int)m_curFrameAnimName];
                oldClip?.Leave();
                m_curFrameAnimName = animName;
            }
            
            var clip = m_animClips[(int)m_curFrameAnimName];
            if (clip == null)
            {
                return;
            }

            clip.Leave();
            SetSprite(clip.GetNext());
            SetImageSize();
            m_preFrameTime = m_isUnscaledTime ? GameTime.UnscaledTime : GameTime.Time;

            if (m_isStarted)
            {
                FrameSpriteMgr.Instance.RegisterAnimator(this);
            }
        }
        
        /// <summary>
        /// 判断指定动画是否循环播放
        /// </summary>
        /// <param name="animName">动画状态</param>
        /// <returns>true=循环播放</returns>
        public bool IsLoopAnim(UIFrameAnimState animName)
            => animName == UIFrameAnimState.Idle || animName == UIFrameAnimState.Move;

        /// <summary>
        /// 设置动画播放速度缩放
        /// </summary>
        /// <param name="speed">速度缩放倍数</param>
        public void SetAnimSpeed(float speed)
        {
            m_speedScale = speed;
        }

        #region 释放资源

        /// <summary>
        /// 主动释放
        /// </summary>
        public void Release()
        {
            MemoryObject.Release(this);
        }

        /// <summary>
        /// 释放资源回调
        /// </summary>
        public override void OnRelease()
        {
            m_initVersion++;
            m_initCts?.Cancel();
            m_initCts?.Dispose();
            m_initCts = null;
            if (FrameSpriteMgr.IsValid)
            {
                FrameSpriteMgr.Instance.UnregisterAnimator(this);
            }
            m_isInit = false;
            m_isDestroy = true;
            m_isStarted = false;
            m_frameSpritePool = null;
            SetImageSize(true);
            if (m_image != null)
            {
                m_image.sprite = null;
            }
            m_image = null;
            m_curFrameAnimName = UIFrameAnimState.Idle;
            m_changeFrameAnimName = UIFrameAnimState.Idle;
            // m_deathFrameAnimName = UIFrameAnimState.Death;
            m_curCfgLocation = string.Empty;
            m_isBindDisplayImage = false;
            m_deathSpeed = 1.0f;
            m_uiModelScale = Vector3.one;
            m_isSetFirstFrame = false;
            m_isUnscaledTime = false;
            m_preFrameTime = 0;
            m_speedScale = 1.0f;

            for (int i = 0; i < m_animClips.Length; i++)
            {
                m_animClips[i]?.OnDestroy();
                m_animClips[i] = null;
            }
        }

        #endregion
    }
}
