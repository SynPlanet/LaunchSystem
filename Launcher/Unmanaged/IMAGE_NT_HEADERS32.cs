using System.Runtime.InteropServices;

namespace Launcher.Unmanaged
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct IMAGE_NT_HEADERS32
    {
        public int Signature;
        public IMAGE_FILE_HEADER FileHeader;
        public IMAGE_OPTIONAL_HEADER32 OptionalHeader;
    }
}
