using Microsoft.Extensions.Options;
#if ANDROID
using Sentry.Android;
#endif

namespace Sentry.Maui.Tests;

public class SentryMauiLogcatsTests
{
    private class Fixture
    {
        public MauiAppBuilder Builder { get; }
        public FakeTransport Transport { get; private set; } = new FakeTransport();
        public InMemoryDiagnosticLogger Logger { get; private set; } = new InMemoryDiagnosticLogger();

        public Fixture()
        {
            var builder = MauiApp.CreateBuilder();
            builder.Services.AddSingleton(Substitute.For<IApplication>());

            builder.Services.Configure<SentryMauiOptions>(options =>
            {
                options.Transport = Transport;
                options.Dsn = ValidDsn;
                options.AttachScreenshot = false; //Disable the screenshot attachment to have the logcat as primary attachment
                options.Debug = true;
                options.DiagnosticLogger = Logger;
                options.AutoSessionTracking = false; //Get rid of session envelope for easier Assert
                options.CacheDirectoryPath = null;   //Do not wrap our FakeTransport with a caching transport
                options.FlushTimeout = TimeSpan.FromSeconds(10);
            });
            Builder = builder;
        }
    }

    private readonly Fixture _fixture = new();

#if ANDROID
    [Fact]
    public void CaptureException_CheckLogcatType()
    {
        var builder = _fixture.Builder.UseSentry(options =>
        {
            options.Android.LogCatIntegration = Android.LogCatIntegrationType.All;
        });

        // Arrange
        var processor = Substitute.For<ISentryEventProcessorWithHint>();
        using var app = builder.Build();
        SentryHint hint = null;
        var options = app.Services.GetRequiredService<IOptions<SentryMauiOptions>>().Value;

        var scope = new Scope(options);

        // Act
        processor.Process(Arg.Any<SentryEvent>(), Arg.Do<SentryHint>(h => hint = h)).Returns(new SentryEvent());
        options.AddEventProcessor(processor);

        _ = new SentryClient(options).CaptureEvent(new SentryEvent(), scope);


        // Assert
        hint.Should().NotBeNull();
        hint.Attachments.First().ContentType.Should().Be("text/plain", hint.Attachments.First().ContentType);
    }
#endif
}
