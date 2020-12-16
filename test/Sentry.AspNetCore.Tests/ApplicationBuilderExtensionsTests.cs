using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using NSubstitute;
using Sentry.Extensibility;
using Xunit;

namespace Sentry.AspNetCore.Tests
{
    public class ApplicationBuilderExtensionsTests
    {
        private class Fixture
        {
            public ISentryEventProcessor SentryEventProcessor { get; set; } = Substitute.For<ISentryEventProcessor>();
            public ISentryEventExceptionProcessor SentryEventExceptionProcessor { get; set; } = Substitute.For<ISentryEventExceptionProcessor>();
            public SentryAspNetCoreOptions SentryAspNetCoreOptions { get; set; } = new();

            public IApplicationBuilder GetSut()
            {
                var provider = Substitute.For<IServiceProvider>();
                var options = Substitute.For<IOptions<SentryAspNetCoreOptions>>();
                _ = options.Value.Returns(SentryAspNetCoreOptions);
                _ = provider.GetService(typeof(IOptions<SentryAspNetCoreOptions>)).Returns(options);

                if (SentryEventProcessor != null)
                {
                    _ = provider.GetService(typeof(ISentryEventProcessor)).Returns(SentryEventProcessor);
                }

                if (SentryEventExceptionProcessor != null)
                {
                    _ = provider.GetService(typeof(ISentryEventExceptionProcessor)).Returns(SentryEventExceptionProcessor);
                }

                _ = provider.GetService(typeof(IEnumerable<ISentryEventProcessor>))
                        .Returns(SentryEventProcessor != null
                            ? new[] { SentryEventProcessor }
                            : Enumerable.Empty<ISentryEventProcessor>());

                _ = provider.GetService(typeof(IEnumerable<ISentryEventExceptionProcessor>))
                        .Returns(SentryEventExceptionProcessor != null
                            ? new[] { SentryEventExceptionProcessor }
                            : Enumerable.Empty<ISentryEventExceptionProcessor>());


                var sut = Substitute.For<IApplicationBuilder>();
                _ = sut.ApplicationServices.Returns(provider);
                return sut;
            }
        }

        private readonly Fixture _fixture = new();

        [Fact]
        public void UseSentry_DiagnosticSet_NoOverriden()
        {
            var diagnosticLogger = Substitute.For<IDiagnosticLogger>();
            _fixture.SentryAspNetCoreOptions.DiagnosticLogger = diagnosticLogger;
            _fixture.SentryAspNetCoreOptions.Debug = true;
            var sut = _fixture.GetSut();
            _ = sut.UseSentry();

            Assert.Same(diagnosticLogger, _fixture.SentryAspNetCoreOptions.DiagnosticLogger);
        }

        [Fact]
        public void UseSentry_SentryEventProcessor_AccessibleThroughSentryOptions()
        {
            var sut = _fixture.GetSut();

            _ = sut.UseSentry();

            Assert.Contains(_fixture.SentryAspNetCoreOptions.GetAllEventProcessors(),
                actual => actual == _fixture.SentryEventProcessor);
        }

        [Fact]
        public void UseSentry_OriginalEventProcessor_StillAvailable()
        {
            var originalProviders = _fixture.SentryAspNetCoreOptions.EventProcessorsProviders;

            var sut = _fixture.GetSut();

            _ = sut.UseSentry();

            var missing = originalProviders.Except(_fixture.SentryAspNetCoreOptions.EventProcessorsProviders);
            Assert.Empty(missing);
        }

        [Fact]
        public void UseSentry_SentryEventExceptionProcessor_AccessibleThroughSentryOptions()
        {
            var sut = _fixture.GetSut();

            _ = sut.UseSentry();

            Assert.Contains(_fixture.SentryAspNetCoreOptions.GetAllExceptionProcessors(),
                actual => actual == _fixture.SentryEventExceptionProcessor);
        }

        [Fact]
        public void UseSentry_OriginalEventExceptionProcessor_StillAvailable()
        {
            var originalProviders = _fixture.SentryAspNetCoreOptions.ExceptionProcessorsProviders;

            var sut = _fixture.GetSut();

            _ = sut.UseSentry();

            var missing = originalProviders.Except(_fixture.SentryAspNetCoreOptions.ExceptionProcessorsProviders);
            Assert.Empty(missing);
        }

        [Fact]
        public void UseSentry_NoEventProcessor_OriginalCallbackNotPatched()
        {
            _fixture.SentryEventProcessor = null;
            var originalProviders = _fixture.SentryAspNetCoreOptions.EventProcessorsProviders;

            var sut = _fixture.GetSut();

            _ = sut.UseSentry();

            Assert.Same(originalProviders, _fixture.SentryAspNetCoreOptions.EventProcessorsProviders);
        }

        [Fact]
        public void UseSentry_NoEventExceptionProcessor_OriginalCallbackNotPatched()
        {
            _fixture.SentryEventExceptionProcessor = null;
            var originalProviders = _fixture.SentryAspNetCoreOptions.ExceptionProcessorsProviders;

            var sut = _fixture.GetSut();

            _ = sut.UseSentry();

            Assert.Same(originalProviders, _fixture.SentryAspNetCoreOptions.ExceptionProcessorsProviders);
        }
    }
}
