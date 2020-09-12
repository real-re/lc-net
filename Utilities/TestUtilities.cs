using System;
using System.Reflection;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class TestAttribute : Attribute { }

internal static class TestUtilities
{
    internal static void RunAllTests()
    {
        var attrType = typeof(TestAttribute);
        var types = attrType.Assembly.GetTypes();
        foreach (var t in types)
        {
            if (t.IsDefined(attrType))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("--- " + t.Name + " ---");
                Console.WriteLine();
                t.GetMethod("Run", BindingFlags.Public|BindingFlags.Static)?.Invoke(null, null);
                Console.WriteLine();
            }
        }
    }
}
