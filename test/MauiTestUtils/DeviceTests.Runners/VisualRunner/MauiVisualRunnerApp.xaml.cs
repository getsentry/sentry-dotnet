#nullable enable
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;
using Microsoft.Maui.TestUtils.DeviceTests.Runners.VisualRunner.Pages;

<<<<<<< HEAD
namespace Microsoft.Maui.TestUtils.DeviceTests.Runners.VisualRunner
{
	partial class MauiVisualRunnerApp : Application
	{
		readonly TestOptions _options;
		readonly ILogger _logger;
=======
namespace Microsoft.Maui.TestUtils.DeviceTests.Runners.VisualRunner;

public partial class MauiVisualRunnerApp : Application
{
    private readonly TestOptions _options;
    private readonly ILogger _logger;
>>>>>>> chore/net8-devicetests

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

<<<<<<< HEAD
			return new Window(navPage);
		}
	}
}
=======
        return new Window(navPage);
    }
}
>>>>>>> chore/net8-devicetests
