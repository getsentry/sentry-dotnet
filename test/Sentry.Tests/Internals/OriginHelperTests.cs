namespace Sentry.Tests.Internals;

public class OriginHelperTests
{
    [Theory]
    [InlineData("manual.category.integration.part")]
    [InlineData("manual.CATEGORY.INTEGRATION.PART")]
    [InlineData("manual.cat_012.int_345.part_6789")]
    [InlineData("manual.cat_.int_.part_")]
    [InlineData("auto.ABCdef123_")]
    public void IsValidOrigin_ValidOrigin_True(string input)
    {
        var result = OriginHelper.IsValidOrigin(input);
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("manual.invalid category.integration.part")]
    [InlineData("manual.CATEGORY.INTEGRATION.PART.invalid_segment")]
    [InlineData("manual.forbidden_@#$_characters")]
    [InlineData("invalid")]
    [InlineData("auto.foo&")]
    [InlineData("invalid.foo.b^ar")]
    [InlineData("invalid.foo.bar.4*2")]
    public void IsValidOrigin_InvalidOrigin_False(string input)
    {
        var result = OriginHelper.IsValidOrigin(input);
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("manual.category.integration.part")]
    [InlineData("manual.CATEGORY.INTEGRATION.PART")]
    [InlineData("manual.cat_012.int_345.part_6789")]
    [InlineData("manual.cat_.int_.part_")]
    [InlineData("auto.ABCdef123_")]
    public void TryParse_ValidOrigin_ReturnsInput(string input)
    {
        var result = OriginHelper.TryParse(input);
        result.Should().Be(input);
    }

    [Theory]
    [InlineData("manual.invalid category.integration.part")]
    [InlineData("manual.CATEGORY.INTEGRATION.PART.invalid_segment")]
    [InlineData("manual.forbidden_@#$_characters")]
    [InlineData("invalid")]
    [InlineData("auto.foo&")]
    [InlineData("invalid.foo.b^ar")]
    [InlineData("invalid.foo.bar.4*2")]
    public void TryParse_InvalidOrigin_ReturnsNull(string input)
    {
        var result = OriginHelper.TryParse(input);
        result.Should().Be(null);
    }
}
