#if ANDROID
using Sentry.Android.Callbacks;

namespace Sentry.Tests.Platforms.Android;

public class ScopeCallbackTests
{
    [Fact]
    public void Run_SetsEnvironment_UpdatesJavaScope()
    {
        using var javaOptions = new JavaSdk.SentryOptions();
        using var javaScope = new JavaSdk.Scope(javaOptions);
        var scope = global::Android.Runtime.Extensions.JavaCast<JavaSdk.IScope>(javaScope);
        using var callback = new ScopeCallback(s => s.Environment = "staging");

        callback.Run(scope);

        scope.Environment.Should().Be("staging");
    }
}
#endif
