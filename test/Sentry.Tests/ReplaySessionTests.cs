namespace Sentry.Tests;

public class ReplaySessionTests
{
    [Fact]
    public void PlatformDefault_OnlyOnAndroidAndIos()
    {
#if __ANDROID__ || __IOS__
        ReplaySession.PlatformDefault.Should().BeSameAs(ReplaySession.Instance);
#else
        ReplaySession.PlatformDefault.Should().BeNull();
#endif
    }

#if !__ANDROID__ && !__IOS__
    [Fact]
    public void CaptureReplay_OffMobile_ReturnsNull()
    {
        var @event = new SentryEvent { SentryExceptions = [new SentryException { Type = "InvalidOperationException" }] };

        ReplaySession.Instance.CaptureReplay(@event).Should().BeNull();
    }
#endif

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-an-id")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("00000000000000000000000000000000")]
    public void ToReplayId_NoReplay_ReturnsNull(string nativeId)
    {
        ReplaySession.ToReplayId(nativeId).Should().BeNull();
    }

    [Theory]
    [InlineData("bfd31b89a59d41c99d96dc2baf840ecd")]
    [InlineData("bfd31b89-a59d-41c9-9d96-dc2baf840ecd")]
    public void ToReplayId_ReplayId_ReturnsIt(string nativeId)
    {
        ReplaySession.ToReplayId(nativeId).Should().Be(new SentryId(Guid.Parse("bfd31b89a59d41c99d96dc2baf840ecd")));
    }

    [Theory]
    [InlineData("AppDomain.UnhandledException", false, null, true)]
    [InlineData("UnhandledExceptionRaiser", false, null, true)]
    [InlineData("generic", false, true, true)]
    [InlineData("GraphqlInstrumentation", false, null, false)]
    [InlineData("UnhandledExceptionRaiser", true, null, false)]
    [InlineData("generic", true, null, false)]
    [InlineData("generic", null, null, false)]
    public void IsCrash_Mechanism_ReturnsWhetherTheAppCrashed(string type, bool? handled, bool? terminal, bool expected)
    {
        var mechanism = new Mechanism { Type = type, Handled = handled };
        if (terminal is { } isTerminal)
        {
            mechanism.Terminal = isTerminal;
        }

        var @event = new SentryEvent
        {
            SentryExceptions = [new SentryException { Type = "InvalidOperationException" }, new SentryException { Mechanism = mechanism }]
        };

        ReplaySession.IsCrash(@event).Should().Be(expected);
    }

    [Fact]
    public void IsCrash_NoExceptions_ReturnsFalse()
    {
        ReplaySession.IsCrash(new SentryEvent()).Should().BeFalse();
    }

    [Fact]
    public void CaptureOnMainThread_OnMainThread_CapturesInline()
    {
        var replayId = SentryId.Create();
        var dispatched = false;

        var result = ReplaySession.CaptureOnMainThread(() => replayId, () => null, isMainThread: true,
            _ => dispatched = true, TimeSpan.Zero, null);

        result.Should().Be(replayId);
        dispatched.Should().BeFalse();
    }

    [Fact]
    public void CaptureOnMainThread_OffMainThread_CapturesOnDispatcher()
    {
        var replayId = SentryId.Create();
        var dispatched = false;

        var result = ReplaySession.CaptureOnMainThread(() => replayId, () => SentryId.Create(), isMainThread: false,
            work =>
            {
                dispatched = true;
                work();
            }, TimeSpan.Zero, null);

        result.Should().Be(replayId);
        dispatched.Should().BeTrue();
    }

    [Fact]
    public void CaptureOnMainThread_MainThreadBusy_LogsAndReturnsFallback()
    {
        var logger = new InMemoryDiagnosticLogger();
        var options = new SentryOptions { Debug = true, DiagnosticLogger = logger };
        var activeReplayId = SentryId.Create();
        Action pendingCapture = null;

        var result = ReplaySession.CaptureOnMainThread(() => SentryId.Create(), () => activeReplayId,
            isMainThread: false, work => pendingCapture = work, TimeSpan.Zero, options);

        result.Should().Be(activeReplayId);
        logger.Entries.Should().Contain(entry => entry.Level == SentryLevel.Warning);
        pendingCapture.Should().NotBeNull();
        pendingCapture();
    }

    [Fact]
    public void CaptureOnMainThread_CapturesNoReplay_ReturnsNullNotFallback()
    {
        var result = ReplaySession.CaptureOnMainThread(() => null, () => SentryId.Create(), isMainThread: false,
            work => work(), TimeSpan.Zero, null);

        result.Should().BeNull();
    }

    [Fact]
    public void CaptureOnMainThread_CaptureThrows_Rethrows()
    {
        var exception = new InvalidOperationException();

        var act = () => ReplaySession.CaptureOnMainThread(() => throw exception, () => null, isMainThread: false,
            work => work(), TimeSpan.Zero, null);

        act.Should().Throw<InvalidOperationException>().Which.Should().BeSameAs(exception);
    }
}
