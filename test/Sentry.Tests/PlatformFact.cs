namespace Sentry.Tests;

public enum Platform
{
    Windows,
    Linux,
    MacOS
}

public class PlatformFact : FactAttribute
{
    public PlatformFact(Platform platform)
    {
        var actual = platform switch
        {
            Platform.Windows => OSPlatform.Windows,
            Platform.Linux => OSPlatform.Linux,
            Platform.MacOS => OSPlatform.OSX,
            _ => throw new NotSupportedException()
        };

        if (!RuntimeInformation.IsOSPlatform(actual))
            Skip = "Ignored - Not Platform: " + actual;
    }
}
