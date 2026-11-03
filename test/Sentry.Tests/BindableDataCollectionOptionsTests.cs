#if !NETFRAMEWORK
using Microsoft.Extensions.Configuration;

namespace Sentry.Tests;

public class BindableDataCollectionOptionsTests : BindableTests<DataCollectionOptions>
{
    public BindableDataCollectionOptionsTests()
        : base(
            nameof(DataCollectionOptions.Cookies),
            nameof(DataCollectionOptions.HttpBodies),
            nameof(DataCollectionOptions.UrlQueryParams),
            nameof(DataCollectionOptions.StackFrameVariables),
            nameof(DataCollectionOptions.HttpHeaders),
            nameof(DataCollectionOptions.GraphQL),
            nameof(DataCollectionOptions.GenAI),
            nameof(DataCollectionOptions.Mcp))
    {
    }

    [Fact]
    public void BindableProperties_MatchOptionsProperties()
    {
        var actual = GetPropertyNames<BindableSentryOptions.DataCollectionOptions>();
        AssertPropertiesMatchOptions(actual);
    }

    [Fact]
    public void ApplyTo_SetsOptionsFromConfig()
    {
        // Arrange
        var actual = new DataCollectionOptions();
        var bindable = new BindableSentryOptions.DataCollectionOptions();

        // Act
        Fixture.Config.Bind(bindable);
        bindable.ApplyTo(actual);

        // Assert
        AssertContainsExpectedPropertyValues(actual);
    }
}

