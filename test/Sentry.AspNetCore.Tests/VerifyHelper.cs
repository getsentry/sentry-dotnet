namespace Sentry.AspNetCore.Tests;

internal static class VerifyHelper
{
    public static SettingsTask ScrubAspMembers(this SettingsTask settings)
    {
        return settings
            .IgnoreMembers("ConnectionId", "RequestId")
            .ScrubLinesWithReplace(_ => _.Split(new[] { " (Sentry.AspNetCore.Tests) " }, StringSplitOptions.None)[0]);
    }
}
