namespace VKProxy.CommandLine;

public class FuncCommand : Command
{
    private readonly Func<Task> func;
    private readonly string help;

    public FuncCommand(string name, Func<Task> func, string help = null) : base(name)
    {
        this.func = func;
        this.help = help;
    }

    public override void Help()
    {
        if (!string.IsNullOrEmpty(help))
            Console.WriteLine(help);
    }

    public override Func<Task> Parse(IEnumerator<string> value)
    {
        return func;
    }
}
