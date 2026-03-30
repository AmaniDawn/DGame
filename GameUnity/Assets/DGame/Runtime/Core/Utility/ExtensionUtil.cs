using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace DGame
{
    public static class ExtensionUtil
    {
        public static void AddCustomEventListener(this UIBehaviour control, EventTriggerType type, UnityAction<BaseEventData> action)
        {
            Utility.UnityUtil.AddCustomEventListener(control, type, action);
        }

        public static void RemoveCustomEventListener(this UIBehaviour control, EventTriggerType type, UnityAction<BaseEventData> action)
        {
            Utility.UnityUtil.RemoveCustomEventListener(control, type, action);
        }
    }
}