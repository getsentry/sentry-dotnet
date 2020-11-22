using Sentry.PlatformAbstractions;
using Xunit;

namespace Sentry.Tests.PlatformAbstractions
{
    public class RuntimeTests
    {
        [Fact]
        public void Current_Equal()
        {
            Assert.Equal(Runtime.Current, Runtime.Current);
        }
    }
}
