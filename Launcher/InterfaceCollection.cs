using System;
using System.Collections.Generic;
using System.Reflection;
using Launcher.Settings;

namespace Launcher
{
    class InterfaceCollection
    {
        #region Singleton
        
        static InterfaceCollection instance = null;

        public static InterfaceCollection Instance
        {
            get
            {
                if (instance == null)
                    instance = new InterfaceCollection();
                return instance;
            }
        }
        #endregion

        Type[] interfaces;
        const string BaseNameSpace = "TypeLib";

        private InterfaceCollection()
        {
            List<Type> interfaceCollection = new List<Type>();
            List<Type> allTypes = new List<Type>();
            Assembly assembly = typeof(IConnected).Assembly;
            allTypes.AddRange(assembly.GetTypes());
            Add(NameInterfaces(Settings.Settings.Instance.Loader.ComCodeArray, allTypes), interfaceCollection, allTypes);
            Add(NameInterfaces(Settings.Settings.Instance.Loader.ManagedCodeArray, allTypes), interfaceCollection, allTypes);
            Add(NameInterfaces(Settings.Settings.Instance.Loader.UnmanagedCodeArray, allTypes), interfaceCollection, allTypes);
            interfaces = interfaceCollection.ToArray();
        }

        private List<string> NameInterfaces(IComCode[] comCode, List<Type> allTypes)
        {
            List<string> interfaces = new List<string>();
            foreach (var item in comCode)
            {
                var array = GetInterfaces(item.AvailableInterfaces, allTypes);
                if (array.Count == 0) LogManager.Instance.Launcher.Information(LogLevel.Report, string.Format(Local.Message.InterfaceCollection_Message6_P1, item.Guid));
                else LogManager.Instance.Launcher.Information(LogLevel.Report, string.Format(Local.Message.InterfaceCollection_Message5_P2, item.Guid, array.Count));
                foreach (var itemInterface in array) LogManager.Instance.Launcher.Information(LogLevel.Report, string.Format(Local.Message.InterfaceCollection_Message0_P1, itemInterface));
                interfaces.AddRange(array);
            }
            return interfaces;
        }

        private List<string> NameInterfaces(IManagedCode[] managedCode, List<Type> allTypes)
        {
            List<string> interfaces = new List<string>();
            foreach (var item in managedCode)
            {
                var array = GetInterfaces(item.AvailableInterfaces, allTypes);
                if (array.Count == 0) LogManager.Instance.Launcher.Information(LogLevel.Report, string.Format(Local.Message.InterfaceCollection_Message2_P1, item.Path));
                else LogManager.Instance.Launcher.Information(LogLevel.Report, string.Format(Local.Message.InterfaceCollection_Message1_P2, item.Path, array.Count));
                foreach (var itemInterface in array) LogManager.Instance.Launcher.Information(LogLevel.Report, string.Format(Local.Message.InterfaceCollection_Message0_P1, itemInterface));
                interfaces.AddRange(array);
            }
            return interfaces;
        }

        private List<string> NameInterfaces(IUnmanagedCode[] unmanagedCode, List<Type> allTypes)
        {
            List<string> interfaces = new List<string>();
            foreach (var item in unmanagedCode)
            {
                var array = GetInterfaces(item.AvailableInterfaces, allTypes);
                if (array.Count == 0) LogManager.Instance.Launcher.Information(LogLevel.Report, string.Format(Local.Message.InterfaceCollection_Message4_P1, item.Path));
                else LogManager.Instance.Launcher.Information(LogLevel.Report, string.Format(Local.Message.InterfaceCollection_Message3_P2, item.Path, array.Count));
                foreach (var itemInterface in array) LogManager.Instance.Launcher.Information(LogLevel.Report, string.Format(Local.Message.InterfaceCollection_Message0_P1, itemInterface));
                interfaces.AddRange(array);
            }
            return interfaces;
        }

        private List<string> GetInterfaces(string[] names, List<Type> allTypes)
        {
            List<string> array = new List<string>();
            foreach (var name in names)
            {
                if (allTypes.Find(x => x.FullName == name) == null) continue;
                array.Add(name);
            }
            return array;
        }

        private void Add(List<string> names, List<Type> interfaceCollection, List<Type> allTypes)
        {
            foreach (var name in names)
            {
                Type type = allTypes.Find(x => x.FullName == name);
                if (type == null)
                    continue;
                if (interfaceCollection.Find(x => x.FullName == name) != null)
                    continue;
                if (type.Namespace.IndexOf(BaseNameSpace) == 0)
                    interfaceCollection.Add(type);
            }
        }

        public Type GetInterface(string name)
        {
            foreach (var item in interfaces)
            {
                if (item.FullName == name)
                    return item;
            }
            return null;
        }

        public Type[] GetInterfaces()
        {
            return interfaces;
        }
    }
}
