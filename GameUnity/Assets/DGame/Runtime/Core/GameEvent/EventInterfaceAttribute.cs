using System;

namespace DGame
{
    public enum EEventGroup
    {
        /// <summary>
        /// UI交互相关
        /// </summary>
        GroupUI,

        /// <summary>
        /// 逻辑层内部交互相关
        /// </summary>
        GroupLogic,
        
        /// <summary>
        /// Battle层交互相关
        /// </summary>
        GroupBattle,
    }

    [AttributeUsage(AttributeTargets.Interface)]
    public class EventInterfaceAttribute : Attribute
    {
        public EEventGroup EventGroup { get; }

        public EventInterfaceAttribute(EEventGroup group)
        {
            EventGroup = group;
        }
    }
}