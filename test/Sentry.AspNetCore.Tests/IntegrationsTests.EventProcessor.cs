using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Sentry.Extensibility;
using Xunit;

namespace Sentry.AspNetCore.Tests
{
    public partial class IntegrationsTests
    {
        [Fact]
        public async Task SentryEventProcessor_Transient_LifetimeRespected()
        {
            var processorsResolved = new List<ISentryEventProcessor>();
            ConfigureServices = c => c.AddTransient(_ =>
            {
                var processor = Substitute.For<ISentryEventProcessor>();
                processorsResolved.Add(processor);
                return processor;
            });

            Build();
            _ = await HttpClient.GetAsync("/throw");
            _ = await HttpClient.GetAsync("/throw");

            // First resolve only to decided if worth patching SentryOptions
            Assert.Equal(3, processorsResolved.Count);
            _ = processorsResolved[0].DidNotReceive().Process(Arg.Any<SentryEvent>());
            _ = processorsResolved[1].Received(1).Process(Arg.Any<SentryEvent>());
            _ = processorsResolved[2].Received(1).Process(Arg.Any<SentryEvent>());
        }

        [Fact]
        public async Task SentryEventExceptionProcessor_Transient_LifetimeRespected()
        {
            var exceptionProcessorsResolved = new List<ISentryEventExceptionProcessor>();
            ConfigureServices = c => c.AddTransient(_ =>
            {
                var processor = Substitute.For<ISentryEventExceptionProcessor>();
                exceptionProcessorsResolved.Add(processor);
                return processor;
            });

            Build();
            _ = await HttpClient.GetAsync("/throw");
            _ = await HttpClient.GetAsync("/throw");

            // First resolve only to decided if worth patching SentryOptions
            Assert.Equal(3, exceptionProcessorsResolved.Count);
            exceptionProcessorsResolved[0].DidNotReceive().Process(Arg.Any<Exception>(), Arg.Any<SentryEvent>());
            exceptionProcessorsResolved[1].Received(1).Process(Arg.Any<Exception>(), Arg.Any<SentryEvent>());
            exceptionProcessorsResolved[2].Received(1).Process(Arg.Any<Exception>(), Arg.Any<SentryEvent>());
        }
    }
}
