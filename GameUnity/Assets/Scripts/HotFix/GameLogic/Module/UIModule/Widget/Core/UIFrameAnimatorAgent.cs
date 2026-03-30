using Cysharp.Threading.Tasks;
using DGame;
using GameProto;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public enum UIFrameAnimState
    {
        Idle,
        Move,
        Death,
        Skill,
        Hurt,
        Max
    }

    public sealed class UIFrameAnimatorAgent : IMemory
    {
        #region 字段

        private const float FRAME_INTERVAL = 0.125f; // 1秒8帧
        private const float FRAME_TIMER_INTERVAL = FRAME_INTERVAL * 0.25f * 0.5f; // 提高八倍采样率
        private const float NORMAL_BASE_SPEED = 1.5f; // 1秒12帧
        private const float ELITE_BASE_SPEED = 1.5f; // 1秒12帧

        private GameTimer m_gameTimer;
        private FrameSpritePool m_frameSpritePool;
        private Image m_image;
        private bool m_isInit;
        private UIFrameAnimState m_curFrameAnimName = UIFrameAnimState.Idle;
        private UIFrameAnimState m_changeFrameAnimName = UIFrameAnimState.Idle;
        // private UIFrameAnimState m_deathFrameAnimName = UIFrameAnimState.Death;
        private UIFrameClip[] m_animClips = new UIFrameClip[(int)UIFrameAnimState.Max];
        private string m_curCfgLocation;
        private bool m_isBindDisplayImage;
        private float m_deathSpeed = 1.0f;
        private Vector3 m_uiModelScale;
        private bool m_isSetFirstFrame;
        private bool m_isUnscaledTime;
        private float m_preFrameTime;

        private float m_speedScale = 1.0f;
        private float m_curBaseSpeed;
        private bool m_isDestroy;

        /// <summary>
        /// 是否有效（未销毁、已初始化、已绑定Image）
        /// </summary>
        public bool IsValid => !m_isDestroy && m_isInit && m_image != null;

        #endregion

        /// <summary>
        /// 创建帧动画代理实例
        /// </summary>
        public static UIFrameAnimatorAgent Create() => MemoryPool.Spawn<UIFrameAnimatorAgent>();

        /// <summary>
        /// 初始化帧动画代理，异步加载帧动画资源
        /// </summary>
        /// <param name="modelConfig">模型配置</param>
        public async UniTask Init(ModelConfig modelConfig)
        {
            if (modelConfig == null || string.IsNullOrEmpty(modelConfig.FrameCfgLocation))
            {
                DLogger.Error($"请检查模型配置表，模型配置表为空");
                return;
            }
            m_curCfgLocation = modelConfig.FrameCfgLocation;
            m_frameSpritePool = await FrameSpriteMgr.Instance.GetFrameSpritePool(m_curCfgLocation);
            if (m_frameSpritePool == null)
            {
                DLogger.Error($"没有找到帧动画配置文件: {m_curCfgLocation}");
            }

            m_uiModelScale = modelConfig.UIScale > 0 ? new Vector3(modelConfig.UIScale, modelConfig.UIScale, modelConfig.UIScale) : Vector3.one;
            m_deathSpeed = modelConfig.DeathFrameSpeed > 0 ? modelConfig.DeathFrameSpeed : 1;
            m_curBaseSpeed = NORMAL_BASE_SPEED;
            m_isBindDisplayImage = false;

            if (m_isDestroy)
            {
                return;
            }

            m_animClips[(int)UIFrameAnimState.Idle] = new UIFrameClip(UIFrameAnimState.Idle,
                m_frameSpritePool.GetSprites(FrameAnimName.idle), IsLoopAnim(UIFrameAnimState.Idle));
            m_animClips[(int)UIFrameAnimState.Move] = new UIFrameClip(UIFrameAnimState.Move,
                m_frameSpritePool.GetSprites(FrameAnimName.run), IsLoopAnim(UIFrameAnimState.Move));
            m_animClips[(int)UIFrameAnimState.Death] = new UIFrameClip(UIFrameAnimState.Death,
                m_frameSpritePool.GetSprites(FrameAnimName.death), IsLoopAnim(UIFrameAnimState.Death));
            m_animClips[(int)UIFrameAnimState.Skill] = new UIFrameClip(UIFrameAnimState.Skill,
                m_frameSpritePool.GetSprites(FrameAnimName.skill), IsLoopAnim(UIFrameAnimState.Skill));
            m_animClips[(int)UIFrameAnimState.Hurt] = new UIFrameClip(UIFrameAnimState.Hurt,
                m_frameSpritePool.GetSprites(FrameAnimName.hurt), IsLoopAnim(UIFrameAnimState.Hurt));
            m_isInit = true;
            SetFirstFrame();
        }

        /// <summary>
        /// 设置是否使用不受时间缩放影响的时间
        /// </summary>
        /// <param name="isUnscaledTime">true=使用UnscaledTime，false=使用普通Time</param>
        public void SetUnscaledTime(bool isUnscaledTime)
        {
            m_isUnscaledTime = isUnscaledTime;
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

            if (m_isSetFirstFrame)
            {
                return;
            }

            m_isSetFirstFrame = true;
            m_curFrameAnimName = m_changeFrameAnimName;
            var curClip = m_animClips[(int)m_curFrameAnimName];

            if (curClip == null)
            {
                DLogger.Warning($"没找到动画Clip: {m_curFrameAnimName}");
                return;
            }

            SetSprite(curClip.GetNext());
        }

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

            if (m_isUnscaledTime)
            {
                if (GameTimer.IsNull(m_gameTimer))
                {
                    m_gameTimer = GameModule.GameTimerModule.CreateUnscaledLoopGameTimer(FRAME_TIMER_INTERVAL, Update);
                }
            }
            else
            {
                if (GameTimer.IsNull(m_gameTimer))
                {
                    m_gameTimer = GameModule.GameTimerModule.CreateLoopGameTimer(FRAME_TIMER_INTERVAL, Update);
                }
            }
        }

        private void Update(object[] args)
        {
            if (!IsValid)
            {
                return;
            }

            var curClip = m_animClips[(int)m_curFrameAnimName];

            if (curClip == null)
            {
                return;
            }

            if (curClip.IsStop())
            {
                return;
            }

            float gameTime = m_isUnscaledTime ? GameTime.UnscaledTime : GameTime.Time;
            var deltaTime = gameTime - m_preFrameTime;

            if (deltaTime * GetSpeed() > FRAME_INTERVAL)
            {
                SetSprite(curClip.GetNext());
                SetImageSize();
                m_preFrameTime = gameTime;
            }
        }

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
            MemoryPool.Release(this);
        }

        /// <summary>
        /// 释放资源回调
        /// </summary>
        public void OnRelease()
        {
            GameModule.GameTimerModule.DestroyGameTimer(m_gameTimer);
            m_isInit = false;
            m_isDestroy = true;
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