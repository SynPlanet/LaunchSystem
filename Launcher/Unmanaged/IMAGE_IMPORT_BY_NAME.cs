using System.Runtime.InteropServices;

namespace Launcher.Unmanaged
{
    [StructLayout(LayoutKind.Sequential)]
    struct IMAGE_IMPORT_BY_NAME
    {
        public ushort Hint;
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] Name;
    }
}