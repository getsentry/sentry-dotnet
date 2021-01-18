using System;
using FluentAssertions;
using Sentry.Extensibility;
using Sentry.Protocol;
using Xunit;

namespace Sentry.Tests
{
    public class ScopeTests
    {
        private readonly Scope _sut = new();

        [Fact]
        public void OnEvaluate_FiresOnlyOnce()
        {
            var counter = 0;
            _sut.OnEvaluating += (_, _) => counter++;

            _sut.Evaluate();
            _sut.Evaluate();

            Assert.Equal(1, counter);
        }

        [Fact]
        public void OnEvaluate_NoEventHandler_DoesNotReevaluate()
        {
            var counter = 0;
            _sut.Evaluate();

            _sut.OnEvaluating += (_, _) => counter++;

            _sut.Evaluate();

            Assert.Equal(0, counter);
        }

        [Fact]
        public void OnEvaluate_EventHandlerThrows_ExceptionAsBreadcrumb()
        {
            var expected = new InvalidOperationException("test");

            _sut.OnEvaluating += (_, _) => throw expected;
            _sut.Evaluate();

            var crumb = Assert.Single(_sut.Breadcrumbs);

            Assert.Equal(BreadcrumbLevel.Error, crumb!.Level);

            Assert.Equal(
                "Failed invoking event handler: " + expected,
                crumb.Message);
        }

        [Fact]
        public void OnEvaluate_EventHandlerThrows_DoesNotReevaluate()
        {
            var counter = 0;

            _sut.OnEvaluating += (_, _) =>
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
            var options = new SentryOptions();
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
        public void TransactionName_TransactionNotStarted_NameIsSet()
        {
            // Arrange
            var scope = new Scope();

            // Act
            scope.TransactionName = "foo";

            // Assert
            scope.TransactionName.Should().Be("foo");
            scope.Transaction.Should().BeNull();
        }

        [Fact]
        public void TransactionName_TransactionStarted_NameIsSetAndOverwritten()
        {
            // Arrange
            var scope = new Scope();
            scope.Transaction = new Transaction(DisabledHub.Instance);
            scope.Transaction.Name = "bar";

            // Act
            scope.TransactionName = "foo";

            // Assert
            scope.TransactionName.Should().Be("foo");
            scope.TransactionName.Should().Be(scope.Transaction?.Name);
        }

        [Fact]
        public void TransactionName_TransactionStarted_NameIsSetToNullCoercedToEmpty()
        {
            // Arrange
            var scope = new Scope();
            scope.Transaction = new Transaction(DisabledHub.Instance);
            scope.Transaction.Name = "bar";

            // Act
            scope.TransactionName = null;

            // Assert
            scope.TransactionName.Should().BeNullOrEmpty();
            scope.TransactionName.Should().Be(scope.Transaction?.Name);
        }

        [Fact]
        public void TransactionName_TransactionStarted_NameReturnsActualTransactionName()
        {
            // Arrange
            var scope = new Scope();

            scope.TransactionName = "bar";

            // Act
            scope.Transaction = new Transaction(DisabledHub.Instance);
            scope.Transaction.Name = "foo";

            // Assert
            scope.TransactionName.Should().Be("foo");
            scope.TransactionName.Should().Be(scope.Transaction?.Name);
        }
    }
}
