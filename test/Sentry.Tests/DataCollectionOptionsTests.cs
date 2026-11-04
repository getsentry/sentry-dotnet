namespace Sentry.Tests;

public class DataCollectionOptionsTests
{

    [Fact]
    public void DataCollection_WhenAssigned_LeavesEveryCategoryUnset()
    {
        var options = new SentryOptions { DataCollection = new DataCollectionOptions() };

        using (new AssertionScope())
        {
            options.DataCollection.UserInfo.Should().BeNull();
            options.DataCollection.Cookies.Should().BeNull();
            options.DataCollection.HttpBodies.Should().BeNull();
            options.DataCollection.UrlQueryParams.Should().BeNull();
            options.DataCollection.DatabaseQueryData.Should().BeNull();
            options.DataCollection!.HttpHeaders.Request.Should().BeNull();
            options.DataCollection.HttpHeaders.Response.Should().BeNull();
            options.DataCollection.GraphQL.Document.Should().BeNull();
            options.DataCollection.GraphQL.Variables.Should().BeNull();
            options.DataCollection.GenAI.Inputs.Should().BeNull();
            options.DataCollection.GenAI.Outputs.Should().BeNull();
        }
    }

}

public class KeyValueCollectionBehaviorTests
{
    [Fact]
    public void Off_ReturnsIndependentInstances()
    {
        var cookies = KeyValueCollectionBehavior.Off;
        cookies.Mode = KeyValueCollectionMode.DenyList;

        KeyValueCollectionBehavior.Off.Mode.Should().Be(KeyValueCollectionMode.Off);
    }

    [Fact]
    public void DenyList_WithTerms_KeepsNonEmptyTerms()
    {
        var behavior = KeyValueCollectionBehavior.DenyList("forwarded", "", null!, "-ip");

        behavior.Mode.Should().Be(KeyValueCollectionMode.DenyList);
        behavior.Terms.Should().Equal("forwarded", "-ip");
    }

    [Fact]
    public void AllowList_KeepsTerms()
    {
        var behavior = KeyValueCollectionBehavior.AllowList("user-agent");

        behavior.Mode.Should().Be(KeyValueCollectionMode.AllowList);
        behavior.Terms.Should().Equal("user-agent");
    }

    [Fact]
    public void DenyList_ExplicitNullTerms_HasEmptyTerms()
    {
        KeyValueCollectionBehavior.DenyList(null!).Terms.Should().BeEmpty();
    }

    [Theory]
    [InlineData(true, KeyValueCollectionMode.DenyList)]
    [InlineData(false, KeyValueCollectionMode.Off)]
    public void ImplicitConversionFromBool_MapsToExpectedMode(bool collect, KeyValueCollectionMode expected)
    {
        KeyValueCollectionBehavior behavior = collect;

        behavior.Mode.Should().Be(expected);
    }

}
