namespace Sentry.Tests;

public partial class BaggageHeaderTests
{
    [Fact]
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

    private static SettingsTask VerifyHeader(BaggageHeader header)
    {
        return Verify(header.Members)
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
