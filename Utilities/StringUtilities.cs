
public unsafe static class StringUtilities
{
    public static byte* ToBytes(this string source)
    {
        fixed (char* str = source)
        {
            byte* ptr = stackalloc byte[source.Length * 2];
            byte* bstr = (byte*)str;
            for (int i = 0; i < source.Length * 2; i++)
                ptr[i++] = bstr[i];
            return ptr;
        }
    }

    public static int ToHash(this string source)
    {
        int result = 7777;
        fixed (char* ptr = source)
        {
            byte* bstr = (byte*)ptr;
            for (int i = 0; i < source.Length * 2; i++)
            {
                result = (result << 5) + result + *bstr;
                ++bstr;
            }

            return result;
        }
    }
}
