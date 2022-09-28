namespace Sentry.Tests;

[UsesVerify]
public class BaggageHeaderTests
{
    [Fact]
    [Trait("Category", "Verify")]
    public Task Parse_Full()
    {
        var header = BaggageHeader.TryParse(
            "sentry-trace_id=771a43a4192642f0b136d5159a501700," +
            "sentry-public_key=49d0f7386ad645858ae85020e393bef3," +
            "sentry-sample_rate=0.01337," +
            "sentry-release=foo@abc+123," +
            "sentry-environment=production," +
            "sentry-user_segment=segment-a," +
            "sentry-transaction=something%2cI%20think," +
            "sentry-other_value1=Am%C3%A9lie," +
            "sentry-other_value2=Foo%20Bar%20Baz," +
            "other-vendor-value-1=foo," +
            "other-vendor-value-2=foo;bar;");

        Assert.NotNull(header);

        return VerifyHeader(header)
            .AppendValue("SentryOtherValue1", header.GetValue("sentry-other_value1")!);
    }

    [Fact]
    [Trait("Category", "Verify")]
    public Task Parse_FromExample()
    {
        // Taken from https://develop.sentry.dev/sdk/performance/dynamic-sampling-context/#baggage
        var header = BaggageHeader.TryParse(
            "other-vendor-value-1=foo;bar;baz, sentry-trace_id=771a43a4192642f0b136d5159a501700, sentry-public_key=49d0f7386ad645858ae85020e393bef3, sentry-sample_rate=0.01337, sentry-user_id=Am%C3%A9lie, other-vendor-value-2=foo;bar;");

        return VerifyHeader(header);
    }

    private static SettingsTask VerifyHeader(BaggageHeader header)
    {
        return Verify(header)
            .DontScrubGuids()
            .AddExtraSettings(x => x.Converters.Add(new SentryIdConverter()))
            .AppendValue("items", header.GetRawMembers());
    }

    private class SentryIdConverter : WriteOnlyJsonConverter<SentryId>
    {
        public override void Write(VerifyJsonWriter writer, SentryId value)
        {
            writer.WriteValue(value.ToString());
        }
    }
}
