using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using Sentry.Infrastructure;
using Sentry.Internal;
using Sentry.Protocol;

// ReSharper disable once CheckNamespace
namespace Sentry
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
                    string category,
                    string type,
                    (string, string)? dataPair = null,
                    BreadcrumbLevel level = default)
        {
            scope.AddBreadcrumb(
                clock: null,
                message: message,
                category: category,
                type: type,
                data: dataPair?.ToImmutableDictionary(),
                level: level);
        }

        /// <summary>
        /// Adds a breadcrumb to the scope.
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <param name="message">The message.</param>
        /// <param name="category">The category.</param>
        /// <param name="type">The type.</param>
        /// <param name="data">The data.</param>
        /// <param name="level">The level.</param>
        public static void AddBreadcrumb(
                    this Scope scope,
                    string message,
                    string category = null,
                    string type = null,
                    IDictionary<string, string> data = null,
                    BreadcrumbLevel level = default)
        {
            scope.AddBreadcrumb(
                clock: null,
                message: message,
                category: category,
                type: type,
                data: data?.ToImmutableDictionary(),
                level: level);
        }

        /// <summary>
        /// Adds a breadcrumb to the scope
        /// </summary>
        /// <remarks>
        /// This overload is used for testing.
        /// </remarks>
        /// <param name="scope">The scope.</param>
        /// <param name="clock">The clock which controls timestamps</param>
        /// <param name="message">The message.</param>
        /// <param name="category">The category.</param>
        /// <param name="type">The type.</param>
        /// <param name="data">The data</param>
        /// <param name="level">The level.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void AddBreadcrumb(this Scope scope,
            ISystemClock clock,
            string message,
            string category = null,
            string type = null,
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
        internal static void AddBreadcrumb(this Scope scope, Breadcrumb breadcrumb)
        {
            var breadcrumbs = scope.InternalBreadcrumbs ?? ImmutableList<Breadcrumb>.Empty;

            var overflow = breadcrumbs.Count - (scope.Options?.MaxBreadcrumbs
                                                ?? Constants.DefaultMaxBreadcrumbs) + 1;
            if (overflow > 0)
            {
                breadcrumbs = breadcrumbs.RemoveRange(0, overflow);
            }

            scope.InternalBreadcrumbs = breadcrumbs.Add(breadcrumb);
        }

        private static IImmutableDictionary<string, string> ToImmutableDictionary(
            this (string name, string value) tuple)
            => ImmutableDictionary<string, string>.Empty
                .SetItem(tuple.name, tuple.value);

        /// <summary>
        /// Sets the fingerprint to the <see cref="Scope"/>
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <param name="fingerprint">The fingerprint.</param>
        public static void SetFingerprint(this Scope scope, IEnumerable<string> fingerprint)
            => scope.InternalFingerprint = fingerprint?.ToImmutableList();

        /// <summary>
        /// Sets the extra key-value to the <see cref="Scope"/>
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public static void SetExtra(this Scope scope, string key, object value)
            => scope.InternalExtra = (scope.InternalExtra ?? ImmutableDictionary<string, object>.Empty).SetItem(key, value);

        /// <summary>
        /// Sets the extra key-value pairs to the <see cref="Scope"/>
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <param name="values">The values.</param>
        public static void SetExtras(this Scope scope, IEnumerable<KeyValuePair<string, object>> values)
            => scope.InternalExtra = (scope.InternalExtra ?? ImmutableDictionary<string, object>.Empty).SetItems(values);

        /// <summary>
        /// Sets the tag to the <see cref="Scope"/>
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public static void SetTag(this Scope scope, string key, string value)
            => scope.InternalTags = (scope.InternalTags ?? ImmutableDictionary<string, string>.Empty).SetItem(key, value);

        /// <summary>
        /// Set all items as tags
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <param name="tags"></param>
        public static void SetTags(this Scope scope, IEnumerable<KeyValuePair<string, string>> tags)
            => scope.InternalTags = (scope.InternalTags ?? ImmutableDictionary<string, string>.Empty).SetItems(tags);

        /// <summary>
        /// Removes a tag from the <see cref="Scope"/>
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <param name="key"></param>
        public static void UnsetTag(this Scope scope, string key)
            => scope.InternalTags = scope.InternalTags?.Remove(key);

        /// <summary>
        /// Copy the data from one scope to the other
        /// </summary>
        /// <param name="from">The scope to data copy from.</param>
        /// <param name="to">The scope to copy data to.</param>
        /// <remarks>
        /// Will override the value on 'to' when the value exists on 'from'.
        /// If 'from' is null, 'to' is unmodified.
        /// This is a shallow copy.
        /// </remarks>
        public static void CopyTo(this Scope from, Scope to)
        {
            // Fingerprint isn't combined. It's absolute.
            // One set explicitly on target (i.e: event)
            // takes precedence and is not overwriten
            if (to.InternalFingerprint == null
                && from.InternalFingerprint != null)
            {
                to.InternalFingerprint = from.InternalFingerprint;
            }

            if (from.InternalBreadcrumbs != null)
            {
                to.InternalBreadcrumbs = to.InternalBreadcrumbs != null
                    ? to.InternalBreadcrumbs.AddRange(from.InternalBreadcrumbs)
                    : from.InternalBreadcrumbs;
            }

            if (from.InternalExtra != null)
            {
                to.InternalExtra = to.InternalExtra != null
                    ? from.InternalExtra.SetItems(to.InternalExtra)
                    : from.InternalExtra;
            }

            if (from.InternalTags != null)
            {
                to.InternalTags = to.InternalTags != null
                    ? from.InternalTags.SetItems(to.InternalTags)
                    : from.InternalTags;
            }

            if (from.InternalContexts != null)
            {
                to.Contexts = from.InternalContexts;
            }

            if (from.InternalRequest != null)
            {
                to.Request = from.InternalRequest;
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
                if (from.Sdk.Name != null && from.Sdk.Version != null)
                {
                    to.Sdk.Name = from.Sdk.Name;
                    to.Sdk.Version = from.Sdk.Version;
                }

                if (from.Sdk.InternalIntegrations != null)
                {
                    to.Sdk.InternalIntegrations =
                        to.Sdk.InternalIntegrations?.AddRange(from.Sdk.InternalIntegrations)
                        ?? from.Sdk.InternalIntegrations;
                }
            }
        }

        /// <summary>
        /// Applies the state object into the scope
        /// </summary>
        /// <param name="scope">The scope to apply the data.</param>
        /// <param name="state">The state object to apply.</param>
        public static void Apply(this Scope scope, object state)
        {
            switch (state)
            {
                case string scopeString:
                    // TODO: find unique key to support multiple single-string scopes
                    scope.SetTag("scope", scopeString);
                    break;
                case IEnumerable<KeyValuePair<string, string>> keyValStringString:
                    scope.SetTags(keyValStringString
                        .Where(kv => !string.IsNullOrEmpty(kv.Value)));
                    break;
                case IEnumerable<KeyValuePair<string, object>> keyValStringObject:
                    {
                        scope.SetTags(keyValStringObject
                            .Select(k => new KeyValuePair<string, string>(
                                k.Key,
                                k.Value?.ToString()))
                            .Where(kv => !string.IsNullOrEmpty(kv.Value)));

                        break;
                    }
                case ValueTuple<string, string> tupleStringString:
                    if (!string.IsNullOrEmpty(tupleStringString.Item2))
                    {
                        scope.SetTag(tupleStringString.Item1, tupleStringString.Item2);
                    }
                    break;
                default:
                    scope.SetExtra("state", state);
                    break;
            }
        }
    }
}
