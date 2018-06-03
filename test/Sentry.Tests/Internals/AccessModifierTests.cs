using System;
using System.Collections.Generic;
using System.Linq;
using Sentry.Extensibility;
using Sentry.Tests.Helpers.Reflection;
using Xunit;

namespace Sentry.Tests.Internals
{
    public class AccessModifierTests
    {
        private const string InternalsNamespace = "Sentry.Internals";

        [Theory]
        [MemberData(nameof(GetTypesInInternalsNamespace))]
        public void TypesInInternalsNamespace_AreNotPublic(Type typeInNamespace)
            => Assert.True(!typeInNamespace.IsPublic);

        [Theory]
        [MemberData(nameof(GetNonPublicTypes))]
        public void NonPublicTypes_AreInInternalsNamespace(Type internalType)
            => Assert.StartsWith(InternalsNamespace, internalType.Namespace);

        public static IEnumerable<object[]> GetNonPublicTypes()
            => typeof(ISentryClient).Assembly.GetTypes()
                .Where(t => !t.IsPublic && !t.IsNested && t.Namespace.StartsWith("Sentry"))
                .Select(t => new[] { t });

        public static IEnumerable<object[]> GetTypesInInternalsNamespace()
            => typeof(ISentryClient).Assembly.GetTypes("Sentry.Internals")
                .Select(t => new[] { t });
    }
}
