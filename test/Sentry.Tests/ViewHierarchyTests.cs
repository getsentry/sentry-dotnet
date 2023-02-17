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

public class ViewHierarchyNodeTests
{
    private class TestViewHierarchyNode : ViewHierarchyNode
    {
        public TestViewHierarchyNode(string type) : base(type) { }
        public bool WriteAdditionalPropertiesGotCalled { get; private set; }

        protected override void WriteAdditionalProperties(Utf8JsonWriter writer, IDiagnosticLogger logger)
        {
            WriteAdditionalPropertiesGotCalled = true;
        }
    }

    [Fact]
    public void WriteTo_ContainsTypeAndChildren()
    {
        // Arrange
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        var sut = new TestViewHierarchyNode("Test_Node");
        sut.Children.Add(new TestViewHierarchyNode("Child_Node"));

        // Act
        sut.WriteTo(writer, null);

        writer.Flush();
        stream.Seek(0, SeekOrigin.Begin);

        // Assert
        var serializedNode = new StreamReader(stream, Encoding.ASCII).ReadToEnd();
        serializedNode.Should().Be(
            "{" +
                "\"type\":\"Test_Node\"," +
                "\"children\":[" +
                    "{" +
                        "\"type\":\"Child_Node\"," +
                        "\"children\":[]" +
                    "}" +
                "]" +
            "}");
    }

    [Fact]
    public void WriteAdditionalProperties_WriteTo_GetsCalled()
    {
        // Arrange
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        var sut = new TestViewHierarchyNode("Test_Node");

        // Act
        sut.WriteTo(writer, null);

        // Assert
        Assert.True(sut.WriteAdditionalPropertiesGotCalled);
    }
}
