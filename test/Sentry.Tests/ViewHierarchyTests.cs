namespace Sentry.Tests;

public class ViewHierarchyTests
{
    [Fact]
    public void WriteTo_ContainsRenderingSystemAndWindows()
    {
        // Arrange
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        var sut = new ViewHierarchy("Test_Rendering_System");

        // Act
        sut.WriteTo(writer, null);

        writer.Flush();
        stream.Seek(0, SeekOrigin.Begin);

        // Assert
        var serializedViewHierarchy = new StreamReader(stream, Encoding.ASCII).ReadToEnd();
        serializedViewHierarchy.Should().Be(
            "{" +
                "\"rendering_system\":\"Test_Rendering_System\"," +
                "\"windows\":[]" +
            "}");
    }
}
