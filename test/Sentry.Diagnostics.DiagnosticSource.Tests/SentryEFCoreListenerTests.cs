using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using Sentry.Internals.DiagnosticSource;
using Xunit;

namespace Sentry.Diagnostics.DiagnosticSource.Tests
{
    public class SentryEFCoreListenerTests
    {
        internal const string EFQueryCompiling = SentryEFCoreListener.EFQueryCompiling;
        internal const string EFQueryCompiled = SentryEFCoreListener.EFQueryCompiled;
        internal const string EFConnectionOpening = SentryEFCoreListener.EFConnectionOpening;
        internal const string EFCommandExecuting = SentryEFCoreListener.EFCommandExecuting;
        internal const string EFCommandExecuted = SentryEFCoreListener.EFCommandExecuted;
        internal const string EFCommandFailed = SentryEFCoreListener.EFCommandFailed;
        internal const string EFConnectionClosed = SentryEFCoreListener.EFConnectionClosed;

        private Func<ISpan, bool> GetValidator(string type)
            => type switch
            {
                _ when
                        type == EFQueryCompiling ||
                        type == EFQueryCompiled
                    => (span) => span.Description != null && span.Operation == "db.query_compiler",
                _ when
                        type == EFConnectionOpening ||
                        type == EFConnectionClosed
                    => (span) => span.Description == null && span.Operation == "db.connection",
                _ when
                        type == EFCommandExecuting ||
                        type == EFCommandExecuting ||
                        type == EFCommandFailed
                    => (span) => span.Description != null && span.Operation == "db.query",
                _ => throw new NotSupportedException()
            };

        private class ThrowToStringClass
        {
            public override string ToString() => throw new Exception("ThrowToStringClass");
        }

        private class Fixture
        {
            internal TransactionTracer Tracer { get; }

            public IReadOnlyCollection<ISpan> Spans => Tracer?.Spans;
            public IHub Hub { get; set; }
            public Fixture()
            {
                Hub = Substitute.For<IHub>();
                Tracer = new TransactionTracer(Hub, "foo", "bar");
                Hub.GetSpan().ReturnsForAnyArgs((_) => Spans?.LastOrDefault(s => !s.IsFinished) ?? Tracer);
                Hub.CaptureEvent(Arg.Any<SentryEvent>(), Arg.Any<Scope>()).Returns((_) =>
                {
                    Spans.LastOrDefault(s => s.IsFinished is false)?.Finish(SpanStatus.InternalError);
                    return SentryId.Empty;
                });
            }
        }

        private readonly Fixture _fixture = new();

        [Fact]
        public void OnNext_UnknownKey_SpanNotInvoked()
        {
            // Assert
            var hub = _fixture.Hub;
            var interceptor = new SentryEFCoreListener(hub, new SentryOptions());

            // Act
            interceptor.OnNext(new("Unknown", null));

            // Assert
            hub.DidNotReceive().GetSpan();
        }

        [Theory]
        [InlineData(EFQueryCompiling, "data")]
        [InlineData(EFConnectionOpening, null)]
        [InlineData(EFCommandExecuting, "data")]
        public void OnNext_KnownKey_GetSpanInvoked(string key, string value)
        {
            // Arrange
            var hub = _fixture.Hub;
            var interceptor = new SentryEFCoreListener(hub, new SentryOptions());

            // Act
            interceptor.OnNext(new(key, value));

            // Assert
            hub.Received(1).GetSpan();
            var child = _fixture.Spans.First(s => GetValidator(key)(s));
        }

        [Theory]
        [InlineData(EFConnectionOpening, null)]
        [InlineData(EFCommandExecuting, "data")]
        public void OnNext_KnownKeyButDisabled_GetSpanNotInvoked(string key, string value)
        {
            // Arrange
            var hub = _fixture.Hub;
            var interceptor = new SentryEFCoreListener(hub, new SentryOptions());
            if (key == EFCommandExecuting)
            {
                interceptor.DisableQuerySpan();
            }
            else
            {
                interceptor.DisableConnectionSpan();
            }

            // Act
            interceptor.OnNext(new(key, value));

            // Assert
            hub.Received(0).GetSpan();
        }

        [Fact]
        public void OnNext_HappyPath_IsValid()
        {
            // Arrange
            var hub = _fixture.Hub;
            var interceptor = new SentryEFCoreListener(hub, new SentryOptions());
            var expectedSql = "SELECT * FROM ...";
            var efSql = "ef Junk\r\nSELECT * FROM ...";

            // Act
            interceptor.OnNext(new(EFQueryCompiling, efSql));
            interceptor.OnNext(new(EFQueryCompiled, efSql));
            interceptor.OnNext(new(EFConnectionOpening, null));
            interceptor.OnNext(new(EFCommandExecuting, efSql));
            interceptor.OnNext(new(EFCommandExecuted, efSql));
            interceptor.OnNext(new(EFConnectionClosed, efSql));

            // Assert
            hub.Received(3).GetSpan();
            var compilerSpan = _fixture.Spans.First(s => GetValidator(EFQueryCompiling)(s));
            var connectionSpan = _fixture.Spans.First(s => GetValidator(EFConnectionOpening)(s));
            var commandSpan = _fixture.Spans.First(s => GetValidator(EFCommandExecuting)(s));
            // Validate if all spans were finished.
            Assert.All(_fixture.Spans, (span) =>
            {
                Assert.True(span.IsFinished);
                if (span.Operation == "db.connection")
                {
                    Assert.Null(span.Description);
                }
                else
                {
                    Assert.Equal(expectedSql, span.Description);
                }
                Assert.Equal(SpanStatus.Ok, span.Status);
            });
            // Check connections between spans.
            Assert.Equal(_fixture.Tracer.SpanId, compilerSpan.ParentSpanId);
            Assert.Equal(_fixture.Tracer.SpanId, connectionSpan.ParentSpanId);
            Assert.Equal(connectionSpan.SpanId, commandSpan.ParentSpanId);
        }

