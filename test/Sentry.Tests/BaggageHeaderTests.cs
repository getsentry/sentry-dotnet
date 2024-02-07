namespace Sentry.Tests;

public partial class BaggageHeaderTests
{
    [Fact]
    public void BaggageHeader_TryParse_Empty()
    {
        var header = BaggageHeader.TryParse("");
        Assert.Null(header);
    }

    [Fact]
    public void BaggageHeader_Create()
    {
        var header = BaggageHeader.Create(new List<KeyValuePair<string, string>>
        {
            {"foo", "123"},
            {"bar", "456"}
        });

        var expected = new List<KeyValuePair<string, string>>
        {
            {"foo", "123"},
            {"bar", "456"}
        };

        Assert.Equal(expected, header.Members);
    }

    [Fact]
    public void BaggageHeader_Create_WithSentryPrefix()
    {
        var header = BaggageHeader.Create(new List<KeyValuePair<string, string>>
        {
            {"foo", "123"},
            {"bar", "456"}
        }, useSentryPrefix: true);

        var expected = new List<KeyValuePair<string, string>>
        {
            {"sentry-foo", "123"},
            {"sentry-bar", "456"}
        };

        Assert.Equal(expected, header.Members);
    }

    [Fact]
    public void BaggageHeader_ToString()
    {
        var header = BaggageHeader.Create(new List<KeyValuePair<string, string>>
        {
            {"test-bad-chars", @" ""(),/:;<=>?@[\]{}"},
            {"sentry-public_key", "49d0f7386ad645858ae85020e393bef3"},
            {"test-name", "Amélie"},
            {"test-name", "John"},
        });

        Assert.Equal(
            "test-bad-chars=%20%22%28%29%2C%2F%3A%3B%3C%3D%3E%3F%40%5B%5C%5D%7B%7D, " +
            "sentry-public_key=49d0f7386ad645858ae85020e393bef3, test-name=Am%C3%A9lie, test-name=John",
            header.ToString());
    }

    [Fact]
    public void BaggageHeader_Merge()
    {
        var header1 = BaggageHeader.Create(new List<KeyValuePair<string, string>>
        {
            {"foo", "123"},
        });

        var header2 = BaggageHeader.Create(new List<KeyValuePair<string, string>>
        {
            {"bar", "456"},
            {"baz", "789"},
        });

        var header3 = BaggageHeader.Create(new List<KeyValuePair<string, string>>
        {
            {"foo", "789"},
            {"baz", "000"},
        });

        var merged = BaggageHeader.Merge(new[] { header1, header2, header3 });

        var expected = new List<KeyValuePair<string, string>>
        {
            {"foo", "123"},
            {"bar", "456"},
            {"baz", "789"},
            {"foo", "789"},
            {"baz", "000"}
        };

        Assert.Equal(expected, merged.Members);
    }
}
