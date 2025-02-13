using System;
using System.Runtime.InteropServices;

namespace Launcher.Unmanaged
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct MEMORYMODULE
    {
        public IntPtr headers;
        public UIntPtr codeBase;
        public IntPtr modules;
        public int numModules;
        public int initialized;
    }
}