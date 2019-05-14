using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Sentry.Protocol;

// ReSharper disable once CheckNamespace
namespace Sentry
{
    ///
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class BaseScopeExtensions
    {
        /// <summary>
        /// Whether a <see cref="User"/> has been set to the scope with any of its fields non null.
        /// </summary>
        /// <param name="scope"></param>
        /// <returns>True if a User was set to the scope. Otherwise, false.</returns>
        public static bool HasUser(this BaseScope scope)
            => scope.InternalUser != null
               && (scope.InternalUser.Email != null
                   || scope.InternalUser.Id != null
                   || scope.InternalUser.Username != null
                   || scope.InternalUser.InternalOther?.Count > 0
                   || scope.InternalUser.IpAddress != null);

#if HAS_VALUE_TUPLE
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
                    this BaseScope scope,
                    string message,
                    string category,
                    string type,
                    in (string, string)? dataPair = null,
                    BreadcrumbLevel level = default)
        {
            Dictionary<string, string> data = null;
            if (dataPair != null)
            {
                data = new Dictionary<string, string>
                {
                    {dataPair.Value.Item1, dataPair.Value.Item2}
                };
            }

            scope.AddBreadcrumb(
                timestamp: null,
                message: message,
                category: category,
                type: type,
                data: data,
                level: level);
        }
#endif

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
            this BaseScope scope,
            string message,
            string category = null,
            string type = null,
            Dictionary<string, string> data = null,
            BreadcrumbLevel level = default)
        {
            scope.AddBreadcrumb(
                timestamp: null,
                message: message,
                category: category,
                type: type,
                data: data,
                level: level);
        }

        /// <summary>
        /// Adds a breadcrumb to the scope
        /// </summary>
        /// <remarks>
        /// This overload is used for testing.
        /// </remarks>
        /// <param name="scope">The scope.</param>
        /// <param name="timestamp">The timestamp</param>
        /// <param name="message">The message.</param>
        /// <param name="category">The category.</param>
        /// <param name="type">The type.</param>
        /// <param name="data">The data</param>
        /// <param name="level">The level.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void AddBreadcrumb(
            this BaseScope scope,
            DateTimeOffset? timestamp,
            string message,
            string category = null,
            string type = null,
            IReadOnlyDictionary<string, string> data = null,
            BreadcrumbLevel level = default)
            => scope.AddBreadcrumb(new Breadcrumb(
                timestamp: timestamp,
                message: message,
                type: type,
                data: data,
                category: category,
                level: level));

        /// <summary>
        /// Adds a breadcrumb to the <see cref="BaseScope"/>
        /// </summary>
        /// <param name="scope">Scope</param>
        /// <param name="breadcrumb">The breadcrumb.</param>
        internal static void AddBreadcrumb(this BaseScope scope, Breadcrumb breadcrumb)
        {
            if (scope == null)
            {
                return;
            }

            if (scope.ScopeOptions?.BeforeBreadcrumb is Func<Breadcrumb, Breadcrumb> callback)
            {
                breadcrumb = callback(breadcrumb);

                if (breadcrumb == null)
                {
                    return;
                }
            }

            var breadcrumbs = (ConcurrentQueue<Breadcrumb>)scope.Breadcrumbs;

            var overflow = breadcrumbs.Count - (scope.ScopeOptions?.MaxBreadcrumbs
                                                ?? Constants.DefaultMaxBreadcrumbs) + 1;
            if (overflow > 0)
            {
                breadcrumbs.TryDequeue(out _);
            }

            breadcrumbs.Enqueue(breadcrumb);
        }

        /// <summary>
        /// Sets the fingerprint to the <see cref="BaseScope"/>
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <param name="fingerprint">The fingerprint.</param>
        public static void SetFingerprint(this BaseScope scope, IEnumerable<string> fingerprint)
            => scope.InternalFingerprint = fingerprint;

        /// <summary>
        /// Sets the extra key-value to the <see cref="BaseScope"/>
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public static void SetExtra(this BaseScope scope, string key, object value)
            => ((ConcurrentDictionary<string, object>)scope.Extra).AddOrUpdate(key, value, (s, o) => value);

        /// <summary>
        /// Sets the extra key-value pairs to the <see cref="BaseScope"/>
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <param name="values">The values.</param>
        public static void SetExtras(this BaseScope scope, IEnumerable<KeyValuePair<string, object>> values)
        {
            var extra = (ConcurrentDictionary<string, object>)scope.Extra;
            foreach (var keyValuePair in values)
            {
                extra.AddOrUpdate(keyValuePair.Key, keyValuePair.Value, (s, o) => keyValuePair.Value);
            }
        }

        /// <summary>
        /// Sets the tag to the <see cref="BaseScope"/>
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public static void SetTag(this BaseScope scope, string key, string value)
            => ((ConcurrentDictionary<string, string>)scope.Tags).AddOrUpdate(key, value, (s, o) => value);

        /// <summary>
        /// Set all items as tags
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <param name="tags"></param>
        public static void SetTags(this BaseScope scope, IEnumerable<KeyValuePair<string, string>> tags)
        {
            var internalTags = (ConcurrentDictionary<string, string>)scope.Tags;
            foreach (var keyValuePair in tags)
            {
                internalTags.AddOrUpdate(keyValuePair.Key, keyValuePair.Value, (s, o) => keyValuePair.Value);
            }
        }

        /// <summary>
        /// Removes a tag from the <see cref="BaseScope"/>
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <param name="key"></param>
        public static void UnsetTag(this BaseScope scope, string key)
            => scope.InternalTags?.TryRemove(key, out _);

        /// <summary>
        /// Applies the data from one scope to the other while
        /// </summary>
        /// <param name="from">The scope to data copy from.</param>
        /// <param name="to">The scope to copy data to.</param>
        /// <remarks>
        /// Applies the data of 'from' into 'to'.
        /// If data in 'from' is null, 'to' is unmodified.
        /// Conflicting keys are not overriden
        /// This is a shallow copy.
        /// </remarks>
        public static void Apply(this BaseScope from, BaseScope to)
        {
            if (from == null || to == null)
            {
                return;
            }

            // Fingerprint isn't combined. It's absolute.
            // One set explicitly on target (i.e: event)
            // takes precedence and is not overwritten
            if (to.InternalFingerprint == null
                && from.InternalFingerprint != null)
            {
                to.InternalFingerprint = from.InternalFingerprint;
            }

            if (from.InternalBreadcrumbs != null)
            {
                ((ConcurrentQueue<Breadcrumb>)to.Breadcrumbs).EnqueueAll(from.InternalBreadcrumbs);
            }

            if (from.InternalExtra != null)
            {
                foreach (var extra in from.Extra)
                {
                    ((ConcurrentDictionary<string, object>)to.Extra).TryAdd(extra.Key, extra.Value);
                }
            }

            if (from.InternalTags != null)
            {
                foreach (var tag in from.Tags)
                {
                    ((ConcurrentDictionary<string, string>)to.Tags).TryAdd(tag.Key, tag.Value);
                }
            }

            from.InternalContexts?.CopyTo(to.Contexts);
            from.InternalRequest?.CopyTo(to.Request);
            from.InternalUser?.CopyTo(to.User);

            if (to.Environment == null)
            {
                to.Environment = from.Environment;
            }

            if (to.Transaction == null)
            {
                to.Transaction = from.Transaction;
            }

            if (to.Level == null)
            {
                to.Level = from.Level;
            }

            if (from.Sdk != null)
            {
                if (from.Sdk.Name != null && from.Sdk.Version != null)
                {
                    to.Sdk.Name = from.Sdk.Name;
                    to.Sdk.Version = from.Sdk.Version;
                }

                if (from.Sdk.InternalPackages != null)
                {
                    foreach (var package in from.Sdk.InternalPackages)
                    {
                        to.Sdk.AddPackage(package);
                    }
                }
            }
        }

        /// <summary>
        /// Applies the state object into the scope
        /// </summary>
        /// <param name="scope">The scope to apply the data.</param>
        /// <param name="state">The state object to apply.</param>
        public static void Apply(this BaseScope scope, object state)
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
#if HAS_VALUE_TUPLE
                case ValueTuple<string, string> tupleStringString:
                    if (!string.IsNullOrEmpty(tupleStringString.Item2))
                    {
                        scope.SetTag(tupleStringString.Item1, tupleStringString.Item2);
                    }
                    break;
#endif
                default:
                    scope.SetExtra("state", state);
                    break;
            }
        }
    }
}
