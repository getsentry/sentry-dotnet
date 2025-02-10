using OperatingSystem = System.OperatingSystem;

#if MACCATALYST
namespace Sentry.Maui.Tests;

public class Issue2710
{
    [Fact]
    public void MacVersions()
    {
        var elike = Substitute.For<IEventLike>();
        elike.Contexts = new SentryContexts();

        var enricher = new Enricher(new SentryOptions());
        enricher.Apply(elike);

        elike.Contexts.OperatingSystem.Name.Should().Be("macOS");
        elike.Contexts.OperatingSystem.Version.Should().Be(Environment.OSVersion.Version.ToString());

        // output.WriteLine("OS Arch: " + RuntimeInformation.OSArchitecture);
        // output.WriteLine("Runtime: " + RuntimeInformation.RuntimeIdentifier);
        // output.WriteLine("FW: " + RuntimeInformation.FrameworkDescription);
        // output.WriteLine("RAW:" + RuntimeInformation.OSDescription);
        // output.WriteLine("Env Platform: " + Environment.OSVersion.Platform);
        // output.WriteLine("OS Version: " + Environment.OSVersion.Version);
    }
}

#endif
