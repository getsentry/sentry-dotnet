using System;
using System.Linq;
using Xunit;

namespace Sentry.Tests.Internals
{
    public class AccessModifierTests
    {
        private const string InternalsNamespace = "Sentry.Internal";

        [Fact]
        public void TypesInInternalsNamespace_AreNotPublic()
        {
            var types = typeof(ISentryClient).Assembly
                .GetTypes()
                .Where(t => t.Namespace?.StartsWith(InternalsNamespace, StringComparison.Ordinal) == true)
                .ToArray();

            Assert.All(types, type =>
            {
                Assert.False(type.IsPublic, $"Expected type {type.Name} to be internal.");
            });
        }
    }
}
