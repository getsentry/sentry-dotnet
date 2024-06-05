namespace Sentry.Tests.Protocol.Context;

public class OriginTests
{
    [Theory]
    [InlineData("auto")]
    [InlineData("auto.http")]
    [InlineData("auto.http.sentry_dotnet")]
    [InlineData("auto.http.sentry_dotnet.opentelemetry")]
    public void ToString_ReturnsMemberString(string input)
    {
        // Arrange
        var origin = new Origin(input);

        // Act
        var result = origin.ToString();

        // Assert
        Assert.Equal(input, result);
    }

    [Fact]
    public void Equality_SameObject_ReturnsTrue()
    {
        var origin = new Origin();
        // ReSharper disable once EqualExpressionComparison
        Assert.True(origin.Equals(origin));
    }

    [Fact]
    public void Equality_SameToString_ReturnsTrue()
    {
        var originString = "manual.cat.part";

        var origin1 = new Origin(originString);
        var origin2 = new Origin(originString);

        Assert.True(origin1.Equals(origin2));
    }

    [Fact]
    public void Equality_DifferentToString_ReturnsFalse()
    {
        var origin1 = new Origin("manual.cat.part1");
        var origin2 = new Origin("manual.cat.part2");

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
        var originString = "manual.cat.part";

        var origin1 = new Origin(originString);
        var origin2 = new Origin(originString);

        Assert.Equal(origin1.GetHashCode(), origin2.GetHashCode());
    }

    [Theory]
    [InlineData("manual.category.integration.part")]
    [InlineData("manual.CATEGORY.INTEGRATION.PART")]
    [InlineData("manual.cat_012.int_345.part_6789")]
    [InlineData("manual.cat_.int_.part_")]
    [InlineData("auto.ABCdef123_")]
    public void Origin_ValidOrigin_ConstructedCorrectly(string validName)
    {
        var origin = new Origin(validName);
    }

    [Theory]
    [InlineData("manual.invalid category.integration.part")]
    [InlineData("manual.CATEGORY.INTEGRATION.PART.invalid_segment")]
    [InlineData("manual.forbidden_@#$_characters")]
    [InlineData("invalid")]
    [InlineData("auto.foo&")]
    [InlineData("invalid.foo.b^ar")]
    [InlineData("invalid.foo.bar.4*2")]
    public void Origin_InvalidCategory_ArgumentException(string input)
    {
        Assert.Throws<ArgumentException>(() =>
        {
            var origin = new Origin(input);
        });
    }
}
