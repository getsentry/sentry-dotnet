using Sentry.Tests.Helpers.Reflection;

namespace Sentry.Tests.Protocol;

public abstract class ImmutableTests<TType>
{
    [Fact]
    public void Type_IsImmutable()
    {
        typeof(TType).AssertImmutable();
    }
}
