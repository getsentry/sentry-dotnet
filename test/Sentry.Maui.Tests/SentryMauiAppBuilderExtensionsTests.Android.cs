using Microsoft.Maui.LifecycleEvents;
using Sentry.Maui.Internal;
using Sentry.Maui.Tests.Mocks;

namespace Sentry.Maui.Tests;

public partial class SentryMauiAppBuilderExtensionsTests
{
    [Fact]
    public void UseSentry_BindsToApplicationStartupEvent_Android()
    {
        // Arrange
        var application = MockApplication.Create();
        var binder = Substitute.For<MauiEventsBinder>();

        var builder = _fixture.Builder;
        builder.Services.AddSingleton<IApplication>(application);
        builder.Services.AddSingleton(binder);
        builder.UseSentry(ValidDsn);
        using var app = builder.Build();

        var androidApplication = new MockAndroidApplication(application, app.Services);

        // Act
        var lifecycleEventService = app.Services.GetRequiredService<ILifecycleEventService>();
        lifecycleEventService.InvokeEvents<AndroidLifecycle.OnApplicationCreating>
            (nameof(AndroidLifecycle.OnApplicationCreating), del => del.Invoke(androidApplication));

        // Assert
        binder.Received(1).HandleApplicationEvents(application);
    }

    private class MockAndroidApplication : global::Android.App.Application, IPlatformApplication
    {
        public MockAndroidApplication(IApplication application, IServiceProvider services)
        {
            Application = application;
            Services = services;
        }

        public IApplication Application { get; }

        public IServiceProvider Services { get; }
    }
}
