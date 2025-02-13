using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Launcher.CommonMemory;
using TypeLib;
using System.Security.Cryptography;
using System.Text;
using SynPlanet;

namespace Launcher.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    internal partial class MainWindow : Window
    {
        DispatcherTimer timer;
        bool? isRunRealTime = null;
        bool isExit = false;

        public MainWindow(int closeEventId)
        {
            try 
            {
                //LoadingWindow.Instance.Open();
                Launcher.Settings.Settings.Instance.Initialize();
                LogManager.Instance.Launcher.Information(string.Format(Local.Message.MainWindow_Message4_P1, Environment.Is64BitProcess ? "x64" : "x86"));
                ASB3301();
                try
                {
                    timer = new DispatcherTimer();
                    timer.Interval = TimeSpan.FromMilliseconds(100);
                    timer.Tick += new EventHandler(timer_Tick);
                    Exception exeption = null;
                    if (CommonMemoryManager.Instance.Manager != null)
                    {
                        CommonMemoryManager.Instance.Manager.SetLog(LogManager.Instance.GetInterface("CommonMemory Launcher"));
                        Thread thread = new Thread(new ThreadStart(delegate() { try { CommonMemoryManager.Instance.Manager.Connected(); } catch (Exception ex) { exeption = ex; } }));
                        thread.Start();
                        thread.Join();
                    }
                    if (exeption != null)
                        throw exeption;
                    Initialized += Window_Initialized;
                   
                    InitializeComponent();
                }
                catch (Exception ex) { LogManager.Instance.Launcher.Exception(ex, Local.Message.MainWindow_Message0); Dispatcher.BeginInvoke(new Action(delegate() { Exit(this, null); })); }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, Local.Message.MainWindow_Message0, MessageBoxButton.OK, MessageBoxImage.Error); Dispatcher.BeginInvoke(new Action(delegate() { Exit(this, null); })); }
            //LoadingWindow.Instance.Destroy();
            this.Closed+=new EventHandler(MainWindow_Closed);
            this.Visibility = Visibility.Hidden;

            var eventName = EventWaitHandleName + Environment.MachineName + closeEventId;
            _closEventWaitHandle = OpenOrCreate(eventName, false, EventResetMode.ManualReset);
            LogManager.Instance.Launcher.Information("Close event created. Event name:" + eventName);
            _closeEventTask = Task.Factory.StartNew(new Action(delegate()
            {
                if (_closEventWaitHandle == null) return;
                _closEventWaitHandle.WaitOne();
                if (isExit) return;
                Dispatcher.BeginInvoke(new Action(delegate() { isExit = false; 
                    LogManager.Instance.Launcher.Information("Close event cought. Stop Launcher.");
                    Exit(this, null); }));
            }));
        }

        const string EventWaitHandleName = "Close_";
        Task _closeEventTask;
        EventWaitHandle _closEventWaitHandle;

        EventWaitHandle OpenOrCreate(string name, bool initialState, EventResetMode mode)
        {
            EventWaitHandle ewh = null;
            try
            {
                ewh = EventWaitHandle.OpenExisting(name);
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                //Handle does not exist, create it.
                ewh = new EventWaitHandle(initialState, mode, name);
            }

            return ewh;
        }

        #region License struff 

        ASPManager aSPManager;

        void ASB3301()
        {
            aSPManager = new ASPManager();
            aSPManager.CheckLicense((int)SynPlanet.ProductTypes.LAUNCHER, out string info);
            if (aSPManager.IsValid) aSPManager.OnFailed += ASPManager_OnFailed;
            else OnLicenseFailed();
        }

        void ASPManager_OnFailed(object sender, EventArgs e)
        {
            OnLicenseFailed();
        }

        void OnLicenseFailed()
        {
            LogManager.Instance.Launcher.Information(string.Format(Local.Message.LoaderMessage0_P1, "Launcher.exe"));
            Process.GetCurrentProcess().Kill();
        }

        #endregion

        void MainWindow_Closed(object sender, EventArgs e)
        {
            Exit(this, null);
        }

        void timer_Tick(object sender, EventArgs e)
        {
            if (isRunRealTime != null && isRunRealTime.Value && InstanceCollection.Instance.GetInstanceItem("RealTime.Dispatcher", "RealTime") == null)
                Exit(sender, null);
            else if (InstanceCollection.Instance.IsEmpty)
                Exit(sender, null);
        }

        private void Open(object sender, RoutedEventArgs e)
        {
            IInstanceItem item = InstanceCollection.Instance.GetInstanceItem("RealTime.Dispatcher", "RealTime");
            if (item != null)
            {
                IWrapperInfo wrapperInfo = GetWrapperInfo(item, "TypeLib.IDispatcherWindow");
                if (wrapperInfo != null)
                {
                    ((IDispatcherWindow)wrapperInfo.InstanceWrapper).Open();
                }
            }
        }

        private void Exit(object sender, RoutedEventArgs e)
        {
            if (isExit) return;
            isExit = true;
            //Thread thread = new Thread(new ThreadStart(Terminate));
            //thread.IsBackground = true;
            //thread.Start();
            if (timer != null) timer.Stop();
            try
            {
                try { Startup.Stop(); }
                catch (Exception ex) { LogManager.Instance.Launcher.Exception(ex, Local.Message.MainWindow_Message1); }
                try { if (CommonMemoryManager.Instance.Manager != null) CommonMemoryManager.Instance.Manager.Disconnected(); }
                catch (Exception ex) { LogManager.Instance.Launcher.Exception(ex, Local.Message.MainWindow_Message1); }
                try { CommonMemoryManager.Instance.Dispose(); }
                catch (Exception ex) { LogManager.Instance.Launcher.Exception(ex, Local.Message.MainWindow_Message1); }
                LogManager.Instance.Launcher.Information(Local.Message.MainWindow_Message3);
                Thread.Sleep(2000);
                LogManager.Instance.Terminate();
                Launcher.Settings.Settings.Instance.Terminate();
            }
            catch { }
            tray.Dispose();
            if (_closEventWaitHandle != null)
            {
                _closEventWaitHandle.Set();
                _closEventWaitHandle.Close();
            }
            this.Close();
            App.Current.Shutdown(0);
        }
        /*
        private void Terminate()
        {
            Thread.Sleep(5000);
            for (bool IsAttached = true; IsAttached; CheckRemoteDebuggerPresent(Process.GetCurrentProcess().Handle, ref IsAttached)) Thread.Sleep(50);
            LogManager.Instance.Launcher.Information(Local.Message.MainWindow_Message2);
            Process.GetCurrentProcess().Kill();
        }
        */
       

        private void Window_Initialized(object sender, EventArgs e)
        {
            try
            {
                /* set center of screen (for child windows if needed)*/
                double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
                double screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
                double windowWidth = this.Width;
                double windowHeight = this.Height;
                this.Left = (screenWidth / 2) - (windowWidth / 2);
                this.Top = (screenHeight / 2) - (windowHeight / 2);

                /* start  */
                Startup.GetGraphEnManager();
                GetGraphEnSettings();
                InstanceCollection.Instance.StartUpInitialized = false;
                Startup.Initialize();
                InstanceCollection.Instance.StartUpInitialized = true;
                GetRealTime();
                timer.Start();
            }
            catch (Exception ex) { LogManager.Instance.Launcher.Exception(LogLevel.Basic, ex); Dispatcher.BeginInvoke(new Action(delegate() { Close(); })); }
        }

        void GetRealTime()
        {
            IInstanceItem item = InstanceCollection.Instance.GetInstanceItem("RealTime.Dispatcher", "RealTime");
            if (item != null)
            {
                var realtime = item.Instance as IDispatcherWindow;
                if (realtime.IsStartupOpen)
                    realtime.Open();
                isRunRealTime = true;
                IWrapperInfo wrapperInfo = GetWrapperInfo(item, "TypeLib.IDispatcherWindow");
                if (wrapperInfo == null)
                {
                    IWrapper wrapper = WrapperBuilder.Instance.GetWrapper("TypeLib.IDispatcherWindow");
                    wrapper.Add(item.Instance);
                    Type interfaceType = InterfaceCollection.Instance.GetInterface("TypeLib.IDispatcherWindow");
                    IntPtr ptrWrapper = Marshal.GetComInterfaceForObject(wrapper.Instance, interfaceType);
                    item.AddWrapperInfo(wrapper.Instance, ptrWrapper, interfaceType);
                }
            }
        }

        void GetGraphEnSettings()
        {
            IInstanceItem item = InstanceCollection.Instance.GetInstanceItem("GraphEn.Manager", "GraphEnManager");
            if (item != null)
            {
                IWrapperInfo wrapperInfo = GetWrapperInfo(item, "TypeLib.IGraphEnManager");
                if (wrapperInfo == null)
                {
                    IWrapper wrapper = WrapperBuilder.Instance.GetWrapper("TypeLib.GraphEn.IGraphEnManager");
                    if (wrapper == null) return;
                    wrapper.Add(item.Instance);
                    Type interfaceType = InterfaceCollection.Instance.GetInterface("TypeLib.GraphEn.IGraphEnManager");
                    IntPtr ptrWrapper = Marshal.GetComInterfaceForObject(wrapper.Instance, interfaceType);
                    item.AddWrapperInfo(wrapper.Instance, ptrWrapper, interfaceType);
                }
            }
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

        private void ContextMenu_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                IInstanceItem item = InstanceCollection.Instance.GetInstanceItem("RealTime.Dispatcher", "RealTime");
                ContextMenu contextMenu = Resources["TrayMenu"] as ContextMenu;
                foreach (object obj in contextMenu.Items)
                {
                    MenuItem menu = obj as MenuItem;
                    if (menu == null) continue;
                    if (menu.Name == "realTime")
                    {
                       if (item == null)
                            menu.IsEnabled = false;
                        else
                            menu.IsEnabled = true;
                    }
                }
            }
        }

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool CheckRemoteDebuggerPresent(IntPtr hProcess, ref bool isDebuggerPresent);
    }
}
