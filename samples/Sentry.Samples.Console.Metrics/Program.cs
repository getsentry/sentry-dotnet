using System.Diagnostics.Metrics;
using System.Text.RegularExpressions;

namespace Sentry.Samples.Console.Metrics;

internal static class Program
{
    private static readonly Random Roll = Random.Shared;

    // Sentry also supports capturing System.Diagnostics.Metrics
    private static readonly Meter HatsMeter = new("HatCo.HatStore", "1.0.0");
    private static readonly Counter<int> HatsSold = HatsMeter.CreateCounter<int>(
        name: "hats-sold",
        unit: "Hats",
        description: "The number of hats sold in our store");

    private static async Task Main()
    {
        // Enable the SDK
        using (SentrySdk.Init(options =>
               {
                   options.Dsn =
                       // NOTE: ADD YOUR OWN DSN BELOW so you can see the events in your own Sentry account
                       "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537";

                   options.Debug = true;
                   options.StackTraceMode = StackTraceMode.Enhanced;
                   // Initialize some (non null) ExperimentalMetricsOptions to enable Sentry Metrics,
                   options.ExperimentalMetrics = new ExperimentalMetricsOptions
                   {
                        EnableCodeLocations = true, // Set this to false if you don't want to track code locations
                        CaptureSystemDiagnosticsInstruments = [
                            // Capture System.Diagnostics.Metrics matching the name "HatCo.HatStore", which is the name
                            // of the custom HatsMeter defined above
                            "hats-sold"
                        ],
                        // Capture all built in metrics (this is the default - you can override this to capture some or
                        // none of these if you prefer)
                        CaptureSystemDiagnosticsMeters = BuiltInSystemDiagnosticsMeters.All
                   };
               }))
        {
            System.Console.WriteLine("Measure, Yeah, Measure!");
            do
            {
                // Perform your task here
                switch (Roll.Next(1,4))
                {
                    case 1:
                        PlaySetBingo(10);
                        break;
                    case 2:
                        MeasureShrimp(30);
                        break;
                    case 3:
                        // Here we're emitting the metric using System.Diagnostics.Metrics instead of SentrySdk.Metrics.
                        // We won't see accurate code locations for these, so Sentry.Metrics are preferable but support
                        // for System.Diagnostics.Metrics means Sentry can collect a bunch built in metrics without you
                        // having to instrument anything... see case 4 below
                        HatsSold.Add(Roll.Next(0, 1000));
                        break;
                    case 4:
                        // Here we demonstrate collecting some built in metrics for HTTP requests... this works because
                        // we've configured ExperimentalMetricsOptions.CaptureInstruments to match "http.client.*"
                        //
                        // See https://learn.microsoft.com/en-us/dotnet/core/diagnostics/built-in-metrics-system-net#systemnethttp
                        var httpClient = new HttpClient();
                        var url = "https://api.sampleapis.com/coffee/hot";
                        var result = await httpClient.GetAsync(url);
                        System.Console.WriteLine($"GET {url} {result.StatusCode}");
                        break;
                }

                // Optional: Delay to prevent tight looping
                var sleepTime = Roll.Next(1, 5);
                System.Console.WriteLine($"Sleeping for {sleepTime} second(s).");
                System.Console.WriteLine("Press any key to stop...");
                Thread.Sleep(TimeSpan.FromSeconds(sleepTime));
            }
            while (!System.Console.KeyAvailable);
            System.Console.WriteLine("Measure up");
        }
    }

    private static void PlaySetBingo(int attempts)
    {
        var solution = new[] { 3, 5, 7, 11, 13, 17 };

        // The Timing class creates a distribution that is designed to measure the amount of time it takes to run code
        // blocks. By default it will use a unit of Seconds - we're configuring it to use milliseconds here though.
        using (new Timing("bingo", MeasurementUnit.Duration.Millisecond))
        {
            for (var i = 0; i < attempts; i++)
            {
                var guess = Roll.Next(1, 100);
                // This demonstrates the use of a set metric.
                SentrySdk.Metrics.Gauge("guesses", guess);

                // And this is a counter
                SentrySdk.Metrics.Increment(solution.Contains(guess) ? "correct_answers" : "incorrect_answers");
            }
        }
    }

    private static void MeasureShrimp(int sampleCount)
    {
        using (new Timing(nameof(MeasureShrimp), MeasurementUnit.Duration.Millisecond))
        {
            for (var i = 0; i < sampleCount; i++)
            {
                var sizeOfShrimp = 15 + Roll.NextDouble() * 30;
                // This is an example of emitting a distribution metric
                SentrySdk.Metrics.Distribution("shrimp.size", sizeOfShrimp, MeasurementUnit.Custom("cm"));
            }
        }
    }
}
