using System;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Internal;
using Sentry.Testing;
using Xunit;

namespace Sentry.Tests.Internals
{
    public class ProcessInfoTests
    {
        [Fact]
        public void Ctor_StartupTimeSimilarToUtcNow()
        {
            //Arrange
            var options = new SentryOptions();

            //Act
            var processInfo = new ProcessInfo(options);
            var utcNow = DateTimeOffset.UtcNow;

            //Assert
            Assert.True(utcNow >= processInfo.StartupTime);
            Assert.True((utcNow - processInfo.StartupTime).Value.TotalSeconds <= 1);
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
        public void Ctor_DefaultOptionValue_IsBestMode()
        {
            Assert.Equal(StartupTimeDetectionMode.Best, new SentryOptions().DetectStartupTime);
        }

        [Fact]
        public async Task Ctor_DefaultArguments_ImproveStartupTimePrecision()
        {
            // Not passing a mock callback here so this is 'an integration test' with GetCurrentProcess()
            var sut = new ProcessInfo(new SentryOptions());
            var initialTime = sut.StartupTime;
            await sut.PreciseAppStartupTask;

            Assert.NotEqual(initialTime, sut.StartupTime);
            // The SDK init time must have happened before the process started.
            Assert.True(sut.StartupTime < initialTime);
        }

    }
}
