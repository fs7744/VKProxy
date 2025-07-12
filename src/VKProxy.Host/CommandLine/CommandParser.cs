namespace VKProxy.CommandLine;

public class CommandParser : IDisposable
{
    private readonly Dictionary<string, Command> commands = new Dictionary<string, Command>(StringComparer.OrdinalIgnoreCase);

    public CommandParser()
    {
        commands["--help"] = new FuncCommand("--help", () =>
        {
            foreach (var item in commands.Values.Distinct())
            {
                item.Help();
            }
            return Task.CompletedTask;
        }, "--help (-h)     show all options, View more at https://fs7744.github.io/VKProxy.Doc/docs/introduction.html");
        commands["-h"] = commands["--help"];
    }

    public void Add(Command command)
    {
        commands[command.Name] = command;
    }

    public void Dispose()
    {
        commands.Clear();
    }

    public Func<Task> Parse(string[] args)
    {
        try
        {
            if (args == null || args.Length == 0) args = new string[] { "--help" };
            var e = (args as IEnumerable<string>).GetEnumerator();

            if (e.MoveNext())
            {
                if (!commands.TryGetValue(e.Current, out var cmd))
                {
                    throw new CommandParseException($"Not found command {e.Current}!");
                }

                return cmd.Parse(e);
            }
        }
        catch (CommandParseException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
        }
        catch (DoCommandException ex)
        {
            if (commands.TryGetValue(ex.Message, out var cmd))
            {
                return cmd.Parse(null);
            }
        }

        return null;
    }
}