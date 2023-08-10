namespace Sentry.Tests.Internals;

public class ObjectExtensionsTests
{
    [Fact]
    public void With_CreatesNewInstance()
    {
        var obj = new object();

        var result = obj.Fused<TestClass>();

        Assert.IsType<TestClass>(result);
        Assert.NotNull(result);
    }

    [Fact]
    public void With_ReturnsSameInstance()
    {
        var obj = new object();

        var result1 = obj.Fused<TestClass>();
        var result2 = obj.Fused<TestClass>();

        Assert.Same(result1, result2);
    }

    private class TestClass
    {
        public int X { get; set; }
    }
}
