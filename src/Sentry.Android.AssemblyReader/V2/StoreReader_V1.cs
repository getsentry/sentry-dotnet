/*
 * Adapted from https://github.com/dotnet/android/blob/5ebcb1dd1503648391e3c0548200495f634d90c6/tools/assembly-store-reader-mk2/AssemblyStore/StoreReader_V1.cs
 * Original code licensed under the MIT License (https://github.com/dotnet/android/blob/5ebcb1dd1503648391e3c0548200495f634d90c6/LICENSE.TXT)
 */

namespace Sentry.Android.AssemblyReader.V2;

internal class StoreReader_V1 : AssemblyStoreReader
{
	public override string Description => "Assembly store v1";
	public override bool NeedsExtensionInName => false;

	public static IList<string> ApkPaths      { get; }
	public static IList<string> AabPaths      { get; }
	public static IList<string> AabBasePaths  { get; }

	static StoreReader_V1 ()
	{
		ApkPaths = new List<string> ().AsReadOnly ();
		AabPaths = new List<string> ().AsReadOnly ();
		AabBasePaths = new List<string> ().AsReadOnly ();
	}

	public StoreReader_V1 (Stream store, string path, DebugLogger? logger)
		: base (store, path, logger)
	{}

	protected override bool IsSupported ()
	{
		return false;
	}

	protected override void Prepare ()
	{
	}

	protected override ulong GetStoreStartDataOffset () => 0;
}
