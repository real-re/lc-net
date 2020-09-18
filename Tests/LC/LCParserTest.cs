using System;
using Re.LC;

[Test]
public static class LCParserTest
{
    public static void Run()
    {
        Parse("Config/Sample.lc");
        Parse("Config/Example.lc");
    }

    private static void Parse(string path)
    {
        var lc = LCParser.From(path);

        if (!lc.HasValue)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Load file `{path}` error");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"Load file `{path}` success");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("---- LC File ----");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(lc.Value.ToString());
        }
    }
}
