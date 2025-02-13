using System;
using System.Xml;

namespace Launcher.Settings
{
    class Settings
    {
        static Settings instance = null;

        public static Settings Instance
        {
            get
            {
                if (instance == null)
                {
                    try
                    {
                        instance = new Settings();
                    }
                    catch (Exception ex) { throw new LauncherException(ex, string.Format(Local.Message.Settings_Message0_P1, FileName)); }
                }
                return instance;
            }
        }

        const string FileName = @"Settings.conf";

        #region Tags
        const string ConfigurationTag = "Configuration";
        const string StartupTag = "Startup";
        const string ApplicationPathsTag = "ApplicationPaths";
        const string PathTag = "Path";
        const string LoadTag = "Load";
        const string ManagedTag = "Managed";
        const string UnmanagedTag = "Unmanaged";
        const string ComTag = "Com";
        const string InterfacesTag = "Interfaces";
        const string InterfaceTag = "Interface";
        #endregion

        #region Attributes
        const string InstanceNameAttribute = "InstanceName";
        const string PathAttribute = "Path";
        const string ClassAttribute = "Class";
        const string GuidAttribute = "Guid";
        const string LogLevelAttribute = "LogLevel";
        const string CanDebugAttribute = "CanDebug";
        #endregion

        private Settings()
        {

        }

        public void Initialize()
        {
            EntryPoints = new IStartup[0];
            ApplicationPaths = new string[0];
            XmlTextReader xml = new XmlTextReader(FileName);
            xml.WhitespaceHandling = WhitespaceHandling.None;
            try
            {
                if (!FindConfiguration(xml)) throw new SettingsException(string.Format(Local.Message.Settings_Message1_P1, FileName));
                for (string tag = GetConfigurationTag(xml, 1); tag != null; tag = GetConfigurationTag(xml, 1))
                {
                    if (tag == StartupTag && xml.IsEmptyElement == false) ReadStartUp(xml);
                    else if (tag == ApplicationPathsTag && xml.IsEmptyElement == false) ReadApplicationPaths(xml);
                    else if (tag == LoadTag && xml.IsEmptyElement == false) ReadLoad(xml);
                }
            }
            catch { throw; }
            finally { xml.Close(); }
        }

        public void Terminate()
        {
            EntryPoints = new IStartup[0];
            ApplicationPaths = new string[0];
        }

        private void ReadLoad(XmlTextReader xml)
        {
            Loader = new LoaderClass();
            for (string tag = GetConfigurationTag(xml, 2); tag != null; tag = GetConfigurationTag(xml, 2))
            {
                if (tag == ManagedTag) ReadManaged(xml);
                else if (tag == UnmanagedTag) ReadUnmanaged(xml);
                else if (tag == ComTag) ReadCom(xml);
            }
        }

        private void ReadCom(XmlTextReader xml)
        {
            string guid = xml.GetAttribute(GuidAttribute);
            if (guid == null) guid = string.Empty;
            if (guid.Trim(' ', '\t') == string.Empty) throw new SettingsException(string.Format(Local.Message.Settings_Message2_P1, xml.LineNumber.ToString()));
            var assembly = new ComCode(guid);
            ReadInterfaces(xml, assembly);
            ((LoaderClass)Loader).AddAssembly(assembly);
        }

        private void ReadUnmanaged(XmlTextReader xml)
        {
            string path = xml.GetAttribute(PathAttribute);
            string canDebugStr = xml.GetAttribute(CanDebugAttribute);
            if (path == null) path = string.Empty;
            if (path.Trim(' ', '\t') == string.Empty) throw new SettingsException(string.Format(Local.Message.Settings_Message3_P1, xml.LineNumber.ToString()));
            var assembly = new UnmanagedCode(path.Trim(' ', '\t'));
            if (canDebugStr == null) assembly.CanDebug = false;
            else
            {
                bool canAtached;
                if (!bool.TryParse(canDebugStr, out canAtached)) canAtached = false;
                assembly.CanDebug = canAtached;
            }
            ReadInterfaces(xml, assembly);
            ((LoaderClass)Loader).AddAssembly(assembly);
        }

