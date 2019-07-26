using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using NLog.Common;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
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

            public Target GetTarget(Action<SentryNLogOptions> customConfig = null, bool asyncTarget = false)
            {
                var options = Options;
                if (customConfig != null)
                {
                    options = new SentryNLogOptions();
                    customConfig(options);
                }

                Target target = new SentryTarget(
                    options,
                    HubAccessor,
                    SdkDisposeHandle,
                    Clock)
                {
                    Name = "sentry",
                    Dsn = ValidDsnWithoutSecret,
                };

                if (asyncTarget)
                {
                    target = new AsyncTargetWrapper(target)
                    {
                        Name = "sentry_async"
                    };
                }
                return target;
            }

            public LogFactory GetLoggerFactory(Action<SentryNLogOptions> customConfig = null, bool asyncTarget = false)
            {
                Target target = GetTarget(customConfig, asyncTarget);

                var config = new LoggingConfiguration();
                config.AddTarget("sentry", target);
                config.AddRule(LogLevel.Trace, LogLevel.Fatal, target);

                var factory = new LogFactory(config);

                return factory;
            }

            public (LogFactory factory, Target target) GetLoggerFactoryAndTarget(Action<SentryNLogOptions> customConfig = null, bool asyncTarget = false)
            {
                var factory = GetLoggerFactory(customConfig, asyncTarget);

                return (factory, factory.Configuration.AllTargets.OfType<SentryTarget>().FirstOrDefault());
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

            SentryEvent actual = null;
            _fixture.Hub.When(h => h.CaptureEvent(Arg.Any<SentryEvent>()))
                    .Do(c => actual = c.Arg<SentryEvent>());

            logger.Info(DefaultMessage);

            var expected = typeof(SentryTarget).Assembly.GetNameAndVersion();

            Assert.NotNull(actual);
            var package = Assert.Single(actual.Sdk.Packages);
            Assert.Equal("nuget:" + expected.Name, package.Name);
            Assert.Equal(expected.Version, package.Version);
        }

        [Theory]
        [ClassData(typeof(LogLevelData))]
        public void Log_LoggerLevel_Set(LogLevel nlogLevel, SentryLevel? sentryLevel)
        {
            // Make sure test cases are not filtered out by the default min levels:
            _fixture.Options.MinimumEventLevel = LogLevel.Trace;
            _fixture.Options.MinimumBreadcrumbLevel = LogLevel.Trace;

            var logger = _fixture.GetLogger();

            var evt = new LogEventInfo()
            {
                Message = DefaultMessage,
                Level = nlogLevel
            };

            logger.Log(evt);

            _fixture.Hub.Received(1)
                .CaptureEvent(Arg.Is<SentryEvent>(e => e.Level == sentryLevel));
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
            string message = null;

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

        [Fact]
        public async Task LogManager_WhenFlushCalled_CallsSentryFlushAsync()
        {
            const int NLogTimeout = 2;
            var timeout = TimeSpan.FromSeconds(NLogTimeout);

            LogFactory factory = _fixture.GetLoggerFactory(o => o.FlushTimeout = timeout, asyncTarget: true);

            LogManager.Configuration = factory.Configuration;

            // Verify that it's asynchronous
            Assert.NotEmpty(factory.Configuration.AllTargets.OfType<AsyncTargetWrapper>());

            var logger = factory.GetLogger("sentry");

            var hub = _fixture.Hub;

            logger.Info("Here's a message");
            logger.Debug("Here's another message");
            logger.Error(new Exception(DefaultMessage));

            var testDisposable = Substitute.For<IDisposable>();

            AsyncContinuation continuation = e =>
            {
                testDisposable.Dispose();
            };

            factory.Flush(continuation, timeout);

            await Task.Delay(timeout);

            testDisposable.Received().Dispose();
            hub.Received().FlushAsync(Arg.Any<TimeSpan>()).GetAwaiter().GetResult();
        }

        internal class LogLevelData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { LogLevel.Debug, SentryLevel.Debug };
                yield return new object[] { LogLevel.Trace, SentryLevel.Debug };
                yield return new object[] { LogLevel.Info, SentryLevel.Info };
                yield return new object[] { LogLevel.Warn, SentryLevel.Warning };
                yield return new object[] { LogLevel.Error, SentryLevel.Error };
                yield return new object[] { LogLevel.Fatal, SentryLevel.Fatal };
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
