using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Re.LC.Utilities
{
    public unsafe struct SpanLinkedList : IDisposable
    {
        public int Count => _count;
        public bool IsEmptySource => _source is null;
        public Span<char> First => (_source != null && _count != 0)
            ? new Span<char>(_source + _head[0].Start, _head[0].Length)
            : new Span<char>(_source, _length);

        public static SpanLinkedList Empty => new SpanLinkedList();

        private readonly char* _source;
        private readonly int _length;
        private Range* _head;
        private Range* _tail;
        private int _count;
        private bool _disposed;

        public SpanLinkedList(Span<char> source)
        {
            if (source.IsEmpty)
            {
                _source = null;
            }
            else
            {
                fixed (char* p = &source.GetPinnableReference())
                    _source = p;
            }

            _tail = _head = null;
            _count = 0;
            _length = source.Length;
            _disposed = false;
        }

        public void Add(int start, int length)
        {
            if (start + length > _length)
                return;
            if (start > _length || length > (_length - start))
                return;

            var ptr = (Range*)Marshal.AllocHGlobal(sizeof(Range));
            ptr->Start = start;
            ptr->Length = length;

            if (_head is null)
                _head = ptr;
            else
                _tail->Next = ptr;
            _tail = ptr;
            _count++;
        }

        public void Clear()
        {
            if (_count != 0 || _head != null)
            {
                var next = _head;

                for (int i = 0; i < _count; i++)
                {
                    var node = next;
                    next = next->Next;
                    // Console.WriteLine($"Free -> {node->Value}");
                    Marshal.FreeHGlobal((IntPtr)node);
                }

                _head = null;
                _tail = null;
                _count = 0;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_source, _head, _count);
        }

        public void Dispose()
        {
            if (_disposed) return;

            Clear();
            _disposed = true;
        }

        public ref struct Enumerator
        {
            public Span<char> Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => new Span<char>(_source + _current->Start, _current->Length);
            }

            private readonly char* _source;
            private readonly int _count;
            private int _index;
            private Range* _current;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Enumerator(char* source, Range* head, int count)
            {
                _count = count;
                _index = count;
                _current = head;
                _source = source;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if (_index++ < _count)
                    return (_current = _current->Next) != null;

                if (_index == _count)
                    return false;

                _index = 0; // NOTE: 特殊计数法 用于LinkedList
                return _current != null;
            }
        }

        public struct Range
        {
            public int Start;
            public int Length;
            public Range* Next;

            public Range(int start, int length)
            {
                Start = start;
                Length = length;
                Next = null;
            }
        }
    }
}
