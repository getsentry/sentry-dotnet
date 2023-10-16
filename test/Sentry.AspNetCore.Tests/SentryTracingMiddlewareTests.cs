#if NETCOREAPP3_1_OR_GREATER
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sentry.AspNetCore.TestUtils;
using Sentry.Internal.OpenTelemetry;

namespace Sentry.AspNetCore.Tests;

public class SentryTracingMiddlewareTests
{
    [Fact]
    public async Task Transactions_are_grouped_by_route()
    {
        // Arrange
        var sentryClient = Substitute.For<ISentryClient>();

        var hub = new Hub(new SentryOptions { Dsn = ValidDsn, TracesSampleRate = 1 }, sentryClient);

        var server = new TestServer(new WebHostBuilder()
            .UseDefaultServiceProvider(di => di.EnableValidation())
            .UseSentry()
            .ConfigureServices(services =>
            {
                services.AddRouting();

                services.RemoveAll(typeof(Func<IHub>));
                services.AddSingleton<Func<IHub>>(() => hub);
            })
            .Configure(app =>
            {
                app.UseRouting();

                app.UseEndpoints(routes =>
                {
                    routes.Map("/person/{id}", async ctx =>
                    {
                        var id = ctx.GetRouteValue("id") as string;
                        await ctx.Response.WriteAsync($"Person #{id}");
                    });
                });
            }));

        var client = server.CreateClient();

        // Act
        await client.GetStringAsync("/person/13");
        await client.GetStringAsync("/person/69");

        // Assert
        sentryClient.Received(2).CaptureTransaction(
            Arg.Is<Transaction>(transaction =>
                transaction.Name == "GET /person/{id}" &&
                transaction.NameSource == TransactionNameSource.Route),
            Arg.Any<Hint>()
            );
    }

    [Fact]
    public async Task Transaction_is_bound_on_the_scope_automatically()
    {
        // Arrange
        ITransactionData transaction = null;

        var sentryClient = Substitute.For<ISentryClient>();

        var hub = new Hub(new SentryOptions { Dsn = ValidDsn }, sentryClient);

        var server = new TestServer(new WebHostBuilder()
            .UseDefaultServiceProvider(di => di.EnableValidation())
            .UseSentry()
            .ConfigureServices(services =>
            {
                services.AddRouting();

                services.RemoveAll(typeof(Func<IHub>));
                services.AddSingleton<Func<IHub>>(() => hub);
            })
            .Configure(app =>
            {
                app.UseRouting();

                app.UseEndpoints(routes =>
                {
                    routes.Map("/person/{id}", _ =>
                    {
                        transaction = (ITransactionData) hub.GetSpan();
                        return Task.CompletedTask;
                    });
                });
            }));

        var client = server.CreateClient();

        // Act
        await client.GetStringAsync("/person/13");

        // Assert
        transaction.Should().NotBeNull();
        transaction.Name.Should().Be("GET /person/{id}");
        transaction.NameSource.Should().Be(TransactionNameSource.Route);
    }

