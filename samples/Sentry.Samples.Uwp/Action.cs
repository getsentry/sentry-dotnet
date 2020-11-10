using System;
using System.IO;
using System.Threading.Tasks;
using Sentry.Protocol;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace Sentry.Samples.Uwp
{
    internal static class Action
    {
        internal static async Task TurnOnCooker()
        {
            var topgasAnimation = MainPage.Compositor.CreateColorKeyFrameAnimation();
            topgasAnimation.Duration = TimeSpan.FromSeconds(5);
            topgasAnimation.InsertKeyFrame(1.0f, Color.FromArgb(255, 51, 75, 59));

            var bottomgasAnimation = MainPage.Compositor.CreateColorKeyFrameAnimation();
            bottomgasAnimation.Duration = TimeSpan.FromSeconds(5);
            bottomgasAnimation.InsertKeyFrame(1.0f, Color.FromArgb(255, 0, 63, 0));

            MainPage.TopGradientStop.StartAnimation(nameof(MainPage.TopGradientStop.Color), topgasAnimation);
            MainPage.BottomGradientStop.StartAnimation(nameof(MainPage.BottomGradientStop.Color), bottomgasAnimation);

            SentrySdk.AddBreadcrumb("Gas leak detected", "warning", level: BreadcrumbLevel.Warning);
            await GasLeak();
        }

        internal static async Task GasLeak()
        {
                MediaElement mediaElement = null;
                var tuple = await LoadStreamAsync();
                await UiThreadHelper.RunAsync(CoreDispatcherPriority.High, () =>
                {
                    mediaElement = new MediaElement();
                    mediaElement.AutoPlay = false;
                    mediaElement.SetSource(tuple.Item1, tuple.Item2);
                });

                await Task.Delay(3000);
                while (!MainPage.BulbOn)
                {
                    await Task.Delay(10);
                }
                await IgnitionStart(mediaElement);
        }

        private static async Task IgnitionStart(MediaElement mediaElement)
        {
            MainPage.BulbOn = false;
            await ExplodeSoundAsync(mediaElement);
            //Lock the UI.
            MainPage.Lock = true;

            await UiThreadHelper.RunAsync(CoreDispatcherPriority.High, () =>
            {
                MainPage.BuldEffectRef.Visibility = Visibility.Collapsed;
                MainPage.BuldRef.Source = new BitmapImage(new Uri(MainPage.BaseUri, @"/Assets/bulboff.png"));
            });

            await Task.Delay(10);
            await Explosion();

        }

        private static async Task Explosion()
        {
            var topgasAnimation = MainPage.Compositor.CreateColorKeyFrameAnimation();
            topgasAnimation.Duration = TimeSpan.FromMilliseconds(1000);

            topgasAnimation.InsertKeyFrame(0.7f, Color.FromArgb(255, 255, 162, 85));
            topgasAnimation.InsertKeyFrame(1.0f, Color.FromArgb(255, 255, 255, 255));

            var bottomgasAnimation = MainPage.Compositor.CreateColorKeyFrameAnimation();
            bottomgasAnimation.Duration = TimeSpan.FromMilliseconds(1000);
            bottomgasAnimation.InsertKeyFrame(0.7f, Color.FromArgb(255, 255, 91, 20));
            bottomgasAnimation.InsertKeyFrame(1.0f, Color.FromArgb(255, 255, 255, 255));

            MainPage.TopGradientStop.StartAnimation(nameof(MainPage.TopGradientStop.Color), topgasAnimation);
            MainPage.BottomGradientStop.StartAnimation(nameof(MainPage.BottomGradientStop.Color), bottomgasAnimation);

            await ScreenCrack();
        }


        private static async Task ScreenCrack()
        {
            await Task.Delay(1400);
            await UiThreadHelper.RunAsync(CoreDispatcherPriority.High, () =>
            {
                MainPage.ScreenEffectRef.Visibility = Visibility.Visible;
            });
            await Task.Delay(300);
            throw new InternalBufferOverflowException("Failed to handle explosion");
        }

        private static async Task<Tuple<IRandomAccessStream, string>> LoadStreamAsync()
        {
            Windows.Storage.StorageFolder folder = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFolderAsync("Assets");
            Windows.Storage.StorageFile file = await folder.GetFileAsync("explosion.wav");
            var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
            return new Tuple<IRandomAccessStream, string>(stream, file.ContentType);
        }

        internal static async Task ExplodeSoundAsync(MediaElement mysong)
        {
            await UiThreadHelper.RunAsync(CoreDispatcherPriority.Normal,
            () => mysong.Play());
        }
    }
}
