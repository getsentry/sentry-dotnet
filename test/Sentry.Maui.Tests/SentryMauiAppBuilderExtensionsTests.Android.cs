using Microsoft.Maui.LifecycleEvents;
using Sentry.Maui.Internal;

namespace Sentry.Maui.Tests;

public partial class SentryMauiAppBuilderExtensionsTests
{
    [Fact]
    public void UseSentry_BindsToLifecycleEvents_Android()
    {
        // Arrange
        var binder = Substitute.For<IMauiEventsBinder>();

        var builder = _fixture.Builder;
        builder.Services.AddSingleton(binder);

        // Act
        builder.UseSentry(ValidDsn);
        using var app = builder.Build();
        var application = new FakeAndroidApplication(app.Services);

        var lifecycleEventService = app.Services.GetRequiredService<ILifecycleEventService>();
        lifecycleEventService.InvokeEvents<AndroidLifecycle.OnApplicationCreating>
            (nameof(AndroidLifecycle.OnApplicationCreating), del => del.Invoke(application));

        // Assert
        binder.Received(1).BindMauiEvents();
    }

    private class FakeAndroidApplication : global::Android.App.Application, IPlatformApplication
    {
        public FakeAndroidApplication(IServiceProvider services)
        {
            Services = services;
        }

        public IServiceProvider Services { get; }
        public IApplication Application => null;
    }
}
