using Sentry.PlatformAbstractions;

// ReSharper disable once CheckNamespace
// Stack trace filters out Sentry frames by namespace
namespace Other.Tests.Internals;

public partial class SentryStackTraceFactoryTests
{
    private class Fixture
    {
        public SentryOptions SentryOptions { get; } = new();
        public SentryStackTraceFactory GetSut() => new(SentryOptions);
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void Create_NoExceptionAndAttachStackTraceOptionFalse_NullResult()
    {
        _fixture.SentryOptions.AttachStacktrace = false;
        var sut = _fixture.GetSut();

        Assert.Null(sut.Create());
    }

    [Fact]
    public void Create_NoExceptionAndAttachStackTraceOptionOnWithOriginalMode_CurrentStackTrace()
    {
        _fixture.SentryOptions.AttachStacktrace = true;
        _fixture.SentryOptions.StackTraceMode = StackTraceMode.Original;
        var sut = _fixture.GetSut();

        var stackTrace = sut.Create();

        Assert.NotNull(stackTrace);

        Assert.Equal(
            nameof(Create_NoExceptionAndAttachStackTraceOptionOnWithOriginalMode_CurrentStackTrace),
            stackTrace.Frames.Last().Function);

        Assert.DoesNotContain(stackTrace.Frames, p =>
            p.Function?.StartsWith(
                nameof(DebugStackTrace.CreateFrame) + '(',
                StringComparison.Ordinal
            ) == true);
    }

    [Theory]
    [InlineData(StackTraceMode.Original, "AsyncWithWait_StackTrace { <lambda> }")]
#if !TEST_TRIMMABLE
    [InlineData(StackTraceMode.Enhanced, "void SentryStackTraceFactoryTests.AsyncWithWait_StackTrace(StackTraceMode mode, string method)+() => { }")]
#endif
    public void AsyncWithWait_StackTrace(StackTraceMode mode, string method)
    {
        _fixture.SentryOptions.AttachStacktrace = true;
        _fixture.SentryOptions.StackTraceMode = mode;
        var sut = _fixture.GetSut();

        SentryStackTrace stackTrace = null!;
        Task.Run(() => stackTrace = sut.Create()).Wait();

        Assert.NotNull(stackTrace);

        Assert.Equal(method, stackTrace.Frames.Last().Function);

        if (_fixture.SentryOptions.StackTraceMode == StackTraceMode.Original)
        {
            Assert.Equal("System.Threading.Tasks.Task`1", stackTrace.Frames[stackTrace.Frames.Count - 2].Module);
        }
    }

    [Theory]
    [InlineData(StackTraceMode.Original, "MoveNext")] // Should be "AsyncWithAwait_StackTrace { <lambda> }", but see note in SentryStackTraceFactory
#if !TEST_TRIMMABLE
    [InlineData(StackTraceMode.Enhanced, "async Task SentryStackTraceFactoryTests.AsyncWithAwait_StackTrace(StackTraceMode mode, string method)+(?) => { }")]
#endif
    public async Task AsyncWithAwait_StackTrace(StackTraceMode mode, string method)
    {
        _fixture.SentryOptions.AttachStacktrace = true;
        _fixture.SentryOptions.StackTraceMode = mode;
        var sut = _fixture.GetSut();

        var stackTrace = await Task.Run(async () =>
        {
            await Task.Yield();
            return sut.Create();
        });

        Assert.NotNull(stackTrace);

        Assert.Equal(method, stackTrace.Frames.Last().Function);
    }

#if !TEST_TRIMMABLE
    [Fact]
    public void Create_NoExceptionAndAttachStackTraceOptionOnWithEnhancedMode_CurrentStackTrace()
    {
        _fixture.SentryOptions.AttachStacktrace = true;
        _fixture.SentryOptions.StackTraceMode = StackTraceMode.Enhanced;
        var sut = _fixture.GetSut();

        var stackTrace = sut.Create();

        Assert.NotNull(stackTrace);

        Assert.Equal(
            $"void {GetType().Name}.{nameof(Create_NoExceptionAndAttachStackTraceOptionOnWithEnhancedMode_CurrentStackTrace)}()",
            stackTrace.Frames.Last().Function);

        Assert.DoesNotContain(stackTrace.Frames, p =>
            p.Function?.StartsWith(
                nameof(DebugStackTrace.CreateFrame) + '(',
                StringComparison.Ordinal
            ) == true);
    }
#endif

    [Fact]
    public void Create_WithExceptionAndDefaultAttachStackTraceOption_HasStackTrace()
    {
        var sut = _fixture.GetSut();

        Exception exception;
        try
        {
            Throw();
            void Throw() => throw null!;
        }
        catch (Exception e) { exception = e; }

        Assert.NotNull(sut.Create(exception));
    }

    [Fact]
    public void Create_WithExceptionAndAttachStackTraceOptionOn_HasStackTrace()
    {
        _fixture.SentryOptions.AttachStacktrace = true;
        var sut = _fixture.GetSut();

        Exception exception;
        try
        {
            Throw();
            void Throw() => throw null!;
        }
        catch (Exception e) { exception = e; }

        var stackTrace = sut.Create(exception);

        Assert.Equal(new StackTrace(exception, true).FrameCount, stackTrace?.Frames.Count);
    }

    [SkippableFact]
    public void FileNameShouldBeRelative()
    {
        Skip.If(RuntimeInfo.GetRuntime().IsMono());

        _fixture.SentryOptions.AttachStacktrace = true;
        var sut = _fixture.GetSut();

        Exception exception;
        try
        {
            Throw();
            void Throw() => throw new();
        }
        catch (Exception e)
        {
            exception = e;
        }

        var stackTrace = sut.Create(exception);

        Assert.NotNull(stackTrace);

        var frame = stackTrace.Frames[0];

#if __MOBILE__
        // We don't get file paths on mobile unless we've got a debugger attached.
        Skip.If(string.IsNullOrEmpty(frame.FileName));
#endif

        var path = Path.Combine("Internals", "SentryStackTraceFactoryTests.cs");
        Assert.Equal(path, frame.FileName);

        var fullPath = GetThisFilePath();
        Assert.Equal(fullPath, frame.AbsolutePath);
    }

    private static string GetThisFilePath([CallerFilePath] string path = null) => path;

    [Theory]
    [InlineData(StackTraceMode.Original, "ByRefMethodThatThrows")]
#if !TEST_TRIMMABLE
    [InlineData(StackTraceMode.Enhanced, "(Fixture f, int b) SentryStackTraceFactoryTests.ByRefMethodThatThrows(int value, in int valueIn, ref int valueRef, out int valueOut)")]
#endif
    public void Create_InlineCase_IncludesAmpersandAfterParameterType(StackTraceMode mode, string method)
    {
        _fixture.SentryOptions.StackTraceMode = mode;

        // Arrange
        var i = 5;
        var exception = Record.Exception(() => ByRefMethodThatThrows(i, in i, ref i, out i));

        _fixture.SentryOptions.AttachStacktrace = true;
        var factory = _fixture.GetSut();

        // Act
        var stackTrace = factory.Create(exception);

        // Assert
        var frame = stackTrace!.Frames.Last();
        frame.Function.Should().Be(method);
    }

    // ReSharper disable UnusedParameter.Local
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (Fixture f, int b) ByRefMethodThatThrows(int value, in int valueIn, ref int valueRef, out int valueOut) =>
        throw new Exception();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void GenericMethodThatThrows<T>(T value) =>
        throw new Exception();
    // ReSharper restore UnusedParameter.Local
}
