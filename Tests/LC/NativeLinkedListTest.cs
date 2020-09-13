using System;
using System.Diagnostics;
using Re.Collections.Native;
using Re.LC;

[Test]
public unsafe static class NativeLinkedListTest
{
    public static void Run()
    {
        TestLinkedList();
    }

    private static void TestLinkedList()
    {
        const int count = 10;
        var llist = new NativeLinkedList<LCRange>();
        for (int i = 0; i < count; i++)
            llist.AddAfter(new LCRange(i, (i + 7) * 3));

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("-- foreach supported --");
        Console.ForegroundColor = ConsoleColor.Blue;
        foreach (var item in llist)
            Console.WriteLine(item);
        Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Peek -> {llist.Peek()} -> Count: {llist.Count}\n");
        Debug.Assert(llist.Count == count);
        // Pops Nodes
        Console.WriteLine($"Pop  -> {llist.Pop()} -> Count: {llist.Count}");
        Debug.Assert(llist.Count == count - 1);
        Console.WriteLine($"Pop  -> {llist.Pop()} -> Count: {llist.Count}");
        Debug.Assert(llist.Count == count - 2);
        Console.WriteLine($"Pop  -> {llist.Pop()} -> Count: {llist.Count}");
        Debug.Assert(llist.Count == count - 3);
        // Clear All Nodes and free (Count-1)*size memory spaces
        llist.Clear();
        Debug.Assert(llist.IsEmpty);
        Console.WriteLine($"Clear -> Count: {llist.Count}");
        Console.WriteLine();

        TestLinkedList(llist);
    }

    private static void TestLinkedList<T>(NativeLinkedList<T> llist) where T : unmanaged
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("Test: ");
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.Write(nameof(NativeLinkedList<T>));
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write('<');
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write(typeof(T).Name);
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write('>');
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write(" --> ");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(llist.ToString());
    }
}
