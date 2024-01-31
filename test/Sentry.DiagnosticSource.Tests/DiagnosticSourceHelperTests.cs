using Sentry.Internal.DiagnosticSource;

namespace Sentry.DiagnosticSource.Tests;

public class DiagnosticSourceHelperTests
{
    [Fact]
    public void FilterNewLineValue_StringWithNewLine_SubStringAfterNewLine()
    {
        // Arrange
        var text = "1234\r\nSELECT *...\n FROM ...";
        var expectedText = "SELECT *...\n FROM ...";

        // Act
        var value = EFDiagnosticSourceHelper.FilterNewLineValue(text);

        // Assert
        Assert.Equal(expectedText, value);
    }

    [Fact]
    public void FilterNewLineValue_NullObject_NullString()
    {
        // Act
        var value = EFDiagnosticSourceHelper.FilterNewLineValue(null);

        // Assert
        Assert.Null(value);
    }

    [Fact]
    public void FilterNewLineValue_OneLineString_OneLineString()
    {
        // Arrange
        var text = "1234";
        var expectedText = "1234";

        // Act
        var value = EFDiagnosticSourceHelper.FilterNewLineValue(text);

        // Assert
        Assert.Equal(expectedText, value);
    }

    [Fact]
    public void FilterNewLineValue_EmptyString_EmptyString()
    {
        // Arrange
        var text = "";
        var expectedText = "";

        // Act
        var value = EFDiagnosticSourceHelper.FilterNewLineValue(text);

        // Assert
        Assert.Equal(expectedText, value);
    }
}
