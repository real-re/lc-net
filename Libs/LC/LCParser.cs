#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Re.Collections.Native;
using static Re.LC.LCDebug;

namespace Re.LC
{
    public unsafe static class LCParser
    {
        public static LC? From(string filePath)
        {
            // throw new FileNotFoundException(filePath);
            if (!File.Exists(filePath))
            {
                Debug.WriteLine($"File {filePath} not found!");
                return null;
            }

            using var stream = File.OpenText(filePath);
            Span<char> buffer = stackalloc char[(int)stream.BaseStream.Length];
            stream.ReadBlock(buffer);

            if (buffer.IsEmpty)
                throw new NullReferenceException();

            // return Parse(lines);
            return Parse(buffer);
        }

        public static LC? FromString(string str)
        {
            if (string.IsNullOrEmpty(str))
                return null;

            fixed (char* ptr = str)
                return Parse(new Span<char>(ptr, str.Length * 2));
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

        public static LC? Parse(Span<char> data, bool isTrimed = true)
        {
            if (data.IsEmpty)
                return null;

            //TODO: Move Trim Function to Settings
            if (isTrimed)
                data = data.Trim();

            #region Main Parser

            var ctx = new LCParserContext();
            var section = new LCSection();

            using var sectionLList = new NativeLinkedList<LCSection>(section);
            using var kvLList = new NativeLinkedList<LCValue>();

            int start = 0; // Block start position
            int inlineDepth = 0;
            int length = data.Length;

            ctx.OnLCParserStart?.Invoke();
            for (int i = length - 1; i >= 0; i--)
            {
                switch (data[i])
                {
                    case '\0':
                        break;
                    case '#': // Commit
                        start = i;
                        // Jump To Next Line Start Position
                        while (++i < length)
                        {
                            if (data[i] == '\n')
                                break;
                        }

                        Log(data[start..i], LCDocType.Commit);
                        start = i + 1;
                        break;
                    case '\n':
                        if (ctx.isArray)
                            continue;
                        if (ctx.isValue)
                        {
                            ctx.isValue = false;
                            if (ctx.hasPairKey)
                            {
                                ctx.hasPairKey = false;
                                Log(data[start..i].Trim(), LCDocType.Value);
                            }
                            else
                            {
                                Log(data[start..i].Trim(), LCDocType.ValueOnly);
                            }
                        }
                        start = i + 1;
                        break;
                    case '=':
                        ctx.isValue = true;
                        ctx.hasPairKey = true;
                        ctx.endOfKey = true;
                        if (start == i)
                            LogError("NOT FOUND KEY", LCDocType.Key);

                        // Add Key
                        // ctx.AddKey(data.Slice(start, len));
                        Log(data[start..i].Trim(), LCDocType.Key);

                        // ctx.isValueStart = true;
                        start = i + 1;
                        // ctx.isValue = true;
                        break;
                    case '{':
                        ctx.isMap = true;
                        break;
                    case '}':
                        ctx.endOfMap = true;
                        break;
                    case '[':
                        if (ctx.inlineValue)
                        {
                            inlineDepth++;
                        }
                        else if (ctx.hasPairKey)
                        {
                            ctx.isArray = true;

                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("[ -- BEGIN Array");
                        }
                        else if (ctx.isArray)
                        {
                            ctx.inlineValue = true;
                        }
                        else
                        {
                            ctx.isSection = true;
                        }
                        start = i + 1;
                        break;
                    case ']':
                        if (ctx.isSection)
                        {
                            ctx.isSection = false;
                            section = new LCSection(data[start..i].Trim());
                            Log(data[start..i].Trim(), LCDocType.Section);
                        }
                        else if (ctx.inlineValue)
                        {
                            if (--inlineDepth == 0)
                            {
                                ctx.inlineValue = false;
                                inlineDepth = 0;
                            }
                            // Wait for next close `]`
                        }
                        else if (ctx.isArray)
                        {
                            ctx.isArray = false;

                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("] -- Array EOF");
                        }

                        while (++i < length)
                        {
                            if (data[i] == '\n')
                                break;
                            else if (data[i] != ' ' || data[i] != '\t' || data[i] != '\r')
                                LogError("Syntax", LCDocType.Value);
                        }
                        start = i + 1;
                        // if (ctx.isNewSection)
                        //     ctx.isNewSection = false;
                        // else
                        //     ctx.isEndOfArray = true;
                        break;
                    // case ' ' or '\t' or '\r':
                    //     if (start == i)
                    //         start++;
                    //     break;
                    default:
                        // Add Value
                        // ctx.AddValue(data.Slice(start, len), LCValueType.$Type);
                        // if (ctx.EndValue)
                        //     ctx.BeginKey = true;
                        break;
                }
            }
            ctx.OnLCParserEOF?.Invoke();

            #endregion

            return new LC();
        }
    }

    // [StructLayout(LayoutKind.Explicit)]
    internal ref struct LCParserContext
    {
        // Section
        public bool isSection;
        public bool endOfSection;
        // Key
        public bool isKey;
        public bool endOfKey;
        public bool hasPairKey;
        // Value
        public bool isValue;
        public bool endOfValue;
        public bool inlineValue;
        // Array Value
        public bool isArray;
        public bool endOfArray;
        // Map Value
        public bool isMap;
        public bool endOfMap;

        public Action? OnLCParserStart;
        public Action? OnLCParserEOF;
    }

