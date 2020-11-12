#nullable enable
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Re.LC
{
    public unsafe struct LCSection
    {
        public string? Name => new string(key, 0, nameLength);
        public int Count => length;
        public bool IsEmpty => length == 0 && values is null;

        private char* key;
        private int nameLength;
        private LCValue* values;
        private int length;

        public LCValue this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (values is null)
                    throw new NullReferenceException();

                if (index < 0 || index >= length)
                    throw new IndexOutOfRangeException();

                return values[index];
            }
        }

        public LCSection(Span<char> name) : this(name, Span<LCValue>.Empty) { }
        public LCSection(ReadOnlySpan<char> name) : this(name, Span<LCValue>.Empty) { }

        public LCSection(ReadOnlySpan<char> name, Span<LCValue> kv)
        {
            if (!name.IsEmpty)
            {
                fixed (char* ptr = &name.GetPinnableReference())
                    this.key = ptr;
            }
            else
            {
                this.key = null;
            }
            if (!kv.IsEmpty)
            {
                fixed (LCValue* ptr = &kv.GetPinnableReference())
                    this.values = ptr;
            }
            else
            {
                this.values = null;
            }
            this.nameLength = name.Length;
            this.length = kv.Length;
        }

        public void SetValues(Span<LCValue> kv)
        {
            fixed (LCValue* ptr = &kv.GetPinnableReference())
                this.values = ptr;
            this.length = kv.Length;
        }

        public override string? ToString()
        {
            if (IsEmpty)
                return null;

            StringBuilder builder = new($"Section: {Name ?? "__MAIN__"}\n");
            for (int i = 0; i < length; i++)
                builder.Append(values[i].ToString());
            builder.AppendLine();
            return builder.ToString();
        }
    }

    public enum LCValueType : byte
    {
        Unknown,
        // Value Type
        Bool,
        Char,
        Byte,
        SByte,
        Int16,
        UInt16,
        Int,
        UInt,
        Int64,
        UInt64,
        Float,
        Double,
        Decimal,
        Enum,
        // Reference Type (Will As Span<char>)
        String,
        // LC Value Type
        LCArray,
        LCMap
    }

    public interface ILCValue
    {
        LCValueType Type { get; }
        string? Key { get; }
    }

    /// <summary>
    /// Could inclued key-value, key-only or value-only
    /// </summary>
    public unsafe struct LCValue : ILCValue, IEquatable<LCValue>
    {
        public string? Key => new string(key, 0, keyLength);
        public LCValueType Type => type;

        private char* key;
        private int keyLength;
        private void* value;
        private int valueLength;
        /// <summary>
        /// Value type
        /// </summary>
        private LCValueType type;

        #region Key-Value

        public LCValue(char* key, int length) : this(key, length, null, 0, LCValueType.Unknown) { }

        public LCValue(char* key, int length, void* value, int valueLength, LCValueType type)
        {
            this.key = key;
            this.keyLength = length;
            this.value = value;
            this.valueLength = valueLength;
            this.type = type;
        }

        public LCValue(Span<char> key) : this(key, null, 0, LCValueType.Unknown) { }
        public LCValue(Span<char> key, Span<char> value) : this(key.ToPointer(), key.Length, value.ToPointer(), value.Length, LCValueType.String) { }
        public LCValue(Span<char> key, void* value, int length, LCValueType type) : this(key.ToPointer(), key.Length, value, length, type) { }
        public LCValue(ReadOnlySpan<char> key, ReadOnlySpan<char> value) : this(key, value.ToPointer(), value.Length, LCValueType.String) { }
        public LCValue(ReadOnlySpan<char> key, void* value, int length, LCValueType type) : this(key.ToPointer(), key.Length, value, length, type) { }
        // public LCValue(ReadOnlySpan<char> key)
        // {
        //     fixed (char* kPtr = &key.GetPinnableReference()) this.key = kPtr; this.keyLength = key.Length;
        //     this.value = null;
        //     this.valueLength = 0;
        //     this.type = LCValueType.String;
        // }
        // public LCValue(ReadOnlySpan<char> key, ReadOnlySpan<char> value)
        // {
        //     fixed (char* kPtr = &key.GetPinnableReference()) this.key = kPtr; this.keyLength = key.Length;
        //     fixed (char* vPtr = &value.GetPinnableReference()) this.value = vPtr; this.valueLength = value.Length;
        //     this.type = LCValueType.String;
        // }
        public LCValue(Span<char> key, bool value) : this(key, Unsafe.AsPointer(ref value), 1, LCValueType.Bool) { }
        public LCValue(Span<char> key, char value) : this(key, Unsafe.AsPointer(ref value), 1, LCValueType.Char) { }
        public LCValue(Span<char> key, byte value) : this(key, Unsafe.AsPointer(ref value), 1, LCValueType.Byte) { }
        public LCValue(Span<char> key, sbyte value) : this(key, Unsafe.AsPointer(ref value), 1, LCValueType.SByte) { }
        public LCValue(Span<char> key, short value) : this(key, Unsafe.AsPointer(ref value), 1, LCValueType.Int16) { }
        public LCValue(Span<char> key, ushort value) : this(key, Unsafe.AsPointer(ref value), 1, LCValueType.UInt16) { }
        public LCValue(Span<char> key, int value) : this(key, Unsafe.AsPointer(ref value), 1, LCValueType.Int) { }
        public LCValue(Span<char> key, uint value) : this(key, Unsafe.AsPointer(ref value), 1, LCValueType.UInt) { }
        public LCValue(Span<char> key, long value) : this(key, Unsafe.AsPointer(ref value), 1, LCValueType.Int64) { }
        public LCValue(Span<char> key, ulong value) : this(key, Unsafe.AsPointer(ref value), 1, LCValueType.UInt64) { }
        public LCValue(Span<char> key, float value) : this(key, Unsafe.AsPointer(ref value), 1, LCValueType.Float) { }
        public LCValue(Span<char> key, double value) : this(key, Unsafe.AsPointer(ref value), 1, LCValueType.Double) { }
        public LCValue(Span<char> key, decimal value) : this(key, Unsafe.AsPointer(ref value), 1, LCValueType.Decimal) { }
        public LCValue(ReadOnlySpan<char> key, bool value) : this(key, Unsafe.AsPointer(ref value), 1, LCValueType.Bool) { }
        public LCValue(ReadOnlySpan<char> key, char value) : this(key, Unsafe.AsPointer(ref value), 1, LCValueType.Char) { }
        public LCValue(ReadOnlySpan<char> key, byte value) : this(key, Unsafe.AsPointer(ref value), 1, LCValueType.Byte) { }
        public LCValue(ReadOnlySpan<char> key, sbyte value) : this(key, Unsafe.AsPointer(ref value), 1, LCValueType.SByte) { }
        public LCValue(ReadOnlySpan<char> key, short value) : this(key, Unsafe.AsPointer(ref value), 1, LCValueType.Int16) { }
        public LCValue(ReadOnlySpan<char> key, ushort value) : this(key, Unsafe.AsPointer(ref value), 1, LCValueType.UInt16) { }
        public LCValue(ReadOnlySpan<char> key, int value) : this(key, Unsafe.AsPointer(ref value), 1, LCValueType.Int) { }
        public LCValue(ReadOnlySpan<char> key, uint value) : this(key, Unsafe.AsPointer(ref value), 1, LCValueType.UInt) { }
        public LCValue(ReadOnlySpan<char> key, long value) : this(key, Unsafe.AsPointer(ref value), 1, LCValueType.Int64) { }
        public LCValue(ReadOnlySpan<char> key, ulong value) : this(key, Unsafe.AsPointer(ref value), 1, LCValueType.UInt64) { }
        public LCValue(ReadOnlySpan<char> key, float value) : this(key, Unsafe.AsPointer(ref value), 1, LCValueType.Float) { }
        public LCValue(ReadOnlySpan<char> key, double value) : this(key, Unsafe.AsPointer(ref value), 1, LCValueType.Double) { }
        public LCValue(ReadOnlySpan<char> key, decimal value) : this(key, Unsafe.AsPointer(ref value), 1, LCValueType.Decimal) { }

        // public LCValue(ReadOnlySpan<char> key, LCArray value)
        // {
        //     fixed (char* kPtr = &key.GetPinnableReference()) this.key = kPtr; this.keyLength = key.Length;
        //     this.value = value; this.valueLength = 0;
        //     this.type = LCValueType.LCArray;
        // }
        // public LCValue(ReadOnlySpan<char> key, LCMap value)
        // {
        //     fixed (char* kPtr = &key.GetPinnableReference()) this.key = kPtr; this.keyLength = key.Length;
        //     this.value = value; this.valueLength = 0;
        //     this.type = LCValueType.LCMap;
        // }

        #endregion

        #region Value-Only

        // public LCValue(ReadOnlySpan<char> value, LCValueType type) { this.key = null; this.value = value; this.type = type; }
        // // LC Value Type
        // public LCValue(LCArray value) { this.key = null; this.value = &value; this.type = LCValueType.LCArray; }
        // public LCValue(LCMap value) { this.key = null; this.value = &value; this.type = LCValueType.LCMap; }

        #endregion

        #region Read Value Methods

        public T ReadValue<T>() where T : unmanaged => *(T*)value;

        // public bool ReadBool() => *(bool*)value;
        // public char ReadChar() => *(char*)value;
        // public byte ReadByte() => *(byte*)value;
        // public sbyte ReadSByte() => *(sbyte*)value;
        // public short ReadInt16() => *(short*)value;
        // public ushort ReadUInt16() => *(ushort*)value;
        // public int ReadInt() => *(int*)value;
        // public uint ReadUInt() => *(uint*)value;
        // public long ReadInt64() => *(long*)value;
        // public ulong ReadUInt64() => *(ulong*)value;
        // public float ReadFloat() => *(float*)value;
        // public double ReadDouble() => *(double*)value;
        // public decimal ReadDecimal() => *(decimal*)value;
        public string ReadString() => new string((char*)value, 0, valueLength);
        // LC Value Type
        public Span<LCValue> ReadArray() => (value != null && valueLength > 0) ? new Span<LCValue>(value, valueLength) : default;
        // public LCMap ReadMap() => *(LCMap*)value;

        #endregion

        public void SetKey(char* key, int length)
        {
            this.key = key;
            this.keyLength = length;
        }

        public void SetKey(Span<char> key)
        {
            fixed (char* ptr = &key.GetPinnableReference())
                this.key = ptr;
            this.keyLength = key.Length;
        }

        public void SetKey(ReadOnlySpan<char> key)
        {
            fixed (char* ptr = &key.GetPinnableReference())
                this.key = ptr;
            this.keyLength = key.Length;
        }

        public void SetValue(Span<char> value) => SetValue(Unsafe.AsPointer(ref value.GetPinnableReference()), value.Length, LCValueType.String);
        public void SetValue(Span<LCValue> value) => SetValue(Unsafe.AsPointer(ref value.GetPinnableReference()), value.Length, LCValueType.LCArray);

        public void SetValue(void* value, int length, LCValueType type)
        {
            this.value = value;
            this.valueLength = length;
            this.type = type;
        }

        public override string ToString()
        {
            StringBuilder builder = new();
            if (!string.IsNullOrEmpty(Key))
                builder.Append(Key).Append("\t: ");
            (type switch
            {
                LCValueType.Unknown => builder.Append(nameof(LCValueType.Unknown)),
                // Value Type
                LCValueType.Bool => builder.Append(ReadValue<bool>()),
                LCValueType.Char => builder.Append(ReadValue<char>()),
                LCValueType.Byte => builder.Append(ReadValue<byte>()),
                LCValueType.SByte => builder.Append(ReadValue<sbyte>()),
                LCValueType.Int16 => builder.Append(ReadValue<short>()),
                LCValueType.UInt16 => builder.Append(ReadValue<ushort>()),
                LCValueType.Int => builder.Append(ReadValue<int>()),
                LCValueType.UInt => builder.Append(ReadValue<uint>()),
                LCValueType.Int64 => builder.Append(ReadValue<long>()),
                LCValueType.UInt64 => builder.Append(ReadValue<ulong>()),
                LCValueType.Float => builder.Append(ReadValue<float>()),
                LCValueType.Double => builder.Append(ReadValue<double>()),
                LCValueType.Decimal => builder.Append(ReadValue<decimal>()),
                LCValueType.Enum => builder.Append(nameof(LCValueType.Enum)),
                // Reference Type (Will As Span<char>)
                LCValueType.String => builder.Append(ReadString()),
                // LC Value Type
                LCValueType.LCArray => builder.Append(ReadArray().Join()),
                LCValueType.LCMap => builder.Append(ReadValue<LCMap>().ToString()),
                _ => builder.Append("[ Unknown Value Type ]"),
            }).AppendLine();
            return builder.ToString();
        }

        public bool Equals(LCValue other) => this == other;

        public static bool operator ==(LCValue left, LCValue right)
        {
            if (left.Key != right.Key) return false;
            if (left.keyLength != right.keyLength) return false;
            if (left.type != right.type) return false;
            if (left.valueLength != right.valueLength) return false;
            return left.type switch
            {
                LCValueType.Bool => left.ReadValue<bool>() == right.ReadValue<bool>(),
                LCValueType.Char => left.ReadValue<char>() == right.ReadValue<char>(),
                LCValueType.Byte => left.ReadValue<byte>() == right.ReadValue<byte>(),
                LCValueType.SByte => left.ReadValue<sbyte>() == right.ReadValue<sbyte>(),
                LCValueType.Int16 => left.ReadValue<short>() == right.ReadValue<short>(),
                LCValueType.UInt16 => left.ReadValue<ushort>() == right.ReadValue<ushort>(),
                LCValueType.Int => left.ReadValue<int>() == right.ReadValue<int>(),
                LCValueType.UInt => left.ReadValue<uint>() == right.ReadValue<uint>(),
                LCValueType.Int64 => left.ReadValue<long>() == right.ReadValue<long>(),
                LCValueType.UInt64 => left.ReadValue<ulong>() == right.ReadValue<ulong>(),
                LCValueType.Float => left.ReadValue<float>() == right.ReadValue<float>(),
                LCValueType.Double => left.ReadValue<double>() == right.ReadValue<double>(),
                LCValueType.Decimal => left.ReadValue<decimal>() == right.ReadValue<decimal>(),
                // LCValueType.Enum => left.ReadValue<Enum> == right.ReadValue<Enum>(),
                LCValueType.String => left.ReadString() == right.ReadString(),
                LCValueType.LCArray => left.ReadArray() == right.ReadArray(),
                // LCValueType.LCMap => left.ReadValue<LCMap> == right.ReadValue<LCMap>(),
                _ => false,
            };
        }

        public static bool operator !=(LCValue left, LCValue right) => !(left == right);
    }

    // public unsafe struct LCArray : ILCValue
    // {
    //     public string? Key => new string(key);
    //     public LCValueType Type => LCValueType.LCArray;
    //     public LCValueType ValuesType => valuesType;

    //     // Key - Same Type Values
    //     private char* key;
    //     private void* value;
    //     private LCValueType valuesType;

    //     public LCArray(char* key, void* value, LCValueType valuesType)
    //     {
    //         this.key = key;
    //         this.value = value;
    //         this.valuesType = valuesType;
    //     }
    // }

    public unsafe struct LCMap : ILCValue
    {
        public string? Key => new string(key);
        public LCValueType Type => LCValueType.LCMap;
        public LCValueType MapKeyType => keyType;
        public LCValueType MapValueType => valueType;

        //TODO: Dictionary
        private char* key;
        private LCValueType keyType;
        private LCValueType valueType;

        public LCMap(char* key, LCValueType keyType, LCValueType valueType)
        {
            this.key = key;
            this.keyType = keyType;
            this.valueType = valueType;
        }

        public static LCMap Create<TKey, TValue>(char* key)
        {
            return new LCMap(key,
                keyType: LCValueUtilities.GetLCType<TKey>(),
                valueType: LCValueUtilities.GetLCType<TValue>());
        }
    }

    public unsafe readonly struct LCRange
    {
        public readonly int Start;
        public readonly int End;

        public LCRange(int start, int end)
        {
            this.Start = start;
            this.End = end;
        }

        public override string ToString()
        {
            return $"(Start: {Start}, End: {End})";
        }
    }

    /** LC Features
     * 1. Operator .. and ...
     *     # NOTE: Generaly used for frames of no events
     *     frames = [
     *         # Generaly Way
     *         naurto_idle_001
     *         naurto_idle_002
     *         naurto_idle_005
     *         naurto_idle_007
     *
     *         # First Way
     *         naruto_idle_{001..007}
     *
     *         # Secound Way
     *         naruto_idle_001
     *         ...
     *         naruto_idle_007
     *     ]
     *
     * 2. Tag
     *     # NOTE: Used before key of key-value
     */

    internal unsafe static class LCValueUtilities
    {
        public static LCValueType GetLCType<T>()
        {
            var t = typeof(T);
            if (t.IsPrimitive)
            {
                if (t == typeof(LCValue*))
                    return LCValueType.LCArray;
                if (t == typeof(LCMap))
                    return LCValueType.LCMap;
                if (t == typeof(Enum))
                    return LCValueType.Enum;
                if (t == typeof(int))
                    return LCValueType.Int;
                if (t == typeof(float))
                    return LCValueType.Float;
                if (t == typeof(char))
                    return LCValueType.Char;
                if (t == typeof(byte))
                    return LCValueType.Byte;
                if (t == typeof(short))
                    return LCValueType.Int16;
                if (t == typeof(long))
                    return LCValueType.Int64;
                if (t == typeof(double))
                    return LCValueType.Double;
                if (t == typeof(uint))
                    return LCValueType.UInt;
                if (t == typeof(sbyte))
                    return LCValueType.SByte;
                if (t == typeof(ushort))
                    return LCValueType.UInt16;
                if (t == typeof(ulong))
                    return LCValueType.UInt64;
            }
            if (t == typeof(string))
                return LCValueType.String;
            return LCValueType.Unknown;
        }

        public static string? Join<LCT>(this Span<LCT> source, string? sep = null) where LCT : ILCValue
        {
            if (source.Length == 0)
                return string.Empty;

            StringBuilder builder = new("[\n");
            if (sep is null)
            {
                foreach (var item in source)
                    builder.Append('\t').Append(item.ToString());
            }
            else
            {
                foreach (var item in source)
                    builder.Append('\t').Append(item.ToString()).Append(sep);
                builder.Remove(builder.Length - sep.Length, sep.Length);
            }
            // Remove last sep
            builder.Append(']');
            return builder.ToString();
        }

        public static char* ToPointer(this Span<char> source)
        {
            fixed (char* ptr = &source.GetPinnableReference()) return ptr;
        }

        public static char* ToPointer(this ReadOnlySpan<char> source)
        {
            fixed (char* ptr = &source.GetPinnableReference()) return ptr;
        }
    }
}
#nullable disable
