using System.Collections.Generic;
using System.Diagnostics;
using Foundation;
using UIKit;

namespace Sentry.Samples.Mobile.iOS
{
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {
        // This method is invoked when the application has loaded and is ready to run. In this
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            var sw = Stopwatch.StartNew();
            global::Xamarin.Forms.Forms.Init();
            LoadApplication(new App());

            var @return = base.FinishedLaunching(app, options);

            SentrySdk.AddBreadcrumb(
                "FinishedLaunching",
                "app.lifecycle",
                "event",
                new Dictionary<string, string>()
            {
                {"timing", sw.Elapsed.ToString()},
                {"return", @return.ToString()}
            });
            return @return;
        }
    }
}
