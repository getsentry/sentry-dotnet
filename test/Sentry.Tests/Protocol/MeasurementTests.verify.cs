using VerifyTests;

namespace Sentry.Tests.Protocol;

public partial class MeasurementTests
{
    [Fact]
    public Task Transaction_Serializes_Measurements()
    {
        var transaction = new SentryTransaction("name", "operation");
        transaction.Contexts.Trace.SpanId = SpanId.Empty;

        transaction.SetMeasurement("_", 0, MeasurementUnit.None);
        transaction.SetMeasurement("a", int.MaxValue);
        transaction.SetMeasurement("b", int.MaxValue, MeasurementUnit.Duration.Second);
        transaction.SetMeasurement("c", long.MaxValue);
        transaction.SetMeasurement("d", long.MaxValue, MeasurementUnit.Information.Kilobyte);
        transaction.SetMeasurement("e", ulong.MaxValue);
        transaction.SetMeasurement("f", ulong.MaxValue, MeasurementUnit.Information.Exbibyte);
        transaction.SetMeasurement("g", double.MaxValue);
        transaction.SetMeasurement("h", double.MaxValue, MeasurementUnit.Custom("foo"));
        transaction.SetMeasurement("i", 0.5, MeasurementUnit.Fraction.Ratio);
        transaction.SetMeasurement("j", 88.25, MeasurementUnit.Fraction.Percent);

        var json = transaction.ToJsonString();
        return VerifyJson(json);
    }
}
