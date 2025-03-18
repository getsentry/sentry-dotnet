#if NET9_0_OR_GREATER
/*
 * Adapted from https://github.com/dotnet/android/blob/86260ed36dfe1a90c8ed6a2bb1cd0607d637f403/tools/assembly-store-reader-mk2/AssemblyStore/AssemblyStoreItem.cs
 * Original code licensed under the MIT License (https://github.com/dotnet/android/blob/5ebcb1dd1503648391e3c0548200495f634d90c6/LICENSE.TXT)
 */

namespace Sentry.Android.AssemblyReader.V2;

abstract class AssemblyStoreItem
{
	public string Name                  { get; }
	public IList<ulong> Hashes          { get; }
	public bool Is64Bit                 { get; }
	public uint DataOffset              { get; protected set; }
	public uint DataSize                { get; protected set; }
	public uint DebugOffset             { get; protected set; }
	public uint DebugSize               { get; protected set; }
	public uint ConfigOffset            { get; protected set; }
	public uint ConfigSize              { get; protected set; }
	public AndroidTargetArch TargetArch { get; protected set; }

	protected AssemblyStoreItem (string name, bool is64Bit, List<ulong> hashes)
	{
		Name = name;
		Hashes = hashes.AsReadOnly ();
		Is64Bit = is64Bit;
	}
}

#endif
