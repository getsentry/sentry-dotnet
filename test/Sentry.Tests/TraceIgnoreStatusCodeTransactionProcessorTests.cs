using Sentry.Internal.OpenTelemetry;

namespace Sentry.Tests;

public class TraceIgnoreStatusCodeTransactionProcessorTests
{
    private static SentryOptions OptionsWithIgnoredCodes(params HttpStatusCodeRange[] ranges)
    {
        var options = new SentryOptions();
        foreach (var range in ranges)
        {
            options.TraceIgnoreStatusCodes.Add(range);
        }
        return options;
    }

    private static SentryTransaction TransactionWithStatusCode(int statusCode)
    {
        var transaction = new SentryTransaction("name", "operation");
        transaction.SetData(OtelSemanticConventions.AttributeHttpResponseStatusCode, statusCode);
        return transaction;
    }

    [Fact]
    public void Process_EmptyIgnoreList_ReturnsTransaction()
    {
        // Arrange
        var options = new SentryOptions();
        var processor = new TraceIgnoreStatusCodeTransactionProcessor(options);
        var transaction = TransactionWithStatusCode(404);

        // Act
        var result = processor.Process(transaction);

        // Assert
        result.Should().BeSameAs(transaction);
    }

    [Fact]
    public void Process_StatusCodeNotInIgnoreList_ReturnsTransaction()
    {
        // Arrange
        var options = OptionsWithIgnoredCodes(404);
        var processor = new TraceIgnoreStatusCodeTransactionProcessor(options);
        var transaction = TransactionWithStatusCode(200);

        // Act
        var result = processor.Process(transaction);

        // Assert
        result.Should().BeSameAs(transaction);
    }

    [Fact]
    public void Process_StatusCodeInIgnoreList_ReturnsNull()
    {
        // Arrange
        var options = OptionsWithIgnoredCodes(404);
        var processor = new TraceIgnoreStatusCodeTransactionProcessor(options);
        var transaction = TransactionWithStatusCode(404);

        // Act
        var result = processor.Process(transaction);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Process_StatusCodeInIgnoredRange_ReturnsNull()
    {
        // Arrange
        var options = OptionsWithIgnoredCodes((400, 499));
        var processor = new TraceIgnoreStatusCodeTransactionProcessor(options);
        var transaction = TransactionWithStatusCode(404);

        // Act
        var result = processor.Process(transaction);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Process_StatusCodeOutsideIgnoredRange_ReturnsTransaction()
    {
        // Arrange
        var options = OptionsWithIgnoredCodes((400, 499));
        var processor = new TraceIgnoreStatusCodeTransactionProcessor(options);
        var transaction = TransactionWithStatusCode(500);

        // Act
        var result = processor.Process(transaction);

        // Assert
        result.Should().BeSameAs(transaction);
    }

    [Fact]
    public void Process_NoStatusCodeExtra_ReturnsTransaction()
    {
        // Arrange
        var options = OptionsWithIgnoredCodes(404);
        var processor = new TraceIgnoreStatusCodeTransactionProcessor(options);
        var transaction = new SentryTransaction("name", "operation");

        // Act
        var result = processor.Process(transaction);

        // Assert
        result.Should().BeSameAs(transaction);
    }

    [Fact]
    public void Process_MultipleIgnoredCodes_MatchesAny()
    {
        // Arrange
        var options = OptionsWithIgnoredCodes(404, 429);
        var processor = new TraceIgnoreStatusCodeTransactionProcessor(options);

        // Act & Assert
        processor.Process(TransactionWithStatusCode(404)).Should().BeNull();
        processor.Process(TransactionWithStatusCode(429)).Should().BeNull();
        processor.Process(TransactionWithStatusCode(200)).Should().NotBeNull();
    }
}
