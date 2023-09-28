using Target = NLog.Targets.Target;

namespace Sentry.NLog.Tests;

public class SentryTargetTests
{
    private const string DefaultMessage = "This is a logged message";

    private class Fixture
    {
        public SentryNLogOptions Options { get; } = new() { Dsn = ValidDsn };

        public IHub Hub { get; } = Substitute.For<IHub>();

        public Func<IHub> HubAccessor { get; set; }

        public IDisposable SdkDisposeHandle { get; set; } = Substitute.For<IDisposable>();

        public Scope Scope { get; }

        public Fixture()
        {
            Hub.IsEnabled.Returns(true);
            HubAccessor = () => Hub;
            Scope = new Scope(new SentryOptions());
            Hub.ConfigureScope(Arg.Invoke(Scope));
        }

        public Target GetTarget(bool asyncTarget = false)
        {
            var target = new SentryTarget(
                Options,
                HubAccessor,
                SdkDisposeHandle,
                new MockClock())
            {
                Name = "sentry",
                Dsn = Options.Dsn ?? Options.DsnLayout,
            };

            if (asyncTarget)
            {
                return new AsyncTargetWrapper(target)
                {
                    Name = "sentry_async"
                };
            }
            return target;
        }

        public LogFactory GetLoggerFactory(bool asyncTarget = false)
        {
            var target = GetTarget(asyncTarget);

            var factory = new LogFactory();

            var config = new LoggingConfiguration(factory);
            config.AddTarget("sentry", target);
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, target);

            factory.Configuration = config;
            return factory;
        }

        public Logger GetLogger() => GetLoggerFactory().GetLogger("sentry");
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void Can_configure_from_xml_file()
    {
        var configXml = $@"
                <nlog throwConfigExceptions='true'>
                    <extensions>
                        <add type='{typeof(SentryTarget).AssemblyQualifiedName}' />
                    </extensions>
                    <targets>
                        <target type='Sentry' name='sentry' dsn='{ValidDsn}' release='1.2.3' environment='test'>
                            <options>
                                <attachStacktrace>True</attachStacktrace>
                            </options>
                        </target>
                    </targets>
                </nlog>";

        var stringReader = new StringReader(configXml);
        var xmlReader = XmlReader.Create(stringReader);
        var logFactory = new LogFactory();
        logFactory.Configuration = new XmlLoggingConfiguration(xmlReader, null, logFactory);

        var t = logFactory.Configuration.FindTargetByName("sentry") as SentryTarget;
        Assert.NotNull(t);
        if (t.Options.Dsn != null)
        {
            Assert.Equal(ValidDsn, t.Options.Dsn);
        }

        Assert.Equal("test", t.Options.Environment);
        Assert.Equal("1.2.3", t.Options.Release);
        Assert.True(t.Options.AttachStacktrace);
    }

    [Fact]
    public void Can_configure_user_from_xml_file()
    {
        var configXml = $@"
                <nlog throwConfigExceptions='true'>
                    <extensions>
                        <add type='{typeof(SentryTarget).AssemblyQualifiedName}' />
                    </extensions>
                    <targets>
                        <target type='Sentry' name='sentry' dsn='{ValidDsn}'>
                            <user username='myUser'>
                                <other name='mood' layout='joyous'/>
                            </user>
                        </target>
                    </targets>
                </nlog>";

        var stringReader = new StringReader(configXml);
        var xmlReader = XmlReader.Create(stringReader);
        var logFactory = new LogFactory();
        logFactory.Configuration = new XmlLoggingConfiguration(xmlReader, null, logFactory);

        var t = logFactory.Configuration.FindTargetByName("sentry") as SentryTarget;
        Assert.NotNull(t);
        Assert.Equal(ValidDsn, t.Options.Dsn);
        Assert.Equal("myUser", t.User?.Username?.ToString());

        var other = t.User?.Other;
        Assert.NotNull(other);
        Assert.NotEmpty(other);
        Assert.Equal("mood", other[0].Name);
        Assert.Equal("joyous", other[0].Layout.ToString());
    }

    [Fact]
    public void Shutdown_DisposesSdk()
    {
        _fixture.Options.InitializeSdk = false;
        var target = _fixture.GetTarget();
        LogManager.Setup().LoadConfiguration(c => c.ForLogger().WriteTo(target));

        var sut = LogManager.GetCurrentClassLogger();

        sut.Error(DefaultMessage);

        _fixture.SdkDisposeHandle.DidNotReceive().Dispose();

        LogManager.Shutdown();

        _fixture.SdkDisposeHandle.Received(1).Dispose();
    }

