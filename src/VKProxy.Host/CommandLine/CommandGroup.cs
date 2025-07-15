namespace VKProxy.CommandLine;

public class CommandGroup : Command
{
    public Dictionary<string, Command> commands = new Dictionary<string, Command>(StringComparer.OrdinalIgnoreCase);

    public CommandGroup(string name, string desc) : base(name, desc)
    {
    }

    public override void Help()
    {
        Console.Write(Name);
        Console.Write("     ");
        Console.WriteLine(Desc);
        foreach (var arg in commands.Values.Distinct())
        {
            Console.Write("     ");
            Console.Write(arg.Name);
            Console.Write("     ");
            Console.WriteLine(arg.Desc);
        }
    }

    public override Func<Task> Parse(IEnumerator<string> value)
    {
        if (value.MoveNext() && commands.TryGetValue(value.Current, out var h))
        {
            try
            {
                return h.Parse(value);
            }
            catch (DoCommandException cmd)
            {
                if (cmd.Message.Equals("--help", StringComparison.OrdinalIgnoreCase))
                {
                    h.Help();
                    return null;
                }
                else
                    throw;
            }
            catch (Exception)
            {
                throw;
            }
        }

        throw new CommandParseException($"Not found {Name} command: {value.Current}!");
    }

    public void Add(Command cmd)
    {
        commands.Add(cmd.Name, cmd);
    }
}