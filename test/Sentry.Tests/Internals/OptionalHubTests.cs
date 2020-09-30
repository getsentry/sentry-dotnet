using System;
using System.Threading.Tasks;
using Sentry.Internal;
using Xunit;

namespace Sentry.Tests.Internals
{
    public class OptionalHubTests
    {
        // Issue: https://github.com/getsentry/sentry-dotnet/issues/123
        [Fact]
        public void FromOptions_NoDsn_DisposeDoesNotThrow()
        {
            var sut = OptionalHub.FromOptions(new SentryOptions()) as IDisposable;
            sut?.Dispose();
        }

        [Fact]
        public Task FromOptions_NoDsn_FlushAsyncDoesNotThrow()
        {
            var sut = OptionalHub.FromOptions(new SentryOptions());
            return sut.FlushAsync(TimeSpan.FromDays(1));
        }
    }
}
