using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;

namespace Sentry.AspNetCore
{
    internal class SentryWebHost : IWebHost
    {
        private readonly IWebHost _webHost;

        public IFeatureCollection ServerFeatures => _webHost.ServerFeatures;
        public IServiceProvider Services => _webHost.Services;

        public SentryWebHost(IWebHost webHost)
        {
            _webHost = webHost ?? throw new ArgumentNullException(nameof(webHost));
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            SentryCore.AddBreadcrumb($"Starting the web host.");
            return _webHost.StartAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
            => _webHost.StartAsync(cancellationToken);

        public void Start()
        {
            SentryCore.AddBreadcrumb($"Starting the web host.");
            _webHost.Start();
        }

        public void Dispose() => _webHost.Dispose();
    }
}
