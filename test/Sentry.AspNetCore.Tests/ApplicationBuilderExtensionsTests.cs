using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
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
            public SentryAspNetCoreOptions SentryAspNetCoreOptions { get; set; } = new SentryAspNetCoreOptions
            {
                SentryOptions = new SentryOptions()
            };

            public IApplicationBuilder GetSut()
            {
                var provider = Substitute.For<IServiceProvider>();
                provider.GetService(typeof(SentryAspNetCoreOptions)).Returns(SentryAspNetCoreOptions);

                if (SentryEventProcessor != null)
                {
                    provider.GetService(typeof(ISentryEventProcessor)).Returns(SentryEventProcessor);
                }

                if (SentryEventExceptionProcessor != null)
                {
                    provider.GetService(typeof(ISentryEventExceptionProcessor)).Returns(SentryEventExceptionProcessor);
                }

                provider.GetService(typeof(IEnumerable<ISentryEventProcessor>))
                    .Returns(SentryEventProcessor != null
                        ? new[] { SentryEventProcessor }
                        : Enumerable.Empty<ISentryEventProcessor>());

                provider.GetService(typeof(IEnumerable<ISentryEventExceptionProcessor>))
                    .Returns(SentryEventExceptionProcessor != null
                        ? new[] { SentryEventExceptionProcessor }
                        : Enumerable.Empty<ISentryEventExceptionProcessor>());


                var sut = Substitute.For<IApplicationBuilder>();
                sut.ApplicationServices.Returns(provider);
                return sut;
            }
        }

        private readonly Fixture _fixture = new Fixture();

        [Fact]
        public void UseSentry_SentryEventProcessor_AccessibleThroughSentryOptions()
        {
            var sut = _fixture.GetSut();

            sut.UseSentry();

            Assert.Contains(_fixture.SentryAspNetCoreOptions.SentryOptions.GetEventProcessors(),
                actual => actual == _fixture.SentryEventProcessor);
        }

        [Fact]
        public void UseSentry_OriginalEventProcessor_StillAvailable()
        {
            var originalFunc = _fixture.SentryAspNetCoreOptions.SentryOptions.GetEventProcessors;

            var sut = _fixture.GetSut();

            sut.UseSentry();

            var missing = originalFunc().Except(_fixture.SentryAspNetCoreOptions.SentryOptions.GetEventProcessors());
            Assert.Empty(missing);
        }

        [Fact]
        public void UseSentry_SentryEventExceptionProcessor_AccessibleThroughSentryOptions()
        {
            var sut = _fixture.GetSut();

            sut.UseSentry();

            Assert.Contains(_fixture.SentryAspNetCoreOptions.SentryOptions.GetExceptionProcessors(),
                actual => actual == _fixture.SentryEventExceptionProcessor);
        }

        [Fact]
        public void UseSentry_OriginalEventExceptionProcessor_StillAvailable()
        {
            var originalFunc = _fixture.SentryAspNetCoreOptions.SentryOptions.GetExceptionProcessors;

            var sut = _fixture.GetSut();

            sut.UseSentry();

            var missing = originalFunc().Except(_fixture.SentryAspNetCoreOptions.SentryOptions.GetExceptionProcessors());
            Assert.Empty(missing);
        }

        [Fact]
        public void UseSentry_NoEventProcessor_OriginalCallbackNotPatched()
        {
            _fixture.SentryEventProcessor = null;
            var originalFunc = _fixture.SentryAspNetCoreOptions.SentryOptions.GetEventProcessors;

            var sut = _fixture.GetSut();

            sut.UseSentry();

            Assert.Same(originalFunc, _fixture.SentryAspNetCoreOptions.SentryOptions.GetEventProcessors);
        }

        [Fact]
        public void UseSentry_NoEventExceptionProcessor_OriginalCallbackNotPatched()
        {
            _fixture.SentryEventExceptionProcessor = null;
            var originalFunc = _fixture.SentryAspNetCoreOptions.SentryOptions.GetExceptionProcessors;

            var sut = _fixture.GetSut();

            sut.UseSentry();

            Assert.Same(originalFunc, _fixture.SentryAspNetCoreOptions.SentryOptions.GetExceptionProcessors);
        }
    }
}
