namespace Sentry.Tests.Internals;

/// <summary>
/// Copied/Modified from:
/// https://github.com/mentaldesk/fuse/blob/91af00dc9bc7e1deb2f11ab679c536194f85dd4a/MentalDesk.Fuse.Tests/ObjectExtensionsTests.cs
/// </summary>
public class ObjectExtensionsTests
{
    [Fact]
    public void SetFused_AutoPropertyName_StoresProperty()
    {
        var obj = new object();
        obj.SetFused("Value");

        var result = obj.GetFused<string>();

        Assert.Equal("Value", result);
    }

    [Fact]
    public void GetFused_ValidProperty_ReturnsValue()
    {
        var obj = new object();
        obj.SetFused("Test", "Value");

        var result = obj.GetFused<string>("Test");

        Assert.Equal("Value", result);
    }

    [Fact]
    public void GetFused_UnassignedProperty_ReturnsNull()
    {
        var obj = new object();

        var result = obj.GetFused<string>("Invalid");

        result.Should().BeNull();
    }

    [Fact]
    public void GetFused_InvalidPropertyType_ReturnsNull()
    {
        var obj = new object();
        obj.SetFused("StringProperty", "StringValue");

        var result = obj.GetFused<int?>("StringProperty");

        result.Should().BeNull();
    }
}
