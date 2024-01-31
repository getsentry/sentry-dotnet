namespace Sentry;

// `unknown` is omitted since: Value set by Relay for legacy SDKs. SDKs must not set this value explicitly.
/// <summary>
/// Transaction source.
/// This information is required by dynamic sampling. Contains information about how the name
/// of the transaction was determined. This will be used by the server to decide whether or not
/// to scrub identifiers from the transaction name, or replace the entire name with a placeholder.
/// The source should only be set by integrations and not by developers directly
/// https://develop.sentry.dev/sdk/event-payloads/transaction/#transaction-annotations
/// </summary>
public enum TransactionNameSource
{
    /// <summary>
    /// User-defined name.
    /// </summary>
    /// <example>
    ///   <list type="bullet">
    ///     <item>my_transaction</item>
    ///   </list>
    /// </example>
    Custom,
    /// <summary>
    /// Raw URL, potentially containing identifiers.
    /// </summary>
    /// <example>
    ///   <list type="bullet">
    ///     <item>/auth/login/john123/</item>
    ///     <item>GET /auth/login/john123/</item>
    ///   </list>
    /// </example>
    Url,
    /// <summary>
    /// Parametrized URL / route
    /// </summary>
    /// <example>
    ///   <list type="bullet">
    ///     <item>/auth/login/:userId/</item>
    ///     <item>GET /auth/login/{user}/</item>
    ///   </list>
    /// </example>
    Route,
    /// <summary>
    /// Name of the view handling the request.
    /// </summary>
    /// <example>
    ///   <list type="bullet">
    ///     <item>UserListView</item>
    ///   </list>
    /// </example>
    View,
    /// <summary>
    /// Named after a software component, such as a function or class name.
    /// </summary>
    /// <example>
    ///   <list type="bullet">
    ///     <item>AuthLogin.login</item>
    ///     <item>LoginActivity.login_button</item>
    ///   </list>
    /// </example>
    Component,
    /// <summary>
    /// Name of a background task
    /// </summary>
    /// <example>
    ///   <list type="bullet">
    ///     <item>sentry.tasks.do_something</item>
    ///   </list>
    /// </example>
    Task
}

internal static class TransactionNameSourceExtensions
{
    /// <summary>
    /// Determines if the <paramref name="transactionNameSource"/> is considered "high quality"
    /// for purposes of dynamic sampling.
    /// </summary>
    /// <remarks>
    /// Currently, only <see cref="TransactionNameSource.Url"/> is considered low quality,
    /// and the others are high quality, but this may change in the future.
    /// </remarks>
    /// <seealso href="https://develop.sentry.dev/sdk/performance/dynamic-sampling-context/#note-on-low-quality-transaction-names"/>
    public static bool IsHighQuality(this TransactionNameSource transactionNameSource) =>
        transactionNameSource != TransactionNameSource.Url;
}
