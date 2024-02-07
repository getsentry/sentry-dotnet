namespace Sentry.Tests.Protocol;

public partial class MeasurementTests
{
    private static readonly MeasurementUnit EmptyUnit = new();

    [Fact]
    public void Constructor_IntValue()
    {
        var m = new Measurement(int.MaxValue);
        Assert.Equal(int.MaxValue, m.Value);
        Assert.Equal(EmptyUnit, m.Unit);
    }

    [Fact]
    public void Constructor_LongValue()
    {
        var m = new Measurement(long.MaxValue);
        Assert.Equal(long.MaxValue, m.Value);
        Assert.Equal(EmptyUnit, m.Unit);
    }

    [Fact]
    public void Constructor_ULongValue()
    {
        var m = new Measurement(ulong.MaxValue);
        Assert.Equal(ulong.MaxValue, m.Value);
        Assert.Equal(EmptyUnit, m.Unit);
    }

    [Fact]
    public void Constructor_DoubleValue()
    {
        var m = new Measurement(double.MaxValue);
        Assert.Equal(double.MaxValue, m.Value);
        Assert.Equal(EmptyUnit, m.Unit);
    }

    [Fact]
    public void Constructor_IntValue_WithUnit()
    {
        var m = new Measurement(int.MaxValue, MeasurementUnit.Duration.Second);
        Assert.Equal(int.MaxValue, m.Value);
        Assert.Equal(MeasurementUnit.Duration.Second, m.Unit);
    }

    [Fact]
    public void Constructor_LongValue_WithUnit()
    {
        var m = new Measurement(long.MaxValue, MeasurementUnit.Duration.Second);
        Assert.Equal(long.MaxValue, m.Value);
        Assert.Equal(MeasurementUnit.Duration.Second, m.Unit);
    }

    [Fact]
    public void Constructor_ULongValue_WithUnit()
    {
        var m = new Measurement(ulong.MaxValue, MeasurementUnit.Duration.Second);
        Assert.Equal(ulong.MaxValue, m.Value);
        Assert.Equal(MeasurementUnit.Duration.Second, m.Unit);
    }

    [Fact]
    public void Constructor_DoubleValue_WithUnit()
    {
        var m = new Measurement(double.MaxValue, MeasurementUnit.Duration.Second);
        Assert.Equal(double.MaxValue, m.Value);
        Assert.Equal(MeasurementUnit.Duration.Second, m.Unit);
    }

    [Fact]
    public void Json_IntValue()
    {
        var m = new Measurement(int.MaxValue);
        var json = m.ToJsonString();
        Assert.Equal("""{"value":2147483647}""", json);
    }

    [Fact]
    public void Json_LongValue()
    {
        var m = new Measurement(long.MaxValue);
        var json = m.ToJsonString();
        Assert.Equal("""{"value":9223372036854775807}""", json);
    }

    [Fact]
    public void Json_ULongValue()
    {
        var m = new Measurement(ulong.MaxValue);
        var json = m.ToJsonString();
        Assert.Equal("""{"value":18446744073709551615}""", json);
    }

    [Fact]
    public void Json_DoubleValue()
    {
        var m = new Measurement(double.MaxValue);
        var json = m.ToJsonString();
        Assert.Equal("""{"value":1.7976931348623157E+308}""", json);
    }

    [Fact]
    public void Json_IntValue_WithNone()
    {
        var m = new Measurement(int.MaxValue, MeasurementUnit.None);
        var json = m.ToJsonString();
        Assert.Equal("""{"value":2147483647,"unit":"none"}""", json);
    }

    [Fact]
    public void Json_LongValue_WithNone()
    {
        var m = new Measurement(long.MaxValue, MeasurementUnit.None);
        var json = m.ToJsonString();
        Assert.Equal("""{"value":9223372036854775807,"unit":"none"}""", json);
    }

    [Fact]
    public void Json_ULongValue_WithNone()
    {
        var m = new Measurement(ulong.MaxValue, MeasurementUnit.None);
        var json = m.ToJsonString();
        Assert.Equal("""{"value":18446744073709551615,"unit":"none"}""", json);
    }

