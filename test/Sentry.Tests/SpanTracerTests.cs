namespace Sentry.Tests;

public class SpanTracerTests
{
    [Fact]
    public async Task SetExtra_DataInserted_NoDataLoss()
    {
        // Run 20 times to avoid flaky tests scapping.
        for (var amount = 0; amount < 20; amount++)
        {
            // Arrange
            var hub = Substitute.For<IHub>();
            var transaction = new SpanTracer(hub, "test_name", "test_op");
            var evt = new ManualResetEvent(false);
            var ready = new ManualResetEvent(false);
            var counter = 0;
            // Act
            var tasks = Enumerable.Range(1, 4).Select(_ => Task.Run(() =>
            {
                var threadId = Interlocked.Increment(ref counter);

                if (threadId == 4)
                {
                    ready.Set();
                }
                evt.WaitOne();

                for (var i = 0; i < amount; i++)
                {
                    transaction.SetExtra(Guid.NewGuid().ToString(), Guid.NewGuid());
                }
            })).ToList();
            ready.WaitOne();
            evt.Set();
            await Task.WhenAll(tasks);

            // Arrange
            // 4 tasks testing X amount should be the same amount as Extras.
            Assert.Equal(4 * amount, transaction.Extra.Count);
        }
    }
}
