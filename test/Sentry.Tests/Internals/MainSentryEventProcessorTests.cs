using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using NSubstitute;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Protocol;
using Sentry.Reflection;
using Sentry.Testing;
using Xunit;
using Constants = Sentry.Internal.Constants;

namespace Sentry.Tests.Internals
{
    public class MainSentryEventProcessorTests
    {
        private class Fixture
        {
            public ISentryStackTraceFactory SentryStackTraceFactory { get; set; } = Substitute.For<ISentryStackTraceFactory>();
            public SentryOptions SentryOptions { get; set; } = new SentryOptions();
            public MainSentryEventProcessor GetSut() => new MainSentryEventProcessor(SentryOptions, () => SentryStackTraceFactory);
        }

        private readonly Fixture _fixture = new Fixture();

        [Fact]
        public void Process_DefaultOptions_NoUserNameSet()
        {
            var sut = _fixture.GetSut();
            var evt = new SentryEvent();

            _ = sut.Process(evt);

            Assert.Null(evt.User.Username);
        }

        [Fact]
        public void Process_SendDefaultPiiTrueIdEnvironmentDefault_UserNameSet()
        {
            var evt = new SentryEvent();

            _fixture.SentryOptions.SendDefaultPii = true;
            var sut = _fixture.GetSut();

            _ = sut.Process(evt);

            Assert.Equal(Environment.UserName, evt.User.Username);
        }

        [Fact]
        public void Process_SendDefaultPiiTrueIdEnvironmentTrue_UserNameSet()
        {
            var evt = new SentryEvent();

            _fixture.SentryOptions.SendDefaultPii = true;
            _fixture.SentryOptions.IsEnvironmentUser = true;
            var sut = _fixture.GetSut();

            _ = sut.Process(evt);

            Assert.Equal(Environment.UserName, evt.User.Username);
        }

        [Fact]
        public void Process_SendDefaultPiiTrueIdEnvironmentFalse_UserNameNotSet()
        {
            var evt = new SentryEvent();
            _fixture.SentryOptions.SendDefaultPii = true;
            _fixture.SentryOptions.IsEnvironmentUser = false;

            var sut = _fixture.GetSut();
            _ = sut.Process(evt);

            Assert.Null(evt.User.Username);
        }

        [Fact]
        public void Process_DefaultOptions_NoServerNameSet()
        {
            var sut = _fixture.GetSut();
            var evt = new SentryEvent();

            _ = sut.Process(evt);

            Assert.Null(evt.ServerName);
        }

        [Fact]
        public void Process_SendDefaultPiiTrue_ServerNameSet()
        {
            var sut = _fixture.GetSut();
            var evt = new SentryEvent();

            _fixture.SentryOptions.SendDefaultPii = true;
            _ = sut.Process(evt);

            Assert.Equal(Environment.MachineName, evt.ServerName);
        }

        [Fact]
        public void Process_SendDefaultPiiTrueNameOnOptionAndOnEvent_ServerNameNotOverwritten()
        {
            var expectedServerName = "expected server name";
            _fixture.SentryOptions.ServerName = "Value on options doesn't take precedence over the event";
            var sut = _fixture.GetSut();
            var evt = new SentryEvent { ServerName = expectedServerName };
            _fixture.SentryOptions.SendDefaultPii = true;
            _ = sut.Process(evt);

            Assert.Equal(expectedServerName, evt.ServerName);
        }

        [Fact]
        public void Process_SendDefaultPiiTrueAndNameOnOption_ServerNameSetToOptionsValue()
        {
            var expectedServerName = "expected server name";
            _fixture.SentryOptions.ServerName = expectedServerName;
            var sut = _fixture.GetSut();
            var evt = new SentryEvent();

            _fixture.SentryOptions.SendDefaultPii = true;
            _ = sut.Process(evt);

            Assert.Equal(expectedServerName, evt.ServerName);
        }

        [Fact]
        public void Process_SendDefaultPiiFalseAndNameOnOption_ServerNameSetToOptionsValue()
        {
            var expectedServerName = "expected server name";
            _fixture.SentryOptions.ServerName = expectedServerName;
            var sut = _fixture.GetSut();
            var evt = new SentryEvent();

            _fixture.SentryOptions.SendDefaultPii = false;
            _ = sut.Process(evt);

            Assert.Equal(expectedServerName, evt.ServerName);
        }

