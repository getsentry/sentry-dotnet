using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace Sentry.SourceGenerators.Tests;

public class BuildPropertySourceGeneratorTests
{
    [SkippableFact]
    public Task RunResult_Success()
    {
        Skip.If(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

        var driver = BuildDriver(typeof(Program).Assembly, ("PublishAot", "false"), ("OutputType", "exe"));
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.Should().BeNull();
        result.GeneratedSources.Length.Should().Be(1);
        result.GeneratedSources.First().HintName.Should().Be("__BuildProperties.g.cs");
        var source = result.GeneratedSources.First().SourceText.ToString();
        return Verify(source);
    }

    [SkippableFact]
    public Task RunResult_BadStrings()
    {
        Skip.If(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

        // we're hijacking PublishAot to make life easy
        var driver = BuildDriver(typeof(Program).Assembly, ("My\\Key", "test\\test"), ("OutputType", "exe"));
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.Should().BeNull();
        result.GeneratedSources.Length.Should().Be(1);
        result.GeneratedSources.First().HintName.Should().Be("__BuildProperties.g.cs");
        var source = result.GeneratedSources.First().SourceText.ToString();
        return Verify(source);
    }

    [SkippableFact]
    public Task RunResult_Publish_AotTrue()
    {
        Skip.If(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

        var driver = BuildDriver(typeof(Program).Assembly, ("PublishAot", "true"), ("OutputType", "exe"));
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.Should().BeNull();
        result.GeneratedSources.Length.Should().Be(1);
        result.GeneratedSources.First().HintName.Should().Be("__BuildProperties.g.cs");
        var source = result.GeneratedSources.First().SourceText.ToString();
        return Verify(source);
    }

    [SkippableTheory]
    [InlineData("no", true)]
    [InlineData("true", false)]
    [InlineData("false", true)]
    public void RunResult_SentryDisableSourceGenerator_Values(string value, bool sourceGenExpected)
    {
        Skip.If(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

        var driver = BuildDriver(typeof(Program).Assembly, ("SentryDisableSourceGenerator", value), ("OutputType", "exe"));
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.Should().BeNull();

        var generated = result.GeneratedSources.Any(x => x.HintName.Equals("__BuildProperties.g.cs"));
        generated.Should().Be(sourceGenExpected);
    }

    [SkippableFact]
    public Task RunResult_Expect_None()
    {
        Skip.If(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

        var driver = BuildDriver(typeof(Program).Assembly, ("PublishAot", "false"));
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.Should().BeNull();
        result.GeneratedSources.Length.Should().Be(0);

        return Verify(result);
    }

    private static GeneratorDriver BuildDriver(Assembly metadataAssembly, params IEnumerable<(string Key, string Value)> buildProperties)
    {
        var metadataReference = MetadataReference.CreateFromFile(metadataAssembly.Location);
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        var compilation = CSharpCompilation.Create("TestAssembly", [], [metadataReference], options);
        var generator = new BuildPropertySourceGenerator();

        var dict = buildProperties.ToDictionary(x => "build_property." + x.Key, x => x.Value, comparer: StringComparer.InvariantCultureIgnoreCase);
        var provider = new MockAnalyzerConfigOptionsProvider(dict);

        var driver = CSharpGeneratorDriver.Create([generator], optionsProvider: provider);
        return driver.RunGenerators(compilation);
    }
}

file class MockAnalyzerConfigOptionsProvider(Dictionary<string, string> buildProperties) : AnalyzerConfigOptionsProvider
{
    private readonly MockAnalyzerConfigOptions _options = new(buildProperties);

    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => _options;
    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => _options;
    public override AnalyzerConfigOptions GlobalOptions => _options;
}

file class MockAnalyzerConfigOptions(Dictionary<string, string> values) : AnalyzerConfigOptions
{
    public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
        => values.TryGetValue(key, out value);

    public override IEnumerable<string> Keys => values.Keys;
}
