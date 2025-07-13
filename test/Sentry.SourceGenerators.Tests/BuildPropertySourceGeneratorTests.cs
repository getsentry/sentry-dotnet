using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace Sentry.SourceGenerators.Tests;

public class BuildPropertySourceGeneratorTests
{
    private const string HintName = "Sentry.Generated.BuildPropertyInitializer.g.cs";

    [SkippableFact]
    public Task RunResult_Success()
    {
        Skip.If(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

        var driver = BuildDriver(OutputKind.ConsoleApplication, typeof(Program).Assembly, ("PublishAot", "false"));
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.Should().BeNull();
        result.GeneratedSources.Length.Should().Be(1);
        result.GeneratedSources.First().HintName.Should().Be(HintName);
        var source = result.GeneratedSources.First().SourceText.ToString();
        return Verify(source);
    }

    [SkippableFact]
    public Task RunResult_BadStrings()
    {
        Skip.If(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

        // we're hijacking PublishAot to make life easy
        var driver = BuildDriver(OutputKind.ConsoleApplication, typeof(Program).Assembly, ("My\\Key", "test\\test"));
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.Should().BeNull();
        result.GeneratedSources.Length.Should().Be(1);
        result.GeneratedSources.First().HintName.Should().Be(HintName);
        var source = result.GeneratedSources.First().SourceText.ToString();
        return Verify(source);
    }

    [SkippableFact]
    public Task RunResult_Publish_AotTrue()
    {
        Skip.If(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

        var driver = BuildDriver(OutputKind.ConsoleApplication, typeof(Program).Assembly, ("PublishAot", "true"));
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.Should().BeNull();
        result.GeneratedSources.Length.Should().Be(1);
        result.GeneratedSources.First().HintName.Should().Be(HintName);
        var source = result.GeneratedSources.First().SourceText.ToString();
        return Verify(source);
    }

    [SkippableFact]
    public void RunResult_NoProperties_NoGeneratedSources()
    {
        Skip.If(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

        var driver = BuildDriver(OutputKind.ConsoleApplication, typeof(Program).Assembly);
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.Should().BeNull();

        result.GeneratedSources.Should().BeEmpty();
    }

    [SkippableTheory]
    [InlineData(OutputKind.ConsoleApplication, true)]
    [InlineData(OutputKind.WindowsApplication, true)]
    [InlineData(OutputKind.WindowsRuntimeApplication, true)]
    [InlineData(OutputKind.DynamicallyLinkedLibrary, false)]
    [InlineData(OutputKind.NetModule, false)]
    [InlineData(OutputKind.WindowsRuntimeMetadata, false)]
    public void RunResult_OutputType_Values(OutputKind outputKind, bool sourceGenExpected)
    {
        Skip.If(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

        var driver = BuildDriver(outputKind, typeof(Program).Assembly, ("PublishTrimmed", "true"));
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.Should().BeNull();

        var generated = result.GeneratedSources.Any(x => x.HintName.Equals(HintName));
        generated.Should().Be(sourceGenExpected);
    }

    [SkippableTheory]
    [InlineData("no", true)]
    [InlineData("true", false)]
    [InlineData("false", true)]
    public void RunResult_SentryDisableSourceGenerator_Values(string value, bool sourceGenExpected)
    {
        Skip.If(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

        var driver = BuildDriver(OutputKind.ConsoleApplication, typeof(Program).Assembly, ("SentryDisableSourceGenerator", value));
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.Should().BeNull();

        var generated = result.GeneratedSources.Any(x => x.HintName.Equals(HintName));
        generated.Should().Be(sourceGenExpected);
    }

    [SkippableFact]
    public void RunResult_Expect_None()
    {
        Skip.If(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

        var driver = BuildDriver(OutputKind.DynamicallyLinkedLibrary, typeof(Program).Assembly, ("PublishAot", "false"));
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.Should().BeNull();
        result.GeneratedSources.Length.Should().Be(0);
    }

    private static GeneratorDriver BuildDriver(OutputKind outputKind, Assembly metadataAssembly, params IEnumerable<(string Key, string Value)> buildProperties)
    {
        var metadataReference = MetadataReference.CreateFromFile(metadataAssembly.Location);
        var options = new CSharpCompilationOptions(outputKind);
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
