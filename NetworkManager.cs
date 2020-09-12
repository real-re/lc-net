using System;

namespace Ret
{
    [Test]
    public unsafe static class NetworkManager
    {
        public static void Test()
        {
            char* id = stackalloc[] { '7', '7', '7', '7', '7', '7', '7' };
            char* token = stackalloc[] { 't', 'o', 'k', 'e', 'n' };

            Send(id, token);
        }

        public static void Send(char* id, char* token)
        {
            Console.WriteLine($"ID: {new string(id)} Token: {new string(token)}");
        }
    }
}
