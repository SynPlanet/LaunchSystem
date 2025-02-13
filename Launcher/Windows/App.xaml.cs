using System;
using System.Windows;
using Launcher.Windows;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }

        private const string ShutDownArgsName = "ID";
       int _id = 0;

        void App_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length != 0)
            {
                for (int i = 0; i != e.Args.Length; ++i)
                {
                    var pak = e.Args[i].Split('=');
                    if (pak.Length < 2) continue;
                    if (pak[0] == ShutDownArgsName)
                        Int32.TryParse(pak[1], out _id);
                }
            }
            var mainWindow = new MainWindow(_id);
            //mainWindow.Show();
        }
    }
}
