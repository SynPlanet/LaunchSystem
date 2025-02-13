
namespace Launcher.Settings
{
    interface IUnmanagedCode
    {
        string Path { get; }

        bool CanDebug { get; }

        TypeCode Type { get; }

        string[] AvailableInterfaces { get; }
    }
}
