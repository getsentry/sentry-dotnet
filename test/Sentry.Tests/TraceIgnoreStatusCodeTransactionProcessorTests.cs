using Sentry.Internal.OpenTelemetry;

namespace Sentry.Tests;

public class TraceIgnoreStatusCodeTransactionProcessorTests
{
    private static SentryOptions OptionsWithIgnoredCodes(params HttpStatusCodeRange[] ranges) =>
        new() { TraceIgnoreStatusCodes = [.. ranges] };

    private static SentryTransaction TransactionWithStatusCode(int statusCode)
    {
        var transaction = new SentryTransaction("name", "operation");
        transaction.SetData(OtelSemanticConventions.AttributeHttpResponseStatusCode, statusCode);
        return transaction;
    }

    [Theory]
    [InlineData(301)]
    [InlineData(305)]
    [InlineData(307)]
    [InlineData(308)]
    [InlineData(399)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(404)]
    public void Process_DefaultOptions_DropsIgnoredStatusCode(int statusCode)
    {
        // Arrange
        var processor = new TraceIgnoreStatusCodeTransactionProcessor(new SentryOptions());
        var transaction = TransactionWithStatusCode(statusCode);

        // Act
        var result = processor.Process(transaction);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(200)]
    [InlineData(300)]
    [InlineData(302)]
    [InlineData(303)]
    [InlineData(304)]
    [InlineData(400)]
    [InlineData(405)]
    [InlineData(500)]
    public void Process_DefaultOptions_ReturnsTransaction(int statusCode)
    {
        // Arrange
        var processor = new TraceIgnoreStatusCodeTransactionProcessor(new SentryOptions());
        var transaction = TransactionWithStatusCode(statusCode);

        // Act
        var result = processor.Process(transaction);

        // Assert
        result.Should().BeSameAs(transaction);
    }

    [Fact]
    public void Process_EmptyIgnoreList_ReturnsTransaction()
    {
        // Arrange
        var options = OptionsWithIgnoredCodes();
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
        var options = OptionsWithIgnoredCodes((100, 599));
        var processor = new TraceIgnoreStatusCodeTransactionProcessor(options);
        var transaction = new SentryTransaction("name", "operation");

        // Act
        var result = processor.Process(transaction);

        // Assert
        result.Should().BeSameAs(transaction);
    }

    [Fact]
    public void Process_StatusCodeStoredAsShort_IsDropped()
    {
        // Regression test: OTel stores the status code as short, not int.
        // Arrange
        var options = OptionsWithIgnoredCodes(404);
        var processor = new TraceIgnoreStatusCodeTransactionProcessor(options);
        var transaction = new SentryTransaction("name", "operation");
        transaction.SetData(OtelSemanticConventions.AttributeHttpResponseStatusCode, (short)404);

        // Act
        var result = processor.Process(transaction);

        // Assert
        result.Should().BeNull();
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
