using System;

namespace DGame
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Field)]
    public class DisplayName : Attribute
    {
        private string m_name;
        private string m_tooltip;
        public string displayName => m_name;
        public string tooltip => m_tooltip;

        public DisplayName(string name, string tooltip)
        {
            m_name = name;
            m_tooltip = tooltip;
        }

        public DisplayName(string name)
        {
            m_name = name;
        }
    }
}