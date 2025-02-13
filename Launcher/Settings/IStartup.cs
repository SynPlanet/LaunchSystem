using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Launcher.Settings
{
    interface IStartup
    {
        string Name { get; }
        string InstanceName { get; }
        bool Initialized { get; set; }
    }
}
