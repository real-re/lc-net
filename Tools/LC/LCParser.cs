#nullable enable
using System;
using System.IO;

namespace Re.LC
{
    public unsafe static class LCParser
    {
        public static LC? From(string filePath)
        {
            // throw new FileNotFoundException(filePath);
            if (!File.Exists(filePath))
                return null;

            using var stream = File.OpenText(filePath);
            Span<char> buffer = stackalloc char[(int)stream.BaseStream.Length];
            stream.ReadBlock(buffer);

            if (buffer.IsEmpty)
                throw new NullReferenceException();

            // return Parse(lines);
            return new LC(
                name: Path.GetFileName(filePath),
                lines: null
            );
        }

        public static LC? FromString(string lc)
        {
            if (string.IsNullOrEmpty(lc))
                return null;

            fixed (char* ptr = lc)
                return Parse(new Span<char>(ptr, lc.Length * 2));
        }

        public static LC? FromString(char[] lc)
        {
            if (lc == null || lc.Length == 0)
                return null;

            fixed (char* ptr = lc)
                return Parse(new Span<char>(ptr, lc.Length));
        }

        public static LC? FromString(byte[] lc)
        {
            if (lc == null || lc.Length == 0)
                return null;

            fixed (byte* ptr = lc)
                return Parse(new Span<char>(ptr, lc.Length));
        }

        public static LC? Parse(Span<char> data)
        {
            if (data.IsEmpty)
                return null;

            #region Main Parser

            var ctx = new LCParserContext();
            int i = 0;
            Span<char> buffer = Span<char>.Empty;

            ctx.OnLCParserStart?.Invoke();
            foreach (var c in data)
            {
                switch (c)
                {
                    case ' ' or '\t' or '\r':
                        continue;
                    case '\n':
                        ctx.isNewLine = true;
                        break;
                    case '=':
                        ctx.endOfKey = true;

                        ctx.isValue = true;
                        break;
                    case '{':
                        ctx.isMap = true;
                        break;
                    case '}':
                        ctx.endOfMap = true;
                        break;
                    case '[':
                        ctx.isArrayOrSection = true;
                        break;
                    case ']':
                        ctx.endOfArrayOrSection = true;
                        break;
                    default:
                        ++i;
                        if (ctx.isArray)
                            ctx.isKey = true;
                        break;
                }
            }
            ctx.OnLCParserEOF?.Invoke();

            #endregion

            return null;
        }
    }

    // [StructLayout(LayoutKind.Explicit)]
    internal ref struct LCParserContext
    {
        public bool isNewLine;
        // Section
        public bool isSection;
        public bool endOfSection;
        // Key
        public bool isKey;
        public bool endOfKey;
        // Value
        public bool isValue;
        public bool endOfValue;
        // Array Value
        public bool isArray;
        public bool endOfArray;
        // Map Value
        public bool isMap;
        public bool endOfMap;
        // Array Or Section
        public bool isArrayOrSection;
        public bool endOfArrayOrSection;

        internal LCData data;

        public Action? OnLCParserStart;
        public Action? OnLCParserEOF;
    }

    public unsafe ref struct LCData
    {
        public string Name { get => name.ToString(); }
        public Span<LCSection> Sections
        {
            get => sections;
            set
            {
                sections = value;
                hasHeader = !sections.IsEmpty && sections[0].Name != null;
            }
        }

        public LCSection? Header => (hasHeader && !Sections.IsEmpty) ? Sections[0] : default;

        private Span<char> name;
        private Span<LCSection> sections;
        private bool hasHeader;

        public LCData(Span<char> name, Span<LCSection> sections)
        {
            this.name = name;
            this.sections = sections;
            this.hasHeader = !sections.IsEmpty && sections[0].Name != null;
        }
    }

    public unsafe struct LCSection
    {
        private char* name;
        private char** keys;
        private LCValue* values;
        private int length;

        //TODO: Custom string intern pool
        public string? Name => name == null ? null : new string(name);

        public LCValue this[int index]
        {
            get
            {
                if (keys == null || values == null)
                    throw new NullReferenceException();

                if (index < 0 || index >= length)
                    throw new IndexOutOfRangeException();

                return values[index];
            }
        }
    }

    public enum LCValueType : byte
    {
        Unknown,
        Null,
        VoidPtr,
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
        // Reference Type
        String,
        // LC Value Type
        LCArray,
        LCTable,
        LCMap,
    }

    public interface ILCValue
    {
        LCValueType Type { get; }
        string Key { get; }
    }

