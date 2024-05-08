namespace Sentry.Tests.Protocol.Exceptions;

public class SentryStackFrameTests
{
    private readonly IDiagnosticLogger _testOutputLogger;

    public SentryStackFrameTests(ITestOutputHelper output)
    {
        _testOutputLogger = new TestOutputDiagnosticLogger(output);
    }

    [Fact]
    public void SerializeObject_AllPropertiesSetToNonDefault_SerializesValidObject()
    {
        var sut = new SentryStackFrame
        {
            FileName = "FileName",
            Function = "Function",
            Module = "Module",
            LineNumber = 1,
            ColumnNumber = 2,
            AbsolutePath = "AbsolutePath",
            ContextLine = "ContextLine",
            PreContext = { "pre" },
            PostContext = { "post" },
            InApp = true,
            Vars = { { "var", "val" } },
            FramesOmitted = { 1, 2 },
            Package = "Package",
            Platform = "Platform",
            ImageAddress = 3,
            SymbolAddress = 4,
            InstructionAddress = 5,
            AddressMode = "rel:0"
        };

        var actual = sut.ToJsonString(_testOutputLogger, indented: true);

        Assert.Equal("""
            {
              "pre_context": [
                "pre"
              ],
              "post_context": [
                "post"
              ],
              "vars": {
                "var": "val"
              },
              "frames_omitted": [
                1,
                2
              ],
              "filename": "FileName",
              "function": "Function",
              "module": "Module",
              "lineno": 1,
              "colno": 2,
              "abs_path": "AbsolutePath",
              "context_line": "ContextLine",
              "in_app": true,
              "package": "Package",
              "platform": "Platform",
              "image_addr": "0x3",
              "symbol_addr": "0x4",
              "instruction_addr": "0x5",
              "addr_mode": "rel:0"
            }
            """,
            actual);

        var parsed = Json.Parse(actual, SentryStackFrame.FromJson);

        parsed.Should().BeEquivalentTo(sut);
    }

    [Fact]
    public void PreContext_Getter_NotNull()
    {
        var sut = new SentryStackFrame();
        Assert.NotNull(sut.PreContext);
    }

    [Fact]
    public void PostContext_Getter_NotNull()
    {
        var sut = new SentryStackFrame();
        Assert.NotNull(sut.PostContext);
    }

    [Fact]
    public void Vars_Getter_NotNull()
    {
        var sut = new SentryStackFrame();
        Assert.NotNull(sut.Vars);
    }

    [Fact]
    public void FramesOmitted_Getter_NotNull()
    {
        var sut = new SentryStackFrame();
        Assert.NotNull(sut.FramesOmitted);
    }

    [Fact]
    public void ConfigureAppFrame_InAppIncludeMatches_TrueSet()
    {
        // Arrange
        var module = "IncludedModule";
        var sut = new SentryStackFrame
        {
            Module = module
        };
        var options = new SentryOptions();
        options.AddInAppInclude(module);

        // Act
        sut.ConfigureAppFrame(options);

        // Assert
        Assert.True(sut.InApp);
    }

    [Fact]
    public void ConfigureAppFrame_InAppExcludeMatches_TrueSet()
    {
        // Arrange
        var module = "ExcludedModule";
        var sut = new SentryStackFrame
        {
            Module = module
        };
        var options = new SentryOptions();
        options.AddInAppExclude(module);

        // Act
        sut.ConfigureAppFrame(options);

        // Assert
        Assert.False(sut.InApp);
    }

    [Fact]
    public void ConfigureAppFrame_InAppRuleDoesntMatch_TrueSet()
    {
        // Arrange
        var module = "AppModule";
        var sut = new SentryStackFrame
        {
            Module = module
        };
        var options = new SentryOptions();

        // Act
        sut.ConfigureAppFrame(options);

        // Assert
        Assert.True(sut.InApp);
    }

    [Theory]
    [InlineData("Namespace", ".*", true)] // Substring + Regex
    [InlineData("OtherNamespace", ".*", false)] // Substring + Regex
    [InlineData("Name.*", ".*", true)] // Regex + Regex
    [InlineData("OtherName.*", ".*", false)] // Regex + Regex
    [InlineData(@"Namespace\..*", "Foo", true)] // Regex + Substring
    [InlineData(@"OtherNamespace\..*", "Namespace", false)] // Regex + Substring
    public void ConfigureAppFrame_InAppRuleExclude_MatchesRegex(string include, string exclude, bool shouldMatch)
    {
        // Arrange
        var module = "Namespace.AppModule";
        var sut = new SentryStackFrame
        {
            Module = module
        };
        var options = new SentryOptions
        {
            InAppInclude = [new Regex(include)],
            InAppExclude = [new Regex(exclude)]
        };

        // Act
        sut.ConfigureAppFrame(options);

        // Assert
        sut.InApp.Should().Be(shouldMatch);
    }

    [Fact]
    public void ConfigureAppFrame_WithDefaultOptions_RecognizesInAppFrame()
    {
        var options = new SentryOptions();
        var sut = new SentryStackFrame()
        {
            Function = "Program.<Main>() {QuickJitted}",
            Module = "Console.Customized"
        };

        // Act
        sut.ConfigureAppFrame(options);

        // Assert
        Assert.True(sut.InApp);
    }

    // Sentry internal frame is marked properly with default options.
    // This is an actual frame as captured by Sentry.Profiling.
    [Fact]
    public void ConfigureAppFrame_WithDefaultOptions_RecognizesSentryInternalFrame()
    {
        var options = new SentryOptions();
        var sut = new SentryStackFrame()
        {
            Function = "Sentry.Internal.Hub.StartTransaction(class Sentry.ITransactionContext,class System.Collections.Generic.IReadOnlyDictionary`2<class System.String,class System.Object>) {QuickJitted}",
            Module = "Sentry"
        };

        // Act
        sut.ConfigureAppFrame(options);

        // Assert
        Assert.False(sut.InApp);
    }

    [Fact]
    public void ConfigureAppFrame_InAppAlreadySet_InAppIgnored()
    {
        // Arrange
        var module = "ExcludedModule";
        var sut = new SentryStackFrame
        {
            Module = module
        };
        var options = new SentryOptions();
        options.AddInAppExclude(module);
        sut.InApp = true;

        // Act
        sut.ConfigureAppFrame(options);

        // Assert
        Assert.True(sut.InApp, "InApp started as true but ConfigureAppFrame changed it to false.");
    }

    [Fact]
    public void ConfigureAppFrame_NativeAOTWithoutMethodInfo_InAppIsNull()
    {
        // See values set by TryCreateNativeAOTFrame
        var sut = new SentryStackFrame
        {
            ImageAddress = 1,
            InstructionAddress = 2
        };

        // Act
        sut.ConfigureAppFrame(new());

        // Assert
        Assert.Null(sut.InApp);
    }

#if NET8_0_OR_GREATER
    [Fact]
    public void ConfigureAppFrame_NativeAOTWithoutMethodInfo_InAppIsSet()
    {
        var sut = DebugStackTrace.ParseNativeAOTToString(
            "System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task) + 0x42 at offset 66 in file:line:column <filename unknown>:0:0");
        sut.ConfigureAppFrame(new());
        Assert.False(sut.InApp);

        sut = DebugStackTrace.ParseNativeAOTToString(
            "Program.<<Main>$>d__0.MoveNext() + 0xdd at offset 221 in file:line:column <filename unknown>:0:0");
        sut.ConfigureAppFrame(new());
        Assert.True(sut.InApp);
    }
#endif
}
