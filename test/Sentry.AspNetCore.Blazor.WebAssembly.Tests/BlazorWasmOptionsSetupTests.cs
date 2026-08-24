using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Sentry.AspNetCore.Blazor.WebAssembly.Internal;

namespace Sentry.AspNetCore.Blazor.WebAssembly.Tests;

public class BlazorWasmOptionsSetupTests
{
    private readonly FakeNavigationManager _navigationManager;
    private readonly IHub _hub;
    private readonly Scope _scope;
    private readonly BlazorWasmOptionsSetup _sut;

    public BlazorWasmOptionsSetupTests()
    {
        _navigationManager = new FakeNavigationManager(
            baseUri: "https://localhost:5001/",
            initialUri: "https://localhost:5001/");

        _hub = Substitute.For<IHub>();
        _scope = new Scope(new SentryOptions());
        _hub.SubstituteConfigureScope(_scope);

        _sut = new BlazorWasmOptionsSetup(_navigationManager, _hub);
    }

    [Fact]
    public void Configure_SetsInitialRequestUrl()
    {
        // Act
        _sut.Configure(new SentryBlazorOptions());

        // Assert
        _scope.Request.Url.Should().Be("/");
    }

    [Fact]
    public void Configure_SetsInitialRequestUrl_WithPath()
    {
        // Arrange
        var nav = new FakeNavigationManager(
            baseUri: "https://localhost:5001/",
            initialUri: "https://localhost:5001/counter");
        var sut = new BlazorWasmOptionsSetup(nav, _hub);

        // Act
        sut.Configure(new SentryBlazorOptions());

        // Assert
        _scope.Request.Url.Should().Be("/counter");
    }

    [Fact]
    public void Navigation_CreatesBreadcrumbWithCorrectTypeAndCategory()
    {
        // Arrange
        _sut.Configure(new SentryBlazorOptions());

        // Act
        _navigationManager.NavigateTo("/dashboard");

        // Assert
        var crumb = _scope.Breadcrumbs.Should().ContainSingle().Subject;
        crumb.Type.Should().Be("navigation");
        crumb.Category.Should().Be("navigation");
    }

    [Fact]
    public void Navigation_BreadcrumbHasNoMessage()
    {
        // Arrange
        _sut.Configure(new SentryBlazorOptions());

        // Act
        _navigationManager.NavigateTo("/dashboard");

        // Assert
        var crumb = _scope.Breadcrumbs.Should().ContainSingle().Subject;
        crumb.Message.Should().BeNull();
    }

    [Fact]
    public void Navigation_CreatesBreadcrumbWithRelativePaths()
    {
        // Arrange
        _sut.Configure(new SentryBlazorOptions());

        // Act
        _navigationManager.NavigateTo("/dashboard");

        // Assert
        var crumb = _scope.Breadcrumbs.Should().ContainSingle().Subject;
        crumb.Data.Should().ContainKey("from").WhoseValue.Should().Be("/");
        crumb.Data.Should().ContainKey("to").WhoseValue.Should().Be("/dashboard");
    }

    [Fact]
    public void Navigation_UpdatesRequestUrl()
    {
        // Arrange
        _sut.Configure(new SentryBlazorOptions());

        // Act
        _navigationManager.NavigateTo("/dashboard");

        // Assert
        _scope.Request.Url.Should().Be("/dashboard");
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
        var breadcrumbs = _scope.Breadcrumbs.ToList();
        breadcrumbs.Should().HaveCount(2);

        var first = breadcrumbs[0];
        first.Data.Should().ContainKey("from").WhoseValue.Should().Be("/");
        first.Data.Should().ContainKey("to").WhoseValue.Should().Be("/page1");

        var second = breadcrumbs[1];
        second.Data.Should().ContainKey("from").WhoseValue.Should().Be("/page1");
        second.Data.Should().ContainKey("to").WhoseValue.Should().Be("/page2");
    }

    [Fact]
    public void Navigation_FromInitialPath_TracksCorrectFrom()
    {
        // Arrange - start on /login
        var nav = new FakeNavigationManager(
            baseUri: "https://localhost:5001/",
            initialUri: "https://localhost:5001/login");
        var sut = new BlazorWasmOptionsSetup(nav, _hub);
        sut.Configure(new SentryBlazorOptions());

        // Act
        nav.NavigateTo("/home");

        // Assert
        var crumb = _scope.Breadcrumbs.Should().ContainSingle().Subject;
        crumb.Data.Should().ContainKey("from").WhoseValue.Should().Be("/login");
        crumb.Data.Should().ContainKey("to").WhoseValue.Should().Be("/home");
    }

    [Fact]
    public void DuplicateNavigation_SkipsBreadcrumb()
    {
        // Arrange
        _sut.Configure(new SentryBlazorOptions());

        // Act — navigate to /page1, then fire LocationChanged again for the same URL
        _navigationManager.NavigateTo("/page1");
        _navigationManager.NavigateTo("/page1");

        // Assert — only one breadcrumb should be created
        _scope.Breadcrumbs.Should().ContainSingle();
    }
}
