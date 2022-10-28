namespace Sentry.Tests;

[UsesVerify]
public class BaggageHeaderTests
{
    [Fact]
    [Trait("Category", "Verify")]
    public Task BaggageHeader_TryParse_Full()
    {
        // note: whitespace is intentionally varied as it should be ignored
        var header = BaggageHeader.TryParse(
            "sentry-trace_id=771a43a4192642f0b136d5159a501700," +
            "sentry-public_key = 49d0f7386ad645858ae85020e393bef3 , " +
            "sentry-sample_rate=0.01337, " +
            "sentry-release=foo@abc+123," +
            "sentry-environment=production," +
            "sentry-user_segment =segment-a," +
            "sentry-transaction=something%2c%20I%20think," +
            "sentry-other_value1=Am%C3%A9lie, " +
            "sentry-other_value2= Foo%20Bar%20Baz ," +
            "other-vendor-value-1=foo," +
            "other-vendor-value-2=foo;bar;," +
            "dup-value=something, " +
            "dup-value=something,");

        Assert.NotNull(header);

        return VerifyHeader(header);
    }

    [Fact]
    [Trait("Category", "Verify")]
    public Task BaggageHeader_TryParse_FromExample()
    {
        // Taken from https://develop.sentry.dev/sdk/performance/dynamic-sampling-context/#baggage
        var header = BaggageHeader.TryParse(
            "other-vendor-value-1=foo;bar;baz, " +
            "sentry-trace_id=771a43a4192642f0b136d5159a501700, " +
            "sentry-public_key=49d0f7386ad645858ae85020e393bef3, " +
            "sentry-sample_rate=0.01337, " +
            "sentry-user_id=Am%C3%A9lie, " +
            "other-vendor-value-2=foo;bar;");

        return VerifyHeader(header);
    }

    [Fact]
    [Trait("Category", "Verify")]
    public Task BaggageHeader_TryParse_OnlySentry()
    {
        // Taken from https://develop.sentry.dev/sdk/performance/dynamic-sampling-context/#baggage
        var header = BaggageHeader.TryParse(
            "other-vendor-value-1=foo;bar;baz, " +
            "sentry-trace_id=771a43a4192642f0b136d5159a501700, " +
            "sentry-public_key=49d0f7386ad645858ae85020e393bef3, " +
            "sentry-sample_rate=0.01337, " +
            "sentry-user_id=Am%C3%A9lie, " +
            "other-vendor-value-2=foo;bar;",
            onlySentry: true);

        return VerifyHeader(header);
    }

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

        var merged = BaggageHeader.Merge(new[] {header1, header2, header3});

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

    private static SettingsTask VerifyHeader(BaggageHeader header)
    {
        return Verifier.Verify(header.Members)
            .DontScrubGuids()
            .AddExtraSettings(x => x.Converters.Add(new SentryIdConverter()));
    }

    private class SentryIdConverter : WriteOnlyJsonConverter<SentryId>
    {
        public override void Write(VerifyJsonWriter writer, SentryId value)
        {
            writer.WriteValue(value.ToString());
        }
    }
}
