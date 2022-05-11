using System.ComponentModel;

namespace Sentry.AspNetCore;

/// <summary>
/// Methods to extract ASP.NET Core specific data from <see cref="TransactionSamplingContext"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SamplingExtensions
{
    internal const string KeyForHttpMethod = "__HttpMethod";
    internal const string KeyForHttpRoute = "__HttpRoute";
    internal const string KeyForHttpPath = "__HttpPath";

    /// <summary>
    /// Gets the HTTP method associated with the transaction.
    /// May return null if the value has not been set by the integration.
    /// </summary>
    /// <remarks>
    /// This method extracts data from <see cref="TransactionSamplingContext.CustomSamplingContext"/>
    /// which is populated by Sentry's ASP.NET Core integration.
    /// </remarks>
    public static string? TryGetHttpMethod(this TransactionSamplingContext samplingContext) =>
        samplingContext.CustomSamplingContext.GetValueOrDefault(KeyForHttpMethod) as string;

    /// <summary>
    /// Gets the HTTP route associated with the transaction.
    /// May return null if the value has not been set by the integration.
    /// </summary>
    /// <remarks>
    /// This method extracts data from <see cref="TransactionSamplingContext.CustomSamplingContext"/>
    /// which is populated by Sentry's ASP.NET Core integration.
    /// </remarks>
    public static string? TryGetHttpRoute(this TransactionSamplingContext samplingContext) =>
        samplingContext.CustomSamplingContext.GetValueOrDefault(KeyForHttpRoute) as string;

    /// <summary>
    /// Gets the HTTP path associated with the transaction.
    /// May return null if the value has not been set by the integration.
    /// </summary>
    /// <remarks>
    /// This method extracts data from <see cref="TransactionSamplingContext.CustomSamplingContext"/>
    /// which is populated by Sentry's ASP.NET Core integration.
    /// </remarks>
    public static string? TryGetHttpPath(this TransactionSamplingContext samplingContext) =>
        samplingContext.CustomSamplingContext.GetValueOrDefault(KeyForHttpPath) as string;
}
