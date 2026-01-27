#if __ANDROID__
using Sentry.JavaSdk;

// ReSharper disable once CheckNamespace - match generated code namespace
namespace Sentry.JavaSdk.Android.Replay;

// Add IReplayBreadcrumbConverter interface to DefaultReplayBreadcrumbConverter, to work source generator issue
internal partial class DefaultReplayBreadcrumbConverter : IReplayBreadcrumbConverter
{
}
#endif