    [Fact]
    public void Json_DoubleValue_WithNone()
    {
        var m = new Measurement(double.MaxValue, MeasurementUnit.None);
        var json = m.ToJsonString();
        Assert.Equal("""{"value":1.7976931348623157E+308,"unit":"none"}""", json);
    }

    [Fact]
    public void Json_IntValue_WithUnit()
    {
        var m = new Measurement(int.MaxValue, MeasurementUnit.Duration.Second);
        var json = m.ToJsonString();
        Assert.Equal("""{"value":2147483647,"unit":"second"}""", json);
    }

    [Fact]
    public void Json_LongValue_WithUnit()
    {
        var m = new Measurement(long.MaxValue, MeasurementUnit.Duration.Second);
        var json = m.ToJsonString();
        Assert.Equal("""{"value":9223372036854775807,"unit":"second"}""", json);
    }

    [Fact]
    public void Json_ULongValue_WithUnit()
    {
        var m = new Measurement(ulong.MaxValue, MeasurementUnit.Duration.Second);
        var json = m.ToJsonString();
        Assert.Equal("""{"value":18446744073709551615,"unit":"second"}""", json);
    }

    [Fact]
    public void Json_DoubleValue_WithUnit()
    {
        var m = new Measurement(double.MaxValue, MeasurementUnit.Duration.Second);
        var json = m.ToJsonString();
        Assert.Equal("""{"value":1.7976931348623157E+308,"unit":"second"}""", json);
    }

    [Fact]
    public void Transaction_SetMeasurement_IntValue()
    {
        // Arrange
        var client = Substitute.For<ISentryClient>();
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            TracesSampleRate = 1.0
        };
        var hub = new Hub(options, client);
        var transaction = hub.StartTransaction("name", "operation");

        // Act
        transaction.SetMeasurement("foo", int.MaxValue);
        transaction.Finish();

