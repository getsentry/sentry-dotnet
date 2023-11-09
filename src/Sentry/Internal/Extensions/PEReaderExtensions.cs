using Sentry.Extensibility;
using Sentry.Protocol;

namespace Sentry.Internal.Extensions;

internal static class PEReaderExtensions
{
    public static PEDebugImageData? TryGetPEDebugImageData(this PEReader peReader)
    {
        try
        {
            return peReader.GetPEDebugImageData();
        }
        catch
        {
            return null;
        }
    }

    private static PEDebugImageData GetPEDebugImageData(this PEReader peReader)
    {
        var headers = peReader.PEHeaders;
        var codeId = headers.PEHeader is { } peHeader
            ? $"{headers.CoffHeader.TimeDateStamp:X8}{peHeader.SizeOfImage:x}"
            : null;

        string? debugId = null;
        string? debugFile = null;
        string? debugChecksum = null;

        var debugDirectoryEntries = peReader.ReadDebugDirectory();

        foreach (var entry in debugDirectoryEntries)
        {
            switch (entry.Type)
            {
                case DebugDirectoryEntryType.PdbChecksum:
                    {
                        var checksum = peReader.ReadPdbChecksumDebugDirectoryData(entry);
                        var checksumHex = checksum.Checksum.AsSpan().ToHexString();
                        debugChecksum = $"{checksum.AlgorithmName}:{checksumHex}";
                        break;
                    }

                case DebugDirectoryEntryType.CodeView:
                    {
                        var codeView = peReader.ReadCodeViewDebugDirectoryData(entry);
                        debugFile = codeView.Path;

                        // Specification:
                        // https://github.com/dotnet/runtime/blob/main/docs/design/specs/PE-COFF.md#codeview-debug-directory-entry-type-2
                        //
                        // See also:
                        // https://learn.microsoft.com/dotnet/csharp/language-reference/compiler-options/code-generation#debugtype
                        //
                        // Note: Matching PDB ID is stored in the #Pdb stream of the .pdb file.

                        if (entry.IsPortableCodeView)
                        {
                            // Portable PDB Format
                            // Version Major=any, Minor=0x504d
                            debugId = $"{codeView.Guid}-{entry.Stamp:x8}";
                        }
                        else
                        {
                            // Full PDB Format (Windows only)
                            // Version Major=0, Minor=0
                            debugId = $"{codeView.Guid}-{codeView.Age}";
                        }

                        break;
                    }
            }

            if (debugId != null && debugChecksum != null)
            {
                // No need to keep looking, once we have both.
                break;
            }
        }

        return new PEDebugImageData
        {
            CodeId = codeId,
            DebugId = debugId,
            DebugChecksum = debugChecksum,
            DebugFile = debugFile
        };
    }
}

/// <summary>
/// The subset of information about a DebugImage that we can obtain from a PE file.
/// This rest of the information is obtained from the Module.
/// </summary>
internal sealed class PEDebugImageData
{
    public string Type => "pe_dotnet";
    public string? ImageAddress { get; set; }
    public long? ImageSize { get; set; }
    public string? DebugId { get; set; }
    public string? DebugChecksum { get; set; }
    public string? DebugFile { get; set; }
    public string? CodeId { get; set; }
}

internal static class PEDebugImageDataExtensions
{
    internal static DebugImage? ToDebugImage(this PEDebugImageData? imageData, string? codeFile, Guid? moduleVersionId)
    {
        return imageData?.DebugId == null
            ? null
            : new DebugImage
            {
                Type = imageData.Type,
                CodeId = imageData.CodeId,
                CodeFile = codeFile,
                DebugId = imageData.DebugId,
                DebugChecksum = imageData.DebugChecksum,
                DebugFile = imageData.DebugFile,
                ModuleVersionId = moduleVersionId
            };
    }
}
