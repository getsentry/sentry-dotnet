using System;
using System.Collections.Generic;
using System.Linq;
using Sentry.Tests.Helpers.Reflection;
using Xunit;

namespace Sentry.Tests.Internals
{
    public class AccessModifierTests
    {
        private const string InternalsNamespace = "Sentry.Internal";

        [Theory]
        [MemberData(nameof(GetTypesInInternalsNamespace))]
        public void TypesInInternalsNamespace_AreNotPublic(Type typeInNamespace)
            => Assert.True(!typeInNamespace.IsPublic,
                $"Expected type {typeInNamespace.Name} to be internal.");

        public static IEnumerable<object[]> GetTypesInInternalsNamespace()
            => typeof(ISentryClient).Assembly.GetTypes(InternalsNamespace)
                .Select(t => new[] { t });
    }
}
