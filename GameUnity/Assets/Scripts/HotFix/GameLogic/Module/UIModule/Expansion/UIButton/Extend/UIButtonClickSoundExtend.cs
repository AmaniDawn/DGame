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

            SoundConfigMgr.Instance.Play(m_clickSoundID, DGame.AudioType.UISound, isInPool: true);
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
