using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Launcher
{
    interface IWrapperInfo
    {
        IntPtr PtrWrapper { get; }

        object InstanceWrapper { get; }

        Type InterfaceType { get; }
    }
}
