using System;
using System.Runtime.InteropServices;
using Collections.Pooled;

[Test]
public unsafe static class DataBuffer
{
    private static readonly PooledDictionary<int, IntPtr> _Cached = new PooledDictionary<int, IntPtr>();
    private static readonly PooledDictionary<int, object> _CachedManaged = new PooledDictionary<int, object>();

    public static void Cache(void* value, string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            Console.WriteLine($"Cache {typeof(void*)} type ID: {id} can't be null");
            return;
        }
        Cache(value, id.ToHash());
    }

    public static void Cache(void* value, int id)
    {
        if (value is null)
        {
            Console.WriteLine($"Cache {typeof(void*)} type ID: {id} can't be null");
            return;
        }
        _Cached.Add(id, new IntPtr(value));
    }

    public static void Cache<TValue>(TValue value, int id) where TValue : unmanaged
    {
        var ptr = Marshal.AllocHGlobal(sizeof(TValue));
        *(TValue*)ptr = value;
        _Cached.Add(id, ptr);
    }

    public static void Cache<TValue>(TValue value, string id) where TValue : unmanaged
    {
        if (string.IsNullOrEmpty(id))
        {
            Console.WriteLine($"Cache value {value.GetType().Name} or id: {id} can't be null");
            return;
        }

        var ptr = Marshal.AllocHGlobal(sizeof(TValue));
        *(TValue*)ptr = value;
        _Cached.Add(id.ToHash(), ptr);
    }

    public static void Cache(IntPtr ptr, int id)
    {
        if (ptr == IntPtr.Zero)
        {
            Console.WriteLine($"Cache value: {nameof(IntPtr)} ID: {id} can't be null");
            return;
        }
        _Cached.Add(id, ptr);
    }

    public static void Cache(IntPtr ptr, string id)
    {
        if (ptr == IntPtr.Zero || string.IsNullOrEmpty(id))
        {
            Console.WriteLine($"Cache value: {nameof(IntPtr)} or id: {id} can't be null");
            return;
        }
        _Cached.Add(id.ToHash(), ptr);
    }

    public static void Cache(object value, int id)
    {
        if (value is null)
        {
            Console.WriteLine($"Cache value: {value} ID: {id} can't be null");
            return;
        }
        _CachedManaged.Add(id, value);
    }

    public static void Cache(object value, string id)
    {
        if (value is null || string.IsNullOrEmpty(id))
        {
            Console.WriteLine($"Cache value: {value} or id: {id} can't be null");
            return;
        }
        _CachedManaged.Add(id.ToHash(), value);
    }

    public static T Get<T>(string id, bool autoRemove = true) where T : unmanaged
    {
        if (string.IsNullOrEmpty(id))
            return default;

        return Get<T>(id.ToHash(), autoRemove);
    }

    public static T Get<T>(int id, bool autoRemove = true) where T : unmanaged
    {
        if (autoRemove)
        {
            if (_Cached.Remove(id, out var ptr))
            {
                var value = *(T*)ptr;
                Marshal.FreeHGlobal(ptr);
                return value;
            }
        }
        else
        {
            if (_Cached.TryGetValue(id, out var ptr))
                return *(T*)ptr;
        }
        return default;
    }

    public static T* Get<T>(int id) where T : unmanaged
    {
        if (_Cached.TryGetValue(id, out var ptr))
            return (T*)ptr;
        return null;
    }

    public static T GetObject<T>(string id, bool autoRemove = true) where T : class
    {
        if (string.IsNullOrEmpty(id))
            return default;

        return GetObject<T>(id.ToHash(), autoRemove);
    }

    public static T GetObject<T>(int id, bool autoRemove = true) where T : class
    {
        if (autoRemove)
        {
            if (_CachedManaged.Remove(id, out var data))
                return (T)data;
        }
        else
        {
            if (_CachedManaged.TryGetValue(id, out var data))
                return (T)data;
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
        TestCache("User ID");
        TestGet("User ID");

        // Managed Type
        const string obj = "Jack";
        Cache(obj, "String");
        Console.WriteLine($"Cache Value: {obj} by ID: String");

        var objResult = GetObject<string>("String");
        Console.WriteLine($"Cached Value: {objResult} by ID: String");
    }

    private static void TestCache(string id)
    {
        // Value Type
        var value = (100, 777);
        Cache(value, id);
        Console.WriteLine($"Cache Value: {value} by ID: {id}");
    }

    private static void TestGet(string id)
    {
        var result = Get<(int, int)>(id);
        Console.WriteLine($"Cached Value: {result} by ID: {id}");
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Blue;
    }
}
