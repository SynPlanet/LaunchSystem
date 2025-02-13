
namespace Launcher.Settings
{
    interface ILoader
    {
        IManagedCode[] ManagedCodeArray { get; }

        IUnmanagedCode[] UnmanagedCodeArray { get; }

        IComCode[] ComCodeArray { get; }
    }
}
