#nullable enable

namespace Sentry.Tests.Internals;

public class LazyLiteTests
{
    private int _valueFactoryInvoked = 0;

    [Fact]
    public void Default_HasNoFactory_ValueConsideredCreated()
    {
        LazyLite<string?> lazy = default;

        lazy.IsValueCreated.Should().BeTrue();
        _valueFactoryInvoked.Should().Be(0);

        lazy.Value.Should().BeNull();

        lazy.IsValueCreated.Should().BeTrue();
        _valueFactoryInvoked.Should().Be(0);
    }

    [Fact]
    public void Null_HasNoFactory_ValueConsideredCreated()
    {
        LazyLite<string?> lazy = new(null);

        lazy.IsValueCreated.Should().BeTrue();
        _valueFactoryInvoked.Should().Be(0);

        lazy.Value.Should().BeNull();

        lazy.IsValueCreated.Should().BeTrue();
        _valueFactoryInvoked.Should().Be(0);
    }

    [Fact]
    public void NullFactory_HasFactory_CreatesNullValue()
    {
        LazyLite<string?> lazy = new(NullFactory);

        lazy.IsValueCreated.Should().BeFalse();
        _valueFactoryInvoked.Should().Be(0);

        lazy.Value.Should().BeNull();

        lazy.IsValueCreated.Should().BeTrue();
        _valueFactoryInvoked.Should().Be(1);
    }

    [Fact]
    public void ValueFactory_HasFactory_CreatesValue()
    {
        LazyLite<string?> lazy = new(ValueFactory);

        lazy.IsValueCreated.Should().BeFalse();
        _valueFactoryInvoked.Should().Be(0);

        lazy.Value.Should().Be("Created");

        lazy.IsValueCreated.Should().BeTrue();
        _valueFactoryInvoked.Should().Be(1);
    }

    [Fact]
    public void ValueFactory_HasFactory_ReuseCachedValue()
    {
        LazyLite<string?> lazy = new(ValueFactory);

        lazy.Value.Should().Be("Created");

        lazy.IsValueCreated.Should().BeTrue();
        _valueFactoryInvoked.Should().Be(1);

        lazy.Value.Should().Be("Created");

        lazy.IsValueCreated.Should().BeTrue();
        _valueFactoryInvoked.Should().Be(1);
    }

    private string? NullFactory()
    {
        _valueFactoryInvoked++;
        return null;
    }

    private string ValueFactory()
    {
        _valueFactoryInvoked++;
        return "Created";
    }
}
