namespace Sentry.Tests.Internals;

[UsesVerify]
public class CollectionExtensionsTests
{
    [Fact]
    [Trait("Category", "Verify")]
    public Task GetOrCreate_invalid_type()
    {
        var dictionary = new ConcurrentDictionary<string, object> {["key"] = 1};
        return Throws(() => dictionary.GetOrCreate<Value>("key"))
            .IgnoreStackTrace();
    }

    class Value
    {
    }
}
