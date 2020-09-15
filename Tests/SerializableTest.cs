using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;

[Test]
public unsafe static class SerializableTest
{
    public static void Run()
    {
        char* name = stackalloc[] { 'J', 'A', 'C', 'K' };

        // Person* p = stackalloc Person[1] {
        //     new Person()
        //     {
        //         Name = name,
        //     }
        // };

        // Serialize
        var p = new Person()
        {
            Name = name,
            SubName = "Kenny",
            ID = 70
        };
        var json = JsonSerializer.Serialize(p);
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"Json <-- {json}");

        // Deserialize
        var person = JsonSerializer.Deserialize<Person>(json);
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"Json --> {person} [ Name: {person.SubName} ID: {person.ID} ]");

        Person.Serialize("");
    }
}

[Serializable]
public unsafe struct Person
{
    public char* Name;
    public string SubName { get; set; }
    public int ID { get; set; }

    private Person(SerializationInfo serializationInfo, StreamingContext streamingContext)
    {
        fixed (char* pName = serializationInfo.GetString(nameof(Name)))
            Name = pName;
        SubName = "Sub Name";
        ID = default;
    }

    public static Person Serialize(string lc)
    {
        var p = new Person();
        var props = p.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        Console.WriteLine(string.Join(", ", props.Select(prop => prop.Name)));
        return p;
    }
}
