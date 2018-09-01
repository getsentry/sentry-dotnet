using System;
using Sentry.Protocol;
using Xunit;

namespace Sentry.Tests
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
    }
}
