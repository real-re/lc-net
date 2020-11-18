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
            ArrayKey,
            Value,
            ValueOnly,
            ArrayValue,
            ArrayValueOnly,
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

        [Conditional("DEBUG")]
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
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(end);
        }

        [Conditional("DEBUG")]
        internal static void Log(Span<char> value, LCDocType type)
        {
            string name;
            (Console.ForegroundColor, name) = type switch
            {
                LCDocType.Section => (ConsoleColor.DarkBlue, "SECTION"),
                LCDocType.Key => (ConsoleColor.DarkRed, "KEY   "),
                LCDocType.ArrayKey => (ConsoleColor.DarkRed, "\tKEY   "),
                LCDocType.Value => (ConsoleColor.DarkCyan, "VALUE "),
                LCDocType.ValueOnly => (ConsoleColor.DarkCyan, "VALUE-ONLY"),
                LCDocType.ArrayValue => (ConsoleColor.DarkCyan, "\tVALUE "),
                LCDocType.ArrayValueOnly => (ConsoleColor.DarkCyan, "\tVALUE-ONLY"),
                LCDocType.ArrayValues => (ConsoleColor.DarkCyan, "\tARRAY-VALUES"),
                LCDocType.EmptyArray => (ConsoleColor.DarkRed, "\tEMPTY-ARRAY"),
                LCDocType.Commit => (ConsoleColor.DarkGray, "COMMIT"),
                LCDocType.InternalKey => (ConsoleColor.DarkMagenta, "INTERNAL-KEY"),
                LCDocType.Tag => (ConsoleColor.DarkGreen, "@TAG  "),
                _ => (ConsoleColor.Red, "UNKNOWN"),
            };
            Console.Write(name);
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write(": ");
            Console.ForegroundColor = type == LCDocType.Commit
                ? ConsoleColor.DarkGray
                : ConsoleColor.Blue;
            Console.WriteLine(value.ToString());
        }

        internal static void LogError(string value, LCDocType type)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            throw new SyntaxErrorException($"[ERROR] {value} Type: {type}");
        }

        internal static void LCSyntaxError(string? value)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            var frame = new StackTrace(1, true).GetFrame(0);
            if (frame is null)
                throw new SyntaxErrorException($"[Syntax ERROR] {value}");
            throw new SyntaxErrorException($"[Syntax ERROR] {value} in `{frame.GetFileName()}` Line: {frame.GetFileLineNumber()} Column: {frame.GetFileColumnNumber()}");
        }
    }
}
#nullable disable
