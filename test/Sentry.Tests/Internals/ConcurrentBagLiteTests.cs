namespace Sentry.Tests.Internals;

public class ConcurrentBagLiteTests
{
    [Fact]
    public void Add_Test()
    {
        // Arrange
        var bag = new ConcurrentBagLite<int>();

        // Act
        bag.Add(1);

        // Assert
        bag.Count.Should().Be(1);

        // Act
        bag.Add(2);
        bag.Add(3);

        // Assert
        bag.Count.Should().Be(3);
        var items = bag.ToArray();
        items.Should().BeEquivalentTo([1, 2, 3]);
    }

    [Fact]
    public void Ctor_FromCollection_Test()
    {
        // Arrange & Act
        var bag = new ConcurrentBagLite<int>(new[] { 1, 2, 3 });

        // Assert
        bag.Count.Should().Be(3);
        bag.ToArray().Should().BeEquivalentTo([1, 2, 3]);
    }

    [Fact]
    public void Count_EmptyBag_Test()
    {
        // Arrange
        var bag = new ConcurrentBagLite<int>();

        // Act & Assert
        bag.Count.Should().Be(0);
    }

    [Fact]
    public void IsEmpty_Test()
    {
        // Arrange
        var bag = new ConcurrentBagLite<int>();

        // Act & Assert
        bag.IsEmpty.Should().BeTrue();
        bag.Add(1);
        bag.IsEmpty.Should().BeFalse();
        bag.Clear();
        bag.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Clear_Test()
    {
        // Arrange
        var bag = new ConcurrentBagLite<int>();
        bag.Add(1);
        bag.Add(2);
        bag.Add(3);

        // Act
        bag.Clear();

        // Assert
        bag.Count.Should().Be(0);
        bag.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void GetEnumerator_Test()
    {
        // Arrange
        var bag = new ConcurrentBagLite<int>();
        bag.Add(1);
        bag.Add(2);
        bag.Add(3);

        // Act
        var items = bag.ToList();

        // Assert
        items.Should().BeEquivalentTo([1, 2, 3]);
    }

    [Fact]
    public void GetEnumerator_Snapshot_DoesNotThrowOnConcurrentModification()
    {
        // Arrange
        var bag = new ConcurrentBagLite<int>();
        bag.Add(1);
        bag.Add(2);

        // Act
        using var enumerator = bag.GetEnumerator();
        bag.Add(3); // modify after enumerator created — should not throw when iterating snapshot

        var items = new List<int>();
        while (enumerator.MoveNext())
        {
            items.Add(enumerator.Current);
        }

        // Assert
        items.Should().BeEquivalentTo([1, 2]);
    }

    [Fact]
    public async Task TestConcurrency()
    {
        // Arrange
        var bag = new ConcurrentBagLite<int>();
        var count = 100;
        var tasks = new Task[count];

        // Act
        for (var i = 0; i < count; i++)
        {
            var toAdd = i;
            tasks[i] = Task.Run(() => bag.Add(toAdd));
        }
        await Task.WhenAll(tasks);

        // Assert
        bag.Count.Should().Be(count);
        bag.ToArray().Should().BeEquivalentTo(Enumerable.Range(0, count));
    }
}
