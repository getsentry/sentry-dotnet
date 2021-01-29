using Sentry.Internal;
using Sentry.Testing;
using Sentry.Tests.Helpers.Reflection;
using Xunit;
using static Sentry.Internal.Constants;
using static Sentry.Constants;

namespace Sentry.Tests.Internals
{
    public class DsnLocatorTests
    {
        [Fact]
        public void FindDsnOrDisable_NoEnvironmentVariableNorAttribute_ReturnsDisabledDsn()
        {
            EnvironmentVariableGuard.WithVariable(
                DsnEnvironmentVariable,
                null,
                () =>
                {
                    Assert.Equal(DisableSdkDsnValue, DsnLocator.FindDsnStringOrDisable());
                });
        }

        [Fact]
        public void FindDsnOrDisable_DsnOnEnvironmentVariable_ReturnsTheDsn()
        {
            const string expected = DsnSamples.ValidDsnWithoutSecret;

            EnvironmentVariableGuard.WithVariable(
                DsnEnvironmentVariable,
                expected,
                () =>
                {
                    Assert.Equal(expected, DsnLocator.FindDsnStringOrDisable());
                });
        }

        [Fact]
        public void FindDsnOrDisable_DsnOnEnvironmentVariableAndAttribute_ReturnsTheDsnFromEnvironmentVariable()
        {
            const string expected = DsnSamples.ValidDsnWithoutSecret;

            EnvironmentVariableGuard.WithVariable(
                DsnEnvironmentVariable,
                expected,
                () =>
                {
                    var asm = AssemblyCreationHelper.CreateAssemblyWithDsnAttribute(DsnSamples.ValidDsnWithSecret);
                    Assert.Equal(expected, DsnLocator.FindDsnStringOrDisable(asm));
                });
        }

        [Fact]
        public void FindDsn_NoDsnInAsm_ReturnsNull()
        {
            var asm = AssemblyCreationHelper.CreateAssembly();
            var actual = DsnLocator.FindDsn(asm);

            Assert.Null(actual);
        }

        [Fact]
        public void FindDsn_ValidDsnInAsm_FindsTheDsnString()
        {
            const string expected = DsnSamples.ValidDsnWithoutSecret;

            var asm = AssemblyCreationHelper.CreateAssemblyWithDsnAttribute(expected);
            var actual = DsnLocator.FindDsn(asm);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void FindDsn_NullDsnInAsm_ReturnsNull()
        {
            const string expected = null;

            var asm = AssemblyCreationHelper.CreateAssemblyWithDsnAttribute(expected);
            var actual = DsnLocator.FindDsn(asm);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void FindDsn_InvalidDsnInAsm_ReturnsInvalidDsn()
        {
            const string expected = DsnSamples.InvalidDsn;

            var asm = AssemblyCreationHelper.CreateAssemblyWithDsnAttribute(expected);

            // Not responsible to do validation, returns raw string
            var actual = DsnLocator.FindDsn(asm);

            Assert.Equal(expected, actual);
        }
    }
}
