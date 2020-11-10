using System;
using System.Collections.Generic;
using System.Numerics;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;

namespace Sentry.Samples.Uwp
{
    public sealed partial class MainPage : Page
    {
        public bool CookerEnabled = false;
        public static bool BulbOn = false;
        public static bool Lock;

        public static Image BuldRef;
        public static Image ScreenEffectRef;
        public static Rectangle BuldEffectRef;

        public static Compositor Compositor;
        public static CompositionLinearGradientBrush GradientBrush;
        public static CompositionColorGradientStop TopGradientStop;
        public static CompositionColorGradientStop BottomGradientStop;
        public static SpriteVisual BackgroundVisual;

        public static new  Uri BaseUri = null;

        public MainPage()
        {
            InitializeComponent();

            Compositor = Window.Current.Compositor;

            //Set Background color.
            GradientBrush = Compositor.CreateLinearGradientBrush();
            GradientBrush.StartPoint = new Vector2(0.5f, 0);
            GradientBrush.EndPoint = new Vector2(0.5f, 1);

            TopGradientStop = Compositor.CreateColorGradientStop(0, Color.FromArgb(255, 0, 0, 0));
            BottomGradientStop = Compositor.CreateColorGradientStop(0.352f, Color.FromArgb(255, 51, 27, 59));
            GradientBrush.ColorStops.Add(BottomGradientStop);
            GradientBrush.ColorStops.Add(TopGradientStop);
            BackgroundVisual = Compositor.CreateSpriteVisual();
            BackgroundVisual.Brush = GradientBrush;
            ElementCompositionPreview.SetElementChildVisual(Gradient, BackgroundVisual);

            BuldRef = Bulb;
            BuldEffectRef = BuldLight;
            ScreenEffectRef = ScreenEffect;

            BaseUri = base.BaseUri;

            Gradient.SizeChanged += (s, e) =>
            {
                if (e.NewSize == e.PreviousSize)
                {
                    return;
                }
                BackgroundVisual.Size = e.NewSize.ToVector2();
                GradientBrush.CenterPoint = BackgroundVisual.Size / 2;
            };
        }

        private void Message_Click(object sender, RoutedEventArgs e)
        {
            if (Lock)
            {
                return;
            }
            SentrySdk.AddBreadcrumb("message", "ui.click");
            _ = SentrySdk.CaptureMessage("Hello UWP");
            _ = new MessageDialog("Hello UWP").ShowAsync();
        }
        private void Navigation_Click(object sender, RoutedEventArgs e)
        {
            if (Lock)
            {
                return;
            }
            SentrySdk.AddBreadcrumb(null, "navigation", "navigation", new Dictionary<string, string>() { { "to", $"/AboutPage" }, { "from", $"/MainPage" } });
            _ = Frame.Navigate(typeof(AboutPage));

        }

        private async void Cooker_Click(object sender, RoutedEventArgs e)
        {
            SentrySdk.AddBreadcrumb("cooker on", "ui.click");
            CookerEnabled = true;
            Cooker.IsEnabled = false;
            Cooker.Content = " Cooker is on";
            await Action.TurnOnCooker();
        }

        private void BuldSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (Lock)
            {
                return;
            }
            if (BulbOn)
            {
                SentrySdk.AddBreadcrumb("Light off", "ui.click");
                Bulb.Source = new BitmapImage(new Uri(BaseUri, @"/Assets/bulboff.png"));
                BuildSwitch.Content = "Turn on light";
                BuldLight.Visibility = Visibility.Collapsed;
            }
            else
            {
                SentrySdk.AddBreadcrumb("Light on", "ui.click");
                Bulb.Source = new BitmapImage(new Uri(BaseUri, @"/Assets/bulbon.png"));
                BuildSwitch.Content = "Turn off light";
                BuldLight.Visibility = Visibility.Visible;
            }
            BulbOn = !BulbOn;
        }
    }
}
