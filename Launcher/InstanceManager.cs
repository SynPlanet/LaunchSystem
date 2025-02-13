using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Launcher.CommonMemory;

namespace Launcher
{
    class InstanceManager : MarshalByRefObject, IInstanceManager
    {
        List<RequestInterface> requestInterfaces;
        string name;
        string instanceName;
        string dllName;
        string parentName;

        public InstanceManager(RequestInterface[] requestInterfaces, string name, string instanceName, string dllName, string parentName)
        {
            this._isStartUp = false;
            this.name = name;
            this.parentName = parentName;
            this.instanceName = instanceName;
            this.dllName = dllName;
            this.requestInterfaces = new List<RequestInterface>(requestInterfaces);
            LogManager.Instance.Launcher.Information(LogLevel.Attention, string.Format(Local.Message.InstanceManager_Message0_P3, dllName, name, instanceName));
            if (requestInterfaces.Length == 0) LogManager.Instance.Launcher.Information(LogLevel.Detail, Local.Message.InstanceManager_Message1);
            else LogManager.Instance.Launcher.Information(LogLevel.Detail, Local.Message.InstanceManager_Message2);
            foreach (var item in requestInterfaces)
                LogManager.Instance.Launcher.Information(LogLevel.Detail, string.Format(Local.Message.InstanceManager_Message3_P3, item.Name, item.InterfaceName, item.CanCreate.ToString()));
        }

        public static void Initialize()
        {
            LogManager.Instance.Launcher.Information(LogLevel.Report, Local.Message.InstanceManager_Message4);
            try { foreach (var item in Settings.Settings.Instance.EntryPoints)Initialize(item); }
            catch (Exception ex) { LogManager.Instance.Launcher.Exception(ex); }
            LogManager.Instance.Launcher.Information(LogLevel.Report, Local.Message.InstanceManager_Message5);
        }

        public static void InitializeGraphEnManager()
        {
            LogManager.Instance.Launcher.Information(LogLevel.Report, Local.Message.InstanceManager_Message4);
            try { foreach (var item in Settings.Settings.Instance.EntryPoints) {
                    if (item.Name != "GraphEn.Manager") continue;
                    Initialize(item);
                    break;
                }
            }
            catch (Exception ex) { LogManager.Instance.Launcher.Exception(ex); }
            LogManager.Instance.Launcher.Information(LogLevel.Report, Local.Message.InstanceManager_Message5);
        }

        private static void Initialize(Settings.IStartup entryPoint)
        {
            if (entryPoint.Initialized) return;
            string dllName;
            object instance = null;
            InstanceManager manager = null;
            if (InstanceCollection.Instance.GetInstanceItem(entryPoint.Name, entryPoint.InstanceName) != null) return;
            try { instance = AvailableTypeCollection.Instance.GetConnected(entryPoint.Name, out dllName); LogManager.Instance.Launcher.Information(LogLevel.Detail, string.Format(Local.Message.InstanceManager_Message6_P3, dllName, entryPoint.Name, entryPoint.InstanceName)); }
            catch (Exception ex) { throw new LauncherException(ex, Local.Message.InstanceManager_Message7); }
            try 
            {
				manager = new InstanceManager(GetRequestInterfaces((IConnected)instance), entryPoint.Name, entryPoint.InstanceName, dllName, string.Empty)
				{
					_isStartUp = true
				};
			}
            catch (Exception ex) { throw new LauncherException(ex, string.Format(Local.Message.InstanceManager_Message8_P3, dllName, entryPoint.Name, entryPoint.InstanceName)); }
            InstanceCollection.Instance.Add(entryPoint.Name, entryPoint.InstanceName, instance);
            
            try { ((IConnected)instance).Initialize(Marshal.GetComInterfaceForObject(manager, typeof(IInstanceManager)), LogManager.Instance.GetPtr(dllName), CommonMemoryManager.Instance.Ptr);
                entryPoint.Initialized = true;
            }
            catch (Exception ex) { throw new LauncherException(ex, string.Format(Local.Message.InstanceManager_Message9_P3, dllName, entryPoint.Name, entryPoint.InstanceName)); }
        }

        public static void Release()
        {
            LogManager.Instance.Launcher.Information(LogLevel.Attention, Local.Message.InstanceManager_Message10);
            InstanceCollection.Instance.ReleaseAll();
        }

