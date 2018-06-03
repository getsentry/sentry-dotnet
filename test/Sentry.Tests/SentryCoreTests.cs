using System;
using System.Linq;
using System.Reflection;
using Sentry.Extensibility;
using Xunit;

namespace Sentry.Tests
{
    public class SentryCoreTests
    {
        [Fact]
        public void Init_BrokenDsn_Throws()
        {
            Assert.Throws<UriFormatException>(() => SentryCore.Init("invalid stuff"));
        }

        [Fact]
        public void Init_EmptyDsn_DisabledSdk()
        {
            Assert.False(SentryCore.IsEnabled);
            try
            {
                SentryCore.Init(string.Empty);
                Assert.False(SentryCore.IsEnabled);
            }
            finally
            {
                SentryCore.CloseAndFlush();
            }
        }

        [Fact]
        public void CloseAndFlush_MultipleCalls_NoOp()
        {
            SentryCore.CloseAndFlush();
            SentryCore.CloseAndFlush();
            Assert.False(SentryCore.IsEnabled);
        }

        [Fact]
        public void Implements_Sdk()
        {
            var sdk = typeof(ISentryClient).GetMembers(BindingFlags.Public | BindingFlags.Instance);
            var sentryCore = typeof(SentryCore).GetMembers(BindingFlags.Public | BindingFlags.Static);

            Assert.Empty(sdk.Select(m => m.ToString()).Except(sentryCore.Select(m => m.ToString())));
        }

        [Fact]
        public void Implements_ScopeManagement()
        {
            var scopeManagement = typeof(ISentryScopeManagement).GetMembers(BindingFlags.Public | BindingFlags.Instance);
            var sentryCore = typeof(SentryCore).GetMembers(BindingFlags.Public | BindingFlags.Static);

            Assert.Empty(scopeManagement.Select(m => m.ToString()).Except(sentryCore.Select(m => m.ToString())));
        }
    }
}
