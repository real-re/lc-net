using System;
using Collections.Pooled;

[Test]
public unsafe static class DataBuffer
{
    private static readonly PooledDictionary<int, IntPtr> _Cached = new PooledDictionary<int, IntPtr>();
    private static readonly PooledDictionary<int, object> _CachedManaged = new PooledDictionary<int, object>();

    public static void Cache(void* value, string id)
    {
        if (value == null || id == null)
        {
            Console.WriteLine($"Cache pointer type or id:{id} can't be null");
            return;
        }
        _Cached.Add(id.ToHash(), new IntPtr(value));
    }

    public static void Cache<TValue>(TValue value, string id) where TValue : unmanaged
    {
        if (id == null)
        {
            Console.WriteLine($"Cache value type or id:{id} can't be null");
            return;
        }
        _Cached.Add(id.ToHash(), new IntPtr(&value));
        _CachedManaged.Add(id.ToHash(), value);
    }

    public static void Cache(IntPtr ptr, string id)
    {
        if (ptr == IntPtr.Zero || id == null)
        {
            Console.WriteLine($"Cache value: void* or id:{id} can't be null");
            return;
        }
        _Cached.Add(id.ToHash(), ptr);
    }

    public static void Cache(object value, string id)
    {
        if (value == null || id == null)
        {
            Console.WriteLine($"Cache value:{value} or id:{id} can't be null");
            return;
        }
        _CachedManaged.Add(id.ToHash(), value);
    }

    public static T Get<T>(string id, bool autoRemove = true) where T : unmanaged
    {
        if (id == null)
            return default;

        var key = id.ToHash();
        if (autoRemove)
        {
            if (_Cached.Remove(key, out var ptr))
                return *(T*)ptr;
        }
        else
        {
            if (_Cached.TryGetValue(key, out var ptr))
                return *(T*)ptr;
        }
        return default;
    }

    public static T GetObject<T>(string id, bool autoRemove = true) where T : class
    {
        if (id == null)
            return null;

        var key = id.ToHash();
        if (autoRemove)
        {
            if (_CachedManaged.Remove(key, out var data))
                return (T)data;
        }
        else
        {
            if (_CachedManaged.TryGetValue(key, out var data))
            {
                return (T)data;
            }
        }
        return null;
    }

    internal static void Clear()
    {
        _Cached.Clear();
        _CachedManaged.Clear();
    }

    public static void Run()
    {
        Console.ForegroundColor = ConsoleColor.Red;

        // Value Type
        var value = Point();
        Cache(value, "Int");
        Console.WriteLine($"Cache Value: {value} by ID: Integer");

        var result = Get<(int, int)>("Int");
        Console.WriteLine($"Cached Value: {result} by ID: Integer");
        result = ((int, int))GetObject<object>("Int");
        Console.WriteLine($"Object Value: {result}");
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Blue;

        //Managed Type
        const string obj = "Jack";
        Cache(obj, "String");
        Console.WriteLine($"Cache Value: {obj} by ID: String");

        var objResult = GetObject<string>("String");
        Console.WriteLine($"Cached Value: {objResult} by ID: String");

        static (int, int) Point() => (10, 7);
    }
}
