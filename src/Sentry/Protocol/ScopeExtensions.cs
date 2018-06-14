using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using Sentry.Infrastructure;
using Sentry.Internal;

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

        /// <summary>
        /// Adds a breadcrumb to the <see cref="Scope"/>
        /// </summary>
        /// <param name="scope">Scope</param>
        /// <param name="breadcrumb">The breadcrumb.</param>
        public static void AddBreadcrumb(this Scope scope, Breadcrumb breadcrumb)
        {
            var breadcrumbs = scope.Breadcrumbs;

            var overflow = breadcrumbs.Count - (scope.Options?.MaxBreadcrumbs
                                                ?? Constants.DefaultMaxBreadcrumbs) + 1;
            if (overflow > 0)
            {
                breadcrumbs = breadcrumbs.RemoveRange(0, overflow);
            }

            scope.Breadcrumbs = breadcrumbs.Add(breadcrumb);
        }

        private static IImmutableDictionary<string, string> ToImmutableDictionary(
            this (string name, string value) tuple)
            => ImmutableDictionary<string, string>.Empty
                .Add(tuple.name, tuple.value);

        // shallow copy from this scope over to the parameter
        // Will override the value on 'to' when the value exists on 'from'
        public static void CopyTo(this Scope from, Scope to)
        {
            if (from.InternalFingerprint != null)
            {
                to.Fingerprint = to.Fingerprint?.AddRange(from.InternalFingerprint) ?? from.InternalFingerprint;
            }
            if (from.InternalBreadcrumbs != null)
            {
                to.Breadcrumbs = to.Breadcrumbs?.AddRange(from.InternalBreadcrumbs) ?? from.InternalBreadcrumbs;
            }
            if (from.InternalExtra != null)
            {
                to.Extra = to.Extra?.AddRange(from.InternalExtra) ?? from.InternalExtra;
            }
            if (from.InternalTags != null)
            {
                to.Tags = to.Tags?.AddRange(from.InternalTags) ?? from.InternalTags;
            }
            if (from.InternalContexts != null)
            {
                to.Contexts = from.InternalContexts;
            }
            if (from.InternalUser != null)
            {
                to.User = from.InternalUser;
            }
            if (from.Environment != null)
            {
                to.Environment = from.Environment;
            }
            if (from.Sdk != null)
            {
                to.Sdk = from.Sdk;
            }
        }

        // Applies the 'state' into the scope
        public static void Apply(this Scope scope, object state)
        {
            switch (state)
            {
                case string scopeString:
                    scope.SetTag(scopeString, string.Empty);
                    break;
                case IEnumerable<KeyValuePair<string, string>> keyValStringString:
                    scope.SetTags(keyValStringString);
                    break;
                case IEnumerable<KeyValuePair<string, object>> keyValStringObject:
                    {
                        scope.SetTags(keyValStringObject
                            .Select(k => new KeyValuePair<string, string>(
                                k.Key,
                                k.Value?.ToString() ?? string.Empty)));
                        break;
                    }
                case ValueTuple<string, string> tupleStringString:
                    scope.SetTag(tupleStringString.Item1, tupleStringString.Item2 ?? string.Empty);
                    break;
                default:
                    scope.SetExtra(state.ToString(), string.Empty);
                    break;
            }
        }
    }
}
