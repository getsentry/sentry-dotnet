#if NET6_0_OR_GREATER
using Sentry.Tests;

namespace Sentry.Maui.Tests;

public class EnricherTests
{
    [PlatformFact(Platform.MacOS)]
    public void macOS_Platform_Version()
    {
        var elike = Substitute.For<IEventLike>();
        elike.Sdk.Returns(new SdkVersion());
        elike.User = new SentryUser();
        elike.Contexts = new SentryContexts();

        var enricher = new Enricher(new SentryOptions());
        enricher.Apply(elike);

        var os = elike.Contexts.OperatingSystem;
        os.Name.Should().Be("macOS");
        os.Version.Should().Be(Environment.OSVersion.Version.ToString());
    }
}
#endif
