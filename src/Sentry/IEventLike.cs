namespace Sentry;

/// <summary>
/// Models members common between types that represent event-like data.
/// </summary>
public interface IEventLike : IHasTags, IHasExtra
{
    /// <summary>
    /// A trail of events which happened prior to an issue.
    /// </summary>
    /// <seealso href="https://docs.sentry.io/platforms/dotnet/enriching-events/breadcrumbs/"/>
    IReadOnlyCollection<Breadcrumb> Breadcrumbs { get; }

    /// <summary>
    /// Adds a breadcrumb.
    /// </summary>
    void AddBreadcrumb(Breadcrumb breadcrumb);

    /// <summary>
    /// The release distribution of the application.
    /// </summary>
    public string? Distribution { get; set; }

    /// <summary>
    /// Sentry level.
    /// </summary>
    SentryLevel? Level { get; set; }

    /// <summary>
    /// Gets or sets the HTTP.
    /// </summary>
    /// <value>
    /// The HTTP.
    /// </value>
    Request Request { get; set; }

    /// <summary>
    /// Gets the structured Sentry context.
    /// </summary>
    /// <value>
    /// The contexts.
    /// </value>
    Contexts Contexts { get; set; }

    /// <summary>
    /// Gets the user information.
    /// </summary>
    /// <value>
    /// The user.
    /// </value>
    SentryUser User { get; set; }

    /// <summary>
    /// The release version of the application.
    /// </summary>
    string? Release { get; set; }

    /// <summary>
    /// The environment name, such as 'production' or 'staging'.
    /// </summary>
    /// <remarks>Requires Sentry 8.0 or higher.</remarks>
    string? Environment { get; set; }

    /// <summary>
    /// The name of the transaction in which there was an event.
    /// </summary>
    /// <remarks>
    /// A transaction should only be defined when it can be well defined.
    /// On a Web framework, for example, a transaction is the route template
    /// rather than the actual request path. That is so GET /user/10 and /user/20
    /// (which have route template /user/{id}) are identified as the same transaction.
    /// </remarks>
    string? TransactionName { get; set; }

    /// <summary>
    /// SDK information.
    /// </summary>
    /// <remarks>New in Sentry version: 8.4</remarks>
    SdkVersion Sdk { get; }

    /// <summary>
    /// A list of strings used to dictate the deduplication of this event.
    /// </summary>
    /// <seealso href="https://docs.sentry.io/platforms/dotnet/data-management/event-grouping/grouping-enhancements/"/>
    /// <remarks>
    /// A value of {{ default }} will be replaced with the built-in behavior, thus allowing you to extend it, or completely replace it.
    /// New in version Protocol: version '7'
    /// </remarks>
    /// <example> { "fingerprint": ["myrpc", "POST", "/foo.bar"] } </example>
    /// <example> { "fingerprint": ["{{ default }}", "http://example.com/my.url"] } </example>
    IReadOnlyList<string> Fingerprint { get; set; }
}

/// <summary>
/// Extensions for <see cref="IEventLike"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class EventLikeExtensions
{
#if !NETFRAMEWORK
    /// <summary>
    /// Adds a breadcrumb to the object.
    /// </summary>
    /// <param name="eventLike">The object.</param>
    /// <param name="message">The message.</param>
    /// <param name="category">The category.</param>
    /// <param name="type">The type.</param>
    /// <param name="dataPair">The data key-value pair.</param>
    /// <param name="level">The level.</param>
    public static void AddBreadcrumb(
        this IEventLike eventLike,
        string message,
        string? category,
        string? type,
        (string, string)? dataPair = null,
        BreadcrumbLevel level = default)
    {
        Dictionary<string, string>? data = null;

        if (dataPair != null)
        {
            data = new Dictionary<string, string>
            {
                {dataPair.Value.Item1, dataPair.Value.Item2}
            };
        }

        eventLike.AddBreadcrumb(
            null,
            message,
            category,
            type,
            data,
            level);
    }
#endif

    /// <summary>
    /// Adds a breadcrumb to the object.
    /// </summary>
    /// <param name="eventLike">The object.</param>
    /// <param name="message">The message.</param>
    /// <param name="category">The category.</param>
    /// <param name="type">The type.</param>
    /// <param name="data">The data.</param>
    /// <param name="level">The level.</param>
    public static void AddBreadcrumb(
        this IEventLike eventLike,
        string message,
        string? category = null,
        string? type = null,
        IReadOnlyDictionary<string, string>? data = null,
        BreadcrumbLevel level = default)
    {
        eventLike.AddBreadcrumb(
            null,
            message,
            category,
            type,
            data,
            level);
    }

    /// <summary>
    /// Adds a breadcrumb to the object.
    /// </summary>
    /// <remarks>
    /// This overload is used for testing.
    /// </remarks>
    /// <param name="eventLike">The object.</param>
    /// <param name="timestamp">The timestamp</param>
    /// <param name="message">The message.</param>
    /// <param name="category">The category.</param>
    /// <param name="type">The type.</param>
    /// <param name="data">The data</param>
    /// <param name="level">The level.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void AddBreadcrumb(
        this IEventLike eventLike,
        DateTimeOffset? timestamp,
        string message,
        string? category = null,
        string? type = null,
        IReadOnlyDictionary<string, string>? data = null,
        BreadcrumbLevel level = default)
    {
        eventLike.AddBreadcrumb(new Breadcrumb(
            timestamp,
            message,
            type,
            data,
            category,
            level));
    }

    /// <summary>
    /// Whether a <see cref="SentryUser"/> has been set to the object with any of its fields non null.
    /// </summary>
    public static bool HasUser(this IEventLike eventLike) => eventLike.User.HasAnyData();

    /// <summary>
    /// Sets the fingerprint to the object.
    /// </summary>
    public static void SetFingerprint(this IEventLike eventLike, IEnumerable<string> fingerprint)
        => eventLike.Fingerprint = fingerprint as IReadOnlyList<string> ?? fingerprint.ToArray();

    /// <summary>
    /// Sets the fingerprint to the object.
    /// </summary>
    public static void SetFingerprint(this IEventLike eventLike, params string[] fingerprint)
        => eventLike.Fingerprint = fingerprint;
}
