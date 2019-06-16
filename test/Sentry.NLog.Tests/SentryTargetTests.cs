using System;
using System.Linq;

using NLog;
using NLog.Config;

using NSubstitute;

using Sentry.Infrastructure;
using Sentry.Protocol;
using Sentry.Reflection;

using Xunit;

namespace Sentry.NLog.Tests
{
    using static DsnSamples;

    public class SentryTargetTests
    {
        private const string DefaultMessage = "This is a logged message";

        private class Fixture
        {
            public SentryNLogOptions Options { get; set; } = new SentryNLogOptions();

            public IHub Hub { get; set; } = Substitute.For<IHub>();

            public Func<IHub> HubAccessor { get; set; }

            public ISystemClock Clock { get; set; } = Substitute.For<ISystemClock>();

            public IDisposable SdkDisposeHandle { get; set; } = Substitute.For<IDisposable>();

            public Scope Scope { get; }

            public Fixture()
            {
                Hub.IsEnabled.Returns(true);
                HubAccessor = () => Hub;
                Scope = new Scope(new SentryOptions());
                Hub.ConfigureScope(Arg.Invoke(Scope));
            }

            public SentryTarget GetTarget(Action<SentryNLogOptions> customConfig = null)
            {
                var options = Options;
                if (customConfig != null)
                {
                    options = new SentryNLogOptions();
                    customConfig(options);
                }

                var target = new SentryTarget(
                    options,
                    HubAccessor,
                    SdkDisposeHandle,
                    Clock)
                {
                    Dsn = ValidDsnWithoutSecret,
                };
                return target;
            }

            public LogFactory GetLoggerFactory(Action<SentryNLogOptions> customConfig = null)
            {
                var target = GetTarget(customConfig);
                var config = new LoggingConfiguration();
                config.AddTarget("sentry", target);
                config.AddRule(LogLevel.Trace, LogLevel.Fatal, target);

                var factory = new LogFactory(config);

                return factory;
            }

            public (LogFactory factory, SentryTarget target) GetLoggerFactoryAndTarget(Action<SentryNLogOptions> customConfig = null)
            {
                var factory = GetLoggerFactory(customConfig);

                return (factory, factory.Configuration?.FindTargetByName<SentryTarget>("sentry"));
            }

            public Logger GetLogger(Action<SentryNLogOptions> customConfig = null) => GetLoggerFactory(customConfig).GetLogger("sentry");
        }

        private readonly Fixture _fixture = new Fixture();

        [Fact]
        public void Can_configure_from_xml_file()
        {
            string configXml = $@"
                <nlog throwConfigExceptions='true'>
                    <extensions>
                        <add type='{typeof(SentryTarget).AssemblyQualifiedName}' />
                    </extensions>
                    <targets>
                        <target type='Sentry' name='sentry' dsn='{ValidDsnWithoutSecret}'>
                            <options>
                                <environment>Development</environment>
                            </options>
                        </target>
                    </targets>
                </nlog>";

            var c = XmlLoggingConfiguration.CreateFromXmlString(configXml);

            var t = c.FindTargetByName("sentry") as SentryTarget;
            Assert.NotNull(t);
            Assert.Equal(ValidDsnWithoutSecret, t.Options.Dsn.ToString());
            Assert.Equal("Development", t.Options.Environment);
        }

        [Fact]
        public void Shutdown_DisposesSdk()
        {
            var factory = _fixture.GetLoggerFactory(a => a.InitializeSdk = false);
            LogManager.Configuration = factory.Configuration;

            var sut = factory.GetCurrentClassLogger();

            sut.Error(DefaultMessage);

            _fixture.SdkDisposeHandle.DidNotReceive().Dispose();

            LogManager.Shutdown();

            _fixture.SdkDisposeHandle.Received(1).Dispose();
        }

        [Fact]
        public void Shutdown_NoDisposeHandleProvided_DoesNotThrow()
        {
            var factory = _fixture.GetLoggerFactory(a => a.InitializeSdk = false);
            LogManager.Configuration = factory.Configuration;

            var sut = factory.GetCurrentClassLogger();

            sut.Error(DefaultMessage);
            LogManager.Shutdown();
        }

        [Fact]
        public void Log_WithException_CreatesEventWithException()
        {
            var expected = new Exception("expected");

            var logger = _fixture.GetLoggerFactory().GetLogger("sentry");

            logger.Error(expected, DefaultMessage);

            _fixture.Hub.Received(1)
                    .CaptureEvent(Arg.Is<SentryEvent>(e => e.Exception == expected));
        }

        [Fact]
        public void Log_WithOnlyException_GeneratesBreadcrumbFromException()
        {
            var expectedException = new Exception("expected message");

            const BreadcrumbLevel expectedLevel = BreadcrumbLevel.Error;

            var logger = _fixture.GetLogger(o => o.MinimumEventLevel = LogLevel.Fatal);

            logger.Error(expectedException);

            var b = _fixture.Scope.Breadcrumbs.First();

            Assert.Equal(b.Message, $"{expectedException.GetType()}: {expectedException.Message}");
            Assert.Equal(b.Timestamp, _fixture.Clock.GetUtcNow());
            Assert.Null(b.Category);
            Assert.Equal(b.Level, expectedLevel);
            Assert.Null(b.Type);
            Assert.NotNull(b.Data);
            Assert.Equal(expectedException.GetType().ToString(), b.Data["exception_type"]);
            Assert.Equal(expectedException.Message, b.Data["exception_message"]);
        }

