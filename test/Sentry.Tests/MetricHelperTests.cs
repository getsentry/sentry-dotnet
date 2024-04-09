using Sentry.Protocol.Metrics;

namespace Sentry.Tests;

public class MetricHelperTests
{
    [Fact]
    public void GetMetricBucketKey_GeneratesExpectedKey()
    {
        // Arrange
        var type = MetricType.Counter;
        var metricKey = "quibbles";
        var unit = MeasurementUnit.None;
        var tags = new Dictionary<string, string> { ["tag1"] = "value1" };

        // Act
        var result = MetricHelper.GetMetricBucketKey(type, metricKey, unit, tags);

        // Assert
        result.Should().Be("c_quibbles_none_tag1=value1");
    }

    [Fact]
    public void GetTagsKey_ReturnsEmpty_WhenTagsIsNull()
    {
        var result = MetricHelper.GetTagsKey(null);
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetTagsKey_ReturnsEmpty_WhenTagsIsEmpty()
    {
        var result = MetricHelper.GetTagsKey(new Dictionary<string, string>());
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetTagsKey_ReturnsValidString_WhenTagsHasOneEntry()
    {
        var tags = new Dictionary<string, string> { { "tag1", "value1" } };
        var result = MetricHelper.GetTagsKey(tags);
        result.Should().Be("tag1=value1");
    }

    [Fact]
    public void GetTagsKey_ReturnsCorrectString_WhenTagsHasMultipleEntries()
    {
        var tags = new Dictionary<string, string> { { "tag1", "value1" }, { "tag2", "value2" } };
        var result = MetricHelper.GetTagsKey(tags);
        result.Should().Be("tag1=value1,tag2=value2");
    }

    [Fact]
    public void GetTagsKey_EscapesCharacters_WhenTagsContainDelimiters()
    {
        var tags = new Dictionary<string, string> { { "tag1\\", "value1\\" }, { "tag2,", "value2," }, { "tag3=", "value3=" } };
        var result = MetricHelper.GetTagsKey(tags);
        result.Should().Be(@"tag1\\=value1\\,tag2\,=value2\,,tag3\==value3\=");
    }

    [Theory]
    [InlineData(30)]
    [InlineData(31)]
    [InlineData(39)]
    public void GetTimeBucketKey_RoundsDownToNearestTenSeconds(int seconds)
    {
        // Arrange
        // Returns the number of seconds that have elapsed since 1970-01-01T00:00:00Z
        var timestamp = new DateTimeOffset(1970, 1, 1, 1, 1, seconds, TimeSpan.Zero);

        // Act
        var result = timestamp.GetTimeBucketKey();

        // Assert
        result.Should().Be(3690); // (1 hour) + (1 minute) plus (30 seconds) = 3690
    }

    [Theory]
    [InlineData(1970, 1, 1, 12, 34, 56, 0)]
    [InlineData(1970, 1, 2, 12, 34, 56, 1)]
    public void GetDayBucketKey_RoundsStartOfDay(int year, int month, int day, int hour, int minute, int second, int expectedDays)
    {
        // Arrange
        var timestamp = new DateTimeOffset(year, month, day, hour, minute, second, TimeSpan.Zero);

        // Act
        var result = timestamp.GetDayBucketKey();

        // Assert
        const int secondsInADay = 60 * 60 * 24;
        result.Should().Be(expectedDays * secondsInADay);
    }

    [Theory]
    [InlineData("Test123_:/@.{}[]$-", "Test123_:/@.{}[]$-")] // Valid characters
    [InlineData("test\nvalue", "test<LF>value")]
    [InlineData("test\rvalue", "test<CR>value")]
    [InlineData("test\tvalue", "test<HT>value")]
    [InlineData(@"test\value", @"test\\value")]
    [InlineData("test|value", "test\u007cvalue")]
    [InlineData("test,value", "test\u002cvalue")]
    public void SanitizeValue_ShouldReplaceReservedCharacters(string input, string expected)
    {
        // Act
        var result = MetricHelper.SanitizeValue(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Test123_.", "Test123_.")] // Valid characters
    [InlineData("test{value}", "test_value_")]
    [InlineData("test-value", "test_value")]
    public void SanitizeMetricUnit_ShouldReplaceInvalidCharactersWithUnderscore(string input, string expected)
    {
        // Act
        var result = MetricHelper.SanitizeMetricUnit(input);

        // Assert
        result.Should().Be(expected);
    }
}
