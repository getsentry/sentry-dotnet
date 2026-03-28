using Sentry.Integrations;

namespace Sentry.Tests.Integrations;

public class GlobalRootScopeIntegrationTests
{
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

        var hub = Substitute.For<IHub>();
        var integration = new GlobalRootScopeIntegration();

        // Act
        integration.Register(hub, options);

        // Assert
        hub.Received(1).ConfigureScope(Arg.Any<Action<Scope>>());
    }

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

        var hub = Substitute.For<IHub>();
        var integration = new GlobalRootScopeIntegration();

        // Act
        integration.Register(hub, options);

        // Assert
        hub.DidNotReceive().ConfigureScope(Arg.Any<Action<Scope>>());
    }

    [Fact]
    public void Register_GlobalModeEnabled_SetsUserIdFromInstallationId()
    {
        // Arrange
        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            IsGlobalModeEnabled = true,
            AutoSessionTracking = false
        };

        // Capture the action passed to ConfigureScope
        Action<Scope> capturedAction = null;
        var hub = Substitute.For<IHub>();
        hub.When(h => h.ConfigureScope(Arg.Any<Action<Scope>>()))
           .Do(call => capturedAction = call.Arg<Action<Scope>>());

        var integration = new GlobalRootScopeIntegration();
        integration.Register(hub, options);

        // Apply the captured action to a real scope
        var scope = new Scope(options);

        capturedAction.Should().NotBeNull();
        capturedAction(scope);

        // The scope's User.Id should be set to the InstallationId
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

        // Capture the action passed to ConfigureScope
        Action<Scope> capturedAction = null;
        var hub = Substitute.For<IHub>();
        hub.When(h => h.ConfigureScope(Arg.Any<Action<Scope>>()))
           .Do(call => capturedAction = call.Arg<Action<Scope>>());

        var integration = new GlobalRootScopeIntegration();
        integration.Register(hub, options);

        // Apply the captured action to a scope that already has a User.Id
        var scope = new Scope(options);
        const string existingUserId = "my-custom-user-id";
        scope.User.Id = existingUserId;

        capturedAction.Should().NotBeNull();
        capturedAction(scope);

        // The existing User.Id should not be overwritten
        scope.User.Id.Should().Be(existingUserId);
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
