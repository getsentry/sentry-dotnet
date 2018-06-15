using Sentry.Tests.Helpers.Reflection;
using Xunit;

// ReSharper disable once CheckNamespace
namespace Sentry.Tests
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
