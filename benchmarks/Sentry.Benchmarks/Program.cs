using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace Sentry.Benchmarks
{
    internal class Program
    {
        private static void Main(string[] args)
            => new BenchmarkSwitcher(typeof(Program).Assembly).Run(args, new Config());

        private class Config : ManualConfig
        {
            public Config()
            {
                Add(Job.Core);

                Add(MemoryDiagnoser.Default);

                Add(MarkdownExporter.GitHub);

                Add(DefaultConfig.Instance.GetLoggers().ToArray());
                Add(DefaultConfig.Instance.GetColumnProviders().ToArray());
            }
        }
    }
}
