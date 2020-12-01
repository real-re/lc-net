using System;
using System.Diagnostics;
using Re.LC;

[Test]
public static class LCParserTest
{
    public static void Run()
    {
        // Parse("Config/Sample.lc");
        Parse("Config/Example.lc");

        Test_LC_Parser(
            @"# Test LC Document
[Section One]
Key = Value
",
            // Result
            new(stackalloc LCSection[] {
            new("Section One", stackalloc LCValue[] {
                new("Key", "Value"),
            }),
        }));

        Test_LC_Parser(
            "[Section One] [Section Two] name = Naruto [Section Three]",
            // Result
            new(stackalloc LCSection[] {
            new("Section One"),
            new("Section Two",
                stackalloc LCValue[] {
                new("name", "Naruto")
            }),
            new("Section Three"),
        }));
    }

    private static void Parse(string path)
    {
        var lc = LCParser.From(path);

        if (lc.IsEmpty)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Load empty file `{path}`");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"\nLoad file `{path}` success\n");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("---- LC File ----");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(lc.ToString());
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }

    private static void Test_LC_Parser(string doc, LCData result)
    {
        LC lc = LCParser.Parse(doc);
        // compare result
        Span<LCSection> source = lc.Sections;
        Span<LCSection> target = result.Sections;
        if (source.Length != target.Length)
            goto End;

        for (int i = 0; i < source.Length; i++)
        {
            LCSection ss = source[i];
            LCSection ts = target[i];
            if (source[i].Count != target[i].Count)
                goto End;

            for (int j = 0; j < ss.Count; j++)
            {
                if (ss[i] != ts[i])
                    goto End;
            }
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("[TEST SUCCEED]");
        return;
    End:
        var frame = new StackTrace(1, true).GetFrame(0); // FIXME: Not working
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Test case failed in Line {frame.GetFileLineNumber()}");
    }
}
