namespace VKProxy.CommandLine;

public abstract class Command
{
    public Command(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public abstract Func<Task> Parse(IEnumerator<string> value);

    public abstract void Help();
}