using System;
using System.Collections.Generic;

namespace Sentry
{
    /// <summary>
    /// SDK API contract which combines a client and scope management.
    /// </summary>
    /// <remarks>
    /// The contract of which <see cref="T:Sentry.SentrySdk" /> exposes statically.
    /// This interface exist to allow better testability of integrations which otherwise
    /// would require dependency to the static <see cref="T:Sentry.SentrySdk" />.
    /// </remarks>
    /// <inheritdoc cref="ISentryClient" />
    /// <inheritdoc cref="ISentryScopeManager" />
    public interface IHub :
        ISentryClient,
        ISentryScopeManager
    {
        /// <summary>
        /// Last event id recorded in the current scope.
        /// </summary>
        SentryId LastEventId { get; }

        /// <summary>
        /// Starts a transaction.
        /// </summary>
        ITransaction StartTransaction(
            ITransactionContext context,
            IReadOnlyDictionary<string, object?> customSamplingContext
        );

        /// <summary>
        /// Binds specified exception the specified span.
        /// </summary>
        /// <remarks>
        /// This method is used internally and is not meant for public use.
        /// </remarks>
        void BindException(Exception exception, ISpan span);

        /// <summary>
        /// Gets the currently ongoing (not finished) span or <code>null</code> if none available.
        /// </summary>
        ISpan? GetSpan();

        /// <summary>
        /// Gets the Sentry trace header for the last active span.
        /// </summary>
        SentryTraceHeader? GetTraceHeader();

        /// <summary>
        /// Starts a new session, optionally with the provided unique identifier.
        /// Session identifier can be user ID, IP address, device MAC address, or any other similarly distinct value.
        /// </summary>
        void StartSession();

        /// <summary>
        /// Ends the currently active session.
        /// </summary>
        void EndSession(SessionEndStatus status = SessionEndStatus.Exited);
    }
}
