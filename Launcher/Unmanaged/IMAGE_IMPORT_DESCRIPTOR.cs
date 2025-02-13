using System;
using System.Runtime.InteropServices;

namespace Launcher.Unmanaged
{
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    struct IMAGE_IMPORT_DESCRIPTOR
    {
        [FieldOffset(0)]
        public UInt32 Characteristics;
        [FieldOffset(0)]
        public UInt32 OriginalFirstThunk;
        [FieldOffset(4)]
        public UInt32 TimeDateStamp;
        [FieldOffset(8)]
        public UInt32 ForwarderChain;
        [FieldOffset(12)]
        public UInt32 Name;
        [FieldOffset(16)]
        public UInt32 FirstThunk;
    }
}
