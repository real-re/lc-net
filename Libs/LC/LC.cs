using System;

namespace Re.LC
{
    /// <summary>
    /// Low-level configuration language
    /// </summary>
    public struct LC
    {
        public string Name => m_Name;
        public string Path => m_Path;
        // public Span<char> Lines { get; private set; }

        private string m_Name;
        private string m_Path;

        public void Save()
        {
            Save(m_Path);
        }

        public void Save(string path)
        {
        }
    }
}
