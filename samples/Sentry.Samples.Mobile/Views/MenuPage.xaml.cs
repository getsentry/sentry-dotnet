using Sentry.Samples.Mobile.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Sentry.Protocol;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Sentry.Samples.Mobile.Views
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MenuPage : ContentPage
    {
        MainPage RootPage { get => Application.Current.MainPage as MainPage; }
        List<HomeMenuItem> menuItems;
        public MenuPage()
        {
            InitializeComponent();

            menuItems = new List<HomeMenuItem>
            {
                new HomeMenuItem {Id = MenuItemType.Browse, Title="Browse" },
                new HomeMenuItem {Id = MenuItemType.About, Title="Unhandled Exception" }
            };

            ListViewMenu.ItemsSource = menuItems;

            ListViewMenu.SelectedItem = menuItems[0];
            ListViewMenu.ItemSelected += async (sender, e) =>
            {
                if (e.SelectedItem == null)
                {
                    SentrySdk.CaptureMessage("No item selected.", SentryLevel.Warning);
                    return;
                }

                var id = (int)((HomeMenuItem)e.SelectedItem).Id;
                SentrySdk.AddBreadcrumb(
                    $"Navigating to {((HomeMenuItem)e.SelectedItem).Title}",
                    "app.lifecycle",
                    "navigation");

                await RootPage.NavigateFromMenu(id);
            };
        }
    }
}
