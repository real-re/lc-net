using System;
// Test Program
// TestUtilities.RunAllTests();

// LCParserTest.Run();
using Re.LC;

if (args.Length == 0)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("args[0] is LC string");
    Console.ForegroundColor = ConsoleColor.Gray;
    return;
}

goto DisplayInfo;

Begin:
string input = args[0];
// Console.WriteLine($"Got input -> {input}");
LC lc = LCParser.Parse(input);
goto End;

DisplayInfo:
Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine();
Console.WriteLine("---------------------");
Console.WriteLine("Parse LC...");
Console.WriteLine("---------------------");
Console.WriteLine();
goto Begin;

End:
Console.ForegroundColor = ConsoleColor.DarkGreen;
Console.WriteLine();
Console.WriteLine("---------------------");
Console.WriteLine("Print result...");
Console.WriteLine("---------------------");
Console.WriteLine();
Console.WriteLine(lc.ToString());

Console.ForegroundColor = ConsoleColor.Gray;
