using Xunit;

namespace Sentry.Protocol.Tests
{
    public abstract class ImmutableTests<TType>
    {
        [Fact]
        public void Type_IsImmutable()
        {
            typeof(TType).AssertImmutable();
        }
    }
}
