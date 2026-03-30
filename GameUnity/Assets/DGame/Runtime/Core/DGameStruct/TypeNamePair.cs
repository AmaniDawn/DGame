using System;

namespace DGame
{
    public readonly struct TypeNamePair : IEquatable<TypeNamePair>
    {
        private readonly Type m_type;
        public Type Type => m_type;
        private readonly string m_name;
        public string Name => m_name;

        public TypeNamePair(Type type) : this(type, string.Empty)
        { }

        public TypeNamePair(Type type, string name)
        {
            if (type == null)
            {
                throw new DGameException("类型无效");
            }

            m_type = type;
            m_name = string.IsNullOrEmpty(name) ? string.Empty : name;
        }

        public override string ToString()
        {
            if (m_type == null)
            {
                throw new DGameException("类型无效");
            }

            string typeName = m_type.FullName;
            return string.IsNullOrEmpty(m_name) ? typeName : Utility.StringUtil.Format("{0}.{1}", typeName, m_name);
        }

        public static bool operator ==(TypeNamePair a, TypeNamePair b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(TypeNamePair a, TypeNamePair b)
        {
            return !(a == b);
        }

        public bool Equals(TypeNamePair other)
        {
            return m_type == other.Type && m_name == other.Name;
        }

        public override bool Equals(object obj)
        {
            return obj is TypeNamePair other && Equals(other);
        }

        public override int GetHashCode()
        {
            return m_type.GetHashCode() ^ m_name.GetHashCode();
        }
    }
}