    public unsafe struct LCValue : ILCValue
    {
        public string Key => new string(key);
        public LCValueType Type => type;

        private char* key;
        private void* value;
        private LCValueType type;

        public LCValue(char* key, bool value) { this.key = key; this.value = &value; this.type = LCValueType.Bool; }
        public LCValue(char* key, char value) { this.key = key; this.value = &value; this.type = LCValueType.Char; }
        public LCValue(char* key, byte value) { this.key = key; this.value = &value; this.type = LCValueType.Byte; }
        public LCValue(char* key, sbyte value) { this.key = key; this.value = &value; this.type = LCValueType.SByte; }
        public LCValue(char* key, short value) { this.key = key; this.value = &value; this.type = LCValueType.Int16; }
        public LCValue(char* key, ushort value) { this.key = key; this.value = &value; this.type = LCValueType.UInt16; }
        public LCValue(char* key, int value) { this.key = key; this.value = &value; this.type = LCValueType.Int; }
        public LCValue(char* key, uint value) { this.key = key; this.value = &value; this.type = LCValueType.UInt; }
        public LCValue(char* key, long value) { this.key = key; this.value = &value; this.type = LCValueType.Int64; }
        public LCValue(char* key, ulong value) { this.key = key; this.value = &value; this.type = LCValueType.UInt64; }
        public LCValue(char* key, float value) { this.key = key; this.value = &value; this.type = LCValueType.Float; }
        public LCValue(char* key, double value) { this.key = key; this.value = &value; this.type = LCValueType.Double; }
        public LCValue(char* key, decimal value) { this.key = key; this.value = &value; this.type = LCValueType.Decimal; }
        public LCValue(char* key, string value) { this.key = key; fixed (char* p = value) this.value = p; this.type = LCValueType.String; }
        // LC Value Type
        public LCValue(char* key, LCArray value) { this.key = key; this.value = &value; this.type = LCValueType.LCArray; }
        public LCValue(char* key, LCTable value) { this.key = key; this.value = &value; this.type = LCValueType.LCTable; }
        public LCValue(char* key, LCMap value) { this.key = key; this.value = &value; this.type = LCValueType.LCMap; }

        public T ReadValue<T>() where T : unmanaged => *(T*)value;

        public bool ReadBool() => *(bool*)value;
        public char ReadChar() => *(char*)value;
        public byte ReadByte() => *(byte*)value;
        public sbyte ReadSByte() => *(sbyte*)value;
        public short ReadInt16() => *(short*)value;
        public ushort ReadUInt16() => *(ushort*)value;
        public int ReadInt() => *(int*)value;
        public uint ReadUInt() => *(uint*)value;
        public long ReadInt64() => *(long*)value;
        public ulong ReadUInt64() => *(ulong*)value;
        public float ReadFloat() => *(float*)value;
        public double ReadDouble() => *(double*)value;
        public decimal ReadDecimal() => *(decimal*)value;
        public string ReadString() => new string((char*)value);
        // LC Value Type
        public LCArray ReadArray() => *(LCArray*)value;
        public LCTable ReadTable() => *(LCTable*)value;
        public LCMap ReadMap() => *(LCMap*)value;
    }

    public unsafe struct LCArray : ILCValue
    {
        public string Key => new string(key);
        public LCValueType Type => LCValueType.LCArray;
        public LCValueType ValuesType => valuesType;

        // Key - Same Type Values
        private char* key;
        private void* value;
        private LCValueType valuesType;

        public LCArray(char* key, void* value, LCValueType valuesType)
        {
            this.key = key;
            this.value = value;
            this.valuesType = valuesType;
        }
    }

    public unsafe struct LCTable : ILCValue
    {
        public string Key => new string(key);
        public LCValueType Type => LCValueType.LCTable;

        //NOTE: Mix Array
        // Key - AnyValue
        // Value Only
        private char* key;
        private LCValue* values;
    }

    public unsafe struct LCMap : ILCValue
    {
        public string Key => new string(key);
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
                keyType: LCValueTypeUtilities.GetLCType<TKey>(),
                valueType: LCValueTypeUtilities.GetLCType<TValue>());
        }
    }

    internal static class LCValueTypeUtilities
    {
        public static LCValueType GetLCType<T>()
        {
            var t = typeof(T);
            if (t.IsPrimitive)
            {
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
            if (t.IsPointer)
                return LCValueType.VoidPtr;
            // if (t.IsEnum)
            //     return LCValueType.Enum;
            // if (t.IsClass)
            //     return LCValueType.Unknown;
            return LCValueType.Unknown;
        }
    }
}
#nullable disable
