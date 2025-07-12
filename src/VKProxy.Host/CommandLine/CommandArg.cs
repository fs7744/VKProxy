namespace VKProxy.CommandLine;

public class CommandArg
{
    public CommandArg(string name, string shortName, string env, string desc, Action<string> action, bool hasArg = true)
    {
        Name = $"--{name}";
        if (shortName != null)
            ShortName = $"-{shortName}";
        Env = env;
        Desc = desc;
        Action = action;
        HasArg = hasArg;
    }

    public string Name { get; }
    public string ShortName { get; }
    public string Env { get; }
    public string Desc { get; }
    public Action<string> Action { get; }
    public Action Check { get; }
    public bool HasArg { get; }
}