        [Fact]
        public void Process_ReleaseOnOptions_SetToEvent()
        {
            const string expectedVersion = "1.0 - f4d6b23";
            _fixture.SentryOptions.Release = expectedVersion;
            var sut = _fixture.GetSut();
            var evt = new SentryEvent();

            _ = sut.Process(evt);

            Assert.Equal(expectedVersion, evt.Release);
        }

        [Fact]
        public void Process_NoReleaseOnOptions_SameAsCachedVersion()
        {
            var sut = _fixture.GetSut();
            var evt = new SentryEvent();

            _ = sut.Process(evt);

            Assert.Equal(sut.Release, evt.Release);
        }

        [Theory]
        [InlineData(null, Constants.ProductionEnvironmentSetting)] // Missing: will get default value.
        [InlineData("", Constants.ProductionEnvironmentSetting)] // Missing: will get default value.
        [InlineData(" ", Constants.ProductionEnvironmentSetting)] // Missing: will get default value.
        [InlineData("a", "a")] // Provided: nothing will change.
        [InlineData("production", "production")] // Provided: nothing will change. (value has a lower case 'p', different to default value)
        [InlineData("aBcDe F !@#$ gHi", "aBcDe F !@#$ gHi")] // Provided: nothing will change. (Case check)
        public void Process_EnvironmentOnOptions_SetToEvent(string environment, string expectedEnvironment)
        {
            _fixture.SentryOptions.Environment = environment;
            var sut = _fixture.GetSut();
            var evt = new SentryEvent();

            _ = sut.Process(evt);

            Assert.Equal(expectedEnvironment, evt.Environment);
        }

        [Theory]
        [InlineData(null, Constants.ProductionEnvironmentSetting)] // Missing: will get default value.
        [InlineData("", Constants.ProductionEnvironmentSetting)] // Missing: will get default value.
        [InlineData(" ", Constants.ProductionEnvironmentSetting)] // Missing: will get default value.
        [InlineData("a", "a")] // Provided: nothing will change.
        [InlineData("Production", "Production")] // Provided: nothing will change. (value has a upper case 'p', different to default value)
        [InlineData("aBcDe F !@#$ gHi", "aBcDe F !@#$ gHi")] // Provided: nothing will change. (Case check)
        public void Process_NoEnvironmentOnOptions_SameAsEnvironmentVariable(string environment, string expectedEnvironment)
        {
            var sut = _fixture.GetSut();
            var evt = new SentryEvent();

            EnvironmentVariableGuard.WithVariable(
                Constants.EnvironmentEnvironmentVariable,
                environment,
                () =>
                {
                    _ = sut.Process(evt);
                });

            Assert.Equal(expectedEnvironment, evt.Environment);
        }

        [Fact]
        public void Process_NoLevelOnEvent_SetToError()
        {
            var sut = _fixture.GetSut();
            var evt = new SentryEvent
            {
                Level = null
            };

            _ = sut.Process(evt);

            Assert.Equal(SentryLevel.Error, evt.Level);
        }

        [Fact]
        public void Process_NoExceptionOnEvent_ExceptionProcessorsNotInvoked()
        {
            var invoked = false;

            _fixture.SentryOptions.AddExceptionProcessorProvider(() =>
            {
                invoked = true;
                return new[] { Substitute.For<ISentryEventExceptionProcessor>() };
            });
            var sut = _fixture.GetSut();

            var evt = new SentryEvent();
            _ = sut.Process(evt);

            Assert.False(invoked);
        }

        [Fact]
        public void Process_Platform_CSharp()
        {
            var sut = _fixture.GetSut();
            var evt = new SentryEvent();
            _ = sut.Process(evt);

            Assert.Equal(Sentry.Protocol.Constants.Platform, evt.Platform);
        }

        [Fact]
        public void Process_Modules_NotEmpty()
        {
            var sut = _fixture.GetSut();
            var evt = new SentryEvent();
            _ = sut.Process(evt);

            Assert.NotEmpty(evt.Modules);
        }

        [Fact]
        public void Process_Modules_IsEmpty_WhenSpecified()
        {
            _fixture.SentryOptions.ReportAssemblies = false;
            var sut = _fixture.GetSut();
            var evt = new SentryEvent();
            _ = sut.Process(evt);

            Assert.Empty(evt.Modules);
        }

