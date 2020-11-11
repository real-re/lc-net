#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Re.Collections.Native;
using Re.LC.Utilities;
using static Re.LC.LCDebug;
using DbgState = Re.LC.LCDebug.LCDbgState;

namespace Re.LC
{
    public unsafe static class LCParser
    {
        public static LC From(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return LC.Empty;
            }

            // throw new FileNotFoundException(filePath);
            if (!File.Exists(filePath))
            {
                Debug.WriteLine($"File {filePath} not found!");
                return LC.Empty;
            }

            using var stream = File.OpenText(filePath);
            Span<char> buffer = new char[(int)stream.BaseStream.Length];
            stream.ReadBlock(buffer);

            if (buffer.IsEmpty)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[WARN] Loaded a empty file `{filePath}`!");
                return LC.Empty;
            }
            return Parse(buffer, filePath);
        }

        public static LC Parse(string str)
        {
            if (string.IsNullOrEmpty(str))
                return LC.Empty;

            fixed (char* ptr = str)
                return Parse(new Span<char>(ptr, str.Length * 2));
        }

        public static LC Parse(char[] lc)
        {
            if (lc is null || lc.Length == 0)
                return LC.Empty;

            fixed (char* ptr = lc)
                return Parse(new Span<char>(ptr, lc.Length));
        }

        public static LC Parse(byte[] lc)
        {
            if (lc is null || lc.Length == 0)
                return LC.Empty;

            fixed (byte* ptr = lc)
                return Parse(new Span<char>(ptr, lc.Length));
        }

        public static LC Parse(Span<char> data, string? path = null, bool isTrimed = true)
        {
            if (data.IsEmpty)
                return LC.Empty;

            //TODO: Move Trim Function to Settings
            if (isTrimed)
                data = data.Trim();

            #region Main Parser

            LCParserContext ctx = new();

            using NativeLinkedList<LCSection> sectionLList = new();
            using NativeLinkedList<LCValue> kvLList = new();
            using SpanLinkedList splitResults = new(data);

            Span<char> sectionName = default;
            Span<char> key = default;

            int start = 0; // Block start position
            int inlineDepth = 0;
            int length = data.Length;

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
                                        AddKeyValue(ref key, value);
                                        Log(value, LCDocType.Value);
                                    }
                                    else
                                    {
                                        //TODO: Appent value to array
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
                                        AddKeyValue(ref key, value);
                                        Log(value, LCDocType.Value);
                                    }
                                    else
                                    {
                                        LogSyntaxError($"ValueOnly [ {value.ToString()} ]");
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
                        if (!key.IsEmpty)
                        {
                            LogSyntaxError($"Find tow key [{key.ToString()}] [{data[start..i].Trim().ToString()}]");
                        }
                        else
                        {
                            key = data[start..i].Trim();
                            Log(key, ctx.isArray ? LCDocType.ArrayKey : LCDocType.Key);
                        }
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
                            if (kvLList.Count > 0)
                            {
                                sectionLList.AddAfter(new LCSection(sectionName, kvLList.ToSpan()));
                                kvLList.Clear();
                            }
                            sectionName = data[start..i].Trim();

                            Log(sectionName, LCDocType.Section);

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

            return new LC(new LCData(Span<char>.Empty, path, sectionLList.ToSpan()));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void AddKeyValue(ref Span<char> key, Span<char> value)
            {
                if (key.IsEmpty)
                {
                    LogError("Empty key", LCDocType.Key);
                    return;
                }

                kvLList.AddAfter(new LCValue(key, value));
                key = default;
            }
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
        public string? Path { get; set; }
        public Span<LCSection> Sections { get; }
        public bool HasTopLevelSection { get; }

        public LCData(Span<char> name, string? path, Span<LCSection> sections)
        {
            this.Name = name;
            this.Path = path;
            this.Sections = sections;
            this.HasTopLevelSection = !sections.IsEmpty && sections[0].Name is null;
        }

        public override string? ToString()
        {
            if (Sections.IsEmpty)
                return $"Section: {(Name.IsEmpty ? "null" : Name.ToString())}, null";

            var builder = new StringBuilder();

            foreach (var section in Sections)
                builder.AppendLine(section.ToString());

            return builder.ToString();
        }
    }

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

        public LCSection(Span<char> name, Span<LCValue> kv)
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
    public unsafe struct LCValue : ILCValue
    {
        public string? Key => new string(key, 0, keyLength);
        public LCValueType Type => type;

        private char* key;
        private int keyLength;
        private void* value;
        private int valueLength;
        private LCValueType type;

        #region Key-Value

        public LCValue(Span<char> key, Span<char> value)
        {
            fixed (char* kPtr = &key.GetPinnableReference()) this.key = kPtr; this.keyLength = key.Length;
            fixed (char* vPtr = &value.GetPinnableReference()) this.value = vPtr; this.valueLength = value.Length;
            this.type = LCValueType.String;
        }
        // public LCValue(Span<char> key, LCArray value)
        // {
        //     fixed (char* kPtr = &key.GetPinnableReference()) this.key = kPtr; this.keyLength = key.Length;
        //     this.value = value; this.valueLength = 0;
        //     this.type = LCValueType.LCArray;
        // }
        // public LCValue(Span<char> key, LCMap value)
        // {
        //     fixed (char* kPtr = &key.GetPinnableReference()) this.key = kPtr; this.keyLength = key.Length;
        //     this.value = value; this.valueLength = 0;
        //     this.type = LCValueType.LCMap;
        // }

        #endregion

        #region Value-Only

        // public LCValue(Span<char> value, LCValueType type) { this.key = null; this.value = value; this.type = type; }
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
        // public LCArray ReadArray() => *(LCArray*)value;
        // public LCMap ReadMap() => *(LCMap*)value;

        #endregion

        public override string ToString()
        {
            StringBuilder builder = new();
            builder.Append("Key: ")
                    .Append(' ')
                    .Append(Key)
                    .Append("\tValue: ");
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
                LCValueType.LCArray => builder.Append(ReadValue<LCArray>().ToString()),
                LCValueType.LCMap => builder.Append(ReadValue<LCMap>().ToString()),
                _ => builder.Append("[ Unknown Value Type ]"),
            }).AppendLine();
            return builder.ToString();
        }
    }

    public unsafe struct LCArray : ILCValue
    {
        public string? Key => new string(key);
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
            if (t == typeof(string))
                return LCValueType.String;
            return LCValueType.Unknown;
        }
    }
}
#nullable disable
