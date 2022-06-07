namespace Sentry.Tests.Protocol.Exceptions;

public class SentryStackFrameTests
{
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
            InstructionOffset = 5,
            InstructionAddress = "0xffffffff",
            AddressMode = "rel:0"
        };

        var actual = sut.ToJsonString();

        Assert.Equal(
            "{" +
            "\"pre_context\":[\"pre\"]," +
            "\"post_context\":[\"post\"]," +
            "\"vars\":{\"var\":\"val\"}," +
            "\"frames_omitted\":[1,2]," +
            "\"filename\":\"FileName\"," +
            "\"function\":\"Function\"," +
            "\"module\":\"Module\"," +
            "\"lineno\":1," +
            "\"colno\":2," +
            "\"abs_path\":\"AbsolutePath\"," +
            "\"context_line\":\"ContextLine\"," +
            "\"in_app\":true," +
            "\"package\":\"Package\"," +
            "\"platform\":\"Platform\"," +
            "\"image_addr\":\"0x3\"," +
            "\"symbol_addr\":\"0x4\"," +
            "\"instruction_addr\":\"0xffffffff\"," +
            "\"instruction_offset\":5," +
            "\"addr_mode\":\"rel:0\"" +
            "}",
            actual);

        var parsed = SentryStackFrame.FromJson(Json.Parse(actual));

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
        var sut = new SentryStackFrame();
        sut.Module = module;
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
        var sut = new SentryStackFrame();
        sut.Module = module;
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
        var sut = new SentryStackFrame();
        sut.Module = module;
        var options = new SentryOptions();

        // Act
        sut.ConfigureAppFrame(options);

        // Assert
        Assert.True(sut.InApp);
    }

    [Fact]
    public void ConfigureAppFrame_InAppAlreadySet_InAppIgnored()
    {
        // Arrange
        var module = "ExcludedModule";
        var sut = new SentryStackFrame();
        sut.Module = module;
        var options = new SentryOptions();
        options.AddInAppExclude(module);
        sut.InApp = true;

        // Act
        sut.ConfigureAppFrame(options);

        // Assert
        Assert.True(sut.InApp, "InApp started as true but ConfigureAppFrame changed it to false.");
    }
}
