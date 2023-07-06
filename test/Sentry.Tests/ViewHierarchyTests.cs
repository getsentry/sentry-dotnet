namespace Sentry.Tests;

public class ViewHierarchyTests
{
    [Fact]
    public void WriteTo_ContainsRenderingSystemAndWindows()
    {
        // Arrange
        var sut = new ViewHierarchy("Test_Rendering_System");

        // Act
        var actual = sut.ToJsonString(indented: true);

        // Assert
        Assert.Equal("""
            {
              "rendering_system": "Test_Rendering_System",
              "windows": []
            }
            """, actual);
    }

    [Fact]
    public void WriteTo_ContainsRenderingSystemAndOneWindow()
    {
        // Arrange
        var sut = new ViewHierarchy("Test_Rendering_System");
        sut.Windows.Add(new ViewHierarchyNodeTests.TestViewHierarchyNode("Test_Node"));

        // Act
        var actual = sut.ToJsonString(indented: true);

        // Assert
        Assert.Equal("""
            {
              "rendering_system": "Test_Rendering_System",
              "windows": [
                {
                  "type": "Test_Node",
                  "children": []
                }
              ]
            }
            """, actual);
    }
}
