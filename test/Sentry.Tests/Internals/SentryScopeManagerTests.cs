using Sentry.Internal.ScopeStack;

namespace Sentry.Tests.Internals;

public class SentryScopeManagerTests
{
    private class Fixture
    {
        public SentryOptions SentryOptions { get; set; } = new()
        {
            IsGlobalModeEnabled = false
        };

        public ISentryClient Client { get; set; } = Substitute.For<ISentryClient>();

        public SentryScopeManager GetSut() => new(SentryOptions, Client);
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void GetCurrent_Scope_ReturnsInstance() => Assert.NotNull(_fixture.GetSut().GetCurrent().Key);

    [Fact]
    public void GetCurrent_Client_ReturnsInstance() => Assert.NotNull(_fixture.GetSut().GetCurrent().Value);

    [Fact]
    public void GetCurrent_Equality_SameOnInstance()
    {
        var sut = _fixture.GetSut();

        var root = sut.GetCurrent();

        Assert.Equal(root, sut.GetCurrent());
    }

    [Fact]
    public void GetCurrent_Equality_FalseOnModifiedClient()
    {
        var sut = _fixture.GetSut();

        var root = sut.GetCurrent();
        sut.BindClient(Substitute.For<ISentryClient>());

        Assert.NotEqual(root, sut.GetCurrent());
    }

    [Fact]
    public void GetCurrent_Equality_FalseOnModifiedScope()
    {
        var sut = _fixture.GetSut();

        var root = sut.GetCurrent();
        _ = sut.PushScope();

        Assert.NotEqual(root, sut.GetCurrent());
    }

    [Fact]
    public void BindClient_Null_DisablesClient()
    {
        var sut = _fixture.GetSut();

        sut.BindClient(null);
        var currentScope = sut.GetCurrent();

        Assert.Same(DisabledHub.Instance, currentScope.Value);
    }

    [Fact]
    public void BindClient_Scope_StaysTheSame()
    {
        var sut = _fixture.GetSut();
        var currentScope = sut.GetCurrent();

        sut.BindClient(Substitute.For<ISentryClient>());
        Assert.Same(currentScope.Key, sut.GetCurrent().Key);
    }

    [Fact]
    public void BindClient_ScopeState_StaysTheSame()
    {
        var sut = _fixture.GetSut();
        var currentScope = sut.GetCurrent();

        var scope1 = sut.PushScope(1);
        var scope2 = sut.PushScope(2);
        Assert.Equal(2, sut.GetCurrent().Key.Data["state"]);

        sut.BindClient(Substitute.For<ISentryClient>());

        Assert.Equal(2, sut.GetCurrent().Key.Data["state"]);

        scope2.Dispose();
        Assert.Equal(1, sut.GetCurrent().Key.Data["state"]);
    }

    [Fact]
    public void ConfigureScope_NullArgument_NoOp()
    {
        var sut = _fixture.GetSut();
        sut.ConfigureScope(null);
    }

    [Fact]
    public void ConfigureScopeAsync_NullArgument_ReturnsCompletedTask()
    {
        var sut = _fixture.GetSut();
        Assert.Equal(Task.CompletedTask, sut.ConfigureScopeAsync(null));
    }

    [Fact]
    public async Task ConfigureScopeAsync_Callback_InvokesCallback()
    {
        var sut = _fixture.GetSut();
        var isInvoked = false;

        await sut.ConfigureScopeAsync(_ =>
        {
            isInvoked = true;
            return Task.CompletedTask;
        });

        Assert.True(isInvoked);
    }

