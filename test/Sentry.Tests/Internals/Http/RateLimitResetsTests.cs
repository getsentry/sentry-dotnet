using Sentry.Internal.Http;

namespace Sentry.Tests.Internals.Http;

public class RateLimitResetsTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly RateLimitCategory ErrorCategory = new("error");
    private static readonly EnvelopeItem ErrorItem = EnvelopeItem.FromEvent(new SentryEvent());

    [Fact]
    public void Extend_ShorterResetAfterLongerReset_KeepsLongerReset()
    {
        var resets = new RateLimitResets();

        resets.Extend(ErrorCategory, Now.AddHours(1));
        resets.Extend(ErrorCategory, Now.AddSeconds(1));

        resets.IsRateLimited(ErrorItem, Now.AddMinutes(1)).Should().BeTrue();
    }

    [Fact]
    public void Extend_LongerResetAfterShorterReset_UsesLongerReset()
    {
        var resets = new RateLimitResets();

        resets.Extend(ErrorCategory, Now.AddSeconds(1));
        resets.Extend(ErrorCategory, Now.AddHours(1));

        resets.IsRateLimited(ErrorItem, Now.AddMinutes(1)).Should().BeTrue();
    }

    [Fact]
    public void IsRateLimited_ResetPassed_ReturnsFalse()
    {
        var resets = new RateLimitResets();

        resets.Extend(ErrorCategory, Now.AddSeconds(1));

        resets.IsRateLimited(ErrorItem, Now.AddMinutes(1)).Should().BeFalse();
    }

    [Fact]
    public void IsRateLimited_OtherCategory_ReturnsFalse()
    {
        var resets = new RateLimitResets();

        resets.Extend(new RateLimitCategory("transaction"), Now.AddHours(1));

        resets.IsRateLimited(ErrorItem, Now).Should().BeFalse();
    }
}
