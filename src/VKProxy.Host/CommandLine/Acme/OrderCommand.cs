namespace VKProxy.CommandLine;

internal class OrderCommand : CommandGroup
{
    public OrderCommand() : base("order", "Manage ACME orders.")
    {
        Add(new ListOrderCommand());
    }
}