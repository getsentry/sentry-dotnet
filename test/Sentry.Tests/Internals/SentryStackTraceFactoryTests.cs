using System.Diagnostics;
using System.Runtime.CompilerServices;
using Sentry.PlatformAbstractions;

// ReSharper disable once CheckNamespace
// Stack trace filters out Sentry frames by namespace
namespace Other.Tests.Internals;

[UsesVerify]
public class SentryStackTraceFactoryTests
{
    private class Fixture
    {
        public SentryOptions SentryOptions { get; set; } = new();
        public SentryStackTraceFactory GetSut() => new(SentryOptions);
    }

    private readonly Fixture _fixture = new();
    private static readonly string ThisNamespace = typeof(SentryStackTraceFactoryTests).Namespace;

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
                nameof(SentryStackTraceFactory.CreateFrame) + '(',
                StringComparison.Ordinal
            ) == true);
    }

    [Theory]
    [InlineData(StackTraceMode.Original, "AsyncWithWait_StackTrace { <lambda> }")]
    [InlineData(StackTraceMode.Enhanced, "void SentryStackTraceFactoryTests.AsyncWithWait_StackTrace(StackTraceMode mode, string method)+() => { }")]
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
    [InlineData(StackTraceMode.Enhanced, "async Task SentryStackTraceFactoryTests.AsyncWithAwait_StackTrace(StackTraceMode mode, string method)+(?) => { }")]
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
                nameof(SentryStackTraceFactory.CreateFrame) + '(',
                StringComparison.Ordinal
            ) == true);
    }

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

        var expected = Path.Combine("Internals", "SentryStackTraceFactoryTests.cs");
        var path = sut.Create(exception)?.Frames[0].FileName;

#if __MOBILE__
        // We don't get file paths on mobile unless we've got a debugger attached.
        Skip.If(string.IsNullOrEmpty(path));
#endif
        Assert.Equal(expected, path);
    }

    [Theory]
    [InlineData(StackTraceMode.Original, "ByRefMethodThatThrows")]
    [InlineData(StackTraceMode.Enhanced, "(Fixture f, int b) SentryStackTraceFactoryTests.ByRefMethodThatThrows(int value, in int valueIn, ref int valueRef, out int valueOut)")]
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

    [SkippableTheory]
    [InlineData(StackTraceMode.Original)]
    [InlineData(StackTraceMode.Enhanced)]
    [Trait("Category", "Verify")]
    public Task MethodGeneric(StackTraceMode mode)
    {
        // TODO: Mono gives different results.  Investigate why.
        Skip.If(RuntimeInfo.GetRuntime().IsMono(), "Not supported on Mono");

        _fixture.SentryOptions.StackTraceMode = mode;

        // Arrange
        var i = 5;
        var exception = Record.Exception(() => GenericMethodThatThrows(i));

        _fixture.SentryOptions.AttachStacktrace = true;
        var factory = _fixture.GetSut();

        // Act
        var stackTrace = factory.Create(exception);

        // Assert;
        var frame = stackTrace!.Frames.Single(x => x.Function!.Contains("GenericMethodThatThrows"));
        return Verifier.Verify(frame)
            .IgnoreMembers<SentryStackFrame>(
                x => x.Package,
                x => x.LineNumber,
                x => x.ColumnNumber,
                x => x.InstructionOffset).AddScrubber(x => x.Replace(@"\", @"/"))
            .UseParameters(mode);
    }

    [Fact]
    public void CreateSentryStackFrame_AppNamespace_InAppFrame()
    {
        var frame = new StackFrame();
        var sut = _fixture.GetSut();

        var actual = sut.CreateFrame(frame);

        Assert.True(actual.InApp);
    }

    [Fact]
    public void CreateSentryStackFrame_AppNamespaceExcluded_NotInAppFrame()
    {
        _fixture.SentryOptions.AddInAppExclude(ThisNamespace);
        var sut = _fixture.GetSut();
        var frame = new StackFrame();

        var actual = sut.CreateFrame(frame);

        Assert.False(actual.InApp);
    }

    [Fact]
    public void CreateSentryStackFrame_NamespaceIncludedAndExcluded_IncludesTakesPrecedence()
    {
        _fixture.SentryOptions.AddInAppExclude(ThisNamespace);
        _fixture.SentryOptions.AddInAppInclude(ThisNamespace);
        var sut = _fixture.GetSut();
        var frame = new StackFrame();

        var actual = sut.CreateFrame(frame);

        Assert.True(actual.InApp);
    }

    // https://github.com/getsentry/sentry-dotnet/issues/64
    [Fact]
    public void DemangleAnonymousFunction_NullFunction_ContinuesNull()
    {
        var stackFrame = new SentryStackFrame
        {
            Function = null
        };

        SentryStackTraceFactory.DemangleAnonymousFunction(stackFrame);
        Assert.Null(stackFrame.Function);
    }

    [Fact]
    public void DemangleAsyncFunctionName_NullModule_ContinuesNull()
    {
        var stackFrame = new SentryStackFrame
        {
            Module = null
        };

        SentryStackTraceFactory.DemangleAnonymousFunction(stackFrame);
        Assert.Null(stackFrame.Module);
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
