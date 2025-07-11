﻿namespace VKProxy.CommandLine;

public abstract class ArgsCommand<T> : Command where T : new()
{
    private readonly string desc;

    protected ArgsCommand(string name, string desc) : base(name)
    {
        this.desc = desc;
        AddArg(new CommandArg("help", "h", null, "show all options", s => throw new DoCommandException("--help"), false));
    }

    public T Args { get; } = new T();

    public Dictionary<string, CommandArg> commandArgs = new Dictionary<string, CommandArg>(StringComparer.OrdinalIgnoreCase);

    public override void Help()
    {
        Console.Write(Name);
        Console.Write("     ");
        Console.WriteLine(desc);
        foreach (var arg in commandArgs.Values.Distinct())
        {
            Console.Write("     ");
            Console.WriteLine($"{arg.Name}{(arg.ShortName == null && arg.Env == null ? string.Empty : $" ({arg.ShortName}{(arg.Env != null && arg.ShortName != null ? "," : "")}{(arg.Env == null ? "" : $"Environment:{arg.Env}")})")}");
            Console.Write("         ");
            Console.WriteLine(arg.Desc);
        }
    }

    public override Func<Task> Parse(IEnumerator<string> value)
    {
        foreach (var arg in commandArgs.Values.Where(i => i.Env != null))
        {
            var v = Environment.GetEnvironmentVariable(arg.Env);
            if (!string.IsNullOrEmpty(v))
            {
                try
                {
                    arg.Action(v);
                }
                catch (Exception ex)
                {
                    throw new CommandParseException($"Command:{Name}, args: {arg.Env}, value: {v}, {ex.Message}");
                }
            }
        }

        while (value.MoveNext())
        {
            if (commandArgs.TryGetValue(value.Current, out var h))
            {
                var v = h.HasArg && value.MoveNext() ? value.Current : null;

                if (v != null && v.StartsWith("-"))
                {
                    throw new CommandParseException($"Command:{Name}, args: {h.Name}, not found value");
                }
                try
                {
                    h.Action(v);
                }
                catch (DoCommandException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new CommandParseException($"Command:{Name}, args: {h.Name}, value: {v}, {ex.Message}");
                }
            }
            else
            {
                throw new CommandParseException($"Command:{Name}, Not found args: {value.Current}!");
            }
        }
        return Do();
    }

    public void AddArg(CommandArg arg)
    {
        commandArgs.Add(arg.Name, arg);
        if (arg.ShortName != null)
            commandArgs.Add(arg.ShortName, arg);
    }

    public abstract Func<Task> Do();
}