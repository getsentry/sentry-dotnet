using System.Collections.Specialized;
using Sentry.AspNet.Internal;

namespace Sentry.AspNet.Tests.Internal;

public class SystemWebHttpRequestTests
{
    [Fact]
    public void GetFormData_GoodData_ReturnsCorrectValues()
    {
        // Arrange
        var formCollection = new NameValueCollection
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };

        // Act
        var form = SystemWebHttpRequest.GetFormData(formCollection).ToDict();

        // Assert
        form.Should().NotBeNull();
        form.Should().Contain(kvp => kvp.Key == "key1" && kvp.Value.Contains("value1"));
        form.Should().Contain(kvp => kvp.Key == "key2" && kvp.Value.Contains("value2"));
    }

    [Fact]
    public void GetFormData_BadData_ReturnsCorrectValues()
    {
        // Arrange
        var formCollection = new NameValueCollection
        {
            { "key1", "value1" },
            { "key2", "value2" },
            { null, "badkey" },
            { "badvalue", null },
            { null, null }
        };

        // Act
        var form = SystemWebHttpRequest.GetFormData(formCollection).ToDict();

        // Assert
        form.Should().NotBeNull();
        form.Should().Contain(kvp => kvp.Key == "key1" && kvp.Value.Contains("value1"));
        form.Should().Contain(kvp => kvp.Key == "key2" && kvp.Value.Contains("value2"));
    }
}
