using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sentry.AspNetCore.TestUtils;
using Sentry.Ben.BlockingDetector;

namespace Sentry.AspNetCore.Tests;

public partial class IntegrationsTests
{
    private const string BlockingCallDetectorMechanism = "BlockingCallDetector";

    [Fact]
    public async Task InvokeAsync_CaptureBlockingCallsEnabled_ReusesListenerAcrossRequests()
    {
        var middlewareInstances = new ConcurrentQueue<SentryMiddleware>();

        Configure = options => options.CaptureBlockingCalls = true;
        AfterConfigureBuilder = builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<SentryMiddleware>();
            services.AddTransient(serviceProvider =>
            {
                var middleware = ActivatorUtilities.CreateInstance<SentryMiddleware>(serviceProvider);
                middlewareInstances.Enqueue(middleware);
                return middleware;
            });
        });

        try
        {
            Build();

            _ = await HttpClient.GetAsync("/");
            _ = await HttpClient.GetAsync("/");

            var instances = middlewareInstances.ToArray();
            Assert.Equal(2, instances.Length);
            Assert.NotSame(instances[0], instances[1]);
            Assert.NotNull(instances[0].Monitor);
            Assert.NotNull(instances[0].Listener);
            Assert.Same(instances[0].Monitor, instances[1].Monitor);
            Assert.Same(instances[0].Listener, instances[1].Listener);
            Assert.Same(ServiceProvider.GetRequiredService<IBlockingMonitor>(), instances[0].Monitor);
            Assert.Same(ServiceProvider.GetRequiredService<TaskBlockingListener>(), instances[0].Listener);
        }
        finally
        {
            DisposeTestHost();
        }
    }

    [Fact]
    public async Task InvokeAsync_BlockingCallDetectionEnabled_CapturesBlockingCallEvent()
    {
        var events = new ConcurrentQueue<SentryEvent>();
        Configure = options =>
        {
            options.CaptureBlockingCalls = true;
            options.SetBeforeSend(@event =>
            {
                events.Enqueue(@event);
                return @event;
            });
        };
        Handlers =
        [
            new RequestHandler
            {
                Path = "/blocking",
                Handler = _ =>
                {
                    Task.Delay(25).Wait();
                    return Task.CompletedTask;
                }
            }
        ];

        try
        {
            Build();

            _ = await HttpClient.GetAsync("/blocking");

            Assert.Contains(events, IsBlockingCallDetectorEvent);
        }
        finally
        {
            DisposeTestHost();
        }
    }

    [Fact]
    public async Task InvokeAsync_BlockingCallDetectionDisabled_DoesNotCaptureOrInstantiateDetector()
    {
        var events = new ConcurrentQueue<SentryEvent>();
        var middlewareCount = 0;
        var monitorCount = 0;
        var listenerCount = 0;
        Configure = options =>
        {
            options.CaptureBlockingCalls = false;
            options.SetBeforeSend(@event =>
            {
                events.Enqueue(@event);
                return @event;
            });
        };
        ConfigureCountingBlockingDetectionServices(
            () => Interlocked.Increment(ref middlewareCount),
            () => Interlocked.Increment(ref monitorCount),
            () => Interlocked.Increment(ref listenerCount));
        Handlers =
        [
            new RequestHandler
            {
                Path = "/blocking",
                Handler = _ =>
                {
                    Task.Delay(25).Wait();
                    return Task.CompletedTask;
                }
            }
        ];

        try
        {
            Build();

            _ = await HttpClient.GetAsync("/blocking");

            Assert.Equal(1, middlewareCount);
            Assert.Equal(0, monitorCount);
            Assert.Equal(0, listenerCount);
            Assert.DoesNotContain(events, IsBlockingCallDetectorEvent);
        }
        finally
        {
            DisposeTestHost();
        }
    }

    private void ConfigureCountingBlockingDetectionServices(
        Action middlewareCreated,
        Action monitorCreated,
        Action listenerCreated)
    {
        AfterConfigureBuilder = builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<SentryMiddleware>();
            services.AddTransient(serviceProvider =>
            {
                middlewareCreated();
                return ActivatorUtilities.CreateInstance<SentryMiddleware>(serviceProvider);
            });

            services.RemoveAll<IBlockingMonitor>();
            services.AddSingleton<IBlockingMonitor>(serviceProvider =>
            {
                monitorCreated();
                return ActivatorUtilities.CreateInstance<BlockingMonitor>(serviceProvider);
            });

            services.RemoveAll<TaskBlockingListener>();
            services.AddSingleton(serviceProvider =>
            {
                listenerCreated();
                return ActivatorUtilities.CreateInstance<TaskBlockingListener>(serviceProvider);
            });
        });
    }

    private static bool IsBlockingCallDetectorEvent(SentryEvent @event) =>
        @event.SentryExceptions?.Any(exception => exception.Mechanism?.Type == BlockingCallDetectorMechanism) == true;

    private void DisposeTestHost()
    {
        HttpClient?.Dispose();
        TestServer?.Dispose();
    }
}
