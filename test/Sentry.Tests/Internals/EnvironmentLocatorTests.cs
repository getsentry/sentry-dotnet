using System;
using Sentry.Internal;
using Sentry.Testing;
using Xunit;

namespace Sentry.Tests.Internals
{
    public class EnvironmentLocatorTests
    {
        [Fact]
        public void Locate_WithEnvironmentVariable_ReturnsEnvironmentVariableValue()
        {
            const string expectedVersion = "the environment name";
            EnvironmentVariableGuard.WithVariable(
                Constants.EnvironmentEnvironmentVariable,
                expectedVersion,
                () =>
                {
                    Assert.Equal(expectedVersion, EnvironmentLocator.Locate());
                });
        }

        [Fact]
        public void Locate_WithoutEnvironmentVariable_ReturnsNull()
        {
            EnvironmentVariableGuard.WithVariable(
                Constants.ReleaseEnvironmentVariable,
                null,
                () =>
                {
                    Assert.Null(EnvironmentLocator.Locate());
                });
        }

        [Fact]
        public void Current_CachesValue()
        {
            var expected = EnvironmentLocator.Current;
            EnvironmentVariableGuard.WithVariable(
                Constants.ReleaseEnvironmentVariable,
                Guid.NewGuid().ToString(),
                () =>
                {
                    Assert.Equal(expected, EnvironmentLocator.Current);
                });
        }
    }
}
