using Sentry.PlatformAbstractions;

namespace Sentry.Tests.PlatformAbstractions;

public class RuntimeExtensionsTests
{
    [Theory]
    [InlineData(".NET Framework", true)]
    [InlineData(".NET Framework Foo", true)]
    [InlineData(".NET", false)]
    [InlineData(".NET Foo", false)]
    [InlineData(".NET Core", false)]
    [InlineData(".NET Core Foo", false)]
    [InlineData("Mono", false)]
    [InlineData("Mono Foo", false)]
    public void IsNetFx(string name, bool shouldMatch)
    {
        var runtime = new SentryRuntime(name);
        var result = runtime.IsNetFx();
        Assert.Equal(shouldMatch, result);
    }

    [Theory]
    [InlineData(".NET Framework", false)]
    [InlineData(".NET Framework Foo", false)]
    [InlineData(".NET", true)]
    [InlineData(".NET Foo", true)]
    [InlineData(".NET Core", true)]
    [InlineData(".NET Core Foo", true)]
    [InlineData("Mono", false)]
    [InlineData("Mono Foo", false)]
    public void IsNetCore(string name, bool shouldMatch)
    {
        var runtime = new SentryRuntime(name);
        var result = runtime.IsNetCore();
        Assert.Equal(shouldMatch, result);
    }

    [Theory]
    [InlineData(".NET Framework", false)]
    [InlineData(".NET Framework Foo", false)]
    [InlineData(".NET", false)]
    [InlineData(".NET Foo", false)]
    [InlineData(".NET Core", false)]
    [InlineData(".NET Core Foo", false)]
    [InlineData("Mono", true)]
    [InlineData("Mono Foo", true)]
    public void IsMono(string name, bool shouldMatch)
    {
        var runtime = new SentryRuntime(name);
        var result = runtime.IsMono();
        Assert.Equal(shouldMatch, result);
    }
}
