#nullable enable
using System;
using System.Data;
using System.Diagnostics;

namespace Re.LC
{
    internal static class LCDebug
    {
        internal enum LCDocType
        {
            // ErrorSyntax,
            Section,
            Key,
            Value,
            ValueOnly,
            ArrayValue,
            ArrayValues,
            EmptyArray,
            Commit,         // Start with `#`
            // Special Types
            InternalKey,    // Start with `@`
            // Features Types
            /** Tag:
             * example:
             *     # Core Settings of character
             *     [Core]
             *     name = Naruto
             *     # The tag feature must be define before key
             *     # Single Tag decleartion
             *     [Required] full = Uzumaki Naruto
             *     # Multi Tag decleartion
             *     [Required AllowedNullable] @callback_name = Uzumaki Naruto
             */
            Tag,            // With [ tag_name ]
        }

        internal enum LCDbgState
        {
            Begin_Array,
            End_Array,
            Begin_Inline_Array,
            End_Inline_Array,
            Begin_Map,
            End_Map,
        }

        internal static void Log(LCDbgState state)
        {
            var (begin, end) = state switch
            {
                LCDbgState.Begin_Array => ("[", "Begin Array"),
                LCDbgState.End_Array => ("]", "End Array"),
                LCDbgState.Begin_Inline_Array => ("[", "Begin Inline Array"),
                LCDbgState.End_Inline_Array => ("]", "End Inline Array"),
                LCDbgState.Begin_Map => ("{", "Begin Map"),
                LCDbgState.End_Map => ("}", "End Map"),
                _ => ("UNKNOWN SEP", "ERROR SYNTAX"),
            };
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(begin);
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.Write(" --> ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(end);
        }

        internal static void Log(Span<char> value, LCDocType type)
        {
            string name;
            (Console.ForegroundColor, name) = type switch
            {
                LCDocType.Section => (ConsoleColor.Blue, "SECTION"),
                LCDocType.Key => (ConsoleColor.Red, "KEY  "),
                LCDocType.Value => (ConsoleColor.Cyan, "VALUE"),
                LCDocType.ValueOnly => (ConsoleColor.Cyan, "VALUE-ONLY"),
                LCDocType.ArrayValue => (ConsoleColor.Cyan, "\tARRAY-VALUE"),
                LCDocType.ArrayValues => (ConsoleColor.Cyan, "\tARRAY-VALUES"),
                LCDocType.EmptyArray => (ConsoleColor.DarkRed, "\tEMPTY-ARRAY"),
                LCDocType.Commit => (ConsoleColor.DarkGray, "COMMIT"),
                LCDocType.InternalKey => (ConsoleColor.DarkMagenta, "INTERNAL-KEY"),
                LCDocType.Tag => (ConsoleColor.Green, "@TAG"),
                _ => (ConsoleColor.DarkRed, "UNKNOWN"),
            };
            Console.Write($"{name} : ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(value.ToString());
        }

        internal static void LogError(string value, LCDocType type)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            throw new SyntaxErrorException($"[ERROR] {value} Type: {type}");
        }

        internal static void LogSyntaxError(string? value)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            var frame = new StackTrace(1, true).GetFrame(0);
            if (frame == null)
                throw new SyntaxErrorException($"[Syntax ERROR] {value}");
            throw new SyntaxErrorException($"[Syntax ERROR] {value} in `{frame.GetFileName()}` Line: {frame.GetFileLineNumber()} Column: {frame.GetFileColumnNumber()}");
        }
    }
}
#nullable disable
