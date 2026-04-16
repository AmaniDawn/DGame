using Coffee.UIExtensions;
using DGame;
using UnityEngine;

namespace GameLogic
{
    public class UIParticleWidget : UIWidget
    {
        #region Override

        protected override void BindMemberProperty()
        {
            m_uiParticle = DGame.Utility.UnityUtil.AddMonoBehaviour<UIParticle>(gameObject);

            if (m_uiParticle == null)
            {
                DLogger.Error($"UIParticle为空，请检查: [{gameObject.name}]");
                return;
            }

            InitUIParticle();
        }

        #endregion

        #region 字段

        private UIParticle m_uiParticle;
        private float m_scale = 1f;
        private bool m_maskable = true;
        private bool m_useCustomView = false;
        private UIParticle.AutoScalingMode m_autoScalingMode = UIParticle.AutoScalingMode.UIParticle;
        private UIParticle.PositionMode m_positionMode = UIParticle.PositionMode.Relative;
        private UIParticle.MeshSharing m_meshSharing = UIParticle.MeshSharing.None;
        
        /// <summary>
        /// 是否受 UI Mask 组件影响，开启后超出 Mask 区域会被裁剪
        /// </summary>
        public bool Maskable
        {
            get => m_maskable;
            set
            {
                if (m_maskable != value)
                {
                    m_maskable = value;
                    if (m_uiParticle != null)
                    {
                        m_uiParticle.maskable = m_maskable;
                    }
                }
            }
        }
        
        /// <summary>
        /// 是否使用自定义视图，当粒子因最小/最大尺寸显示不正常时可启用
        /// </summary>
        public bool UseCustomView
        {
            get => m_useCustomView;
            set
            {
                if (m_useCustomView != value)
                {
                    m_useCustomView = value;

                    if (m_uiParticle != null)
                    {
                        m_uiParticle.useCustomView = m_useCustomView;
                    }
                }
            }
        }
        
        /// <summary>
        /// 自动缩放模式，控制当 Canvas 缩放改变时的自动调整方式
        /// </summary>
        public UIParticle.AutoScalingMode AutoScalingMode
        {
            get => m_autoScalingMode;
            set
            {
                if (m_autoScalingMode != value)
                {
                    m_autoScalingMode = value;

                    if (m_uiParticle != null)
                    {
                        m_uiParticle.autoScalingMode = m_autoScalingMode;
                    }
                }
            }
        }
        
        /// <summary>
        /// 发射位置模式，Relative=从缩放后位置发射，Absolute=从世界坐标位置发射
        /// </summary>
        public UIParticle.PositionMode PositionMode
        {
            get => m_positionMode;
            set
            {
                if (m_positionMode != value)
                {
                    m_positionMode = value;

                    if (m_uiParticle != null)
                    {
                        m_uiParticle.positionMode = m_positionMode;
                    }
                }
            }
        }
        
        /// <summary>
        /// 网格共享模式，大量相同特效时启用可优化性能
        /// </summary>
        public UIParticle.MeshSharing MeshSharing
        {
            get => m_meshSharing;
            set
            {
                if (m_meshSharing != value)
                {
                    m_meshSharing = value;

                    if (m_uiParticle != null)
                    {
                        m_uiParticle.meshSharing = m_meshSharing;
                    }
                }
            }
        }

        #endregion

        #region 函数
        
        private void InitUIParticle()
        {
            m_uiParticle.RefreshParticles();
            m_uiParticle.scale = 1f;
            m_uiParticle.maskable = m_maskable;
            m_uiParticle.autoScalingMode = m_autoScalingMode;
            m_uiParticle.positionMode = m_positionMode;
            m_uiParticle.meshSharing = m_meshSharing;
            m_uiParticle.useCustomView = m_useCustomView;
        }

        /// <summary>
        /// 根物体缩放
        /// </summary>
        /// <param name="scale"></param>
        public void ChangeScale(float scale)
        {
            if (scale != this.m_scale && transform != null)
            {
                this.m_scale = scale;
                transform.localScale = Vector3.one * this.m_scale;
            }
        }
        
        /// <summary>
        /// 播放粒子系统
        /// </summary>
        public void Play()
        {
            if (m_uiParticle != null)
            {
                m_uiParticle.Play();
            }
        }

        /// <summary>
        /// 暂停粒子系统
        /// </summary>
        public void Pause()
        {
            if (m_uiParticle != null)
            {
                m_uiParticle.Pause();
            }
        }
        
        /// <summary>
        /// 恢复粒子系统播放
        /// </summary>
        public void Resume()
        {
            if (m_uiParticle != null)
            {
                m_uiParticle.Resume();
            }
        }
        
        /// <summary>
        /// 停止粒子系统
        /// </summary>
        public void Stop()
        {
            if (m_uiParticle != null)
            {
                m_uiParticle.Stop();
            }
        }
        
        /// <summary>
        /// 开始粒子发射
        /// </summary>
        public void StartEmission()
        {
            if (m_uiParticle != null)
            {
                m_uiParticle.StartEmission();
            }
        }
        
        /// <summary>
        /// 停止粒子发射
        /// </summary>
        public void StopEmission()
        {
            if (m_uiParticle != null)
            {
                m_uiParticle.StopEmission();
            }
        }

        /// <summary>
        /// 刷新粒子系统，重新收集 GameObject 下的所有 ParticleSystem
        /// </summary>
        public void RefreshParticles()
        {
            if (m_uiParticle != null)
            {
                m_uiParticle.RefreshParticles();
            }
        }
        
        /// <summary>
        /// 2D模式缩放
        /// </summary>
        /// <param name="scale"></param>
        public void ChangeUIParticleScale(float scale)
        {
            if (m_uiParticle != null)
            {
                m_uiParticle.scale = scale;
            }
        }
        
        /// <summary>
        /// 3D模式缩放
        /// </summary>
        /// <param name="scale"></param>
        public void ChangeUIParticleScale(Vector3 scale)
        {
            if (m_uiParticle != null)
            {
                m_uiParticle.scale3D = scale;
            }
        }

        /// <summary>
        /// 镜像翻转
        /// </summary>
        /// <param name="isMirror"></param>
        public void ChangeMirror(bool isMirror)
        {
            if (transform != null)
            {
                transform.localScale = new Vector3((isMirror ? -1 : 1) * m_scale, m_scale, 1);
            }
        }

        /// <summary>
        /// 修改根物体位置
        /// </summary>
        /// <param name="pos"></param>
        public void ChangeLocalPos(Vector3 pos)
        {
            if (transform != null)
            {
                transform.transform.localPosition = pos;
            }
        }
        
        /// <summary>
        /// 清除所有粒子
        /// </summary>
        public void Clear()
        {
            if (m_uiParticle != null)
            {
                m_uiParticle.Clear();
            }
        }

        #endregion
    }
}