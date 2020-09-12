namespace Re.LC.Utilities
{
    public unsafe struct NativeLinkedList<T> where T : unmanaged
    {
        private Node m_Root;
        private Node m_Last;
        private int m_Count;

        public int Count => m_Count;

        public NativeLinkedList(T value)
        {
            m_Last = m_Root = new Node(&value);
            m_Count = 1;
        }

        public void Add(T value)
        {
            if (m_Count > 0)
            {
                var next = new Node(&value);
                m_Last.Next = &next;
                m_Last = next;
            }
            else
            {
                m_Root.Value = &value;
                m_Last = m_Root;
            }
            m_Count++;
        }

        // public void Remove()
        // {
        //    m_Count--;
        // }

        // public void Clear()
        // {
        //    m_Count = 0;
        // }

        public Node GetEnumerator()
        {
            return m_Root;
        }

        public struct Node
        {
            public T* Value;
            public Node* Next;

            public Node(T* value)
            {
                Value = value;
                Next = null;
            }

            public Node(T* value, Node* next)
            {
                Value = value;
                Next = next;
            }
        }
    }
}
