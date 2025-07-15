using System.Reflection;

namespace VKProxy.CommandLine;

internal class VersionCommand : FuncCommand
{
    public VersionCommand() : base("--version", "VKProxy version", () =>
    {
        Console.WriteLine($"VKProxy {Assembly.GetExecutingAssembly().GetName().Version.ToString()}  .NET {Environment.Version.ToString()}");
        return Task.CompletedTask;
    })
    {
    }
}