        [Fact]
        public void OnNext_HappyPathWithError_TransactionWithErroredCommand()
        {
            // Arrange
            var hub = _fixture.Hub;
            var interceptor = new SentryEFCoreListener(hub, new SentryOptions());
            var expectedSql = "SELECT * FROM ...";
            var efSql = "ef Junk\r\nSELECT * FROM ...";

            // Act
            interceptor.OnNext(new(EFQueryCompiling, efSql));
            interceptor.OnNext(new(EFQueryCompiled, efSql));
            interceptor.OnNext(new(EFConnectionOpening, null));
            interceptor.OnNext(new(EFCommandExecuting, efSql));
            interceptor.OnNext(new(EFCommandFailed, efSql));
            interceptor.OnNext(new(EFConnectionClosed, efSql));

            // Assert
            hub.Received(3).GetSpan();
            var compilerSpan = _fixture.Spans.First(s => GetValidator(EFQueryCompiling)(s));
            var connectionSpan = _fixture.Spans.First(s => GetValidator(EFConnectionOpening)(s));
            var commandSpan = _fixture.Spans.First(s => GetValidator(EFCommandFailed)(s));

            // Validate if all spans were finished.
            Assert.All(new[] { compilerSpan, connectionSpan },
                (span) =>
            {
                Assert.True(span.IsFinished);
                Assert.Equal(SpanStatus.Ok, span.Status);
            });

            // Assert the failed command.
            Assert.True(commandSpan.IsFinished);
            Assert.Equal(SpanStatus.InternalError, commandSpan.Status);

            // Check connections between spans.
            Assert.Equal(_fixture.Tracer.SpanId, compilerSpan.ParentSpanId);
            Assert.Equal(_fixture.Tracer.SpanId, connectionSpan.ParentSpanId);
            Assert.Equal(connectionSpan.SpanId, commandSpan.ParentSpanId);

            Assert.Equal(expectedSql, commandSpan.Description);
        }

        [Fact]
        public void OnNext_HappyPathWithError_TransactionWithErroredCompiler()
        {
            // Arrange
            var hub = _fixture.Hub;
            var interceptor = new SentryEFCoreListener(hub, new SentryOptions());
            var expectedSql = "SELECT * FROM ...";
            var efSql = "ef Junk\r\nSELECT * FROM ...";

            // Act
            interceptor.OnNext(new(EFQueryCompiling, efSql));
            hub.CaptureEvent(new SentryEvent(), null);

            // Assert
            hub.Received(1).GetSpan();
            var compilerSpan = _fixture.Spans.First(s => GetValidator(EFQueryCompiling)(s));

            Assert.True(compilerSpan.IsFinished);
            Assert.Equal(SpanStatus.InternalError, compilerSpan.Status);

            Assert.Equal(_fixture.Tracer.SpanId, compilerSpan.ParentSpanId);

            Assert.Equal(expectedSql, compilerSpan.Description);
        }

        [Fact]
        public void OnNext_ThrowsException_ExceptionIsolated()
        {
            // Arrange
            var hub = _fixture.Hub;
            var interceptor = new SentryEFCoreListener(hub, new SentryOptions());
            var exceptionReceived = false;

            // Act
            try
            {
                interceptor.OnNext(new(EFQueryCompiling, new ThrowToStringClass()));
            }
            catch (Exception)
            {
                exceptionReceived = true;
            }

            // Assert
            Assert.False(exceptionReceived);
        }

        [Fact]
        public void FilterNewLineValue_StringWithNewLine_SubStringAfterNewLine()
        {
            // Arrange
            var text = "1234\r\nSELECT *...\n FROM ...";
            var expectedText = "SELECT *...\n FROM ...";

            // Act
            var value = SentryEFCoreListener.FilterNewLineValue(text);

            // Assert
            Assert.Equal(expectedText, value);
        }

        [Fact]
        public void FilterNewLineValue_NullObject_NullString()
        {
            // Act
            var value = SentryEFCoreListener.FilterNewLineValue(null);

            // Assert
            Assert.Null(value);
        }

        [Fact]
        public void FilterNewLineValue_OneLineString_OneLineString()
        {
            // Arrange
            var text = "1234";
            var expectedText = "1234";

            // Act
            var value = SentryEFCoreListener.FilterNewLineValue(text);

            // Assert
            Assert.Equal(expectedText, value);
        }

        [Fact]
        public void FilterNewLineValue_EmptyString_EmptyString()
        {
            // Arrange
            var text = "";
            var expectedText = "";

            // Act
            var value = SentryEFCoreListener.FilterNewLineValue(text);

            // Assert
            Assert.Equal(expectedText, value);
        }
    }
}
