using System.Text;

namespace HttpMataki.NET.UnitTests;

public class EncodingHelperTests
{
    public EncodingHelperTests()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    [Theory]
    [InlineData("utf-8", "utf-8")]
    [InlineData("gb2312", "gb2312")]
    [InlineData(null, "utf-8")]
    [InlineData("", "utf-8")]
    [InlineData("invalid-charset", "utf-8")]
    public void GetEncodingFromContentType_ReturnsExpectedEncoding(string? charset, string expectedName)
    {
        var encoding = EncodingHelper.GetEncodingFromContentType(charset);
        Assert.Equal(expectedName, encoding.WebName);
    }
}