using Sentry.Testing;

namespace Sentry.Tests.Protocol;

public partial class SentryEventTests
{
    private readonly IDiagnosticLogger _testOutputLogger;

    public SentryEventTests(ITestOutputHelper output)
    {
        _testOutputLogger = new TestOutputDiagnosticLogger(output);
    }

    [Fact]
    public void Ctor_Platform_CSharp()
    {
        var evt = new SentryEvent();
        Assert.Equal(Constants.Platform, evt.Platform);
    }

    [Fact]
    public void Ctor_Timestamp_NonDefault()
    {
        var evt = new SentryEvent();
        Assert.NotEqual(default, evt.Timestamp);
    }

    [Fact]
    public void Ctor_EventId_NonDefault()
    {
        var evt = new SentryEvent();
        Assert.NotEqual(default, evt.EventId);
    }

    [Fact]
    public void Ctor_Exception_Stored()
    {
        var e = new Exception();
        var evt = new SentryEvent(e);
        Assert.Same(e, evt.Exception);
    }

    [Fact]
    public void SentryThreads_Getter_NotNull()
    {
        var evt = new SentryEvent();
        Assert.NotNull(evt.SentryThreads);
    }

    [Fact]
    public void SentryThreads_SetToNUll_Getter_NotNull()
    {
        var evt = new SentryEvent
        {
            SentryThreads = null
        };

        Assert.NotNull(evt.SentryThreads);
    }

    [Fact]
    public void SentryExceptions_Getter_NotNull()
    {
        var evt = new SentryEvent();
        Assert.NotNull(evt.SentryExceptions);
    }

    [Fact]
    public void SentryExceptions_SetToNUll_Getter_NotNull()
    {
        var evt = new SentryEvent
        {
            SentryExceptions = null
        };

        Assert.NotNull(evt.SentryExceptions);
    }

    [Fact]
    public void Modules_Getter_NotNull()
    {
        var evt = new SentryEvent();
        Assert.NotNull(evt.Modules);
    }
}
