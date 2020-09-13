#nullable enable
using System;
using System.Runtime.InteropServices;
using Re.Collections.Native;

namespace Re.LC
{
    public unsafe struct LCParserSettings
    {
        public LCLineSectionHandler SectionHandler;
        public LCLineKeyValueHandler KeyValueHandler;

        public event Action? OnStart;
    }

    #region LC Line Processor

    public delegate void LCLineSectionHandler(Span<char> value);
    public delegate void LCLineKeyValueHandler(Span<char> key, Span<char> value);

    public interface ILCLineHandler
    {
        int Type { get; }
        void Process(Span<char> value);
    }

    public unsafe struct LCSectionHandler : ILCLineHandler
    {
        public int Type => 0;

        /// <summary>
        /// Process section value
        /// </summary>
        /// <param name="value">All characters in brackets of section</param>
        public void Process(Span<char> value)
        {
            // default return trimed section name;
            value = value.Trim();
        }
    }

    public unsafe struct LCKeyValueHandler : ILCLineHandler
    {
        public int Type => 1;

        public void Process(Span<char> keyValue)
        {
        }

        public void ProcessKeyValue(Span<char> key, Span<char> value)
        {
        }
    }

    #endregion
}
#nullable disable
