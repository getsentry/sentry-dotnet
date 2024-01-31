using System.Diagnostics.Metrics;

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
                   options.SampleRate = 1.0f; // Not recommended in production - may adversely impact quota
                   options.TracesSampleRate = 1.0f; // Not recommended in production - may adversely impact quota
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
                        CaptureSystemDiagnosticsMeters = BuiltInSystemDiagnosticsMeters.All,
                        CaptureSystemDiagnosticsEventSourceNames = [ IterationEventCounterSource.EventSourceName ]
                   };
               }))
        {
            System.Console.WriteLine("Measure, Yeah, Measure!");

            Action[] actions = [PlaySetBingo, CreateRevenueGauge, MeasureShrimp, SellHats];
            do
            {
                IterationEventCounterSource.Log.AddLoopCount();

                // // Run a random action
                // var idx = Roll.Next(0, actions.Length);
                // actions[idx]();
                //
                // // Make an API call
                // await CallSampleApiAsync();

                // Optional: Delay to prevent tight looping
                var sleepTime = Roll.Next(1, 5);
                System.Console.WriteLine($"Sleeping for {sleepTime} second(s).");
                System.Console.WriteLine("Press any key to stop...");
                await Task.Delay(TimeSpan.FromSeconds(sleepTime));
            }
            while (!System.Console.KeyAvailable);
            System.Console.WriteLine("Measure up");
        }
    }

    private static void PlaySetBingo()
    {
        const int attempts = 10;
        var solution = new[] { 3, 5, 7, 11, 13, 17 };

        // StartTimer creates a distribution that is designed to measure the amount of time it takes to run code
        // blocks. By default it will use a unit of Seconds - we're configuring it to use milliseconds here though.
        // The return value is an IDisposable and the timer will stop when the timer is disposed of.
        using (SentrySdk.Metrics.StartTimer("bingo", MeasurementUnit.Duration.Millisecond))
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

    private static void CreateRevenueGauge()
    {
        const int sampleCount = 100;
        using (SentrySdk.Metrics.StartTimer(nameof(CreateRevenueGauge), MeasurementUnit.Duration.Millisecond))
        {
            for (var i = 0; i < sampleCount; i++)
            {
                var movement = Roll.NextDouble() * 30 - Roll.NextDouble() * 10;
                // This demonstrates measuring something in your app using a gauge... we're also using a custom
                // measurement unit here (which is optional - by default the unit will be "None")
                SentrySdk.Metrics.Gauge("revenue", movement, MeasurementUnit.Custom("$"));
            }
        }
    }

    private static void MeasureShrimp()
    {
        const int sampleCount = 30;
        using (SentrySdk.Metrics.StartTimer(nameof(MeasureShrimp), MeasurementUnit.Duration.Millisecond))
        {
            for (var i = 0; i < sampleCount; i++)
            {
                var sizeOfShrimp = 15 + Roll.NextDouble() * 30;
                // This is an example of emitting a distribution metric
                SentrySdk.Metrics.Distribution("shrimp.size", sizeOfShrimp, MeasurementUnit.Custom("cm"));
            }
        }
    }

    private static void SellHats()
    {
        // Here we're emitting the metric using System.Diagnostics.Metrics instead of SentrySdk.Metrics.
        // We won't see accurate code locations for these, so Sentry.Metrics are preferable but support
        // for System.Diagnostics.Metrics means Sentry can collect a bunch built in metrics without you
        // having to instrument anything... see case 4 below
        HatsSold.Add(Roll.Next(0, 1000));
    }

    private static async Task CallSampleApiAsync()
    {
        // Here we demonstrate collecting some built in metrics for HTTP requests... this works because we have:
        // `ExperimentalMetricsOptions.CaptureSystemDiagnosticsMeters = BuiltInSystemDiagnosticsMeters.All`
        //
        // See https://learn.microsoft.com/en-us/dotnet/core/diagnostics/built-in-metrics-system-net#systemnethttp
        var httpClient = new HttpClient();
        var url = "https://api.sampleapis.com/coffee/hot";
        var result = await httpClient.GetAsync(url);
        System.Console.WriteLine($"GET {url} {result.StatusCode}");
    }
}
