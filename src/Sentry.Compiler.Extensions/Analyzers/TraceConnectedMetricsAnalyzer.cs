using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Sentry.Compiler.Extensions.Analyzers;

/// <summary>
/// Guide consumers to use the public API of <see href="https://develop.sentry.dev/sdk/telemetry/metrics/">Sentry Trace-connected Metrics</see> correctly.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TraceConnectedMetricsAnalyzer : DiagnosticAnalyzer
{
    private static readonly string Title = "Unsupported numeric type of Metric";
    private static readonly string MessageFormat = "{0} is unsupported type for Sentry Metrics. The only supported types are byte, short, int, long, float, and double.";
    private static readonly string Description = "Integers should be a 64-bit signed integer, while doubles should be a 64-bit floating point number.";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.Sentry1001,
        title: Title,
        messageFormat: MessageFormat,
        category: DiagnosticCategories.Sentry,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description,
        helpLinkUri: null
    );

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterOperationAction(Execute, OperationKind.Invocation);
    }

    private static void Execute(OperationAnalysisContext context)
    {
        Debug.Assert(context.Operation.Language == LanguageNames.CSharp);
        Debug.Assert(context.Operation.Kind is OperationKind.Invocation);

        context.CancellationToken.ThrowIfCancellationRequested();

        if (context.Operation is not IInvocationOperation invocation)
        {
            return;
        }

        var method = invocation.TargetMethod;
        if (method.DeclaredAccessibility != Accessibility.Public || method.IsStatic || method.Parameters.Length == 0)
        {
            return;
        }

        if (!method.IsGenericMethod || method.Arity != 1 || method.TypeArguments.Length != 1)
        {
            return;
        }

        if (method.ContainingAssembly is null || method.ContainingAssembly.Name != "Sentry")
        {
            return;
        }

        if (method.ContainingNamespace is null || method.ContainingNamespace.Name != "Sentry")
        {
            return;
        }

        string fullyQualifiedMetadataName;
        if (method.Name is "EmitCounter" or "EmitGauge" or "EmitDistribution")
        {
            fullyQualifiedMetadataName = "Sentry.SentryMetricEmitter";
        }
        else if (method.Name is "TryGetValue")
        {
            fullyQualifiedMetadataName = "Sentry.SentryMetric";
        }
        else
        {
            return;
        }

        var typeArgument = method.TypeArguments[0];
        if (typeArgument.SpecialType is SpecialType.System_Byte or SpecialType.System_Int16 or SpecialType.System_Int32 or SpecialType.System_Int64 or SpecialType.System_Single or SpecialType.System_Double)
        {
            return;
        }

        if (typeArgument is ITypeParameterSymbol)
        {
            return;
        }

        var sentryType = context.Compilation.GetTypeByMetadataName(fullyQualifiedMetadataName);
        if (sentryType is null)
        {
            return;
        }

        if (!SymbolEqualityComparer.Default.Equals(method.ContainingType, sentryType))
        {
            return;
        }

        var location = invocation.Syntax.GetLocation();
        var diagnostic = Diagnostic.Create(Rule, location, typeArgument.ToDisplayString(SymbolDisplayFormats.FullNameFormat));
        context.ReportDiagnostic(diagnostic);
    }
}
