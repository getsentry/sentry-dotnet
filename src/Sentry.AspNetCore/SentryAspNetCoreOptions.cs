using Sentry.Extensions.Logging;

namespace Sentry.AspNetCore
{
    /// <summary>
    /// An options class for the ASP.NET Core Sentry integration
    /// </summary>
    /// <inheritdoc />
    public class SentryAspNetCoreOptions : SentryLoggingOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether to [include the request payload].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [the request payload shall be included in events]; otherwise, <c>false</c>.
        /// </value>
        public bool IncludeRequestPayload { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [include System.Diagnostic.Activity data] to events.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [include activity data]; otherwise, <c>false</c>.
        /// </value>
        /// <see cref="System.Diagnostics.Activity"/>
        /// <seealso href="https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/ActivityUserGuide.md"/>
        public bool IncludeActivityData { get; set; }
    }
}
