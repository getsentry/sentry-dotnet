namespace ELFSharp.ELF
{
    internal static class Consts
    {
        public const string ObjectsStringTableName = ".strtab";
        public const string DynamicStringTableName = ".dynstr";
        public const int SymbolEntrySize32 = 16;
        public const int SymbolEntrySize64 = 24;
        public const int MinimalELFSize = 16;
    }
}
