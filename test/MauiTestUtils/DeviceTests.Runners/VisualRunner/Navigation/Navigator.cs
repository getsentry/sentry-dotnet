#nullable enable
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.TestUtils.DeviceTests.Runners.VisualRunner.Pages;

namespace Microsoft.Maui.TestUtils.DeviceTests.Runners.VisualRunner
{
    internal class TestNavigator : ITestNavigation

    /* Unmerged change from project 'TestUtils.DeviceTests.Runners(net8.0-ios)'
    Before:
            readonly INavigation _navigation;
    After:
            private readonly INavigation _navigation;
    */

    /* Unmerged change from project 'TestUtils.DeviceTests.Runners(net8.0-maccatalyst)'
    Before:
            readonly INavigation _navigation;
    After:
            private readonly INavigation _navigation;
    */
    {
        private readonly INavigation _navigation;

        public TestNavigator(INavigation navigation)
        {
            _navigation = navigation;
        }

        public Task NavigateTo(PageType page, object? dataContext = null)
        {
            ContentPage p = page switch
            {
                PageType.Home => new HomePage(),
                PageType.AssemblyTestList => new TestAssemblyPage(),
                PageType.TestResult => new TestResultPage(),
                PageType.Credits => new CreditsPage(),
                _ => throw new ArgumentOutOfRangeException(nameof(page)),
            };

            p.BindingContext = dataContext;

            return _navigation.PushAsync(p);
        }
    }
}
