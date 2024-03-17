#nullable enable
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;
using Microsoft.Maui.TestUtils.DeviceTests.Runners.VisualRunner.Pages;

namespace Microsoft.Maui.TestUtils.DeviceTests.Runners.VisualRunner
{
    public partial class MauiVisualRunnerApp : Application

    /* Unmerged change from project 'TestUtils.DeviceTests.Runners(net8.0-ios)'
    Before:
            readonly TestOptions _options;
            readonly ILogger _logger;
    After:
            private readonly TestOptions _options;
            private readonly ILogger _logger;
    */

    /* Unmerged change from project 'TestUtils.DeviceTests.Runners(net8.0-maccatalyst)'
    Before:
            readonly TestOptions _options;
            readonly ILogger _logger;
    After:
            private readonly TestOptions _options;
            private readonly ILogger _logger;
    */
    {
        private readonly TestOptions _options;
        private readonly ILogger _logger;

        public MauiVisualRunnerApp(TestOptions options, ILogger logger)
        {
            _options = options;
            _logger = logger;

            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var hp = new HomePage();

            var nav = new TestNavigator(hp.Navigation);

            var runner = new DeviceRunner(_options.Assemblies, nav, _logger);

            var vm = new HomeViewModel(nav, runner);

            hp.BindingContext = vm;

            var navPage = new NavigationPage(hp);

            return new Window(navPage);
        }
    }
}