        [Fact]
        public void Process_SdkNameAndVersion_ToDefault()
        {
            var sut = _fixture.GetSut();
            var evt = new SentryEvent();

            _ = sut.Process(evt);

            Assert.Equal(Constants.SdkName, evt.Sdk.Name);
            Assert.Equal(typeof(ISentryClient).Assembly.GetNameAndVersion().Version, evt.Sdk.Version);
        }

        [Fact]
        public void Process_SdkNameAndVersion_NotModified()
        {
            const string expectedName = "TestSdk";
            const string expectedVersion = "1.0";
            var sut = _fixture.GetSut();

            var evt = new SentryEvent
            {
                Sdk =
                {
                    Name = expectedName,
                    Version = expectedVersion
                }
            };

            _ = sut.Process(evt);

            Assert.Equal(expectedName, evt.Sdk.Name);
            Assert.Equal(expectedVersion, evt.Sdk.Version);
        }

        [Fact]
        public void Process_AttachStacktraceTrueAndNoExceptionInEvent_CallsStacktraceFactory()
        {
            _fixture.SentryOptions.AttachStacktrace = true;
            var sut = _fixture.GetSut();

            var evt = new SentryEvent();
            _ = sut.Process(evt);

            _ = _fixture.SentryStackTraceFactory.Received(1).Create();
        }

        [Fact]
        public void Process_AttachStacktraceTrueAndExistentThreadInEvent_AddsNewThread()
        {
            var expected = new SentryStackTrace();
            _ = _fixture.SentryStackTraceFactory.Create(Arg.Any<Exception>()).Returns(expected);
            _fixture.SentryOptions.AttachStacktrace = true;
            var sut = _fixture.GetSut();

            Thread.CurrentThread.Name = "second";
            var evt = new SentryEvent { SentryThreads = new []{ new SentryThread { Name = "first" }}};
            _ = sut.Process(evt);

            Assert.Equal(2, evt.SentryThreads.Count());
            Assert.Equal("first", evt.SentryThreads.First().Name);
            Assert.Equal("second", evt.SentryThreads.Last().Name);
        }

        [Fact]
        public void Process_AttachStacktraceTrueAndExceptionInEvent_DoesNotCallStacktraceFactory()
        {
            _fixture.SentryOptions.AttachStacktrace = true;
            var sut = _fixture.GetSut();

            var evt = new SentryEvent(new Exception());
            _ = sut.Process(evt);

            _ = _fixture.SentryStackTraceFactory.DidNotReceive().Create();
        }

        [Fact]
        public void Process_DeviceTimezoneSet()
        {
            var sut = _fixture.GetSut();

            var evt = new SentryEvent();
            _ = sut.Process(evt);

            Assert.Equal(TimeZoneInfo.Local, evt.Contexts.Device.Timezone);
        }

        [Theory]
        [MemberData(nameof(CultureInfoTestCase))]
        public void Process_CultureInfo_ValuesOnContext(Action<CultureInfo> setter, Func<CultureInfo> getter, string key)
        {
            // Arrange
            var originalValue = getter();
            try
            {
                setter(CultureInfo.CreateSpecificCulture("pt-BR"));
                var sut = _fixture.GetSut();

                var evt = new SentryEvent();
                // Act
                _ = sut.Process(evt);

                // Assert
                dynamic ret = evt.Contexts[key];
#pragma warning disable IDE0058 // Expression value is never used, cannot use _ = because it'll affect the test result
                Assert.Equal(getter().Name, ret["Name"]);
                Assert.Equal(getter().DisplayName, ret["DisplayName"]);
                Assert.Equal(getter().Calendar.GetType().Name, ret["Calendar"]);
#pragma warning restore IDE0058
            }
            finally
            {
                setter(originalValue);
            }
        }

        public static IEnumerable<object[]> CultureInfoTestCase()
        {
            yield return new object[]
            {
                new Action<CultureInfo>(c => CultureInfo.CurrentUICulture = c),
                new Func<CultureInfo>(() => CultureInfo.CurrentUICulture),
                nameof(CultureInfo.CurrentUICulture)
            };
            yield return new object[]
            {
                new Action<CultureInfo>(c => CultureInfo.CurrentCulture = c),
                new Func<CultureInfo>(() => CultureInfo.CurrentCulture),
                nameof(CultureInfo.CurrentCulture)
            };
        }
    }
}
