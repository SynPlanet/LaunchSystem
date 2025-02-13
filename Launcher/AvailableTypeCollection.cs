using System;
using System.Collections.Generic;
using System.Reflection;
using Launcher.Settings;

namespace Launcher
{
    class AvailableTypeCollection
    {
        #region Singleton
        static AvailableTypeCollection instance;
        public static AvailableTypeCollection Instance
        {
            get
            {
                if (instance == null)
                {
                    try { instance = new AvailableTypeCollection(); }
                    catch (Exception ex) { LogManager.Instance.Launcher.Exception(ex, "Ошибка загрузки типов"); }
                }
                return instance;
            }
        }
        #endregion

        List<Item> items;
        Random rnd = new Random();
        private AvailableTypeCollection()
        {
            LogManager.Instance.Launcher.Information(LogLevel.Report, Local.Message.AvailableTypeCollection_Message0);
            items = new List<Item>();
            items.AddRange(GetManagedItems());
            items.AddRange(GetComItems());
            LogManager.Instance.Launcher.Information(LogLevel.Report, Local.Message.AvailableTypeCollection_Message1);
        }

        public object GetConnected(string name, out string dllName)
        {
            dllName = null;
            Item item = items.Find(x => x.Name == name);
            if (item == null)
            {
                foreach (var code in Settings.Settings.Instance.Loader.UnmanagedCodeArray)
                {
                    if (GetAssemblyName(code.Path) == name)
                    {
                        dllName = name;
                        return (new CreatorDynamicLibrary()).GetInstance(code);
                    }
                }
                throw new Exception(string.Format(Local.Message.AvailableTypeCollection_Message2_P1, name));
            }
            dllName = GetAssemblyName(item.Type.Assembly.Location);
            return Activator.CreateInstance(item.Type);
        }

        private string GetAssemblyName(string path)
        {
            string name = path;
            int index = path.LastIndexOf('\\');
            if (index != -1)
                name = path.Remove(0, index + 1);
            return name;
        }

        private List<Item> GetComItems()
        {
            List<Item> items = new List<Item>();
            IComCode[] array = null;
            try { array = Settings.Settings.Instance.Loader.ComCodeArray; }
            catch (Exception ex) { throw new LauncherException(ex, Local.Message.AvailableTypeCollection_Message3); }
            foreach (var item in array)
            {
                try
                {
                    Guid guid = new Guid(item.Guid);
                    Type type = Type.GetTypeFromCLSID(guid);
                    LogManager.Instance.Launcher.Information(LogLevel.Report, string.Format(Local.Message.AvailableTypeCollection_Message4_P1, item.Guid));
                    if (type != null) items.Add(new Item(item.Guid, type));
                }
                catch (Exception ex) { LogManager.Instance.Launcher.Exception(ex); }
            }
            return items;
        }

        private List<Item> GetManagedItems()
        {
            List<Item> items = new List<Item>();
            IManagedCode[] array = null;
            try { array = Settings.Settings.Instance.Loader.ManagedCodeArray; }
            catch (Exception ex) { throw new LauncherException(ex, Local.Message.AvailableTypeCollection_Message3); }
            
            foreach (var item in array)
            {
                try
                {
                    Assembly assembly = null;
                    assembly = Assembly.LoadFrom(item.Path);
                    
                    LogManager.Instance.Launcher.Information(LogLevel.Report, string.Format(Local.Message.AvailableTypeCollection_Message4_P1, item.Path));
                    Type type = assembly.GetType(item.TypeName);
                    if (type != null) items.Add(new Item(item.TypeName, type));
                    else LogManager.Instance.Launcher.Exception(LogLevel.Basic, string.Format(Local.Message.AvailableTypeCollection_Message5_P2, item.Path, item.TypeName));
                }
                catch (Exception ex) { LogManager.Instance.Launcher.Exception(ex); }
                
            }
            return items;
        }

        class Item
        {
            public Item(string name, Type type)
            {
                Name = name;
                Type = type;
            }

            public Type Type { get; private set; }

            public string Name { get; private set; }
        }
    }
}
