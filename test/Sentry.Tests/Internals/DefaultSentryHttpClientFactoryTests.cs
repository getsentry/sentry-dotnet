using System;
using System.Net;
using System.Net.Http;
using Sentry.Http;
using Sentry.Internal.Http;
using Xunit;

namespace Sentry.Tests.Internals
{
    public class DefaultSentryHttpClientFactoryTests
    {
        private class Fixture
        {
            public HttpOptions HttpOptions { get; set; } = new HttpOptions(new Uri("https://sentry.yo/store"));
            public Action<HttpClientHandler, Dsn, HttpOptions> ConfigureHandler { get; set; }
            public Action<HttpClient, Dsn, HttpOptions> ConfigureClient { get; set; }

            public DefaultSentryHttpClientFactory GetSut()
                => new DefaultSentryHttpClientFactory(ConfigureHandler, ConfigureClient);
        }

        private readonly Fixture _fixture = new Fixture();

        [Fact]
        public void Create_NullDsn_ThrowsArgumentNullException()
        {
            var sut = _fixture.GetSut();
            var ex = Assert.Throws<ArgumentNullException>(() => sut.Create(null, _fixture.HttpOptions));
            Assert.Equal("dsn", ex.ParamName);
        }

        [Fact]
        public void Create_Returns_HttpClient()
        {
            var sut = _fixture.GetSut();

            Assert.NotNull(sut.Create(DsnSamples.Valid, _fixture.HttpOptions));
        }

        [Fact]
        public void Create_DecompressionMethodNone_SetToClientHandler()
        {
            _fixture.HttpOptions.DecompressionMethods = DecompressionMethods.None;

            var configureHandlerInvoked = false;
            _fixture.ConfigureHandler = (handler, dsn, arg3) =>
            {
                Assert.Equal(DecompressionMethods.None, handler.AutomaticDecompression);
                configureHandlerInvoked = true;
            };
            var sut = _fixture.GetSut();

            sut.Create(DsnSamples.Valid, _fixture.HttpOptions);

            Assert.True(configureHandlerInvoked);
        }

        [Fact]
        public void Create_DecompressionMethodDefault_AllBitsSet()
        {
            var configureHandlerInvoked = false;
            _fixture.ConfigureHandler = (handler, dsn, o) =>
            {
                Assert.Same(_fixture.HttpOptions, o);
                Assert.Equal(~DecompressionMethods.None, handler.AutomaticDecompression);
                configureHandlerInvoked = true;
            };
            var sut = _fixture.GetSut();

            sut.Create(DsnSamples.Valid, _fixture.HttpOptions);

            Assert.True(configureHandlerInvoked);
        }

        [Fact]
        public void Create_DefaultHeaders_AcceptJson()
        {
            var configureHandlerInvoked = false;
            _fixture.ConfigureClient = (client, dsn, o) =>
            {
                Assert.Same(_fixture.HttpOptions, o);
                Assert.Equal("application/json", client.DefaultRequestHeaders.Accept.ToString());
                configureHandlerInvoked = true;
            };
            var sut = _fixture.GetSut();

            sut.Create(DsnSamples.Valid, _fixture.HttpOptions);

            Assert.True(configureHandlerInvoked);
        }
    }
}
