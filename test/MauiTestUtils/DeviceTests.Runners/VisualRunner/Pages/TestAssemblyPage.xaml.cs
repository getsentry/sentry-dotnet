#nullable enable
using Microsoft.Maui.Controls;

namespace Microsoft.Maui.TestUtils.DeviceTests.Runners.VisualRunner.Pages
{
    public partial class TestAssemblyPage : ContentPage
    {
        public TestAssemblyPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            testsList.SelectedItem = null;

            if (BindingContext is ViewModelBase vm)
                vm.OnAppearing();
        }
    }
}
