namespace Sentry.Tests;

public class MeasurementUnitTests
{
    [Fact]
    public void DefaultNone()
    {
        MeasurementUnit m = new();
        Assert.Equal(MeasurementUnit.None, m);
        Assert.Equal("", m.ToString());
    }

    [Fact]
    public void CanUseDurationUnits()
    {
        MeasurementUnit m = MeasurementUnit.Duration.Second;
        Assert.Equal("second", m.ToString());
    }

    [Fact]
    public void CanUseInformationUnits()
    {
        MeasurementUnit m = MeasurementUnit.Information.Byte;
        Assert.Equal("byte", m.ToString());
    }

    [Fact]
    public void CanUseFractionUnits()
    {
        MeasurementUnit m = MeasurementUnit.Fraction.Percent;
        Assert.Equal("percent", m.ToString());
    }

    [Fact]
    public void CanUseCustomUnits()
    {
        var m = MeasurementUnit.Custom("foo");
        Assert.Equal("foo", m.ToString());
    }

    [Fact]
    public void ZeroInequality()
    {
        MeasurementUnit m1 = (MeasurementUnit.Duration)0;
        MeasurementUnit m2 = (MeasurementUnit.Information)0;
        Assert.NotEqual(m1, m2);
    }

    [Fact]
    public void ZeroDifferentHashCodes()
    {
        MeasurementUnit m1 = (MeasurementUnit.Duration)0;
        MeasurementUnit m2 = (MeasurementUnit.Information)0;
        Assert.NotEqual(m1.GetHashCode(), m2.GetHashCode());
    }

    [Fact]
    public void SimpleEquality()
    {
        MeasurementUnit m1 = MeasurementUnit.Duration.Second;
        MeasurementUnit m2 = MeasurementUnit.Duration.Second;
        Assert.Equal(m1, m2);

        // we overload the == operator, so check that as well
        Assert.True(m1 == m2);
    }

    [Fact]
    public void SimpleInequality()
    {
        MeasurementUnit m1 = MeasurementUnit.Duration.Second;
        MeasurementUnit m2 = MeasurementUnit.Duration.Millisecond;
        Assert.NotEqual(m1, m2);

        // we overload the != operator, so check that as well
        Assert.True(m1 != m2);
    }

    [Fact]
    public void MixedInequality()
    {
        MeasurementUnit m1 = MeasurementUnit.Duration.Nanosecond;
        MeasurementUnit m2 = MeasurementUnit.Information.Bit;
        Assert.NotEqual(m1, m2);
    }

    [Fact]
    public void CustomEquality()
    {
        var m1 = MeasurementUnit.Custom("foo");
        var m2 = MeasurementUnit.Custom("foo");
        Assert.Equal(m1, m2);
    }

    [Fact]
    public void CustomInequality()
    {
        var m1 = MeasurementUnit.Custom("foo");
        var m2 = MeasurementUnit.Custom("bar");
        Assert.NotEqual(m1, m2);
    }

    [Fact]
    public void MixedInequalityWithCustom()
    {
        var m1 = MeasurementUnit.Custom("second");
        var m2 = MeasurementUnit.Duration.Second;
        Assert.NotEqual(m1, m2);
    }
}
