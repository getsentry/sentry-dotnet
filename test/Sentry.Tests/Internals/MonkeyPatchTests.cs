namespace Sentry.Tests.Internals;

public class MonkeyPatchTests
{
    [Fact]
    public void Patched_WithValidProperty_ReturnsValue()
    {
        var obj = new object();
        obj.Patch("Test", "Value");

        var result = obj.Patched<string>("Test");

        Assert.Equal("Value", result);
    }

    [Fact]
    public void Patched_WithInvalidProperty_ReturnsNull()
    {
        var obj = new object();

        var result = obj.Patched<string>("Invalid");

        Assert.Null(result);
    }

    [Fact]
    public void With_CreatesNewInstance()
    {
        var obj = new object();

        var result = obj.With<TestClass>();

        Assert.IsType<TestClass>(result);
        Assert.NotNull(result);
    }

    [Fact]
    public void With_ReturnsSameInstance()
    {
        var obj = new object();

        var result1 = obj.With<TestClass>();
        var result2 = obj.With<TestClass>();

        Assert.Same(result1, result2);
    }

    class TestClass
    {
        public int X { get; set; }
    }
}
