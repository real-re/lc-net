using System;
using System.Reflection;

namespace Re.LC
{
    public unsafe static class LCSerializer
    {
        public static string Serialize<T>(T t)
        {
            return null;
        }

        public static byte* SerializeToBytes<T>(T t)
        {
            return null;
        }

        // public static T Deserialize<T>(Span<char> value) where T : new()
        // public static T Deserialize<T>(char* value) where T : new()
        public static T Deserialize<T>(string value) where T : new()
        {
            var props = typeof(T).GetProperties(kDefaultFlags);
            if (props is null || props.Length == 0)
                return default;

            var t = new T();
            foreach (var prop in props)
            {
                //TODO: 避免SetValue的装箱操作
                // prop.SetValue();
            }

            return t;
        }

        private const BindingFlags kDefaultFlags = BindingFlags.Public | BindingFlags.Instance;
    }
}
