#nullable enable
// ReSharper disable once CheckNamespace
// Stack trace filters out Sentry frames by namespace

namespace Other.Tests.Internals;

[UsesVerify]

public class DebugStackTraceTests
{
    private class Fixture
    {
        public SentryOptions SentryOptions { get; set; } = new();
        public InjectableDebugStackTrace GetSut() => new(SentryOptions);
    }

    private readonly Fixture _fixture = new();
    private static readonly string ThisNamespace = typeof(SentryStackTraceFactoryTests).Namespace!;

// TODO: Create integration test to test this behaviour when publishing AOT apps
// See https://github.com/getsentry/sentry-dotnet/issues/2772
    [Fact]
    public void CreateSentryStackFrame_AppNamespace_InAppFrame()
    {
        var frame = new StackFrame();
        var sut = _fixture.GetSut();

        var actual = sut.CreateFrame(new RealStackFrame(frame));

        Assert.True(actual?.InApp);
    }

// TODO: Create integration test to test this behaviour when publishing AOT apps
// See https://github.com/getsentry/sentry-dotnet/issues/2772
    [Fact]
    public void CreateSentryStackFrame_AppNamespaceExcluded_NotInAppFrame()
    {
        _fixture.SentryOptions.AddInAppExclude(ThisNamespace);
        var sut = _fixture.GetSut();
        var frame = new StackFrame();

        var actual = sut.CreateFrame(new RealStackFrame(frame));

        Assert.False(actual?.InApp);
    }

// TODO: Create integration test to test this behaviour when publishing AOT apps
// See https://github.com/getsentry/sentry-dotnet/issues/2772
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CreateSentryStackFrame_SystemType_NotInAppFrame(bool useEnhancedStackTrace)
    {
        // Arrange
        var sut = _fixture.GetSut();
        var exception = Assert.ThrowsAny<Exception>(() => _ = Convert.FromBase64String("This will throw."));
        var stackTrace = new StackTrace(exception);
        var frame = useEnhancedStackTrace ? EnhancedStackTrace.GetFrames(stackTrace)[0] : stackTrace.GetFrame(0);

        // Sanity Check
        Assert.NotNull(frame);
        Assert.Equal(typeof(Convert), frame.GetMethod()?.DeclaringType);

        // Act
        var actual = sut.CreateFrame(new RealStackFrame(frame));

        // Assert
        Assert.False(actual?.InApp);
    }

// TODO: Create integration test to test this behaviour when publishing AOT apps
// See https://github.com/getsentry/sentry-dotnet/issues/2772
    [Fact]
    public void CreateSentryStackFrame_NamespaceIncludedAndExcluded_IncludesTakesPrecedence()
    {
        _fixture.SentryOptions.AddInAppExclude(ThisNamespace);
        _fixture.SentryOptions.AddInAppInclude(ThisNamespace);
        var sut = _fixture.GetSut();
        var frame = new StackFrame();

        var actual = sut.CreateFrame(new RealStackFrame(frame));

        Assert.True(actual?.InApp);
    }

    // https://github.com/getsentry/sentry-dotnet/issues/64
    [Fact]
    public void DemangleAnonymousFunction_NullFunction_ContinuesNull()
    {
        var stackFrame = new SentryStackFrame
        {
            Function = null
        };

        DebugStackTrace.DemangleAnonymousFunction(stackFrame);
        Assert.Null(stackFrame.Function);
    }

    [Fact]
    public void DemangleAsyncFunctionName_NullModule_ContinuesNull()
    {
        var stackFrame = new SentryStackFrame
        {
            Module = null
        };

        DebugStackTrace.DemangleAnonymousFunction(stackFrame);
        Assert.Null(stackFrame.Module);
    }

    [Fact]
    public void MergeDebugImages_Empty()
    {
        var sut = _fixture.GetSut();
        var e = new SentryEvent();
        sut.MergeDebugImagesInto(e);
        Assert.Null(e.DebugImages);
    }

    [Fact]
    public void MergeDebugImages_ThrowsOnSecondRun()
    {
        var sut = _fixture.GetSut();
        sut.Inject(1);

        var e = new SentryEvent();
        sut.MergeDebugImagesInto(e);
        Assert.NotNull(e.DebugImages);
        Assert.Throws<InvalidOperationException>(() => sut.MergeDebugImagesInto(e));
    }

