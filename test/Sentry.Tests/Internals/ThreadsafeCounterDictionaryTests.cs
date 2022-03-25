namespace Sentry.Tests.Internals;

public class ThreadsafeCounterDictionaryTests
{
    [Fact]
    public void UnusedCounterIndexerReturnsZero()
    {
        var counters = new ThreadsafeCounterDictionary<string>();
        Assert.Equal(0, counters["foo"]);
    }

    [Fact]
    public void UnusedCounterTryGetValueReturnsTrueAndOutputsZero()
    {
        var counters = new ThreadsafeCounterDictionary<string>();
        Assert.True(counters.TryGetValue("foo", out var value));
        Assert.Equal(0, value);
    }

    [Fact]
    public void CounterIndexerReturnsValue()
    {
        var counters = new ThreadsafeCounterDictionary<string>();
        counters.Increment("foo");
        Assert.Equal(1, counters["foo"]);
    }

    [Fact]
    public void CounterTryGetValueReturnsTrueAndOutputValue()
    {
        var counters = new ThreadsafeCounterDictionary<string>();
        counters.Increment("foo");
        Assert.True(counters.TryGetValue("foo", out var value));
        Assert.Equal(1, value);
    }

    [Fact(Timeout = 1200)]
    public async Task CanIncrementManyCountersSimultaneously()
    {
        // This test should pass in roughly 700ms - 800ms on most systems.
        // If the implementation used a hard lock while incrementing, it would take about 1500ms - 2000ms.
        // If the implementation did a naive increment (_value++), it would not be threadsafe and this test would fail.

        const int numCounters = 10;
        const int numTasksPerCounter = 100;
        const int numIterationsPerTask = 10000;

        var counters = new ThreadsafeCounterDictionary<string>();

        var tasks = new List<Task>(capacity: numCounters * numTasksPerCounter);
        for (var i = 0; i < numCounters; i++)
        {
            var counterName = $"counter_{i}";
            for (var j = 0; j < numTasksPerCounter; j++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    await Task.Yield();
                    for (var k = 0; k < numIterationsPerTask; k++)
                    {
                        counters.Increment(counterName);
                    }
                }));
            }
        }

        await Task.WhenAll(tasks);

        const int expectedCount = numTasksPerCounter * numIterationsPerTask;
        for (var i = 0; i < numCounters; i++)
        {
            Assert.Equal(expectedCount, counters[$"counter_{i}"]);
        }
    }

    [Fact]
    public void CanReadOneCounterWithoutResetting()
    {
        var counters = new ThreadsafeCounterDictionary<string>();
        counters.Increment("foo");

        var actual1 = counters["foo"];
        var actual2 = counters["foo"];

        Assert.Equal(1, actual1);
        Assert.Equal(1, actual2);
    }

    [Fact]
    public void CanReadAndResetOneCounter()
    {
        var counters = new ThreadsafeCounterDictionary<string>();
        counters.Increment("foo");

        var actual1 = counters.ReadAndReset("foo");
        var actual2 = counters["foo"];

        Assert.Equal(1, actual1);
        Assert.Equal(0, actual2);
    }

    [Fact]
    public void CanReadAllCountersWithoutResetting()
    {
        var counters = new ThreadsafeCounterDictionary<string>();
        counters.Increment("foo");
        counters.Increment("bar");
        counters.Increment("bar");

        var actual1 = counters.ToDictionary();
        var actual2 = counters.ToDictionary();

        var expected = new Dictionary<string, int> {{"foo", 1}, {"bar", 2}};
        Assert.Equal(expected, actual1);
        Assert.Equal(expected, actual2);
    }

    [Fact]
    public void CanReadAndResetAllCounters()
    {
        var counters = new ThreadsafeCounterDictionary<string>();
        counters.Increment("foo");
        counters.Increment("bar");
        counters.Increment("bar");

        var actual1 = counters.ReadAllAndReset();
        var actual2 = counters.ToDictionary();

        var expected1 = new Dictionary<string, int> {{"foo", 1}, {"bar", 2}};
        var expected2 = new Dictionary<string, int> {{"foo", 0}, {"bar", 0}};

        Assert.Equal(expected1, actual1);
        Assert.Equal(expected2, actual2);
    }

    [Fact]
    public void CanCountDistinctCounters()
    {
        var counters = new ThreadsafeCounterDictionary<string>();
        counters.Increment("foo");
        counters.Increment("foo");
        counters.Increment("bar");
        counters.Increment("bar");
        counters.Increment("baz");

        Assert.Equal(3, counters.Count);
    }

    [Fact]
    public void CanGetCounterKeys()
    {
        var counters = new ThreadsafeCounterDictionary<string>();
        counters.Increment("a");
        counters.Increment("a");
        counters.Increment("b");
        counters.Increment("b");
        counters.Increment("c");

        var actual = counters.Keys.OrderBy(x => x);

        Assert.Equal(new[] {"a", "b", "c"}, actual);
    }

    [Fact]
    public void CanGetCounterValues()
    {
        var counters = new ThreadsafeCounterDictionary<string>();
        counters.Increment("a");
        counters.Increment("b");
        counters.Increment("b");
        counters.Increment("c");
        counters.Increment("c");
        counters.Increment("c");

        var actual = counters.Values.OrderBy(x => x);

        Assert.Equal(new[] {1, 2, 3}, actual);
    }

}
