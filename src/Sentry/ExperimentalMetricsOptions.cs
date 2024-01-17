namespace Sentry;

/// <summary>
/// Settings for the experimental Metrics feature. This feature is preview only and will very likely change in the future
/// without a major version bump... so use at your own risk.
/// </summary>
public class ExperimentalMetricsOptions
{
    /// <summary>
    /// Determines whether code locations should be recorded for Metrics
    /// </summary>
    public bool EnableCodeLocations { get; set; } = true;

    private IList<SubstringOrRegexPattern> _captureInstruments = new List<SubstringOrRegexPattern>();

    /// <summary>
    /// <para>
    /// A customizable list of <see cref="SubstringOrRegexPattern"/>s defining which Instruments should be collected and
    /// reported to Sentry.
    /// </para>
    /// <para>
    /// These can be either custom System.Diagnostics.Metrics that you have instrumented in your
    /// application or any of the built in metrics that are available.
    /// </para>
    /// <para>
    /// See https://learn.microsoft.com/en-us/dotnet/core/diagnostics/built-in-metrics for more information.
    /// </para>
    /// </summary>
    public IList<SubstringOrRegexPattern> CaptureInstruments
    {
        // NOTE: During configuration binding, .NET 6 and lower used to just call Add on the existing item.
        //       .NET 7 changed this to call the setter with an array that already starts with the old value.
        //       We have to handle both cases.
        get => _captureInstruments;
        set => _captureInstruments = value.SetWithConfigBinding();
    }
}
