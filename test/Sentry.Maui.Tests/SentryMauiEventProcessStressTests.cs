using Microsoft.Extensions.Options;
#if ANDROID
using Sentry.Android;
using Sentry.JavaSdk.Protocol;
#endif

namespace Sentry.Maui.Tests;

public class SentryMauiEventProcessStressTests
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
    public async void CaptureExceptions()
    {
        var builder = _fixture.Builder.UseSentry(options =>
        {
            options.Debug = false;
            options.AutoSessionTracking = true;
            options.IsGlobalModeEnabled = false;
            options.StackTraceMode = StackTraceMode.Enhanced;
            options.DiagnosticLevel = SentryLevel.Debug;
            options.TracesSampleRate = 1.0;
            options.SampleRate = 1.0F;
            options.MaxBreadcrumbs = 200;
            options.CreateElementEventsBreadcrumbs = false;
            options.IncludeBackgroundingStateInBreadcrumbs = false;
            options.IncludeTextInBreadcrumbs = false;
            options.IncludeTitleInBreadcrumbs = false;
            options.SendDefaultPii = false;
            options.AttachScreenshot = false;
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
        var logCount = 200;
        List<SentryId> eventIds = new List<SentryId>();
        List<Task> tasks = new List<Task>();

        for (int i = 0; i < logCount; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                eventIds.Add(SentrySdk.CaptureException(new NotImplementedException("Sample Exception " + i)));
            }));
        }
        await Task.WhenAll(tasks);

        var envelopes = _fixture.Transport.GetSentEnvelopes().Where(e => eventIds.Contains(e.TryGetEventId()?? new SentryId()));

        // Assert
        envelopes.Should().NotBeEmpty("eventIds.Count: {0} ", eventIds.Count);
    }
#endif
}
