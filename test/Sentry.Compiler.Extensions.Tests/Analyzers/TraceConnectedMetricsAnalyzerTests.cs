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
            SolutionTransforms = { SolutionTransforms.Nullable },
            TestState =
            {
                ReferenceAssemblies = ReferenceAssemblies.Current,
                AdditionalReferences = { typeof(SentryMetricEmitter).Assembly },
                Sources =
                {
                    """
                    #nullable disable
                    using System;
                    using Sentry;

                    public class AnalyzerTest
                    {
                        public void Init(SentryOptions options)
                        {
                            options.Experimental.EnableMetrics = false;
                        }

                        public void Emit(IHub hub)
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
            },
        };

        await test.RunAsync();
    }

    [Fact]
    public async Task SupportedInvocations_NoDiagnostics()
    {
        var test = new CSharpAnalyzerTest<TraceConnectedMetricsAnalyzer, DefaultVerifier>
        {
            SolutionTransforms = { SolutionTransforms.Nullable },
            TestState =
            {
                ReferenceAssemblies = ReferenceAssemblies.Current,
                AdditionalReferences = { typeof(SentryMetricEmitter).Assembly },
                Sources =
                {
                    """
                    #nullable enable
                    using System;
                    using Sentry;

                    public class AnalyzerTest
                    {
                        public void Init(SentryOptions options)
                        {
                            options.Experimental.SetBeforeSendMetric(static SentryMetric? (SentryMetric metric) => metric.TryGetValue<double>(out _) ? metric : null);
                            options.Experimental.SetBeforeSendMetric(OnBeforeSendMetric);
                            options.Experimental.SetBeforeSendMetric(OnBeforeSendMetric<double>);
                        }

                        public void Emit(IHub hub)
                        {
                            var scope = new Scope(new SentryOptions());
                            var metrics = SentrySdk.Experimental.Metrics;

                    #pragma warning disable SENTRYTRACECONNECTEDMETRICS
                            metrics.EmitCounter("name", 1);
                            hub.Metrics.EmitCounter("name", 1f);
                            SentrySdk.Experimental.Metrics.EmitCounter<double>("name", 1.1d, [], scope);

                            metrics.EmitGauge("name", 2);
                            hub.Metrics.EmitGauge("name", 2f);
                            SentrySdk.Experimental.Metrics.EmitGauge<double>("name", 2.2d, MeasurementUnit.Custom("unit"), [], scope);

                            metrics.EmitDistribution("name", 3);
                            hub.Metrics.EmitDistribution("name", 3f);
                            SentrySdk.Experimental.Metrics.EmitDistribution<double>("name", 3.3d, MeasurementUnit.Custom("unit"), [], scope);
                    #pragma warning restore SENTRYTRACECONNECTEDMETRICS
                        }

                        private static SentryMetric? OnBeforeSendMetric(SentryMetric metric)
                        {
                            if (metric.TryGetValue<double>(out _))
                            {
                                return metric;
                            }

                            return null;
                        }

                        private static SentryMetric? OnBeforeSendMetric<T>(SentryMetric metric) where T : struct
                        {
                            if (metric.TryGetValue<T>(out _))
                            {
                                return metric;
                            }

                            return null;
                        }
                    }

                    public static class Extensions
                    {
                        public static void EmitCounter<T>(this SentryMetricEmitter metrics) where T : struct
                        {
                            metrics.EmitCounter<T>("default", default(T), [], (Scope?)null);
                        }

                        public static void EmitCounter<T>(this SentryMetricEmitter metrics, string name) where T : struct
                        {
                            metrics.EmitCounter<T>(name, default(T), [], (Scope?)null);
                        }

                        public static void EmitGauge<T>(this SentryMetricEmitter metrics) where T : struct
                        {
                            metrics.EmitGauge<T>("default", default(T), default(MeasurementUnit), [], (Scope?)null);
                        }

                        public static void EmitGauge<T>(this SentryMetricEmitter metrics, string name) where T : struct
                        {
                            metrics.EmitGauge<T>(name, default(T), default(MeasurementUnit), [], (Scope?)null);
                        }

                        public static void EmitDistribution<T>(this SentryMetricEmitter metrics) where T : struct
                        {
                            metrics.EmitDistribution<T>("default", default(T), default(MeasurementUnit), [], (Scope?)null);
                        }

                        public static void EmitDistribution<T>(this SentryMetricEmitter metrics, string name) where T : struct
                        {
                            metrics.EmitDistribution<T>(name, default(T), default(MeasurementUnit), [], (Scope?)null);
                        }
                    }
                    """
                },
                ExpectedDiagnostics = { },
            },
        };

        await test.RunAsync();
    }

    [Fact]
    public async Task UnsupportedInvocations_ReportDiagnostics()
    {
        var test = new CSharpAnalyzerTest<TraceConnectedMetricsAnalyzer, DefaultVerifier>
        {
            SolutionTransforms = { SolutionTransforms.Nullable },
            TestState =
            {
                ReferenceAssemblies = ReferenceAssemblies.Current,
                AdditionalReferences = { typeof(SentryMetricEmitter).Assembly },
                Sources =
                {
                    """
                    #nullable enable
                    using System;
                    using Sentry;

                    public class AnalyzerTest
                    {
                        public void Init(SentryOptions options)
                        {
                            options.Experimental.SetBeforeSendMetric(static SentryMetric? (SentryMetric metric) => {|#0:metric.TryGetValue<sbyte>(out _)|#0} ? metric : null);
                            options.Experimental.SetBeforeSendMetric(OnBeforeSendMetric);
                            options.Experimental.SetBeforeSendMetric(OnBeforeSendMetric<ulong>);
                        }

                        public void Emit(IHub hub)
                        {
                            var scope = new Scope(new SentryOptions());
                            var metrics = SentrySdk.Experimental.Metrics;

                    #pragma warning disable SENTRYTRACECONNECTEDMETRICS
                            {|#10:metrics.EmitCounter("name", (uint)1)|#10};
                            {|#11:hub.Metrics.EmitCounter("name", (StringComparison)1f)|#11};
                            {|#12:SentrySdk.Experimental.Metrics.EmitCounter<decimal>("name", 1.1m, [], scope)|#12};

                            {|#13:metrics.EmitGauge("name", (uint)2)|#13};
                            {|#14:hub.Metrics.EmitGauge("name", (StringComparison)2f)|#14};
                            {|#15:SentrySdk.Experimental.Metrics.EmitGauge<decimal>("name", 2.2m, MeasurementUnit.Custom("unit"), [], scope)|#15};

                            {|#16:metrics.EmitDistribution("name", (uint)3)|#16};
                            {|#17:hub.Metrics.EmitDistribution("name", (StringComparison)3f)|#17};
                            {|#18:SentrySdk.Experimental.Metrics.EmitDistribution<decimal>("name", 3.3m, MeasurementUnit.Custom("unit"), [], scope)|#18};
                    #pragma warning restore SENTRYTRACECONNECTEDMETRICS
                        }

                        private static SentryMetric? OnBeforeSendMetric(SentryMetric metric)
                        {
                            if ({|#1:metric.TryGetValue<ushort>(out _)|#1})
                            {
                                return metric;
                            }

                            return null;
                        }

                        private static SentryMetric? OnBeforeSendMetric<T>(SentryMetric metric) where T : struct
                        {
                            if (metric.TryGetValue<T>(out _))
                            {
                                return metric;
                            }

                            return null;
                        }
                    }
                    """
                },
                ExpectedDiagnostics =
                {
                    CreateDiagnostic(0, typeof(sbyte)),
                    CreateDiagnostic(1, typeof(ushort)),

                    CreateDiagnostic(10, typeof(uint)),
                    CreateDiagnostic(11, typeof(StringComparison)),
                    CreateDiagnostic(12, typeof(decimal)),
                    CreateDiagnostic(13, typeof(uint)),
                    CreateDiagnostic(14, typeof(StringComparison)),
                    CreateDiagnostic(15, typeof(decimal)),
                    CreateDiagnostic(16, typeof(uint)),
                    CreateDiagnostic(17, typeof(StringComparison)),
                    CreateDiagnostic(18, typeof(decimal)),
                },
            },
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
