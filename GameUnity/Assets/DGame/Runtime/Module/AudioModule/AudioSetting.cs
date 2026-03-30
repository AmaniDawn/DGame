using UnityEngine;

namespace DGame
{
    [CreateAssetMenu(fileName = "AudioSetting", menuName = "DGame/AudioSetting")]
    public class AudioSetting : ScriptableObject
    {
        [Tooltip("确保顺序和AudioType保持一致")]
        public AudioGroupConfig[] audioGroupConfigs = null;
    }
}