    [Fact]
    public async Task Transaction_is_started_automatically_from_incoming_trace_header()
    {
        // Arrange
        var sentryClient = Substitute.For<ISentryClient>();

        var hub = new Hub(new SentryOptions { Dsn = ValidDsn, TracesSampleRate = 1 }, sentryClient);

        var server = new TestServer(new WebHostBuilder()
            .UseDefaultServiceProvider(di => di.EnableValidation())
            .UseSentry()
            .ConfigureServices(services =>
            {
                services.AddRouting();

                services.RemoveAll(typeof(Func<IHub>));
                services.AddSingleton<Func<IHub>>(() => hub);
            })
            .Configure(app =>
            {
                app.UseRouting();

                app.UseEndpoints(routes => routes.Map("/person/{id}", _ => Task.CompletedTask));
            }));

        var client = server.CreateClient();

        // Act
        using var request = new HttpRequestMessage(HttpMethod.Get, "/person/13")
        {
            Headers =
            {
                {"sentry-trace", "75302ac48a024bde9a3b3734a82e36c8-1000000000000000-0"}
            }
        };

        await client.SendAsync(request);

        // Assert
        sentryClient.Received(1).CaptureTransaction(Arg.Is<Transaction>(t =>
            t.Name == "GET /person/{id}" &&
            t.NameSource == TransactionNameSource.Route &&
            t.TraceId == SentryId.Parse("75302ac48a024bde9a3b3734a82e36c8") &&
            t.ParentSpanId == SpanId.Parse("1000000000000000") &&
            t.IsSampled == false
        ),
        Arg.Any<Hint>()
        );
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TraceID_from_trace_header_propagates_to_outbound_requests(bool shouldPropagate)
    {
        // Arrange
        var sentryClient = Substitute.For<ISentryClient>();

        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            TracesSampleRate = 1
        };

        if (!shouldPropagate)
        {
            options.TracePropagationTargets.Clear();
        }

        var hub = new Hub(options, sentryClient);

        HttpRequestHeaders outboundRequestHeaders = null;

        var server = new TestServer(new WebHostBuilder()
            .UseDefaultServiceProvider(di => di.EnableValidation())
            .UseSentry()
            .ConfigureServices(services =>
            {
                services.AddRouting();

                services.RemoveAll(typeof(Func<IHub>));
                services.AddSingleton<Func<IHub>>(() => hub);
            })
            .Configure(app =>
            {
                app.UseRouting();

                app.UseEndpoints(routes => routes.Map("/person/{id}", async _ =>
                {
                    // simulate an outbound request and capture the request headers
                    using var innerHandler = new RecordingHttpMessageHandler(new FakeHttpMessageHandler());
                    using var sentryHandler = new SentryHttpMessageHandler(innerHandler, hub);
                    using var client = new HttpClient(sentryHandler);
                    await client.GetAsync("https://localhost/");
                    using var request = innerHandler.GetRequests().Single();
                    outboundRequestHeaders = request.Headers;
                }));
            }));

        var client = server.CreateClient();

        // Act
        using var request = new HttpRequestMessage(HttpMethod.Get, "/person/13")
        {
            Headers =
            {
                {"sentry-trace", "75302ac48a024bde9a3b3734a82e36c8-1000000000000000"}
            }
        };

        await client.SendAsync(request);

        // Assert
        Assert.NotNull(outboundRequestHeaders);
        if (shouldPropagate)
        {
            outboundRequestHeaders.Should().Contain(h =>
                h.Key == "sentry-trace" &&
                h.Value.First().StartsWith("75302ac48a024bde9a3b3734a82e36c8-"));
        }
        else
        {
            outboundRequestHeaders.Should().NotContain(h => h.Key == "sentry-trace");
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Baggage_header_propagates_to_outbound_requests(bool shouldPropagate)
    {
        // incoming baggage header
        const string incomingBaggage =
            "sentry-trace_id=75302ac48a024bde9a3b3734a82e36c8, " +
            "sentry-public_key=d4d82fc1c2c4032a83f3a29aa3a3aff, " +
            "sentry-sample_rate=0.5, " +
            "foo-bar=abc123";

        // other baggage already on the outbound request (manually in this test, but in theory by some other middleware)
        const string existingOutboundBaggage = "other-value=abc123";

        // we expect this to be the result on outbound requests
        string expectedOutboundBaggage;
        if (shouldPropagate)
        {
            expectedOutboundBaggage =
                "other-value=abc123, " +
                "sentry-trace_id=75302ac48a024bde9a3b3734a82e36c8, " +
                "sentry-public_key=d4d82fc1c2c4032a83f3a29aa3a3aff, " +
                "sentry-sample_rate=0.5";
        }
        else
        {
            expectedOutboundBaggage = "other-value=abc123";
        }

        // Note that we "play nice" with existing headers on the outbound request, but we do not propagate other
        // non-Sentry headers on the inbound request.  The expectation is that the other vendor would add their
        // own middleware to do that.

        // Arrange
        var sentryClient = Substitute.For<ISentryClient>();

        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            TracesSampleRate = 1
        };

        if (!shouldPropagate)
        {
            options.TracePropagationTargets.Clear();
        }

        var hub = new Hub(options, sentryClient);

        HttpRequestHeaders outboundRequestHeaders = null;

        var server = new TestServer(new WebHostBuilder()
            .UseDefaultServiceProvider(di => di.EnableValidation())
            .UseSentry()
            .ConfigureServices(services =>
            {
                services.AddRouting();

                services.RemoveAll(typeof(Func<IHub>));
                services.AddSingleton<Func<IHub>>(() => hub);
            })
            .Configure(app =>
            {
                app.UseRouting();

                app.UseEndpoints(routes => routes.Map("/person/{id}", async _ =>
                {
                    // simulate an outbound request and capture the request headers
                    using var innerHandler = new RecordingHttpMessageHandler(new FakeHttpMessageHandler());
                    using var sentryHandler = new SentryHttpMessageHandler(innerHandler, hub);
                    using var client = new HttpClient(sentryHandler);
                    client.DefaultRequestHeaders.Add("baggage", existingOutboundBaggage);
                    await client.GetAsync("https://localhost/");
                    using var request = innerHandler.GetRequests().Single();
                    outboundRequestHeaders = request.Headers;
                }));
            }));

        var client = server.CreateClient();

        // Act
        using var request = new HttpRequestMessage(HttpMethod.Get, "/person/13")
        {
            Headers =
            {
                {"baggage", incomingBaggage}
            }
        };

        await client.SendAsync(request);

        // Assert
        Assert.NotNull(outboundRequestHeaders);
        outboundRequestHeaders.Should().Contain(h =>
            h.Key == "baggage" &&
            h.Value.First() == expectedOutboundBaggage);
    }

    [Fact]
    public async Task Baggage_header_sets_dynamic_sampling_context()
    {
        // incoming baggage header
        const string baggage =
            "sentry-trace_id=75302ac48a024bde9a3b3734a82e36c8, " +
            "sentry-public_key=d4d82fc1c2c4032a83f3a29aa3a3aff, " +
            "sentry-sample_rate=0.5";

        // Arrange
        TransactionTracer transaction = null;

        var sentryClient = Substitute.For<ISentryClient>();

        var hub = new Hub(new SentryOptions { Dsn = ValidDsn, TracesSampleRate = 1 }, sentryClient);

        var server = new TestServer(new WebHostBuilder()
            .UseDefaultServiceProvider(di => di.EnableValidation())
            .UseSentry()
            .ConfigureServices(services =>
            {
                services.AddRouting();

                services.RemoveAll(typeof(Func<IHub>));
                services.AddSingleton<Func<IHub>>(() => hub);
            })
            .Configure(app =>
            {
                app.UseRouting();

                app.UseEndpoints(routes =>
                {
                    routes.Map("/person/{id}", _ =>
                    {
                        transaction = (TransactionTracer) hub.GetSpan();
                        return Task.CompletedTask;
                    });
                });
            }));

        var client = server.CreateClient();

        // Act
        using var request = new HttpRequestMessage(HttpMethod.Get, "/person/13")
        {
            Headers =
            {
                {"baggage", baggage}
            }
        };

        await client.SendAsync(request);

        // Assert
        var dsc = transaction?.DynamicSamplingContext;
        Assert.NotNull(dsc);
        Assert.Equal(baggage, dsc.ToBaggageHeader().ToString());
    }

    [Fact]
    public async Task Transaction_is_automatically_populated_with_request_data()
    {
        // Arrange
        ITransactionData transaction = null;

        var sentryClient = Substitute.For<ISentryClient>();

        var hub = new Hub(new SentryOptions { Dsn = ValidDsn, TracesSampleRate = 1 }, sentryClient);

        var server = new TestServer(new WebHostBuilder()
            .UseDefaultServiceProvider(di => di.EnableValidation())
            .UseSentry()
            .ConfigureServices(services =>
            {
                services.AddRouting();

                services.RemoveAll(typeof(Func<IHub>));
                services.AddSingleton<Func<IHub>>(() => hub);
            })
            .Configure(app =>
            {
                app.UseRouting();

                app.UseEndpoints(routes =>
                {
                    routes.Map("/person/{id}", _ =>
                    {
                        transaction = (ITransactionData) hub.GetSpan();
                        return Task.CompletedTask;
                    });
                });
            }));

        var client = server.CreateClient();

        // Act
        using var request = new HttpRequestMessage(HttpMethod.Get, "/person/13") { Headers = { { "foo", "bar" } } };

        await client.SendAsync(request);

        // Assert
        transaction.Should().NotBeNull();
        transaction.Request.Method.Should().Be("GET");
        transaction.Request.Url.Should().Be("http://localhost/person/13");
        transaction.Request.Headers.Should().Contain(new KeyValuePair<string, string>("foo", "bar"));
        transaction.Data.Should().ContainKey(OtelSemanticConventions.AttributeHttpRequestMethod);
        transaction.Data[OtelSemanticConventions.AttributeHttpRequestMethod].Should().Be("GET");
        transaction.Data.Should().ContainKey(OtelSemanticConventions.AttributeHttpResponseStatusCode);
        transaction.Data[OtelSemanticConventions.AttributeHttpResponseStatusCode].Should().Be(200);
    }

    [Fact]
    public async Task Transaction_sampling_context_contains_HTTP_context_data()
    {
        // Arrange
        TransactionSamplingContext samplingContext = null;
        HttpContext httpContext = null;

        var sentryClient = Substitute.For<ISentryClient>();

        var hub = new Hub(new SentryOptions
        {
            Dsn = ValidDsn,
            TracesSampler = ctx =>
            {
                samplingContext = ctx;
                return 1;
            }
        }, sentryClient);

        var server = new TestServer(new WebHostBuilder()
            .UseDefaultServiceProvider(di => di.EnableValidation())
            .UseSentry()
            .ConfigureServices(services =>
            {
                services.AddRouting();

                services.RemoveAll(typeof(Func<IHub>));
                services.AddSingleton<Func<IHub>>(() => hub);
            })
            .Configure(app =>
            {
                app.UseRouting();

                app.UseEndpoints(routes => routes.Map("/person/{id}", context =>
                {
                    httpContext = context;
                    return Task.CompletedTask;
                }));
            }));

        var client = server.CreateClient();

        // Act
        await client.GetAsync("/person/13");

        // Assert
        samplingContext.Should().NotBeNull();
        samplingContext.TryGetHttpMethod().Should().Be("GET");
        samplingContext.TryGetHttpRoute().Should().Be("/person/{id}");
        samplingContext.TryGetHttpPath().Should().Be("/person/13");
        samplingContext.TryGetHttpContext().Should().BeSameAs(httpContext);
    }

    [Fact]
    public async Task Transaction_binds_exception_thrown()
    {
        // Arrange
        var sentryClient = Substitute.For<ISentryClient>();

        var hub = new Hub(new SentryOptions
        {
            Dsn = ValidDsn,
            TracesSampler = _ => 1.0
        }, sentryClient);
        var exception = new Exception();

        var server = new TestServer(new WebHostBuilder()
            .UseDefaultServiceProvider(di => di.EnableValidation())
            .UseSentry()
            .ConfigureServices(services =>
            {
                services.AddRouting();

                services.RemoveAll(typeof(Func<IHub>));
                services.AddSingleton<Func<IHub>>(() => hub);
            })
            .Configure(app =>
            {
                // We have to do this before routing, otherwise it won't wrap our SentryTracingMiddleware, which is what
                // binds the ExceptionToSpanMap
                app.Use(async (_, c) =>
                {
                    try
                    {
                        await c().ConfigureAwait(false);
                    }
                    catch
                    {
                        // We just want to know if it got into Sentry's Hub
                    }
                });
                app.UseRouting();

                app.UseEndpoints(routes => routes.Map("/person/{id}", _ => throw exception));
            }));

        var client = server.CreateClient();

        // Act
        await client.GetAsync("/person/13");

        // Assert
        Assert.True(hub.ExceptionToSpanMap.TryGetValue(exception, out var span));
        Assert.Equal(SpanStatus.InternalError, span?.Status);
    }

    [Fact]
    public async Task Transaction_TransactionNameProviderSetSet_TransactionNameSet()
    {
        // Arrange
        Transaction transaction = null;

        var expectedName = "My custom name";

        var sentryClient = Substitute.For<ISentryClient>();
        sentryClient.When(x => x.CaptureTransaction(Arg.Any<Transaction>(), Arg.Any<Hint>()))
            .Do(callback => transaction = callback.Arg<Transaction>());
        var options = new SentryAspNetCoreOptions
        {
            Dsn = ValidDsn,
            TracesSampleRate = 1
        };

        var hub = new Hub(options, sentryClient);

        var server = new TestServer(new WebHostBuilder()
            .UseSentry(aspNewOptions => aspNewOptions.TransactionNameProvider = _ => expectedName)
            .ConfigureServices(services =>
            {
                services.RemoveAll(typeof(Func<IHub>));
                services.AddSingleton<Func<IHub>>(() => hub);
            }).Configure(app => app.UseSentryTracing()));

        var client = server.CreateClient();

        // Act
        try
        {
            await client.GetStringAsync("/person/13.bmp");
        }
        // Expected error.
        catch (HttpRequestException ex) when (ex.Message.Contains("404"))
        { }

        // Assert
        transaction.Should().NotBeNull();
        transaction.Name.Should().Be($"GET {expectedName}");
        transaction.NameSource.Should().Be(TransactionNameSource.Custom);
    }

    [Fact]
    public async Task Transaction_TransactionNameProviderSetUnset_TransactionNameSetToUrlPath()
    {
        // Arrange
        Transaction transaction = null;

        var sentryClient = Substitute.For<ISentryClient>();
        sentryClient.When(x => x.CaptureTransaction(Arg.Any<Transaction>(), Arg.Any<Hint>()))
            .Do(callback => transaction = callback.Arg<Transaction>());
        var options = new SentryAspNetCoreOptions
        {
            Dsn = ValidDsn,
            TracesSampleRate = 1
        };

        var hub = new Hub(options, sentryClient);

        var server = new TestServer(new WebHostBuilder()
            .UseSentry()
            .ConfigureServices(services =>
            {
                services.RemoveAll(typeof(Func<IHub>));
                services.AddSingleton<Func<IHub>>(() => hub);
            }).Configure(app => app.UseSentryTracing()));

        var client = server.CreateClient();

        // Act
        try
        {
            await client.GetStringAsync("/person/13.bmp");
        }
        // Expected error.
        catch (HttpRequestException ex) when (ex.Message.Contains("404"))
        { }

        // Assert
        transaction.Should().NotBeNull();
        transaction.Name.Should().Be("GET /person/13.bmp");
        transaction.NameSource.Should().Be(TransactionNameSource.Url);
    }
}

#endif
