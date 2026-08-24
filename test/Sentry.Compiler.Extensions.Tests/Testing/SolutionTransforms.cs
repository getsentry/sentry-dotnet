using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Sentry.Compiler.Extensions.Tests.Testing;

internal static class SolutionTransforms
{
    private static readonly ImmutableDictionary<string, ReportDiagnostic> s_nullableWarnings = GetNullableWarningsFromCompiler();

    internal static Func<Solution, ProjectId, Solution> Nullable { get; } = static (solution, projectId) =>
    {
        var project = solution.GetProject(projectId);
        Assert.NotNull(project);

        var compilationOptions = project.CompilationOptions;
        Assert.NotNull(compilationOptions);

        compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(compilationOptions.SpecificDiagnosticOptions.SetItems(s_nullableWarnings));

        solution = solution.WithProjectCompilationOptions(projectId, compilationOptions);
        return solution;
    };

    private static ImmutableDictionary<string, ReportDiagnostic> GetNullableWarningsFromCompiler()
    {
        string[] args = { "/warnaserror:nullable" };
        var commandLineArguments = CSharpCommandLineParser.Default.Parse(args, Environment.CurrentDirectory, Environment.CurrentDirectory, null);
        var nullableWarnings = commandLineArguments.CompilationOptions.SpecificDiagnosticOptions;

        // Workaround for https://github.com/dotnet/roslyn/issues/41610
        nullableWarnings = nullableWarnings
            .SetItem("CS8632", ReportDiagnostic.Error)
            .SetItem("CS8669", ReportDiagnostic.Error);

        return nullableWarnings;
    }
}
