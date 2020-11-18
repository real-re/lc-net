#nullable enable
using System.Collections.Specialized;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Re.Collections.Native;
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
                return Parse(new Span<char>(ptr, str.Length));
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

        public static LC Parse(Span<char> data, string? path = null, bool isTrimed = false)
        {
            if (data.IsEmpty)
                return LC.Empty;

            //TODO: Move Trim Function to Settings
            if (isTrimed)
                data = data.Trim();

            LCParserContext ctx = new();
            ctx.data = data;

            ref NativeLinkedList<LCSection> sectionLList = ref ctx.sectionLList;
            ref NativeLinkedList<LCValue> kvLList = ref ctx.kvLList; // Items list of section

            ref Span<char> sectionName = ref ctx.sectionName;
            ref Span<char> key = ref ctx.key;
            ref NativeLinkedList<LCValue> arr = ref ctx.arr;

            ref int i = ref ctx.index;
            ref int start = ref ctx.start; // Block start position
            int inlineDepth = 0; // Inline array counter
            bool hasEOF = data[^1] is '\n' or ' ' or '\t' or '\r' or '\0';
            int length = hasEOF ? data.Length : data.Length + 1;

            char* pData;
            fixed (char* ptr = &data.GetPinnableReference())
                pData = ptr;

            ctx.OnLCParserStart?.Invoke();
            for (i = 0; i < length; i++)
            {
            Head:
                switch (pData[i])
                {
                    case '#': // Commit
                        start = i;
                        // Jump To Next Line Start Position
                        while (++i < length && data[i] != '\n') ;

                        Log(data[start..i], LCDocType.Commit);
                        start = i + 1;
                        break;
                    case '\n' or ' ' or '\t' or '\r' or '\0':
                        if (ctx.isArray)
                        {
                            // Print array value
                            if (start != i) //NOTE: 防止右侧只有括号，而输出空值
                            {
                                var value = data[start..i].Trim();
                                if (!value.IsEmpty)
                                {
                                    if (ctx.hasKey)
                                    {
                                        ctx.hasKey = false;
                                        ctx.PushArrayItem(new LCValue(key, value));
                                        Log(value, LCDocType.ArrayValue);
                                    }
                                    else
                                    {
                                        // Check Next Flag Is '=' (Has Key Flag)
                                        while (++i < length)
                                        {
                                            if (data[i] is '\t' or ' ' or '\n' or '\r') continue;
                                            if (data[i] is '=') goto case '=';
                                            break;
                                        }

                                        ctx.PushArrayValue(value);
                                        Log(value, LCDocType.ArrayValueOnly);
                                        start = i;
                                        goto Head;
                                    }
                                }
                            }

                            while (++i < length)
                            {
                                if (data[i] is '\t' or ' ' or '\n' or '\r') continue;
                                if (data[i] is '=') goto case '=';
                                break;
                            }
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
                                        ctx.PushKeyValue(value);
                                        Log(value, LCDocType.Value);
                                    }
                                    else
                                    {
                                        LCSyntaxError($"ValueOnly [ {value.ToString()} ]");
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
                            LCSyntaxError("Not found key");

                        // Add Key
                        if (!key.IsEmpty)
                        {
                            LCSyntaxError($"Find tow key [{key.ToString()}] [{data[start..i].Trim().ToString()}]");
                        }
                        else
                        {
                            ctx.PushKey();
                            Log(key, ctx.isArray ? LCDocType.ArrayKey : LCDocType.Key);
                        }
                        start = ++i;
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
                            //TODO: For Now, Only Support Multiple-Line Array
                            ctx.isMultipleLineArray = true;
                            ctx.PushArrayWithKey();

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
                                sectionName.Clear();
                                kvLList.Clear();
                            }
                            sectionName = data[start..i].Trim(); //NOTE: Section name can be empty

                            Log(sectionName, LCDocType.Section);

                            while (++i < length)
                            {
                                if (data[i] is ' ' or '\t' or '\r')
                                    continue;
                                if (data[i] == '\n')
                                    break;

                                LCSyntaxError($"Cannot has char `{data[i]}`");
                            }

                            if (i >= length)
                                goto End;

                            start = i;
                            break;
                        }
                        if (ctx.isArray)
                        {
                            if (ctx.isValue)
                            {
                                //TODO: Check if there has an array key or didn't add an array
                                // then try as value as in-array value-only
                                // if failed then syntax error
                                // if(CheckHasArrayKey())
                                //
                                // else
                                //
                                ctx.isValue = false;
                            }
                            if (ctx.isMultipleLineArray)
                            {
                                ctx.isMultipleLineArray = false;
                                ctx.EndOfArray();
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
                }
            }
        End:
            ctx.OnLCParserEOF?.Invoke();
            // EOF: '\n', ' ', '\t', '\r', '\0'

            // [ Check if has not save data ]
            // NOTE: Before start parse data, first check if not found EOF [see EOF] at last
            // then add a EOF char at last or array count plus one
            // (When pointer array index out of range will get '\0')

            // Flush last sectoin data
            if (!sectionName.IsEmpty && kvLList.Count > 0)
                sectionLList.AddAfter(new LCSection(sectionName, kvLList.ToSpan()));
            LC result = new(new LCData(Span<char>.Empty, path, sectionLList.ToSpan()));

            // Clear unmanaged memory
            sectionLList.Dispose();
            kvLList.Dispose();
            arr.Dispose();

            return result;
        }
    }

    internal ref struct LCParserContext
    {
        public int index;
        public int start;
        public Span<char> data;

        // Section
        public bool isSection;
        // Key
        public bool hasKey;
        // Value
        public bool isValue;
        public bool inlineArray;
        // Array Value
        public bool isArray; // Multi-Line Array
        public bool isMultipleLineArray;
        // Map Value
        public bool isMap;

        public NativeLinkedList<LCSection> sectionLList;
        public NativeLinkedList<LCValue> kvLList; // Items list of section

        public Span<char> sectionName;
        public Span<char> key;

        public NativeLinkedList<LCValue> arr;

        public Action? OnLCParserStart;
        public Action? OnLCParserEOF;

        public LCParserContext(NativeLinkedList<LCSection> sectionLList,
                               NativeLinkedList<LCValue> kvLList,
                               Span<char> sectionName,
                               Span<char> key,
                               NativeLinkedList<LCValue> arr,
                               Action? onLCParserStart = null,
                               Action? onLCParserEOF = null)
        {
            this.index = 0;
            this.start = 0;
            this.data = default;

            this.isSection = false;
            this.hasKey = false;
            this.isValue = false;
            this.inlineArray = false;
            this.isArray = false;
            this.isMultipleLineArray = false;
            this.isMap = false;

            this.sectionLList = sectionLList;
            this.kvLList = kvLList;
            this.sectionName = sectionName;
            this.key = key;
            this.arr = arr;

            this.OnLCParserStart = onLCParserStart;
            this.OnLCParserEOF = onLCParserEOF;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushKey()
        {
            this.key = data[start..index].Trim();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushKey(Span<char> key)
        {
            this.key = key;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushKeyValue(Span<char> value)
        {
            PushKeyValue(this.key, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushKeyValue(Span<char> key, Span<char> value)
        {
            if (key.IsEmpty)
            {
                LogError("Empty key", LCDocType.Key);
                return;
            }

            kvLList.AddAfter(new LCValue(key, value));
            this.key = default;
        }

        // TODO: 支持多重嵌套数组、内联数组
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushArrayWithKey()
        {
            if (!arr.IsEmpty)
            {
                LCSyntaxError("Found not empty array before Key-Array");
            }

            if (key.IsEmpty) // Inline array
            {
                // if parent node is Array and has key then matched Inline Array
                //     PushInlineArray();
                // else
                //     throw "Not found key of array"
                //           "Not match inline array" 
            }
            else
            {
                kvLList.AddAfter(new LCValue(key));
                key = default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushInlineArray()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushArrayItem(LCValue item)
        {
            arr.AddAfter(item);
            key = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushArrayValue(Span<char> value)
        {
            arr.AddAfter(new LCValue(Span<char>.Empty, value));
            key = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EndOfArray()
        {
            var arrItem = kvLList.Peek();
            if (arrItem.Key is null)
            {
                LCSyntaxError("Found null array key");
            }

            arrItem.SetValue(arr.ToSpan());
            kvLList.SetValue(kvLList.Count - 1, arrItem);
            arr = default;

            Log(DbgState.End_Array);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EndOfInlineArray()
        {
        }
    }

    public unsafe ref struct LCData
    {
        public Span<char> Name { get; set; }
        public string? Path { get; set; }
        public Span<LCSection> Sections { get; }
        public bool HasTopLevelSection { get; }

        public LCData(Span<LCSection> sections) : this(Span<char>.Empty, null, sections) { }

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

        public Span<LCSection>.Enumerator GetEnumerator()
        {
            return Sections.GetEnumerator();
        }
    }
}
#nullable disable
