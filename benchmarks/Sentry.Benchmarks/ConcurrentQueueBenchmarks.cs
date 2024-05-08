using BenchmarkDotNet.Attributes;
using Sentry.Internal;
using Sentry.Protocol.Envelopes;

namespace Sentry.Benchmarks;

public class ConcurrentQueueBenchmarks
{
    [Params(1000)]
    public int N;

    [Benchmark]
    public async Task ConcurrentQueueLiteAsync()
    {
        ConcurrentQueueLite<Envelope> queue = new();
        List<Task> tasks = new();
        for (var i = 0; i < N; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                queue.Enqueue(Envelope.FromEvent(new SentryEvent()));
            }));
            tasks.Add(Task.Run(() =>
            {
                while (queue.TryPeek(out var item))
                {
                    queue.TryDequeue(out _);
                }
            }));
        }
        await Task.WhenAll(tasks);
        queue.Clear();
    }

    [Benchmark]
    public async Task ConcurrentQueueAsync()
    {
        ConcurrentQueue<Envelope> queue = new();
        List<Task> tasks = new();
        for (var i = 0; i < N; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                queue.Enqueue(Envelope.FromEvent(new SentryEvent()));
            }));
            tasks.Add(Task.Run(() =>
            {
                while (queue.TryPeek(out var item))
                {
                    queue.TryDequeue(out _);
                }
            }));
        }
        await Task.WhenAll(tasks);
        queue.Clear();
    }
}
