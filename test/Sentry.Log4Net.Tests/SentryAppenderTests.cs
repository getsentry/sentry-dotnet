using System;
using log4net;
using log4net.Core;
using NSubstitute;
using Sentry.Protocol;
using Sentry.Reflection;
using Xunit;

namespace Sentry.Log4Net.Tests
{
    public class SentryAppenderTests
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

            public SentryAppender GetSut()
            {
                var sut = new SentryAppender(InitAction, Hub)
                {
                    Dsn = Dsn
                };
                sut.ActivateOptions();
                return sut;
            }
        }

        private readonly Fixture _fixture = new Fixture();

        [Fact]
        public void Append_WithException_CreatesEventWithException()
        {
            var sut = _fixture.GetSut();

            var expected = new Exception("expected");

            var evt = new LoggingEvent(null, null, "logger", Level.Critical, null, expected);

            sut.DoAppend(evt);

            _fixture.Hub.Received(1)
                .CaptureEvent(Arg.Is<SentryEvent>(e => e.Exception == expected));
        }

        [Fact]
        public void Append_Log4NetSdk_Name()
        {
            var sut = _fixture.GetSut();

            var evt = new LoggingEvent(new LoggingEventData());

            sut.DoAppend(evt);

            var expected = typeof(SentryAppender).Assembly.GetNameAndVersion();
            _fixture.Hub.Received(1)
                .CaptureEvent(Arg.Is<SentryEvent>(e => e.Sdk.Name == Constants.SdkName
                                                       && e.Sdk.Version == expected.Version));
        }

        [Fact]
        public void Append_Log4NetSdk_Packages()
        {
            var sut = _fixture.GetSut();

            var evt = new LoggingEvent(new LoggingEventData());

            SentryEvent actual = null;
            _fixture.Hub.When(h => h.CaptureEvent(Arg.Any<SentryEvent>()))
                .Do(c => actual = c.Arg<SentryEvent>());

            sut.DoAppend(evt);

            var expected = typeof(SentryAppender).Assembly.GetNameAndVersion();

            Assert.NotNull(actual);
            var package = Assert.Single(actual.Sdk.Packages);
            Assert.Equal("nuget:" + expected.Name, package.Name);
            Assert.Equal(expected.Version, package.Version);

        }

        [Fact]
        public void Append_LoggerNameAndLevel_Set()
        {
            const string expectedLogger = "logger";
            const SentryLevel expectedLevel = SentryLevel.Error;

            var sut = _fixture.GetSut();

            var evt = new LoggingEvent(null, null, expectedLogger, Level.Error, null, null);

            sut.DoAppend(evt);

            _fixture.Hub.Received(1)
                .CaptureEvent(Arg.Is<SentryEvent>(e => e.Logger == expectedLogger
                                                       && e.Level == expectedLevel));
        }

        [Fact]
        public void Append_RenderedMessage_Set()
        {
            const string expected = "message";

            var sut = _fixture.GetSut();

            var evt = new LoggingEvent(null, null, null, null, expected, null);

            sut.DoAppend(evt);

            _fixture.Hub.Received(1)
                .CaptureEvent(Arg.Is<SentryEvent>(e => e.Message == expected));
        }

        [Fact]
        public void Append_NullEvent_NoOp()
        {
            var sut = _fixture.GetSut();

            sut.DoAppend(null as LoggingEvent);
        }

        [Fact]
        public void Append_ByDefault_DoesNotSetUser()
        {
            var sut = _fixture.GetSut();
            var evt = new LoggingEvent(new LoggingEventData
            {
                Identity = "sentry-user"
            });

            sut.DoAppend(evt);

            _fixture.Hub.Received(1)
                .CaptureEvent(Arg.Is<SentryEvent>(e => e.InternalUser == null));
        }

        [Fact]
        public void Append_ConfiguredSendUser_UserInEvent()
        {
            const string expected = "sentry-user";
            var sut = _fixture.GetSut();
            var evt = new LoggingEvent(new LoggingEventData
            {
                Identity = expected
            });

            sut.SendIdentity = true;
            sut.DoAppend(evt);

            _fixture.Hub.Received(1)
                .CaptureEvent(Arg.Is<SentryEvent>(e => e.User.Id == expected));
        }

        [Fact]
        public void Append_NoDsn_InitNotCalled()
        {
            _fixture.Dsn = null;
            var sut = _fixture.GetSut();

            var evt = new LoggingEvent(new LoggingEventData());
            sut.DoAppend(evt);

            Assert.False(_fixture.InitInvoked);
        }

        [Fact]
        public void Append_WithEnabledHub_InitNotCalled()
        {
            _fixture.Hub.IsEnabled.Returns(true);
            var sut = _fixture.GetSut();

            var evt = new LoggingEvent(new LoggingEventData());
            sut.DoAppend(evt);

            Assert.False(_fixture.InitInvoked);
        }

        [Fact]
        public void Append_WithDsn_InitCalled()
        {
            var sut = _fixture.GetSut();

            var evt = new LoggingEvent(new LoggingEventData());
            sut.DoAppend(evt);

            Assert.True(_fixture.InitInvoked);
            Assert.Same(_fixture.Dsn, _fixture.DsnReceivedOnInit);
        }

        [Fact]
        public void Append_NoDsn_HubNotCalled()
        {
            _fixture.Dsn = null;
            var sut = _fixture.GetSut();

            var evt = new LoggingEvent(new LoggingEventData());
            sut.DoAppend(evt);

            Assert.False(_fixture.InitInvoked);
            _fixture.Hub.DidNotReceiveWithAnyArgs().CaptureEvent(null);
        }

        [Fact]
        public void Append_NoDsnAndDisabledHub_HubNotCalled()
        {
            _fixture.Dsn = null;
            _fixture.Hub.IsEnabled.Returns(false);
            var sut = _fixture.GetSut();

            var evt = new LoggingEvent(new LoggingEventData());
            sut.DoAppend(evt);

            Assert.False(_fixture.InitInvoked);
            _fixture.Hub.DidNotReceiveWithAnyArgs().CaptureEvent(null);
        }

        [Fact]
        public void Append_LocationInformation_AsExtra()
        {
            const string expectedClass = "class";
            const string expectedMethod = "method";
            const string expectedFileName = "fileName";
            const int expectedLineNumber = 100;

            var sut = _fixture.GetSut();

            var evt = new LoggingEvent(new LoggingEventData
            {
                LocationInfo = new LocationInfo(
                    expectedClass,
                    expectedMethod,
                    expectedFileName,
                    expectedLineNumber.ToString())
            });

            sut.DoAppend(evt);

            _fixture.Hub.Received(1)
                .CaptureEvent(Arg.Is<SentryEvent>(e =>
                    e.Extra["ClassName"].ToString() == expectedClass
                    && e.Extra["FileName"].ToString() == expectedFileName
                    && e.Extra["MethodName"].ToString() == expectedMethod
                    && (int)e.Extra["LineNumber"] == expectedLineNumber));
        }

        [Fact]
        public void Append_ThreadContext_AsExtra()
        {
            var expected = new object();
            var id = Guid.NewGuid().ToString();
            ThreadContext.Properties[id] = expected;

            var sut = _fixture.GetSut();
            sut.Dsn = "dsn";

            var evt = new LoggingEvent(new LoggingEventData());
            sut.DoAppend(evt);

            _fixture.Hub.Received(1)
                .CaptureEvent(Arg.Is<SentryEvent>(e => e.Extra[id] == expected));
        }

        [Fact]
        public void Close_DisposesSdk()
        {
            const string expectedDsn = "dsn";
            var sut = _fixture.GetSut();
            sut.Dsn = expectedDsn;

            var evt = new LoggingEvent(new LoggingEventData());
            sut.DoAppend(evt);

            _fixture.SdkDisposeHandle.DidNotReceive().Dispose();

            sut.Close();

            _fixture.SdkDisposeHandle.Received(1).Dispose();
        }
    }
}
