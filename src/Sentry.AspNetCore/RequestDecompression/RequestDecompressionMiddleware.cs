// Adapted from: https://github.com/dotnet/aspnetcore/blob/c18e93a9a2e2949e1a9c880da16abf0837aa978f/src/Middleware/RequestDecompression/src/RequestDecompressionMiddleware.cs

// // Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.RequestDecompression;
using Microsoft.Extensions.Logging;

namespace Sentry.AspNetCore.RequestDecompression;

/// <summary>
/// Enables HTTP request decompression.
/// </summary>
internal sealed partial class RequestDecompressionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestDecompressionMiddleware> _logger;
    private readonly IRequestDecompressionProvider _provider;
    private readonly IHub _hub;

    /// <summary>
    /// Initialize the request decompression middleware.
    /// </summary>
    /// <param name="next">The delegate representing the remaining middleware in the request pipeline.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">The <see cref="IRequestDecompressionProvider"/>.</param>
    /// <param name="hub">The Sentry Hub</param>
    public RequestDecompressionMiddleware(
        RequestDelegate next,
        ILogger<RequestDecompressionMiddleware> logger,
        IRequestDecompressionProvider provider,
        IHub hub)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(hub);

        _next = next;
        _logger = logger;
        _provider = provider;
        _hub = hub;
    }

    /// <summary>
    /// Invoke the middleware.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <returns>A task that represents the execution of this middleware.</returns>
    public Task Invoke(HttpContext context)
    {
        Stream? decompressionStream = null;
        try
        {
            decompressionStream = _provider.GetDecompressionStream(context);
        }
        catch (Exception e)
        {
            HandleException(e);
        }
        return decompressionStream is null
            ? _next(context)
            : InvokeCore(context, decompressionStream);
    }

    private async Task InvokeCore(HttpContext context, Stream decompressionStream)
    {
        var request = context.Request.Body;
        try
        {
            try
            {
                var sizeLimit =
                    context.GetEndpoint()?.Metadata?.GetMetadata<IRequestSizeLimitMetadata>()?.MaxRequestBodySize
                        ?? context.Features.Get<IHttpMaxRequestBodySizeFeature>()?.MaxRequestBodySize;

                context.Request.Body = new SizeLimitedStream(decompressionStream, sizeLimit, static (long sizeLimit) => throw new BadHttpRequestException(
                        $"The decompressed request body is larger than the request body size limit {sizeLimit}.",
                        StatusCodes.Status413PayloadTooLarge));
            }
            catch (Exception e)
            {
                HandleException(e);
            }

            await _next(context).ConfigureAwait(false);
        }
        finally
        {
            context.Request.Body = request;
            await decompressionStream.DisposeAsync().ConfigureAwait(false);
        }
    }

    private void HandleException(Exception e)
    {
        const string description =
            "An exception was captured and then re-thrown, when attempting to decompress the request body." +
            "The web server likely returned a 5xx error code as a result of this exception.";
        e.SetSentryMechanism(nameof(RequestDecompressionMiddleware), description, handled: false);
        _hub.CaptureException(e);
        ExceptionDispatchInfo.Capture(e).Throw();
    }
}
