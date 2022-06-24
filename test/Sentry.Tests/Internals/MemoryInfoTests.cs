#if NETCOREAPP3_1_OR_GREATER

namespace Sentry.Tests.Internals;

[UsesVerify]
public class MemoryInfoTests
{
    [Fact]
    public Task WriteTo()
    {
#if NET5_0_OR_GREATER
        var info = new MemoryInfo(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, true, false, new[] {TimeSpan.FromSeconds(1)});
#else
        var info = new MemoryInfo(1, 2, 3, 4, 5, 6);
#endif
        var json = info.ToJsonString();

        var settings = new VerifySettings();
        settings.UniqueForTargetFrameworkAndVersion();
        return VerifyJson(json, settings);
    }
}

#endif
