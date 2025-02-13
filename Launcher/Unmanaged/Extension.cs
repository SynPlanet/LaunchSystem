using System;
using System.Runtime.InteropServices;

namespace Launcher.Unmanaged
{
    static class Extension
    {
        public static IntPtr ToIntPtr(this UIntPtr ptr)
        {
            return new IntPtr((long)ptr.ToUInt64());
        }

        public static IntPtr Offset(this IntPtr ptr, long offset)
        {
            return new IntPtr(ptr.ToInt64() + offset);
        }

        public static IntPtr Offset(this IntPtr ptr, int offset)
        {
            return new IntPtr(ptr.ToInt64() + offset);
        }

        public static T GetStruct<T>(this IntPtr ptr) where T : struct
        {
            return (T)Marshal.PtrToStructure(ptr, typeof(T));
        }

        public static void SetStruct<T>(this IntPtr ptr, T value) where T : struct
        {
            Marshal.StructureToPtr(value, ptr, true);
        }

        public static T GetStruct<T>(this UIntPtr ptr) where T : struct
        {
            IntPtr pointer;
            if (Environment.Is64BitProcess) pointer = new IntPtr((long)ptr.ToUInt64());
            else pointer = new IntPtr((int)ptr.ToUInt32());
            return pointer.GetStruct<T>();
        }

        public static void SetStruct<T>(this UIntPtr ptr, T value) where T : struct
        {
            IntPtr pointer;
            if (Environment.Is64BitProcess) pointer = new IntPtr((long)ptr.ToUInt64());
            else pointer = new IntPtr((int)ptr.ToUInt32());
            pointer.SetStruct<T>(value);
        }

        public static T GetStruct<T>(this byte[] data, int offset) where T : struct
        {
            int size = Marshal.SizeOf(typeof(T));
            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(data, offset, ptr, size);
                return (T)Marshal.PtrToStructure(ptr, typeof(T));
            }
            finally { Marshal.FreeHGlobal(ptr); }
        }
    }
}