        private static IntPtr GetInstance(IInstanceItem item, string interfaceName, string name, string instanceName, string nameParent, string instanceNameParent)
        {
            IWrapper wrapper;
            Type interfaceType;
            IntPtr ptrWrapper = IntPtr.Zero;
            if (item != null)
            {
                try
                {
                    IWrapperInfo wrapperInfo = GetWrapperInfo(item, interfaceName);
                    InstanceCollection.Instance.Bind(InstanceCollection.Instance.GetInstanceItem(name, instanceName), InstanceCollection.Instance.GetInstanceItem(nameParent, instanceNameParent));
                    if (wrapperInfo != null)
                        return wrapperInfo.PtrWrapper;
                    wrapper = WrapperBuilder.Instance.GetWrapper(interfaceName);
                    wrapper.Add(item.Instance);
                    interfaceType = InterfaceCollection.Instance.GetInterface(interfaceName);
                    ptrWrapper = Marshal.GetComInterfaceForObject(wrapper.Instance, interfaceType);
                    item.AddWrapperInfo(wrapper.Instance, ptrWrapper, interfaceType);
                }
                catch (Exception ex) { LogManager.Instance.Launcher.Exception(ex); }
                return ptrWrapper;
            }
            string dllName;
            try
            {
                object instance = AvailableTypeCollection.Instance.GetConnected(name, out dllName);
                if (instance == null)
                    return IntPtr.Zero;
                InstanceManager manager = null;
                try { manager = new InstanceManager(GetRequestInterfaces((IConnected)instance), name, instanceName, dllName, instanceNameParent); }
                catch (Exception ex) { { throw new LauncherException(ex, string.Format(Local.Message.InstanceManager_Message8_P3, dllName, name, instanceName)); } }
                wrapper = WrapperBuilder.Instance.GetWrapper(interfaceName);
                if (wrapper == null)
                    return IntPtr.Zero;
                wrapper.Add(instance);
                interfaceType = InterfaceCollection.Instance.GetInterface(interfaceName);
                if (interfaceType == null)
                    return IntPtr.Zero;
                ptrWrapper = Marshal.GetComInterfaceForObject(wrapper.Instance, interfaceType);
                InstanceCollection.Instance.Add(name, instanceName, instance, wrapper.Instance, ptrWrapper, interfaceType, InstanceCollection.Instance.GetInstanceItem(nameParent, instanceNameParent));
                ((IConnected)instance).Initialize(Marshal.GetComInterfaceForObject(manager, typeof(IInstanceManager)), LogManager.Instance.GetPtr(dllName), CommonMemoryManager.Instance.Ptr);
            }
            catch (Exception ex) { LogManager.Instance.Launcher.Exception(ex); }
            return ptrWrapper;
        }

        public IntPtr GetInstance(string interfaceName, string name, string instanceName)
        {
            LogManager.Instance.Launcher.Information(LogLevel.Detail, string.Format(Local.Message.InstanceManager_Message11_P4, dllName, interfaceName, name, instanceName));
            IntPtr ptr = IntPtr.Zero;
            RequestInterface requestInterface = requestInterfaces.Find(x => x.InterfaceName == interfaceName && x.Name == name);
            IInstanceItem item = InstanceCollection.Instance.GetInstanceItem(requestInterface.Name, instanceName);
            if (item == null && requestInterface.CanCreate == false) LogManager.Instance.Launcher.Information(LogLevel.Detail, string.Format(Local.Message.InstanceManager_Message12_P4, dllName, interfaceName, name, instanceName));
            else ptr = GetInstance(item, interfaceName, name, instanceName, this.name, this.instanceName);
            try { return ptr; }
            finally
            {
                if (ptr == IntPtr.Zero) LogManager.Instance.Launcher.Exception(LogLevel.Detail, string.Format(Local.Message.InstanceManager_Message13_P4, dllName, interfaceName, name, instanceName));
                else LogManager.Instance.Launcher.Information(LogLevel.Detail, string.Format(Local.Message.InstanceManager_Message14_P4, dllName, interfaceName, name, instanceName));
            }
        }

        public bool IsExistInstance(string interfaceName, string name, string instanceName)
        {
            RequestInterface requestInterface = requestInterfaces.Find(x => x.InterfaceName == interfaceName && x.Name == name);
            return InstanceCollection.Instance.GetInstanceItem(requestInterface.Name, instanceName) != null;
        }

        public bool IsExistInterface(string interfaceName, string name)
        {
            foreach (var item in requestInterfaces)
            {
                if (item.InterfaceName == interfaceName && item.Name == name)
                    return true;
            }
            return false;
        }

