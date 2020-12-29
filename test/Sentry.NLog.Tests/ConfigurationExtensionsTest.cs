using System;
using NLog;
using NLog.Config;
using NLog.Layouts;
using Xunit;

namespace Sentry.NLog.Tests
{
    public class ConfigurationExtensionsTest
    {
        private readonly LoggingConfiguration _sut = new();

        [Fact]
        public void AddSentry_Parameterless_DefaultTargetName()
        {
            var actual = _sut.AddSentry();
            Assert.Equal(ConfigurationExtensions.DefaultTargetName, actual.AllTargets[0].Name);
        }

        [Fact]
        public void AddSentry_ConfigCallback_CallbackInvoked()
        {
            var expected = TimeSpan.FromDays(1);
            var actual = _sut.AddSentry(o => o.FlushTimeout = expected);
            var sentryTarget = Assert.IsType<SentryTarget>(actual.AllTargets[0]);
            Assert.Equal(expected.TotalSeconds, sentryTarget.FlushTimeoutSeconds);
        }

        [Fact]
        public void AddSentry_DsnAndConfigCallback_CallbackInvokedAndDsnUsed()
        {
            var expectedTimeout = TimeSpan.FromDays(1);
            var expectedDsn = "https://a@sentry.io/1";
            var actual = _sut.AddSentry(expectedDsn, o => o.FlushTimeout = expectedTimeout);
            var sentryTarget = Assert.IsType<SentryTarget>(actual.AllTargets[0]);
            Assert.Equal(expectedTimeout.TotalSeconds, sentryTarget.FlushTimeoutSeconds);
            Assert.Equal(expectedDsn, sentryTarget.Options.Dsn);
        }

        [Fact]
        public void AddTag_SetToTarget()
        {
            var sut = new SentryNLogOptions();

            Layout layout = "b";
            sut.AddTag("a", layout);

            var tag = Assert.Single(sut.Tags);
            Assert.Equal("a", tag.Name);
            Assert.Equal(layout, tag.Layout);
        }
    }
}
