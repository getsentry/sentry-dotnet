namespace Sentry;

/// <inheritdoc cref="MetricsOptions"/>
[Obsolete("Metrics are no longer experimental. Use MetricsOptions instead. This class will be removed in a future version.")]
public class ExperimentalMetricsOptions : MetricsOptions
{
}

/// <summary>
/// Settings for Sentry Metrics.
/// </summary>
public class MetricsOptions
{
    /// <summary>
    /// Determines whether code locations should be recorded for Metrics
    /// </summary>
    public bool EnableCodeLocations { get; set; } = true;

    private IList<SubstringOrRegexPattern> _captureSystemDiagnosticsInstruments = new List<SubstringOrRegexPattern>();

    /// <summary>
    /// <para>
    /// A list of Substrings or Regular Expressions. Any `System.Diagnostics.Metrics.Instrument` whose name
    /// matches one of the items in this list will be collected and reported to Sentry.
    /// </para>
    /// <para>
    /// These can be either custom Instruments that you have created or any of the built in metrics that are available.
    /// </para>
    /// <para>
    /// See https://learn.microsoft.com/en-us/dotnet/core/diagnostics/built-in-metrics for more information.
    /// </para>
    /// </summary>
    public IList<SubstringOrRegexPattern> CaptureSystemDiagnosticsInstruments
    {
        // NOTE: During configuration binding, .NET 6 and lower used to just call Add on the existing item.
        //       .NET 7 changed this to call the setter with an array that already starts with the old value.
        //       We have to handle both cases.
        get => _captureSystemDiagnosticsInstruments;
        set => _captureSystemDiagnosticsInstruments = value.WithConfigBinding();
    }

    private IList<SubstringOrRegexPattern> _captureSystemDiagnosticsMeters = BuiltInSystemDiagnosticsMeters.All;

    /// <summary>
    /// <para>
    /// A list of Substrings or Regular Expressions. Instruments for any `System.Diagnostics.Metrics.Meter`
    /// whose name matches one of the items in this list will be collected and reported to Sentry.
    /// </para>
    /// <para>
    /// These can be either custom Instruments that you have created or any of the built in metrics that are available.
    /// </para>
    /// <para>
    /// See https://learn.microsoft.com/en-us/dotnet/core/diagnostics/built-in-metrics for more information.
    /// </para>
    /// </summary>
    public IList<SubstringOrRegexPattern> CaptureSystemDiagnosticsMeters
    {
        // NOTE: During configuration binding, .NET 6 and lower used to just call Add on the existing item.
        //       .NET 7 changed this to call the setter with an array that already starts with the old value.
        //       We have to handle both cases.
        get => _captureSystemDiagnosticsMeters;
        set => _captureSystemDiagnosticsMeters = value.WithConfigBinding();
    }
}
