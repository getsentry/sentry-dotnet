using System;
using System.Threading.Tasks;
using Sentry.Internal;
using Sentry.Testing;
using Xunit;

namespace Sentry.Tests.Internals
{
    public class ProcessInfoTests
    {
        [Fact]
        public async Task SetupStartupTime_StartupTimeSet()
        {
            //Arrange
            var unsetDateTime = new DateTime(1995, 01, 01);
            var options = new SentryOptions();
            var processInfo = new ProcessInfo(options);
            processInfo.StartupTime = unsetDateTime;
            var func = new Func<bool>(() => processInfo.StartupTime != unsetDateTime);

            //Act
            processInfo.SetupStartupTime();

            //Assert
            Assert.True(await func.WaitConditionAsync(true, TimeSpan.FromSeconds(1)));
        }


        [Fact]
        public async Task SetupStartupTime_MultipleCalls_DoesntCrash()
        {
            //Arrange
            var unsetDateTime = new DateTime(1995, 01, 01);
            var options = new SentryOptions();
            var processInfo = new ProcessInfo(options);
            processInfo.StartupTime = unsetDateTime;
            var func = new Func<bool>(() => processInfo.StartupTime != unsetDateTime);

            //Act
            for (int i = 0; i < 10; i++)
            { 
                processInfo.SetupStartupTime();
            }

            //Assert
            Assert.True(await func.WaitConditionAsync(true, TimeSpan.FromSeconds(2)));
        }
    }
}
