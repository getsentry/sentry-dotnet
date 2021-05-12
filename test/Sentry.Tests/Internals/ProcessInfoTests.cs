using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Testing;
using Xunit;

namespace Sentry.Tests.Internals
{
    public class ProcessInfoTests
    {
        [Fact]
        public async Task Ctor_StartupTimeSimilarToUtcNow()
        {
            //Arrange
            var options = new SentryOptions();

            //Act
            var sut = new ProcessInfo(options);
            await sut.PreciseAppStartupTask;
            var utcNow = DateTimeOffset.UtcNow;

            //Assert
            Assert.True(utcNow >= sut.StartupTime, "Startup Time is before 'now': +" +
                                                   "StartupTime: " + sut.StartupTime +
                                                   "Now: " + utcNow);

            var diff = (utcNow - sut.StartupTime).Value.TotalSeconds;
            // CI is often slow and the diff stays around 10 seconds. 60 is enough to validate the code though:
            Assert.True(diff <= 60, "diff isn't less expected 60 seconds: " + diff);
        }

        [Fact]
        public async Task Ctor_StartupTimeDetectionModeNone_NoDateTimeSet()
        {
            var options = new SentryOptions {DetectStartupTime = StartupTimeDetectionMode.None};

            var sut = new ProcessInfo(options);
            await sut.PreciseAppStartupTask;

            Assert.Null(sut.BootTime);
            Assert.Null(sut.StartupTime);
        }

        [Fact]
        public async Task Ctor_StartupTimeDetectionModeFast_TimeSet()
        {
            var options = new SentryOptions {DetectStartupTime = StartupTimeDetectionMode.Fast};

            var sut = new ProcessInfo(options);
            await sut.PreciseAppStartupTask;

            Assert.NotNull(sut.BootTime);
            Assert.NotNull(sut.StartupTime);
        }

        [Fact]
        public void Options_DefaultOptionValue_IsBestMode()
        {
            Assert.Equal(StartupTimeDetectionMode.Best, new SentryOptions().DetectStartupTime);
        }

        [Fact]
        public async Task Ctor_DefaultArguments_ImproveStartupTimePrecision()
        {
            // Not passing a mock callback here so this is 'an integration test' with GetCurrentProcess()
            var logger = Substitute.For<IDiagnosticLogger>();
            var now = DateTimeOffset.UtcNow;
            var sut = new ProcessInfo(new SentryOptions {DiagnosticLogger = logger});
            var initialTime = sut.StartupTime;
            await sut.PreciseAppStartupTask;

            Assert.NotNull(initialTime);
            if (initialTime == sut.StartupTime)
            {
                // If the task completed before we awaited:
                Assert.True(sut.StartupTime <= now);
            }
            else
            {
                Assert.NotEqual(initialTime, sut.StartupTime);
                // The SDK init time must have happened before the process started.
                Assert.True(sut.StartupTime < initialTime, "Startup Time is not before 'initialTime': +" +
                                                           "StartupTime: " + sut.StartupTime +
                                                           "initialTime: " + initialTime);
            }
        }

        [Theory]
        [InlineData(StartupTimeDetectionMode.None, false)]
        [InlineData(StartupTimeDetectionMode.Fast, false)]
        [InlineData(StartupTimeDetectionMode.Best, true)]
        public async Task Ctor_PreciseAppStartCallback_RunsOnlyOnBestMode(
            StartupTimeDetectionMode mode,
            bool fastCallbackInvoked)
        {
            var options = new SentryOptions {DetectStartupTime = mode};
            var check = new Func<DateTimeOffset>(() => DateTimeOffset.MaxValue);

            var sut = new ProcessInfo(options, check);
            await sut.PreciseAppStartupTask;

            Assert.Equal(fastCallbackInvoked, DateTimeOffset.MaxValue == sut.StartupTime);
        }
    }
}
