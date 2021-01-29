using System;
using System.Collections.Generic;
using System.ComponentModel;
using Sentry.Protocol;

namespace Sentry
{
    /// <summary>
    /// Implemented by objects that contain breadcrumbs.
    /// </summary>
    public interface IHasBreadcrumbs
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
    }

    /// <summary>
    /// Extensions for <see cref="IHasBreadcrumbs"/>.
    /// </summary>
    public static class HasBreadcrumbsExtensions
    {
#if HAS_VALUE_TUPLE
        /// <summary>
        /// Adds a breadcrumb to the object.
        /// </summary>
        /// <param name="hasBreadcrumbs">The object.</param>
        /// <param name="message">The message.</param>
        /// <param name="category">The category.</param>
        /// <param name="type">The type.</param>
        /// <param name="dataPair">The data key-value pair.</param>
        /// <param name="level">The level.</param>
        public static void AddBreadcrumb(
            this IHasBreadcrumbs hasBreadcrumbs,
            string message,
            string? category,
            string? type,
            (string, string)? dataPair = null,
            BreadcrumbLevel level = default)
        {
            // Not to throw on code that ignores nullability warnings.
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (hasBreadcrumbs is null)
            {
                return;
            }

            Dictionary<string, string>? data = null;

            if (dataPair != null)
            {
                data = new Dictionary<string, string>
                {
                    {dataPair.Value.Item1, dataPair.Value.Item2}
                };
            }

            hasBreadcrumbs.AddBreadcrumb(
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
        /// <param name="hasBreadcrumbs">The object.</param>
        /// <param name="message">The message.</param>
        /// <param name="category">The category.</param>
        /// <param name="type">The type.</param>
        /// <param name="data">The data.</param>
        /// <param name="level">The level.</param>
        public static void AddBreadcrumb(
            this IHasBreadcrumbs hasBreadcrumbs,
            string message,
            string? category = null,
            string? type = null,
            Dictionary<string, string>? data = null,
            BreadcrumbLevel level = default)
        {
            // Not to throw on code that ignores nullability warnings.
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (hasBreadcrumbs is null)
            {
                return;
            }

            hasBreadcrumbs.AddBreadcrumb(
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
        /// <param name="hasBreadcrumbs">The object.</param>
        /// <param name="timestamp">The timestamp</param>
        /// <param name="message">The message.</param>
        /// <param name="category">The category.</param>
        /// <param name="type">The type.</param>
        /// <param name="data">The data</param>
        /// <param name="level">The level.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void AddBreadcrumb(
            this IHasBreadcrumbs hasBreadcrumbs,
            DateTimeOffset? timestamp,
            string message,
            string? category = null,
            string? type = null,
            IReadOnlyDictionary<string, string>? data = null,
            BreadcrumbLevel level = default)
        {
            // Not to throw on code that ignores nullability warnings.
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (hasBreadcrumbs is null)
            {
                return;
            }

            hasBreadcrumbs.AddBreadcrumb(new Breadcrumb(
                timestamp,
                message,
                type,
                data,
                category,
                level));
        }
    }
}
