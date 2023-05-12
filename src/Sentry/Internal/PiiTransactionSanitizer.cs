namespace Sentry.Internal;

/// <summary>
/// Sanitizes data that potentially contains Personally Identifiable Information (PII) before sending it to Sentry.
/// </summary>
internal static class PiiTransactionSanitizer
{
    /// <summary>
    /// Redacts PII from the transaction description
    /// </summary>
    /// <param name="transaction">The transaction to be sanitized</param>
    /// <returns>The Transaction with redacted description</returns>
    public static Transaction Sanitize(this Transaction transaction)
    {
        transaction.Description = transaction.Description.Sanitize();
        return transaction;
    }
}
