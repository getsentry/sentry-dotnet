// ReSharper disable once CheckNamespace
// Stack trace filters out Sentry frames by namespace
namespace Other.Tests.Internals;

#if PLATFORM_NEUTRAL && NET8_0_OR_GREATER

public class StringStackTraceFactoryTests
{
    [Fact]
    public Task MethodGeneric()
    {
        // Arrange
        const int i = 5;
        var exception = Record.Exception(() => GenericMethodThatThrows(i));

        var options = new SentryOptions
        {
            AttachStacktrace = true
        };
        var factory = new StringStackTraceFactory(options);

        // Act
        var stackTrace = factory.Create(exception);

        // Assert;
        var frame = stackTrace!.Frames.Single(x => x.Function!.Contains("GenericMethodThatThrows"));
        return Verify(frame)
            .IgnoreMembers<SentryStackFrame>(
            x => x.Package,
            x => x.LineNumber,
            x => x.ColumnNumber,
            x => x.InstructionAddress,
            x => x.FunctionId)
           .AddScrubber(x => x.Replace(@"\", @"/"));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void GenericMethodThatThrows<T>(T value) =>
        throw new Exception();

    [Theory]
    [InlineData("at MyNamespace.MyClass.MyMethod in /path/to/file.cs:line 42", "MyNamespace", "MyClass.MyMethod", "/path/to/file.cs", "42")]
    [InlineData("at Foo.Bar.Baz in C:\\code\\foo.cs:line 123", "Foo", "Bar.Baz", "C:\\code\\foo.cs", "123")]
    public void FullStackTraceLine_ValidInput_Matches(
        string input, string expectedModule, string expectedFunction, string expectedFile, string expectedLine)
    {
        var match = StringStackTraceFactory.FullStackTraceLine.Match(input);
        Assert.True(match.Success);
        Assert.Equal(expectedModule, match.Groups["Module"].Value);
        Assert.Equal(expectedFunction, match.Groups["Function"].Value);
        Assert.Equal(expectedFile, match.Groups["FileName"].Value);
        Assert.Equal(expectedLine, match.Groups["LineNo"].Value);
    }

    [Theory]
    [InlineData("at MyNamespace.MyClass.MyMethod +")]
    [InlineData("random text")]
    [InlineData("at . in :line ")]
    public void FullStackTraceLine_InvalidInput_DoesNotMatch(string input)
    {
        var match = StringStackTraceFactory.FullStackTraceLine.Match(input);
        Assert.False(match.Success);
    }
}

#endif
