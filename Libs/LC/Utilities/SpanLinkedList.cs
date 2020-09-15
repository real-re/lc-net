using System;
using System.Runtime.InteropServices;

namespace Re.LC.Utilities
{
    public unsafe struct SpanLinkedList : IDisposable
    {
        public int Count => m_Count;
        public bool IsEmpty => m_Source == null;

        private char* m_Source;
        private Range* m_Ranges;
        private int m_Count;
        private int m_Length;
        private bool m_Disposed;

        public SpanLinkedList(Span<char> source)
        {
            if (source.IsEmpty)
            {
                m_Source = null;
                m_Ranges = null;
            }
            else
            {
                fixed (char* p = &source.GetPinnableReference())
                    m_Source = p;
                m_Ranges = null;
            }

            m_Count = 0;
            m_Length = source.Length;
            m_Disposed = false;
        }

        public void Add(int start, int length)
        {
            if (length > m_Length)
                return;

            m_Count++;
        }

        public void Dispose()
        {
            if (m_Disposed) return;

            if (m_Ranges == null)
                return;

            for (int i = 0; i < m_Count; i++)
            {
            }
            m_Disposed = true;
        }

        public struct Range
        {
            public int Start;
            public int Length;
            public Range Next;
        }
    }
}
