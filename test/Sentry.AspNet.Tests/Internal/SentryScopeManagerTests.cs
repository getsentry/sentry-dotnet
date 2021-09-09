using NSubstitute;
using Sentry.AspNet.Internal;
using Sentry.Internal;
using Xunit;

namespace Sentry.AspNet.Tests.Internal
{
    public class SentryScopeManagerTests
    {
        private class Fixture
        {
            public SentryScopeManager GetSut() => new SentryScopeManager(
                new HttpContextScopeStackContainer(),
                new SentryOptions(),
                Substitute.For<ISentryClient>());
        }

        private Fixture _fixture => new();

        [Fact]
        public void SetScopeStack_NoHttpContext_FallbackSet()
        {
            // Arrange
            var scopeManager = _fixture.GetSut();

            // Act
            scopeManager.PushScope();

            // Assert
            Assert.NotNull(scopeManager.ScopeStackContainer.Stack);
        }

        [Fact]
        public void GetScopeStack_NoHttpContext_Null()
        {
            // Arrange
            var scopeManager = _fixture.GetSut();

            // Assert
            Assert.Null(scopeManager.ScopeStackContainer.Stack);
        }
    }
}
