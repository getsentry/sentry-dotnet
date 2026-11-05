namespace Sentry.Tests;

public class DataCollectionOptionsTests
{
    [Fact]
    public void ConfigureDataCollection_StartsAtSpecDefaults()
    {
        var options = new SentryOptions();

        options.ConfigureDataCollection(_ => { });

        using (new AssertionScope())
        {
            options.DataCollection!.UserInfo.Should().BeTrue();
            options.DataCollection.HttpBodies.Should().Be(HttpBodyTypes.All);
            options.DataCollection.DatabaseQueryData.Should().BeTrue();
            options.DataCollection.Cookies.Mode.Should().Be(KeyValueCollectionMode.DenyList);
            options.DataCollection.UrlQueryParams.Mode.Should().Be(KeyValueCollectionMode.DenyList);
            options.DataCollection.HttpHeaders.Request.Mode.Should().Be(KeyValueCollectionMode.DenyList);
            options.DataCollection.HttpHeaders.Response.Mode.Should().Be(KeyValueCollectionMode.DenyList);
            options.DataCollection.GraphQL.Document.Should().BeTrue();
            options.DataCollection.GraphQL.Variables.Should().BeTrue();
            options.DataCollection.GenAI.Inputs.Should().BeTrue();
            options.DataCollection.GenAI.Outputs.Should().BeTrue();
        }
    }

    [Fact]
    public void ConfigureDataCollection_NestedCategories_AreSettableInPlace()
    {
        var options = new SentryOptions();

        options.ConfigureDataCollection(dataCollection =>
        {
            dataCollection.HttpHeaders.Request.Mode = KeyValueCollectionMode.AllowList;
            dataCollection.GraphQL.Variables = false;
            dataCollection.GenAI.Inputs = false;
        });

        using (new AssertionScope())
        {
            options.DataCollection!.HttpHeaders.Request.Mode.Should().Be(KeyValueCollectionMode.AllowList);
            options.DataCollection.GraphQL.Variables.Should().BeFalse();
            options.DataCollection.GenAI.Inputs.Should().BeFalse();
        }
    }

    [Fact]
    public void ConfigureDataCollection_DefaultBehaviors_AreIndependentPerCategory()
    {
        var options = new SentryOptions();

        options.ConfigureDataCollection(dataCollection => dataCollection.Cookies.Mode = KeyValueCollectionMode.Off);

        using (new AssertionScope())
        {
            options.DataCollection!.Cookies.Mode.Should().Be(KeyValueCollectionMode.Off);
            options.DataCollection.UrlQueryParams.Mode.Should().Be(KeyValueCollectionMode.DenyList);
            options.DataCollection.HttpHeaders.Request.Mode.Should().Be(KeyValueCollectionMode.DenyList);
        }
    }

    [Fact]
    public void ConfigureDataCollection_CalledTwice_BuildsOnTheSameOptions()
    {
        var options = new SentryOptions();

        options.ConfigureDataCollection(dataCollection => dataCollection.UserInfo = false);
        options.ConfigureDataCollection(dataCollection => dataCollection.DatabaseQueryData = false);

        using (new AssertionScope())
        {
            options.DataCollection!.UserInfo.Should().BeFalse();
            options.DataCollection.DatabaseQueryData.Should().BeFalse();
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
