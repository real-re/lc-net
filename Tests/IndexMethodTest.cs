#nullable enable
using System;
using System.Collections.Generic;

[Test]
public static class IndexMethodTest
{
    public static void Run()
    {
        var mip = new MultiIndexParamter();
        var value = mip[7, 5, 10];
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine(value);
    }
}

public class MultiIndexParamter
{
    public string? this[int id, int index, int length]
    {
        get
        {
            if (_Dict.TryGetValue((id, index, length), out var value))
                return value;
            return null;
        }
    }

    private readonly Dictionary<(int id, int index, int length), string> _Dict = new Dictionary<(int id, int index, int length), string>
    {
        [(7, 5, 10)] = "Some String Value"
    };
}

#nullable disable
