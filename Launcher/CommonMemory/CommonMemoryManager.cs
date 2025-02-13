using System;
using System.Runtime.InteropServices;
//using Launcher.RealTime;

namespace Launcher.CommonMemory
{
    class CommonMemoryManager : IDisposable
    {
        CommonMemory commonMemory;
        static CommonMemoryManager instance = null;

        public static CommonMemoryManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new CommonMemoryManager();
                return instance;
            }
        }

        private CommonMemoryManager()
        {
            try
            {
                commonMemory = new CommonMemory();
            }
            catch (Exception ex) { LogManager.Instance.Launcher.Exception(ex); }
            //commonMemory = new CommonMemory();
            Manager = commonMemory as ICommonMemoryManager;
            if (Manager == null) Ptr = IntPtr.Zero;
            else Ptr = Marshal.GetComInterfaceForObject(commonMemory, typeof(ICommonMemoryFullControl));
        }

        public ICommonMemoryManager Manager { get; private set; }

        public IntPtr Ptr { get; private set; }

        public void Dispose()
        {
            commonMemory = null;
            Manager = null;
            if (Ptr != IntPtr.Zero) Marshal.Release(Ptr);
            Ptr = IntPtr.Zero;
            GC.Collect();
        }
    }
}
