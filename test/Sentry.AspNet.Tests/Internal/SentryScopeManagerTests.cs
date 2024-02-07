using Sentry.AspNet.Internal;

namespace Sentry.AspNet.Tests.Internal;

public class SentryScopeManagerTests
{
    private static SentryScopeManager GetSut() => new(
        new SentryOptions
        {
            ScopeStackContainer = new HttpContextScopeStackContainer()
        },
        Substitute.For<ISentryClient>());

    [Fact]
    public void SetScopeStack_NoHttpContext_FallbackSet()
    {
        // Arrange
        var scopeManager = GetSut();

        // Act
        scopeManager.PushScope();

        // Assert
        Assert.NotNull(scopeManager.ScopeStackContainer.Stack);
    }

    [Fact]
    public void GetScopeStack_NoHttpContext_Null()
    {
        // Arrange
        var scopeManager = GetSut();

        // Assert
        Assert.Null(scopeManager.ScopeStackContainer.Stack);
    }
}
