﻿#if !NETCOREAPP2_1
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Sentry.Protocol;
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

            var hub = new Internal.Hub(sentryClient, new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithoutSecret
            });

            var server = new TestServer(new WebHostBuilder()
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
    }
}
#endif
