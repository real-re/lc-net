using System;

namespace Re.LC.Utilities
{
    internal unsafe static class SpanUtilities
    {
        internal static void Split(this Span<char> span, char sep, SpanLinkedList results)
        {
            if (span.IsEmpty)
                return;

            int len = span.Length;
            int i = 0, start = 0;

            fixed (char* p = &span.GetPinnableReference())
            {
                do
                {
                    if (p[i] == sep)
                    {
                        if (start != i)
                            results.Add(start, i - start);
                        start = ++i;
                        continue;
                    }
                    ++i;
                }
                while (i < len);

                if (start != len)
                    results.Add(start, i - start);
            }
        }
    }
}
