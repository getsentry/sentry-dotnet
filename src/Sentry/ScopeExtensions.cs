using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using Sentry.Extensibility;
using System.Linq;
using Sentry.Internal;
using Sentry.Protocol;
using Constants = Sentry.Protocol.Constants;

namespace Sentry
{
    /// <summary>
    /// Scope extensions.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ScopeExtensions
    {
        /// <summary>
        /// Whether a <see cref="Protocol.User"/> has been set to the scope with any of its fields non null.
        /// </summary>
        /// <param name="scope"></param>
        /// <returns>True if a User was set to the scope. Otherwise, false.</returns>
        public static bool HasUser(this IScope scope)
            => scope.User.Email != null
               || scope.User.Id != null
               || scope.User.Username != null
               || scope.User.InternalOther?.Count > 0
               || scope.User.IpAddress != null;

#if HAS_VALUE_TUPLE
        /// <summary>
        /// Adds a breadcrumb to the scope.
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <param name="message">The message.</param>
        /// <param name="category">The category.</param>
        /// <param name="type">The type.</param>
        /// <param name="dataPair">The data key-value pair.</param>
        /// <param name="level">The level.</param>
        public static void AddBreadcrumb(
            this IScope scope,
            string message,
            string? category,
            string? type,
            (string, string)? dataPair = null,
            BreadcrumbLevel level = default)
        {
            // Not to throw on code that ignores nullability warnings.
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (scope is null)
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

            scope.AddBreadcrumb(
                null,
                message,
                category,
                type,
                data,
                level);
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
            this IScope scope,
            string message,
            string? category = null,
            string? type = null,
            Dictionary<string, string>? data = null,
            BreadcrumbLevel level = default)
        {
            // Not to throw on code that ignores nullability warnings.
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (scope is null)
            {
                return;
            }

            scope.AddBreadcrumb(
                null,
                message,
                category,
                type,
                data,
                level);
        }

        /// <summary>
        /// Adds a breadcrumb to the scope.
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
            this IScope scope,
            DateTimeOffset? timestamp,
            string message,
            string? category = null,
            string? type = null,
            IReadOnlyDictionary<string, string>? data = null,
            BreadcrumbLevel level = default)
        {
            // Not to throw on code that ignores nullability warnings.
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (scope is null)
            {
                return;
            }

            scope.AddBreadcrumb(new Breadcrumb(
                timestamp,
                message,
                type,
                data,
                category,
                level));
        }

        /// <summary>
        /// Adds a breadcrumb to the <see cref="IScope"/>.
        /// </summary>
        /// <param name="scope">Scope.</param>
        /// <param name="breadcrumb">The breadcrumb.</param>
        internal static void AddBreadcrumb(this IScope scope, Breadcrumb breadcrumb)
        {
            if (scope.ScopeOptions?.BeforeBreadcrumb is { } beforeBreadcrumb)
            {
                if (beforeBreadcrumb(breadcrumb) is { } processedBreadcrumb)
                {
                    breadcrumb = processedBreadcrumb;
                }
                else
                {
                    // Callback returned null, which means the breadcrumb should be dropped
                    return;
                }
            }

            if (scope.Breadcrumbs is ICollection<Breadcrumb> breadcrumbsCollection)
            {
                var overflow = breadcrumbsCollection.Count -
                    (scope.ScopeOptions?.MaxBreadcrumbs ?? Constants.DefaultMaxBreadcrumbs) + 1;

                if (overflow > 0)
                {
                    if (scope.Breadcrumbs.FirstOrDefault() is { } first)
                    {
                        breadcrumbsCollection.Remove(first);
                    }
                }

                breadcrumbsCollection.Add(breadcrumb);
            }
            else if (scope.Breadcrumbs is ConcurrentQueue<Breadcrumb> breadcrumbsQueue)
            {
                var overflow = breadcrumbsQueue.Count -
                    (scope.ScopeOptions?.MaxBreadcrumbs ?? Constants.DefaultMaxBreadcrumbs) + 1;

                if (overflow > 0)
                {
                    breadcrumbsQueue.TryDequeue(out _);
                }

                breadcrumbsQueue.Enqueue(breadcrumb);
            }
        }

        /// <summary>
        /// Sets the fingerprint to the <see cref="IScope"/>.
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <param name="fingerprint">The fingerprint.</param>
        public static void SetFingerprint(this IScope scope, IEnumerable<string> fingerprint)
            => scope.Fingerprint = fingerprint;

        /// <summary>
        /// Sets the extra key-value to the <see cref="IScope"/>.
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public static void SetExtra(this IScope scope, string key, object? value)
        {
            if (scope.Extra is IDictionary<string, object?> extra)
            {
                extra[key] = value;
            }
        }

        /// <summary>
        /// Sets the extra key-value pairs to the <see cref="IScope"/>.
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <param name="values">The values.</param>
        public static void SetExtras(this IScope scope, IEnumerable<KeyValuePair<string, object?>> values)
        {
            foreach (var (key, value) in values)
            {
                scope.SetExtra(key, value);
            }
        }

        /// <summary>
        /// Sets the tag to the <see cref="IScope"/>.
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public static void SetTag(this IScope scope, string key, string value)
        {
            if (scope.Tags is IDictionary<string, string> tags)
            {
                tags[key] = value;
            }
        }

        /// <summary>
        /// Set all items as tags.
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <param name="tags"></param>
        public static void SetTags(this IScope scope, IEnumerable<KeyValuePair<string, string>> tags)
        {
            foreach (var (key, value) in tags)
            {
                scope.SetTag(key, value);
            }
        }

