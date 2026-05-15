namespace Sentry.Tests;

public class SentryEventExtensionsTests
{
    [Fact]
    public void IsFromUnhandledException_NoException_ReturnsFalse()
    {
        // Arrange
        var sentryEvent = new SentryEvent();

        // Act
        var result = sentryEvent.IsFromUnhandledException();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsFromUnhandledException_HandledExceptionInExceptionProperty_ReturnsFalse()
    {
        // Arrange
        var exception = new Exception("test");
        exception.Data[Mechanism.HandledKey] = true;
        var sentryEvent = new SentryEvent(exception);

        // Act
        var result = sentryEvent.IsFromUnhandledException();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsFromUnhandledException_UnhandledExceptionInExceptionProperty_ReturnsTrue()
    {
        // Arrange
        var exception = new Exception("test");
        exception.Data[Mechanism.HandledKey] = false;
        var sentryEvent = new SentryEvent(exception);

        // Act
        var result = sentryEvent.IsFromUnhandledException();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsFromUnhandledException_HandledExceptionInSentryExceptions_ReturnsFalse()
    {
        // Arrange
        var sentryEvent = new SentryEvent
        {
            SentryExceptions = new[]
            {
                new SentryException
                {
                    Type = "Exception",
                    Value = "test",
                    Mechanism = new Mechanism { Handled = true }
                }
            }
        };

        // Act
        var result = sentryEvent.IsFromUnhandledException();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsFromUnhandledException_UnhandledExceptionInSentryExceptions_ReturnsTrue()
    {
        // Arrange
        var sentryEvent = new SentryEvent
        {
            SentryExceptions = new[]
            {
                new SentryException
                {
                    Type = "Exception",
                    Value = "test",
                    Mechanism = new Mechanism { Handled = false }
                }
            }
        };

        // Act
        var result = sentryEvent.IsFromUnhandledException();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsFromUnhandledException_MixedExceptions_ReturnsTrue()
    {
        // Arrange - if ANY exception is unhandled, returns true
        var sentryEvent = new SentryEvent
        {
            SentryExceptions = new[]
            {
                new SentryException
                {
                    Type = "Exception",
                    Value = "handled",
                    Mechanism = new Mechanism { Handled = true }
                },
                new SentryException
                {
                    Type = "Exception",
                    Value = "unhandled",
                    Mechanism = new Mechanism { Handled = false }
                }
            }
        };

        // Act
        var result = sentryEvent.IsFromUnhandledException();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsFromTerminalException_NoException_ReturnsFalse()
    {
        // Arrange
        var sentryEvent = new SentryEvent();

        // Act
        var result = sentryEvent.IsFromTerminalException();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsFromTerminalException_HandledExceptionInExceptionProperty_ReturnsFalse()
    {
        // Arrange
        var exception = new Exception("test");
        exception.Data[Mechanism.HandledKey] = true;
        var sentryEvent = new SentryEvent(exception);

        // Act
        var result = sentryEvent.IsFromTerminalException();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsFromTerminalException_UnhandledTerminalExceptionInExceptionProperty_ReturnsTrue()
    {
        // Arrange
        var exception = new Exception("test");
        exception.Data[Mechanism.HandledKey] = false;
        // Terminal is true by default when unhandled
        var sentryEvent = new SentryEvent(exception);

        // Act
        var result = sentryEvent.IsFromTerminalException();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsFromTerminalException_UnhandledNonTerminalExceptionInExceptionProperty_ReturnsFalse()
    {
        // Arrange
        var exception = new Exception("test");
        exception.Data[Mechanism.HandledKey] = false;
        exception.Data[Mechanism.TerminalKey] = false; // Explicitly non-terminal
        var sentryEvent = new SentryEvent(exception);

        // Act
        var result = sentryEvent.IsFromTerminalException();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsFromTerminalException_UnhandledTerminalExceptionInSentryExceptions_ReturnsTrue()
    {
        // Arrange
        var sentryEvent = new SentryEvent
        {
            SentryExceptions = new[]
            {
                new SentryException
                {
                    Type = "Exception",
                    Value = "test",
                    Mechanism = new Mechanism { Handled = false } // Terminal is null, defaults to true
                }
            }
        };

        // Act
        var result = sentryEvent.IsFromTerminalException();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsFromTerminalException_UnhandledExplicitlyTerminalExceptionInSentryExceptions_ReturnsTrue()
    {
        // Arrange
        var sentryEvent = new SentryEvent
        {
            SentryExceptions = new[]
            {
                new SentryException
                {
                    Type = "Exception",
                    Value = "test",
                    Mechanism = new Mechanism { Handled = false, Terminal = true }
                }
            }
        };

        // Act
        var result = sentryEvent.IsFromTerminalException();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsFromTerminalException_UnhandledNonTerminalExceptionInSentryExceptions_ReturnsFalse()
    {
        // Arrange
        var sentryEvent = new SentryEvent
        {
            SentryExceptions = new[]
            {
                new SentryException
                {
                    Type = "Exception",
                    Value = "test",
                    Mechanism = new Mechanism { Handled = false, Terminal = false }
                }
            }
        };

        // Act
        var result = sentryEvent.IsFromTerminalException();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsFromTerminalException_MixedExceptions_ReturnsTrueIfAnyTerminal()
    {
        // Arrange
        var sentryEvent = new SentryEvent
        {
            SentryExceptions = new[]
            {
                new SentryException
                {
                    Type = "Exception",
                    Value = "non-terminal",
                    Mechanism = new Mechanism { Handled = false, Terminal = false }
                },
                new SentryException
                {
                    Type = "Exception",
                    Value = "terminal",
                    Mechanism = new Mechanism { Handled = false, Terminal = true }
                }
            }
        };

        // Act
        var result = sentryEvent.IsFromTerminalException();

        // Assert
        Assert.True(result);
    }
}
