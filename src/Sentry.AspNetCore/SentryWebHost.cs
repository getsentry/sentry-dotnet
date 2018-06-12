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

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                SentryCore.AddBreadcrumb($"Starting the web host.");
                await _webHost.StartAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                SentryCore.CaptureException(e);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _webHost.StartAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                SentryCore.CaptureException(e);
            }
        }

        public void Start()
        {
            try
            {
                _webHost.Start();
            }
            catch (Exception e)
            {
                SentryCore.CaptureException(e);
            }
        }

        public void Dispose()
        {
            _webHost.Dispose();
        }
    }
}