        // Assert
        client.Received(1).CaptureTransaction(
            Arg.Is<SentryTransaction>(t =>
                t.Measurements.Count == 1 &&
                t.Measurements["foo"].Value.As<int>() == int.MaxValue &&
                t.Measurements["foo"].Unit == EmptyUnit),
            Arg.Any<Scope>(),
            Arg.Any<SentryHint>()
            );
    }

    [Fact]
    public void Transaction_SetMeasurement_LongValue()
    {
        // Arrange
        var client = Substitute.For<ISentryClient>();
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            TracesSampleRate = 1.0
        };
        var hub = new Hub(options, client);
        var transaction = hub.StartTransaction("name", "operation");

        // Act
        transaction.SetMeasurement("foo", long.MaxValue);
        transaction.Finish();

        // Assert
        client.Received(1).CaptureTransaction(
            Arg.Is<SentryTransaction>(t =>
                t.Measurements.Count == 1 &&
                t.Measurements["foo"].Value.As<long>() == long.MaxValue &&
                t.Measurements["foo"].Unit == EmptyUnit),
            Arg.Any<Scope>(),
            Arg.Any<SentryHint>()
            );
    }

    [Fact]
    public void Transaction_SetMeasurement_ULongValue()
    {
        // Arrange
        var client = Substitute.For<ISentryClient>();
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            TracesSampleRate = 1.0
        };
        var hub = new Hub(options, client);
        var transaction = hub.StartTransaction("name", "operation");

        // Act
        transaction.SetMeasurement("foo", ulong.MaxValue);
        transaction.Finish();

        // Assert
        client.Received(1).CaptureTransaction(
            Arg.Is<SentryTransaction>(t =>
                t.Measurements.Count == 1 &&
                t.Measurements["foo"].Value.As<ulong>() == ulong.MaxValue &&
                t.Measurements["foo"].Unit == EmptyUnit),
            Arg.Any<Scope>(),
            Arg.Any<SentryHint>()
            );
    }

    [Fact]
    public void Transaction_SetMeasurement_DoubleValue()
    {
        // Arrange
        var client = Substitute.For<ISentryClient>();
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            TracesSampleRate = 1.0
        };
        var hub = new Hub(options, client);
        var transaction = hub.StartTransaction("name", "operation");

        // Act
        transaction.SetMeasurement("foo", double.MaxValue);
        transaction.Finish();

        // Assert
        client.Received(1).CaptureTransaction(
            Arg.Is<SentryTransaction>(t =>
                t.Measurements.Count == 1 &&
                Math.Abs(t.Measurements["foo"].Value.As<double>() - double.MaxValue) < double.Epsilon &&
                t.Measurements["foo"].Unit == EmptyUnit),
            Arg.Any<Scope>(),
            Arg.Any<SentryHint>()
        );
    }

    [Fact]
    public void Transaction_SetMeasurement_IntValue_WithUnit()
    {
        // Arrange
        var client = Substitute.For<ISentryClient>();
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            TracesSampleRate = 1.0
        };
        var hub = new Hub(options, client);
        var transaction = hub.StartTransaction("name", "operation");

        // Act
        transaction.SetMeasurement("foo", int.MaxValue, MeasurementUnit.Duration.Nanosecond);
        transaction.Finish();

        // Assert
        client.Received(1).CaptureTransaction(
            Arg.Is<SentryTransaction>(t =>
                t.Measurements.Count == 1 &&
                t.Measurements["foo"].Value.As<int>() == int.MaxValue &&
                t.Measurements["foo"].Unit == MeasurementUnit.Duration.Nanosecond),
            Arg.Any<Scope>(),
            Arg.Any<SentryHint>()
            );
    }

    [Fact]
    public void Transaction_SetMeasurement_LongValue_WithUnit()
    {
        // Arrange
        var client = Substitute.For<ISentryClient>();
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            TracesSampleRate = 1.0
        };
        var hub = new Hub(options, client);
        var transaction = hub.StartTransaction("name", "operation");

        // Act
        transaction.SetMeasurement("foo", long.MaxValue, MeasurementUnit.Duration.Nanosecond);
        transaction.Finish();

        // Assert
        client.Received(1).CaptureTransaction(
            Arg.Is<SentryTransaction>(t =>
                t.Measurements.Count == 1 &&
                t.Measurements["foo"].Value.As<long>() == long.MaxValue &&
                t.Measurements["foo"].Unit == MeasurementUnit.Duration.Nanosecond),
            Arg.Any<Scope>(),
            Arg.Any<SentryHint>()
            );
    }

    [Fact]
    public void Transaction_SetMeasurement_ULongValue_WithUnit()
    {
        // Arrange
        var client = Substitute.For<ISentryClient>();
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            TracesSampleRate = 1.0
        };
        var hub = new Hub(options, client);
        var transaction = hub.StartTransaction("name", "operation");

        // Act
        transaction.SetMeasurement("foo", ulong.MaxValue, MeasurementUnit.Duration.Nanosecond);
        transaction.Finish();

        // Assert
        client.Received(1).CaptureTransaction(
            Arg.Is<SentryTransaction>(t =>
                t.Measurements.Count == 1 &&
                t.Measurements["foo"].Value.As<ulong>() == ulong.MaxValue &&
                t.Measurements["foo"].Unit == MeasurementUnit.Duration.Nanosecond),
            Arg.Any<Scope>(),
            Arg.Any<SentryHint>()
            );
    }

    [Fact]
    public void Transaction_SetMeasurement_DoubleValue_WithUnit()
    {
        // Arrange
        var client = Substitute.For<ISentryClient>();
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            TracesSampleRate = 1.0
        };
        var hub = new Hub(options, client);
        var transaction = hub.StartTransaction("name", "operation");

        // Act
        transaction.SetMeasurement("foo", double.MaxValue, MeasurementUnit.Duration.Nanosecond);
        transaction.Finish();

        // Assert
        client.Received(1).CaptureTransaction(
            Arg.Is<SentryTransaction>(t =>
                t.Measurements.Count == 1 &&
                Math.Abs(t.Measurements["foo"].Value.As<double>() - double.MaxValue) < double.Epsilon &&
                t.Measurements["foo"].Unit == MeasurementUnit.Duration.Nanosecond),
            Arg.Any<Scope>(),
            Arg.Any<SentryHint>()
            );
    }
}
