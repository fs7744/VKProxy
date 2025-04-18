namespace VKProxy.Core.Infrastructure;

public interface IRandomFactory
{
    /// <summary>
    /// Create a instance of random class.
    /// </summary>
    Random CreateRandomInstance();
}

public sealed class NullRandomFactory : IRandomFactory
{
    public Random CreateRandomInstance()
    {
        throw new NotImplementedException();
    }
}

public sealed class RandomFactory : IRandomFactory
{
    /// <inheritdoc/>
    public Random CreateRandomInstance()
    {
        return Random.Shared;
    }
}