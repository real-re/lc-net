using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Re.Collections.Native
{
    public unsafe struct NativeLinkedList<T> : IDisposable where T : unmanaged
    {
        public int Count => m_Count;
        public bool IsEmpty => m_Count == 0;
        public Node? First => m_Head == null ? null : *m_Head;
        public Node? Last => m_Tail == null ? null : *m_Tail;

        private Node* m_Head;
        private Node* m_Tail;
        private int m_Count;
        private bool m_Disposed;

        public NativeLinkedList(T value)
        {
            Node* ptr = (Node*)Marshal.AllocHGlobal(sizeof(Node));
            ptr->Value = value;

            m_Tail = m_Head = ptr;
            m_Count = 1;
            m_Disposed = false;
        }

        public void AddAfter(T value)
        {
            if (m_Count > 0)
            {
                Node* nextPtr = (Node*)Marshal.AllocHGlobal(sizeof(Node));
                nextPtr->Value = value;

                m_Tail->Next = nextPtr;
                m_Tail = nextPtr;
            }
            else if (m_Head == null)
            {
                Node* nextPtr = (Node*)Marshal.AllocHGlobal(sizeof(Node));
                nextPtr->Value = value;

                m_Head = nextPtr;
                m_Tail = nextPtr;
            }
            else // 减少一次内存分配
            {
                m_Head->Value = value;
                m_Tail = m_Head;
            }

            m_Count++;
        }

        public void AddBefore(T value)
        {
            Node* newHead = (Node*)Marshal.AllocHGlobal(sizeof(Node));
            newHead->Value = value;

            if (m_Count > 0 && m_Head != null)
                newHead->Next = m_Head;

            m_Head = newHead;
            m_Count++;
        }

        public T Peek()
        {
            if (m_Tail == null)
                return default;

            return m_Tail->Value;
        }

        public T Pop()
        {
            if (m_Count == 0)
                return default;

            Node* tail = m_Tail;
            Node* node = m_Head;
            var value = m_Tail->Value;

            if (node->Next != null)
            {
                // Remove and return the last node
                for (int i = 0; i < m_Count - 2; i++)
                    node = node->Next;

                // Set the penultimate node to tail node
                m_Tail = node;
                m_Tail->Next = null;
                // Console.WriteLine($"Free -> {tail->Value}");
                Marshal.FreeHGlobal((IntPtr)tail);
            }
            else
            {
                m_Head->Value = default;
                // Console.WriteLine($"Free -> {tail->Value}");
                // 不会清除头部节点，用于下次分配
                // Marshal.FreeHGlobal(tail);
                // TODO: 缓存空节点的内存 Clear() -> 保留内置最低节点长度的内存
            }

            m_Count--;
            return value;
        }

        public void Clear()
        {
            if (m_Count != 0 || m_Head != null)
            {
                var next = m_Head->Next;

                for (int i = 0; i < m_Count - 1; i++)
                {
                    var node = next;
                    next = next->Next;
                    // Console.WriteLine($"Free -> {node->Value}");
                    Marshal.FreeHGlobal((IntPtr)node);
                }

                m_Head->Value = default;
                m_Head->Next = null;
                m_Tail = m_Head;
                m_Count = 0;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(m_Head, m_Count);
        }

        public override string ToString()
        {
            if (m_Count == 0)
                return "null";

            var builder = new StringBuilder();
            var sep = stackalloc char[] { ',', ' ' };
            var p = m_Head;

            do
            {
                builder.Append(p->Value.ToString())
                       .Append(sep, 2);
                p = p->Next;
            }
            while (p != null);

            if (builder.Length == 0)
                return "null";

            builder.AppendLine();
            return builder.ToString();
        }

        public void Dispose()
        {
            if (m_Disposed) return;

            if (!m_Disposed)
                Clear();
            m_Disposed = true;
        }

        public struct Node
        {
            public T Value;
            public Node* Next;

            public Node(T value)
            {
                Value = value;
                Next = null;
            }

            public Node(T value, Node* next)
            {
                Value = value;
                Next = next;
            }
        }

        public ref struct Enumerator
        {
            public ref T Current => ref m_Current->Value;

            private int m_Count;
            private Node* m_Current;
            private Node* m_Head;

            internal Enumerator(Node* head, int count)
            {
                m_Count = count;
                m_Current = null;
                m_Head = head;
            }

            public bool MoveNext()
            {
                if (m_Count == 0)
                    return false;

                if (m_Current == null)
                {
                    m_Count--;
                    m_Current = m_Head;
                    return m_Current != null;
                }

                m_Count--;
                m_Current = m_Current->Next;
                return m_Current != null;
            }
        }
    }
}
