using Sentry.PlatformAbstractions;
using Xunit;

namespace Sentry.Testing
{
    public sealed class NoMonoFact : FactAttribute
    {
        public override string Skip
        {
            get => Runtime.Current.IsMono() ? "Not compatible with Mono" : null;
            set => _ = value;
        }
    }
}
