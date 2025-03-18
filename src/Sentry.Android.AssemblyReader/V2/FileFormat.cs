#if NET9_0_OR_GREATER
/*
 * Adapted from https://github.com/dotnet/android/blob/5ebcb1dd1503648391e3c0548200495f634d90c6/tools/assembly-store-reader-mk2/AssemblyStore/FileFormat.cs
 * Original code licensed under the MIT License (https://github.com/dotnet/android/blob/5ebcb1dd1503648391e3c0548200495f634d90c6/LICENSE.TXT)
 */

namespace Sentry.Android.AssemblyReader.V2;

internal enum FileFormat
{
	Aab,
	AabBase,
	Apk,
	AssemblyStore,
	ELF,
	Zip,
	Unknown,
}

#endif
