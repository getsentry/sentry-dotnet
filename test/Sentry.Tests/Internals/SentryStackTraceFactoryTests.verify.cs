using Sentry.PlatformAbstractions;

// ReSharper disable once CheckNamespace
// Stack trace filters out Sentry frames by namespace
namespace Other.Tests.Internals;

// TODO: Create integration test to test this behaviour when publishing AOT apps
// See https://github.com/getsentry/sentry-dotnet/pull/2732#discussion_r1371006441
#if !TRIMMABLE
[UsesVerify]
public partial class SentryStackTraceFactoryTests
{
    [SkippableTheory]
    [InlineData(StackTraceMode.Original)]
    [InlineData(StackTraceMode.Enhanced)]
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
        return Verify(frame)
            .IgnoreMembers<SentryStackFrame>(
                x => x.Package,
                x => x.LineNumber,
                x => x.ColumnNumber,
                x => x.InstructionAddress,
                x => x.FunctionId)
            .AddScrubber(x => x.Replace(@"\", @"/"))
            .UseParameters(mode);
    }
}
#endif
