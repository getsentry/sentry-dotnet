#if ANDROID
namespace Sentry.Tests.Platforms.Android;

[CollectionDefinition(nameof(ReplaySessionTests), DisableParallelization = true)]
public class ReplaySessionTestsCollection;

[Collection(nameof(ReplaySessionTests))]
public class ReplaySessionTests
{
    [Fact]
    public void ReadJavaScopeReplayId_GlobalScopeHasReplayId_ReturnsIt()
    {
        var scopes = JavaSdk.ScopesAdapter.Instance!;
        var empty = JavaSdk.Protocol.SentryId.EmptyId!;
        var current = scopes.Scope.ReplayId;
        var isolation = scopes.IsolationScope.ReplayId;
        var global = scopes.GlobalScope.ReplayId;
        var replayId = SentryId.Create();
        try
        {
            scopes.Scope.ReplayId = empty;
            scopes.IsolationScope.ReplayId = empty;
            scopes.GlobalScope.ReplayId = new JavaSdk.Protocol.SentryId(replayId.ToString());

            ReplaySession.ReadJavaScopeReplayId().Should().Be(replayId);
        }
        finally
        {
            scopes.Scope.ReplayId = current;
            scopes.IsolationScope.ReplayId = isolation;
            scopes.GlobalScope.ReplayId = global;
        }
    }
}
#endif
