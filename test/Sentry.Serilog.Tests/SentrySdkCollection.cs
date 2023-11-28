#if NET6_0_OR_GREATER
namespace Sentry.Serilog.Tests;

[CollectionDefinition(nameof(SerilogAspNetSentrySdkTestFixture))]
public sealed class SentrySdkCollection : ICollectionFixture<SerilogAspNetSentrySdkTestFixture>
{
    // This class has no code, and is never instantiated. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
    // See: https://xunit.net/docs/shared-context#collection-fixture
}
#endif
