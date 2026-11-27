using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Sentry.Compiler.Extensions.Tests;

public class SentryMetricEmitterOverloadResolutionTests
{
    private const string Source = """
        using System.Collections.Generic;
        using Sentry;

        public static class Program
        {
            public static void Emit(SentryMetricEmitter metrics)
            {
                var attributes = new[] { new KeyValuePair<string, object>("key", "value") };
                metrics.EmitCounter("counter", 1, attributes);
                metrics.EmitCounter("counter", 1, attributes, null);
                metrics.EmitGauge("gauge", 1.0, MeasurementUnit.Duration.Second, attributes);
                metrics.EmitDistribution("distribution", 1.0, MeasurementUnit.Duration.Second, attributes);
        #pragma warning disable CS0618
                metrics.EmitGauge("gauge", 1.0, "unit", attributes);
                metrics.EmitDistribution("distribution", 1.0, "unit", attributes);
        #pragma warning restore CS0618
            }
        }
        """;

    [Theory]
    [InlineData(LanguageVersion.CSharp7_3)]
    [InlineData(LanguageVersion.CSharp9)]
    [InlineData(LanguageVersion.CSharp12)]
    [InlineData(LanguageVersion.Latest)]
    public void Emit_ArrayAttributes_IsNotAmbiguous(LanguageVersion languageVersion)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(Source, new CSharpParseOptions(languageVersion));
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Append(typeof(SentryMetricEmitter).Assembly.Location)
            .Distinct()
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create(
            "OverloadResolution",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var errors = compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        Assert.Empty(errors);
    }
}