    [Fact]
    public void PushScope_Parameterless_SetsNewAsCurrent()
    {
        var sut = _fixture.GetSut();
        var first = sut.GetCurrent();
        _ = sut.PushScope();
        var second = sut.GetCurrent();

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void PushScope_Parameterless_UsesSameClient()
    {
        var sut = _fixture.GetSut();
        var firstScope = sut.GetCurrent();
        _ = sut.PushScope();
        var secondScope = sut.GetCurrent();

        Assert.Same(firstScope.Value, secondScope.Value);
    }

    [Fact]
    public void PushScope_StateInstance_UsesSameClient()
    {
        var sut = _fixture.GetSut();
        var firstScope = sut.GetCurrent();
        _ = sut.PushScope(new object());
        var secondScope = sut.GetCurrent();

        Assert.Same(firstScope.Value, secondScope.Value);
    }

    [Fact]
    public void PushScope_StateInstance_SetsNewAsCurrent()
    {
        var sut = _fixture.GetSut();
        var first = sut.GetCurrent();
        _ = sut.PushScope(new object());
        var second = sut.GetCurrent();

        Assert.NotEqual(first, second);
    }

    [Fact]
    public async Task NewTask_SeesRootScope()
    {
        var sut = _fixture.GetSut();
        var root = sut.GetCurrent();

        await Task.Run(async () =>
        {
            await Task.Yield();
            Assert.Equal(root, sut.GetCurrent());
        });
    }

    [Fact]
    public void PushScope_Disposed_BackToParent()
    {
        var sut = _fixture.GetSut();
        var root = sut.GetCurrent();

        sut.PushScope().Dispose();
        Assert.Equal(root, sut.GetCurrent());
    }

    [Fact]
    public void Scope_DisposedOutOfOrder()
    {
        var sut = _fixture.GetSut();
        var root = sut.GetCurrent();

        var first = sut.PushScope();
        var second = sut.PushScope();

        first.Dispose();
        second.Dispose();

        Assert.Equal(root, sut.GetCurrent());
    }

    [Fact]
    public void Scope_Clear_DoesntAffectParentScope()
    {
        var sut = _fixture.GetSut();

        //Set some values on the scope (and store these to compare later)
        sut.GetCurrent().Key.ApplyFakeValues();
        Scope fakeValues = sut.GetCurrent().Key.Clone();
        fakeValues.Transaction.Should().NotBeNull(); // Sanity check... make sure it's not nulls everywhere

        //Push a new scope
        var root = sut.PushScope();

        // Ensure the values are there
        using (new AssertionScope())
        {
            sut.GetCurrent().Key.ShouldBeEquivalentTo(fakeValues);
        }

        // Clear the child scope
        sut.GetCurrent().Key.Clear();

        //Ensure the values are gone
        using (new AssertionScope())
        {
            sut.GetCurrent().Key.ShouldBeEquivalentTo(new Scope());
        }

        //Pop the scope
        root.Dispose();

        //Ensure the value have returned
        using (new AssertionScope())
        {
            sut.GetCurrent().Key.ShouldBeEquivalentTo(fakeValues);
        }
    }

    [Fact]
    public async Task AsyncTasks_IsolatedScopes()
    {
        var sut = _fixture.GetSut();
        var root = sut.GetCurrent();

        var t1Evt = new ManualResetEvent(false);
        var t2Evt = new ManualResetEvent(false);

        // Creates event second, disposes first
        var t1 = Task.Run(() =>
        {
            _ = t1Evt.Set(); // unblock task start

            // Wait t2 create scope
            _ = t2Evt.WaitOne();
            try
            {
                // t2 created scope, make sure this parent is still root
                Assert.Equal(root, sut.GetCurrent());

                var scope = sut.PushScope();

                Assert.NotEqual(root, sut.GetCurrent());

                scope.Dispose();
            }
            finally
            {
                _ = t1Evt.Set();
            }

            Assert.Equal(root, sut.GetCurrent());
        });

        _ = t1Evt.WaitOne(); // Wait t1 start
        _ = t1Evt.Reset();

        // Creates a scope first, disposes after t2
        var t2 = Task.Run(() =>
        {

            var scope = sut.PushScope();
            try
            {
                // Create a scope first
                Assert.NotEqual(root, sut.GetCurrent());
            }
            finally
            {
                _ = t2Evt.Set(); // release t1
            }

            // Wait for t1 to create and dispose its scope
            _ = t1Evt.WaitOne();
            scope.Dispose();

            Assert.Equal(root, sut.GetCurrent());
        });

        await Task.WhenAll(t1, t2);

        Assert.Equal(root, sut.GetCurrent());
    }

    [Fact]
    public async Task Async_IsolatedScopes()
    {
        var sut = _fixture.GetSut();
        var root = sut.GetCurrent();
        void AddRandomTag() => sut.GetCurrent().Key.SetTag(Guid.NewGuid().ToString(), "1");
        void AssertTagCount(int count) => Assert.Equal(count, sut.GetCurrent().Key.Tags.Count);

        AddRandomTag();
        AssertTagCount(1);
        var rdn = new Random();

        async Task Test(int i)
        {
            if (i > 5)
            {
                return;
            }

            AssertTagCount(i);
            using (sut.PushScope())
            {
                AddRandomTag();
                AssertTagCount(i + 1);
                await Test(i + 1);
                // Reorder
                await Task.Delay(rdn.Next(0, 5));
                AddRandomTag();
                AssertTagCount(i + 2);
                await Test(i + 2);
            }
            AssertTagCount(i);
        }

        await Task.WhenAll(Enumerable.Range(1, 5)
            .Select(_ => Enumerable.Range(1, 5)
                .Select(_ => Test(1)))
            .SelectMany(t => t));

        Assert.Equal(root, sut.GetCurrent());
    }

    [Fact]
    public void GlobalMode_PushScope_SameScope()
    {
        // Arrange
        _fixture.SentryOptions.IsGlobalModeEnabled = true;
        var sut = _fixture.GetSut();

        // Act
        var (scope1, client1) = sut.GetCurrent();
        using var _ = sut.PushScope();
        var (scope2, client2) = sut.GetCurrent();

        // Assert
        scope1.Should().BeSameAs(scope2);
        client1.Should().BeSameAs(client2);
    }


    [Fact]
    public void GlobalMode_Disabled_Uses_AsyncLocalScopeStackContainer()
    {
        _fixture.SentryOptions.IsGlobalModeEnabled = false;
        var sut = _fixture.GetSut();
        Assert.IsType<AsyncLocalScopeStackContainer>(sut.ScopeStackContainer);
    }

    [Fact]
    public void GlobalMode_Enabled_Uses_GlobalScopeStackContainer()
    {
        _fixture.SentryOptions.IsGlobalModeEnabled = true;
        var sut = _fixture.GetSut();
        Assert.IsType<GlobalScopeStackContainer>(sut.ScopeStackContainer);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Uses_Provided_ScopeStackContainer_Ignoring_GlobalMode(bool globalModeSetting)
    {
        var container = Substitute.For<IScopeStackContainer>();
        _fixture.SentryOptions.ScopeStackContainer = container;
        _fixture.SentryOptions.IsGlobalModeEnabled = globalModeSetting;
        var sut = _fixture.GetSut();
        Assert.Same(container, sut.ScopeStackContainer);
    }
}
