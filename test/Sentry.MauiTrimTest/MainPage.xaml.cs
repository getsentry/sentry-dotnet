namespace Sentry.MauiTrimTest;

public partial class MainPage : ContentPage
{

    /* Unmerged change from project 'Sentry.MauiTrimTest(net9.0-ios18.0)'
    Before:
        int count = 0;
    After:
        private int count = 0;
    */
    private int count = 0;

    public MainPage()
    {
        InitializeComponent();
    }

    private void OnCounterClicked(object sender, EventArgs e)
    {
        count++;

        if (count == 1)
            CounterBtn.Text = $"Clicked {count} time";
        else
            CounterBtn.Text = $"Clicked {count} times";

        SemanticScreenReader.Announce(CounterBtn.Text);
    }
}