    [Fact]
    public void MergeDebugImages()
    {
        var sut1 = _fixture.GetSut();
        sut1.Inject(1111);
        sut1.Inject(2222);
        sut1.Frames.Add(new SentryStackFrame() { Function = "1", AddressMode = "rel:0" });
        sut1.Frames.Add(new SentryStackFrame() { Function = "2", AddressMode = "rel:1" });
        sut1.Frames.Add(new SentryStackFrame() { Function = "3", AddressMode = "rel:0" });

        var e = new SentryEvent();
        sut1.MergeDebugImagesInto(e);
        Assert.NotNull(e.DebugImages);
        Assert.Equal(2, e.DebugImages.Count);
        Assert.Equal("1111.dll", e.DebugImages[0].CodeFile);
        Assert.Equal("2222.dll", e.DebugImages[1].CodeFile);

        // Stack trace isn't changed - there were no DebugImage relocations yet.
        CheckStackTraceIsUnchanged(sut1);

        var sut2 = _fixture.GetSut();
        sut2.Inject(3333);
        sut2.Inject(1111);
        sut2.Inject(4444);
        sut2.Frames.Add(new SentryStackFrame() { Function = "1", AddressMode = "rel:0" });
        sut2.Frames.Add(new SentryStackFrame() { Function = "2", AddressMode = "rel:1" });
        sut2.Frames.Add(new SentryStackFrame() { Function = "3", AddressMode = "rel:0" });
        sut2.Frames.Add(new SentryStackFrame() { Function = "4", AddressMode = "rel:2" });

        // Only the two new debug images are added.
        sut2.MergeDebugImagesInto(e);
        Assert.Equal(4, e.DebugImages.Count);
        Assert.Equal("1111.dll", e.DebugImages[0].CodeFile);
        Assert.Equal("2222.dll", e.DebugImages[1].CodeFile);
        Assert.Equal("3333.dll", e.DebugImages[2].CodeFile);
        Assert.Equal("4444.dll", e.DebugImages[3].CodeFile);

        // First stack trace must remain unchanged.
        CheckStackTraceIsUnchanged(sut1);

        // Stack trace is updated to reflect new positions
        Assert.Equal(4, sut2.Frames.Count);
        Assert.Equal("1", sut2.Frames[0].Function);
        Assert.Equal("rel:2", sut2.Frames[0].AddressMode);
        Assert.Equal("2", sut2.Frames[1].Function);
        Assert.Equal("rel:0", sut2.Frames[1].AddressMode);
        Assert.Equal("3", sut2.Frames[2].Function);
        Assert.Equal("rel:2", sut2.Frames[2].AddressMode);
        Assert.Equal("4", sut2.Frames[3].Function);
        Assert.Equal("rel:3", sut2.Frames[3].AddressMode);

        void CheckStackTraceIsUnchanged(SentryStackTrace stackTrace)
        {
            Assert.Equal(3, stackTrace.Frames.Count);
            Assert.Equal("1", stackTrace.Frames[0].Function);
            Assert.Equal("rel:0", stackTrace.Frames[0].AddressMode);
            Assert.Equal("2", stackTrace.Frames[1].Function);
            Assert.Equal("rel:1", stackTrace.Frames[1].AddressMode);
            Assert.Equal("3", stackTrace.Frames[2].Function);
            Assert.Equal("rel:0", stackTrace.Frames[2].AddressMode);
        }
    }

#if NET6_0_OR_GREATER
    [Fact]
    public void ParseNativeAOTToString()
    {
        var frame = DebugStackTrace.ParseNativeAOTToString(
            "System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task) + 0x42 at offset 66 in file:line:column <filename unknown>:0:0");
        Assert.Equal("System.Runtime.CompilerServices.TaskAwaiter", frame.Module);
        Assert.Equal("HandleNonSuccessAndDebuggerNotification(Task)", frame.Function);
        Assert.Null(frame.Package);

        frame = DebugStackTrace.ParseNativeAOTToString(
            "Program.<<Main>$>d__0.MoveNext() + 0xdd at offset 221 in file:line:column <filename unknown>:0:0");
        Assert.Equal("Program.<<Main>$>d__0", frame.Module);
        Assert.Equal("MoveNext()", frame.Function);
        Assert.Null(frame.Package);

        frame = DebugStackTrace.ParseNativeAOTToString(
            "Sentry.Samples.Console.Basic!<BaseAddress>+0x4abb3b at offset 283 in file:line:column <filename unknown>:0:0");
        Assert.Null(frame.Module);
        Assert.Null(frame.Function);
        Assert.Null(frame.Package);
    }

    // TODO: Create integration test to test this behaviour when publishing AOT apps
    // See https://github.com/getsentry/sentry-dotnet/issues/2772
    [Fact]
    public Task CreateFrame_ForNativeAOT()
    {
        var sut = _fixture.GetSut();
        var frame = sut.CreateFrame(new StubNativeAOTStackFrame()
        {
            Function = "DoSomething(int, long)",
            Module = "Foo.Bar",
            ImageBase = 1,
            IP = 2,
        });

        return VerifyJson(frame.ToJsonString());
    }
#endif

    private class InjectableDebugStackTrace : DebugStackTrace
    {
        public InjectableDebugStackTrace(SentryOptions options) : base(options) { }

        public void Inject(int identifier)
        {
            DebugImages.Add(new DebugImage
            {
                CodeFile = $"{identifier}.dll",
                ModuleVersionId = new Guid($"00000000-0000-0000-0000-{identifier:D12}")
            });
        }
    }
    internal class StubNativeAOTStackFrame : IStackFrame
    {
        internal string? Function;
        internal string? Module;
        internal nint ImageBase;
        internal nint IP;

        public StackFrame? Frame => null;

        public int GetFileColumnNumber() => 0;

        public int GetFileLineNumber() => 0;

        public string? GetFileName() => null;

        public int GetILOffset() => StackFrame.OFFSET_UNKNOWN;

        public MethodBase? GetMethod() => null;

        public nint GetNativeImageBase() => ImageBase;

        public nint GetNativeIP() => IP;

        public bool HasNativeImage() => true;

        public override string ToString()
        {
            if (Function is not null && Module is not null)
            {
                return $"{Module}.{Function} + 0x{ImageBase:x} at offset 0x{IP - ImageBase:x} in file:line:column <filename unknown>:0:0";
            }
            else
            {
                return "<unknown>";
            }
        }
    }
}