    [Fact]
    public void Shutdown_NoDisposeHandleProvided_DoesNotThrow()
    {
        _fixture.Options.InitializeSdk = false;
        var factory = _fixture.GetLoggerFactory();

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

        _fixture.Options.MinimumEventLevel = LogLevel.Fatal;
        var logger = _fixture.GetLogger();

        logger.Error(expectedException);

        var b = _fixture.Scope.Breadcrumbs.First();

        Assert.Equal($"{expectedException.GetType()}: {expectedException.Message}", b.Message);
        Assert.Equal(DateTimeOffset.MaxValue, b.Timestamp);
        Assert.Equal(logger.Name, b.Category);
        Assert.Equal(expectedLevel, b.Level);
        Assert.Null(b.Type);
        Assert.NotNull(b.Data);
        Assert.Equal(b.Data["exception_type"], expectedException.GetType().ToString());
        Assert.Equal(b.Data["exception_message"], expectedException.Message);
    }

    [Fact]
    public void Log_NLogSdk_Name()
    {
        _fixture.Options.MinimumEventLevel = LogLevel.Info;
        var logger = _fixture.GetLogger();

        var expected = typeof(SentryTarget).Assembly.GetNameAndVersion();
        logger.Info(DefaultMessage);

        _fixture.Hub.Received(1)
            .CaptureEvent(Arg.Is<SentryEvent>(e => e.Sdk.Name == Constants.SdkName
                                                   && e.Sdk.Version == expected.Version));
    }

