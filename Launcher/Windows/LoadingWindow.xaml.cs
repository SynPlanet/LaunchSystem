using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Threading;

namespace Launcher.Windows
{
    /// <summary>
    /// Interaction logic for LoadingWindow.xaml
    /// </summary>
    internal partial class LoadingWindow : Window, ILoadingWindow
    {
        static ILoadingWindow instance;

        public static ILoadingWindow Instance
        {
            get
            {
                if (instance == null)
                    instance = new LoadingWindow();
                return instance;
            }
        }

        private LoadingWindow()
        {
            InitializeComponent();
        }

        public void Open()
        {
            Show();
        }

        public void Destroy()
        {
            Close();
        }
    }
}
