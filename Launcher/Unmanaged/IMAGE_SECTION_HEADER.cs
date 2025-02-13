using System;
using System.Runtime.InteropServices;

namespace Launcher.Unmanaged
{
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    struct IMAGE_SECTION_HEADER
    {
        [FieldOffset(0)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public char[] Name;
        [FieldOffset(8)]
        public UInt32 PhysicalAddress;
        [FieldOffset(8)]
        public UInt32 VirtualSize;
        [FieldOffset(12)]
        public UInt32 VirtualAddress;
        [FieldOffset(16)]
        public UInt32 SizeOfRawData;
        [FieldOffset(20)]
        public UInt32 PointerToRawData;
        [FieldOffset(24)]
        public UInt32 PointerToRelocations;
        [FieldOffset(28)]
        public UInt32 PointerToLinenumbers;
        [FieldOffset(30)]
        public UInt16 NumberOfRelocations;
        [FieldOffset(32)]
        public UInt16 NumberOfLinenumbers;
        [FieldOffset(36)]
        public DataSectionFlags Characteristics;

        public override string ToString()
        {
            return new string(Name);
        }
    }
}
