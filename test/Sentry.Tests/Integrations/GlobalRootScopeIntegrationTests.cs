using Sentry.Integrations;

namespace Sentry.Tests.Integrations;

public class GlobalRootScopeIntegrationTests
{
    [Fact]
    public void Register_GlobalModeDisabled_DoesNotConfigureScope()
    {
        // Arrange
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            IsGlobalModeEnabled = false,
            AutoSessionTracking = false
        };
        var scope = new Scope();

        var hub = Substitute.For<IHub>();
        hub.SubstituteConfigureScope(scope);
        var integration = new GlobalRootScopeIntegration();

        // Act
        integration.Register(hub, options);

        // Assert
        hub.DidNotReceive().ConfigureScope(Arg.Any<Action<Scope>>());
        scope.User.Id.Should().BeNull();
    }

    [Fact]
    public void Register_GlobalModeEnabled_SetsInstallationIdOnRootScope()
    {
        // Arrange
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            IsGlobalModeEnabled = true,
            AutoSessionTracking = false
        };
        var scope = new Scope();

        var hub = Substitute.For<IHub>();
        hub.SubstituteConfigureScope(scope);
        var integration = new GlobalRootScopeIntegration();

        // Act
        integration.Register(hub, options);

        // Assert
        hub.Received(1).ConfigureScope(Arg.Any<Action<Scope>>());
        scope.User.Id.Should().Be(options.InstallationId);
    }

    [Fact]
    public void Register_GlobalModeEnabled_DoesNotOverwriteExistingUserId()
    {
        // Arrange
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            IsGlobalModeEnabled = true,
            AutoSessionTracking = false
        };
        var oldId = "old-id";
        var scope = new Scope
        {
            User =
            {
                Id = oldId
            }
        };

        var hub = Substitute.For<IHub>();
        hub.SubstituteConfigureScope(scope);
        var integration = new GlobalRootScopeIntegration();

        // Act
        integration.Register(hub, options);

        // Assert
        hub.Received(1).ConfigureScope(Arg.Any<Action<Scope>>());
        scope.User.Id.Should().Be(oldId);
    }

    [Fact]
    public void Enricher_GlobalModeEnabled_DoesNotSetInstallationId()
    {
        // Verify the enricher no longer sets User.Id when global mode is enabled,
        // ensuring users can clear the User.Id set by GlobalRootScopeIntegration.
        var options = new SentryOptions { IsGlobalModeEnabled = true };
        var enricher = new Sentry.Internal.Enricher(options);

        var eventLike = Substitute.For<IEventLike>();
        eventLike.Sdk.Returns(new SdkVersion());
        eventLike.User = new SentryUser();
        eventLike.Contexts = new SentryContexts();

        enricher.Apply(eventLike);

        eventLike.User.Id.Should().BeNull();
    }

    [Fact]
    public void Enricher_GlobalModeDisabled_SetsInstallationIdAsFallback()
    {
        // Verify the enricher still sets User.Id when global mode is disabled (e.g. ASP.NET Core).
        var options = new SentryOptions { IsGlobalModeEnabled = false };
        var enricher = new Sentry.Internal.Enricher(options);

        var eventLike = Substitute.For<IEventLike>();
        eventLike.Sdk.Returns(new SdkVersion());
        eventLike.User = new SentryUser();
        eventLike.Contexts = new SentryContexts();

        enricher.Apply(eventLike);

        eventLike.User.Id.Should().Be(options.InstallationId);
    }
}
