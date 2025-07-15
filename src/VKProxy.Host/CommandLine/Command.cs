namespace VKProxy.CommandLine;

public abstract class Command
{
    public Command(string name, string desc)
    {
        Name = name;
        Desc = desc;
    }

    public string Name { get; }
    public string Desc { get; protected set; }

    public abstract Func<Task> Parse(IEnumerator<string> value);

    public abstract void Help();
}