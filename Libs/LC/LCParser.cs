#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Re.Collections.Native;
using Re.LC.Utilities;
using static Re.LC.LCDebug;
using DbgState = Re.LC.LCDebug.LCDbgState;

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
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[WARN] Loaded a empty file `{filePath}`!");
                return null;
            }
            return Parse(buffer);
        }

        public static LC? Parse(string str)
        {
            if (string.IsNullOrEmpty(str))
                return null;

            fixed (char* ptr = str)
                return Parse(new Span<char>(ptr, str.Length * 2));
        }

        public static LC? Parse(char[] lc)
        {
            if (lc == null || lc.Length == 0)
                return null;

            fixed (char* ptr = lc)
                return Parse(new Span<char>(ptr, lc.Length));
        }

        public static LC? Parse(byte[] lc)
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
            using var splitResults = new SpanLinkedList(data);

            int start = 0; // Block start position
            int inlineDepth = 0;
            int length = data.Length;
            // char* p; //TODO: Use pointer insted of Span<char>

            // fixed (char* cPtr = &data.GetPinnableReference())
            //     p = cPtr;

            ctx.OnLCParserStart?.Invoke();
            for (int i = 0; i < length; i++)
            {
            Head:
                switch (data[i])
                {
                    case '#': // Commit
                        start = i;
                        // Jump To Next Line Start Position
                        while (++i < length && data[i] != '\n') ;

                        Log(data[start..i], LCDocType.Commit);
                        start = i + 1;
                        break;
                    case '\n':
                        if (ctx.isArray)
                        {
                            // AppendArrayValue();
                            // Print array value
                            if (start != i) //NOTE: 防止右侧只有括号，而输出空值
                            {
                                var value = data[start..i].Trim();
                                if (!value.IsEmpty)
                                {
                                    if (ctx.hasKey)
                                    {
                                        ctx.hasKey = false;
                                        Log(value, LCDocType.Value);
                                    }
                                    else
                                    {
                                        Log(value, LCDocType.ArrayValue);
                                    }
                                }
                            }

                            while (++i < length && (data[i] is '\t' or ' ' or '\r')) ;
                            start = i;
                            goto Head;
                        }
                        else if (ctx.isValue)
                        {
                            if (start != i) //NOTE: 防止右侧只有括号，而输出空值
                            {
                                var value = data[start..i].Trim();
                                if (!value.IsEmpty)
                                {
                                    ctx.isValue = false;
                                    if (ctx.hasKey)
                                    {
                                        ctx.hasKey = false;
                                        Log(data[start..i].Trim(), LCDocType.Value);
                                    }
                                    else
                                    {
                                        Log(data[start..i].Trim(), LCDocType.ValueOnly);
                                    }
                                }
                            }
                            start = ++i;
                            goto Head;
                        }
                        break;
                    case '=':
                        ctx.isValue = true;
                        ctx.hasKey = true;
                        if (start == i)
                            LogError("NOT FOUND KEY", LCDocType.Key);

                        // Add Key
                        // ctx.AddKey(data[start, i]);
                        Log(data[start..i].Trim(), ctx.isArray ? LCDocType.ArrayKey : LCDocType.Key);

                        // ctx.isValueStart = true;
                        start = ++i;
                        // ctx.isValue = true;
                        goto Head;
                    case '{':
                        ctx.isMap = true;
                        Log(DbgState.Begin_Map);
                        break;
                    case '}':
                        ctx.isMap = false;
                        Log(DbgState.End_Map);
                        break;
                    case '[':
                        if (ctx.inlineArray)
                        {
                            inlineDepth++;
                            // TODO: Check Syntax Error
                        }
                        else if (ctx.hasKey)
                        {
                            // TODO: Check Value is not in the same line with Key
                            ctx.isArray = true;
                            //NOTE: That key already pair to this array
                            ctx.hasKey = false;
                            //TODO: For Now, Only Support Single-Line Array
                            ctx.isSingleLineArray = true;

                            Log(DbgState.Begin_Array);
                        }
                        else if (ctx.isArray)
                        {
                            // if (arrays[inlineDepth].Line == arrays[0].Line)
                            //     ctx.inlineArray = true;
                        }
                        else
                        {
                            ctx.isSection = true;
                        }

                        start = ++i;
                        goto Head;
                    case ']':
                        if (ctx.isSection)
                        {
                            ctx.isSection = false;
                            section = new LCSection(data[start..i].Trim());

                            Log(data[start..i].Trim(), LCDocType.Section);

                            while (++i < length)
                            {
                                if (data[i] is ' ' or '\t' or '\r')
                                    continue;
                                if (data[i] == '\n')
                                    break;

                                LogSyntaxError($"Cannot has char `{data[i]}`");
                            }

                            if (i >= length)
                                goto End;

                            start = i;
                            break;
                        }
                        if (ctx.isArray)
                        {
                            if (ctx.isSingleLineArray)
                            {
                                ctx.isSingleLineArray = false;

                                if (start < i - 1)
                                {
                                    // Is Single Line Array
                                    var value = data[start..(i - 1)];
                                    value.Split(' ', splitResults);
                                    // Is Multi-Line Array

                                    // is array values (multiple)
                                    if (splitResults.Count > 0)
                                    {
                                        foreach (var v in splitResults)
                                            Log(v, LCDocType.ArrayValues);
                                        splitResults.Clear();
                                    }
                                    else // is array values (only one)
                                    {
                                        Log(value.Trim(), LCDocType.ArrayValue);
                                    }
                                }
                                // Is Multi-Line Array
                                Log(DbgState.End_Array);
                            }
                            if (ctx.inlineArray)
                            {
                                if (inlineDepth > 0)
                                {
                                    ctx.inlineArray = false;
                                    --inlineDepth;

                                    Log(DbgState.End_Inline_Array);
                                }
                                // Wait for next close `]`
                            }
                            else
                            {
                                ctx.isArray = false;
                            }
                        }
                        {
                            int index = i + 1;
                            while (index < length)
                            {
                                if (data[index] is not '\n' or ' ' or '\t' or '\r') break;
                                if (data[index] == '[') goto case '[';
                                if (data[index] == ']') goto case ']';
                                if (data[index] == '{') goto case '{';
                                if (data[index] == '}') goto case '}';

                                i = index++;
                                // LogSyntaxError($"Cannot has char `{data[i]}`");
                            }
                            start = i;
                        }
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
        End:
            ctx.OnLCParserEOF?.Invoke();

            #endregion

            return new LC();
        }
    }

    internal ref struct LCParserContext
    {
        // Section
        public bool isSection;
        // Key
        public bool hasKey;
        // Value
        public bool isValue;
        public bool inlineArray;
        // Array Value
        public bool isArray; // Multi-Line Array
        public bool isSingleLineArray;
        // Map Value
        public bool isMap;

        public Action? OnLCParserStart;
        public Action? OnLCParserEOF;
    }

    public unsafe ref struct LCData
    {
        public Span<char> Name { get; set; }
        public Span<LCSection> Sections { get; }
        public bool HasTopLevelSection { get; }

        public LCData(Span<char> name, Span<LCSection> sections, bool hasEmptySection)
        {
            this.Name = name;
            this.Sections = sections;
            this.HasTopLevelSection = !sections.IsEmpty && hasEmptySection && sections[0].Name == null;
        }

        public override string? ToString()
        {
            if (Sections.IsEmpty)
                return $"Section: {(Name.IsEmpty ? "null" : Name.ToString())}, null";

            var builder = new StringBuilder();

            foreach (var section in Sections)
            {
                builder.Append("Section: ").Append(section.Name ?? "__ROOT__").Append(": [ ")
                       .Append(section.IsEmpty ? "null" : section.ToString()).AppendLine(" ]");
            }

            var result = builder.ToString();
            builder.Clear();
            return result;
        }
    }

    public unsafe struct LCSection
    {
        public string? Name => name == null ? null : new string(name);
        public int Count => length;
        public bool IsEmpty => length == 0 && values == null;

        private char* name;
        private LCValue* values;
        private int length;

        public LCValue this[int index]
        {
            get
            {
                if (values == null)
                    throw new NullReferenceException();

                if (index < 0 || index >= length)
                    throw new IndexOutOfRangeException();

                return values[index];
            }
        }

        public LCSection(Span<char> name)
        {
            fixed (char* p = &name.GetPinnableReference())
                this.name = name.IsEmpty ? null : p;
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
                if (t == typeof(LCArray))
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
            if (t.IsClass)
            {
                if (t == typeof(string))
                    return LCValueType.String;
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
