#if !NETCOREAPP // Test runner fails when running on netcoreapp
using System;
using System.Threading;
using Sentry.Integrations;
using Xunit;

namespace Sentry.Tests.Integrations
{
    public class AppDomainAdapterTests
    {
        [Fact]
        public void UnhandledException_FiredOnExceptionUnhandledInThread()
        {
            var evt = new ManualResetEventSlim(false);
            AppDomainAdapter.Instance.UnhandledException += (sender, args) =>
            {
                evt.Set();
            };

            var thread = new Thread(() => throw new Exception())
            {
                IsBackground = false
            };

            thread.Start();
            Assert.True(evt.Wait(TimeSpan.FromSeconds(3)));
        }
    }
}
#endif
