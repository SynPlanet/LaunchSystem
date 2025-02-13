using System;
using System.Runtime.InteropServices;

namespace Launcher.Unmanaged
{
    static class Kernel32
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern UIntPtr VirtualAlloc(UIntPtr lpAddress, uint dwSize, AllocationType flAllocationType, MemoryProtection flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr HeapAlloc(IntPtr hHeap, uint dwFlags, int dwBytes);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetProcessHeap();

        [DllImport("kernel32.dll")]
        public static extern bool IsBadReadPtr(IntPtr lp, uint ucb);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32", ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, byte[] procName);

        [DllImport("kernel32", ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, IntPtr procName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)] /* unnecessary, isn't it? */
        public static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool VirtualFree(UIntPtr lpAddress, UIntPtr dwSize, uint dwFreeType);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool HeapFree(IntPtr hHeap, uint dwFlags, IntPtr lpMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool VirtualProtect(UIntPtr lpAddress, uint dwSize, uint flNewProtect, out int lpflOldProtect);
    }
}
