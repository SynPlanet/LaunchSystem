using System;
using System.IO;
using System.Text;

namespace Launcher
{
    static class Startup
    {
        public static void Initialize()
        {
            if (Settings.Settings.Instance == null) 
                return;
            try { InitializeEnvironmentVariables(); }
            catch (Exception ex) { throw new LauncherException(ex, Local.Message.Startup_Message0); }

            try { InstanceManager.Initialize(); }
            catch (Exception ex) { throw new LauncherException(ex, Local.Message.Startup_Message1); }
        }

        public static void GetGraphEnManager()
        {
            try { InstanceManager.InitializeGraphEnManager(); }
            catch (Exception ex) { throw new LauncherException(ex, Local.Message.Startup_Message2); }
        }

        public static void Stop()
        {
            InstanceManager.Release();
        }

        private static void InitializeEnvironmentVariables()
        {
            StringBuilder path = new StringBuilder();
            path.Append(Environment.GetEnvironmentVariable("Path"));
            string temp = string.Empty;
            foreach (var item in Settings.Settings.Instance.ApplicationPaths)
            {
                temp = Path.GetFullPath(item);
                path.Append(';').Append(temp);
                LogManager.Instance.Launcher.Information(string.Format("Path {0} appended to environment variable Path.", temp));
            }
            LogManager.Instance.Launcher.Information(string.Format("Environment variable Path: {0}", path));
            Environment.SetEnvironmentVariable("path", path.ToString());
        }
    }
}
