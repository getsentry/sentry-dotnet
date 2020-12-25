using Sentry.Protocol;
using Xunit;

namespace Sentry.Tests.Protocol
{
    public class BaseScopeTests
    {
        private readonly Scope _sut = new Scope(new SentryOptions());

        [Fact]
        public void Fingerprint_ByDefault_ReturnsEmptyEnumerable()
        {
            Assert.Empty(_sut.Fingerprint);
        }

        [Fact]
        public void Tags_ByDefault_ReturnsEmpty()
        {
            Assert.Empty(_sut.Tags);
        }

        [Fact]
        public void Breadcrumbs_ByDefault_ReturnsEmpty()
        {
            Assert.Empty(_sut.Breadcrumbs);
        }

        [Fact]
        public void Sdk_ByDefault_ReturnsNotNull()
        {
            Assert.NotNull(_sut.Sdk);
        }

        [Fact]
        public void User_ByDefault_ReturnsNotNull()
        {
            Assert.NotNull(_sut.User);
        }

        [Fact]
        public void User_Settable()
        {
            var expected = new User();
            _sut.User = expected;
            Assert.Same(expected, _sut.User);
        }

        [Fact]
        public void Contexts_ByDefault_NotNull()
        {
            Assert.NotNull(_sut.Contexts);
        }

        [Fact]
        public void Contexts_Settable()
        {
            var expected = new Contexts();
            _sut.Contexts = expected;
            Assert.Same(expected, _sut.Contexts);
        }

        [Fact]
        public void Request_ByDefault_NotNull()
        {
            Assert.NotNull(_sut.Request);
        }

        [Fact]
        public void Request_Settable()
        {
            var expected = new Request();
            _sut.Request = expected;
            Assert.Same(expected, _sut.Request);
        }

        [Fact]
        public void Transaction_Settable()
        {
            var expected = "Transaction";
            _sut.TransactionName = expected;
            Assert.Same(expected, _sut.TransactionName);
        }

        [Fact]
        public void Environment_Settable()
        {
            var expected = "Environment";
            _sut.Environment = expected;
            Assert.Same(expected, _sut.Environment);
        }
    }
}
