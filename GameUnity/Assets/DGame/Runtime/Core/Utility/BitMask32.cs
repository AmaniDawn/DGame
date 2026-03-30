using System;

namespace DGame
{
    public static partial class Utility
    {
        public struct BitMask32
        {
            private int m_mask;

            public static implicit operator int(BitMask32 mask) => mask.m_mask;

            public static implicit operator BitMask32(int mask) => new BitMask32(mask);

            public BitMask32(int mask) => m_mask = mask;

            /// <summary>
            /// 打开位
            /// </summary>
            public void Open(int bit)
            {
                if (bit < 0 || bit > 31)
                {
                    throw new ArgumentOutOfRangeException();
                }
                m_mask |= 1 << bit;
            }

            /// <summary>
            /// 关闭位
            /// </summary>
            public void Close(int bit)
            {
                if (bit < 0 || bit > 31)
                {
                    throw new ArgumentOutOfRangeException();
                }
                m_mask &= ~(1 << bit);
            }

            /// <summary>
            /// 位取反
            /// </summary>
            public void Reverse(int bit)
            {
                if (bit < 0 || bit > 31)
                {
                    throw new ArgumentOutOfRangeException();
                }
                m_mask ^= 1 << bit;
            }

            /// <summary>
            /// 所有位取反
            /// </summary>
            public void Inverse() => m_mask = ~m_mask;

            /// <summary>
            /// 比对位值
            /// </summary>
            public bool Test(int bit)
            {
                if (bit < 0 || bit > 31)
                {
                    throw new ArgumentOutOfRangeException();
                }
                return (m_mask & (1 << bit)) != 0;
            }
        }
    }
}