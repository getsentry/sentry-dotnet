namespace Other.Tests.Internals;

public partial class DebugStackTraceTests
{
    private class Fixture
    {
        public SentryOptions SentryOptions { get; set; } = new();
        public InjectableDebugStackTrace GetSut() => new(SentryOptions);
    }

    private readonly Fixture _fixture = new();
    private static readonly string ThisNamespace = typeof(SentryStackTraceFactoryTests).Namespace;

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
        var checkSut1IsUnchanged = () =>
        {
            Assert.Equal(3, sut1.Frames.Count);
            Assert.Equal("1", sut1.Frames[0].Function);
            Assert.Equal("rel:0", sut1.Frames[0].AddressMode);
            Assert.Equal("2", sut1.Frames[1].Function);
            Assert.Equal("rel:1", sut1.Frames[1].AddressMode);
            Assert.Equal("3", sut1.Frames[2].Function);
            Assert.Equal("rel:0", sut1.Frames[2].AddressMode);
        };

        var e = new SentryEvent();
        sut1.MergeDebugImagesInto(e);
        Assert.Equal(2, e.DebugImages.Count);
        Assert.Equal("1111.dll", e.DebugImages[0].CodeFile);
        Assert.Equal("2222.dll", e.DebugImages[1].CodeFile);

        // Stack trace isn't changed - there were no DebugImage relocations yet.
        checkSut1IsUnchanged();

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
        checkSut1IsUnchanged();

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
    }

    private class InjectableDebugStackTrace : DebugStackTrace
    {
        public InjectableDebugStackTrace(SentryOptions options) : base(options) { }

        public void Inject(int identifier)
        {
            _debugImages.Add(new DebugImage
            {
                CodeFile = $"{identifier}.dll",
                ModuleVersionId = new Guid("00000000-0000-0000-0000-" + identifier.ToString("D12"))
            });
        }
    }
}
