#nullable enable
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

            #region Main Loop

            LCParserContext ctx = new();

            NativeLinkedList<LCSection> sectionLList = new();
            NativeLinkedList<LCValue> kvLList = new(); // Items list of section

            Span<char> sectionName = default;
            Span<char> key = default;
            NativeLinkedList<LCValue> arr = default;

            int start = 0; // Block start position
            int inlineDepth = 0; // Inline array counter
            bool hasEOF = data[^1] is '\n' or ' ' or '\t' or '\r' or '\0';
            int length = hasEOF ? data.Length : data.Length + 1;

            char* pData;
            fixed (char* ptr = &data.GetPinnableReference())
                pData = ptr;

            ctx.OnLCParserStart?.Invoke();
            for (int i = 0; i < length; i++)
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
                                        AddArrayItem(ref arr, new LCValue(key, value));
                                        key = default;
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

                                        AddArrayItem(ref arr, new LCValue(default, value));
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
                            LogSyntaxError("Not found key");

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
                            AddArray(ref arr, ref key);

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

                                LogSyntaxError($"Cannot has char `{data[i]}`");
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
                                EndOfArray(ref arr);
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

            #endregion

            #region Utilities

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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void AddArray(ref NativeLinkedList<LCValue> arr, ref Span<char> key)
            {
                if (!arr.IsEmpty)
                {
                    LogSyntaxError("Found not empty array before Key-Array");
                }

                if (key.IsEmpty) // Inline array
                {
                }
                else
                {
                    kvLList.AddAfter(new LCValue(key));
                    key = default;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void AddArrayItem(ref NativeLinkedList<LCValue> arr, LCValue item)
            {
                arr.AddAfter(item);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void EndOfArray(ref NativeLinkedList<LCValue> arr)
            {
                var arrItem = kvLList.Peek();
                if (arrItem.Key is null)
                {
                    LogSyntaxError("Found null array key");
                }

                arrItem.SetValue(arr.ToSpan());
                kvLList.SetValue(kvLList.Count - 1, arrItem);
                arr = default;

                Log(DbgState.End_Array);
            }

            #endregion
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
        public bool isMultipleLineArray;
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

        public Span<LCSection>.Enumerator GetEnumerator() => new Span<LCSection>.Enumerator();
    }
}
#nullable disable
