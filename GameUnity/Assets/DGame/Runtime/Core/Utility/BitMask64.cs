using System;

namespace DGame
{
    public static partial class Utility
    {
        internal struct BitMask64
        {
            private long m_mask;

            public static implicit operator long(BitMask64 mask) => mask.m_mask;

            public static implicit operator BitMask64(long mask) => new BitMask64(mask);

            public BitMask64(long mask) => this.m_mask = mask;

            /// <summary>
            /// 打开位
            /// </summary>
            public void Open(int bit)
            {
                if (bit < 0 || bit > 63)
                {
                    throw new ArgumentOutOfRangeException();
                }
                m_mask |= 1L << bit;
            }

            /// <summary>
            /// 关闭位
            /// </summary>
            public void Close(int bit)
            {
                if (bit < 0 || bit > 63)
                {
                    throw new ArgumentOutOfRangeException();
                }

                m_mask &= ~(1L << bit);
            }

            /// <summary>
            /// 位取反
            /// </summary>
            public void Reverse(int bit)
            {
                if (bit < 0 || bit > 63)
                {
                    throw new ArgumentOutOfRangeException();
                }
                m_mask ^= 1L << bit;
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
                if (bit < 0 || bit > 63)
                {
                    throw new ArgumentOutOfRangeException();
                }
                return (m_mask & (1L << bit)) != 0;
            }
        }
    }
}