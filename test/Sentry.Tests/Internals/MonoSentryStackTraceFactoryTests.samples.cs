
// ReSharper disable once CheckNamespace
// Stack trace filters out Sentry frames by namespace
namespace Other.Tests.Internals
{
    public partial class MonoSentryStackTraceFactoryTests
    {
        private const string UnityMonoJitAssert = @"  at UnityEngine.Assertions.Assert.Fail (System.String message, System.String userMessage) [0x0003c] in /Users/bokken/buildslave/unity/build/Runtime/Export/Assertions/Assert/AssertBase.cs:29
  at UnityEngine.Assertions.Assert.AreEqual[T] (T expected, T actual, System.String message, System.Collections.Generic.IEqualityComparer`1[T] comparer) [0x0004d] in /Users/bokken/buildslave/unity/build/Runtime/Export/Assertions/Assert/AssertGeneric.cs:31
  at UnityEngine.Assertions.Assert.AreEqual[T] (T expected, T actual, System.String message) [0x00001] in /Users/bokken/buildslave/unity/build/Runtime/Export/Assertions/Assert/AssertGeneric.cs:19
  at UnityEngine.Assertions.Assert.AreEqual[T] (T expected, T actual) [0x00001] in /Users/bokken/buildslave/unity/build/Runtime/Export/Assertions/Assert/AssertGeneric.cs:13
  at SampleScript.AssertFalse () [0x00002] in /Users/bruno/git/bruno-unity/sentry.unity/Assets/SampleScript.cs:53 ";

        private const string UnityMonoJitThrowCustomException = @"CustomException: A custom exception.
  at SampleScript.ThrowExceptionAndCatch () [0x0000d] in /Users/bruno/git/bruno-unity/sentry.unity/Assets/SampleScript.cs:71";

        private const string UnityMonoIl2CppAndroid =
}
