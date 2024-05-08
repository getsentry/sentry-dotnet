namespace Sentry.Tests.Internals;

public class ConcurrentQueueLiteTests
{
    [Fact]
    public void Enqueue_Test(){
        // Arrange
        var queue = new ConcurrentQueueLite<int>();

        // Act
        queue.Enqueue(1);

        // Assert
        queue.Count.Should().Be(1);
    }

    [Fact]
    public void TryDequeue_Test(){
        // Arrange
        var queue = new ConcurrentQueueLite<int>();
        queue.Enqueue(1);

        // Act
        var result = queue.TryDequeue(out var dequeuedItem);

        // Assert
        result.Should().BeTrue();
        dequeuedItem.Should().Be(1);
        queue.Count.Should().Be(0);
    }

    [Fact]
    public void TryDequeue_EmptyQueue_Test(){
        // Arrange
        var queue = new ConcurrentQueueLite<int>();

        // Act
        var result = queue.TryDequeue(out var dequeuedItem);

        // Assert
        result.Should().BeFalse();
        dequeuedItem.Should().Be(default(int));
    }

    [Fact]
    public void Count_EmptyQueue_Test(){
        // Arrange
        var queue = new ConcurrentQueueLite<int>();

        // Act & Assert
        queue.Count.Should().Be(0);
    }

    [Fact]
    public void IsEmpty_Test(){
        // Arrange
        var queue = new ConcurrentQueueLite<int>();

        // Act & Assert
        queue.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Clear_Test(){
        // Arrange
        var queue = new ConcurrentQueueLite<int>();
        queue.Enqueue(1);

        // Act
        queue.Clear();

        // Assert
        queue.Count.Should().Be(0);
    }

    [Fact]
    public void TryPeek_Test(){
        // Arrange
        var queue = new ConcurrentQueueLite<int>();
        queue.Enqueue(1);

        // Act
        var result = queue.TryPeek(out var peekedItem);

        // Assert
        result.Should().BeTrue();
        peekedItem.Should().Be(1);
        queue.Count.Should().Be(1);
    }

    [Fact]
    public void TryPeek_EmptyQueue_Test(){
        // Arrange
        var queue = new ConcurrentQueueLite<int>();

        // Act
        var result = queue.TryPeek(out var peekedItem);

        // Assert
        result.Should().BeFalse();
        peekedItem.Should().Be(default(int));
    }

    [Fact]
    public async Task TestConcurrency(){
        // Arrange
        var queue = new ConcurrentQueueLite<int>();
        var count = 100;
        var tasks = new Task[count * 2];
        var received = 0;

        // Act
        for (var i = 0; i < count; i++)
        {
            var toAdd = i;
            tasks[i] = Task.Run(() =>
            {
                queue.Enqueue(toAdd);
                Interlocked.Increment(ref received);
            });
            tasks[i + count] = Task.Run(() => {
                queue.TryDequeue(out _);
                Interlocked.Increment(ref received);
            });
        }
        await Task.WhenAll(tasks);

        // Assert
        received.Should().Be(count * 2);
    }
}
