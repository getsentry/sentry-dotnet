using Xunit;

namespace Sentry.Tests
{
    [CollectionDefinition(nameof(SentrySdkCollection))]
    public sealed class SentrySdkCollection : ICollectionFixture<SentrySdkCollection>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
        // See: http://xunit.github.io/docs/shared-context.html#collection-fixture
    }
}
