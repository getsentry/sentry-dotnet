using System;

namespace Sentry.Protocol
{
    /// <summary>
    /// Options used by <see cref="IScope"/>.
    /// </summary>
    public interface IScopeOptions
    {
        /// <summary>
        /// Configured scope processor.
        /// </summary>
        ISentryScopeStateProcessor SentryScopeStateProcessor { get; set; }

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
        int MaxBreadcrumbs { get; }

        /// <summary>
        /// Invoked before storing a new breadcrumb.
        /// </summary>
        /// <remarks>
        /// Allows the callback handler access to a breadcrumb and allows modification
        /// or totally dropping the breadcrumb by returning null.
        /// </remarks>
        Func<Breadcrumb, Breadcrumb?>? BeforeBreadcrumb { get; }
    }
}
