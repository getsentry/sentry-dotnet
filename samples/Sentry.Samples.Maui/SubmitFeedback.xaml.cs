using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Sentry.Samples.Maui;

public partial class SubmitFeedback : ContentPage
{
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

        // Handle the feedback submission logic here
        var feedback = new SentryFeedback(message, contactEmail, name);
        SentrySdk.CaptureFeedback(feedback);

        await DisplayAlert("Feedback Submitted", "Thank you for your feedback!", "OK");
        await Navigation.PopModalAsync();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private bool IsValidEmail(string email) => string.IsNullOrWhiteSpace(email) || EmailPattern().IsMatch(email);

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();
}
