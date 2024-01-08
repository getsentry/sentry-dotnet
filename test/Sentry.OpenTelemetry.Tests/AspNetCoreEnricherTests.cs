namespace Sentry.OpenTelemetry.Tests;

public class AspNetCoreEnricherTests
{
    [Fact]
    public void Enrich_SendDefaultPii_UserOnScope()
    {
        // Arrange
        var scope = new Scope();
        var options = new SentryOptions { SendDefaultPii = true };
        var hub = Substitute.For<IHub>();
        hub.ConfigureScope(Arg.Do<Action<Scope>>(action => action(scope)));

        var user = new SentryUser{ Id = "foo" };
        var userFactory = Substitute.For<ISentryUserFactory>();
        userFactory.Create().Returns(user);

        var enricher = new AspNetCoreEnricher(userFactory);

        // Act
        enricher.Enrich(null!, null!, hub, options);

        // Assert
        scope.HasUser().Should().BeTrue();
        scope.User.Should().Be(user);
    }

    [Fact]
    public void Enrich_SendDefaultPiiFalse_NoUserOnScope()
    {
        // Arrange
        var scope = new Scope();
        var originalUser = scope.User;
        var options = new SentryOptions { SendDefaultPii = false };
        var hub = Substitute.For<IHub>();
        hub.ConfigureScope(Arg.Do<Action<Scope>>(action => action(scope)));

        var userFactory = Substitute.For<ISentryUserFactory>();
        var enricher = new AspNetCoreEnricher(userFactory);

        // Act
        enricher.Enrich(null!, null!, hub, options);

        // Assert
        scope.HasUser().Should().BeFalse();
        scope.User.Should().Be(originalUser);
    }
}
