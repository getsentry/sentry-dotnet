using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Sentry.Extensibility;
using System.Linq;
using Sentry.Internal;
using Sentry.Protocol;

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
        /// <param name="eventLike"></param>
        /// <returns>True if a User was set to the scope. Otherwise, false.</returns>
        public static bool HasUser(this IEventLike eventLike)
            => eventLike.User.Email is not null
               || eventLike.User.Id is not null
               || eventLike.User.Username is not null
               || eventLike.User.InternalOther?.Count > 0
               || eventLike.User.IpAddress is not null;

#if HAS_VALUE_TUPLE
        /// <summary>
        /// Adds a breadcrumb to the scope.
        /// </summary>
        /// <param name="eventLike">The scope.</param>
        /// <param name="message">The message.</param>
        /// <param name="category">The category.</param>
        /// <param name="type">The type.</param>
        /// <param name="dataPair">The data key-value pair.</param>
        /// <param name="level">The level.</param>
        public static void AddBreadcrumb(
            this IEventLike eventLike,
            string message,
            string? category,
            string? type,
            (string, string)? dataPair = null,
            BreadcrumbLevel level = default)
        {
            // Not to throw on code that ignores nullability warnings.
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (eventLike is null)
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

            eventLike.AddBreadcrumb(
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
        /// <param name="eventLike">The scope.</param>
        /// <param name="message">The message.</param>
        /// <param name="category">The category.</param>
        /// <param name="type">The type.</param>
        /// <param name="data">The data.</param>
        /// <param name="level">The level.</param>
        public static void AddBreadcrumb(
            this IEventLike eventLike,
            string message,
            string? category = null,
            string? type = null,
            Dictionary<string, string>? data = null,
            BreadcrumbLevel level = default)
        {
            // Not to throw on code that ignores nullability warnings.
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (eventLike is null)
            {
                return;
            }

            eventLike.AddBreadcrumb(
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
        /// <param name="eventLike">The scope.</param>
        /// <param name="timestamp">The timestamp</param>
        /// <param name="message">The message.</param>
        /// <param name="category">The category.</param>
        /// <param name="type">The type.</param>
        /// <param name="data">The data</param>
        /// <param name="level">The level.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void AddBreadcrumb(
            this IEventLike eventLike,
            DateTimeOffset? timestamp,
            string message,
            string? category = null,
            string? type = null,
            IReadOnlyDictionary<string, string>? data = null,
            BreadcrumbLevel level = default)
        {
            // Not to throw on code that ignores nullability warnings.
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (eventLike is null)
            {
                return;
            }

            eventLike.AddBreadcrumb(new Breadcrumb(
                timestamp,
                message,
                type,
                data,
                category,
                level));
        }

        /// <summary>
        /// Sets the fingerprint to the <see cref="Scope"/>.
        /// </summary>
        /// <param name="eventLike">The scope.</param>
        /// <param name="fingerprint">The fingerprint.</param>
        public static void SetFingerprint(this IEventLike eventLike, IEnumerable<string> fingerprint)
            => eventLike.Fingerprint = fingerprint as IReadOnlyList<string> ?? fingerprint.ToArray();

        /// <summary>
        /// Sets the extra key-value pairs to the <see cref="Scope"/>.
        /// </summary>
        /// <param name="eventLike">The scope.</param>
        /// <param name="values">The values.</param>
        public static void SetExtras(this IEventLike eventLike, IEnumerable<KeyValuePair<string, object?>> values)
        {
            foreach (var (key, value) in values)
            {
                eventLike.SetExtra(key, value);
            }
        }

        /// <summary>
        /// Set all items as tags.
        /// </summary>
        /// <param name="eventLike">The scope.</param>
        /// <param name="tags"></param>
        public static void SetTags(this IEventLike eventLike, IEnumerable<KeyValuePair<string, string>> tags)
        {
            foreach (var (key, value) in tags)
            {
                eventLike.SetTag(key, value);
            }
        }

        /// <summary>
        /// Removes a tag from the <see cref="Scope"/>.
        /// </summary>
        /// <param name="eventLike">The scope.</param>
        /// <param name="key"></param>
        public static void UnsetTag(this IEventLike eventLike, string key)
        {
            if (eventLike.Tags is IDictionary<string, string> tags)
            {
                tags.Remove(key);
            }
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

        /// <summary>
        /// Adds an attachment.
        /// </summary>
        public static void AddAttachment(
            this Scope scope,
            Stream stream,
            string fileName,
            AttachmentType type = AttachmentType.Default,
            long? length = null,
            string? contentType = null) =>
            scope.AddAttachment(new Attachment(type, stream, fileName, length, contentType));

        /// <summary>
        /// Adds an attachment.
        /// </summary>
        public static void AddAttachment(
            this Scope scope,
            byte[] data,
            string fileName,
            AttachmentType type = AttachmentType.Default,
            string? contentType = null) =>
            scope.AddAttachment(new MemoryStream(data), fileName, type, data.Length, contentType);

        /// <summary>
        /// Adds an attachment.
        /// </summary>
        public static void AddAttachment(
            this Scope scope,
            string filePath,
            AttachmentType type = AttachmentType.Default,
            string? contentType = null)
        {
            var stream = File.OpenRead(filePath);
            var fileName = Path.GetFileName(filePath);

            scope.AddAttachment(stream, fileName, type, stream.Length, contentType);
        }
    }
}
