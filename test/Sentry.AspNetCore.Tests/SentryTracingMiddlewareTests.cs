﻿#if !NETCOREAPP2_1
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Sentry.AspNetCore.Tests.Utils.Extensions;
using Xunit;

namespace Sentry.AspNetCore.Tests
{
    public class SentryTracingMiddlewareTests
    {
        [Fact]
        public async Task Transactions_are_grouped_by_route()
        {
            // Arrange
            var sentryClient = Substitute.For<ISentryClient>();

            var hub = new Internal.Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithoutSecret,
                TracesSampleRate = 1
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
                    app.UseSentryTracing();

                    app.UseEndpoints(routes =>
                    {
                        routes.Map("/person/{id}", async ctx =>
                        {
                            var id = ctx.GetRouteValue("id") as string;
                            await ctx.Response.WriteAsync($"Person #{id}");
                        });
                    });
                })
            );

            var client = server.CreateClient();

            // Act
            await client.GetStringAsync("/person/13");
            await client.GetStringAsync("/person/69");

            // Assert
            sentryClient.Received(2).CaptureTransaction(
                Arg.Is<Transaction>(transaction => transaction.Name == "GET /person/{id}")
            );
        }

        [Fact]
        public async Task Transaction_is_bound_on_the_scope_automatically()
        {
            // Arrange
            ITransactionData transaction = null;

            var sentryClient = Substitute.For<ISentryClient>();

            var hub = new Internal.Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithoutSecret
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
                    app.UseSentryTracing();

                    app.UseEndpoints(routes =>
                    {
                        routes.Map("/person/{id}", _ =>
                        {
                            transaction = hub.GetSpan() as ITransactionData;
                            return Task.CompletedTask;
                        });
                    });
                })
            );

            var client = server.CreateClient();

            // Act
            await client.GetStringAsync("/person/13");

            // Assert
            transaction.Should().NotBeNull();
            transaction?.Name.Should().Be("GET /person/{id}");
        }

        [Fact]
        public async Task Transaction_is_started_automatically_from_incoming_trace_header()
        {
            // Arrange
            var sentryClient = Substitute.For<ISentryClient>();

            var hub = new Internal.Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithoutSecret,
                TracesSampleRate = 1
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
                    app.UseSentryTracing();

                    app.UseEndpoints(routes =>
                    {
                        routes.Map("/person/{id}", _ => Task.CompletedTask);
                    });
                })
            );

            var client = server.CreateClient();

            // Act
            using var request = new HttpRequestMessage(HttpMethod.Get, "/person/13")
            {
                Headers = {{"sentry-trace", "75302ac48a024bde9a3b3734a82e36c8-1000000000000000-0"}}
            };

            await client.SendAsync(request);

            // Assert
            sentryClient.Received(1).CaptureTransaction(Arg.Is<Transaction>(t =>
                t.Name == "GET /person/{id}" &&
                t.TraceId == SentryId.Parse("75302ac48a024bde9a3b3734a82e36c8") &&
                t.ParentSpanId == SpanId.Parse("1000000000000000") &&
                t.IsSampled == false
            ));
        }

        [Fact]
        public async Task Transaction_is_automatically_populated_with_request_data()
        {
            // Arrange
            ITransactionData transaction = null;

            var sentryClient = Substitute.For<ISentryClient>();

            var hub = new Internal.Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithoutSecret,
                TracesSampleRate = 1
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
                    app.UseSentryTracing();

                    app.UseEndpoints(routes =>
                    {
                        routes.Map("/person/{id}", _ =>
                        {
                            transaction = hub.GetSpan() as ITransactionData;
                            return Task.CompletedTask;
                        });
                    });
                })
            );

            var client = server.CreateClient();

            // Act
            using var request = new HttpRequestMessage(HttpMethod.Get, "/person/13")
            {
                Headers = {{"foo", "bar"}}
            };

            await client.SendAsync(request);

            // Assert
            transaction.Should().NotBeNull();
            transaction?.Request.Method.Should().Be("GET");
            transaction?.Request.Url.Should().Be("http://localhost/person/13");
            transaction?.Request.Headers.Should().Contain(new KeyValuePair<string, string>("foo", "bar"));
        }

        [Fact]
        public async Task Transaction_sampling_context_contains_HTTP_context_data()
        {
            // Arrange
            TransactionSamplingContext samplingContext = null;

            var sentryClient = Substitute.For<ISentryClient>();

            var hub = new Internal.Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithoutSecret,
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
                    app.UseSentryTracing();

                    app.UseEndpoints(routes =>
                    {
                        routes.Map("/person/{id}", _ => Task.CompletedTask);
                    });
                })
            );

            var client = server.CreateClient();

            // Act
            await client.GetAsync("/person/13");

            // Assert
            samplingContext.Should().NotBeNull();
            samplingContext.TryGetHttpMethod().Should().Be("GET");
            samplingContext.TryGetHttpRoute().Should().Be("/person/{id}");
            samplingContext.TryGetHttpPath().Should().Be("/person/13");
        }

        [Fact]
        public async Task Transaction_binds_exception_thrown()
        {
            // Arrange
            TransactionSamplingContext samplingContext = null;

            var sentryClient = Substitute.For<ISentryClient>();

            var hub = new Internal.Hub( new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithoutSecret,
                TracesSampler = ctx =>
                {
                    samplingContext = ctx;
                    return 1;
                }
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
                    app.UseRouting();
                    app.Use(async (r, c) =>
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
                    app.UseSentryTracing();

                    app.UseEndpoints(routes =>
                    {
                        routes.Map("/person/{id}", _ => throw exception);
                    });
                })
            );

            var client = server.CreateClient();

            // Act
            await client.GetAsync("/person/13");

            // Assert
            Assert.True(hub.ExceptionToSpanMap.TryGetValue(exception, out _));
        }
    }
}
#endif
