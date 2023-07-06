namespace Sentry.Tests;

public class ViewHierarchyNodeTests
{
    internal class TestViewHierarchyNode : ViewHierarchyNode
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
        var sut = new TestViewHierarchyNode("Test_Node");
        sut.Children.Add(new TestViewHierarchyNode("Child_Node"));

        // Act
        var actual = sut.ToJsonString(indented: true);

        // Assert
        Assert.Equal("""
            {
              "type": "Test_Node",
              "children": [
                {
                  "type": "Child_Node",
                  "children": []
                }
              ]
            }
            """, actual);
    }

    [Fact]
    public void WriteAdditionalProperties_WriteTo_GetsCalled()
    {
        // Arrange
        var sut = new TestViewHierarchyNode("Test_Node");

        // Act
        _ = sut.ToJsonString();

        // Assert
        Assert.True(sut.WriteAdditionalPropertiesGotCalled);
    }
}
