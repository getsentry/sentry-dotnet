using Sentry.Internal.Extensions;
using Sentry.Extensibility;

namespace Sentry.Internal;

/// <summary>
/// Mockable variant of the Diagnostics.StackFrame.
/// This is necessary to test NativeAOT code that relies on extensions from StackFrameExtensions.
/// </summary>
internal interface IStackFrame
{
    StackFrame? Frame { get; }

    /// <summary>
    /// Returns a pointer to the base address of the native image that this stack frame is executing.
    /// </summary>
    /// <returns>
    /// A pointer to the base address of the native image or System.IntPtr.Zero if you're targeting the .NET Framework.
    /// </returns>
    public nint GetNativeImageBase();

    /// <summary>
    /// Gets an interface pointer to the start of the native code for the method that is being executed.
    /// </summary>
    /// <returns>
    /// An interface pointer to the start of the native code for the method that is being
    /// executed or System.IntPtr.Zero if you're targeting the .NET Framework.
    /// </returns>
    public nint GetNativeIP();

    /// <summary>
    /// Indicates whether the native image is available for the specified stack frame.
    /// </summary>
    /// <returns>
    /// true if a native image is available for this stack frame; otherwise, false.
    /// </returns>
    public bool HasNativeImage();

    /// <summary>
    /// Gets the column number in the file that contains the code that is executing.
    /// This information is typically extracted from the debugging symbols for the executable.
    /// </summary>
    /// <returns>
    /// The file column number, or 0 (zero) if the file column number cannot be determined.
    /// </returns>
    public int GetFileColumnNumber();

    /// <summary>
    /// Gets the line number in the file that contains the code that is executing. This
    /// information is typically extracted from the debugging symbols for the executable.
    /// </summary>
    /// <returns>
    /// The file line number, or 0 (zero) if the file line number cannot be determined.
    /// </returns>
    public int GetFileLineNumber();

    /// <summary>
    /// Gets the file name that contains the code that is executing. This information
    /// is typically extracted from the debugging symbols for the executable.
    /// </summary>
    /// <returns>
    /// The file name, or null if the file name cannot be determined.
    /// </returns>
    public string? GetFileName();

    /// <summary>
    /// Gets the offset from the start of the Microsoft intermediate language (MSIL)
    /// code for the method that is executing. This offset might be an approximation
    /// depending on whether or not the just-in-time (JIT) compiler is generating debugging
    /// code. The generation of this debugging information is controlled by the System.Diagnostics.DebuggableAttribute.
    /// </summary>
    /// <returns>
    /// The offset from the start of the MSIL code for the method that is executing.
    /// </returns>
    public int GetILOffset();

    /// <summary>
    /// Gets the method in which the frame is executing.
    /// </summary>
    /// <returns>
    /// The method in which the frame is executing.
    /// </returns>
    public MethodBase? GetMethod();

    /// <summary>
    /// Builds a readable representation of the stack trace.
    /// </summary>
    /// <returns>
    /// A readable representation of the stack trace.
    /// </returns>
    public string ToString();
}

internal class RealStackFrame : IStackFrame
{
    private readonly StackFrame _frame;

    public RealStackFrame(StackFrame frame)
    {
        _frame = frame;
    }

    public StackFrame? Frame => _frame;

    public override string ToString() => _frame.ToString();

    public int GetFileColumnNumber() => _frame.GetFileColumnNumber();

    public int GetFileLineNumber() => _frame.GetFileLineNumber();

    public string? GetFileName() => _frame.GetFileName();

    public int GetILOffset() => _frame.GetILOffset();

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = AotHelper.SuppressionJustification)]
    public MethodBase? GetMethod() => AotHelper.IsNativeAot
#if !NET8_0_OR_GREATER
#pragma warning disable CS0162 // Unreachable code detected
        // Only unreachable outside NET8_0_OR_GREATER
        // ReSharper disable once HeuristicUnreachableCode
        ? null
#pragma warning restore CS0162 // Unreachable code detected
#endif
        : _frame.GetMethod();

#if NET5_0_OR_GREATER
    public nint GetNativeImageBase() => _frame.GetNativeImageBase();

    public nint GetNativeIP() => _frame.GetNativeIP();

    public bool HasNativeImage() => _frame.HasNativeImage();
#else
    public nint GetNativeImageBase() => default;

    public nint GetNativeIP() => default;

    public bool HasNativeImage() => false;
#endif
}
