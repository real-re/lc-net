#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Re.LC
{
    public unsafe struct LCSection
    {
        public string? Name => (name == null || nameLength <= 0) ? null : new string(name, 0, nameLength);
        public int Count => length;
        public bool IsEmpty => length == 0 && values is null;

        private char* name;
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
                    this.name = ptr;
            }
            else
            {
                this.name = null;
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

            StringBuilder builder = new($"[Section] : {Name ?? "__MAIN__"}\n");
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
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct LCValue : ILCValue, IEquatable<LCValue>
    {
        public string? Key => new string(key, 0, keyLength);
        public LCValueType Type => type;

        [FieldOffset(0)]
        private char* key;
        [FieldOffset(8)]
        private int keyLength;
        [FieldOffset(12)]
        private int valueLength;
        /// <summary>
        /// Value type
        /// </summary>
        [FieldOffset(16)]
        private LCValueType type;
        // Union
        // NOTE：联合体始终比最大的结构体大，所以用大范围数据取缔小范围即可
        [FieldOffset(17)] private bool boolVal;
        [FieldOffset(17)] private long longVal;
        [FieldOffset(17)] private ulong ulongVal;
        [FieldOffset(17)] private double doubleVal;
        // [FieldOffset(17)] private decimal decimalVal;
        [FieldOffset(17)] private void* value;

        #region Key-Value

        public LCValue(char* key, int length) : this(key, length, null, 0, LCValueType.Unknown) { }

        public LCValue(char* key, int length, void* value, int valueLength, LCValueType type)
        {
            this.key = key;
            this.keyLength = length;
            {
                this.boolVal = default;
                this.longVal = default;
                this.ulongVal = default;
                this.doubleVal = default;
                this.value = value;
            }
            this.valueLength = valueLength;
            this.type = type;
        }

        public LCValue(Span<char> key) : this(key, null, 0, LCValueType.Unknown) { }
        public LCValue(Span<char> key, LCValueType type) : this(key, null, 0, type) { }
        public LCValue(Span<char> key, Span<char> value) : this(key.ToPointer(), key.Length, value.ToPointer(), value.Length, LCValueType.String) { }
        public LCValue(Span<char> key, void* value, int length, LCValueType type) : this(key.ToPointer(), key.Length, value, length, type) { }
        public LCValue(ReadOnlySpan<char> key) : this(key, null, 0, LCValueType.Unknown) { }
        public LCValue(ReadOnlySpan<char> key, LCValueType type) : this(key, null, 0, type) { }
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
        // TODO: union type
        public LCValue(Span<char> key, bool value) : this(key, LCValueType.Bool) { this.boolVal = value; }
        public LCValue(Span<char> key, char value) : this(key, LCValueType.Char) { this.longVal = value; }
        public LCValue(Span<char> key, byte value) : this(key, LCValueType.Byte) { this.ulongVal = value; }
        public LCValue(Span<char> key, sbyte value) : this(key, LCValueType.SByte) { this.longVal = value; }
        public LCValue(Span<char> key, short value) : this(key, LCValueType.Int16) { this.longVal = value; }
        public LCValue(Span<char> key, ushort value) : this(key, LCValueType.UInt16) { this.ulongVal = value; }
        public LCValue(Span<char> key, int value) : this(key, LCValueType.Int) { this.longVal = value; }
        public LCValue(Span<char> key, uint value) : this(key, LCValueType.UInt) { this.ulongVal = value; }
        public LCValue(Span<char> key, long value) : this(key, LCValueType.Int64) { this.longVal = value; }
        public LCValue(Span<char> key, ulong value) : this(key, LCValueType.UInt64) { this.ulongVal = value; }
        public LCValue(Span<char> key, float value) : this(key, LCValueType.Float) { this.doubleVal = value; }
        public LCValue(Span<char> key, double value) : this(key, LCValueType.Double) { this.doubleVal = value; }
        // public LCValue(Span<char> key, decimal value) : this(key, LCValueType.Decimal) { this.decimalVal = value; }
        public LCValue(ReadOnlySpan<char> key, bool value) : this(key, LCValueType.Bool) { this.boolVal = value; }
        public LCValue(ReadOnlySpan<char> key, char value) : this(key, LCValueType.Char) { this.longVal = value; }
        public LCValue(ReadOnlySpan<char> key, byte value) : this(key, LCValueType.Byte) { this.ulongVal = value; }
        public LCValue(ReadOnlySpan<char> key, sbyte value) : this(key, LCValueType.SByte) { this.longVal = value; }
        public LCValue(ReadOnlySpan<char> key, short value) : this(key, LCValueType.Int16) { this.longVal = value; }
        public LCValue(ReadOnlySpan<char> key, ushort value) : this(key, LCValueType.UInt16) { this.ulongVal = value; }
        public LCValue(ReadOnlySpan<char> key, int value) : this(key, LCValueType.Int) { this.longVal = value; }
        public LCValue(ReadOnlySpan<char> key, uint value) : this(key, LCValueType.UInt) { this.ulongVal = value; }
        public LCValue(ReadOnlySpan<char> key, long value) : this(key, LCValueType.Int64) { this.longVal = value; }
        public LCValue(ReadOnlySpan<char> key, ulong value) : this(key, LCValueType.UInt64) { this.ulongVal = value; }
        public LCValue(ReadOnlySpan<char> key, float value) : this(key, LCValueType.Float) { this.doubleVal = value; }
        public LCValue(ReadOnlySpan<char> key, double value) : this(key, LCValueType.Double) { this.doubleVal = value; }
        // public LCValue(ReadOnlySpan<char> key, decimal value) : this(key, LCValueType.Decimal) { this.decimalVal = value; }

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

        #region Read Value Methods

        // TODO: 针对各个类型单独实现（参考c++ vector）

        public bool ReadBool() => type switch
        {
            LCValueType.Bool => boolVal,
            LCValueType.Char => longVal > 1,
            LCValueType.Byte => ulongVal > 1,
            LCValueType.SByte => ulongVal > 1,
            LCValueType.Int16 => longVal > 1,
            LCValueType.UInt16 => ulongVal > 1,
            LCValueType.Int => longVal > 1,
            LCValueType.UInt => ulongVal > 1,
            LCValueType.Int64 => longVal > 1,
            LCValueType.UInt64 => ulongVal > 1,
            LCValueType.Float => doubleVal > 1,
            LCValueType.Double => doubleVal > 1,
            LCValueType.Decimal => doubleVal > 1,
            LCValueType.Enum => longVal > 1,
            LCValueType.String => value != null && bool.TryParse(new ReadOnlySpan<char>(value, valueLength), out bool val) && val,
            LCValueType.LCArray => default,
            LCValueType.LCMap => default,
            LCValueType.Unknown => default,
            _ => default,
        };
        public char ReadChar() => type switch
        {
            LCValueType.Bool => char.MinValue,
            LCValueType.Char => (char)ulongVal,
            LCValueType.Byte => (char)longVal,
            LCValueType.SByte => (char)longVal,
            LCValueType.Int16 => (char)longVal,
            LCValueType.UInt16 => (char)ulongVal,
            LCValueType.Int => (char)longVal,
            LCValueType.UInt => (char)ulongVal,
            LCValueType.Int64 => (char)longVal,
            LCValueType.UInt64 => (char)ulongVal,
            LCValueType.Float => (char)doubleVal,
            LCValueType.Double => (char)doubleVal,
            LCValueType.Decimal => (char)doubleVal,
            LCValueType.Enum => (char)longVal,
            LCValueType.String => ((char*)value) == null ? default : char.TryParse(new string((char*)value, 0, valueLength), out char val) ? val : default,
            LCValueType.LCArray => default,
            LCValueType.LCMap => default,
            LCValueType.Unknown => default,
            _ => default,
        };
        public byte ReadByte() => type switch
        {
            LCValueType.Bool => boolVal ? 1 : 0,
            LCValueType.Char => (byte)ulongVal,
            LCValueType.Byte => (byte)longVal,
            LCValueType.SByte => (byte)longVal,
            LCValueType.Int16 => (byte)longVal,
            LCValueType.UInt16 => (byte)ulongVal,
            LCValueType.Int => (byte)longVal,
            LCValueType.UInt => (byte)ulongVal,
            LCValueType.Int64 => (byte)longVal,
            LCValueType.UInt64 => (byte)ulongVal,
            LCValueType.Float => (byte)doubleVal,
            LCValueType.Double => (byte)doubleVal,
            LCValueType.Decimal => (byte)doubleVal,
            LCValueType.Enum => (byte)longVal,
            LCValueType.String => value == null ? default : byte.TryParse(new ReadOnlySpan<char>(value, valueLength), out byte val) ? val : default,
            LCValueType.LCArray => default,
            LCValueType.LCMap => default,
            LCValueType.Unknown => default,
            _ => default,
        };
        public sbyte ReadSByte() => type switch
        {
            LCValueType.Bool => boolVal ? 1 : 0,
            LCValueType.Char => (sbyte)ulongVal,
            LCValueType.Byte => (sbyte)longVal,
            LCValueType.SByte => (sbyte)longVal,
            LCValueType.Int16 => (sbyte)longVal,
            LCValueType.UInt16 => (sbyte)ulongVal,
            LCValueType.Int => (sbyte)longVal,
            LCValueType.UInt => (sbyte)ulongVal,
            LCValueType.Int64 => (sbyte)longVal,
            LCValueType.UInt64 => (sbyte)ulongVal,
            LCValueType.Float => (sbyte)doubleVal,
            LCValueType.Double => (sbyte)doubleVal,
            LCValueType.Decimal => (sbyte)doubleVal,
            LCValueType.Enum => (sbyte)longVal,
            LCValueType.String => value == null ? default : sbyte.TryParse(new ReadOnlySpan<char>(value, valueLength), out sbyte val) ? val : default,
            LCValueType.LCArray => default,
            LCValueType.LCMap => default,
            LCValueType.Unknown => default,
            _ => default,
        };
        public short ReadInt16() => type switch
        {
            LCValueType.Bool => boolVal ? 1 : 0,
            LCValueType.Char => (short)ulongVal,
            LCValueType.Byte => (short)longVal,
            LCValueType.SByte => (short)longVal,
            LCValueType.Int16 => (short)longVal,
            LCValueType.UInt16 => (short)ulongVal,
            LCValueType.Int => (short)longVal,
            LCValueType.UInt => (short)ulongVal,
            LCValueType.Int64 => (short)longVal,
            LCValueType.UInt64 => (short)ulongVal,
            LCValueType.Float => (short)doubleVal,
            LCValueType.Double => (short)doubleVal,
            LCValueType.Decimal => (short)doubleVal,
            LCValueType.Enum => (short)longVal,
            LCValueType.String => value == null ? default : short.TryParse(new ReadOnlySpan<char>(value, valueLength), out short val) ? val : default,
            LCValueType.LCArray => default,
            LCValueType.LCMap => default,
            LCValueType.Unknown => default,
            _ => default,
        };
        public ushort ReadUInt16() => type switch
        {
            LCValueType.Bool => boolVal ? 1 : 0,
            LCValueType.Char => (ushort)ulongVal,
            LCValueType.Byte => (ushort)longVal,
            LCValueType.SByte => (ushort)longVal,
            LCValueType.Int16 => (ushort)longVal,
            LCValueType.UInt16 => (ushort)ulongVal,
            LCValueType.Int => (ushort)longVal,
            LCValueType.UInt => (ushort)ulongVal,
            LCValueType.Int64 => (ushort)longVal,
            LCValueType.UInt64 => (ushort)ulongVal,
            LCValueType.Float => (ushort)doubleVal,
            LCValueType.Double => (ushort)doubleVal,
            LCValueType.Decimal => (ushort)doubleVal,
            LCValueType.Enum => (ushort)longVal,
            LCValueType.String => value == null ? default : ushort.TryParse(new ReadOnlySpan<char>(value, valueLength), out ushort val) ? val : default,
            LCValueType.LCArray => default,
            LCValueType.LCMap => default,
            LCValueType.Unknown => default,
            _ => default,
        };
        public int ReadInt() => type switch
        {
            LCValueType.Bool => boolVal ? 1 : 0,
            LCValueType.Char => (int)ulongVal,
            LCValueType.Byte => (int)longVal,
            LCValueType.SByte => (int)longVal,
            LCValueType.Int16 => (int)longVal,
            LCValueType.UInt16 => (int)ulongVal,
            LCValueType.Int => (int)longVal,
            LCValueType.UInt => (int)ulongVal,
            LCValueType.Int64 => (int)longVal,
            LCValueType.UInt64 => (int)ulongVal,
            LCValueType.Float => (int)doubleVal,
            LCValueType.Double => (int)doubleVal,
            LCValueType.Decimal => (int)doubleVal,
            LCValueType.Enum => (int)longVal,
            LCValueType.String => value == null ? default : int.TryParse(new ReadOnlySpan<char>(value, valueLength), out int val) ? val : default,
            LCValueType.LCArray => default,
            LCValueType.LCMap => default,
            LCValueType.Unknown => default,
            _ => default,
        };
        public uint ReadUInt() => type switch
        {
            LCValueType.Bool => boolVal ? 1 : 0,
            LCValueType.Char => (uint)ulongVal,
            LCValueType.Byte => (uint)longVal,
            LCValueType.SByte => (uint)longVal,
            LCValueType.Int16 => (uint)longVal,
            LCValueType.UInt16 => (uint)ulongVal,
            LCValueType.Int => (uint)longVal,
            LCValueType.UInt => (uint)ulongVal,
            LCValueType.Int64 => (uint)longVal,
            LCValueType.UInt64 => (uint)ulongVal,
            LCValueType.Float => (uint)doubleVal,
            LCValueType.Double => (uint)doubleVal,
            LCValueType.Decimal => (uint)doubleVal,
            LCValueType.Enum => (uint)longVal,
            LCValueType.String => value == null ? default : uint.TryParse(new ReadOnlySpan<char>(value, valueLength), out uint val) ? val : default,
            LCValueType.LCArray => default,
            LCValueType.LCMap => default,
            LCValueType.Unknown => default,
            _ => default,
        };
        public long ReadInt64() => type switch
        {
            LCValueType.Bool => boolVal ? 1 : 0,
            LCValueType.Char => (long)ulongVal,
            LCValueType.Byte => (long)ulongVal,
            LCValueType.SByte => longVal,
            LCValueType.Int16 => longVal,
            LCValueType.UInt16 => (long)ulongVal,
            LCValueType.Int => longVal,
            LCValueType.UInt => (long)ulongVal,
            LCValueType.Int64 => longVal,
            LCValueType.UInt64 => (long)ulongVal,
            LCValueType.Float => (long)doubleVal,
            LCValueType.Double => (long)doubleVal,
            LCValueType.Decimal => (long)doubleVal,
            LCValueType.Enum => longVal,
            LCValueType.String => value == null ? default : long.TryParse(new ReadOnlySpan<char>(value, valueLength), out long val) ? val : default,
            LCValueType.LCArray => default,
            LCValueType.LCMap => default,
            LCValueType.Unknown => default,
            _ => default,
        };
        public ulong ReadUInt64() => type switch
        {
            LCValueType.Bool => boolVal ? 1 : 0,
            LCValueType.Char => ulongVal,
            LCValueType.Byte => ulongVal,
            LCValueType.SByte => (ulong)longVal,
            LCValueType.Int16 => (ulong)longVal,
            LCValueType.UInt16 => ulongVal,
            LCValueType.Int => (ulong)longVal,
            LCValueType.UInt => ulongVal,
            LCValueType.Int64 => (ulong)longVal,
            LCValueType.UInt64 => ulongVal,
            LCValueType.Float => (ulong)doubleVal,
            LCValueType.Double => (ulong)doubleVal,
            LCValueType.Decimal => (ulong)doubleVal,
            LCValueType.Enum => (ulong)longVal,
            LCValueType.String => value == null ? default : ulong.TryParse(new ReadOnlySpan<char>(value, valueLength), out ulong val) ? val : default,
            LCValueType.LCArray => default,
            LCValueType.LCMap => default,
            LCValueType.Unknown => default,
            _ => default,
        };
        public float ReadFloat() => type switch
        {
            LCValueType.Bool => boolVal ? 1 : 0,
            LCValueType.Char => ulongVal,
            LCValueType.Byte => ulongVal,
            LCValueType.SByte => longVal,
            LCValueType.Int16 => longVal,
            LCValueType.UInt16 => ulongVal,
            LCValueType.Int => longVal,
            LCValueType.UInt => ulongVal,
            LCValueType.Int64 => longVal,
            LCValueType.UInt64 => ulongVal,
            LCValueType.Float => (float)doubleVal,
            LCValueType.Double => (float)doubleVal,
            LCValueType.Decimal => (float)doubleVal,
            LCValueType.Enum => longVal,
            LCValueType.String => value == null ? default : float.TryParse(new ReadOnlySpan<char>(value, valueLength), out float val) ? val : default,
            LCValueType.LCArray => default,
            LCValueType.LCMap => default,
            LCValueType.Unknown => default,
            _ => default,
        };
        public double ReadDouble() => type switch
        {
            LCValueType.Bool => boolVal ? 1 : 0,
            LCValueType.Char => ulongVal,
            LCValueType.Byte => ulongVal,
            LCValueType.SByte => longVal,
            LCValueType.Int16 => longVal,
            LCValueType.UInt16 => ulongVal,
            LCValueType.Int => longVal,
            LCValueType.UInt => ulongVal,
            LCValueType.Int64 => longVal,
            LCValueType.UInt64 => ulongVal,
            LCValueType.Float => doubleVal,
            LCValueType.Double => doubleVal,
            LCValueType.Decimal => doubleVal,
            LCValueType.Enum => longVal,
            LCValueType.String => value == null ? default : double.TryParse(new ReadOnlySpan<char>(value, valueLength), out double val) ? val : default,
            LCValueType.LCArray => default,
            LCValueType.LCMap => default,
            LCValueType.Unknown => default,
            _ => default,
        };
        // public decimal ReadDecimal() => *(decimal*)value;
        public string ReadString() => value == null ? string.Empty : new string((char*)value, 0, valueLength);
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

        #region Set Value Methods

        public void SetValue(bool value) => boolVal = value;
        public void SetValue(char value) => ulongVal = value;
        public void SetValue(byte value) => ulongVal = value;
        public void SetValue(sbyte value) => longVal = value;
        public void SetValue(short value) => longVal = value;
        public void SetValue(ushort value) => ulongVal = value;
        public void SetValue(int value) => longVal = value;
        public void SetValue(uint value) => ulongVal = value;
        public void SetValue(long value) => longVal = value;
        public void SetValue(ulong value) => ulongVal = value;
        public void SetValue(float value) => doubleVal = value;
        public void SetValue(double value) => doubleVal = value;
        public void SetValue(Span<char> value) => SetValue(Unsafe.AsPointer(ref value.GetPinnableReference()), value.Length, LCValueType.String);
        public void SetValue(Span<LCValue> value) => SetValue(Unsafe.AsPointer(ref value.GetPinnableReference()), value.Length, LCValueType.LCArray);

        public void SetValue(void* value, int length, LCValueType type)
        {
            this.value = value;
            this.valueLength = length;
            this.type = type;
        }

        #endregion

        public override string ToString()
        {
            StringBuilder builder = new();
            if (!string.IsNullOrEmpty(Key))
                builder.Append(Key).Append(" : ");
            (type switch
            {
                LCValueType.Unknown => builder.Append(nameof(LCValueType.Unknown)),
                // Value Type
                LCValueType.Bool => builder.Append(ReadBool()),
                LCValueType.Char => builder.Append(ReadChar()),
                LCValueType.Byte => builder.Append(ReadByte()),
                LCValueType.SByte => builder.Append(ReadSByte()),
                LCValueType.Int16 => builder.Append(ReadInt16()),
                LCValueType.UInt16 => builder.Append(ReadUInt16()),
                LCValueType.Int => builder.Append(ReadInt()),
                LCValueType.UInt => builder.Append(ReadUInt()),
                LCValueType.Int64 => builder.Append(ReadInt64()),
                LCValueType.UInt64 => builder.Append(ReadUInt64()),
                LCValueType.Float => builder.Append(ReadFloat()),
                LCValueType.Double => builder.Append(ReadDouble()),
                // LCValueType.Decimal => builder.Append(ReadDecimal()),
                LCValueType.Enum => builder.Append(nameof(LCValueType.Enum)),
                // Reference Type (Will As Span<char>)
                LCValueType.String => builder.Append(ReadString()),
                // LC Value Type
                LCValueType.LCArray => builder.Append(ReadArray().Join()),
                // LCValueType.LCMap => builder.Append(ReadValue<LCMap>().ToString()),
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
                LCValueType.Bool => left.ReadBool() == right.ReadBool(),
                LCValueType.Char => left.ReadChar() == right.ReadChar(),
                LCValueType.Byte => left.ReadByte() == right.ReadByte(),
                LCValueType.SByte => left.ReadSByte() == right.ReadSByte(),
                LCValueType.Int16 => left.ReadInt16() == right.ReadInt16(),
                LCValueType.UInt16 => left.ReadUInt16() == right.ReadUInt16(),
                LCValueType.Int => left.ReadInt() == right.ReadInt(),
                LCValueType.UInt => left.ReadUInt() == right.ReadUInt(),
                LCValueType.Int64 => left.ReadInt64() == right.ReadInt64(),
                LCValueType.UInt64 => left.ReadUInt64() == right.ReadUInt64(),
                LCValueType.Float => left.ReadFloat() == right.ReadFloat(),
                LCValueType.Double => left.ReadDouble() == right.ReadDouble(),
                // LCValueType.Decimal => left.ReadDecimal() == right.ReadDecimal(),
                // LCValueType.Enum => left.ReadValue<Enum> == right.ReadValue<Enum>(),
                // TODO: Use strcmp insted of create a string object
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
                    builder.Append(item.ToString());
            }
            else
            {
                foreach (var item in source)
                    builder.Append(item.ToString()).Append(sep);
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
