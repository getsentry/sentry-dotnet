#if __ANDROID__
using Sentry.JavaSdk;

// ReSharper disable once CheckNamespace - match generated code namespace
namespace Sentry.JavaSdk.Android.Replay;

// This partial augments the generated binding to implement the managed interface... to work arould
// a problem with source generators for the bindings
internal partial class DefaultReplayBreadcrumbConverter : IReplayBreadcrumbConverter
{
}
#endif
