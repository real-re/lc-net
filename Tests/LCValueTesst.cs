using System;
using System.Diagnostics;

[Test]
public static class LCValueTesst
{
    public static void Run()
    {
        Span<char> name = stackalloc char[] { 'J', 'a', 'c', 'k' };
        var sec = new LCSection(name);

        Console.WriteLine($"Source Name: {name.ToString()}");
        Console.WriteLine($"Field  Name: {sec.Name}");
        Debug.Assert(name.ToString() == sec.Name);
    }

    public unsafe struct LCSection
    {
        private char* name;

        public string Name => new string(name);

        public LCSection(Span<char> name)
        {
            if (name.IsEmpty)
            {
                this.name = null;
            }
            else
            {
                fixed (char* p = &name.GetPinnableReference())
                    this.name = p;
            }
        }
    }
}
