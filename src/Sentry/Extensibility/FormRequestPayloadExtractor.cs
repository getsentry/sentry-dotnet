namespace Sentry.Extensibility;

/// <summary>
/// Form based request extractor.
/// </summary>
public class FormRequestPayloadExtractor : BaseRequestPayloadExtractor
{
    private const string SupportedContentType = "application/x-www-form-urlencoded";

    /// <summary>
    /// Supports <see cref="IHttpRequest"/> with content type application/x-www-form-urlencoded.
    /// </summary>
    protected override bool IsSupported(IHttpRequest request) =>
        SupportedContentType.Equals(request.ContentType, StringComparison.InvariantCulture)
        || (request.ContentType?.StartsWith($"{SupportedContentType};", StringComparison.InvariantCulture) == true);

    /// <summary>
    /// Extracts the request form data as a dictionary.
    /// </summary>
    protected override object? DoExtractPayLoad(IHttpRequest request)
        => request.Form?.ToDictionary(k => k.Key, v => v.Value);
}
