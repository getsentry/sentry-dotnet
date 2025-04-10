using Microsoft.CodeAnalysis;

namespace Sentry.Analyzers;


/// <summary>
/// Generates the necessary msbuild variables
/// </summary>
[Generator(LanguageNames.CSharp)]
public class BuildVariableSourceGenerator : ISourceGenerator
{
    /// <summary>
    /// Initialize the source gen
    /// </summary>
    /// <param name="context"></param>
    public void Initialize(GeneratorInitializationContext context)
    {
    }

    /// <summary>
    /// Execute the source gen
    /// </summary>
    /// <param name="context"></param>
    public void Execute(GeneratorExecutionContext context)
    {
        var opts = context.AnalyzerConfigOptions.GlobalOptions;
        var properties = opts.Keys.Where(x => x.StartsWith("build_property.")).ToList();
        if (properties.Count == 0)
            return;

        if (opts.TryGetValue("build_property.DisableSentrySourceGenerator", out var _))
            return;

        // we only want to generate code where host setup takes place
        if (!opts.TryGetValue("build_property.outputtype", out var outputType))
            return;

        if (!outputType.Equals("exe", StringComparison.InvariantCultureIgnoreCase))
            return;

        var sb = new StringBuilder();
        sb
            .AppendLine("namespace Sentry;")
            .AppendLine()
            .AppendLine("[global::System.Runtime.CompilerServices.CompilerGenerated]")
            .AppendLine("public static class BuildVariableInitializer")
            .AppendLine("{")
            .AppendLine("\t[global::System.Runtime.CompilerServices.ModuleInitializer]")
            .AppendLine("\tpublic static void Initialize()")
            .AppendLine("\t{")
            .AppendLine("\t\tglobal::Sentry.SentrySdk.InitializeBuildVariables(new global::System.Collections.Generic.Dictionary<string, string> {");

        foreach (var property in properties)
        {
            if (opts.TryGetValue(property, out var value))
            {
                var pn = property.Replace("build_property.", "");
                var ev = value.Replace("\\", "\\\\");
                sb
                    .Append("\t\t\t{")
                    .Append($"\"{pn}\", \"{ev}\"")
                    .Append("},\r\n");
            }
        }

        sb
            .AppendLine("\t\t});") // close dictionary
            .AppendLine("\t}")
            .AppendLine("}");

        context.AddSource("__BuildVariables.g.cs", sb.ToString());
    }
}
