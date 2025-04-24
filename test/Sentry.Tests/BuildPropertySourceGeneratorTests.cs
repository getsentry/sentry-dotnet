#if !__MOBILE__ && (NET8_0 || NET9_0)
#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Sentry.SourceGenerators;

namespace Sentry.Tests;


public class BuildPropertySourceGeneratorTests
{
    [Fact]
    public Task RunResult_Success()
    {
        var driver = BuildDriver(typeof(Program).Assembly, ("PublishAot", "false"), ("OutputType", "exe"));
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.Should().BeNull();
        result.GeneratedSources.Length.Should().Be(1);
        result.GeneratedSources.First().HintName.Should().Be("__BuildVariables.g.cs");
        return Verify(result);
    }


    [Fact]
    public Task RunResult_BadStrings()
    {
        // we're hijacking PublishAot to make life easy
        var driver = BuildDriver(typeof(Program).Assembly, ("My\\Key", "test\\test"), ("OutputType", "exe"));
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.Should().BeNull();
        result.GeneratedSources.Length.Should().Be(1);
        result.GeneratedSources.First().HintName.Should().Be("__BuildVariables.g.cs");
        return Verify(result);
    }


    [Fact]
    public Task RunResult_Publish_AotTrue()
    {
        var driver = BuildDriver(typeof(Program).Assembly, ("PublishAot", "true"), ("OutputType", "exe"));
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.Should().BeNull();
        result.GeneratedSources.Length.Should().Be(1);
        result.GeneratedSources.First().HintName.Should().Be("__BuildVariables.g.cs");
        return Verify(result);
    }


    [Fact]
    public Task RunResult_Expect_None()
    {
        var driver = BuildDriver(typeof(Program).Assembly, ("PublishAot", "false"));
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.Should().BeNull();
        result.GeneratedSources.Length.Should().Be(0);

        return Verify(result);
    }


    static GeneratorDriver BuildDriver(Assembly metadataAssembly, params IEnumerable<(string Key, string Value)> buildProperties)
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
    readonly MockAnalyzerConfigOptions options = new (buildProperties);

    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => options;
    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)  => options;
    public override AnalyzerConfigOptions GlobalOptions => options;
}

file class MockAnalyzerConfigOptions(Dictionary<string, string> values) : AnalyzerConfigOptions
{
    public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
        => values.TryGetValue(key, out value);

    public override IEnumerable<string> Keys => values.Keys;
}
#endif
