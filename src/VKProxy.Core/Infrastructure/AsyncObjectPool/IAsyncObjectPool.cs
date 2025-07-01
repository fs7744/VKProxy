namespace VKProxy.Core.Infrastructure.AsyncObjectPool;

public interface IAsyncObjectPool<T> : IDisposable, IAsyncDisposable where T : IDisposable
{
    T Rent();

    Task<T> RentAsync();

    void Return(T obj);

    ValueTask ReturnAsync(T obj);
}