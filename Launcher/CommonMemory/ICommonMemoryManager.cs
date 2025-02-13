using System.Runtime.InteropServices;

namespace Launcher.CommonMemory
{
    [ComVisible(true)]
    public interface ICommonMemoryManager
    {
        void Connected();
        void Disconnected();
        void Load();
        void Save();
        void SetLog(ILog log);
    }
}
