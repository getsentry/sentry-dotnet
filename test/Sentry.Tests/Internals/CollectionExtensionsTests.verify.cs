namespace Sentry.Tests.Internals;

public class CollectionExtensionsTests
{
    [Fact]
    public Task GetOrCreate_invalid_type()
    {
        var dictionary = new ConcurrentDictionary<string, object> { ["key"] = 1 };
        return Throws(() => dictionary.GetOrCreate<Value>("key"))
            .IgnoreStackTrace();
    }

    private class Value
    {
    }
}
