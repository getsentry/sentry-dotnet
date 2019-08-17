using Xunit;

namespace Sentry.PlatformAbstractions.Tests
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
