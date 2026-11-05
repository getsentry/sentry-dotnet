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
            nameof(DataCollectionOptions.HttpHeaders),
            nameof(DataCollectionOptions.GraphQL),
            nameof(DataCollectionOptions.GenAI))
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
    public void Bind_SectionPresent_OptsInAndLeavesUnsetCategoriesAtDefaults()
    {
        var options = Bind(("DataCollection:UserInfo", "false"));

        using (new AssertionScope())
        {
            options.DataCollection.Should().NotBeNull();
            options.DataCollection!.UserInfo.Should().BeFalse();
            options.DataCollection.Cookies.Mode.Should().Be(KeyValueCollectionMode.DenyList);
            options.DataCollection.HttpHeaders.Request.Mode.Should().Be(KeyValueCollectionMode.DenyList);
        }
    }

    [Fact]
    public void Bind_SectionPresent_MergesIntoOptionsSetInCode()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection([new KeyValuePair<string, string>("DataCollection:DatabaseQueryData", "false")])
            .Build();
        var bindable = new BindableSentryOptions();
        config.Bind(bindable);
        var options = new SentryOptions();
        options.ConfigureDataCollection(dataCollection =>
        {
            dataCollection.UserInfo = false;
            dataCollection.Cookies = KeyValueCollectionBehavior.Off;
        });

        bindable.ApplyTo(options);

        options.DataCollection!.UserInfo.Should().BeFalse();
        options.DataCollection.Cookies.Mode.Should().Be(KeyValueCollectionMode.Off);
        options.DataCollection.DatabaseQueryData.Should().BeFalse();
    }

    [Fact]
    public void Bind_KeyValueCategorySetInCode_IsReplacedWholeByConfig()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                [new KeyValuePair<string, string>("DataCollection:HttpHeaders:Request:Terms:0", "forwarded")])
            .Build();
        var bindable = new BindableSentryOptions();
        config.Bind(bindable);
        var options = new SentryOptions();
        options.ConfigureDataCollection(dataCollection =>
            dataCollection.HttpHeaders.Request = KeyValueCollectionBehavior.AllowList("x-tenant-id"));

        bindable.ApplyTo(options);

        var request = options.DataCollection!.HttpHeaders.Request;
        request.Mode.Should().Be(KeyValueCollectionMode.DenyList);
        request.Terms.Should().Equal("forwarded");
    }

    [Fact]
    public void Bind_NestedCategories_AreApplied()
    {
        var options = Bind(
            ("DataCollection:GraphQL:Document", "false"),
            ("DataCollection:GraphQL:Variables", "false"),
            ("DataCollection:GenAI:Inputs", "false"),
            ("DataCollection:GenAI:Outputs", "false"));

        using (new AssertionScope())
        {
            options.DataCollection!.GraphQL.Document.Should().BeFalse();
            options.DataCollection.GraphQL.Variables.Should().BeFalse();
            options.DataCollection.GenAI.Inputs.Should().BeFalse();
            options.DataCollection.GenAI.Outputs.Should().BeFalse();
        }
    }

    [Theory]
    [InlineData("Off", KeyValueCollectionMode.Off)]
    [InlineData("DenyList", KeyValueCollectionMode.DenyList)]
    [InlineData("AllowList", KeyValueCollectionMode.AllowList)]
    public void Bind_KeyValueMode_IsApplied(string configured, KeyValueCollectionMode expected)
    {
        var options = Bind(("DataCollection:Cookies:Mode", configured));

        options.DataCollection!.Cookies.Mode.Should().Be(expected);
    }

    [Fact]
    public void Bind_KeyValueTerms_AreApplied()
    {
        var options = Bind(
            ("DataCollection:HttpHeaders:Request:Mode", "DenyList"),
            ("DataCollection:HttpHeaders:Request:Terms:0", "forwarded"),
            ("DataCollection:HttpHeaders:Request:Terms:1", "-ip"));

        var request = options.DataCollection!.HttpHeaders.Request;
        request.Mode.Should().Be(KeyValueCollectionMode.DenyList);
        request.Terms.Should().Equal("forwarded", "-ip");
    }

    [Fact]
    public void Bind_ResponseHeaders_AreApplied()
    {
        var options = Bind(("DataCollection:HttpHeaders:Response:Mode", "Off"));

        options.DataCollection!.HttpHeaders.Response.Mode.Should().Be(KeyValueCollectionMode.Off);
    }

    [Fact]
    public void Bind_KeyValueTermsWithoutMode_DefaultsToDenyList()
    {
        var options = Bind(("DataCollection:UrlQueryParams:Terms:0", "session_id"));

        var behavior = options.DataCollection!.UrlQueryParams;
        behavior.Mode.Should().Be(KeyValueCollectionMode.DenyList);
        behavior.Terms.Should().Equal("session_id");
    }

    [Fact]
    public void Bind_HttpBodiesArray_CombinesRecognizedEntries()
    {
        var options = Bind(
            ("DataCollection:HttpBodies:0", "incomingRequest"),
            ("DataCollection:HttpBodies:1", "OutgoingResponse"),
            ("DataCollection:HttpBodies:2", "Typo"));

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
    public void Bind_HttpBodiesEmptyArray_LeavesCategoryAtDefault()
    {
        // A JSON empty array emits the key with an empty value and no children, which the binder surfaces as
        // null rather than an empty list. The spec's "[]" opt-out is therefore not expressible in configuration;
        // "[\"None\"]" is the equivalent. Locked in so a future change to the surrogate has to confront it.
        var options = Bind(("DataCollection:HttpBodies", string.Empty));

        options.DataCollection!.HttpBodies.Should().Be(HttpBodyTypes.All);
    }
}
#endif
