#if NETCOREAPP3_0_OR_GREATER
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sentry.AspNetCore.TestUtils;

namespace Sentry.AspNetCore.Tests;

public class SentryTracingBuilderTests
{
    private class Fixture
    {
        public Action<IServiceCollection> ConfigureServices { get; set; }
        public Action<IApplicationBuilder> Configure { get; set; }
        public Action<SentryAspNetCoreOptions> ConfigureOptions { get; set; } = _ => { };

        public (IServiceCollection services, IApplicationBuilder builder) GetSut()
        {
            IServiceCollection servicesCollection = null;
            IApplicationBuilder applicationBuilder = null;
            _ = new TestServer(new WebHostBuilder()
                .UseDefaultServiceProvider(di => di.EnableValidation())
                .UseSentry(ConfigureOptions)
                .ConfigureServices(services =>
                {
                    ConfigureServices?.Invoke(services);
                    servicesCollection = services;
                })
                .Configure(app =>
                {
                    Configure?.Invoke(app);
                    applicationBuilder = app;
                }));
            return (servicesCollection, applicationBuilder);
        }
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void UseRouting_AutoRegisterTracingDisabled_SentryTracingNotRegistered()
    {
        // Arrange
        _fixture.ConfigureServices = services => services.AddRouting();
        _fixture.Configure = applicationBuilder => applicationBuilder.UseRouting();
        _fixture.ConfigureOptions = options =>
        {
            options.Dsn = Sentry.SentryConstants.DisableSdkDsnValue;
            options.AutoRegisterTracing = false;
        };

        // Act - implicit
        var (_, builder) = _fixture.GetSut();

        // Assert
        builder.IsSentryTracingRegistered().Should().BeFalse();
    }

    [Fact]
    public void UseRouting_OtelInstrumentation_SentryTracingNotRegistered()
    {
        // Arrange
        _fixture.ConfigureServices = services => services.AddRouting();
        _fixture.Configure = applicationBuilder => applicationBuilder.UseRouting();
        _fixture.ConfigureOptions = options =>
        {
            options.Dsn = Sentry.SentryConstants.DisableSdkDsnValue;
            options.Instrumenter = Instrumenter.OpenTelemetry;
        };

        // Act - implicit
        var (_, builder) = _fixture.GetSut();

        // Assert
        builder.IsSentryTracingRegistered().Should().BeFalse();
    }

    [Fact]
    public void UseRouting_SentryTracingRegisteredWithoutWarning()
    {
        // Arrange
        var logger = Substitute.For<ILogger<SentryTracingMiddleware>>();
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger<SentryTracingMiddleware>().Returns(logger);
        _fixture.ConfigureServices = services =>
        {
            services.AddSingleton(loggerFactory);
            services.AddRouting();
        };
        _fixture.Configure = applicationBuilder => applicationBuilder.UseRouting();
        _fixture.ConfigureOptions = options =>
        {
            options.Dsn = Sentry.SentryConstants.DisableSdkDsnValue;
        };

        // Act
        var (_, builder) = _fixture.GetSut();

        builder.IsSentryTracingRegistered().Should().BeTrue();
        logger.Received(0).LogWarning(SentryTracingMiddlewareExtensions.AlreadyRegisteredWarning);
    }

    [Fact]
    public void UseSentryTracing_AutoRegisterTracing_Warning()
    {
        // Arrange
        var logger = Substitute.For<ILogger<SentryTracingMiddleware>>();
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger<SentryTracingMiddleware>().Returns(logger);
        _fixture.ConfigureServices = services =>
        {
            services.AddSingleton(loggerFactory);
            services.AddRouting();
        };
        _fixture.Configure = applicationBuilder =>
        {
            applicationBuilder.UseRouting();
            applicationBuilder.UseSentryTracing();
        };
        _fixture.ConfigureOptions = options =>
        {
            options.Dsn = Sentry.SentryConstants.DisableSdkDsnValue;
        };

        // Act
        var _ = _fixture.GetSut();

        // Assert
        logger.Received(1).LogWarning(SentryTracingMiddlewareExtensions.AlreadyRegisteredWarning);
    }
}
#endif
