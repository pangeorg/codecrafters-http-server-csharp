using System.Text;

public static class StringExtensions {
    public static byte[] ToAsciiBytes(this string value) => Encoding.ASCII.GetBytes(value);
}

