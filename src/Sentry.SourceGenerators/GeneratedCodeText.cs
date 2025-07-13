namespace Sentry.SourceGenerators;

internal static class GeneratedCodeText
{
    public static string? Tool { get; } = typeof(BuildPropertySourceGenerator).Assembly.GetName().Name;
    public static string? Version { get; } = typeof(BuildPropertySourceGenerator).Assembly.GetName().Version?.ToString();
}
