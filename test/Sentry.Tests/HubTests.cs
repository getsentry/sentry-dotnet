using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Sentry;
using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Internal;
using Sentry.Protocol;
using Sentry.Protocol.Envelopes;
using Xunit;

// ReSharper disable once CheckNamespace
// Tests code path which excludes frames with namespace Sentry
namespace NotSentry.Tests
{
    public class HubTests
    {
        private class FakeBackgroundWorker : IBackgroundWorker
        {
            public List<Envelope> Queue { get; } = new();

            public int QueuedItems => Queue.Count;

            public bool EnqueueEnvelope(Envelope envelope)
            {
                Queue.Add(envelope);
                return true;
            }

            public Task FlushAsync(TimeSpan timeout) => Task.CompletedTask;
        }

        [Fact]
        public void PushScope_BreadcrumbWithinScope_NotVisibleOutside()
        {
            // Arrange
            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                BackgroundWorker = new FakeBackgroundWorker()
            });

            // Act & assert
            using (hub.PushScope())
            {
                hub.ConfigureScope(s => s.AddBreadcrumb("test"));
                Assert.Single(hub.ScopeManager.GetCurrent().Key.Breadcrumbs);
            }

            Assert.Empty(hub.ScopeManager.GetCurrent().Key.Breadcrumbs);
        }

        [Fact]
        public void PushAndLockScope_DoesNotAffectOuterScope()
        {
            // Arrange
            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                BackgroundWorker = new FakeBackgroundWorker()
            });

            // Act & assert
            hub.ConfigureScope(s => Assert.False(s.Locked));
            using (hub.PushAndLockScope())
            {
                hub.ConfigureScope(s => Assert.True(s.Locked));
            }

            hub.ConfigureScope(s => Assert.False(s.Locked));
        }

        [Fact]
        public void CaptureMessage_AttachStacktraceFalse_DoesNotIncludeStackTrace()
        {
            // Arrange
            var worker = new FakeBackgroundWorker();

            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                BackgroundWorker = worker,
                AttachStacktrace = true
            });

            // Act
            hub.CaptureMessage("test");

            // Assert
            var envelope = worker.Queue.Single();

            var stackTrace = envelope.Items
                .Select(i => i.Payload)
                .OfType<JsonSerializable>()
                .Select(i => i.Source)
                .OfType<SentryEvent>()
                .Single()
                .SentryExceptionValues;

            stackTrace.Should().BeNull();
        }

        [Fact]
        public void CaptureMessage_FailedQueue_LastEventIdSetToEmpty()
        {
            // Arrange
            var worker = Substitute.For<IBackgroundWorker>();
            worker.EnqueueEnvelope(Arg.Any<Envelope>()).Returns(false);

            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                BackgroundWorker = worker
            });

            // Act
            var actualId = hub.CaptureMessage("test");

            // Assert
            Assert.Equal(Guid.Empty, (Guid)actualId);
            Assert.Equal(Guid.Empty, (Guid)hub.LastEventId);
        }

        [Fact]
        public void CaptureMessage_SuccessQueued_LastEventIdSetToReturnedId()
        {
            // Arrange
            var worker = Substitute.For<IBackgroundWorker>();
            worker.EnqueueEnvelope(Arg.Any<Envelope>()).Returns(true);

            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                BackgroundWorker = worker
            });

            // Act
            var actualId = hub.CaptureMessage("test");

            // Assert
            Assert.NotEqual(default, actualId);
            Assert.Equal(actualId, hub.LastEventId);
        }

        [Fact]
        public void CaptureException_FinishedSpanBoundToSameExceptionExists_EventIsLinkedToSpan()
        {
            // Arrange
            var client = Substitute.For<ISentryClient>();

            var hub = new Hub( new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                TracesSampleRate = 1
            }, client);

            var exception = new Exception("error");

            var transaction = hub.StartTransaction("foo", "bar");
            transaction.Finish(exception);

            // Act
            hub.CaptureException(exception);

            // Assert
            client.Received(1).CaptureEvent(
                Arg.Is<SentryEvent>(evt =>
                    evt.Contexts.Trace.TraceId == transaction.TraceId &&
                    evt.Contexts.Trace.SpanId == transaction.SpanId),
                Arg.Any<Scope>()
            );
        }

        [Fact]
        public void CaptureException_ActiveSpanExistsOnScope_EventIsLinkedToSpan()
        {
            // Arrange
            var client = Substitute.For<ISentryClient>();

            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                TracesSampleRate = 1
            }, client);

            var exception = new Exception("error");

            var transaction = hub.StartTransaction("foo", "bar");

            hub.ConfigureScope(scope => scope.Transaction = transaction);

            // Act
            hub.CaptureException(exception);

            // Assert
            client.Received(1).CaptureEvent(
                Arg.Is<SentryEvent>(evt =>
                    evt.Contexts.Trace.TraceId == transaction.TraceId &&
                    evt.Contexts.Trace.SpanId == transaction.SpanId),
                Arg.Any<Scope>()
            );
        }

        [Fact]
        public void CaptureException_ActiveSpanExistsOnScopeButIsSampledOut_EventIsNotLinkedToSpan()
        {
            // Arrange
            var client = Substitute.For<ISentryClient>();

            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                TracesSampleRate = 0
            }, client);

            var exception = new Exception("error");

            var transaction = hub.StartTransaction("foo", "bar");

            hub.ConfigureScope(scope => scope.Transaction = transaction);

            // Act
            hub.CaptureException(exception);

            // Assert
            client.Received(1).CaptureEvent(
                Arg.Is<SentryEvent>(evt =>
                    evt.Contexts.Trace.TraceId == default &&
                    evt.Contexts.Trace.SpanId == default),
                Arg.Any<Scope>()
            );
        }

        [Fact]
        public void CaptureException_NoActiveSpanAndNoSpanBoundToSameException_EventIsNotLinkedToSpan()
        {
            // Arrange
            var client = Substitute.For<ISentryClient>();

            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                TracesSampleRate = 1
            }, client);

            // Act
            hub.CaptureException(new Exception("error"));

            // Assert
            client.Received(1).CaptureEvent(
                Arg.Is<SentryEvent>(evt =>
                    evt.Contexts.Trace.TraceId == default &&
                    evt.Contexts.Trace.SpanId == default),
                Arg.Any<Scope>()
            );
        }

        [Fact]
        public void CaptureEvent_SessionActive_NoExceptionDoesNotReportError()
        {
            // Arrange
            var client = Substitute.For<ISentryClient>();

            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
            }, client);

            hub.StartSession();

            // Act
            hub.CaptureEvent(new SentryEvent());
            hub.EndSession();

            // Assert
            client.Received().CaptureSession(Arg.Is<SessionUpdate>(s => s.ErrorCount == 0));
        }

        [Fact]
        public void CaptureEvent_SessionActive_ExceptionReportsError()
        {
            // Arrange
            var client = Substitute.For<ISentryClient>();

            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
            }, client);

            hub.StartSession();

            // Act
            hub.CaptureEvent(new SentryEvent(new Exception()));
            hub.EndSession();

            // Assert
            client.Received().CaptureSession(Arg.Is<SessionUpdate>(s => s.ErrorCount == 1));
        }

        [Fact]
        public void CaptureEvent_ActiveSession_UnhandledExceptionSessionEndedAsCrashed()
        {
            // Arrange
            var worker = Substitute.For<IBackgroundWorker>();

            var options = new SentryOptions {Dsn = DsnSamples.ValidDsnWithSecret};
            var client = new SentryClient(options, worker);
            var hub = new Hub(options, client);

            hub.StartSession();

            // Act
            hub.CaptureEvent(new SentryEvent
            {
                SentryExceptions = new[] {new SentryException {Mechanism = new Mechanism {Handled = false}}}
            });

            // Assert
            worker.Received().EnqueueEnvelope(
                Arg.Is<Envelope>(e =>
                    e.Items
                        .Select(i => i.Payload)
                        .OfType<JsonSerializable>()
                        .Select(i => i.Source)
                        .OfType<SessionUpdate>()
                        .Single()
                        .EndStatus == SessionEndStatus.Crashed
                )
            );
        }

        [Fact]
        public void StartTransaction_NameOpDescription_Works()
        {
            // Arrange
            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret
            });

            // Act
            var transaction = hub.StartTransaction("name", "operation", "description");

            // Assert
            transaction.Name.Should().Be("name");
            transaction.Operation.Should().Be("operation");
            transaction.Description.Should().Be("description");
        }

        [Fact]
        public void StartTransaction_FromTraceHeader_CopiesContext()
        {
            // Arrange
            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                TracesSampleRate = 1
            });

            var traceHeader = new SentryTraceHeader(
                SentryId.Parse("75302ac48a024bde9a3b3734a82e36c8"),
                SpanId.Parse("2000000000000000"),
                true
            );

            // Act
            var transaction = hub.StartTransaction("name", "operation", traceHeader);

            // Assert
            transaction.TraceId.Should().Be(SentryId.Parse("75302ac48a024bde9a3b3734a82e36c8"));
            transaction.ParentSpanId.Should().Be(SpanId.Parse("2000000000000000"));
            transaction.IsSampled.Should().BeTrue();
        }

        [Fact]
        public void StartTransaction_FromTraceHeader_SampledInheritedFromParentRegardlessOfSampleRate()
        {
            // Arrange
            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                TracesSampleRate = 0
            });

            var traceHeader = new SentryTraceHeader(
                SentryId.Parse("75302ac48a024bde9a3b3734a82e36c8"),
                SpanId.Parse("2000000000000000"),
                true
            );

            // Act
            var transaction = hub.StartTransaction("name", "operation", traceHeader);

            // Assert
            transaction.IsSampled.Should().BeTrue();
        }

        [Fact]
        public void StartTransaction_FromTraceHeader_CustomSamplerCanSampleOutTransaction()
        {
            // Arrange
            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                TracesSampler = _ => 0,
                TracesSampleRate = 1
            });

            var traceHeader = new SentryTraceHeader(
                SentryId.Parse("75302ac48a024bde9a3b3734a82e36c8"),
                SpanId.Parse("2000000000000000"),
                true
            );

            // Act
            var transaction = hub.StartTransaction("foo", "bar", traceHeader);

            // Assert
            transaction.IsSampled.Should().BeFalse();
        }

        [Fact]
        public void StartTransaction_StaticSampling_SampledIn()
        {
            // Arrange
            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                TracesSampleRate = 1
            });

            // Act
            var transaction = hub.StartTransaction("name", "operation");

            // Assert
            transaction.IsSampled.Should().BeTrue();
        }

        [Fact]
        public void StartTransaction_StaticSampling_SampledOut()
        {
            // Arrange
            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                TracesSampleRate = 0
            });

            // Act
            var transaction = hub.StartTransaction("name", "operation");

            // Assert
            transaction.IsSampled.Should().BeFalse();
        }

        [Fact]
        public void StartTransaction_StaticSampling_50PercentDistribution()
        {
            // 15% deviation is ok
            const double allowedRelativeDeviation = 0.15;

            // Arrange
            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                TracesSampleRate = 0.5
            });

            // Act
            var transactions = Enumerable
                .Range(0, 1_000)
                .Select(i => hub.StartTransaction($"name[{i}]", $"operation[{i}]"))
                .ToArray();

            var transactionsSampledIn = transactions.Where(t => t.IsSampled == true).ToArray();
            var transactionsSampledOut = transactions.Where(t => t.IsSampled == false).ToArray();

            // Assert
            transactionsSampledIn.Length.Should().BeCloseTo(
                (int)(0.5 * transactions.Length),
                (uint)(allowedRelativeDeviation * transactions.Length)
            );

            transactionsSampledOut.Length.Should().BeCloseTo(
                (int)(0.5 * transactions.Length),
                (uint)(allowedRelativeDeviation * transactions.Length)
            );
        }

        [Fact]
        public void StartTransaction_StaticSampling_25PercentDistribution()
        {
            // 15% deviation is ok
            const double allowedRelativeDeviation = 0.15;

            // Arrange
            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                TracesSampleRate = 0.25
            });

            // Act
            var transactions = Enumerable
                .Range(0, 1_000)
                .Select(i => hub.StartTransaction($"name[{i}]", $"operation[{i}]"))
                .ToArray();

            var transactionsSampledIn = transactions.Where(t => t.IsSampled == true).ToArray();
            var transactionsSampledOut = transactions.Where(t => t.IsSampled == false).ToArray();

            // Assert
            transactionsSampledIn.Length.Should().BeCloseTo(
                (int)(0.25 * transactions.Length),
                (uint)(allowedRelativeDeviation * transactions.Length)
            );

            transactionsSampledOut.Length.Should().BeCloseTo(
                (int)(0.75 * transactions.Length),
                (uint)(allowedRelativeDeviation * transactions.Length)
            );
        }

        [Fact]
        public void StartTransaction_StaticSampling_75PercentDistribution()
        {
            // 15% deviation is ok
            const double allowedRelativeDeviation = 0.15;

            // Arrange
            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                TracesSampleRate = 0.75
            });

            // Act
            var transactions = Enumerable
                .Range(0, 1_000)
                .Select(i => hub.StartTransaction($"name[{i}]", $"operation[{i}]"))
                .ToArray();

            var transactionsSampledIn = transactions.Where(t => t.IsSampled == true).ToArray();
            var transactionsSampledOut = transactions.Where(t => t.IsSampled == false).ToArray();

            // Assert
            transactionsSampledIn.Length.Should().BeCloseTo(
                (int)(0.75 * transactions.Length),
                (uint)(allowedRelativeDeviation * transactions.Length)
            );

            transactionsSampledOut.Length.Should().BeCloseTo(
                (int)(0.25 * transactions.Length),
                (uint)(allowedRelativeDeviation * transactions.Length)
            );
        }

        [Fact]
        public void StartTransaction_DynamicSampling_SampledIn()
        {
            // Arrange
            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                TracesSampler = ctx => ctx.TransactionContext.Name == "foo" ? 1 : 0
            });

            // Act
            var transaction = hub.StartTransaction("foo", "op");

            // Assert
            transaction.IsSampled.Should().BeTrue();
        }

        [Fact]
        public void StartTransaction_DynamicSampling_SampledOut()
        {
            // Arrange
            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                TracesSampler = ctx => ctx.TransactionContext.Name == "foo" ? 1 : 0
            });

            // Act
            var transaction = hub.StartTransaction("bar", "op");

            // Assert
            transaction.IsSampled.Should().BeFalse();
        }

        [Fact]
        public void StartTransaction_DynamicSampling_WithCustomContext_SampledIn()
        {
            // Arrange
            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                TracesSampler = ctx => ctx.CustomSamplingContext.GetValueOrDefault("xxx") as string == "zzz" ? 1 : 0
            });

            // Act
            var transaction = hub.StartTransaction(
                new TransactionContext("foo", "op"),
                new Dictionary<string, object> {["xxx"] = "zzz"}
            );

            // Assert
            transaction.IsSampled.Should().BeTrue();
        }

        [Fact]
        public void StartTransaction_DynamicSampling_WithCustomContext_SampledOut()
        {
            // Arrange
            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                TracesSampler = ctx => ctx.CustomSamplingContext.GetValueOrDefault("xxx") as string == "zzz" ? 1 : 0
            });

            // Act
            var transaction = hub.StartTransaction(
                new TransactionContext("foo", "op"),
                new Dictionary<string, object> {["xxx"] = "yyy"}
            );

            // Assert
            transaction.IsSampled.Should().BeFalse();
        }

        [Fact]
        public void StartTransaction_DynamicSampling_FallbackToStatic_SampledIn()
        {
            // Arrange
            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                TracesSampler = _ => null,
                TracesSampleRate = 1
            });

            // Act
            var transaction = hub.StartTransaction("foo", "bar");

            // Assert
            transaction.IsSampled.Should().BeTrue();
        }

        [Fact]
        public void StartTransaction_DynamicSampling_FallbackToStatic_SampledOut()
        {
            // Arrange
            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                TracesSampler = _ => null,
                TracesSampleRate = 0
            });

            // Act
            var transaction = hub.StartTransaction("foo", "bar");

            // Assert
            transaction.IsSampled.Should().BeFalse();
        }

        [Fact]
        public void GetTraceHeader_ReturnsHeaderForActiveSpan()
        {
            // Arrange
            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret
            });

            var transaction = hub.StartTransaction("foo", "bar");

            // Act
            hub.WithScope(scope =>
            {
                scope.Transaction = transaction;

                var header = hub.GetTraceHeader();

                // Assert
                header.Should().NotBeNull();
                header?.SpanId.Should().Be(transaction.SpanId);
                header?.TraceId.Should().Be(transaction.TraceId);
                header?.IsSampled.Should().Be(transaction.IsSampled);
            });
        }

        [Fact]
        public void CaptureTransaction_AfterTransactionFinishes_ResetsTransactionOnScope()
        {
            // Arrange
            var client = Substitute.For<ISentryClient>();

            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret
            }, client);

            var transaction = hub.StartTransaction("foo", "bar");

            hub.WithScope(scope => scope.Transaction = transaction);

            // Act
            transaction.Finish();

            // Assert
            hub.WithScope(scope => scope.Transaction.Should().BeNull());
        }

        [Fact]
        public void Dispose_IsEnabled_SetToFalse()
        {
            // Arrange
            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret
            });

            hub.IsEnabled.Should().BeTrue();

            // Act
            hub.Dispose();

            // Assert
            hub.IsEnabled.Should().BeFalse();
        }

        [Fact]
        public void Dispose_CalledSecondTime_ClientDisposedOnce()
        {
            var client = Substitute.For<ISentryClient, IDisposable>();
            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret
            }, client);

            // Act
            hub.Dispose();
            hub.Dispose();

            // Assert
            (client as IDisposable).Received(1).Dispose();
        }

        [Fact]
        public void StartSession_CapturesUpdate()
        {
            // Arrange
            var client = Substitute.For<ISentryClient>();

            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret
            }, client);

            // Act
            hub.StartSession();

            // Assert
            client.Received().CaptureSession(Arg.Is<SessionUpdate>(s => s.IsInitial));
        }

        [Fact]
        public void EndSession_CapturesUpdate()
        {
            // Arrange
            var client = Substitute.For<ISentryClient>();

            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret
            }, client);

            hub.StartSession();

            // Act
            hub.EndSession();

            // Assert
            client.Received().CaptureSession(Arg.Is<SessionUpdate>(s => !s.IsInitial));
        }

        [Fact]
        public void Ctor_AutoSessionTrackingEnabled_StartsSession()
        {
            // Arrange
            var client = Substitute.For<ISentryClient>();

            // Act
            _ = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                AutoSessionTracking = true
            }, client);

            // Assert
            client.Received().CaptureSession(Arg.Is<SessionUpdate>(s => s.IsInitial));
        }

        [Fact]
        public void Ctor_GlobalModeTrue_DoesNotPushScope()
        {
            // Arrange
            var scopeManager = Substitute.For<IInternalScopeManager>();

            // Act
            _ = new Hub(new SentryOptions
            {
                IsGlobalModeEnabled = true,
                Dsn = DsnSamples.ValidDsnWithSecret,
            }, scopeManager: scopeManager);

            // Assert
            scopeManager.DidNotReceiveWithAnyArgs().PushScope();
        }

        [Fact]
        public void Ctor_GlobalModeFalse_DoesPushScope()
        {
            // Arrange
            var scopeManager = Substitute.For<IInternalScopeManager>();

            // Act
            _ = new Hub(new SentryOptions
            {
                IsGlobalModeEnabled = false,
                Dsn = DsnSamples.ValidDsnWithSecret,
            }, scopeManager: scopeManager);

            // Assert
            scopeManager.Received(1).PushScope();
        }

        [Fact]
        public void Dispose_AutoSessionTrackingEnabled_EndsSession()
        {
            // Arrange
            var client = Substitute.For<ISentryClient>();

            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                AutoSessionTracking = true
            }, client);

            // Act
            hub.Dispose();

            // Assert
            client.Received().CaptureSession(Arg.Is<SessionUpdate>(s => s.EndStatus == SessionEndStatus.Exited));
        }

        [Fact]
        public void ResumeSession_WithinAutoTrackingInterval_ContinuesSameSession()
        {
            // Arrange
            var client = Substitute.For<ISentryClient>();

            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                AutoSessionTrackingInterval = TimeSpan.FromSeconds(9999)
            }, client);

            hub.StartSession();
            hub.PauseSession();

            // Act
            hub.ResumeSession();

            // Assert
            client.DidNotReceive().CaptureSession(Arg.Is<SessionUpdate>(s => s.EndStatus != null));
        }

        [Fact]
        public void ResumeSession_BeyondAutoTrackingInterval_EndsPreviousSessionAndStartsANewOne()
        {
            // Arrange
            var client = Substitute.For<ISentryClient>();
            var clock = Substitute.For<ISystemClock>();

            var options = new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                AutoSessionTrackingInterval = TimeSpan.FromMilliseconds(10)
            };

            var hub = new Hub(
                options,
                client,
                clock: clock,
                sessionManager: new GlobalSessionManager(options, clock));

            clock.GetUtcNow().Returns(DateTimeOffset.Now);

            hub.StartSession();
            hub.PauseSession();

            clock.GetUtcNow().Returns(DateTimeOffset.Now + TimeSpan.FromDays(1));

            // Act
            hub.ResumeSession();

            // Assert
            client.Received().CaptureSession(Arg.Is<SessionUpdate>(s => s.EndStatus == SessionEndStatus.Exited));
            client.Received().CaptureSession(Arg.Is<SessionUpdate>(s => s.IsInitial));
        }

        [Fact]
        public void ResumeSession_NoActiveSession_DoesNothing()
        {
            // Arrange
            var client = Substitute.For<ISentryClient>();
            var clock = Substitute.For<ISystemClock>();

            var options = new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                AutoSessionTrackingInterval = TimeSpan.FromMilliseconds(10)
            };

            var hub = new Hub(
                options,
                clock: clock,
                sessionManager: new GlobalSessionManager(options, clock));

            clock.GetUtcNow().Returns(DateTimeOffset.Now);

            hub.PauseSession();

            clock.GetUtcNow().Returns(DateTimeOffset.Now + TimeSpan.FromDays(1));

            // Act
            hub.ResumeSession();

            // Assert
            client.DidNotReceive().CaptureSession(Arg.Any<SessionUpdate>());
        }

        [Fact]
        public void ResumeSession_NoPausedSession_DoesNothing()
        {
            // Arrange
            var client = Substitute.For<ISentryClient>();
            var clock = Substitute.For<ISystemClock>();

            var options = new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                AutoSessionTrackingInterval = TimeSpan.FromMilliseconds(10)
            };

            var hub = new Hub(
                options,
                clock: clock,
                sessionManager: new GlobalSessionManager(options, clock));

            clock.GetUtcNow().Returns(DateTimeOffset.Now);

            hub.StartSession();

            clock.GetUtcNow().Returns(DateTimeOffset.Now + TimeSpan.FromDays(1));

            // Act
            hub.ResumeSession();

            // Assert
            client.DidNotReceive().CaptureSession(Arg.Is<SessionUpdate>(s => s.EndStatus != null));
        }
    }
}
