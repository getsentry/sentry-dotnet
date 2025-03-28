namespace ELFSharp.ELF.Sections
{
    internal interface IProgBitsSection : ISection
    {
        void WriteContents(byte[] destination, int offset, int length = 0);
    }
}