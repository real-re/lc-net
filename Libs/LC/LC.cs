#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace Re.LC
{
    /// <summary>
    /// Low-level configuration language
    /// </summary>
    public ref struct LC
    {
        public string Name
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Data.Name.ToString();
        }

        public string? Path
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Data.Path;
        }

        public Span<LCSection> Sections
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Data.Sections;
        }

        public bool HasTopLevelSection
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Data.HasTopLevelSection;
        }

        public LCData Data
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Data;
        }

        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                bool isEmpty = false;
                isEmpty |= Data.Sections.IsEmpty;
                return isEmpty;
            }
        }

        public static LC Empty => default;

        private LCData m_Data;

        public LC(LCData data) => m_Data = data;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Flush() => SaveTo(Path);

        public bool SaveTo(string? path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            if (IsEmpty)
            {
                //TODO:
            }

            return false;
        }

        public override string? ToString() => m_Data.ToString();
    }
}
#nullable disable
