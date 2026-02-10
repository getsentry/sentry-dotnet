using System.Text.Json;
using Microsoft.Playwright;

namespace Sentry.AspNetCore.Blazor.WebAssembly.PlaywrightTests;

public class NavigationBreadcrumbTests : IAsyncLifetime
{
    private readonly BlazorWasmTestApp _app = new();
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;

    public async Task InitializeAsync()
    {
        await _app.StartAsync();

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
    }

    public async Task DisposeAsync()
    {
        await _browser.DisposeAsync();
        _playwright.Dispose();
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task Navigation_CreatesBreadcrumbs_WithCorrectFromAndTo()
    {
        var page = await _browser.NewPageAsync();

        // Collect all intercepted envelopes
        var envelopes = new List<string>();
        var envelopeReceived = new TaskCompletionSource<string>();

        await page.RouteAsync("**/api/0/envelope/**", async route =>
        {
            var body = route.Request.PostData;
            if (body != null)
            {
                envelopes.Add(body);
                if (body.Contains("\"breadcrumbs\""))
                {
                    envelopeReceived.TrySetResult(body);
                }
            }
            await route.FulfillAsync(new RouteFulfillOptions
            {
                Status = 200,
                ContentType = "application/json",
                Body = "{}"
            });
        });

        // 1. Navigate to app root
        await page.GotoAsync(_app.BaseUrl);
        await page.WaitForSelectorAsync("#page-title");

        // 2. Navigate to /second (creates first navigation breadcrumb: / -> /second)
        await page.ClickAsync("#nav-second");
        await page.WaitForSelectorAsync("h1:has-text('Second Page')");

        // 3. Navigate to /trigger-capture (creates second breadcrumb: /second -> /trigger-capture)
        await page.ClickAsync("#nav-trigger");
        await page.WaitForSelectorAsync("h1:has-text('Trigger Capture')");

        // 4. Click button to trigger SentrySdk.CaptureMessage — sends event with breadcrumbs
        await page.ClickAsync("#btn-capture");

        // 5. Wait for the envelope containing breadcrumbs
        var envelopeBody = await envelopeReceived.Task.WaitAsync(TimeSpan.FromSeconds(10));

        // 6. Parse and verify
        var eventPayload = SentryEnvelopeParser.ExtractEventFromEnvelope(envelopeBody);
        eventPayload.Should().NotBeNull("expected an event payload in the Sentry envelope");

        var breadcrumbs = eventPayload!.Value.GetProperty("breadcrumbs").EnumerateArray().ToList();

        var navBreadcrumbs = breadcrumbs
            .Where(b =>
                b.TryGetProperty("type", out var t) && t.GetString() == "navigation" &&
                b.TryGetProperty("category", out var c) && c.GetString() == "navigation")
            .ToList();

        navBreadcrumbs.Should().HaveCount(2, "expected two navigation breadcrumbs (/ -> /second -> /trigger-capture)");

        // First navigation: / -> /second
        var first = navBreadcrumbs[0];
        first.GetProperty("data").GetProperty("from").GetString().Should().Be("/");
        first.GetProperty("data").GetProperty("to").GetString().Should().Be("/second");

        // Second navigation: /second -> /trigger-capture
        var second = navBreadcrumbs[1];
        second.GetProperty("data").GetProperty("from").GetString().Should().Be("/second");
        second.GetProperty("data").GetProperty("to").GetString().Should().Be("/trigger-capture");
    }
}
