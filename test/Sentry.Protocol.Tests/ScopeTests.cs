using System;
using NSubstitute;
using Xunit;

namespace Sentry.Protocol.Tests
{
    public class ScopeTests
    {
        private readonly Scope _sut = new Scope();

        [Fact]
        public void OnEvaluate_FiresOnlyOnce()
        {
            var counter = 0;
            _sut.OnEvaluating += (sender, args) => counter++;

            _sut.Evaluate();
            _sut.Evaluate();

            Assert.Equal(1, counter);
        }

        [Fact]
        public void OnEvaluate_NoEventHandler_DoesNotReevaluate()
        {
            var counter = 0;
            _sut.Evaluate();

            _sut.OnEvaluating += (sender, args) => counter++;

            _sut.Evaluate();

            Assert.Equal(0, counter);
        }

        [Fact]
        public void OnEvaluate_EventHandlerThrows_ExceptionAsBreadcrumb()
        {
            var expected = new InvalidOperationException("test");

            _sut.OnEvaluating += (sender, args) => throw expected;
            _sut.Evaluate();

            var crumb = Assert.Single(_sut.Breadcrumbs);

            Assert.Equal(BreadcrumbLevel.Error, crumb.Level);

            Assert.Equal(
                "Failed invoking event handler: " + expected,
                crumb.Message);
        }

        [Fact]
        public void OnEvaluate_EventHandlerThrows_DoesNotReevaluate()
        {
            var counter = 0;

            _sut.OnEvaluating += (sender, args) =>
            {
                counter++;
                throw new InvalidOperationException("test");
            };

            _sut.Evaluate();
            _sut.Evaluate();

            Assert.Equal(1, counter);
        }

        [Fact]
        public void OnEvaluate_HasEvaluatedProperty_True()
        {
            Assert.False(_sut.HasEvaluated);
            _sut.Evaluate();
            Assert.True(_sut.HasEvaluated);
        }

        [Fact]
        public void Clone_NewScope_IncludesOptions()
        {
            var options = Substitute.For<IScopeOptions>();
            var sut = new Scope(options);

            var clone = sut.Clone();

            Assert.Same(options, clone.Options);
        }

        [Fact]
        public void Clone_CopiesFields()
        {
            _sut.Environment = "test";

            var clone = _sut.Clone();

            Assert.Equal(_sut.Environment, clone.Environment);
        }

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
            _sut.Transaction = expected;
            Assert.Same(expected, _sut.Transaction);
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
