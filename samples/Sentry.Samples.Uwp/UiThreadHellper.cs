using Windows.Foundation;
using Windows.UI.Core;

namespace Sentry.Samples.Uwp
{
    public static class UiThreadHelper
    {
        public static IAsyncAction RunAsync(CoreDispatcherPriority priority, DispatchedHandler agileCallback)
        {
            return Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(priority, agileCallback);
        }
    }
}
