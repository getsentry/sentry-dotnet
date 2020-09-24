using NUnit.Framework;

namespace Sentry.PlatformAbstractions.Tests
{
    public class RuntimeTests
    {
        [Test]
        public void Current_SameInstance()
        {
            Assert.AreSame(Runtime.Current, Runtime.Current);
        }
    }
}
