namespace Sentry.Tests.Polyfilling;

public class ImmutableCollectionsPolyfillTests
{
    [Fact]
    public void ImmutableArrayBuilder_DrainToImmutable_CountIsNotCapacity()
    {
        var builder = ImmutableArray.CreateBuilder<string>(2);
        builder.Add("one");

        builder.Count.Should().Be(1);
        builder.Capacity.Should().Be(2);

        var array = builder.DrainToImmutable();
        array.Length.Should().Be(1);
        array.Should().BeEquivalentTo(["one"]);

        builder.Count.Should().Be(0);
        builder.Capacity.Should().Be(0);
    }

    [Fact]
    public void ImmutableArrayBuilder_DrainToImmutable_CountIsCapacity()
    {
        var builder = ImmutableArray.CreateBuilder<string>(2);
        builder.Add("one");
        builder.Add("two");

        builder.Count.Should().Be(2);
        builder.Capacity.Should().Be(2);

        var array = builder.DrainToImmutable();
        array.Length.Should().Be(2);
        array.Should().BeEquivalentTo(["one", "two"]);

        builder.Count.Should().Be(0);
        builder.Capacity.Should().Be(0);
    }
}