public class DataCollectionConfigurationTests
{
    private static SentryOptions Bind(params (string Key, string Value)[] values)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string>(v.Key, v.Value)))
            .Build();

        var bindable = new BindableSentryOptions();
        config.Bind(bindable);

        var options = new SentryOptions();
        bindable.ApplyTo(options);
        return options;
    }

    [Fact]
    public void Bind_NoDataCollectionSection_LeavesDataCollectionUnset()
    {
        var options = Bind(("Dsn", "https://k@o.ingest.sentry.io/1"));

        options.DataCollection.Should().BeNull();
    }

    [Fact]
    public void Bind_SectionPresent_OptsInAndLeavesUnsetCategoriesNull()
    {
        var options = Bind(("DataCollection:UserInfo", "false"));

        using (new AssertionScope())
        {
            options.DataCollection.Should().NotBeNull();
            options.DataCollection!.UserInfo.Should().BeFalse();
            options.DataCollection!.Cookies.Should().BeNull();
            options.DataCollection!.HttpHeaders.Request.Should().BeNull();
        }
    }

    [Fact]
    public void Bind_SectionPresent_MergesIntoOptionsSetInCode()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection([new KeyValuePair<string, string>("DataCollection:Queues", "false")])
            .Build();
        var bindable = new BindableSentryOptions();
        config.Bind(bindable);
        var options = new SentryOptions { DataCollection = new DataCollectionOptions { UserInfo = false } };

        bindable.ApplyTo(options);

        options.DataCollection!.UserInfo.Should().BeFalse();
        options.DataCollection!.Queues.Should().BeFalse();
    }

    [Fact]
    public void Bind_BooleanCategories_AreApplied()
    {
        var options = Bind(
            ("DataCollection:UserInfo", "false"),
            ("DataCollection:DatabaseQueryData", "false"),
            ("DataCollection:Queues", "false"),
            ("DataCollection:FilePaths", "false"),
            ("DataCollection:FrameContextLines", "0"));

        using (new AssertionScope())
        {
            options.DataCollection!.UserInfo.Should().BeFalse();
            options.DataCollection!.DatabaseQueryData.Should().BeFalse();
            options.DataCollection!.Queues.Should().BeFalse();
            options.DataCollection!.FilePaths.Should().BeFalse();
            options.DataCollection!.FrameContextLines.Should().Be(0);
        }
    }

    [Fact]
    public void Bind_NestedCategories_AreApplied()
    {
        var options = Bind(
            ("DataCollection:GraphQL:Document", "false"),
            ("DataCollection:GraphQL:Variables", "false"),
            ("DataCollection:GenAI:Inputs", "false"),
            ("DataCollection:GenAI:Outputs", "false"),
            ("DataCollection:Mcp:Inputs", "false"),
            ("DataCollection:Mcp:Outputs", "false"));

        using (new AssertionScope())
        {
            options.DataCollection!.GraphQL.Document.Should().BeFalse();
            options.DataCollection!.GraphQL.Variables.Should().BeFalse();
            options.DataCollection!.GenAI.Inputs.Should().BeFalse();
            options.DataCollection!.GenAI.Outputs.Should().BeFalse();
            options.DataCollection!.Mcp.Inputs.Should().BeFalse();
            options.DataCollection!.Mcp.Outputs.Should().BeFalse();
        }
    }

    [Theory]
    [InlineData("Off", KeyValueCollectionMode.Off)]
    [InlineData("DenyList", KeyValueCollectionMode.DenyList)]
    [InlineData("AllowList", KeyValueCollectionMode.AllowList)]
    public void Bind_KeyValueMode_IsApplied(string configured, KeyValueCollectionMode expected)
    {
        var options = Bind(("DataCollection:Cookies:Mode", configured));

        options.DataCollection!.Cookies!.Mode.Should().Be(expected);
    }

    [Fact]
    public void Bind_KeyValueTerms_AreApplied()
    {
        var options = Bind(
            ("DataCollection:HttpHeaders:Request:Mode", "DenyList"),
            ("DataCollection:HttpHeaders:Request:Terms:0", "forwarded"),
            ("DataCollection:HttpHeaders:Request:Terms:1", "-ip"));

        var request = options.DataCollection!.HttpHeaders.Request!;
        request.Mode.Should().Be(KeyValueCollectionMode.DenyList);
        request.Terms.Should().Equal("forwarded", "-ip");
    }

    [Fact]
    public void Bind_ResponseHeadersAndStackFrameVariables_AreApplied()
    {
        var options = Bind(
            ("DataCollection:HttpHeaders:Response:Mode", "Off"),
            ("DataCollection:StackFrameVariables:Mode", "AllowList"),
            ("DataCollection:StackFrameVariables:Terms:0", "orderId"));

        using (new AssertionScope())
        {
            options.DataCollection!.HttpHeaders.Response!.Mode.Should().Be(KeyValueCollectionMode.Off);
            options.DataCollection!.StackFrameVariables!.Mode.Should().Be(KeyValueCollectionMode.AllowList);
            options.DataCollection!.StackFrameVariables!.Terms.Should().Equal("orderId");
        }
    }

    [Fact]
    public void Bind_KeyValueTermsWithoutMode_DefaultsToDenyList()
    {
        var options = Bind(("DataCollection:UrlQueryParams:Terms:0", "session_id"));

        var behavior = options.DataCollection!.UrlQueryParams!;
        behavior.Mode.Should().Be(KeyValueCollectionMode.DenyList);
        behavior.Terms.Should().Equal("session_id");
    }

    [Fact]
    public void Bind_KeyValueSectionAbsent_LeavesCategoryUnset()
    {
        var options = Bind(("DataCollection:UserInfo", "false"));

        options.DataCollection!.Cookies.Should().BeNull();
        options.DataCollection!.HttpHeaders.Response.Should().BeNull();
    }

    [Theory]
    [InlineData("false")]
    [InlineData("Off")]
    public void Bind_KeyValueScalarValue_IsIgnoredAndStillOptsIn(string scalar)
    {
        // The bool shorthand that code has for key-value categories has no configuration equivalent: the
        // surrogate binds from Mode/Terms, so a scalar is silently dropped by the configuration binder.
        // The section is still present, so DataCollection is opted into with the category left at its default.
        var options = Bind(("DataCollection:Cookies", scalar));

        using (new AssertionScope())
        {
            options.DataCollection.Should().NotBeNull();
            options.DataCollection!.Cookies.Should().BeNull();
        }
    }

    [Fact]
    public void Bind_HttpBodiesArray_CombinesEntries()
    {
        var options = Bind(
            ("DataCollection:HttpBodies:0", "IncomingRequest"),
            ("DataCollection:HttpBodies:1", "OutgoingResponse"));

        options.DataCollection!.HttpBodies.Should().Be(
            HttpBodyTypes.IncomingRequest | HttpBodyTypes.OutgoingResponse);
    }

    [Fact]
    public void Bind_HttpBodiesNoneEntry_DisablesBodyCollection()
    {
        var options = Bind(("DataCollection:HttpBodies:0", "None"));

        options.DataCollection!.HttpBodies.Should().Be(HttpBodyTypes.None);
    }

    [Fact]
    public void Bind_HttpBodiesEmptyArray_LeavesCategoryUnset()
    {
        // A JSON empty array emits the key with an empty value and no children, which the binder surfaces as
        // null rather than an empty list. The spec's "[]" opt-out is therefore not expressible in configuration;
        // "[\"None\"]" is the equivalent. Locked in so a future change to the surrogate has to confront it.
        var options = Bind(("DataCollection:HttpBodies", string.Empty));

        options.DataCollection!.HttpBodies.Should().BeNull();
    }

    [Fact]
    public void Bind_HttpBodiesScalar_IsIgnored()
    {
        var options = Bind(("DataCollection:HttpBodies", "IncomingRequest"));

        options.DataCollection!.HttpBodies.Should().BeNull();
    }

    [Fact]
    public void Bind_AnyCategory_OptsIn()
    {
        Bind(("DataCollection:Cookies:Mode", "Off")).DataCollection.Should().NotBeNull();
    }
}
#endif
