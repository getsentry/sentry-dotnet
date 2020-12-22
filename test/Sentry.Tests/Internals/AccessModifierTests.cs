using System.Linq;
using Sentry.Tests.Helpers.Reflection;
using Xunit;

namespace Sentry.Tests.Internals
{
    public class AccessModifierTests
    {
        private const string InternalsNamespace = "Sentry.Internal";

        [Fact]
        public void TypesInInternalsNamespace_AreNotPublic()
        {
            var types = typeof(ISentryClient).Assembly.GetTypes(InternalsNamespace).ToArray();

            Assert.All(types, type =>
            {
                Assert.False(type.IsPublic, $"Expected type {type.Name} to be internal.");
            });
        }
    }
}
