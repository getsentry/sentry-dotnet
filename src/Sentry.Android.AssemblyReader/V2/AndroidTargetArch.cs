/*
 * Adapted from https://github.com/dotnet/android-tools/blob/ab2165daf27d4fcb29e88bc022e0ab0be33aff69/src/Xamarin.Android.Tools.AndroidSdk/AndroidTargetArch.cs
 * Original code licensed under the MIT License (https://github.com/dotnet/android-tools/blob/ab2165daf27d4fcb29e88bc022e0ab0be33aff69/LICENSE)
 */
#if NET9_0_OR_GREATER

namespace Sentry.Android.AssemblyReader.V2;

[Flags]
internal enum AndroidTargetArch
{
    None = 0,
    Arm = 1,
    X86 = 2,
    Mips = 4,
    Arm64 = 8,
    X86_64 = 16,
    Other = 0x10000 // hope it's not too optimistic
}

#endif
