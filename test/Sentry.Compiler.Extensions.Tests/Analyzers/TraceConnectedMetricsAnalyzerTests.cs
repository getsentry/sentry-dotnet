using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Sentry.Compiler.Extensions.Analyzers;

using Verifier = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Sentry.Compiler.Extensions.Analyzers.TraceConnectedMetricsAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Sentry.Compiler.Extensions.Tests.Analyzers;

public class TraceConnectedMetricsAnalyzerTests
{
    [Fact]
    public async Task NoCode_NoDiagnostics()
    {
        await Verifier.VerifyAnalyzerAsync("");
    }

    [Fact]
    public async Task NoInvocations_NoDiagnostics()
    {
        var test = new CSharpAnalyzerTest<TraceConnectedMetricsAnalyzer, DefaultVerifier>
        {
            TestState =
            {
                ReferenceAssemblies = TargetFramework.ReferenceAssemblies,
                AdditionalReferences = { typeof(SentryTraceMetrics).Assembly },
                Sources =
                {
                    """
                    #nullable enable
                    using Sentry;

                    public class AnalyzerTest
                    {
                        public void Test(IHub hub)
                        {
                            var metrics = SentrySdk.Experimental.Metrics;

                            _ = metrics.GetType();

                    #pragma warning disable SENTRYTRACECONNECTEDMETRICS
                            _ = hub.Metrics.GetType();
                    #pragma warning restore SENTRYTRACECONNECTEDMETRICS

                            _ = SentrySdk.Experimental.Metrics.Equals(null);
                            _ = SentrySdk.Experimental.Metrics.GetHashCode();
                            _ = SentrySdk.Experimental.Metrics.GetType();
                            _ = SentrySdk.Experimental.Metrics.ToString();
                        }
                    }
                    """
                },
                ExpectedDiagnostics = { },
            }
        };

        await test.RunAsync();
    }

    [Fact]
    public async Task SupportedInvocations_NoDiagnostics()
    {
        var test = new CSharpAnalyzerTest<TraceConnectedMetricsAnalyzer, DefaultVerifier>
        {
            TestState =
            {
                ReferenceAssemblies = TargetFramework.ReferenceAssemblies,
                AdditionalReferences = { typeof(SentryTraceMetrics).Assembly },
                Sources =
                {
                    """
                    #nullable enable
                    using Sentry;

                    public class AnalyzerTest
                    {
                        public void Test(IHub hub)
                        {
                            var scope = new Scope(new SentryOptions());
                            var metrics = SentrySdk.Experimental.Metrics;

                    #pragma warning disable SENTRYTRACECONNECTEDMETRICS
                            metrics.EmitCounter("name", 1);
                            hub.Metrics.EmitCounter("name", 1f);
                            SentrySdk.Experimental.Metrics.EmitCounter<double>("name", 1.1d, [], scope);

                            metrics.EmitGauge("name", 2);
                            hub.Metrics.EmitGauge("name", 2f);
                            SentrySdk.Experimental.Metrics.EmitGauge<double>("name", 2.2d, "unit", [], scope);

                            metrics.EmitDistribution("name", 3);
                            hub.Metrics.EmitDistribution("name", 3f);
                            SentrySdk.Experimental.Metrics.EmitDistribution<double>("name", 3.3d, "unit", [], scope);
                    #pragma warning restore SENTRYTRACECONNECTEDMETRICS
                        }
                    }

                    public static class Extensions
                    {
                        public static void EmitCounter<T>(this SentryTraceMetrics metrics) where T : struct
                        {
                            metrics.EmitCounter<T>("default", default(T), [], null);
                        }

                        public static void EmitCounter<T>(this SentryTraceMetrics metrics, string name) where T : struct
                        {
                            metrics.EmitCounter<T>(name, default(T), [], null);
                        }

                        public static void EmitGauge<T>(this SentryTraceMetrics metrics) where T : struct
                        {
                            metrics.EmitGauge<T>("default", default(T), null, [], null);
                        }

                        public static void EmitGauge<T>(this SentryTraceMetrics metrics, string name) where T : struct
                        {
                            metrics.EmitGauge<T>(name, default(T), null, [], null);
                        }

                        public static void EmitDistribution<T>(this SentryTraceMetrics metrics) where T : struct
                        {
                            metrics.EmitDistribution<T>("default", default(T), null, [], null);
                        }

                        public static void EmitDistribution<T>(this SentryTraceMetrics metrics, string name) where T : struct
                        {
                            metrics.EmitDistribution<T>(name, default(T), null, [], null);
                        }
                    }
                    """
                },
                ExpectedDiagnostics = { },
            },
            SolutionTransforms = { SolutionTransforms.Nullable },
        };

        await test.RunAsync();
    }

