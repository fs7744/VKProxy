using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using VKProxy.Core.Infrastructure;

namespace VKProxy.Core.Sockets;

public abstract partial class TransportConnection : ConnectionContext
{
    private IDictionary<object, object?>? _items;
    private string? _connectionId;

    // Will only have a value if the transport is created from a multiplexed transport.
    public IFeatureCollection? MultiplexedConnectionFeatures { get; protected set; }

    public TransportConnection()
    {
        FastReset();
    }

    public override EndPoint? LocalEndPoint { get; set; }
    public override EndPoint? RemoteEndPoint { get; set; }

    public override string ConnectionId
    {
        get => _connectionId ??= CorrelationIdGenerator.GetNextId();
        set => _connectionId = value;
    }

    public override IFeatureCollection Features => this;

    public virtual MemoryPool<byte> MemoryPool { get; } = default!;

    public override IDuplexPipe Transport { get; set; } = default!;

    public IDuplexPipe Application { get; set; } = default!;

    public override IDictionary<object, object?> Items
    {
        get
        {
            // Lazily allocate connection metadata
            return _items ?? (_items = new ConnectionItems());
        }
        set
        {
            _items = value;
        }
    }

    internal void ResetItems()
    {
        _items?.Clear();
    }

    public override CancellationToken ConnectionClosed { get; set; }

    // DO NOT remove this override to ConnectionContext.Abort. Doing so would cause
    // any TransportConnection that does not override Abort or calls base.Abort
    // to stack overflow when IConnectionLifetimeFeature.Abort() is called.
    // That said, all derived types should override this method should override
    // this implementation of Abort because canceling pending output reads is not
    // sufficient to abort the connection if there is backpressure.
    public override void Abort(ConnectionAbortedException abortReason)
    {
        Debug.Assert(Application != null);
        Application.Input.CancelPendingRead();
    }

    MemoryPool<byte> IMemoryPoolFeature.MemoryPool => MemoryPool;

    IDuplexPipe IConnectionTransportFeature.Transport
    {
        get => Transport;
        set => Transport = value;
    }

    IDictionary<object, object?> IConnectionItemsFeature.Items
    {
        get => Items;
        set => Items = value;
    }

    CancellationToken IConnectionLifetimeFeature.ConnectionClosed
    {
        get => ConnectionClosed;
        set => ConnectionClosed = value;
    }

    void IConnectionLifetimeFeature.Abort() => Abort(new ConnectionAbortedException("The connection was aborted by the application via IConnectionLifetimeFeature.Abort()."));
}