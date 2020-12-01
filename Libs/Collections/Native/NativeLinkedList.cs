using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Re.Collections.Native
{
    public unsafe struct NativeLinkedList<T> : IDisposable where T : unmanaged
    {
        public int Count => _count;
        public bool IsEmpty => _count == 0;
        public Node? First => _head == null ? null : *_head;
        public Node? Last => _tail == null ? null : *_tail;

        private Node* _head;
        private Node* _tail;
        private int _count;
        private bool _disposed;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeLinkedList(T value)
        {
            Node* ptr = (Node*)Marshal.AllocHGlobal(sizeof(Node));
            ptr->Value = value;

            _tail = _head = ptr;
            _count = 1;
            _disposed = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddAfter(T value)
        {
            if (_count > 0)
            {
                Node* nextPtr = (Node*)Marshal.AllocHGlobal(sizeof(Node));
                nextPtr->Value = value;

                _tail->Next = nextPtr;
                _tail = nextPtr;
            }
            else if (_head == null)
            {
                Node* nextPtr = (Node*)Marshal.AllocHGlobal(sizeof(Node));
                nextPtr->Value = value;

                _head = nextPtr;
                _tail = nextPtr;
            }
            else // 减少一次内存分配
            {
                _head->Value = value;
                _tail = _head;
            }

            _count++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddBefore(T value)
        {
            Node* newHead = (Node*)Marshal.AllocHGlobal(sizeof(Node));
            newHead->Value = value;

            if (_count > 0 && _head != null)
                newHead->Next = _head;

            _head = newHead;
            _count++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            if (_count > 0 && _head != null && !_disposed)
            {
                Node* next = _head;

                for (int i = 0; i < _count; i++)
                {
                    Node* node = next;
                    next = next->Next;
                    // Console.WriteLine($"Free -> {node->Value}");
                    Marshal.FreeHGlobal((IntPtr)node);
                }

                _head = null;
                _tail = null;
                _count = 0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Peek()
        {
            if (_tail == null)
                return default;

            return _tail->Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Pop()
        {
            if (_count == 0)
                return default;

            Node* node = _head;
            var value = _tail->Value;

            if (node->Next != null)
            {
                Node* tail = _tail;

                // Remove and return the last node
                for (int i = 0; i < _count - 2; i++)
                    node = node->Next;

                // Set the penultimate node to tail node
                _tail = node;
                _tail->Next = null;
                // Console.WriteLine($"Free -> {tail->Value}");
                Marshal.FreeHGlobal((IntPtr)tail);
            }
            else
            {
                _head->Value = default;
                // Console.WriteLine($"Free -> {tail->Value}");
            }

            _count--;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue(int index, T value)
        {
            if (index < 0 && index <= _count)
                return;

            if (_head == null)
                return;

            // TODO: 优化 根据索引与总数比较，选择从头部遍历，还是尾部更快
            // bool fromHead = index > (_count / 2);

            if (index == _count - 1)
            {
                if (_tail != null)
                    _tail->Value = value;
                return;
            }

            Node* node = _head;
            for (int i = 0; i < _count; i++)
            {
                if (i == index)
                    break;
                node = node->Next;
            }
            if (node == null)
                return;
            node->Value = value;
        }

        public Enumerator GetEnumerator() => new Enumerator(_head, _count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> ToSpan()
        {
            if (_count > 0 && _head != null && !_disposed)
            {
                Span<T> span = new T[_count];
                Node* node = _head;
                for (int i = 0; i < _count; i++)
                {
                    span[i] = node->Value;
                    node = node->Next;
                }
                return span;
            }
            else
            {
                return Span<T>.Empty;
            }
        }

        public override string ToString()
        {
            if (_count == 0 || _head == null || _disposed)
                return string.Empty;

            var builder = new StringBuilder();
            var sep = stackalloc char[] { ',', ' ' };
            var p = _head;

            do
            {
                builder.Append(p->Value.ToString())
                       .Append(sep, 2);
                p = p->Next;
            }
            while (p != null);

            if (builder.Length == 0)
                return string.Empty;

            builder.AppendLine();
            return builder.ToString();
        }

        public void Dispose()
        {
            if (_disposed) return;

            Clear();
            _disposed = true;
        }

        public struct Node
        {
            public T Value;
            public Node* Next;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Node(T value)
            {
                Value = value;
                Next = null;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Node(T value, Node* next)
            {
                Value = value;
                Next = next;
            }
        }

        public ref struct Enumerator
        {
            public ref T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref _current->Value;
            }

            private readonly int _count;
            private int _index;
            private Node* _current;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Enumerator(Node* head, int count)
            {
                _count = count;
                _index = count;
                _current = head;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if (++_index < _count)
                    return (_current = _current->Next) != null;

                if (_index == _count)
                    return false;

                _index = 0;
                return _current != null;
            }
        }
    }
}
