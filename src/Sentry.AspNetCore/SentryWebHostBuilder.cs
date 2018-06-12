using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Sentry.AspNetCore
{
    /// <inheritdoc />
    internal class SentryWebHostBuilder : IWebHostBuilder
    {
        private IWebHostBuilder _builder;

        /// <inheritdoc />
        public SentryWebHostBuilder(IWebHostBuilder builder)
            => _builder = builder ?? throw new ArgumentNullException(nameof(builder));

        /// <inheritdoc />
        public IWebHost Build() => new SentryWebHost(_builder.Build());

        /// <inheritdoc />
        public IWebHostBuilder ConfigureAppConfiguration(
            Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate)
        {
            _builder = _builder.ConfigureAppConfiguration(configureDelegate);
            return this;
        }

        /// <inheritdoc />
        public IWebHostBuilder ConfigureServices(Action<IServiceCollection> configureServices)
        {
            _builder = _builder.ConfigureServices(configureServices);
            return this;
        }

        /// <inheritdoc />
        public IWebHostBuilder ConfigureServices(
            Action<WebHostBuilderContext, IServiceCollection> configureServices)
        {
            _builder = _builder.ConfigureServices(configureServices);
            return this;
        }

        /// <inheritdoc />
        public string GetSetting(string key) => _builder.GetSetting(key);

        /// <inheritdoc />
        public IWebHostBuilder UseSetting(string key, string value) => _builder.UseSetting(key, value);
    }
}
