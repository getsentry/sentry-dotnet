using Sentry.Android.Callbacks;

namespace Sentry.Tests.Platforms.Android;

public class SegfaultSuppressionCallbackTests
{
    [Theory]
    [InlineData("SIGSEGV", "Segfault", true)]
    [InlineData("SIGSEGV", "Other", false)]
    [InlineData("Other", "Segfault", false)]
    public void Execute_WithoutFilter_OnlyDropsMatchingSegfault(string type, string value, bool drop)
    {
        using var callback = new SegfaultSuppressionCallback(null);
        using var exception = new JavaSdk.Protocol.SentryException { Type = type, Value = value };
        using var evt = new JavaSdk.SentryEvent
        {
            Exceptions = new[] { exception },
            EventId = new JavaSdk.Protocol.SentryId(new string('z', 32))
        };
        using var hint = new JavaSdk.Hint();

        var result = callback.Execute(evt, hint);

        if (drop)
        {
            result.Should().BeNull();
        }
        else
        {
            result.Should().BeSameAs(evt);
        }
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public void Execute_WithFilter_SuppressesFirstAndPreservesFilterResult(bool segfault, bool filterDrops)
    {
        var options = new SentryOptions();
        options.Android.SuppressSegfaults = true;
        options.Native.EnableBeforeSend = true;
        var calls = 0;
        options.SetBeforeSend((evt, _) =>
        {
            calls++;
            evt.ServerName = "filtered";
            return filterDrops ? null : evt;
        });
        using var javaOptions = new JavaSdk.SentryOptions();
        using var callback = SentrySdk.CreateBeforeSendCallback(options, javaOptions);
        using var evt = new JavaSdk.SentryEvent();
        if (segfault)
        {
            evt.Exceptions = new[] { new JavaSdk.Protocol.SentryException { Type = "SIGSEGV", Value = "Segfault" } };
        }
        using var hint = new JavaSdk.Hint();

        using var result = callback!.Execute(evt, hint);

        calls.Should().Be(segfault ? 0 : 1);
        if (segfault || filterDrops)
        {
            result.Should().BeNull();
        }
        else
        {
            result!.ServerName.Should().Be("filtered");
        }
    }
}