        /// <summary>
        /// Removes a tag from the <see cref="IScope"/>.
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <param name="key"></param>
        public static void UnsetTag(this IScope scope, string key)
        {
            if (scope.Tags is IDictionary<string, string> tags)
            {
                tags.Remove(key);
            }
        }

        /// <summary>
        /// Applies the data from one scope to the other.
        /// </summary>
        /// <param name="from">The scope to data copy from.</param>
        /// <param name="to">The scope to copy data to.</param>
        /// <remarks>
        /// Applies the data of 'from' into 'to'.
        /// If data in 'from' is null, 'to' is unmodified.
        /// Conflicting keys are not overriden.
        /// This is a shallow copy.
        /// </remarks>
        public static void Apply(this IScope from, IScope to)
        {
            // Not to throw on code that ignores nullability warnings.
            // ReSharper disable ConditionIsAlwaysTrueOrFalse
            if (from is null || to is null)
            // ReSharper enable ConditionIsAlwaysTrueOrFalse
            {
                return;
            }

            // Fingerprint isn't combined. It's absolute.
            // One set explicitly on target (i.e: event)
            // takes precedence and is not overwritten
            if (!to.Fingerprint.Any() && from.Fingerprint.Any())
            {
                to.Fingerprint = from.Fingerprint;
            }

            foreach (var breadcrumb in from.Breadcrumbs)
            {
                to.AddBreadcrumb(breadcrumb);
            }

            foreach (var (key, value) in from.Extra)
            {
                if (!to.Extra.ContainsKey(key))
                {
                    to.SetExtra(key, value);
                }
            }

            foreach (var (key, value) in from.Tags)
            {
                if (!to.Tags.ContainsKey(key))
                {
                    to.SetTag(key, value);
                }
            }

            from.Contexts.CopyTo(to.Contexts);
            from.Request.CopyTo(to.Request);
            from.User.CopyTo(to.User);

            to.Environment ??= from.Environment;
            to.Transaction ??= from.Transaction;
            to.Level ??= from.Level;

            if (from.Sdk is null || to.Sdk is null)
            {
                return;
            }

            if (from.Sdk.Name != null && from.Sdk.Version != null)
            {
                to.Sdk.Name = from.Sdk.Name;
                to.Sdk.Version = from.Sdk.Version;
            }

            if (from.Sdk.InternalPackages is { })
            {
                foreach (var package in from.Sdk.InternalPackages)
                {
                    to.Sdk.AddPackage(package);
                }
            }
        }

        /// <summary>
        /// Applies the state object into the scope.
        /// </summary>
        /// <param name="scope">The scope to apply the data.</param>
        /// <param name="state">The state object to apply.</param>
        public static void Apply(this IScope scope, object state)
        {
            var processor = scope.ScopeOptions?.SentryScopeStateProcessor ?? new DefaultSentryScopeStateProcessor();
            processor.Apply(scope, state);
        }

        /// <summary>
        /// Invokes all event processor providers available.
        /// </summary>
        /// <param name="scope">The Scope which holds the processor providers.</param>
        public static IEnumerable<ISentryEventProcessor> GetAllEventProcessors(this Scope scope)
        {
            foreach (var processor in scope.Options.GetAllEventProcessors())
            {
                yield return processor;
            }

            foreach (var processor in scope.EventProcessors)
            {
                yield return processor;
            }
        }

        /// <summary>
        /// Invokes all exception processor providers available.
        /// </summary>
        /// <param name="scope">The Scope which holds the processor providers.</param>
        public static IEnumerable<ISentryEventExceptionProcessor> GetAllExceptionProcessors(this Scope scope)
        {
            foreach (var processor in scope.Options.GetAllExceptionProcessors())
            {
                yield return processor;
            }

            foreach (var processor in scope.ExceptionProcessors)
            {
                yield return processor;
            }
        }

        /// <summary>
        /// Add an exception processor.
        /// </summary>
        /// <param name="scope">The Scope to hold the processor.</param>
        /// <param name="processor">The exception processor.</param>
        public static void AddExceptionProcessor(this Scope scope, ISentryEventExceptionProcessor processor)
            => scope.ExceptionProcessors.Add(processor);

        /// <summary>
        /// Add the exception processors.
        /// </summary>
        /// <param name="scope">The Scope to hold the processor.</param>
        /// <param name="processors">The exception processors.</param>
        public static void AddExceptionProcessors(this Scope scope, IEnumerable<ISentryEventExceptionProcessor> processors)
        {
            foreach (var processor in processors)
            {
                scope.ExceptionProcessors.Add(processor);
            }
        }

        /// <summary>
        /// Adds an event processor which is invoked when creating a <see cref="SentryEvent"/>.
        /// </summary>
        /// <param name="scope">The Scope to hold the processor.</param>
        /// <param name="processor">The event processor.</param>
        public static void AddEventProcessor(this Scope scope, ISentryEventProcessor processor)
            => scope.EventProcessors.Add(processor);

        /// <summary>
        /// Adds an event processor which is invoked when creating a <see cref="SentryEvent"/>.
        /// </summary>
        /// <param name="scope">The Scope to hold the processor.</param>
        /// <param name="processor">The event processor.</param>
        public static void AddEventProcessor(this Scope scope, Func<SentryEvent, SentryEvent> processor)
            => scope.AddEventProcessor(new DelegateEventProcessor(processor));

        /// <summary>
        /// Adds event processors which are invoked when creating a <see cref="SentryEvent"/>.
        /// </summary>
        /// <param name="scope">The Scope to hold the processor.</param>
        /// <param name="processors">The event processors.</param>
        public static void AddEventProcessors(this Scope scope, IEnumerable<ISentryEventProcessor> processors)
        {
            foreach (var processor in processors)
            {
                scope.EventProcessors.Add(processor);
            }
        }
    }
}
