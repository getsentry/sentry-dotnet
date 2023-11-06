namespace Sentry.Extensions.Logging.Tests;

public class SentryLoggingOptionsExtensionsTests
{
    private readonly SentryLoggingOptions _sut = new();

    [Fact]
    public void ApplyDefaultTags_TagInEvent_DoesNotOverrideTag()
    {
        const string key = "key";
        const string expected = "event tag value";
        var target = new SentryEvent
        {
            Tags =
            {
                [key] = expected
            }
        };
        _sut.DefaultTags[key] = "default value";

        _sut.ApplyDefaultTags(target);

        Assert.Equal(expected, target.Tags[key]);
    }

    [Fact]
    public void ApplyDefaultTags_TagNotInEvent_AppliesTag()
    {
        const string key = "key";
        const string expected = "default tag value";
        var target = new SentryEvent();
        _sut.DefaultTags[key] = expected;

        _sut.ApplyDefaultTags(target);

        Assert.Equal(expected, target.Tags[key]);
    }
}
