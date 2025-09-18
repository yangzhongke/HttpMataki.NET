using System.Text;

namespace HttpMataki.NET;

public static class EncodingHelper
{
    public static Encoding GetEncodingFromContentType(string? charset)
    {
        if (!string.IsNullOrWhiteSpace(charset))
        {
            try
            {
                return Encoding.GetEncoding(charset);
            }
            catch
            {
                return Encoding.UTF8;
            }
        }

        return Encoding.UTF8;
    }
}