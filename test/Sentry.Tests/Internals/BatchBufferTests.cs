namespace Sentry.Tests.Internals;

public class BatchBufferTests
{
    [Fact]
    public void Ctor_CapacityIsNegative_Throws()
    {
        var ctor = () => new BatchBuffer<string>(-1);

        Assert.Throws<ArgumentOutOfRangeException>("capacity", ctor);
    }

    [Fact]
    public void Ctor_CapacityIsZero_Throws()
    {
        var ctor = () => new BatchBuffer<string>(0);

        Assert.Throws<ArgumentOutOfRangeException>("capacity", ctor);
    }

    [Fact]
    public void TryAdd_CapacityOne_CanAddOnce()
    {
        var buffer = new BatchBuffer<string>(1);
        AssertProperties(buffer, 0, 1, true, false);

        buffer.TryAdd("one").Should().BeTrue();
        AssertProperties(buffer, 1, 1, false, true);

        buffer.TryAdd("two").Should().BeFalse();
        AssertProperties(buffer, 1, 1, false, true);
    }

    [Fact]
    public void TryAdd_CapacityTwo_CanAddTwice()
    {
        var buffer = new BatchBuffer<string>(2);
        AssertProperties(buffer, 0, 2, true, false);

        buffer.TryAdd("one").Should().BeTrue();
        AssertProperties(buffer, 1, 2, false, false);

        buffer.TryAdd("two").Should().BeTrue();
        AssertProperties(buffer, 2, 2, false, true);

        buffer.TryAdd("three").Should().BeFalse();
        AssertProperties(buffer, 2, 2, false, true);
    }

    [Fact]
    public void ToArray_IsEmpty_EmptyArray()
    {
        var buffer = new BatchBuffer<string>(3);

        var array = buffer.ToArray();

        Assert.Empty(array);
        AssertProperties(buffer, 0, 3, true, false);
    }

    [Fact]
    public void ToArray_IsNotEmptyNorFull_PartialArray()
    {
        var buffer = new BatchBuffer<string>(3);
        buffer.TryAdd("one").Should().BeTrue();
        buffer.TryAdd("two").Should().BeTrue();

        var array = buffer.ToArray();

        Assert.Collection(array,
            item => Assert.Equal("one", item),
            item => Assert.Equal("two", item));
        AssertProperties(buffer, 2, 3, false, false);
    }

    [Fact]
    public void ToArray_IsFull_FullArray()
    {
        var buffer = new BatchBuffer<string>(3);
        buffer.TryAdd("one").Should().BeTrue();
        buffer.TryAdd("two").Should().BeTrue();
        buffer.TryAdd("three").Should().BeTrue();

        var array = buffer.ToArray();

        Assert.Collection(array,
            item => Assert.Equal("one", item),
            item => Assert.Equal("two", item),
            item => Assert.Equal("three", item));
        AssertProperties(buffer, 3, 3, false, true);
    }

    [Fact]
    public void Clear_IsEmpty_NoOp()
    {
        var buffer = new BatchBuffer<string>(2);

        AssertProperties(buffer, 0, 2, true, false);
        buffer.Clear();
        AssertProperties(buffer, 0, 2, true, false);
    }

    [Fact]
    public void Clear_IsNotEmptyNorFull_ClearArray()
    {
        var buffer = new BatchBuffer<string>(2);
        buffer.TryAdd("one").Should().BeTrue();

        AssertProperties(buffer, 1, 2, false, false);
        buffer.Clear();
        AssertProperties(buffer, 0, 2, true, false);
    }

    [Fact]
    public void Clear_IsFull_ClearArray()
    {
        var buffer = new BatchBuffer<string>(2);
        buffer.TryAdd("one").Should().BeTrue();
        buffer.TryAdd("two").Should().BeTrue();

        AssertProperties(buffer, 2, 2, false, true);
        buffer.Clear();
        AssertProperties(buffer, 0, 2, true, false);
    }

    [Fact]
    public void ToArrayAndClear_IsEmpty_EmptyArray()
    {
        var buffer = new BatchBuffer<string>(2);

        AssertProperties(buffer, 0, 2, true, false);
        var array = buffer.ToArrayAndClear();
        AssertProperties(buffer, 0, 2, true, false);
        Assert.Empty(array);
    }

    [Fact]
    public void ToArrayAndClear_IsNotEmptyNorFull_PartialArray()
    {
        var buffer = new BatchBuffer<string>(2);
        buffer.TryAdd("one").Should().BeTrue();

        AssertProperties(buffer, 1, 2, false, false);
        var array = buffer.ToArrayAndClear();
        AssertProperties(buffer, 0, 2, true, false);
        Assert.Collection(array,
            item => Assert.Equal("one", item));
    }

    [Fact]
    public void ToArrayAndClear_IsFull_FullArray()
    {
        var buffer = new BatchBuffer<string>(2);
        buffer.TryAdd("one").Should().BeTrue();
        buffer.TryAdd("two").Should().BeTrue();

        AssertProperties(buffer, 2, 2, false, true);
        var array = buffer.ToArrayAndClear();
        AssertProperties(buffer, 0, 2, true, false);
        Assert.Collection(array,
            item => Assert.Equal("one", item),
            item => Assert.Equal("two", item));
    }

    private static void AssertProperties<T>(BatchBuffer<T> buffer, int count, int capacity, bool empty, bool full)
    {
        using (new AssertionScope())
        {
            buffer.Count.Should().Be(count);
            buffer.Capacity.Should().Be(capacity);
            buffer.IsEmpty.Should().Be(empty);
            buffer.IsFull.Should().Be(full);
        }
    }
}
