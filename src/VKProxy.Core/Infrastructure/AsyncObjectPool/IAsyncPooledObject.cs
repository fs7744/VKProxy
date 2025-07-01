namespace VKProxy.Core.Infrastructure.AsyncObjectPool;

public interface IAsyncPooledObject<T> : IDisposable, IAsyncDisposable where T : IDisposable
{
    public T Obj { get; }
}
