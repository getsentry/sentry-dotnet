using System.Numerics;

namespace Sentry.Samples.Console.Metrics;

internal static class Program
{
    private static readonly Random Roll = new();

    private static void Main()
    {
        // Enable the SDK
        using (SentrySdk.Init(options =>
               {
                   options.Dsn =
                       // NOTE: ADD YOUR OWN DSN BELOW so you can see the events in your own Sentry account
                       "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537";

                   options.Debug = true;
                   options.IsGlobalModeEnabled = true;
                   // Initialize some (non null) ExperimentalMetricsOptions to enable Sentry Metrics,
                   options.ExperimentalMetrics = new ExperimentalMetricsOptions
                   {
                       EnableCodeLocations =
                           true // Set this to false if you don't want to track code locations for some reason
                   };
               }))
        {
            System.Console.WriteLine("Measure, Yeah, Measure");
            PlaySetBingo(30);
            CreateRevenueGauge(1000);
            MeasureShrimp(1000);
            System.Console.WriteLine("Measure up");
        }
    }

    private static void PlaySetBingo(int attempts)
    {
        var solution = new[] { 3, 5, 7, 11, 13, 17 };

        // The Timing class creates a distribution that is designed to measure the amount of time it takes to run code
        // blocks. By default it will use a unit of Seconds - we're configuring it to use milliseconds here though.
        using (new Timing(nameof(PlaySetBingo), MeasurementUnit.Duration.Millisecond))
        {
            for (var i = 0; i < attempts; i++)
            {
                var guess = Roll.Next(1, 100);
                // This demonstrates the use of a set metric.
                SentrySdk.Metrics.Gauge("guesses", guess);

                if (solution.Contains(guess))
                {
                    // And this is a counter
                    SentrySdk.Metrics.Increment("correct_answers");
                }
            }
        }
    }

    private static void CreateRevenueGauge(int sampleCount)
    {
        using (new Timing(nameof(CreateRevenueGauge), MeasurementUnit.Duration.Millisecond))
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