        [Fact]
        public void Log_NLogSdk_Name()
        {
            var logger = _fixture.GetLogger(o => o.MinimumEventLevel = LogLevel.Info);

            var expected = typeof(SentryTarget).Assembly.GetNameAndVersion();
            logger.Info(DefaultMessage);

            _fixture.Hub.Received(1)
                    .CaptureEvent(Arg.Is<SentryEvent>(e => e.Sdk.Name == Constants.SdkName
                                                           && e.Sdk.Version == expected.Version));
        }

        [Fact]
        public void Log_NLogSdk_Packages()
        {
            var logger = _fixture.GetLogger(o => o.MinimumEventLevel = LogLevel.Info);

            SentryEvent? actual = null;
            _fixture.Hub.When(h => h.CaptureEvent(Arg.Any<SentryEvent>()))
                    .Do(c => actual = c.Arg<SentryEvent>());

            logger.Info(DefaultMessage);

            var expected = typeof(SentryTarget).Assembly.GetNameAndVersion();

            Assert.NotNull(actual);
            var package = Assert.Single(actual.Sdk.Packages);
            Assert.Equal("nuget:" + expected.Name, package.Name);
            Assert.Equal(expected.Version, package.Version);
        }

        [Fact]
        public void Log_LoggerLevel_Set()
        {
            const SentryLevel expectedLevel = SentryLevel.Error;

            var logger = _fixture.GetLogger();

            logger.Error(DefaultMessage);
            _fixture.Hub.Received(1)
                    .CaptureEvent(Arg.Is<SentryEvent>(e => e.Level == expectedLevel));
        }

        [Fact]
        public void Log_RenderedMessage_Set()
        {
            const string unFormatted = "This is the message: {data}";
            object[] args = { "data" };

            var manager = _fixture.GetLoggerFactory();
            var target = manager.Configuration.FindTargetByName<SentryTarget>("sentry");

            var evt = new LogEventInfo(LogLevel.Error, "sentry", null, unFormatted, args);

            var expected = target.Layout.Render(evt);

            manager.GetLogger("sentry").Log(evt);

            _fixture.Hub.Received(1)
                    .CaptureEvent(Arg.Is<SentryEvent>(e => e.LogEntry.Formatted == expected));
        }

        [Fact]
        public void Log_HubAccessorReturnsNull_DoesNotThrow()
        {
            _fixture.HubAccessor = () => null;
            var sut = _fixture.GetLogger();
            sut.Error(DefaultMessage);
        }

        [Fact]
        public void Log_DisabledHub_CaptureNotCalled()
        {
            _fixture.Hub.IsEnabled.Returns(false);
            var sut = _fixture.GetLogger();

            sut.Error(DefaultMessage);

            _fixture.Hub.DidNotReceive().CaptureEvent(Arg.Any<SentryEvent>());
        }

        [Fact]
        public void Log_EnabledHub_CaptureCalled()
        {
            _fixture.Hub.IsEnabled.Returns(true);
            var sut = _fixture.GetLogger();

            sut.Error(DefaultMessage);

            _fixture.Hub.Received(1).CaptureEvent(Arg.Any<SentryEvent>());
        }

        [Fact]
        public void Log_NullLogEvent_CaptureNotCalled()
        {
            var sut = _fixture.GetLogger();
            string? message = null;

            // ReSharper disable once AssignNullToNotNullAttribute
            sut.Error(message);

            _fixture.Hub.DidNotReceive().CaptureEvent(Arg.Any<SentryEvent>());
        }

        [Fact]
        public void Log_Properties_AsExtra()
        {
            const string expectedIp = "127.0.0.1";

            var sut = _fixture.GetLogger();

            sut.Error("Something happened: {IPAddress}", expectedIp);

            _fixture.Hub.Received(1)
                    .CaptureEvent(Arg.Is<SentryEvent>(e => e.Extra["IPAddress"].ToString() == expectedIp));
        }

        [Fact]
        public void Log_WithFormat_EventCaptured()
        {
            const string expectedMessage = "Test {structured} log";
            const int param = 10;

            var sut = _fixture.GetLogger();

            sut.Error(expectedMessage, param);

            _fixture.Hub.Received(1).CaptureEvent(Arg.Is<SentryEvent>(p =>
                p.LogEntry.Formatted == $"Test {param} log"
                && p.LogEntry.Message == expectedMessage));
        }

        [Fact]
        public void Log_SourceContextMatchesSentry_NoScopeConfigured()
        {
            var sut = _fixture.GetLogger();

            sut.Error("message {SourceContext}", "Sentry.NLog");

            _fixture.Hub.DidNotReceive().ConfigureScope(Arg.Any<Action<BaseScope>>());
        }

        [Fact]
        public void Log_SourceContextContainsSentry_NoScopeConfigured()
        {
            var sut = _fixture.GetLogger();

            sut.Error("message {SourceContext}", "Sentry");

            _fixture.Hub.DidNotReceive().ConfigureScope(Arg.Any<Action<BaseScope>>());
        }

        [Fact]
        public void Log_WithCustomBreadcrumbLayout_RendersCorrectly()
        {
            var logger = _fixture.GetLogger(o =>
            {
                o.MinimumBreadcrumbLevel = LogLevel.Trace;
                o.BreadcrumbLayout = "${logger}: ${message}";
            });
            const string message = "This is a breadcrumb";

            logger.Debug(message);

            var b = _fixture.Scope.Breadcrumbs.First();

            Assert.Equal($"{logger.Name}: {message}", b.Message);
        }
    }
}
