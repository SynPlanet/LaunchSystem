
namespace Launcher.Settings
{
    interface IManagedCode
    {
        string TypeName { get; }

        string Path { get; }

        TypeCode Type { get; }

        string[] AvailableInterfaces { get; }
    }
}
