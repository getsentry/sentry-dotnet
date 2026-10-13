namespace Sentry.Tests.Internals.Extensions;

public class JsonExtensionsTests
{
    [Theory]
    [InlineData("\"2025-10-02T20:04:13.1234567Z\"", "2025-10-02T20:04:13.1234567Z")]
    [InlineData("\"2025-10-02T22:04:13.123+02:00\"", "2025-10-02T20:04:13.123Z")]
    [InlineData("1759435453", "2025-10-02T20:04:13Z")]
    [InlineData("1759435453.123", "2025-10-02T20:04:13.123Z")]
    [InlineData("1759435453.1234567", "2025-10-02T20:04:13.1234567Z")]
    [InlineData("1.759435453123E9", "2025-10-02T20:04:13.123Z")]
    public void GetTimestamp_StringOrUnixSeconds_ParsesInstant(string json, string expected)
    {
        var element = JsonDocument.Parse(json).RootElement;

        var timestamp = element.GetTimestamp();

        timestamp.Should().Be(DateTimeOffset.Parse(expected, CultureInfo.InvariantCulture));
    }
}
