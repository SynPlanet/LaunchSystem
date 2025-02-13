using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Launcher
{
    class TypeItem
    {
        public TypeItem(Type type)
        {
            Type = type;
            Name = type.FullName;
        }

        public IntPtr CreateInstance(Type interfaceType)
        {
            object instance = Activator.CreateInstance(Type);
            IntPtr ptr = Marshal.GetComInterfaceForObject(instance, interfaceType);
            return ptr;
        }

        public Type Type { get; private set; }

        public string Name { get; private set; }
    }
}
