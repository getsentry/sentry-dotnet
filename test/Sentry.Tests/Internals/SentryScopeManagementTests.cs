using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Sentry.Internals;
using Xunit;

namespace Sentry.Tests.Internals
{
    public class SentryScopeManagementTests
    {
        private class Fixture
        {
            public IScopeOptions ScopeOptions { get; set; } = Substitute.For<IScopeOptions>();
            public SentryScopeManagement GetSut() => new SentryScopeManagement(ScopeOptions);
        }

        private readonly Fixture _fixture = new Fixture();

        [Fact]
        public void GetCurrent_ReturnsInstance() => Assert.NotNull(_fixture.GetSut().GetCurrent());

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
            Assert.Same(Task.CompletedTask, sut.ConfigureScopeAsync(null));
        }

        [Fact]
        public void ConfigureScopeAsync_Callback_InvokesCallback()
        {
            var sut = _fixture.GetSut();
            var expectedTask = Task.FromResult(1);

            var actualTask = sut.ConfigureScopeAsync(scope => expectedTask);
            Assert.Same(expectedTask, actualTask);
        }

        [Fact]
        public void PushScope_Parameterless_SetsNewAsCurrent()
        {
            var sut = _fixture.GetSut();
            var first = sut.GetCurrent();
            sut.PushScope();
            var second = sut.GetCurrent();

            Assert.NotSame(first, second);
        }

        [Fact]
        public void PushScope_StateInstance_SetsNewAsCurrent()
        {
            var sut = _fixture.GetSut();
            var first = sut.GetCurrent();
            sut.PushScope(new object());
            var second = sut.GetCurrent();

            Assert.NotSame(first, second);
        }

        [Fact]
        public async Task NewTask_SeesRootScope()
        {
            var sut = _fixture.GetSut();
            var root = sut.GetCurrent();

            await Task.Run(async () =>
            {
                await Task.Yield();
                Assert.Same(root, sut.GetCurrent());
            });
        }

        [Fact]
        public void PushScope_Disposed_BackToParent()
        {
            var sut = _fixture.GetSut();
            var root = sut.GetCurrent();

            sut.PushScope().Dispose();
            Assert.Same(root, sut.GetCurrent());
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

            Assert.Same(root, sut.GetCurrent());
        }

        [Fact]
        public async Task AsyncTasks_IsolatedScopes()
        {
            var sut = _fixture.GetSut();
            var root = sut.GetCurrent();

            var evt = new AutoResetEvent(false);

            // Creates event second, disposes first
            var t1 = Task.Run(() =>
            {
                evt.Set(); // unblock task start

                // Wait t2 create scope
                evt.WaitOne();
                try
                {
                    // t2 created scope, make sure this parent is still root
                    Assert.Same(root, sut.GetCurrent());

                    var scope = sut.PushScope();
                    Assert.NotSame(root, sut.GetCurrent());
                    scope.Dispose();
                }
                finally
                {
                    evt.Set();
                }

                Assert.Same(root, sut.GetCurrent());
            });

            evt.WaitOne(); // Wait t1 start

            // Creates a scope first, disposes after t2
            var t2 = Task.Run(() =>
            {

                var scope = sut.PushScope();
                try
                {
                    // Create a scope first
                    Assert.NotSame(root, sut.GetCurrent());
                }
                finally
                {
                    evt.Set(); // release t1
                }

                // Wait for t1 to create and dispose its scope
                evt.WaitOne();
                scope.Dispose();

                Assert.Same(root, sut.GetCurrent());
            });

            await Task.WhenAll(t1, t2);

            Assert.Same(root, sut.GetCurrent());
        }

        [Fact]
        public async Task Async_IsolatedScopes()
        {
            var sut = _fixture.GetSut();
            var root = sut.GetCurrent();
            void AddRandomTag() => sut.GetCurrent().SetTag(Guid.NewGuid().ToString(), "1");
            void AssertTagCount(int count) => Assert.Equal(count, sut.GetCurrent().Tags.Count);

            AddRandomTag();
            AssertTagCount(1);
            var rdn = new Random();

            async Task Test(int i)
            {
                if (i > 5)
                    return;

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
                    .Select(__ => Test(1)))
                .SelectMany(t => t));

            Assert.Same(root, sut.GetCurrent());
        }
    }
}
