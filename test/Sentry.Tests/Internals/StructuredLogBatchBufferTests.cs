#nullable enable

namespace Sentry.Tests.Internals;

public class StructuredLogBatchBufferTests
{
    private sealed class Fixture
    {
        public int Capacity { get; set; } = 2;
        public TimeSpan Timeout { get; set; } = System.Threading.Timeout.InfiniteTimeSpan;
        public string? Name { get; set; }

        public List<StructuredLogBatchBuffer> TimeoutExceededInvocations { get; } = [];

        public StructuredLogBatchBuffer GetSut()
        {
            return new StructuredLogBatchBuffer(Capacity, Timeout, OnTimeoutExceeded, Name);
        }

        private void OnTimeoutExceeded(StructuredLogBatchBuffer buffer)
        {
            TimeoutExceededInvocations.Add(buffer);
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
    public void Ctor()
    {
        _fixture.Capacity = 9;
        _fixture.Name = nameof(Ctor);

        using var buffer = _fixture.GetSut();

        buffer.Capacity.Should().Be(_fixture.Capacity);
        buffer.IsEmpty.Should().BeTrue();
        buffer.Name.Should().Be(_fixture.Name);
    }

    [Fact]
    public void Add_CapacityTwo_CanAddTwice()
    {
        _fixture.Capacity = 2;
        using var buffer = _fixture.GetSut();

        buffer.Capacity.Should().Be(2);
        buffer.IsEmpty.Should().BeTrue();

        buffer.Add("one").Should().Be(StructuredLogBatchBufferAddStatus.AddedFirst);
        buffer.IsEmpty.Should().BeFalse();

        buffer.Add("two").Should().Be(StructuredLogBatchBufferAddStatus.AddedLast);
        buffer.IsEmpty.Should().BeFalse();

        buffer.Add("three").Should().Be(StructuredLogBatchBufferAddStatus.IgnoredCapacityExceeded);
        buffer.IsEmpty.Should().BeFalse();

        buffer.Add("four").Should().Be(StructuredLogBatchBufferAddStatus.IgnoredCapacityExceeded);
        buffer.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void Add_CapacityThree_CanAddThrice()
    {
        _fixture.Capacity = 3;
        using var buffer = _fixture.GetSut();

        buffer.Capacity.Should().Be(3);
        buffer.IsEmpty.Should().BeTrue();

        buffer.Add("one").Should().Be(StructuredLogBatchBufferAddStatus.AddedFirst);
        buffer.IsEmpty.Should().BeFalse();

        buffer.Add("two").Should().Be(StructuredLogBatchBufferAddStatus.Added);
        buffer.IsEmpty.Should().BeFalse();

        buffer.Add("three").Should().Be(StructuredLogBatchBufferAddStatus.AddedLast);
        buffer.IsEmpty.Should().BeFalse();

        buffer.Add("four").Should().Be(StructuredLogBatchBufferAddStatus.IgnoredCapacityExceeded);
        buffer.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void Add_Flushing_CannotAdd()
    {
        _fixture.Capacity = 2;
        var buffer = _fixture.GetSut();

        var flushScope = buffer.TryEnterFlushScope();

        buffer.Add("one").Should().Be(StructuredLogBatchBufferAddStatus.IgnoredIsFlushing);
        buffer.IsEmpty.Should().BeTrue();

        flushScope.Dispose();

        buffer.Add("two").Should().Be(StructuredLogBatchBufferAddStatus.AddedFirst);
        buffer.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void Add_Disposed_CannotAdd()
    {
        _fixture.Capacity = 2;
        var buffer = _fixture.GetSut();

        buffer.Dispose();

        buffer.Add("one").Should().Be(StructuredLogBatchBufferAddStatus.IgnoredIsDisposed);
        buffer.IsEmpty.Should().BeTrue();
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

        array.Messages().Should().Equal(["one"]);
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

        array.Messages().Should().Equal(["one", "two"]);
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

        array.Messages().Should().Equal(["one", "two"]);
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

        first.Messages().Should().Equal(["one", "two"]);
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
            flushScope.Flush().Messages().Should().Equal(["one", "two"]);
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

        first.Flush().Messages().Should().Equal(["one", "two"]);
        AssertFlushThrows<ObjectDisposedException>(second);
    }

    [Fact]
    public void Flush_DisposedScope_Throws()
    {
        _fixture.Capacity = 2;
        using var buffer = _fixture.GetSut();

        buffer.Add("one");
        buffer.Add("two");
        var flushScope = buffer.TryEnterFlushScope();
        flushScope.Dispose();

        AssertFlushThrows<ObjectDisposedException>(flushScope);
    }

    [Fact]
    public void Flush_DisposedBuffer_CannotEnter()
    {
        _fixture.Capacity = 2;
        var buffer = _fixture.GetSut();

        buffer.Dispose();
        using var flushScope = buffer.TryEnterFlushScope();

        flushScope.IsEntered.Should().BeFalse();
        AssertFlushThrows<ObjectDisposedException>(flushScope);
    }

    [Fact]
    public void OnIntervalElapsed_Timeout_InvokesCallback()
    {
        _fixture.Timeout = Timeout.InfiniteTimeSpan;
        using var buffer = _fixture.GetSut();

        buffer.OnIntervalElapsed(null);
        _fixture.TimeoutExceededInvocations.Should().HaveCount(1);

        buffer.OnIntervalElapsed(null);
        _fixture.TimeoutExceededInvocations.Should().HaveCount(2);

        _fixture.TimeoutExceededInvocations[0].Should().BeSameAs(buffer);
        _fixture.TimeoutExceededInvocations[1].Should().BeSameAs(buffer);
    }

    [Fact]
    public void OnIntervalElapsed_Disposed_DoesNotInvokeCallback()
    {
        _fixture.Timeout = Timeout.InfiniteTimeSpan;
        var buffer = _fixture.GetSut();

        buffer.Dispose();
        buffer.OnIntervalElapsed(null);

        _fixture.TimeoutExceededInvocations.Should().BeEmpty();
    }

    // cannot use xUnit's Throws() nor Fluent Assertions' ThrowExactly() because the FlushScope is a ref struct
    private static void AssertFlushThrows<T>(StructuredLogBatchBuffer.FlushScope flushScope)
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

file static class StructuredLogBatchBufferHelpers
{
    public static StructuredLogBatchBufferAddStatus Add(this StructuredLogBatchBuffer buffer, string item)
    {
        SentryLog log = new(DateTimeOffset.MinValue, SentryId.Empty, SentryLogLevel.Trace, item);
        return buffer.Add(log);
    }

    public static string[] Messages(this SentryLog[] logs)
    {
        return logs.Select(static log => log.Message).ToArray();
    }
}
