using System;
using NSubstitute;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Protocol;
using Sentry.Reflection;
using Sentry.Testing;
using Xunit;

namespace Sentry.Tests.Internals
{
    public class MainSentryEventProcessorTests
    {
        public SentryOptions SentryOptions { get; set; } = new SentryOptions();
        internal MainSentryEventProcessor Sut { get; set; }

        public MainSentryEventProcessorTests() => Sut = new MainSentryEventProcessor(SentryOptions);

        [Fact]
        public void Process_ReleaseOnOptions_SetToEvent()
        {
            const string expectedVersion = "1.0 - f4d6b23";
            SentryOptions.Release = expectedVersion;
            var evt = new SentryEvent();

            Sut.Process(evt);

            Assert.Equal(expectedVersion, evt.Release);
        }

        [Fact]
        public void Process_NoReleaseOnOptions_SameAsCachedVersion()
        {
            var evt = new SentryEvent();

            Sut.Process(evt);

            Assert.Equal(Sut.Release, evt.Release);
        }

        [Fact]
        public void Process_EnvironmentOnOptions_SetToEvent()
        {
            const string expected = "Production";
            SentryOptions.Environment = expected;
            var evt = new SentryEvent();

            Sut.Process(evt);

            Assert.Equal(expected, evt.Environment);
        }

        [Fact]
        public void Process_NoEnvironmentOnOptions_SameAsEnvironmentVariable()
        {
            const string expected = "Staging";
            var evt = new SentryEvent();

            EnvironmentVariableGuard.WithVariable(
                Constants.EnvironmentEnvironmentVariable,
                expected,
                () =>
                {
                    Sut.Process(evt);
                });

            Assert.Equal(expected, evt.Environment);
        }

        [Fact]
        public void Process_NoLevelOnEvent_SetToError()
        {
            var evt = new SentryEvent
            {
                Level = null
            };

            Sut.Process(evt);

            Assert.Equal(SentryLevel.Error, evt.Level);
        }

        [Fact]
        public void Process_ExceptionProcessors_Invoked()
        {
            var exceptionProcessor = Substitute.For<ISentryEventExceptionProcessor>();
            SentryOptions.AddExceptionProcessorProvider(() => new[] { exceptionProcessor });

            var evt = new SentryEvent
            {
                Exception = new Exception()
            };

            Sut.Process(evt);

            exceptionProcessor.Received(1).Process(evt.Exception, evt);
        }

        [Fact]
        public void Process_NoExceptionOnEvent_ExceptionProcessorsNotInvoked()
        {
            var invoked = false;

            SentryOptions.AddExceptionProcessorProvider(() =>
            {
                invoked = true;
                return new[] { Substitute.For<ISentryEventExceptionProcessor>() };
            });

            var evt = new SentryEvent();
            Sut.Process(evt);

            Assert.False(invoked);
        }

        [Fact]
        public void Process_Platform_CSharp()
        {
            var evt = new SentryEvent();
            Sut.Process(evt);

            Assert.Equal(Constants.Platform, evt.Platform);
        }

        [Fact]
        public void Process_Modules_NotEmpty()
        {
            var evt = new SentryEvent();
            Sut.Process(evt);

            Assert.NotEmpty(evt.Modules);
        }

        [Fact]
        public void Process_SdkNameAndVersion_ToDefault()
        {
            var evt = new SentryEvent();

            Sut.Process(evt);

            Assert.Equal(Constants.SdkName, evt.Sdk.Name);
            Assert.Equal(typeof(ISentryClient).Assembly.GetNameAndVersion().Version, evt.Sdk.Version);
        }

        [Fact]
        public void Process_SdkNameAndVersion_NotModified()
        {
            const string expectedName = "TestSdk";
            const string expectedVersion = "1.0";

            var evt = new SentryEvent
            {
                Sdk =
                {
                    Name = expectedName,
                    Version = expectedVersion
                }
            };

            Sut.Process(evt);

            Assert.Equal(expectedName, evt.Sdk.Name);
            Assert.Equal(expectedVersion, evt.Sdk.Version);
        }
    }
}