    [Fact]
    public void Log_NLogSdk_Packages()
    {
        _fixture.Options.MinimumEventLevel = LogLevel.Info;
        var logger = _fixture.GetLogger();

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

        var evt = new LogEventInfo
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
            .CaptureEvent(Arg.Is<SentryEvent>(e => e.Message.Formatted == expected));
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

        // ReSharper disable once AssignNullToNotNullAttribute
        sut.Error((string)null);

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
    public void Log_AdditionalGroupingKeyProperty_OverrideDefaultFingerprint()
    {
        var expectedGroupingKey = "someGroupingKey";
        var expectedFingerprint = new[]
        {
            "{{ default }}",
            expectedGroupingKey
        };

        var logger = _fixture.GetLogger();

        var evt = LogEventInfo.Create(LogLevel.Error, logger.Name, DefaultMessage);

        evt.Properties[SentryTarget.AdditionalGroupingKeyProperty] = expectedGroupingKey;

        var actualSentryEvent = default(SentryEvent);
        _fixture.Hub.When(h => h.CaptureEvent(Arg.Is<SentryEvent>(
                e => e.Extra[SentryTarget.AdditionalGroupingKeyProperty].ToString() == expectedGroupingKey)))
            .Do(c => actualSentryEvent = c.Arg<SentryEvent>());

        logger.Log(evt);

        Assert.NotNull(actualSentryEvent);
        Assert.Equal(expectedFingerprint, actualSentryEvent.Fingerprint);
    }

    [Fact]
    public void Log_WithFormat_EventCaptured()
    {
        const string expectedMessage = "Test {structured} log";
        const int param = 10;

        var sut = _fixture.GetLogger();

        sut.Error(expectedMessage, param);

        _fixture.Hub.Received(1).CaptureEvent(Arg.Is<SentryEvent>(p =>
            p.Message.Formatted == $"Test {param} log"
            && p.Message.Message == expectedMessage));
    }

    [Fact]
    public void Log_WithCustomBreadcrumbLayout_RendersCorrectly()
    {
        _fixture.Options.BreadcrumbLayout = "${logger}: ${message}";
        _fixture.Options.MinimumBreadcrumbLevel = LogLevel.Trace;

        var factory = _fixture.GetLoggerFactory();
        var sentryTarget = factory.Configuration.FindTargetByName<SentryTarget>("sentry");
        sentryTarget.IncludeEventDataOnBreadcrumbs = true;
        var logger = factory.GetLogger("sentry");

        const string message = "This is a breadcrumb";

        var evt = LogEventInfo.Create(LogLevel.Debug, logger.Name, message);
        evt.Properties["a"] = "b";
        logger.Log(evt);

        var b = _fixture.Scope.Breadcrumbs.First();
        Assert.Equal($"{logger.Name}: {message}", b.Message);
        Assert.Equal("b", b.Data?["a"]);
    }

    [Fact]
    public void Log_WithCustomBreadcrumbCategory_RendersCorrectly()
    {
        _fixture.Options.MinimumBreadcrumbLevel = LogLevel.Trace;

        var factory = _fixture.GetLoggerFactory();
        var sentryTarget = factory.Configuration.FindTargetByName<SentryTarget>("sentry");
        sentryTarget.BreadcrumbCategory = "${level}";
        var logger = factory.GetLogger("sentry");

        const string message = "This is a breadcrumb";
        var evt = LogEventInfo.Create(LogLevel.Debug, logger.Name, message);
        logger.Log(evt);

        var b = _fixture.Scope.Breadcrumbs.First();
        Assert.Equal("Debug", b.Category);
    }

    [Fact]
    public async Task LogManager_WhenFlushCalled_CallsSentryFlushAsync()
    {
        var timeout = TimeSpan.FromSeconds(2);

        _fixture.Options.FlushTimeout = timeout;
        var factory = _fixture.GetLoggerFactory(asyncTarget: true);

        // Verify that it's asynchronous
        Assert.NotEmpty(factory.Configuration.AllTargets.OfType<AsyncTargetWrapper>());

        var logger = factory.GetLogger("sentry");

        var hub = _fixture.Hub;

        logger.Info("Here's a message");
        logger.Debug("Here's another message");
        logger.Error(new Exception(DefaultMessage));

        var testDisposable = Substitute.For<IDisposable>();

        var tcs = new TaskCompletionSource<object>();

        void Continuation(Exception _)
        {
            testDisposable.Dispose();
            tcs.SetResult(null);
        }

        factory.Flush(Continuation, timeout);

        await Task.WhenAny(tcs.Task, Task.Delay(timeout));
        Assert.True(tcs.Task.IsCompleted);

        testDisposable.Received().Dispose();
        await hub.Received().FlushAsync(Arg.Any<TimeSpan>());
    }

    [Fact]
    public void InitializeTarget_InitializesSdk()
    {
        _fixture.Options.Dsn = Sentry.Constants.DisableSdkDsnValue;
        _fixture.SdkDisposeHandle = null;
        _fixture.Options.InitializeSdk = true;

        var logWriter = new StringWriter();

        try
        {
            InternalLogger.LogWriter = logWriter;
            InternalLogger.LogLevel = LogLevel.Debug;

            _fixture.GetLoggerFactory();

            var logOutput = logWriter.ToString();
            Assert.Contains("Init called with an empty string as the DSN. Sentry SDK will be disabled.", logOutput);
        }
        finally
        {
            InternalLogger.LogWriter = null;
            InternalLogger.LogLevel = LogLevel.Off;
        }
    }

    [Fact]
    public void Dsn_ReturnsDsnFromOptions_Null()
    {
        _fixture.Options.Dsn = null;
        var target = (SentryTarget)_fixture.GetTarget();
        Assert.Null(target.Dsn);
    }

    [Fact]
    public void Dsn_ReturnsDsnFromOptions_Instance()
    {
        var expectedDsn = "https://a@sentry.io/1";
        _fixture.Options.Dsn = expectedDsn;
        var target = (SentryTarget)_fixture.GetTarget();
        Assert.Equal(expectedDsn, target.Options.Dsn);
    }

    [Fact]
    public void Dsn_SupportsNLogLayout_Lookup()
    {
        var expectedDsn = "https://a@sentry.io/1";
        var target = (SentryTarget)_fixture.GetTarget();
        target.Dsn = "${var:mydsn}";
        var logFactory = new LogFactory();
        var logConfig = new LoggingConfiguration(logFactory)
        {
            Variables = { ["mydsn"] = expectedDsn }
        };
        logConfig.AddRuleForAllLevels(target);
        logFactory.Configuration = logConfig;
        Assert.Equal(expectedDsn, target.Options.Dsn);
    }

    [Fact]
    public void MinimumEventLevel_SetInOptions_ReturnsValue()
    {
        var expected = LogLevel.Warn;
        _fixture.Options.MinimumEventLevel = expected;
        var target = (SentryTarget)_fixture.GetTarget();
        Assert.Equal(expected.ToString(), target.MinimumEventLevel);
    }

    [Fact]
    public void MinimumEventLevel_Null_ReturnsLogLevelOff()
    {
        _fixture.Options.MinimumEventLevel = null;
        var target = (SentryTarget)_fixture.GetTarget();
        Assert.Equal(LogLevel.Off.ToString(), target.MinimumEventLevel);
    }

    [Fact]
    public void MinimumEventLevel_SetterReplacesOptions()
    {
        _fixture.Options.MinimumEventLevel = LogLevel.Fatal;
        var target = (SentryTarget)_fixture.GetTarget();
        const string expected = "Debug";
        target.MinimumEventLevel = expected;
        Assert.Equal(expected, target.MinimumEventLevel);
    }

    [Fact]
    public void MinimumBreadcrumbLevel_SetInOptions_ReturnsValue()
    {
        var expected = LogLevel.Warn;
        _fixture.Options.MinimumBreadcrumbLevel = expected;
        var target = (SentryTarget)_fixture.GetTarget();
        Assert.Equal(expected.ToString(), target.MinimumBreadcrumbLevel);
    }

    [Fact]
    public void MinimumBreadcrumbLevel_Null_ReturnsLogLevelOff()
    {
        _fixture.Options.MinimumBreadcrumbLevel = null;
        var target = (SentryTarget)_fixture.GetTarget();
        Assert.Equal(LogLevel.Off.ToString(), target.MinimumBreadcrumbLevel);
    }

    [Fact]
    public void MinimumBreadcrumbLevel_SetterReplacesOptions()
    {
        _fixture.Options.MinimumBreadcrumbLevel = LogLevel.Fatal;
        var target = (SentryTarget)_fixture.GetTarget();
        const string expected = "Debug";
        target.MinimumBreadcrumbLevel = expected;
        Assert.Equal(expected, target.MinimumBreadcrumbLevel);
    }

    [Fact]
    public void SendEventPropertiesAsData_Default_True()
    {
        var target = (SentryTarget)_fixture.GetTarget();
        Assert.True(target.IncludeEventProperties);
    }

    [Fact]
    public void SendEventPropertiesAsTags_Default_False()
    {
        var target = (SentryTarget)_fixture.GetTarget();
        Assert.False(target.IncludeEventPropertiesAsTags);
    }

    [Fact]
    public void SendEventPropertiesAsTags_ValueFromOptions()
    {
        _fixture.Options.IncludeEventPropertiesAsTags = false;
        var target = (SentryTarget)_fixture.GetTarget();
        Assert.False(target.IncludeEventPropertiesAsTags);
    }

    [Fact]
    public void SendEventPropertiesAsTags_SetterReplacesOptions()
    {
        _fixture.Options.IncludeEventPropertiesAsTags = true;
        var target = (SentryTarget)_fixture.GetTarget();
        target.IncludeEventPropertiesAsTags = false;
        Assert.False(target.IncludeEventPropertiesAsTags);
    }

    [Fact]
    public void IncludeEventDataOnBreadcrumbs_Default_False()
    {
        var target = (SentryTarget)_fixture.GetTarget();
        Assert.False(target.IncludeEventDataOnBreadcrumbs);
    }

    [Fact]
    public void IncludeEventDataOnBreadcrumbs_ValueFromOptions()
    {
        _fixture.Options.IncludeEventDataOnBreadcrumbs = false;
        var target = (SentryTarget)_fixture.GetTarget();
        Assert.False(target.IncludeEventDataOnBreadcrumbs);
    }

    [Fact]
    public void IncludeEventDataOnBreadcrumbs_SetterReplacesOptions()
    {
        _fixture.Options.IncludeEventDataOnBreadcrumbs = true;
        var target = (SentryTarget)_fixture.GetTarget();
        target.IncludeEventDataOnBreadcrumbs = false;
        Assert.False(target.IncludeEventDataOnBreadcrumbs);
    }

    [Fact]
    public void ShutdownTimeoutSeconds_ValueFromOptions()
    {
        const int expected = 60;
        _fixture.Options.ShutdownTimeoutSeconds = expected;
        var target = (SentryTarget)_fixture.GetTarget();
        Assert.Equal(expected, target.ShutdownTimeoutSeconds);
    }

    [Fact]
    public void ShutdownTimeoutSeconds_Default_2Seconds()
    {
        var target = (SentryTarget)_fixture.GetTarget();
        Assert.Equal(2, target.ShutdownTimeoutSeconds);
    }

    [Fact]
    public void ShutdownTimeoutSeconds_SetterReplacesOptions()
    {
        var expected = 60;
        _fixture.Options.ShutdownTimeoutSeconds = int.MinValue;
        var target = (SentryTarget)_fixture.GetTarget();
        target.ShutdownTimeoutSeconds = expected;
        Assert.Equal(expected, target.ShutdownTimeoutSeconds);
    }

    [Fact]
    public void FlushTimeoutSeconds_ValueFromOptions()
    {
        var expected = 10;
        _fixture.Options.FlushTimeout = TimeSpan.FromSeconds(expected);
        var target = (SentryTarget)_fixture.GetTarget();
        Assert.Equal(expected, target.FlushTimeoutSeconds);
    }

    [Fact]
    public void FlushTimeoutSeconds_SetterReplacesOptions()
    {
        var expected = 100;
        _fixture.Options.FlushTimeout = TimeSpan.FromSeconds(expected);
        var target = (SentryTarget)_fixture.GetTarget();
        target.FlushTimeoutSeconds = expected;
        Assert.Equal(expected, target.FlushTimeoutSeconds);
    }

    [Fact]
    public void IgnoreEventsWithNoException_SetterReplacesOptions()
    {
        _fixture.Options.IgnoreEventsWithNoException = false;
        var target = (SentryTarget)_fixture.GetTarget();
        target.IgnoreEventsWithNoException = true;
        Assert.True(target.IgnoreEventsWithNoException);
    }

    [Fact]
    public void FlushTimeoutSeconds_Default_15Seconds()
    {
        var target = (SentryTarget)_fixture.GetTarget();
        Assert.Equal(15, target.FlushTimeoutSeconds);
    }

    [Fact]
    public void BreadcrumbLayout_Null_FallsBackToLayout()
    {
        var target = (SentryTarget)_fixture.GetTarget();
        target.BreadcrumbLayout = null;
        Assert.Equal(target.Layout, target.BreadcrumbLayout);
    }

    [Fact]
    public void Ctor_Options_UseHubAdapter()
        => Assert.Equal(HubAdapter.Instance, new SentryTarget(new SentryNLogOptions()).HubAccessor());

    [Fact]
    public void GetTagsFromLogEvent_ContextProperties()
    {
        var factory = _fixture.GetLoggerFactory();
        var sentryTarget = factory.Configuration.FindTargetByName<SentryTarget>("sentry");
        sentryTarget.Tags.Add(new TargetPropertyWithContext("Logger", "${logger:shortName=true}"));
        sentryTarget.IncludeEventPropertiesAsTags = true;

        var logger = factory.GetLogger("sentry");
        logger.Fatal(DefaultMessage);

        _fixture.Hub.Received(1)
            .CaptureEvent(Arg.Is<SentryEvent>(e => e.Tags["Logger"] == "sentry"));
    }

    [Fact]
    public void GetTagsFromLogEvent_PropertiesMapped()
    {
        var factory = _fixture.GetLoggerFactory();
        var sentryTarget = factory.Configuration.FindTargetByName<SentryTarget>("sentry");
        sentryTarget.IncludeEventPropertiesAsTags = true;

        var logger = factory.GetLogger("sentry");
        logger.Fatal("{a}", "b");

        _fixture.Hub.Received(1)
            .CaptureEvent(Arg.Is<SentryEvent>(e => e.Tags["a"] == "b"));
    }

    [Fact]
    public void GetUserFromLayouts_PropertiesMapped()
    {
        var factory = _fixture.GetLoggerFactory();
        var sentryTarget = factory.Configuration.FindTargetByName<SentryTarget>("sentry");
        sentryTarget.User = new SentryNLogUser
        {
            Username = "${logger:shortName=true}",
        };
        sentryTarget.User.Other!.Add(new TargetPropertyWithContext("mood", "joyous"));

        var logger = factory.GetLogger("sentry");
        logger.Fatal(DefaultMessage);

        _fixture.Hub.Received(1)
            .CaptureEvent(Arg.Is<SentryEvent>(e => e.User.Username == "sentry" && e.User.Other["mood"] == "joyous"));
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
