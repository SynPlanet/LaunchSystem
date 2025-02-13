using System;

namespace Launcher
{
    interface IInstanceItem
    {
        void AddWrapperInfo(object instanceWrapper, IntPtr ptrWrapper, Type interfaceType);

        object Instance { get; }

        string InstanceName { get; }

        string Name { get; }

        IWrapperInfo[] Wrappers { get; }
    }


}
