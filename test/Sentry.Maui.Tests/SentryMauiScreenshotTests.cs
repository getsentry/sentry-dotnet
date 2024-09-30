using Microsoft.Extensions.Options;

namespace Sentry.Maui.Tests;

public class SentryMauiScreenshotTests
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
                options.AttachScreenshot = true;
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

#if __MOBILE__
    [SkippableFact]
    public async Task CaptureException_WhenAttachScreenshots_ContainsScreenshotAttachmentAsync()
    {
        Skip.If(TestEnvironment.IsCI, "This test is flaky in CI");

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
        // On Android this can fail due to MAUI not being able to detect current Activity, see issue https://github.com/dotnet/maui/issues/19450
        if (_fixture.Logger.Entries.Any(entry => entry.Level == SentryLevel.Error && entry.Exception is NullReferenceException))
        {
            envelopeItem.Should().BeNull();
        }
        else
        {
            envelopeItem.Should().NotBeNull();
            envelopeItem!.TryGetFileName().Should().Be("screenshot.jpg");
        }
    }

    [SkippableFact]
    public async Task CaptureException_RemoveScreenshot_NotContainsScreenshotAttachmentAsync()
    {
        Skip.If(TestEnvironment.IsCI, "This test is flaky in CI");

        // Arrange
        var builder = _fixture.Builder.UseSentry(options => options.SetBeforeSend((e, hint) =>
            {
                hint.Attachments.Clear();
                return e;
            }
        ));

        // Act
        using var app = builder.Build();
        var client = app.Services.GetRequiredService<ISentryClient>();
        var sentryId = client.CaptureException(new Exception());
        await client.FlushAsync();

        var options = app.Services.GetRequiredService<IOptions<SentryMauiOptions>>().Value;

        var envelope = _fixture.Transport.GetSentEnvelopes().FirstOrDefault(e => e.TryGetEventId() == sentryId);
        envelope.Should().NotBeNull();

        var envelopeItem = envelope!.Items.FirstOrDefault(item => item.TryGetType() == "attachment");

        // Assert
        envelopeItem.Should().BeNull();
    }
#endif
}
