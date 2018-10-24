using System;
using System.Linq;
using NSubstitute;
using Sentry.Protocol;
using Sentry.Reflection;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace Sentry.Serilog.Tests
{
    public class SentrySinkTests
    {
        private class Fixture
        {
            public bool InitInvoked { get; set; }
            public string DsnReceivedOnInit { get; set; }
            public IDisposable SdkDisposeHandle { get; set; } = Substitute.For<IDisposable>();
            public Func<string, IDisposable> InitAction { get; set; }
            public IHub Hub { get; set; } = Substitute.For<IHub>();
            public string Dsn { get; set; } = "dsn";

            public Fixture()
            {
                InitAction = s =>
                {
                    DsnReceivedOnInit = s;
                    InitInvoked = true;
                    return SdkDisposeHandle;
                };
            }

            public SentrySink GetSut()
            {
                var sut = new SentrySink(null, InitAction, Hub)
                {
                    Dsn = Dsn
                };

                return sut;
            }
        }

        private readonly Fixture _fixture = new Fixture();

        [Fact]
        public void Sink_WithException_CreatesEventWithException()
        {
            var sut = _fixture.GetSut();

            var expected = new Exception("expected");

            var evt = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Error, expected, MessageTemplate.Empty,
                Enumerable.Empty<LogEventProperty>());

            sut.Emit(evt);

            _fixture.Hub.Received(1)
                .CaptureEvent(Arg.Is<SentryEvent>(e => e.Exception == expected));
        }

        [Fact]
        public void Sink_SerilogSdk_Name()
        {
            var sut = _fixture.GetSut();

            var evt = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Error, null, MessageTemplate.Empty,
                Enumerable.Empty<LogEventProperty>());

            sut.Emit(evt);

            var expected = typeof(SentrySink).Assembly.GetNameAndVersion();
            _fixture.Hub.Received(1)
                .CaptureEvent(Arg.Is<SentryEvent>(e => e.Sdk.Name == Constants.SdkName
                                                       && e.Sdk.Version == expected.Version));
        }

        [Fact]
        public void Sink_SerilogSdk_Packages()
        {
            var sut = _fixture.GetSut();

            var evt = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Error, null, MessageTemplate.Empty,
                Enumerable.Empty<LogEventProperty>());

            SentryEvent actual = null;
            _fixture.Hub.When(h => h.CaptureEvent(Arg.Any<SentryEvent>()))
                .Do(c => actual = c.Arg<SentryEvent>());

            sut.Emit(evt);

            var expected = typeof(SentrySink).Assembly.GetNameAndVersion();

            Assert.NotNull(actual);
            var package = Assert.Single(actual.Sdk.Packages);
            Assert.Equal("nuget:" + expected.Name, package.Name);
            Assert.Equal(expected.Version, package.Version);
        }

        [Fact]
        public void Sink_LoggerLevel_Set()
        {
            const SentryLevel expectedLevel = SentryLevel.Error;

            var sut = _fixture.GetSut();

            var evt = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Error, null, MessageTemplate.Empty,
                Enumerable.Empty<LogEventProperty>());

            sut.Emit(evt);

            _fixture.Hub.Received(1)
                .CaptureEvent(Arg.Is<SentryEvent>(e => e.Level == expectedLevel));
        }

        [Fact]
        public void Sink_RenderedMessage_Set()
        {
            const string expected = "message";

            var sut = _fixture.GetSut();

            var evt = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Error, null,
                new MessageTemplateParser().Parse(expected), Enumerable.Empty<LogEventProperty>());

            sut.Emit(evt);

            _fixture.Hub.Received(1)
                .CaptureEvent(Arg.Is<SentryEvent>(e => e.LogEntry.Formatted == expected));
        }

        [Fact]
        public void Sink_NoDsn_InitNotCalled()
        {
            _fixture.Dsn = null;
            var sut = _fixture.GetSut();

            var evt = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Error, null, MessageTemplate.Empty,
                Enumerable.Empty<LogEventProperty>());
            sut.Emit(evt);

            Assert.False(_fixture.InitInvoked);
        }

        [Fact]
        public void Sink_WithDsn_InitCalled()
        {
            var sut = _fixture.GetSut();

            var evt = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Error, null, MessageTemplate.Empty,
                Enumerable.Empty<LogEventProperty>());
            sut.Emit(evt);

            Assert.True(_fixture.InitInvoked);
            Assert.Same(_fixture.Dsn, _fixture.DsnReceivedOnInit);
        }

        [Fact]
        public void Sink_NoDsn_HubNotCalled()
        {
            _fixture.Dsn = null;
            var sut = _fixture.GetSut();

            var evt = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Error, null, MessageTemplate.Empty,
                Enumerable.Empty<LogEventProperty>());
            sut.Emit(evt);

            Assert.False(_fixture.InitInvoked);
            _fixture.Hub.DidNotReceiveWithAnyArgs().CaptureEvent(null);
        }

        [Fact]
        public void Sink_Properties_AsExtra()
        {
            const string expectedIp = "127.0.0.1";

            var sut = _fixture.GetSut();

            var evt = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Error, null, MessageTemplate.Empty,
                new[] { new LogEventProperty("IPAddress", new ScalarValue(expectedIp)) });

            sut.Emit(evt);

            _fixture.Hub.Received(1)
                .CaptureEvent(Arg.Is<SentryEvent>(e => e.Extra["IPAddress"].ToString() == expectedIp));
        }

        [Fact]
        public void Close_DisposesSdk()
        {
            const string expectedDsn = "dsn";
            var sut = _fixture.GetSut();
            sut.Dsn = expectedDsn;

            var evt = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Error, null, MessageTemplate.Empty,
                Enumerable.Empty<LogEventProperty>());
            sut.Emit(evt);

            _fixture.SdkDisposeHandle.DidNotReceive().Dispose();

            sut.Dispose();

            _fixture.SdkDisposeHandle.Received(1).Dispose();
        }

        [Fact]
        public void Sink_WithFormat_EventCaptured()
        {
            const string expectedMessage = "Test {structured} log";
            const int param = 10;

            var sut = _fixture.GetSut();

            var evt = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Error, null,
                new MessageTemplateParser().Parse(expectedMessage),
                new[] { new LogEventProperty("structured", new ScalarValue(param)) });

            sut.Emit(evt);

            _fixture.Hub.Received(1).CaptureEvent(Arg.Is<SentryEvent>(p =>
                p.LogEntry.Formatted == $"Test {param} log"
                && p.LogEntry.Message == expectedMessage));
        }
    }
}
