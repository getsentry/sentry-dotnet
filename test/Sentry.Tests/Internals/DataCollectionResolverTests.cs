namespace Sentry.Tests.Internals;

public class DataCollectionResolverTests
{
    private static DataCollectionResolver Resolver(SentryOptions options) => options.DataCollectionResolver;

    [Fact]
    public void Defaults_WithoutSendDefaultPii_CollectNothingOptional()
    {
        var options = new SentryOptions();

        var resolver = Resolver(options);

        using (new AssertionScope())
        {
            resolver.UserInfo.Should().BeFalse();
            resolver.GraphQLDocument.Should().BeFalse();
            resolver.GraphQLVariables.Should().BeFalse();
            resolver.OutgoingRequestBody.Should().BeFalse();
            resolver.IncomingResponseBody.Should().BeFalse();
            resolver.OutgoingResponseBody.Should().BeFalse();
            resolver.Cookies.Mode.Should().Be(KeyValueCollectionMode.Off);
        }
    }

    [Fact]
    public void Defaults_WithSendDefaultPii_CollectEverything()
    {
        var options = new SentryOptions { SendDefaultPii = true };

        var resolver = Resolver(options);

        using (new AssertionScope())
        {
            resolver.UserInfo.Should().BeTrue();
            resolver.GraphQLDocument.Should().BeTrue();
            resolver.GraphQLVariables.Should().BeTrue();
            resolver.GenAIInputs.Should().BeTrue();
            resolver.GenAIOutputs.Should().BeTrue();
            resolver.IncomingRequestBody.Should().BeTrue();
            resolver.OutgoingRequestBody.Should().BeTrue();
            resolver.IncomingResponseBody.Should().BeTrue();
            resolver.OutgoingResponseBody.Should().BeTrue();
            resolver.Cookies.Mode.Should().Be(KeyValueCollectionMode.DenyList);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HeadersAndQueryParams_WithoutDataCollection_AreDenyListRegardlessOfSendDefaultPii(bool sendDefaultPii)
    {
        var options = new SentryOptions { SendDefaultPii = sendDefaultPii };

        var resolver = Resolver(options);

        using (new AssertionScope())
        {
            resolver.RequestHeaders.Mode.Should().Be(KeyValueCollectionMode.DenyList);
            resolver.RequestHeaders.Terms.Should().BeEmpty();
            resolver.ResponseHeaders.Mode.Should().Be(KeyValueCollectionMode.DenyList);
            resolver.ResponseHeaders.Terms.Should().BeEmpty();
            resolver.UrlQueryParams.Mode.Should().Be(KeyValueCollectionMode.DenyList);
            resolver.UrlQueryParams.Terms.Should().BeEmpty();
        }
    }

    [Theory]
    [InlineData(false, KeyValueCollectionMode.Off)]
    [InlineData(true, KeyValueCollectionMode.DenyList)]
    public void HeadersWithLegacyPiiGate_WithoutDataCollection_FollowSendDefaultPii(bool sendDefaultPii, KeyValueCollectionMode expected)
    {
        var options = new SentryOptions { SendDefaultPii = sendDefaultPii };

        var resolver = Resolver(options);

        using (new AssertionScope())
        {
            resolver.RequestHeadersWithLegacyPiiGate.Mode.Should().Be(expected);
            resolver.RequestHeadersWithLegacyPiiGate.Terms.Should().BeEmpty();
            resolver.ResponseHeadersWithLegacyPiiGate.Mode.Should().Be(expected);
            resolver.ResponseHeadersWithLegacyPiiGate.Terms.Should().BeEmpty();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HeadersWithLegacyPiiGate_ConfiguredDataCollection_IgnoresSendDefaultPii(bool sendDefaultPii)
    {
        var options = new SentryOptions { SendDefaultPii = sendDefaultPii };
        options.ConfigureDataCollection(dataCollection =>
        {
            dataCollection.HttpHeaders.Request = KeyValueCollectionBehavior.AllowList("accept");
            dataCollection.HttpHeaders.Response = sendDefaultPii
                ? KeyValueCollectionBehavior.Off
                : KeyValueCollectionBehavior.DenyList("x-tenant-id");
        });

        var resolver = Resolver(options);

        using (new AssertionScope())
        {
            resolver.RequestHeadersWithLegacyPiiGate.Mode.Should().Be(KeyValueCollectionMode.AllowList);
            resolver.RequestHeadersWithLegacyPiiGate.Terms.Should().Equal("accept");
            resolver.ResponseHeadersWithLegacyPiiGate.Mode.Should().Be(
                sendDefaultPii ? KeyValueCollectionMode.Off : KeyValueCollectionMode.DenyList);
        }
    }

    [Fact]
    public void HeadersWithLegacyPiiGate_ConfiguredDataCollectionDefaults_AreDenyList()
    {
        var options = new SentryOptions();
        options.ConfigureDataCollection(_ => { });

        var resolver = Resolver(options);

        using (new AssertionScope())
        {
            resolver.RequestHeadersWithLegacyPiiGate.Mode.Should().Be(KeyValueCollectionMode.DenyList);
            resolver.ResponseHeadersWithLegacyPiiGate.Mode.Should().Be(KeyValueCollectionMode.DenyList);
        }
    }

    [Theory]
    [InlineData(false, KeyValueCollectionMode.Off)]
    [InlineData(true, KeyValueCollectionMode.DenyList)]
    public void UrlFragmentWithLegacyPiiGate_WithoutDataCollection_FollowsSendDefaultPii(bool sendDefaultPii, KeyValueCollectionMode expected)
    {
        var options = new SentryOptions { SendDefaultPii = sendDefaultPii };

        var fragment = Resolver(options).UrlFragmentWithLegacyPiiGate;

        using (new AssertionScope())
        {
            fragment.Mode.Should().Be(expected);
            fragment.Terms.Should().BeEmpty();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UrlFragmentWithLegacyPiiGate_ConfiguredDataCollection_FollowsUrlQueryParams(bool sendDefaultPii)
    {
        var options = new SentryOptions { SendDefaultPii = sendDefaultPii };
        options.ConfigureDataCollection(dataCollection =>
            dataCollection.UrlQueryParams = KeyValueCollectionBehavior.AllowList("page"));

        var fragment = Resolver(options).UrlFragmentWithLegacyPiiGate;

        using (new AssertionScope())
        {
            fragment.Mode.Should().Be(KeyValueCollectionMode.AllowList);
            fragment.Terms.Should().Equal("page");
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void IncomingRequestBody_WithoutDataCollection_IsCollectedRegardlessOfSendDefaultPii(bool sendDefaultPii)
    {
        var options = new SentryOptions { SendDefaultPii = sendDefaultPii };

        Resolver(options).IncomingRequestBody.Should().BeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GenAI_WithoutDataCollection_IsCollectedRegardlessOfSendDefaultPii(bool sendDefaultPii)
    {
        var options = new SentryOptions { SendDefaultPii = sendDefaultPii };

        var resolver = Resolver(options);

        using (new AssertionScope())
        {
            resolver.GenAIInputs.Should().BeTrue();
            resolver.GenAIOutputs.Should().BeTrue();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SanitizeDbStatements_WithoutDataCollection_IsFalseRegardlessOfSendDefaultPii(bool sendDefaultPii)
    {
        var options = new SentryOptions { SendDefaultPii = sendDefaultPii };

        Resolver(options).SanitizeDbStatements.Should().BeFalse();
    }

    [Fact]
    public void SanitizeDbStatements_WithDataCollection_IsTrue()
    {
        var options = new SentryOptions();
        options.ConfigureDataCollection(_ => { });

        Resolver(options).SanitizeDbStatements.Should().BeTrue();
    }

    [Fact]
    public void ConfiguredDataCollection_WithoutSendDefaultPii_UsesSpecDefaults()
    {
        var options = new SentryOptions();
        options.ConfigureDataCollection(_ => { });

        var resolver = Resolver(options);

        using (new AssertionScope())
        {
            resolver.UserInfo.Should().BeTrue();
            resolver.GraphQLDocument.Should().BeTrue();
            resolver.GraphQLVariables.Should().BeTrue();
            resolver.GenAIInputs.Should().BeTrue();
            resolver.GenAIOutputs.Should().BeTrue();
            resolver.IncomingRequestBody.Should().BeTrue();
            resolver.OutgoingRequestBody.Should().BeTrue();
            resolver.IncomingResponseBody.Should().BeTrue();
            resolver.OutgoingResponseBody.Should().BeTrue();
            resolver.Cookies.Mode.Should().Be(KeyValueCollectionMode.DenyList);
        }
    }

    [Fact]
    public void ConfiguredDataCollection_WithSendDefaultPii_IgnoresSendDefaultPii()
    {
        var options = new SentryOptions { SendDefaultPii = true };
        options.ConfigureDataCollection(dataCollection =>
        {
            dataCollection.HttpBodies = HttpBodyTypes.None;
            dataCollection.Cookies = KeyValueCollectionBehavior.Off;
            dataCollection.GraphQL.Document = false;
            dataCollection.GenAI.Outputs = false;
        });

        var resolver = Resolver(options);

        using (new AssertionScope())
        {
            resolver.UserInfo.Should().BeTrue();
            resolver.GraphQLDocument.Should().BeFalse();
            resolver.GraphQLVariables.Should().BeTrue();
            resolver.GenAIInputs.Should().BeTrue();
            resolver.GenAIOutputs.Should().BeFalse();
            resolver.IncomingRequestBody.Should().BeFalse();
            resolver.OutgoingResponseBody.Should().BeFalse();
            resolver.Cookies.Mode.Should().Be(KeyValueCollectionMode.Off);
        }
    }

    [Theory]
    [InlineData(HttpBodyTypes.IncomingRequest)]
    [InlineData(HttpBodyTypes.OutgoingRequest)]
    [InlineData(HttpBodyTypes.IncomingResponse)]
    [InlineData(HttpBodyTypes.OutgoingResponse)]
    public void HttpBodies_ConfiguredForOneDirection_CollectsOnlyThatDirection(HttpBodyTypes bodyType)
    {
        var options = new SentryOptions();
        options.ConfigureDataCollection(dataCollection => dataCollection.HttpBodies = bodyType);

        var resolver = Resolver(options);

        using (new AssertionScope())
        {
            resolver.IncomingRequestBody.Should().Be(bodyType == HttpBodyTypes.IncomingRequest);
            resolver.OutgoingRequestBody.Should().Be(bodyType == HttpBodyTypes.OutgoingRequest);
            resolver.IncomingResponseBody.Should().Be(bodyType == HttpBodyTypes.IncomingResponse);
            resolver.OutgoingResponseBody.Should().Be(bodyType == HttpBodyTypes.OutgoingResponse);
        }
    }

    [Fact]
    public void KeyValueCategories_ConfiguredIndividually_AreReadBack()
    {
        var options = new SentryOptions();
        options.ConfigureDataCollection(dataCollection =>
        {
            dataCollection.Cookies = KeyValueCollectionBehavior.AllowList("theme");
            dataCollection.HttpHeaders.Request = KeyValueCollectionBehavior.DenyList("x-tenant-id");
            dataCollection.HttpHeaders.Response = KeyValueCollectionBehavior.Off;
            dataCollection.UrlQueryParams = KeyValueCollectionBehavior.AllowList("page");
        });

        var resolver = Resolver(options);

        using (new AssertionScope())
        {
            resolver.Cookies.Mode.Should().Be(KeyValueCollectionMode.AllowList);
            resolver.Cookies.Terms.Should().Equal("theme");
            resolver.RequestHeaders.Mode.Should().Be(KeyValueCollectionMode.DenyList);
            resolver.RequestHeaders.Terms.Should().Equal("x-tenant-id");
            resolver.ResponseHeaders.Mode.Should().Be(KeyValueCollectionMode.Off);
            resolver.UrlQueryParams.Mode.Should().Be(KeyValueCollectionMode.AllowList);
            resolver.UrlQueryParams.Terms.Should().Equal("page");
        }
    }

    [Fact]
    public void SendDefaultPii_ChangedAfterFirstRead_IsPickedUp()
    {
        var options = new SentryOptions();
        var resolver = Resolver(options);
        resolver.UserInfo.Should().BeFalse();

        options.SendDefaultPii = true;

        resolver.UserInfo.Should().BeTrue();
    }

    [Fact]
    public void DataCollection_ConfiguredAfterFirstRead_IsPickedUp()
    {
        var options = new SentryOptions { SendDefaultPii = true };
        var resolver = Resolver(options);
        resolver.UserInfo.Should().BeTrue();

        options.ConfigureDataCollection(dataCollection => dataCollection.UserInfo = false);

        resolver.UserInfo.Should().BeFalse();
    }
}
