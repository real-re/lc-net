using System;
// Test Program
// TestUtilities.RunAllTests();

// LCParserTest.Run();

//
// Console Program
//

using Re.LC;

ConsoleColor foregroundColor = Console.ForegroundColor;

if (args.Length == 0)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Invalid argument not found LC string");
    Console.ForegroundColor = foregroundColor;
    return;
}

DisplayInfo();

string input = args[0];
// Console.WriteLine($"Got input -> {input}");
LC lc = LCParser.Parse(input);
// Print result
Console.ForegroundColor = ConsoleColor.Blue;
Console.WriteLine();
Console.WriteLine("---------------------");
Console.WriteLine("Print result...");
Console.WriteLine("---------------------");
Console.WriteLine();
Console.WriteLine(lc.ToString());
// Reset console foreground color
Console.ForegroundColor = foregroundColor;

void DisplayInfo()
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine();
    Console.WriteLine("---------------------");
    Console.WriteLine("Parsing LC...");
    Console.WriteLine("---------------------");
    Console.WriteLine();
}
