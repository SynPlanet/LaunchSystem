using System.Runtime.InteropServices;

namespace Launcher.Unmanaged
{
    [StructLayout(LayoutKind.Sequential)]
    struct IMAGE_BASE_RELOCATION
    {
        public uint VirtualAddress;
        public uint SizeOfBlock;
    }
}