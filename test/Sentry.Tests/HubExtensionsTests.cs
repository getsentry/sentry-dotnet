using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NSubstitute;
using Sentry.Infrastructure;
using Sentry.Protocol;
using Xunit;

namespace Sentry.Tests
{
    public class HubExtensionsTests
    {
        public IHub Sut { get; set; } = Substitute.For<IHub>();
        public Scope Scope { get; set; } = new();

        public HubExtensionsTests()
        {
            Sut.When(h => h.ConfigureScope(Arg.Any<Action<Scope>>()))
                .Do(c => c.Arg<Action<Scope>>()(Scope));
        }

        [Fact]
        public void PushAndLockScope_PushesNewScope()
        {
            _ = Sut.PushAndLockScope();

            _ = Sut.Received(1).PushScope();
        }

        [Fact]
        public void PushAndLockScope_Disposed_DisposesInnerScope()
        {
            var disposable = Substitute.For<IDisposable>();
            _ = Sut.PushScope().Returns(disposable);

            var actual = Sut.PushAndLockScope();
            actual.Dispose();

            disposable.Received(1).Dispose();
        }

        [Fact]
        public void PushAndLockScope_CreatedScopeIsLocked()
        {
            _ = Sut.PushAndLockScope();

            Assert.True(Scope.Locked);
        }

        [Fact]
        public void LockScope_LocksScope()
        {
            Sut.LockScope();

            Assert.True(Scope.Locked);
        }

        [Fact]
        public void UnlockScope_UnlocksScope()
        {
            Sut.LockScope();

            Sut.UnlockScope();

            Assert.False(Scope.Locked);
        }

        [Fact]
        public void AddBreadcrumb_MinimalArguments_CreatesBreadcrumb()
        {
            const string expectedMessage = "message";
            Sut.AddBreadcrumb(expectedMessage);

            _ = Assert.Single(Scope.Breadcrumbs);
            var crumb = Scope.Breadcrumbs.Single();
            Assert.Equal(expectedMessage, crumb.Message);
            Assert.Null(crumb.Category);
            Assert.Null(crumb.Data);
            Assert.Null(crumb.Type);
            Assert.Equal(BreadcrumbLevel.Info, crumb.Level);
            Assert.NotEqual(default, crumb.Timestamp);
        }

        [Fact]
        public void AddBreadcrumb_AllFields_CreatesBreadcrumb()
        {
            var expectedTimestamp = DateTimeOffset.MaxValue;
            var clock = Substitute.For<ISystemClock>();
            _ = clock.GetUtcNow().Returns(expectedTimestamp);

            const string expectedMessage = "message";
            const string expectedType = "type";
            const string expectedCategory = "category";
            var expectedData = new Dictionary<string, string>
            {
                {"Key", null},
                {"Key2", "value2"},
            };
            const BreadcrumbLevel expectedLevel = BreadcrumbLevel.Critical;

            Sut.AddBreadcrumb(
                clock,
                expectedMessage,
                expectedCategory,
                expectedType,
                expectedData,
                expectedLevel);

            _ = Assert.Single(Scope.Breadcrumbs);
            var crumb = Scope.Breadcrumbs.First();

            Assert.Equal(expectedMessage, crumb.Message);
            Assert.Equal(expectedType, crumb.Type);
            Assert.Equal(expectedCategory, crumb.Category);
            Assert.Equal(expectedLevel, crumb.Level);
            Assert.Equal(expectedData.Count, crumb.Data.Count);
            Assert.Equal(expectedData.ToImmutableDictionary(), crumb.Data);
            Assert.Equal(expectedMessage, crumb.Message);
            Assert.Equal(expectedTimestamp, crumb.Timestamp);
        }
    }
}
