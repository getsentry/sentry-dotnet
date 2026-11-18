namespace Sentry.Internal;

/// <summary>
/// Resolves the effective policy for each data collection category: from
/// <see cref="SentryOptions.DataCollection"/> when the application configured it, and from the legacy
/// <see cref="SentryOptions.SendDefaultPii"/> otherwise. Integrations read their policy from here rather than
/// from either option.
/// </summary>
/// <remarks>
/// Resolution is per query rather than a snapshot, because <see cref="SentryOptions"/> stays mutable after
/// <c>Init</c>.
/// </remarks>
internal sealed class DataCollectionResolver(SentryOptions options)
{
    private static readonly KeyValueCollectionBehavior DenyList = KeyValueCollectionBehavior.DenyList();
    private static readonly KeyValueCollectionBehavior Off = KeyValueCollectionBehavior.Off;

    private DataCollectionOptions? Configured => options.DataCollection;

    private bool SendDefaultPii => options.SendDefaultPii;

    public bool UserInfo => Configured?.UserInfo ?? SendDefaultPii;

    public bool DatabaseQueryData => Configured?.DatabaseQueryData ?? SendDefaultPii;

    public bool GraphQLDocument => Configured?.GraphQL.Document ?? SendDefaultPii;

    public bool GraphQLVariables => Configured?.GraphQL.Variables ?? SendDefaultPii;

    public bool GenAIInputs => Configured?.GenAI.Inputs ?? SendDefaultPii;

    public bool GenAIOutputs => Configured?.GenAI.Outputs ?? SendDefaultPii;

    public KeyValueCollectionBehavior Cookies =>
        Configured?.Cookies ?? (SendDefaultPii ? DenyList : Off);

    public KeyValueCollectionBehavior RequestHeaders => Configured?.HttpHeaders.Request ?? DenyList;

    public KeyValueCollectionBehavior ResponseHeaders => Configured?.HttpHeaders.Response ?? DenyList;

    public KeyValueCollectionBehavior UrlQueryParams => Configured?.UrlQueryParams ?? DenyList;

    public bool IncomingRequestBody => CollectsBody(HttpBodyTypes.IncomingRequest, legacyFallback: true);

    public bool OutgoingRequestBody => CollectsBody(HttpBodyTypes.OutgoingRequest, SendDefaultPii);

    public bool IncomingResponseBody => CollectsBody(HttpBodyTypes.IncomingResponse, SendDefaultPii);

    public bool OutgoingResponseBody => CollectsBody(HttpBodyTypes.OutgoingResponse, SendDefaultPii);

    private bool CollectsBody(HttpBodyTypes bodyType, bool legacyFallback) => Configured is { } configured
        ? (configured.HttpBodies & bodyType) == bodyType
        : legacyFallback;
}