        private void ReadManaged(XmlTextReader xml)
        {
            string path = xml.GetAttribute(PathAttribute);
            string className = xml.GetAttribute(ClassAttribute);
            if (path == null) path = string.Empty;
            if (className == null) className = string.Empty;
            if (path.Trim(' ', '\t') == string.Empty) throw new SettingsException(string.Format(Local.Message.Settings_Message4_P1, xml.LineNumber.ToString()));
            if (className.Trim(' ', '\t') == string.Empty) throw new SettingsException(string.Format(Local.Message.Settings_Message5_P1, xml.LineNumber.ToString()));
            var assembly = new ManagedCode(path.Trim(' ', '\t'), className.Trim(' ', '\t'));
            ReadInterfaces(xml, assembly);
            ((LoaderClass)Loader).AddAssembly(assembly);
        }

        private void ReadInterfaces(XmlTextReader xml, IAssembly assembly)
        {
            while (xml.Read() && xml.Depth >= 3)
            {
                if (xml.NodeType == XmlNodeType.Element && xml.Name == InterfacesTag)
                {
                    while (xml.Read() && xml.Depth >= 4)
                    {
                        if (xml.NodeType == XmlNodeType.Element && xml.Name == InterfaceTag && xml.Read() && xml.NodeType == XmlNodeType.Text)
                            assembly.AddAvailableInterface(xml.Value.Trim(' ', '\t'));
                    }
                }
            }
        }

        private void ReadApplicationPaths(XmlTextReader xml)
        {
            while (xml.Read() && xml.Depth >= 2)
            {
                if (xml.NodeType == XmlNodeType.Element && xml.Name == PathTag && xml.Read() && xml.Depth == 3 && xml.NodeType == XmlNodeType.Text)
                    ApplicationPaths = ApplicationPaths.Add(xml.Value.Trim(' ', '\t'));
            }
        }

        private void ReadStartUp(XmlTextReader xml)
        {
            string instanceName = xml.GetAttribute(InstanceNameAttribute);
            string moduleName = null;
            if (xml.Read() && xml.Depth == 2 && xml.NodeType == XmlNodeType.Text) moduleName = xml.Value;
            if (moduleName == null || moduleName.Length == 0) throw new SettingsException(string.Format(Local.Message.Settings_Message6_P1, xml.LineNumber.ToString()));
            if (instanceName == null || instanceName.Length == 0) throw new SettingsException(string.Format(Local.Message.Settings_Message7_P1, xml.LineNumber.ToString()));
            EntryPoints = EntryPoints.Add(new StartUp(moduleName, instanceName));
        }

        private string GetConfigurationTag(XmlTextReader xml, int depth)
        {
            string value = null;
            while (xml.Read() && xml.Depth >= depth)
            {
                if (xml.Depth == depth && xml.NodeType == XmlNodeType.Element && xml.Name == StartupTag) return StartupTag;
                else if (xml.Depth == depth && xml.NodeType == XmlNodeType.Element && xml.Name == ApplicationPathsTag) return ApplicationPathsTag;
                else if (xml.Depth == depth && xml.NodeType == XmlNodeType.Element && xml.Name == LoadTag) return LoadTag;
                else if (xml.Depth == depth && xml.NodeType == XmlNodeType.Element && xml.Name == ManagedTag) return ManagedTag;
                else if (xml.Depth == depth && xml.NodeType == XmlNodeType.Element && xml.Name == UnmanagedTag) return UnmanagedTag;
                else if (xml.Depth == depth && xml.NodeType == XmlNodeType.Element && xml.Name == ComTag) return ComTag;
            }
            return value;
        }

