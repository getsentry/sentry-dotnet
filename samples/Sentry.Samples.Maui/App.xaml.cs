using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.PlatformConfiguration.WindowsSpecific;
using Application = Microsoft.Maui.Controls.Application;

namespace Sentry.Samples.Maui
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // App.Handler.MauiContext.Services.GetService<...>()

            BindingContextChanged += (sender, args) => { };
            PropertyChanging += (sender, args) => { };
            PropertyChanged += (sender, args) => { };
            ParentChanging += (sender, args) => { };
            ParentChanged += (sender, args) => { };
            HandlerChanging += (sender, args) => { };
            HandlerChanged += (sender, args) => { };
            DescendantRemoved += (sender, args) => { };
            DescendantAdded += (sender, args) => { };
            ChildRemoved += (sender, args) => { };
            ChildAdded += (sender, args) => { };
            RequestedThemeChanged += (sender, args) => { };
            PageDisappearing += (sender, args) => { };
            PageAppearing += (sender, args) => { };
            ModalPushing += (sender, args) => { };
            ModalPopping += (sender, args) => { };
            ModalPopped += (sender, args) => { };
            ModalPushed += (sender, args) => { };
        }

        protected override IWindow CreateWindow(IActivationState activationState)
        {
            this.On<Microsoft.Maui.Controls.PlatformConfiguration.Windows>()
                .SetImageDirectory("Assets");

            return new Microsoft.Maui.Controls.Window(new MainPage());
        }
    }
}
