using System;
using Re.LC;

[Test]
public static class LCParserTest
{
    public static void Run()
    {
        var path = "Config/Itachi.lc";
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
        }
    }
}

