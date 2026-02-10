using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Sentry.AspNetCore.Blazor.WebAssembly.Internal;

namespace Sentry.AspNetCore.Blazor.WebAssembly.Tests;

public class BlazorWasmOptionsSetupTests : IDisposable
{
    private readonly FakeNavigationManager _navigationManager;
    private readonly BlazorWasmOptionsSetup _sut;
    private readonly IDisposable _sentryInit;

    public BlazorWasmOptionsSetupTests()
    {
        _navigationManager = new FakeNavigationManager(
            baseUri: "https://localhost:5001/",
            initialUri: "https://localhost:5001/");

        _sut = new BlazorWasmOptionsSetup(_navigationManager);

        _sentryInit = SentrySdk.Init(o =>
        {
            o.Dsn = ValidDsn;
            o.IsGlobalModeEnabled = true;
        });
    }

    public void Dispose()
    {
        _sentryInit.Dispose();
    }

    [Fact]
    public void Configure_SetsInitialRequestUrl()
    {
        // Act
        _sut.Configure(new SentryBlazorOptions());

        // Assert
        SentrySdk.ConfigureScope(scope =>
        {
            scope.Request.Url.Should().Be("/");
        });
    }

    [Fact]
    public void Configure_SetsInitialRequestUrl_WithPath()
    {
        // Arrange
        var nav = new FakeNavigationManager(
            baseUri: "https://localhost:5001/",
            initialUri: "https://localhost:5001/counter");
        var sut = new BlazorWasmOptionsSetup(nav);

        // Act
        sut.Configure(new SentryBlazorOptions());

        // Assert
        SentrySdk.ConfigureScope(scope =>
        {
            scope.Request.Url.Should().Be("/counter");
        });
    }

    [Fact]
    public void Navigation_CreatesBreadcrumbWithCorrectTypeAndCategory()
    {
        // Arrange
        _sut.Configure(new SentryBlazorOptions());

        // Act
        _navigationManager.NavigateTo("/dashboard");

        // Assert
        SentrySdk.ConfigureScope(scope =>
        {
            var crumb = scope.Breadcrumbs.Should().ContainSingle().Subject;
            crumb.Type.Should().Be("navigation");
            crumb.Category.Should().Be("navigation");
        });
    }

    [Fact]
    public void Navigation_CreatesBreadcrumbWithRelativePaths()
    {
        // Arrange
        _sut.Configure(new SentryBlazorOptions());

        // Act
        _navigationManager.NavigateTo("/dashboard");

        // Assert
        SentrySdk.ConfigureScope(scope =>
        {
            var crumb = scope.Breadcrumbs.Should().ContainSingle().Subject;
            crumb.Data.Should().ContainKey("from").WhoseValue.Should().Be("/");
            crumb.Data.Should().ContainKey("to").WhoseValue.Should().Be("/dashboard");
        });
    }

    [Fact]
    public void Navigation_UpdatesRequestUrl()
    {
        // Arrange
        _sut.Configure(new SentryBlazorOptions());

        // Act
        _navigationManager.NavigateTo("/dashboard");

        // Assert
        SentrySdk.ConfigureScope(scope =>
        {
            scope.Request.Url.Should().Be("/dashboard");
        });
    }

    [Fact]
    public void MultipleNavigations_TrackFromCorrectly()
    {
        // Arrange
        _sut.Configure(new SentryBlazorOptions());

        // Act
        _navigationManager.NavigateTo("/page1");
        _navigationManager.NavigateTo("/page2");

        // Assert
        SentrySdk.ConfigureScope(scope =>
        {
            var breadcrumbs = scope.Breadcrumbs.ToList();
            breadcrumbs.Should().HaveCount(2);

            var first = breadcrumbs[0];
            first.Data.Should().ContainKey("from").WhoseValue.Should().Be("/");
            first.Data.Should().ContainKey("to").WhoseValue.Should().Be("/page1");

            var second = breadcrumbs[1];
            second.Data.Should().ContainKey("from").WhoseValue.Should().Be("/page1");
            second.Data.Should().ContainKey("to").WhoseValue.Should().Be("/page2");
        });
    }

    [Fact]
    public void Navigation_FromInitialPath_TracksCorrectFrom()
    {
        // Arrange - start on /login
        var nav = new FakeNavigationManager(
            baseUri: "https://localhost:5001/",
            initialUri: "https://localhost:5001/login");
        var sut = new BlazorWasmOptionsSetup(nav);
        sut.Configure(new SentryBlazorOptions());

        // Act
        nav.NavigateTo("/home");

        // Assert
        SentrySdk.ConfigureScope(scope =>
        {
            var crumb = scope.Breadcrumbs.Should().ContainSingle().Subject;
            crumb.Data.Should().ContainKey("from").WhoseValue.Should().Be("/login");
            crumb.Data.Should().ContainKey("to").WhoseValue.Should().Be("/home");
        });
    }
}
