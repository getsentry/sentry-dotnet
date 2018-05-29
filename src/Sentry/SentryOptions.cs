using System;

namespace Sentry
{
    /// TODO: the SDK options
    public class SentryOptions : IScopeOptions
    {
        /// 
        public bool CompressPayload { get; set; } = true;
        /// 
        public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(3);

        /// <summary>
        /// Gets or sets the maximum breadcrumbs.
        /// </summary>
        /// <remarks>
        /// When the number of events reach this configuration value,
        /// older breadcrumbs start dropping to make room for new ones.
        /// </remarks>
        /// <value>
        /// The maximum breadcrumbs per scope.
        /// </value>
        public int MaxBreadcrumbs { get; set; } = 100;
    }
}
