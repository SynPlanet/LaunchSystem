using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.Threading;

namespace Launcher
{
    class InstanceCollection
    {
        static InstanceCollection instance;

        public static InstanceCollection Instance
        {
            get
            {
                if (instance == null)
                    instance = new InstanceCollection();
                return instance;
            }
        }

        List<Item> items;

        private InstanceCollection()
        {
            items = new List<Item>();
            StartUpInitialized = false;
        }

        public void Add(string name, string instanceName, object instance, object instanceWrapper, IntPtr ptr, Type interfaceType, IInstanceItem parent)
        {
            if (GetInstanceItem(name, instanceName) == null)
                items.Add(new Item(name, instanceName, instance, instanceWrapper, ptr, interfaceType, (Item)parent));
        }

        public void Add(string name, string instanceName, object instance)
        {
            if (GetInstanceItem(name, instanceName) == null)
                items.Add(new Item(name, instanceName, instance));
        }

        public void Bind(IInstanceItem source, IInstanceItem dist)
        {
            ((Item)dist).AddChild((Item)source);
        }

        public IInstanceItem GetInstanceItem(string name, string instanceName)
        {
            return items.Find(x => x.Name == name && x.InstanceName == instanceName);
        }

        public void Release(IInstanceItem instanceItem, IWrapperInfo wrapper)
        {
            Item item = (Item)instanceItem;

            foreach (var parent in item.Parents) /*if (parent.Wrappers.Where(x => x.InterfaceType.FullName == wrapper.InterfaceType.FullName).Count() != 0)*/ ((IConnected)parent.Instance).UnloadedObject(wrapper.InterfaceType.FullName, item.Name, item.InstanceName);
            if (item.Release(wrapper))
            {
                item.RemoveChild();
                items.Remove(item);
                ((IConnected)item.Instance).Release();
                LogManager.Instance.Launcher.Information(LogLevel.Attention, string.Format(Local.Message.InstanceCollection_Message0_P2, item.Name, item.InstanceName));
            }
        }

        public void ReleaseAll()
        {
            while (items.Count != 0)
            {
                Item item = items[0];
                if (item.Wrappers.Length == 0)
                {
                    item.RemoveChild();
                    items.Remove(item);
                    ((IConnected)item.Instance).Release();
                    LogManager.Instance.Launcher.Information(LogLevel.Attention, string.Format(Local.Message.InstanceCollection_Message0_P2, item.Name, item.InstanceName));
                }
                else while (item.Wrappers.Length != 0) Release(item, item.Wrappers[0]);
            }
        }

        public int Count { get { return items.Count; } }

        public bool IsEmpty
        {
            get
            {
                return items.Count == 0;
            }
        }

        long _startUpInitialized;
        public bool StartUpInitialized
        {
            get
            {
                return Interlocked.Read(ref _startUpInitialized) == 1;
            }
            set
            {
                if (value)
                    Interlocked.Exchange(ref _startUpInitialized, 1);
                else Interlocked.Exchange(ref _startUpInitialized, 0);
            }
        }

        class Item : IInstanceItem
        {
            IWrapperInfo[] wrappers;

            public Item(string name, string instanceName, object instance, object instanceWrapper, IntPtr ptrWrapper, Type interfaceType, Item parent)
                : this(name, instanceName, instance)
            {
                Parents = Parents.Add(parent);
                parent.AddChild(this);
                AddWrapperInfo(instanceWrapper, ptrWrapper, interfaceType);
            }

            public Item(string name, string instanceName, object instance)
            {
                wrappers = new IWrapperInfo[0];
                Name = name;
                InstanceName = instanceName;
                Instance = instance;
                Child = new Item[0];
                Parents = new Item[0];
            }

            public void AddWrapperInfo(object instanceWrapper, IntPtr ptrWrapper, Type interfaceType)
            {
                wrappers = wrappers.Add(new WrapperInfo(instanceWrapper, ptrWrapper, interfaceType));
            }

            public void AddChild(Item item)
            {
                Child = Child.Add(item, true);
                item.Parents = item.Parents.Add(this, true);
            }

            private void RemoveChild(Item item)
            {
                Child = Child.Remove(item);
                item.Parents = item.Parents.Remove(this);
            }

            private void RemoveParent(Item item)
            {
                Parents = Parents.Remove(item);
                item.Child = item.Child.Remove(this);
            }

            public void RemoveChild()
            {
                while (Child.Length != 0) RemoveChild(Child[0]);
                while (Parents.Length != 0) RemoveParent(Parents[0]);
            }

            public bool Release(IWrapperInfo wrapperInfo)
            {
                if (wrapperInfo == null) return true;
                wrappers = wrappers.Remove(wrapperInfo);
                Marshal.Release(wrapperInfo.PtrWrapper);
                if (wrappers.Length == 0)
                    return true;
                return false;
            }

            public object Instance { get; private set; }

            public string InstanceName { get; private set; }

            public string Name { get; private set; }

            public IWrapperInfo[] Wrappers { get { return wrappers; } }

            public Item[] Parents { get; private set; }

            public Item[] Child { get; private set; }

            class WrapperInfo : IWrapperInfo
            {
                public WrapperInfo(object instanceWrapper, IntPtr ptrWrapper, Type interfaceType)
                {
                    InstanceWrapper = instanceWrapper;
                    PtrWrapper = ptrWrapper;
                    InterfaceType = interfaceType;
                }

                public IntPtr PtrWrapper { get; private set; }

                public object InstanceWrapper { get; private set; }

                public Type InterfaceType { get; private set; }
            }
        }
    }
}
