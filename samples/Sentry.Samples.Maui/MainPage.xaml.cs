using System;
using Microsoft.Maui.Controls;

namespace Sentry.Samples.Maui
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            this.Appearing += (sender, args) => { };
            this.Disappearing += (sender, args) => { };
            this.Focused += (sender, args) => { };
            this.Unfocused += (sender, args) => { };
            this.BatchCommitted += (sender, args) => { };
            this.ChildAdded += (sender, args) => { };
            this.ChildRemoved += (sender, args) => { };
            this.ChildrenReordered += (sender, args) => { };
            this.DescendantAdded += (sender, args) => { };
            this.DescendantRemoved += (sender, args) => { };
            this.HandlerChanged += (sender, args) => { };
            this.HandlerChanging += (sender, args) => { };
            this.LayoutChanged += (sender, args) => { };
            this.MeasureInvalidated += (sender, args) => { };
            this.NavigatedFrom += (sender, args) => { };
            this.NavigatedTo += (sender, args) => { };
            this.NavigatingFrom += (sender, args) => { };
            this.ParentChanged += (sender, args) => { };
            this.ParentChanging += (sender, args) => { };
            this.PropertyChanged += (sender, args) => { };
            this.PropertyChanging += (sender, args) => { };
            this.SizeChanged += (sender, args) => { };
            this.FocusChangeRequested += (sender, args) => { };
            this.BindingContextChanged += (sender, args) => { };
        }

        int count = 0;
        private void OnCounterClicked(object sender, EventArgs e)
        {
            count++;
            CounterLabel.Text = $"Current count: {count}";
            SentrySdk.CaptureMessage(CounterLabel.Text);
        }
    }
}
