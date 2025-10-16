namespace Sentry.Tests;

public class CaptureFeedbackResultTests
{
    [Fact]
    public void Constructor_WithValidEventId_SetsEventIdAndNoError()
    {
        // Arrange
        var eventId = SentryId.Create();

        // Act
        var result = new CaptureFeedbackResult(eventId);

        // Assert
        result.EventId.Should().Be(eventId);
        result.ErrorReason.Should().BeNull();
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithEmptyEventId_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => new CaptureFeedbackResult(SentryId.Empty);
        act.Should().Throw<ArgumentException>()
            .WithMessage("EventId cannot be empty*")
            .And.ParamName.Should().Be("eventId");
    }

    [Fact]
    public void Constructor_WithErrorReason_SetsEmptyEventIdAndErrorReason()
    {
        // Arrange
        var errorReason = CaptureFeedbackErrorReason.EmptyMessage;

        // Act
        var result = new CaptureFeedbackResult(errorReason);

        // Assert
        result.EventId.Should().Be(SentryId.Empty);
        result.ErrorReason.Should().Be(errorReason);
        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public void Succeeded_WhenErrorReasonIsNone_ReturnsTrue()
    {
        // Arrange
        var result = new CaptureFeedbackResult(SentryId.Create());

        // Act & Assert
        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData(CaptureFeedbackErrorReason.UnknownError)]
    [InlineData(CaptureFeedbackErrorReason.DisabledHub)]
    [InlineData(CaptureFeedbackErrorReason.EmptyMessage)]
    public void Succeeded_WhenErrorReasonIsNotNone_ReturnsFalse(CaptureFeedbackErrorReason errorReason)
    {
        // Arrange
        var result = new CaptureFeedbackResult(errorReason);

        // Act & Assert
        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public void Equals_WithSameEventIdAndErrorReason_ReturnsTrue()
    {
        // Arrange
        var eventId = SentryId.Create();
        var result1 = new CaptureFeedbackResult(eventId);
        var result2 = new CaptureFeedbackResult(eventId);

        // Act & Assert
        result1.Equals(result2).Should().BeTrue();
        result1.Equals((object)result2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentEventIds_ReturnsFalse()
    {
        // Arrange
        var result1 = new CaptureFeedbackResult(SentryId.Create());
        var result2 = new CaptureFeedbackResult(SentryId.Create());

        // Act & Assert
        result1.Equals(result2).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentErrorReasons_ReturnsFalse()
    {
        // Arrange
        var result1 = new CaptureFeedbackResult(CaptureFeedbackErrorReason.DisabledHub);
        var result2 = new CaptureFeedbackResult(CaptureFeedbackErrorReason.EmptyMessage);

        // Act & Assert
        result1.Equals(result2).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        // Arrange
        var result = new CaptureFeedbackResult(SentryId.Create());

        // Act & Assert
        result.Equals(null).Should().BeFalse();
        result.Equals((object)null!).Should().BeFalse();
    }

    [Fact]
    public void OperatorEquals_WithEqualResults_ReturnsTrue()
    {
        // Arrange
        var eventId = SentryId.Create();
        var result1 = new CaptureFeedbackResult(eventId);
        var result2 = new CaptureFeedbackResult(eventId);

        // Act & Assert
        (result1 == result2).Should().BeTrue();
    }

    [Fact]
    public void OperatorEquals_WithDifferentResults_ReturnsFalse()
    {
        // Arrange
        var result1 = new CaptureFeedbackResult(SentryId.Create());
        var result2 = new CaptureFeedbackResult(SentryId.Create());

        // Act & Assert
        (result1 == result2).Should().BeFalse();
    }

    [Fact]
    public void OperatorNotEquals_WithEqualResults_ReturnsFalse()
    {
        // Arrange
        var eventId = SentryId.Create();
        var result1 = new CaptureFeedbackResult(eventId);
        var result2 = new CaptureFeedbackResult(eventId);

        // Act & Assert
        (result1 != result2).Should().BeFalse();
    }

    [Fact]
    public void OperatorNotEquals_WithDifferentResults_ReturnsTrue()
    {
        // Arrange
        var result1 = new CaptureFeedbackResult(SentryId.Create());
        var result2 = new CaptureFeedbackResult(SentryId.Create());

        // Act & Assert
        (result1 != result2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_WithSameValues_ReturnsSameHashCode()
    {
        // Arrange
        var eventId = SentryId.Create();
        var result1 = new CaptureFeedbackResult(eventId);
        var result2 = new CaptureFeedbackResult(eventId);

        // Act & Assert
        result1.GetHashCode().Should().Be(result2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithDifferentValues_ReturnsDifferentHashCode()
    {
        // Arrange
        var result1 = new CaptureFeedbackResult(SentryId.Create());
        var result2 = new CaptureFeedbackResult(SentryId.Create());

        // Act & Assert
        result1.GetHashCode().Should().NotBe(result2.GetHashCode());
    }
}
