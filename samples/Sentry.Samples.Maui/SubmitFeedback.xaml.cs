using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Sentry.Samples.Maui;

public partial class SubmitFeedback : ContentPage
{
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();

    private string _screenshotPath;

    private bool IsValidEmail(string email) => string.IsNullOrWhiteSpace(email) || EmailPattern().IsMatch(email);

    public SubmitFeedback()
    {
        InitializeComponent();
    }

    private async void OnSubmitClicked(object sender, EventArgs e)
    {
        var message = MessageEditor.Text;
        var contactEmail = ContactEmailEntry.Text;
        var name = NameEntry.Text;

        if (string.IsNullOrWhiteSpace(message))
        {
            await DisplayAlert("Validation Error", "Message is required.", "OK");
            return;
        }

        if (!IsValidEmail(contactEmail))
        {
            await DisplayAlert("Validation Error", "Please enter a valid email address.", "OK");
            return;
        }

        SentryHint hint = null;
        if (!string.IsNullOrEmpty(_screenshotPath))
        {
            hint = new SentryHint();
            hint.AddAttachment(_screenshotPath, AttachmentType.Default, "image/png");
        }

        // Handle the feedback submission logic here
        var feedback = new SentryFeedback(message, contactEmail, name);
        SentrySdk.CaptureFeedback(feedback, hint: hint);

        await DisplayAlert("Feedback Submitted", "Thank you for your feedback!", "OK");
        await Navigation.PopModalAsync();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private async void OnAttachScreenshotClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.PickAsync(new PickOptions
            {
                FileTypes = FilePickerFileType.Images,
                PickerTitle = "Select a screenshot"
            });

            if (result != null)
            {
                _screenshotPath = result.FullPath;
                await DisplayAlert("Screenshot Attached", "Screenshot has been attached successfully.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"An error occurred while selecting the screenshot: {ex.Message}", "OK");
        }    }
}
