#nullable enable

using BenchmarkDotNet.Attributes;
using Sentry.Internal;

namespace Sentry.Benchmarks.Internal;

public class LazyBenchmarks
{
    private static string Text = null!;

    [GlobalSetup]
    public void Setup()
    {
        Text = "Factory";
    }

    [Benchmark]
    public (char, char) System_Lazy()
    {
        var lazy = new Lazy<char>(ValueFactory, LazyThreadSafetyMode.None);

        var first = lazy.Value;
        var second = lazy.Value;

        return (first, second);
    }

    [Benchmark]
    public (char, char) Sentry_Internal_LazyLite()
    {
        var lazy = new LazyLite<char>(ValueFactory);

        var first = lazy.Value;
        var second = lazy.Value;

        return (first, second);
    }

    private static char ValueFactory()
    {
        return Text[^1];
    }
}
