using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using Sentry.Infrastructure;

namespace Sentry.Protocol
{
    ///
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ScopeExtensions
    {
        /// <summary>
        /// Adds a breadcrumb to the scope
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <param name="message">The message.</param>
        /// <param name="type">The type.</param>
        /// <param name="category">The category.</param>
        /// <param name="dataPair">The data key-value pair.</param>
        /// <param name="level">The level.</param>
        public static void AddBreadcrumb(
                    this Scope scope,
                    string message,
                    string type,
                    string category,
                    (string, string)? dataPair = null,
                    BreadcrumbLevel level = default)
        {
            scope.AddBreadcrumb(
                clock: null,
                message: message,
                type: type,
                data: dataPair?.ToImmutableDictionary(),
                category: category,
                level: level);
        }

        /// <summary>
        /// Adds a breadcrumb to the scope.
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <param name="message">The message.</param>
        /// <param name="type">The type.</param>
        /// <param name="category">The category.</param>
        /// <param name="data">The data.</param>
        /// <param name="level">The level.</param>
        public static void AddBreadcrumb(
                    this Scope scope,
                    string message,
                    string type,
                    string category = null,
                    IDictionary<string, string> data = null,
                    BreadcrumbLevel level = default)
        {
            scope.AddBreadcrumb(
                clock: null,
                message: message,
                type: type,
                data: data?.ToImmutableDictionary(),
                category: category,
                level: level);
        }

        ///
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void AddBreadcrumb(
            this Scope scope,
            ISystemClock clock,
            string message,
            string type,
            string category = null,
            (string, string)? dataPair = null,
            BreadcrumbLevel level = default)
        {
            scope.AddBreadcrumb(
                clock: clock,
                message: message,
                type: type,
                data: dataPair?.ToImmutableDictionary(),
                category: category,
                level: level);
        }

        ///
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void AddBreadcrumb(
            this Scope scope,
            ISystemClock clock,
            string message,
            string type,
            string category = null,
            IImmutableDictionary<string, string> data = null,
            BreadcrumbLevel level = default)
        {
            scope.AddBreadcrumb(new Breadcrumb(
                timestamp: (clock ?? SystemClock.Clock).GetUtcNow(),
                message: message,
                type: type,
                data: data,
                category: category,
                level: level));
        }

        private static IImmutableDictionary<string, string> ToImmutableDictionary(
            this (string name, string value) tuple)
            => ImmutableDictionary<string, string>.Empty
                .Add(tuple.name, tuple.value);
    }
}