    [Fact]
    public async Task UnsupportedInvocations_ReportDiagnostics()
    {
        var test = new CSharpAnalyzerTest<TraceConnectedMetricsAnalyzer, DefaultVerifier>
        {
            TestState =
            {
                ReferenceAssemblies = TargetFramework.ReferenceAssemblies,
                AdditionalReferences = { typeof(SentryTraceMetrics).Assembly },
                Sources =
                {
                    """
                    #nullable enable
                    using System;
                    using Sentry;

                    public class AnalyzerTest
                    {
                        public void Test(IHub hub)
                        {
                            var scope = new Scope(new SentryOptions());
                            var metrics = SentrySdk.Experimental.Metrics;

                    #pragma warning disable SENTRYTRACECONNECTEDMETRICS
                            {|#0:metrics.EmitCounter("name", (uint)1)|#0};
                            {|#1:hub.Metrics.EmitCounter("name", (StringComparison)1f)|#1};
                            {|#2:SentrySdk.Experimental.Metrics.EmitCounter<decimal>("name", 1.1m, [], scope)|#2};

                            {|#3:metrics.EmitGauge("name", (uint)2)|#3};
                            {|#4:hub.Metrics.EmitGauge("name", (StringComparison)2f)|#4};
                            {|#5:SentrySdk.Experimental.Metrics.EmitGauge<decimal>("name", 2.2m, "unit", [], scope)|#5};

                            {|#6:metrics.EmitDistribution("name", (uint)3)|#6};
                            {|#7:hub.Metrics.EmitDistribution("name", (StringComparison)3f)|#7};
                            {|#8:SentrySdk.Experimental.Metrics.EmitDistribution<decimal>("name", 3.3m, "unit", [], scope)|#8};
                    #pragma warning restore SENTRYTRACECONNECTEDMETRICS
                        }
                    }
                    """
                },
                ExpectedDiagnostics =
                {
                    CreateDiagnostic(0, typeof(uint)),
                    CreateDiagnostic(1, typeof(StringComparison)),
                    CreateDiagnostic(2, typeof(decimal)),
                    CreateDiagnostic(3, typeof(uint)),
                    CreateDiagnostic(4, typeof(StringComparison)),
                    CreateDiagnostic(5, typeof(decimal)),
                    CreateDiagnostic(6, typeof(uint)),
                    CreateDiagnostic(7, typeof(StringComparison)),
                    CreateDiagnostic(8, typeof(decimal)),
                },
            }
        };

        await test.RunAsync();
    }

    private static DiagnosticResult CreateDiagnostic(int markupKey, Type type)
    {
        Assert.NotNull(type.FullName);

        return Verifier.Diagnostic("SENTRY1001")
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithArguments(type.FullName)
            .WithMessage($"{type.FullName} is unsupported type for Sentry Metrics. The only supported types are byte, short, int, long, float, and double.")
            .WithMessageFormat("{0} is unsupported type for Sentry Metrics. The only supported types are byte, short, int, long, float, and double.")
            .WithLocation(markupKey);
    }
}
