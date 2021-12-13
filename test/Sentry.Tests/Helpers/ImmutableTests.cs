using Sentry.Tests.Helpers.Reflection;

// ReSharper disable once CheckNamespace
namespace Sentry.Tests;

public abstract class ImmutableTests<TType>
{
    [Fact]
    public void Type_IsImmutable()
    {
        typeof(TType).AssertImmutable();
    }
}
