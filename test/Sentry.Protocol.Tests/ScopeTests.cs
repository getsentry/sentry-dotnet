using System;
using System.Linq;
using NSubstitute;
using Xunit;

namespace Sentry.Protocol.Tests
{
    public class ScopeTests
    {
        [Fact]
        public void OnEvaluate_FiresOnlyOnce()
        {
            var counter = 0;
            var sut = new Scope();
            sut.OnEvaluating += (sender, args) => counter++;

            sut.Evaluate();
            sut.Evaluate();

            Assert.Equal(1, counter);
        }

        [Fact]
        public void OnEvaluate_NoEventHandler_DoesNotReevaluate()
        {
            var counter = 0;
            var sut = new Scope();
            sut.Evaluate();

            sut.OnEvaluating += (sender, args) => counter++;

            sut.Evaluate();

            Assert.Equal(0, counter);
        }

        [Fact]
        public void OnEvaluate_EventHandlerThrows_ExceptionAsBreadcrumb()
        {
            var expected = new InvalidOperationException("test");

            var sut = new Scope();
            sut.OnEvaluating += (sender, args) => throw expected;
            sut.Evaluate();

            var crumb = Assert.Single(sut.Breadcrumbs);

            Assert.Equal(BreadcrumbLevel.Error, crumb.Level);

            Assert.Equal(
                "Failed invoking event handler: " + expected,
                crumb.Message);
        }

        [Fact]
        public void OnEvaluate_EventHandlerThrows_DoesNotReevaluate()
        {
            var counter = 0;

            var sut = new Scope();
            sut.OnEvaluating += (sender, args) =>
            {
                counter++;
                throw new InvalidOperationException("test");
            };

            sut.Evaluate();
            sut.Evaluate();

            Assert.Equal(1, counter);
        }
    }
}
