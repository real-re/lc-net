using System;
using Re.LC;

[Test]
public static class LCParserTest
{
    public static void Run()
    {
        // Parse("Config/Sample.lc");
        Parse("Config/Example.lc");
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

    private static void Test_LC_Parser(string doc, string result)
    {
    }
}
