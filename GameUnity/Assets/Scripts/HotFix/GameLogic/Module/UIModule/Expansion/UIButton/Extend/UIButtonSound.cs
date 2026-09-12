using GameProto;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameLogic
{
    /// <summary>
    /// 独立的 UI 点击音效组件，适用于不继承 <see cref="BaseUIButton"/> 的 UI 节点。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIButtonSound : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private int m_clickSoundId = (int)SysSoundID.BTN_CLICK;

        /// <summary>
        /// 当前点击音效 ID。默认值为 1000002（<see cref="SysSoundID.BTN_CLICK"/>）。
        /// </summary>
        public int ClickSoundId
        {
            get => m_clickSoundId;
            set => m_clickSoundId = value;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            SoundConfigMgr.Instance.Play(m_clickSoundId, DGame.AudioType.UISound, isInPool: true);
        }
    }
}
