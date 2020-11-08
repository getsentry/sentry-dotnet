using Sentry.Protocol;
using Windows.UI.Xaml.Controls;

namespace Sentry.Samples.Uwp
{
    public sealed partial class UserFeedbackDialog : ContentDialog
    {
        private SentryId _sentryId;
        public UserFeedbackDialog(SentryId sentryId)
        {
            _sentryId = sentryId;
            InitializeComponent();
        }

        private void ContentDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void ContentDialog_SendButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // SentryId, Email and Comments are required.
            SentrySdk.CaptureUserFeedback(
                _sentryId,
                EmailBox.Text,
                CommentBox.Text,
                string.IsNullOrWhiteSpace(NameBox.Text) ? null : NameBox.Text);
        }
    }
}
