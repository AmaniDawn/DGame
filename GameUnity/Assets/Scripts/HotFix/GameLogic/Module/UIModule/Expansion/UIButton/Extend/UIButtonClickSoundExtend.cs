using GameProto;
using UnityEngine;

namespace GameLogic
{
    [System.Serializable]
    public class UIButtonClickSoundExtend
    {
        [SerializeField] private bool m_isUseClickSound = true;
        [SerializeField] private int m_clickSoundID = (int)SysSoundID.BTN_CLICK;

        public void OnPointerClick()
        {

        }

        public void OnPointerDown()
        {
            if (!m_isUseClickSound)
            {
                return;
            }

            if(SoundConfigMgr.Instance.TryGetValue(m_clickSoundID, out var soundCfg))
            {
                GameModule.AudioModule.Play(DGame.AudioType.UISound, soundCfg.Location, isInPool: true);
            }
        }

        public void OnPointerUp()
        {

        }

        public void SetClickSoundID(int soundID)
        {
            m_clickSoundID = soundID;
        }
    }
}