namespace ELFSharp.ELF.Sections
{
    /// <summary>
    ///     Represents an entry in the dynamic table.
    ///     Interface--because this is a union type in C, if we want more detail at some point on the values in the Union type,
    ///     we can have separate classes.
    /// </summary>
    internal interface IDynamicEntry
    {
        DynamicTag Tag { get; }
    }
}