using System;
using System.Collections.Generic;
using Sentry.Protocol;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Sentry.Samples.Uwp
{
    public sealed partial class AboutPage : Page
    {
        public AboutPage() => InitializeComponent();

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            SentrySdk.AddBreadcrumb(null,
                "navigation",
                "navigation",
                new Dictionary<string, string>()
                {
                    { "to", $"/{e.SourcePageType.Name}" },
                    { "from", $"{BaseUri.LocalPath}" }
                });
            base.OnNavigatedFrom(e);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private void Report_Error_Click(object sender, RoutedEventArgs e)
        {
            var sentryId = HandledErrorFunction();
            if (!sentryId.Equals(SentryId.Empty))
            {
                var dialog = new UserFeedbackDialog(sentryId);
                _ = dialog.ShowAsync();
            }
        }

        private SentryId HandledErrorFunction()
        {
            try
            {
                int doSomething = 1;
                doSomething--;
                int zeroDivision = 5 / doSomething;
                return SentryId.Empty;
            }
            catch (Exception ex)
            {
                SentrySdk.AddBreadcrumb(ex.Message, level: BreadcrumbLevel.Error);
                return  SentrySdk.CaptureException(ex);
            }
        }
    }
}
