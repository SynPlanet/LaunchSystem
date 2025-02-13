using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reflection;
using System.IO;


namespace Launcher
{
    class Local
    {
        private static readonly Local instance = new Local();

        public static Local Message { get { return instance; } }

        private Local()
        {
            Initialize(Launcher.Properties.Resources.DefaultLocal);
            //Initialize(Load(string.Format(@"Local\Launcher\{0}.txt", Thread.CurrentThread.CurrentCulture.Name)));
        }

        private string Load(string file)
        {
            try
            {
                using (StreamReader reader = new System.IO.StreamReader(file, Encoding.UTF8)) return reader.ReadToEnd();
            }
            catch { return string.Empty; }
        }

        private void Initialize(string resourceMessages)
        {
            var properties = this.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(x => x.PropertyType == typeof(string) && x.CanRead && x.CanWrite).ToArray();
            var messages = GetMessages(resourceMessages);
            foreach (var item in messages.Join(properties, x => x.Key, y => y.Name, (x, y) => new { Property = y, Message = x.Value }))
            {
                var parts = item.Property.Name.Split(new string[] { "_P" }, StringSplitOptions.RemoveEmptyEntries);
                int value;
                if (int.TryParse(parts[parts.Length - 1], out value))
                {
                    try { string.Format(item.Message, new object[value]); }
                    catch { continue; }
                }
                item.Property.SetValue(this, item.Message.Replace(@"\t", "\t").Replace(@"\n", "\n").Replace(@"\r", "\r"), null);
            }
        }

        private Dictionary<string, string> GetMessages(string resourceMessages)
        {
            Dictionary<string, string> messages = new Dictionary<string, string>();
            foreach (var item in resourceMessages.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length != 0))
            {
                int spaceIndex = item.IndexOf(' ');
                int tabIndex = item.IndexOf('\t');
                if (spaceIndex == -1 && tabIndex == -1) continue;
                if (spaceIndex == -1) spaceIndex = int.MaxValue;
                if (tabIndex == -1) tabIndex = int.MaxValue;
                int index;
                if (spaceIndex < tabIndex) index = spaceIndex;
                else index = tabIndex;
                try { messages.Add(item.Substring(0, index), item.Substring(index).TrimStart()); }
                catch (ArgumentException) { }
            }
            return messages;
        }

        public string Settings_Message0_P1 { get; private set; }
        public string Settings_Message1_P1 { get; private set; }
        public string Settings_Message2_P1 { get; private set; }
        public string Settings_Message3_P1 { get; private set; }
        public string Settings_Message4_P1 { get; private set; }
        public string Settings_Message5_P1 { get; private set; }
        public string Settings_Message6_P1 { get; private set; }
        public string Settings_Message7_P1 { get; private set; }

        public string MainWindow_Message0 { get; private set; }
        public string MainWindow_Message1 { get; private set; }
        public string MainWindow_Message2 { get; private set; }
        public string MainWindow_Message3 { get; private set; }
        public string MainWindow_Message4_P1 { get; private set; }

        public string AvailableTypeCollection_Message0 { get; private set; }
        public string AvailableTypeCollection_Message1 { get; private set; }
        public string AvailableTypeCollection_Message2_P1 { get; private set; }
        public string AvailableTypeCollection_Message3 { get; private set; }
        public string AvailableTypeCollection_Message4_P1 { get; private set; }
        public string AvailableTypeCollection_Message5_P2 { get; private set; }

        public string CreatorDynamicLibrary_Message0_P1 { get; private set; }
        public string CreatorDynamicLibrary_Message1_P1 { get; private set; }
        

        public string InstanceCollection_Message0_P2 { get; private set; }

        public string InstanceManager_Message0_P3 { get; private set; }
        public string InstanceManager_Message1 { get; private set; }
        public string InstanceManager_Message2 { get; private set; }
        public string InstanceManager_Message3_P3 { get; private set; }
        public string InstanceManager_Message4 { get; private set; }
        public string InstanceManager_Message5 { get; private set; }
        public string InstanceManager_Message6_P3 { get; private set; }
        public string InstanceManager_Message7 { get; private set; }
        public string InstanceManager_Message8_P3 { get; private set; }
        public string InstanceManager_Message9_P3 { get; private set; }
        public string InstanceManager_Message10 { get; private set; }
        public string InstanceManager_Message11_P4 { get; private set; }
        public string InstanceManager_Message12_P4 { get; private set; }
        public string InstanceManager_Message13_P4 { get; private set; }
        public string InstanceManager_Message14_P4 { get; private set; }
        public string InstanceManager_Message15_P7 { get; private set; }
        public string InstanceManager_Message16 { get; private set; }
        public string InstanceManager_Message17_P7 { get; private set; }
        public string InstanceManager_Message18_P7 { get; private set; }
        public string InstanceManager_Message19 { get; private set; }
        public string InstanceManager_Message20 { get; private set; }
        public string InstanceManager_Message21_P3 { get; private set; }
        public string InstanceManager_Message22 { get; private set; }
        public string InstanceManager_Message23_P3 { get; private set; }

        public string Startup_Message0 { get; private set; }
        public string Startup_Message1 { get; private set; }
        public string Startup_Message2 { get; private set; }

        public string WrapperBuilder_Message0_P1 { get; private set; }
        public string WrapperBuilder_Message1_P1 { get; private set; }
        public string WrapperBuilder_Message2 { get; private set; }

        public string LoaderMessage0_P1 { get; private set; }
        public string LoaderMessage0_P2 { get; private set; }

        public string InterfaceCollection_Message0_P1 { get; private set; }
        public string InterfaceCollection_Message1_P2 { get; private set; }
        public string InterfaceCollection_Message2_P1 { get; private set; }
        public string InterfaceCollection_Message3_P2 { get; private set; }
        public string InterfaceCollection_Message4_P1 { get; private set; }
        public string InterfaceCollection_Message5_P2 { get; private set; }
        public string InterfaceCollection_Message6_P1 { get; private set; }
    }
}
