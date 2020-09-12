using System;
using System.Collections.Generic;
using System.Text;

[Test]
public unsafe static class HashTest
{
    public static void Run()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        var set = new HashSet<int>();
        var sb = new StringBuilder();
        string[] names = {
            "Asuma",
            "Chiyo",
            "Choji",
            "Deidara",
            "Gaara",
            "Han",
            "Hidan",
            "Hinata",
            "Hiruzen",
            "Ino",
            "Itachi",
            "Jiraiya",
            "JiraiyaSage",
            "Jugo",
            "Kakashi",
            "Kakuzu",
            "Kankuro",
            "Karasu",
            "Karin",
            "Kiba",
            "Kimimaro",
            "Kisame",
            "Konan",
            "Lee",
            "Minato",
            "Nagato",
            "Naruto",
            "NarutoRikudo",
            "NarutoSage",
            "Neji",
            "Orochimaru",
            "Roshi",
            "Pain",
            // AnimalPath,
            // AsuraPath,
            // DevaPath = Pain,
            "Sai",
            "Sakura",
            "Sanshouuo",
            "Sasuke",
            "SasukeImmortal",
            "Shikamaru",
            "Shino",
            "Suigetsu",
            "Tenten",
            "Tobi",
            "Tobirama",
            "Tsunade",
        };

        foreach (var name in names)
        {
            // var hash = Hash(name);
            var hash = name.ToHash();
            if (!set.Add(hash))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Duplicate hash code {hash} from `{name}`");
                Console.ForegroundColor = ConsoleColor.Cyan;
                continue;
            }

            sb.Append("Hash `")
              .Append(name)
              .Append("` : ")
              .Append(hash);

            Console.WriteLine(sb);
            sb.Clear();
            // Console.Write($"\t<==> Reverse: {hash.ReverseHash()}");
        }

        PrintAnsiCharacters();
    }

    private static void PrintAnsiCharacters()
    {
        // const string str = "abcdefghijklmnopqrstuvwxyz" +   // 65-90
        // "ABCDEFGHIJKLMNOPQRSTUVWXYZ";                   // 97-122
    }
}
