using Sentry.PlatformAbstractions;
using Xunit;

namespace Sentry.Tests.PlatformAbstractions
{
    public class RuntimeTests
    {
        [Fact]
        public void Current_SameInstance()
        {
            Assert.Same(Runtime.Current, Runtime.Current);
        }
    }
}
