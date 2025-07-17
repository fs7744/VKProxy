namespace VKProxy.CommandLine;

public class CommandArg
{
    public CommandArg(string name, string shortName, string env, string desc, Action<string> action, bool hasArg = true, Func<bool> check = null)
    {
        Name = $"--{name}";
        if (shortName != null)
            ShortName = $"-{shortName}";
        Env = env;
        Desc = desc;
        Action = action;
        HasArg = hasArg;
        Check = check;
    }

    public string Name { get; }
    public string ShortName { get; }
    public string Env { get; }
    public string Desc { get; }
    public Action<string> Action { get; }
    public Func<bool> Check { get; }
    public bool HasArg { get; }
}