namespace Sentry.Tests.Protocol.Context;

public class OriginTests
{
    [Fact]
    public void ToString_AllParts_ReturnsConcatenatedString()
    {
        // Arrange
        var origin = new Origin {
            Type = OriginType.Manual,
            Category = "http",
            IntegrationName = "sentry_dotnet",
            IntegrationPart = "opentelemetry"
        };

        // Act
        var result = origin.ToString();

        // Assert
        Assert.Equal("manual.http.sentry_dotnet.opentelemetry", result);
    }

    [Fact]
    public void ToString_MissingCategory_ReturnsOnlyTypePart()
    {
        // Arrange
        var origin = new Origin{ Type = OriginType.Auto, Category = "", IntegrationPart = "opentelemetry" };

        // Act
        var result = origin.ToString();

        // Assert
        Assert.Equal("auto", result);
    }

    [Fact]
    public void ToString_MissingIntegrationName_ReturnsFirstTwoParts()
    {
        // Arrange
        var origin = new Origin{ Type = OriginType.Auto, Category = "category" };

        // Act
        var result = origin.ToString();

        // Assert
        Assert.Equal("auto.category", result);
    }

    [Fact]
    public void ToString_MissingIntegrationPart_ReturnsFirstThreeParts()
    {
        // Arrange
        var origin = new Origin{ Type = OriginType.Auto, Category = "category", IntegrationName = "sentry_dotnet" };

        // Act
        var result = origin.ToString();

        // Assert
        Assert.Equal("auto.category.sentry_dotnet", result);
    }

    [Fact]
    public void ToString_NoParts_ReturnsEmptyString()
    {
        // Arrange
        var origin = new Origin();

        // Act
        var result = origin.ToString();

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Equality_SameObject_ReturnsTrue()
    {
        var origin = new Origin();
        Assert.True(origin.Equals(origin));
    }

    [Fact]
    public void Equality_SameToString_ReturnsTrue()
    {
        var origin1 = new Origin{Type = OriginType.Manual, Category = "cat", IntegrationPart = "part"};
        var origin2 = new Origin{Type = OriginType.Manual, Category = "cat", IntegrationPart = "part"};

        Assert.True(origin1.Equals(origin2));
    }

    [Fact]
    public void Equality_DifferentToString_ReturnsFalse()
    {
        var origin1 = new Origin{Type = OriginType.Manual, Category = "cat", IntegrationPart = "part1"};
        var origin2 = new Origin{Type = OriginType.Manual, Category = "cat", IntegrationPart = "part2"};

        Assert.False(origin1.Equals(origin2));
    }

    [Fact]
    public void GetHashCode_SameObject_SameHashCode()
    {
        var origin = new Origin();
        Assert.Equal(origin.GetHashCode(), origin.GetHashCode());
    }

    [Fact]
    public void GetHashCode_EqualObjects_SameHashCode()
    {
        var origin1 = new Origin{Type = OriginType.Auto, Category = "cat"};
        var origin2 = new Origin{Type = OriginType.Auto, Category = "cat"};

        Assert.Equal(origin1.GetHashCode(), origin2.GetHashCode());
    }

    [Theory]
    [InlineData("lowercase")]
    [InlineData("UPPERCASE")]
    [InlineData("0123456789")]
    [InlineData("_")]
    [InlineData("ABCdef123_")]
    public void Origin_ValidPartNames_Assigned(string validName)
    {
        var origin = new Origin
        {
            Category = "Category_" + validName,
            IntegrationName = "IntegrationName" + validName,
            IntegrationPart = "IntegrationPart" + validName
        };
        origin.Category.Should().Be("Category_" + validName);
        origin.IntegrationName.Should().Be("IntegrationName" + validName);
        origin.IntegrationPart.Should().Be("IntegrationPart" + validName);
    }

    [Fact]
    public void Origin_InvalidCategory_ArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            var origin = new Origin
            {
                Category = "invalid name"
            };
        });
    }

    [Fact]
    public void Origin_InvalidIntegrationName_ArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            var origin = new Origin
            {
                IntegrationName = "invalid name"
            };
        });
    }

    [Fact]
    public void Origin_InvalidIntegrationPart_ArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            var origin = new Origin
            {
                IntegrationPart = "invalid name"
            };
        });
    }
}
