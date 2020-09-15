using System;
using System.Data;

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

        internal static void Log(Span<char> value, LCDocType type)
        {
            string name;
            (Console.ForegroundColor, name) = type switch
            {
                LCDocType.Section => (ConsoleColor.Blue, "SECTION"),
                LCDocType.Key => (ConsoleColor.Red, "KEY"),
                LCDocType.Value => (ConsoleColor.Cyan, "VALUE"),
                LCDocType.ValueOnly => (ConsoleColor.Cyan, "VALUE-ONLY"),
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
    }
}