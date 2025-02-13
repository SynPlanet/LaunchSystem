using System.Runtime.InteropServices;

namespace Launcher.Unmanaged
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct IMAGE_FILE_HEADER
    {
        public ushort Machine;
        public ushort NumberOfSections;
        public uint TimeDateStamp;
        public uint PointerToSymbolTable;
        public uint NumberOfSymbols;
        public ushort SizeOfOptionalHeader;
        public ushort Characteristics;
    }
}
