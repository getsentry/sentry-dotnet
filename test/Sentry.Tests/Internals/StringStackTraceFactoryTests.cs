// ReSharper disable once CheckNamespace
// Stack trace filters out Sentry frames by namespace
namespace Other.Tests.Internals;

#if PLATFORM_NEUTRAL

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
}

#endif
