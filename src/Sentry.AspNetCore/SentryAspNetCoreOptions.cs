using System;
using Sentry.Extensibility;
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
        [Obsolete("Use MaxRequestBodySize instead.")]
        public bool IncludeRequestPayload
        {
            get => MaxRequestBodySize != RequestSize.None;
            // As originally there was no truncation, setting to Large.
            set => MaxRequestBodySize = value ? RequestSize.Always : RequestSize.None;
        }

        /// <summary>
        /// Gets or sets a value indicating whether [include System.Diagnostic.Activity data] to events.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [include activity data]; otherwise, <c>false</c>.
        /// </value>
        /// <see cref="System.Diagnostics.Activity"/>
        /// <seealso href="https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/ActivityUserGuide.md"/>
        public bool IncludeActivityData { get; set; }

        /// <summary>
        /// Controls the size of the request body to extract if any.
        /// </summary>
        /// <remarks>
        /// No truncation is done by the SDK.
        /// If the request body is larger than the accepted size, nothing is sent.
        /// </remarks>
        public RequestSize MaxRequestBodySize { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="SentryAspNetCoreOptions"/>.
        /// </summary>
        public SentryAspNetCoreOptions()
        {
            // Don't report Environment.UserName as the user.
            IsEnvironmentUser = false;
        }
    }
}
