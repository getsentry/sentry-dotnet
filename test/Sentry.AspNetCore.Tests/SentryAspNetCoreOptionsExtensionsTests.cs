using Microsoft.Extensions.Logging;
using Sentry.Extensions.Logging;
using Xunit;

namespace Sentry.AspNetCore.Tests
{
    public class SentryAspNetCoreOptionsExtensionsTests
    {
        public SentryAspNetCoreOptions Sut { get; set; } = new SentryAspNetCoreOptions();
        public SentryLoggingOptions SentryLoggingOptions { get; set; } = new SentryLoggingOptions();

        [Fact]
        public void Apply_NullLogging_DefaultLevels()
        {
            Sut.Logging = null;
            Sut.Apply(SentryLoggingOptions);

            Assert.Equal(LogLevel.Information, SentryLoggingOptions.MinimumBreadcrumbLevel);
            Assert.Equal(LogLevel.Error, SentryLoggingOptions.MinimumEventLevel);
        }

        [Fact]
        public void Apply_WithLogging_OverridesDefault()
        {
            Sut.Logging.MinimumBreadcrumbLevel = LogLevel.Critical;
            Sut.Logging.MinimumEventLevel = LogLevel.Critical;

            Sut.Apply(SentryLoggingOptions);

            Assert.Equal(LogLevel.Critical, SentryLoggingOptions.MinimumBreadcrumbLevel);
            Assert.Equal(LogLevel.Critical, SentryLoggingOptions.MinimumEventLevel);
        }

        [Fact]
        public void Apply_OnInit_TakesSentryOption()
        {
            Sut.Apply(SentryLoggingOptions);

            var expected = new SentryOptions();
            SentryLoggingOptions.ConfigureOptionsActions.ForEach(a => a(expected));

            Assert.Equal(expected, Sut.SentryOptions);
        }

        [Fact]
        public void Apply_OnInit_InvokesOptionActions()
        {
            var invoked = false;

            Sut.ConfigureOptionsActions.Add(_ => invoked = true);
            Sut.Apply(SentryLoggingOptions);

            var expected = new SentryOptions();
            SentryLoggingOptions.ConfigureOptionsActions.ForEach(a => a(expected));

            Assert.True(invoked);
        }
    }
}
