using Microsoft.Maui.Controls;

namespace MauiAppSegfault
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new MainPage();
        }
    }
}