    public unsafe ref struct LCData
    {
        public Span<char> Name { get; }
        public Span<LCSection> Sections { get; }
        public bool HasEmptySection { get; }

        public LCData(Span<char> name, Span<LCSection> sections, bool hasEmptySection)
        {
            this.Name = name;
            this.Sections = sections;
            this.HasEmptySection = !sections.IsEmpty && hasEmptySection && sections[0].Name == null;
        }

        public override string? ToString()
        {
            if (Sections.IsEmpty)
                return $"Section: {(Name.IsEmpty ? "null" : Name.ToString())}, null";

            var builder = new StringBuilder();

            foreach (var section in Sections)
            {
                builder.Append("Section: ").Append(section.Name ?? "____ROOT").Append(": [ ")
                       .Append(section.IsEmpty ? "null" : section.ToString()).AppendLine(" ]");
            }

            var result = builder.ToString();
            builder.Clear();
            return result;
        }
    }

    public unsafe struct LCSection
    {
        private char* name;
        private char** keys;
        private LCValue* values;
        private int length;
        private int nameLen;

        //TODO: Custom string intern pool
        public string? Name => name == null ? null : new string(name);
        public int Count => length;
        public bool IsEmpty => length == 0 && keys == null && values == null;

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

        public LCSection(Span<char> name)
        {
            if (name.IsEmpty)
            {
                this.name = null;
            }
            else
            {
                fixed (char* p = &name.GetPinnableReference())
                    this.name = p;
            }
            this.nameLen = name.Length;
            this.keys = null;
            this.values = null;
            this.length = 0;
        }

        public override string? ToString()
        {
            if (IsEmpty)
                return null;

            return null;
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
        // Reference Type (Will As Span<char>)
        String,
        // LC Value Type
        LCArray,
        LCTable,
        LCMap
    }

    public interface ILCValue
    {
        LCValueType Type { get; }
        string Key { get; }
    }

    /// <summary>
    /// Could inclued key-value or value-only
    /// </summary>
    public unsafe struct LCValue : ILCValue
    {
        public string Key => new string(key);
        public LCValueType Type => type;

        private char* key;
        private void* value;
        private LCValueType type;

        #region Key-Value

        public LCValue(char* key) { this.key = key; this.value = null; this.type = LCValueType.Unknown; }
        public LCValue(char* key, void* value) { this.key = key; this.value = value; this.type = LCValueType.VoidPtr; }
        public LCValue(char* key, void* value, LCValueType type) { this.key = key; this.value = value; this.type = type; }
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

        #endregion

        #region Value-Only

        public LCValue(void* value) { this.key = null; this.value = value; this.type = LCValueType.VoidPtr; }
        public LCValue(void* value, LCValueType type) { this.key = null; this.value = value; this.type = type; }
        public LCValue(bool value) { this.key = null; this.value = &value; this.type = LCValueType.Bool; }
        public LCValue(char value) { this.key = null; this.value = &value; this.type = LCValueType.Char; }
        public LCValue(byte value) { this.key = null; this.value = &value; this.type = LCValueType.Byte; }
        public LCValue(sbyte value) { this.key = null; this.value = &value; this.type = LCValueType.SByte; }
        public LCValue(short value) { this.key = null; this.value = &value; this.type = LCValueType.Int16; }
        public LCValue(ushort value) { this.key = null; this.value = &value; this.type = LCValueType.UInt16; }
        public LCValue(int value) { this.key = null; this.value = &value; this.type = LCValueType.Int; }
        public LCValue(uint value) { this.key = null; this.value = &value; this.type = LCValueType.UInt; }
        public LCValue(long value) { this.key = null; this.value = &value; this.type = LCValueType.Int64; }
        public LCValue(ulong value) { this.key = null; this.value = &value; this.type = LCValueType.UInt64; }
        public LCValue(float value) { this.key = null; this.value = &value; this.type = LCValueType.Float; }
        public LCValue(double value) { this.key = null; this.value = &value; this.type = LCValueType.Double; }
        public LCValue(decimal value) { this.key = null; this.value = &value; this.type = LCValueType.Decimal; }
        public LCValue(string value) { this.key = null; fixed (char* p = value) this.value = p; this.type = LCValueType.String; }
        // LC Value Type
        public LCValue(LCArray value) { this.key = null; this.value = &value; this.type = LCValueType.LCArray; }
        public LCValue(LCTable value) { this.key = null; this.value = &value; this.type = LCValueType.LCTable; }
        public LCValue(LCMap value) { this.key = null; this.value = &value; this.type = LCValueType.LCMap; }

        #endregion

        #region Read Value Methods

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

        #endregion
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
        // Key-Value:
        //   key_value_only_array = [
        //     name   = naruto
        //     name_1 = Naruto
        //     name_2 = Uzumaki Naruto
        //   ]
        //
        // Value Only:
        //     value-only-array = [
        // 
        //     ]
        //
        // Mix-Value:
        //     mix array = [
        //     
        //     ]
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
