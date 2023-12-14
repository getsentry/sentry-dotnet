using Microsoft.Extensions.Options;
using Sentry.Internal.Http;

namespace Sentry.Maui.Tests;

public class SentryMauiScreenshotTests
{
    private class Fixture
    {
        public MauiAppBuilder Builder { get; }

        public Fixture()
        {
            var builder = MauiApp.CreateBuilder();
            builder.Services.AddSingleton(Substitute.For<IApplication>());

            builder.Services.Configure<SentryMauiOptions>(options =>
            {
                options.Transport = new FakeTransport();
                options.Dsn = ValidDsn;
                options.AttachScreenshots = true;
                options.AutoSessionTracking = false; //Get rid of session envelope for easier Assert
                options.CacheDirectoryPath = null;   //Do not wrap our FakeTransport with a caching transport
            });

            Builder = builder;
        }
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public async Task CaptureException_WhenAttachScreenshots_ContainsScreenshotAttachmentAsync()
    {
        // Arrange
        var builder = _fixture.Builder.UseSentry();

        // Act
        using var app = builder.Build();
        var client = app.Services.GetRequiredService<ISentryClient>();
        client.CaptureException(new Exception());
        await client.FlushAsync();

        var options = app.Services.GetRequiredService<IOptions<SentryMauiOptions>>().Value;
        var transport = options.Transport as FakeTransport;

        var envelopeItem = transport.GetSentEnvelopes()[0].Items.FirstOrDefault(item => item.TryGetType() == "attachment");

        // Assert
        envelopeItem.Should().NotBeNull();
        envelopeItem.TryGetFileName().Should().Be("screenshot.jpg");
    }

    [Fact]
    public async Task CaptureException_RemoveScreenshot_NotContainsScreenshotAttachmentAsync()
    {
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
        client.CaptureException(new Exception());
        await client.FlushAsync();

        var options = app.Services.GetRequiredService<IOptions<SentryMauiOptions>>().Value;
        var transport = options.Transport as FakeTransport;

        var envelopeItem = transport.GetSentEnvelopes()[0].Items.FirstOrDefault(item => item.TryGetType() == "attachment");

        // Assert
        envelopeItem.Should().BeNull();
    }
}
