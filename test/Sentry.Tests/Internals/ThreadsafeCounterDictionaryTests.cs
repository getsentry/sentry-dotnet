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

    [Fact]
    public void CanIncrementManyCountersSimultaneously()
    {
        const int numCounters = 10;
        const int numThreadsPerCounter = 20;
        const int numIterationsPerThread = 10000;

        var counters = new ThreadsafeCounterDictionary<string>();

        Parallel.For(0, numCounters * numThreadsPerCounter, x =>
        {
            var i = x % numCounters;
            var counterName = $"counter_{i}";

            for (var j = 0; j < numIterationsPerThread; j++)
            {
                counters.Increment(counterName);
            }
        });

        const int expectedCount = numThreadsPerCounter * numIterationsPerThread;
        for (var i = 0; i < numCounters; i++)
        {
            Assert.Equal(expectedCount, counters[$"counter_{i}"]);
        }
    }

    [Fact]
    public void CanAddManyCountersSimultaneously()
    {
        const int numCounters = 10;
        const int numThreadsPerCounter = 20;
        const int numIterationsPerThread = 10000;
        const int multiplier = 3;

        var counters = new ThreadsafeCounterDictionary<string>();

        Parallel.For(0, numCounters * numThreadsPerCounter, x =>
        {
            var i = x % numCounters;
            var counterName = $"counter_{i}";

            for (var j = 0; j < numIterationsPerThread; j++)
            {
                counters.Add(counterName, multiplier);
            }
        });

        const int expectedCount = numThreadsPerCounter * numIterationsPerThread * multiplier;
        for (var i = 0; i < numCounters; i++)
        {
            Assert.Equal(expectedCount, counters[$"counter_{i}"]);
        }
    }

    [Fact]
    public void CanAddValue()
    {
        var counters = new ThreadsafeCounterDictionary<string>
        {
            {"foo", 2}
        };
        Assert.Equal(2, counters["foo"]);
    }

    [Fact]
    public void CanAddToExistingCount()
    {
        var counters = new ThreadsafeCounterDictionary<string>();
        counters.Increment("foo");
        counters.Add("foo", 2);
        Assert.Equal(3, counters["foo"]);
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

        var actual1 = counters.ToDict();
        var actual2 = counters.ToDict();

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
        var actual2 = counters.ToDict();

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

    [Fact]
    public void CanIncrementUsingDiscardReasonWithCategory()
    {
        var a = DiscardReason.QueueOverflow.WithCategory(DataCategory.Error);
        var b = DiscardReason.QueueOverflow.WithCategory(DataCategory.Error);
        var c = DiscardReason.QueueOverflow.WithCategory(DataCategory.Attachment);

        var counters = new ThreadsafeCounterDictionary<DiscardReasonWithCategory>();

        // these should increment the same counter, despite a and b being separate instances
        counters.Increment(a);
        counters.Increment(b);

        // this should increment a different counter entirely
        counters.Increment(c);

        Assert.Equal(2, counters[a]);
        Assert.Equal(2, counters[b]);
        Assert.Equal(1, counters[c]);
    }

    [Fact]
    public void CanReadAndResetManyCountersSimultaneously()
    {
        const int numCounters = 10;
        const int numThreadsPerCounter = 20;
        const int numIterationsPerThread = 10000;

        var counters = new ThreadsafeCounterDictionary<string>();

        var grandTotal = 0;
        void ReadAndAddToGrandTotal()
        {
            var partialTotal = counters.ReadAllAndReset().Values.Sum();
            Interlocked.Add(ref grandTotal, partialTotal);
        }

        var random = new Random();

        Parallel.For(0, numCounters * numThreadsPerCounter, x =>
        {
            var i = x % numCounters;
            var counterName = $"counter_{i}";

            var numIterationsToReadAfter = random.Next(100, 1000);

            for (var j = 0; j < numIterationsPerThread; j++)
            {
                counters.Increment(counterName);

                if (j % numIterationsToReadAfter == 0)
                {
                    ReadAndAddToGrandTotal();
                }
            }

            // include any final counts
            ReadAndAddToGrandTotal();
        });

        const int expectedGrandTotal = numCounters * numThreadsPerCounter * numIterationsPerThread;
        Assert.Equal(expectedGrandTotal, grandTotal);
    }
}
