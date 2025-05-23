using BenchmarkDotNet.Attributes;
using Sentry;
using Sentry.Maui;

public class MvvmBenchmarks
{
    [Params(RegisterEventBinderMethod.ServiceProvider, RegisterEventBinderMethod.InvokeConfigOptions, RegisterEventBinderMethod.Directly)]
    public RegisterEventBinderMethod ResolveOptionsWithServiceProvider;

    [Benchmark(Description = "Build MAUI App")]
    public void BuildMauiAppBenchmark()
    {
        var appBuilder = MauiApp.CreateBuilder()
            // This adds Sentry to your Maui application
            .UseSentry(options =>
            {
                // The DSN is the only required option.
                options.Dsn = DsnSamples.ValidDsn;
                // Automatically create traces for async relay commands in the MVVM Community Toolkit
                options.AddCommunityToolkitIntegration();
            }, ResolveOptionsWithServiceProvider);
        appBuilder.Build();
    }
}
