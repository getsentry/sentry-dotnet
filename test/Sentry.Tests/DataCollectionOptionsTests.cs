namespace Sentry.Tests;

public class DataCollectionOptionsTests
{
    [Fact]
    public void DataCollection_ByDefault_LeavesEveryCategoryUnset()
    {
        var options = new SentryOptions();

        using (new AssertionScope())
        {
            options.DataCollection.UserInfo.Should().BeNull();
            options.DataCollection.Cookies.Should().BeNull();
            options.DataCollection.HttpBodies.Should().BeNull();
            options.DataCollection.UrlQueryParams.Should().BeNull();
            options.DataCollection.DatabaseQueryData.Should().BeNull();
            options.DataCollection.Queues.Should().BeNull();
            options.DataCollection.FilePaths.Should().BeNull();
            options.DataCollection.StackFrameVariables.Should().BeNull();
            options.DataCollection.FrameContextLines.Should().BeNull();
            options.DataCollection.HttpHeaders.Request.Should().BeNull();
            options.DataCollection.HttpHeaders.Response.Should().BeNull();
            options.DataCollection.GraphQL.Document.Should().BeNull();
            options.DataCollection.GraphQL.Variables.Should().BeNull();
            options.DataCollection.GenAI.Inputs.Should().BeNull();
            options.DataCollection.GenAI.Outputs.Should().BeNull();
            options.DataCollection.Mcp.Inputs.Should().BeNull();
            options.DataCollection.Mcp.Outputs.Should().BeNull();
        }
    }

    [Fact]
    public void IsExplicitlyConfigured_ByDefault_IsFalse()
    {
        new SentryOptions().DataCollection.IsExplicitlyConfigured.Should().BeFalse();
    }

    [Fact]
    public void IsExplicitlyConfigured_AfterEnable_IsTrue()
    {
        var options = new SentryOptions();

        options.DataCollection.Enable();

        options.DataCollection.IsExplicitlyConfigured.Should().BeTrue();
    }

    public static IEnumerable<object[]> CategorySetters()
    {
        yield return [(Action<DataCollectionOptions>)(o => o.UserInfo = false)];
        yield return [(Action<DataCollectionOptions>)(o => o.Cookies = KeyValueCollectionBehavior.Off)];
        yield return [(Action<DataCollectionOptions>)(o => o.HttpBodies = HttpBodyTypes.None)];
        yield return [(Action<DataCollectionOptions>)(o => o.UrlQueryParams = KeyValueCollectionBehavior.Off)];
        yield return [(Action<DataCollectionOptions>)(o => o.DatabaseQueryData = false)];
        yield return [(Action<DataCollectionOptions>)(o => o.Queues = false)];
        yield return [(Action<DataCollectionOptions>)(o => o.FilePaths = false)];
        yield return [(Action<DataCollectionOptions>)(o => o.StackFrameVariables = false)];
        yield return [(Action<DataCollectionOptions>)(o => o.FrameContextLines = 0)];
        yield return [(Action<DataCollectionOptions>)(o => o.HttpHeaders.Request = KeyValueCollectionBehavior.Off)];
        yield return [(Action<DataCollectionOptions>)(o => o.HttpHeaders.Response = KeyValueCollectionBehavior.Off)];
        yield return [(Action<DataCollectionOptions>)(o => o.GraphQL.Document = false)];
        yield return [(Action<DataCollectionOptions>)(o => o.GraphQL.Variables = false)];
        yield return [(Action<DataCollectionOptions>)(o => o.GenAI.Inputs = false)];
        yield return [(Action<DataCollectionOptions>)(o => o.GenAI.Outputs = false)];
        yield return [(Action<DataCollectionOptions>)(o => o.Mcp.Inputs = false)];
        yield return [(Action<DataCollectionOptions>)(o => o.Mcp.Outputs = false)];
    }

    [Theory]
    [MemberData(nameof(CategorySetters))]
    public void IsExplicitlyConfigured_WhenAnyCategorySet_IsTrue(Action<DataCollectionOptions> configure)
    {
        var options = new SentryOptions();

        configure(options.DataCollection);

        options.DataCollection.IsExplicitlyConfigured.Should().BeTrue();
    }

    [Fact]
    public void DataCollection_IsNotAffectedBySendDefaultPii()
    {
        var options = new SentryOptions { SendDefaultPii = true };

        options.DataCollection.IsExplicitlyConfigured.Should().BeFalse();
    }
}

public class KeyValueCollectionBehaviorTests
{
    [Fact]
    public void Off_CollectsNothingAndHasNoTerms()
    {
        KeyValueCollectionBehavior.Off.Mode.Should().Be(KeyValueCollectionMode.Off);
        KeyValueCollectionBehavior.Off.Terms.Should().BeEmpty();
    }

    [Fact]
    public void DenyList_WithoutTerms_UsesDenyListModeAndNoAdditionalTerms()
    {
        var behavior = KeyValueCollectionBehavior.DenyList();

        behavior.Mode.Should().Be(KeyValueCollectionMode.DenyList);
        behavior.Terms.Should().BeEmpty();
    }

    [Fact]
    public void DenyList_WithTerms_KeepsTerms()
    {
        var behavior = KeyValueCollectionBehavior.DenyList("forwarded", "-ip");

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

    [Theory]
    [InlineData(true, KeyValueCollectionMode.DenyList)]
    [InlineData(false, KeyValueCollectionMode.Off)]
    public void ImplicitConversionFromBool_MapsToExpectedMode(bool collect, KeyValueCollectionMode expected)
    {
        KeyValueCollectionBehavior behavior = collect;

        behavior.Mode.Should().Be(expected);
    }

    [Fact]
    public void ImplicitConversionFromBool_AssignsToCategory()
    {
        var options = new SentryOptions();

        options.DataCollection.Cookies = false;
        options.DataCollection.StackFrameVariables = true;

        options.DataCollection.Cookies!.Mode.Should().Be(KeyValueCollectionMode.Off);
        options.DataCollection.StackFrameVariables!.Mode.Should().Be(KeyValueCollectionMode.DenyList);
    }
}

public class HttpBodyTypesTests
{
    [Fact]
    public void All_ContainsEveryDirection()
    {
        HttpBodyTypes.All.Should().Be(
            HttpBodyTypes.IncomingRequest | HttpBodyTypes.OutgoingRequest |
            HttpBodyTypes.IncomingResponse | HttpBodyTypes.OutgoingResponse);
    }

    [Fact]
    public void None_IsDefaultAndHasNoFlags()
    {
        default(HttpBodyTypes).Should().Be(HttpBodyTypes.None);
        HttpBodyTypes.All.HasFlag(HttpBodyTypes.None).Should().BeTrue();
    }

    [Fact]
    public void Flags_AreIndependentlyCombinable()
    {
        var bodies = HttpBodyTypes.IncomingRequest | HttpBodyTypes.OutgoingResponse;

        using (new AssertionScope())
        {
            bodies.HasFlag(HttpBodyTypes.IncomingRequest).Should().BeTrue();
            bodies.HasFlag(HttpBodyTypes.OutgoingResponse).Should().BeTrue();
            bodies.HasFlag(HttpBodyTypes.OutgoingRequest).Should().BeFalse();
            bodies.HasFlag(HttpBodyTypes.IncomingResponse).Should().BeFalse();
        }
    }
}
