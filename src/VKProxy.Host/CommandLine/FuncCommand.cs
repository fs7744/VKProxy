namespace VKProxy.CommandLine;

public class FuncCommand : Command
{
    private readonly Func<Task> func;

    public FuncCommand(string name, string desc, Func<Task> func) : base(name, !string.IsNullOrEmpty(desc) ? $"{name}    {desc}" : null)
    {
        this.func = func;
    }

    public override void Help()
    {
        if (!string.IsNullOrEmpty(Desc))
            Console.WriteLine(Desc);
    }

    public override Func<Task> Parse(IEnumerator<string> value)
    {
        return func;
    }
}