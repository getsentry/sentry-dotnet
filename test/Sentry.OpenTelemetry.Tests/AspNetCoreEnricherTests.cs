namespace Sentry.OpenTelemetry.Tests;

public class AspNetCoreEnricherTests
{
    private readonly Scope _scope = new();
    private readonly SentryUser _user = new() { Id = "foo" };

    private void Enrich(SentryOptions options)
    {
        var hub = Substitute.For<IHub>();
        hub.SubstituteConfigureScope(_scope);

        var userFactory = Substitute.For<ISentryUserFactory>();
        userFactory.Create().Returns(_user);

        var enricher = new AspNetCoreEnricher(userFactory);
        enricher.Enrich(null!, null!, hub, options);
    }

    [Fact]
    public void Enrich_SendDefaultPii_UserOnScope()
    {
        // Arrange
        var options = new SentryOptions { SendDefaultPii = true };

        // Act
        Enrich(options);

        // Assert
        _scope.HasUser().Should().BeTrue();
        _scope.User.Should().Be(_user);
    }

    [Fact]
    public void Enrich_SendDefaultPiiFalse_NoUserOnScope()
    {
        // Arrange
        var options = new SentryOptions { SendDefaultPii = false };

        // Act
        Enrich(options);

        // Assert
        _scope.HasUser().Should().BeFalse();
    }

    [Fact]
    public void Enrich_NoOptions_NoUserOnScope()
    {
        // Act
        Enrich(null!);

        // Assert
        _scope.HasUser().Should().BeFalse();
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Enrich_DataCollectionConfigured_UserInfoTakesPrecedenceOverSendDefaultPii(bool sendDefaultPii, bool userInfo)
    {
        // Arrange
        var options = new SentryOptions { SendDefaultPii = sendDefaultPii };
        options.ConfigureDataCollection(dataCollection => dataCollection.UserInfo = userInfo);

        // Act
        Enrich(options);

        // Assert
        _scope.HasUser().Should().Be(userInfo);
    }
}
