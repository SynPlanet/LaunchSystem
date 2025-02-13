
namespace Launcher.Settings
{
    interface IComCode
    {
        string Guid { get; }

        TypeCode Type { get; }

        string[] AvailableInterfaces { get; }
    }
}
