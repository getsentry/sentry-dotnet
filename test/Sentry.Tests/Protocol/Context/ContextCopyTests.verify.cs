namespace Sentry.Tests.Protocol.Context;

[UsesVerify]
[UniqueForAot]
public class ContextCopyTests
{
    [Fact]
    public async Task CopyTo_Basic()
    {
        var target = new Contexts();

        var source = new Contexts
        {
            App = {Name = "Test", Version = "1.2.3"},
            OperatingSystem = {Name = "Android", Version = "999"},
            ["Custom"] = new Dictionary<string, object> {["A"] = "Foo", ["B"] = "Bar", ["C"] = "Baz"}
        };

        source.CopyTo(target);

        await VerifyResultAsync(target);
    }

    [Fact]
    public async Task CopyTo_ExistingOtherContexts()
    {
        var target = new Contexts
        {
            App = {Name = "Test", Version = "1.2.3"},
            ["Custom1"] = new Dictionary<string, object> {["A"] = "Foo1", ["B"] = "Bar1", ["C"] = "Baz1"}
        };

        var source = new Contexts
        {
            OperatingSystem = {Name = "Android", Version = "999"},
            ["Custom2"] = new Dictionary<string, object> {["A"] = "Foo2", ["B"] = "Bar2", ["C"] = "Baz2"}
        };

        source.CopyTo(target);

        await VerifyResultAsync(target);
    }

    [Fact]
    public async Task CopyTo_ExistingSameContexts()
    {
        var target = new Contexts
        {
            App = {Name = "Test", Version = "1.2.3"},
            ["Custom"] = new Dictionary<string, object> {["A"] = "Foo1", ["B"] = "Bar1"}
        };

        var source = new Contexts
        {
            App = {Name = "Something Else"},
            ["Custom"] = new Dictionary<string, object> {["A"] = "Foo2", ["B"] = "Bar2", ["C"] = "Baz2"}
        };

        source.CopyTo(target);

        await VerifyResultAsync(target);
    }

    [Fact]
    public async Task CopyTo_ExistingMixedContexts()
    {
        var target = new Contexts
        {
            App = {Name = "Test", Version = "1.2.3"},
            OperatingSystem = {Name = "Android", Version = "999"},
            ["Custom"] = new Dictionary<string, object> {["A"] = "Foo1", ["B"] = "Bar1"}
        };

        var source = new Contexts
        {
            App = {Name = "Something Else"},
            ["Custom"] = new Dictionary<string, object> {["A"] = "Foo2"}
        };

        source.CopyTo(target);

        await VerifyResultAsync(target);
    }

    private async Task VerifyResultAsync(IDictionary<string, object> dict)
    {
        var verifiable = dict.OrderBy(x => x.Key)
            .ToDictionary(x => x.Key, x => x.Value);
        await Verify(verifiable);
    }
}
