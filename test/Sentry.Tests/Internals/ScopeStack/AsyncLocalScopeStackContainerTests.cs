using Sentry.Internal.ScopeStack;

namespace Sentry.Tests.Internals.ScopeStack;

public class AsyncLocalScopeStackContainerTests
{
    [Fact]
    public async Task Scopes_are_not_shared_between_parallel_async_executions()
    {
        // Arrange
        var container = new AsyncLocalScopeStackContainer();

        var scope1 = new KeyValuePair<Scope, ISentryClient>(
            Substitute.For<Scope>(),
            Substitute.For<ISentryClient>());

        var scope2 = new KeyValuePair<Scope, ISentryClient>(
            Substitute.For<Scope>(),
            Substitute.For<ISentryClient>());

        // Act & assert
        var task1 = Task.Run(async () =>
        {
            container.Stack.Should().BeNull();

            await Task.Yield();

            container.Stack.Should().BeNull();
        });

        var task2 = Task.Run(async () =>
        {
            container.Stack.Should().BeNull();

            container.Stack = new[] { scope1 };
            await Task.Yield();

            container.Stack.Should().BeEquivalentTo(new[] { scope1 });
        });

        var task3 = Task.Run(async () =>
        {
            container.Stack.Should().BeNull();

            container.Stack = new[] { scope2 };
            await Task.Yield();

            container.Stack.Should().BeEquivalentTo(new[] { scope2 });
        });

        await Task.WhenAll(task1, task2, task3);

        container.Stack.Should().BeNull();
    }

    [Fact]
    public async Task Scopes_are_not_shared_between_sequential_async_executions()
    {
        // Arrange
        var container = new AsyncLocalScopeStackContainer();

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

            await Task.Yield();

            container.Stack.Should().BeNull();
        });

        await Task.Run(async () =>
        {
            container.Stack.Should().BeNull();

            container.Stack = new[] { scope1 };
            await Task.Yield();

            container.Stack.Should().BeEquivalentTo(new[] { scope1 });
        });

        await Task.Run(async () =>
        {
            container.Stack.Should().BeNull();

            container.Stack = new[] { scope2 };
            await Task.Yield();

            container.Stack.Should().BeEquivalentTo(new[] { scope2 });
        });

        container.Stack.Should().BeNull();
    }

    [Fact]
    public async Task Scopes_are_shared_between_nested_async_executions()
    {
        // Arrange
        var container = new AsyncLocalScopeStackContainer();

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

            await Task.Yield();

            container.Stack.Should().BeNull();

            await Task.Run(async () =>
            {
                container.Stack.Should().BeNull();

                container.Stack = new[] { scope1 };
                await Task.Yield();

                container.Stack.Should().BeEquivalentTo(new[] { scope1 });

                await Task.Run(async () =>
                {
                    container.Stack.Should().BeEquivalentTo(new[] { scope1 });

                    await Task.Yield();
                    container.Stack = new[] { scope2 };

                    container.Stack.Should().BeEquivalentTo(new[] { scope2 });
                }).ConfigureAwait(false);
            }).ConfigureAwait(false);
        });

        container.Stack.Should().BeNull();
    }
}
