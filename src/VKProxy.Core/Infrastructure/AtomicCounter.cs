namespace VKProxy.Core.Infrastructure;

public sealed class AtomicCounter
{
    private int _value;

    public int Value
    {
        get => Volatile.Read(ref _value);
        set => Volatile.Write(ref _value, value);
    }

    public int Increment()
    {
        return Interlocked.Increment(ref _value);
    }

    public int Decrement()
    {
        return Interlocked.Decrement(ref _value);
    }

    public void Reset()
    {
        Interlocked.Exchange(ref _value, 0);
    }
}