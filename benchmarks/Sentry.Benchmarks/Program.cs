using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace Sentry.Benchmarks;

internal class Program
{
    private static void Main(string[] args)
        => new BenchmarkSwitcher(typeof(Program).Assembly).Run(args, new Config());

    private class Config : ManualConfig
    {
        public Config()
        {
            AddDiagnoser(MemoryDiagnoser.Default);
            AddExporter(MarkdownExporter.GitHub);
            AddLogger(DefaultConfig.Instance.GetLoggers().ToArray());
            AddColumnProvider(DefaultConfig.Instance.GetColumnProviders().ToArray());
        }
    }
}