        private bool FindConfiguration(XmlTextReader xml)
        {
            while (xml.Read())
                if (xml.Depth == 0 && xml.NodeType == XmlNodeType.Element && xml.Name == ConfigurationTag)
                {
                    LogLevel = LogLevel.Basic;
                    string level = xml.GetAttribute(LogLevelAttribute);
                    if (level != null)
                    {
                        switch (level.ToUpper())
                        {
                            case "BASIC": LogLevel = LogLevel.Basic;
                                break;
                            case "REPORT": LogLevel = LogLevel.Report;
                                break;
                            case "ATTENTION": LogLevel = LogLevel.Attention;
                                break;
                            case "DETAIL": LogLevel = LogLevel.Detail;
                                break;
                        }
                    }
                    return true; 
                }
            return false;
        }

        public LogLevel LogLevel { get; private set; }

        public IStartup[] EntryPoints { get; private set; }

        public ILoader Loader { get; private set; }

        public string[] ApplicationPaths { get; private set; }

        class StartUp : IStartup
        {
            public StartUp(string name, string instanceName)
            {
                Name = name;
                InstanceName = instanceName;
                Initialized = false;
            }

            public string Name { get; private set; }

            public string InstanceName { get; private set; }

            public bool Initialized { get; set; }
        }
       
        #region LoaderClass
        class LoaderClass : ILoader
        {
            public LoaderClass()
            {
                ManagedCodeArray = new ManagedCode[0];
                UnmanagedCodeArray = new UnmanagedCode[0];
                ComCodeArray = new ComCode[0];
            }

            public void AddAssembly(IAssembly assembly)
            {
                switch (assembly.Type)
                {
                    case TypeCode.Managed:
                        ManagedCodeArray = ManagedCodeArray.Add((ManagedCode)assembly);
                        break;
                    case TypeCode.Unmanaged:
                        UnmanagedCodeArray = UnmanagedCodeArray.Add((UnmanagedCode)assembly);
                        break;
                    case TypeCode.Com:
                        ComCodeArray = ComCodeArray.Add((ComCode)assembly);
                        break;
                }
            }

            public IManagedCode[] ManagedCodeArray { get; private set; }

            public IUnmanagedCode[] UnmanagedCodeArray { get; private set; }

            public IComCode[] ComCodeArray { get; private set; }
        }
        #endregion

        #region ComCode
        class ComCode : IAssembly, IComCode
        {
            public ComCode(string guid)
            {
                Guid = guid;
                Type = TypeCode.Com;
                AvailableInterfaces = new string[0];
            }

            public void AddAvailableInterface(string name)
            {
                AvailableInterfaces = AvailableInterfaces.Add(name);
            }

            public string Guid { get; private set; }

            public TypeCode Type { get; private set; }

            public string[] AvailableInterfaces { get; private set; }
            
        }
        #endregion

        #region UnmanagedCode
        class UnmanagedCode : MarshalByRefObject, IAssembly, IUnmanagedCode
        {
            public UnmanagedCode(string path)
            {
                Path = path;
                Type = TypeCode.Unmanaged;
                AvailableInterfaces = new string[0];
            }

            public void AddAvailableInterface(string name)
            {
                AvailableInterfaces = AvailableInterfaces.Add(name);
            }

            public string Path { get; private set; }

            public TypeCode Type { get; private set; }

            public string[] AvailableInterfaces { get; private set; }

            public bool CanDebug { get; set; }
        }
        #endregion

        #region ManagedCode
        class ManagedCode : IAssembly, IManagedCode
        {
            public ManagedCode(string path, string typeName)
            {
                Path = path;
                TypeName = typeName;
                AvailableInterfaces = new string[0];
            }

            public void AddAvailableInterface(string name)
            {
                AvailableInterfaces = AvailableInterfaces.Add(name);
            }

            public string TypeName { get; private set; }

            public string Path { get; private set; }

            public TypeCode Type { get; private set; }

            public string[] AvailableInterfaces { get; private set; }
        }
        #endregion

        #region IAssembly
        interface IAssembly
        {
            TypeCode Type { get; }
            string[] AvailableInterfaces { get; }
            void AddAvailableInterface(string name);
        }
        #endregion
    }
}
