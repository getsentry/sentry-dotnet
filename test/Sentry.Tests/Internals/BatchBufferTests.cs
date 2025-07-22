#nullable enable

namespace Sentry.Tests.Internals;

public class BatchBufferTests
{
    private sealed class Fixture
    {
        public int Capacity { get; set; } = 2;
        public TimeSpan Timeout { get; set; } = System.Threading.Timeout.InfiniteTimeSpan;
        public MockClock Clock { get; } = new();
        public List<(BatchBuffer<string> Buffer, DateTimeOffset SignalTime)> TimeoutExceededInvocations { get; } = new();
        public string? Name { get; set; }

        public BatchBuffer<string> GetSut()
        {
            return new BatchBuffer<string>(Capacity, Timeout, Clock, OnTimeoutExceeded, Name);
        }

        private void OnTimeoutExceeded(BatchBuffer<string> buffer, DateTimeOffset signalTime)
        {
            TimeoutExceededInvocations.Add((buffer, signalTime));
        }
    }

    private readonly Fixture _fixture = new();

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void Ctor_CapacityIsOutOfRange_Throws(int capacity)
    {
        _fixture.Capacity = capacity;

        var ctor = () => _fixture.GetSut();

        Assert.Throws<ArgumentOutOfRangeException>("capacity", ctor);
    }

    [Theory]
    [InlineData(-2)]
    [InlineData(0)]
    public void Ctor_TimeoutIsOutOfRange_Throws(int millisecondsTimeout)
    {
        _fixture.Timeout = TimeSpan.FromMilliseconds(millisecondsTimeout);

        var ctor = () => _fixture.GetSut();

        Assert.Throws<ArgumentOutOfRangeException>("timeout", ctor);
    }

    [Fact]
    public void Add_CapacityTwo_CanAddTwice()
    {
        _fixture.Capacity = 2;
        using var buffer = _fixture.GetSut();

        buffer.Capacity.Should().Be(2);
        buffer.IsEmpty.Should().BeTrue();

        buffer.Add("one").Should().Be(BatchBufferAddStatus.AddedFirst);
        buffer.IsEmpty.Should().BeFalse();

        buffer.Add("two").Should().Be(BatchBufferAddStatus.AddedLast);
        buffer.IsEmpty.Should().BeFalse();

        buffer.Add("three").Should().Be(BatchBufferAddStatus.IgnoredCapacityExceeded);
        buffer.IsEmpty.Should().BeFalse();

        buffer.Add("four").Should().Be(BatchBufferAddStatus.IgnoredCapacityExceeded);
        buffer.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void Add_CapacityThree_CanAddThrice()
    {
        _fixture.Capacity = 3;
        using var buffer = _fixture.GetSut();

        buffer.Capacity.Should().Be(3);
        buffer.IsEmpty.Should().BeTrue();

        buffer.Add("one").Should().Be(BatchBufferAddStatus.AddedFirst);
        buffer.IsEmpty.Should().BeFalse();

        buffer.Add("two").Should().Be(BatchBufferAddStatus.Added);
        buffer.IsEmpty.Should().BeFalse();

        buffer.Add("three").Should().Be(BatchBufferAddStatus.AddedLast);
        buffer.IsEmpty.Should().BeFalse();

        buffer.Add("four").Should().Be(BatchBufferAddStatus.IgnoredCapacityExceeded);
        buffer.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void Flush_IsEmpty_EmptyArray()
    {
        _fixture.Capacity = 2;
        using var buffer = _fixture.GetSut();

        using var flushScope = buffer.TryEnterFlushScope();
        var array = flushScope.Flush();

        array.Should().BeEmpty();
        buffer.Capacity.Should().Be(2);
        buffer.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Flush_IsNotEmptyNorFull_PartialCopy()
    {
        _fixture.Capacity = 2;
        using var buffer = _fixture.GetSut();

        buffer.Add("one");
        using var flushScope = buffer.TryEnterFlushScope();
        var array = flushScope.Flush();

        array.Should().Equal(["one"]);
        buffer.Capacity.Should().Be(2);
        buffer.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Flush_IsFull_FullCopy()
    {
        _fixture.Capacity = 2;
        using var buffer = _fixture.GetSut();

        buffer.Add("one");
        buffer.Add("two");
        using var flushScope = buffer.TryEnterFlushScope();
        var array = flushScope.Flush();

        array.Should().Equal(["one", "two"]);
        buffer.Capacity.Should().Be(2);
        buffer.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Flush_CapacityExceeded_FullCopy()
    {
        _fixture.Capacity = 2;
        using var buffer = _fixture.GetSut();

        buffer.Add("one");
        buffer.Add("two");
        buffer.Add("three");
        using var flushScope = buffer.TryEnterFlushScope();
        var array = flushScope.Flush();

        array.Should().Equal(["one", "two"]);
        buffer.Capacity.Should().Be(2);
        buffer.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Flush_DoubleFlush_SecondArrayIsEmpty()
    {
        _fixture.Capacity = 2;
        using var buffer = _fixture.GetSut();

        buffer.Add("one");
        buffer.Add("two");
        using var flushScope = buffer.TryEnterFlushScope();
        var first = flushScope.Flush();
        var second = flushScope.Flush();

        first.Should().Equal(["one", "two"]);
        second.Should().BeEmpty();
    }

    [Fact]
    public void Flush_SecondFlush_NoFlushNoClear()
    {
        _fixture.Capacity = 2;
        using var buffer = _fixture.GetSut();

        buffer.Add("one");
        buffer.Add("two");

        using (var flushScope = buffer.TryEnterFlushScope())
        {
            flushScope.IsEntered.Should().BeTrue();
            buffer.IsEmpty.Should().BeFalse();
        }

        using (var flushScope = buffer.TryEnterFlushScope())
        {
            flushScope.IsEntered.Should().BeTrue();
            flushScope.Flush().Should().Equal(["one", "two"]);
            buffer.IsEmpty.Should().BeTrue();
        }
    }

    [Fact]
    public void Flush_TryEnterFlushScopeTwice_CanOnlyEnterOnce()
    {
        _fixture.Capacity = 2;
        using var buffer = _fixture.GetSut();

        buffer.Add("one");
        buffer.Add("two");
        using var first = buffer.TryEnterFlushScope();
        using var second = buffer.TryEnterFlushScope();

        first.IsEntered.Should().BeTrue();
        second.IsEntered.Should().BeFalse();

        first.Flush().Should().Equal(["one", "two"]);
        AssertFlushThrows<ObjectDisposedException>(second);
    }

    [Fact]
    public void Flush_Disposed_Throws()
    {
        _fixture.Capacity = 2;
        using var buffer = _fixture.GetSut();

        buffer.Add("one");
        buffer.Add("two");
        var flushScope = buffer.TryEnterFlushScope();
        flushScope.Dispose();

        AssertFlushThrows<ObjectDisposedException>(flushScope);
    }

    // cannot use xUnit's Throws() nor Fluent Assertions' ThrowExactly() because the FlushScope is a ref struct
    private static void AssertFlushThrows<T>(BatchBuffer<string>.FlushScope flushScope)
        where T : Exception
    {
        Exception? exception = null;
        try
        {
            flushScope.Flush();
        }
        catch (Exception e)
        {
            exception = e;
        }

        exception.Should().NotBeNull();
        exception.Should().BeOfType<T>();
    }
}
