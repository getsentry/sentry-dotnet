#if !NET7_0_OR_GREATER
// ReSharper disable CheckNamespace
namespace System.Diagnostics;

internal sealed class UnreachableException : Exception
{
    public UnreachableException()
        : base("The program executed an instruction that was thought to be unreachable.")
    {
    }

    public UnreachableException(string? message)
        : base(message ?? "The program executed an instruction that was thought to be unreachable.")
    {
    }

    public UnreachableException(string? message, Exception? innerException)
        : base(message ?? "The program executed an instruction that was thought to be unreachable.", innerException)
    {
    }
}
#endif
