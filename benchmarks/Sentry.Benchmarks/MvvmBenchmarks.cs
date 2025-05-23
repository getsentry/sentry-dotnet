using BenchmarkDotNet.Attributes;
using Sentry;
using Sentry.Maui;

public class MvvmBenchmarks
{
    private MauiAppBuilder AppBuilder;

    [Params(RegisterEventBinderMethod.ServiceProvider, RegisterEventBinderMethod.InvokeConfigOptions, RegisterEventBinderMethod.Directly)]
    public RegisterEventBinderMethod ResolveOptionsWithServiceProvider;

    [GlobalSetup]
    public void Setup()
    {
        AppBuilder = MauiApp.CreateBuilder()
            // This adds Sentry to your Maui application
            .UseSentry(options =>
            {
                // The DSN is the only required option.
                options.Dsn = DsnSamples.ValidDsn;
                // Automatically create traces for async relay commands in the MVVM Community Toolkit
                options.AddCommunityToolkitIntegration();
            }, ResolveOptionsWithServiceProvider);
    }

    [Benchmark(Description = "Build MAUI App")]
    public void BuildMauiAppBenchmark()
    {
        AppBuilder.Build();
    }
}
