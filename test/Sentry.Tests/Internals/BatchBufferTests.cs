namespace Sentry.Tests.Internals;

public class BatchBufferTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void Ctor_CapacityIsOutOfRange_Throws(int capacity)
    {
        var ctor = () => new BatchBuffer<string>(capacity);

        Assert.Throws<ArgumentOutOfRangeException>("capacity", ctor);
    }

    [Fact]
    public void TryAdd_CapacityTwo_CanAddTwice()
    {
        var buffer = new BatchBuffer<string>(2);
        AssertEmpty(buffer, 2);

        buffer.TryAdd("one", out var first).Should().BeTrue();
        Assert.Equal(1, first);
        AssertPartial(buffer, 2);

        buffer.TryAdd("two", out var second).Should().BeTrue();
        Assert.Equal(2, second);
        AssertFull(buffer, 2);

        buffer.TryAdd("three", out var third).Should().BeFalse();
        Assert.Equal(3, third);
        AssertFull(buffer, 2);
    }

    [Fact]
    public void TryAdd_CapacityThree_CanAddThrice()
    {
        var buffer = new BatchBuffer<string>(3);
        AssertEmpty(buffer, 3);

        buffer.TryAdd("one", out var first).Should().BeTrue();
        Assert.Equal(1, first);
        AssertPartial(buffer, 3);

        buffer.TryAdd("two", out var second).Should().BeTrue();
        Assert.Equal(2, second);
        AssertPartial(buffer, 3);

        buffer.TryAdd("three", out var third).Should().BeTrue();
        Assert.Equal(3, third);
        AssertFull(buffer, 3);

        buffer.TryAdd("four", out var fourth).Should().BeFalse();
        Assert.Equal(4, fourth);
        AssertFull(buffer, 3);
    }

    [Fact]
    public void ToArrayAndClear_IsEmpty_EmptyArray()
    {
        var buffer = new BatchBuffer<string>(2);

        var array = buffer.ToArrayAndClear();

        Assert.Empty(array);
        AssertEmpty(buffer, 2);
    }

    [Fact]
    public void ToArrayAndClear_IsNotEmptyNorFull_PartialCopy()
    {
        var buffer = new BatchBuffer<string>(2);
        buffer.TryAdd("one", out _).Should().BeTrue();

        var array = buffer.ToArrayAndClear();

        Assert.Collection(array,
            item => Assert.Equal("one", item));
        AssertEmpty(buffer, 2);
    }

    [Fact]
    public void ToArrayAndClear_IsFull_FullCopy()
    {
        var buffer = new BatchBuffer<string>(2);
        buffer.TryAdd("one", out _).Should().BeTrue();
        buffer.TryAdd("two", out _).Should().BeTrue();

        var array = buffer.ToArrayAndClear();

        Assert.Collection(array,
            item => Assert.Equal("one", item),
            item => Assert.Equal("two", item));
        AssertEmpty(buffer, 2);
    }

    [Fact]
    public void ToArrayAndClear_CapacityExceeded_FullCopy()
    {
        var buffer = new BatchBuffer<string>(2);
        buffer.TryAdd("one", out _).Should().BeTrue();
        buffer.TryAdd("two", out _).Should().BeTrue();
        buffer.TryAdd("three", out _).Should().BeFalse();

        var array = buffer.ToArrayAndClear();

        Assert.Collection(array,
            item => Assert.Equal("one", item),
            item => Assert.Equal("two", item));
        AssertEmpty(buffer, 2);
    }

    [Fact]
    public void ToArrayAndClear_WithLength_PartialCopy()
    {
        var buffer = new BatchBuffer<string>(2);
        buffer.TryAdd("one", out _).Should().BeTrue();
        buffer.TryAdd("two", out _).Should().BeTrue();

        var array = buffer.ToArrayAndClear(1);

        Assert.Collection(array,
            item => Assert.Equal("one", item));
        AssertEmpty(buffer, 2);
    }

    private static void AssertEmpty<T>(BatchBuffer<T> buffer, int capacity)
    {
        AssertProperties(buffer, capacity, true, false);
    }

    private static void AssertPartial<T>(BatchBuffer<T> buffer, int capacity)
    {
        AssertProperties(buffer, capacity, false, false);
    }

    private static void AssertFull<T>(BatchBuffer<T> buffer, int capacity)
    {
        AssertProperties(buffer, capacity, false, true);
    }

    private static void AssertProperties<T>(BatchBuffer<T> buffer, int capacity, bool empty, bool full)
    {
        using (new AssertionScope())
        {
            buffer.Capacity.Should().Be(capacity);
            buffer.IsEmpty.Should().Be(empty);
            buffer.IsFull.Should().Be(full);
        }
    }
}
