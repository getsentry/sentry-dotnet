// ReSharper disable once CheckNamespace
// Stack trace filters out Sentry frames by namespace
namespace Other.Tests.Internals;

// TODO: Create integration test to test this behaviour when publishing AOT apps
// See https://github.com/getsentry/sentry-dotnet/pull/2732#discussion_r1371006441
public class SimpleStackTraceFactoryTests
{
    private class Fixture
    {
        public SentryOptions SentryOptions { get; } = new();
        public SentryStackTraceFactory GetSut() => new(SentryOptions);
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public Task MethodGeneric()
    {
        _fixture.SentryOptions.UseStackTraceFactory(new SimpleStackTraceFactory(_fixture.SentryOptions));

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
            x => x.InstructionAddress,
            x => x.FunctionId)
           .AddScrubber(x => x.Replace(@"\", @"/"));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void GenericMethodThatThrows<T>(T value) =>
        throw new Exception();
}
