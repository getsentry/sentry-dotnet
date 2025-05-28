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
    [SkippableFact]
    public async Task CaptureException_WhenAttachLogcats_DefaultAsync()
    {
        // Arrange
        var builder = _fixture.Builder.UseSentry();

        // Act
        using var app = builder.Build();
        var client = app.Services.GetRequiredService<ISentryClient>();
        var sentryId = client.CaptureException(new Exception());
        await client.FlushAsync();

        var options = app.Services.GetRequiredService<IOptions<SentryMauiOptions>>().Value;

        var envelope = _fixture.Transport.GetSentEnvelopes().FirstOrDefault(e => e.TryGetEventId() == sentryId);
        envelope.Should().NotBeNull("Envelope with sentryId {0} should be sent", sentryId);
        var envelopeItem = envelope!.Items.FirstOrDefault(item => item.TryGetType() == "attachment");

        // Assert
        envelopeItem.Should().BeNull();
    }

    [SkippableFact]
    public async Task CaptureException_WhenAttachLogcats_AllExceptionsAsync()
    {

        // Arrange
        var builder = _fixture.Builder.UseSentry(options =>
        {
            options.Android.LogCatIntegration = Android.LogCatIntegrationType.All;
        });

        // Act
        using var app = builder.Build();
        var client = app.Services.GetRequiredService<ISentryClient>();
        var sentryId = client.CaptureException(new Exception());
        await client.FlushAsync();

        var options = app.Services.GetRequiredService<IOptions<SentryMauiOptions>>().Value;

        var envelope = _fixture.Transport.GetSentEnvelopes().FirstOrDefault(e => e.TryGetEventId() == sentryId);
        envelope.Should().NotBeNull("Envelope with sentryId {0} should be sent", sentryId);

        // Assert
        envelope!.Items.Any(env => env.TryGetFileName() == "logcat.log").Should().BeTrue();
    }

    [SkippableFact]
    public async Task CaptureException_WhenAttachLogcats_UnhandledExceptionsAsync()
    {

        // Arrange
        var builder = _fixture.Builder.UseSentry(options =>
        {
            options.Android.LogCatIntegration = Android.LogCatIntegrationType.Unhandled;
        });

        // Act
        using var app = builder.Build();
        var client = app.Services.GetRequiredService<ISentryClient>();
        var sentryId = client.CaptureException(BuildUnhandledException());

        await client.FlushAsync();

        var envelope = _fixture.Transport.GetSentEnvelopes().FirstOrDefault(e => e.TryGetEventId() == sentryId);
        envelope.Should().NotBeNull("Envelope with sentryId {0} should be sent", sentryId);
        var envelopeItem = envelope!.Items.FirstOrDefault(item => item.TryGetType() == "attachment");

        // Assert
        envelopeItem.Should().NotBeNull();
        envelopeItem!.TryGetFileName().Should().Be("logcat.log");
    }

    [SkippableFact]
    public async Task CaptureException_WhenAttachLogcats_HandledExceptionsAsync()
    {

        // Arrange
        var builder = _fixture.Builder.UseSentry(options =>
        {
            options.Android.LogCatIntegration = Android.LogCatIntegrationType.Unhandled;
        });

        // Act
        using var app = builder.Build();
        var client = app.Services.GetRequiredService<ISentryClient>();
        var sentryId = client.CaptureException(new Exception());

        await client.FlushAsync();

        var envelope = _fixture.Transport.GetSentEnvelopes().FirstOrDefault(e => e.TryGetEventId() == sentryId);
        envelope.Should().NotBeNull("Envelope with sentryId {0} should be sent", sentryId);

        // Assert
        envelope!.Items.Any(item => item.TryGetType() == "attachment").Should().BeFalse();
    }

    [SkippableFact]
    public async Task CaptureException_WhenAttachLogcats_ErrorsAsync()
    {

        // Arrange
        var builder = _fixture.Builder.UseSentry(options =>
        {
            options.Android.LogCatIntegration = Android.LogCatIntegrationType.Errors;
        });

        // Act
        using var app = builder.Build();
        var client = app.Services.GetRequiredService<ISentryClient>();
        var sentryId = client.CaptureException(new Exception());
        await client.FlushAsync();

        var options = app.Services.GetRequiredService<IOptions<SentryMauiOptions>>().Value;

        var envelope = _fixture.Transport.GetSentEnvelopes().FirstOrDefault(e => e.TryGetEventId() == sentryId);
        envelope.Should().NotBeNull("Envelope with sentryId {0} should be sent", sentryId);
        var envelopeItem = envelope!.Items.FirstOrDefault(item => item.TryGetType() == "attachment");

        // Assert
        envelopeItem.Should().NotBeNull();
        envelopeItem!.TryGetFileName().Should().Be("logcat.log");
    }

    [SkippableFact]
    public async Task CaptureException_WhenAttachLogcats_NoneAsync()
    {

        // Arrange
        var builder = _fixture.Builder.UseSentry(options =>
        {
            options.Android.LogCatIntegration = Android.LogCatIntegrationType.None;
        });

        // Act
        using var app = builder.Build();
        var client = app.Services.GetRequiredService<ISentryClient>();
        var sentryId = client.CaptureException(new Exception());
        await client.FlushAsync();

        var options = app.Services.GetRequiredService<IOptions<SentryMauiOptions>>().Value;

        var envelope = _fixture.Transport.GetSentEnvelopes().FirstOrDefault(e => e.TryGetEventId() == sentryId);
        envelope.Should().NotBeNull("Envelope with sentryId {0} should be sent", sentryId);
        var envelopeItem = envelope!.Items.FirstOrDefault(item => item.TryGetType() == "attachment");

        // Assert
        envelopeItem.Should().BeNull();
    }

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

    private static Exception BuildUnhandledException()
    {
        try
        {
            // Throwing will put a stack trace on the exception
            throw new Exception("Error");
        }
        catch (Exception exception)
        {
            // Add extra data to test fully
            exception.Data[Mechanism.HandledKey] = false;
            exception.Data[Mechanism.MechanismKey] = "AppDomain.UnhandledException";
            exception.Data["foo"] = "bar";
            return exception;
        }
    }
#endif
}