        public void Release(string interfaceName, string name, string instanceName)
        {
            RequestInterface requestInterface = requestInterfaces.Find(x => x.InterfaceName == interfaceName && x.Name == name);
            IInstanceItem item = InstanceCollection.Instance.GetInstanceItem(requestInterface.Name, instanceName);
            if (item != null)
            {
                try
                {
                    IWrapperInfo wrapper = GetWrapperInfo(item, interfaceName);
                    if (wrapper != null)
                    {
                        InstanceCollection.Instance.Release(item, wrapper);
                        LogManager.Instance.Launcher.Information(LogLevel.Attention, string.Format(Local.Message.InstanceManager_Message15_P7, dllName, this.name, this.instanceName, Environment.NewLine, interfaceName, name, instanceName));
                    }
                    else throw new LauncherException(Local.Message.InstanceManager_Message16);
                }
                catch (Exception ex) { LogManager.Instance.Launcher.Exception(LogLevel.Basic, ex, string.Format(Local.Message.InstanceManager_Message17_P7, dllName, this.name, this.instanceName, Environment.NewLine, interfaceName, name, instanceName)); }
            }
            else LogManager.Instance.Launcher.Exception(LogLevel.Detail, string.Format(Local.Message.InstanceManager_Message18_P7, dllName, this.name, this.instanceName, Environment.NewLine, interfaceName, name, instanceName));
        }

        private static IWrapperInfo GetWrapperInfo(IInstanceItem item, string interfaceName)
        {
            IWrapperInfo[] wrappers = item.Wrappers;
            foreach (var wrapper in wrappers)
            {
                if (wrapper.InterfaceType.FullName == interfaceName)
                {
                    return wrapper;
                }
            }
            return null;
        }

        private static RequestInterface[] GetRequestInterfaces(IConnected connect)
        {
            int count = 0;
            try { count = connect.CountRequestInterfaces; }
            catch (Exception ex) { throw new LauncherException(ex, Local.Message.InstanceManager_Message19); }
            int sizeStruct = Marshal.SizeOf(typeof(RequestInterface));
            int size = count * sizeStruct;
            IntPtr ptr = Marshal.AllocHGlobal(size);
            try { connect.RequestInterfaces(ptr); }
            catch (Exception ex) { Marshal.FreeHGlobal(ptr); throw new LauncherException(ex, Local.Message.InstanceManager_Message20); }
            byte[] buffer = new byte[size];
            Marshal.Copy(ptr, buffer, 0, buffer.Length);
            Marshal.FreeHGlobal(ptr);
            RequestInterface[] array = new RequestInterface[count];
            ptr = Marshal.AllocHGlobal(sizeStruct);
            try
            {
                for (int i = 0; i < array.Length; i++)
                {
                    Marshal.Copy(buffer, i * sizeStruct, ptr, sizeStruct);
                    array[i] = (RequestInterface)Marshal.PtrToStructure(ptr, typeof(RequestInterface));
                }
            }
            catch  { throw ; }
            finally { Marshal.FreeHGlobal(ptr); }
            return array;
        }

        public void ReleaseItself()
        {
            IInstanceItem item = InstanceCollection.Instance.GetInstanceItem(name, instanceName);
            try
            {
                if (item != null)
                {
                    IWrapperInfo[] wrappers = item.Wrappers;
                    if (wrappers.Length != 0) foreach (var wrapper in wrappers) InstanceCollection.Instance.Release(item, wrapper);
                    else InstanceCollection.Instance.Release(item, null);
                    LogManager.Instance.Launcher.Information(LogLevel.Attention, string.Format(Local.Message.InstanceManager_Message21_P3, dllName, this.name, this.instanceName));
                }
                else throw new LauncherException(Local.Message.InstanceManager_Message22);
            }
            catch (Exception ex) { LogManager.Instance.Launcher.Exception(LogLevel.Detail, ex, string.Format(Local.Message.InstanceManager_Message23_P3, dllName, this.name, this.instanceName)); }
        }

        public string CurrentInstanceName { get { return instanceName; } }

        public string ParentInstanceName { get { return parentName; } }

        public bool StartUpInitialized { get { return InstanceCollection.Instance.StartUpInitialized; } }

        internal bool _isStartUp;
        public bool IsStartUp { get { return _isStartUp; } }

        public void Invoke(IntPtr methodPtr, IntPtr paramPtr)
        {
            return;
        }

        public void BeginInvoke(IntPtr methodPtr, IntPtr paramPtr)
        {
            return;
        }
    }
}
