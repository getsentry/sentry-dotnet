using Sentry.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using Xunit;

namespace Sentry.Extensions.Logging.Tests
{
    public class SentryEFCoreInterceptorTests
    {
        internal const string EFContextInitializedKey = SentryEFCoreInterceptor.EFContextInitializedKey;
        internal const string EFConnectionOpening = SentryEFCoreInterceptor.EFConnectionOpening;
        internal const string EFCommandExecuting = SentryEFCoreInterceptor.EFCommandExecuting;
        internal const string EFCommandExecuted = SentryEFCoreInterceptor.EFCommandExecuted;
        internal const string EFConnectionClosed = SentryEFCoreInterceptor.EFConnectionClosed;

        private Func<ISpan, bool> GetValidator(string type) =>
            type switch
            {
                EFContextInitializedKey
                    => (span) => span.Description == "Opening EF Core context." && span.Operation == "ef.core",
                var x when
                        x == EFConnectionOpening ||
                        x == EFConnectionClosed
                    => (span) => span.Description == "connection" && span.Operation == "db",
                var x when
                        x == EFCommandExecuting ||
                        x == EFCommandExecuting
                => (span) => span.Description != "connection" && span.Operation == "db",
                _ => throw new NotSupportedException()
            };
        private class Fixture
        {
            private TransactionTracer _tracer { get; }

            public IReadOnlyCollection<ISpan> Spans => _tracer.Spans;
            public IHub Hub { get; set; }
            public Fixture()
            {
                Hub = Substitute.For<IHub>();
                _tracer = new TransactionTracer(Hub, "foo", "bar");
                Hub.GetSpan().ReturnsForAnyArgs(_tracer);

            }
        }

        private readonly Fixture _fixture = new();

        [Fact]
        public void OnNext_UnknownKey_SpanNotInvoked()
        {
            //Assert
            var hub = _fixture.Hub;
            var interceptor = new SentryEFCoreInterceptor(hub);

            //Act
            interceptor.OnNext(new("Unknown", null));

            //Assert
            hub.DidNotReceive().GetSpan();
        }

        [Theory]
        [InlineData(EFContextInitializedKey)]
        [InlineData(EFConnectionOpening)]
        [InlineData(EFCommandExecuting)]
        public void OnNext_KnownKey_GetSpanInvoked(string key)
        {
            //Arrange
            var hub = _fixture.Hub;
            var interceptor = new SentryEFCoreInterceptor(hub);

            //Act
            interceptor.OnNext(new(key, null));

            //Assert
            hub.Received(1).GetSpan();
            var child = _fixture.Spans.First(s => GetValidator(key)(s));
        }

        [Fact]
        public void OnNext_HappyPath_IsValid()
        {
            //Arrange
            var hub = _fixture.Hub;
            var interceptor = new SentryEFCoreInterceptor(hub);
            var expectedSql = "SELECT * FROM ...";

            //Act
            interceptor.OnNext(new(EFContextInitializedKey, null));
            interceptor.OnNext(new(EFConnectionOpening, null));
            interceptor.OnNext(new(EFCommandExecuting, null));
            interceptor.OnNext(new(EFCommandExecuted, expectedSql));
            interceptor.OnNext(new(EFConnectionClosed, null));

            //Assert
            hub.Received(3).GetSpan();
            var contextSpan = _fixture.Spans.First(s => GetValidator(EFContextInitializedKey)(s));
            var connectionSpan = _fixture.Spans.First(s => GetValidator(EFConnectionOpening)(s));
            var querySpan = _fixture.Spans.First(s => GetValidator(EFCommandExecuting)(s));
            //Validate if all spans were finished
            Assert.All(_fixture.Spans, (span) =>
            {
                Assert.True(span.IsFinished);
                Assert.Equal(SpanStatus.Ok, span.Status);
            });
            //Check connections between spans
            Assert.Equal(contextSpan.SpanId, connectionSpan.ParentSpanId);
            Assert.Equal(connectionSpan.ParentSpanId, querySpan.ParentSpanId);

            Assert.Equal(expectedSql, querySpan.Description);
        }
    }
}
