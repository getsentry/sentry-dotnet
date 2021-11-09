using Sentry.Internal.ScopeStack;

namespace Sentry.Tests.Internals.ScopeStack;

public class GlobalScopeStackContainerTests
{
    [Fact]
    public async Task Scopes_are_shared_between_parallel_async_executions()
    {
        // Arrange
        var container = new GlobalScopeStackContainer();

        var scope1 = new KeyValuePair<Scope, ISentryClient>(
            Substitute.For<Scope>(),
            Substitute.For<ISentryClient>());

        var scope2 = new KeyValuePair<Scope, ISentryClient>(
            Substitute.For<Scope>(),
            Substitute.For<ISentryClient>());

        // Act & assert
        await Task.Run(async () =>
        {
            container.Stack.Should().BeNull();

            container.Stack = new[] { scope1, scope2 };
            await Task.Yield();

            container.Stack.Should().BeEquivalentTo(new[] { scope1, scope2 });
        });

        container.Stack.Should().BeEquivalentTo(new[] { scope1, scope2 });
    }
}
