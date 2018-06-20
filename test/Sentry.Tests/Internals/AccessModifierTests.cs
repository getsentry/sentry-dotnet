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

        [Theory]
        [MemberData(nameof(GetNonPublicTypes))]
        public void NonPublicTypes_AreInInternalsNamespace(Type internalType)
            => Assert.True(internalType.FullName.StartsWith(InternalsNamespace),
                $"Not in the expected namespace. Expected: {InternalsNamespace} but found: " + internalType.FullName);

        public static IEnumerable<object[]> GetNonPublicTypes()
            => typeof(ISentryClient).Assembly.GetTypes()
                .Where(t => !t.IsPublic && !t.IsNested && t.Namespace.StartsWith("Sentry"))
                .Select(t => new[] { t });

        public static IEnumerable<object[]> GetTypesInInternalsNamespace()
            => typeof(ISentryClient).Assembly.GetTypes(InternalsNamespace)
                .Select(t => new[] { t });
    }
}
