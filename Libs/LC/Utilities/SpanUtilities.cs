using System;

namespace Re.LC.Utilities
{
    public unsafe static class SpanUtilities
    {
        public static Span<char> Split(this Span<char> span, char sep)
        {
            if (span.IsEmpty)
                return span;

            int len = span.Length;
            int i = 0, start = 0;

            fixed (char* p = &span.GetPinnableReference())
            {
                while (i < len)
                {
                    if (p[i] == sep)
                    {
                        if (start != i)
                            AddValue(p[start..i]);
                        start = ++i;
                        continue;
                    }
                    ++i;
                }
            }
            return span;
        }
    }
}
