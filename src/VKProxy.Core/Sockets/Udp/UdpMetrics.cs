using System.Diagnostics.Metrics;

namespace VKProxy.Core.Sockets.Udp;

internal sealed class UdpMetrics
{
    public const string MeterName = "Microsoft.AspNetCore.Server.Kestrel.Udp";
    private readonly Meter _meter;
    private readonly Counter<long> _udpReceiveBytes;
    private readonly Counter<long> _clientUdpReceiveBytes;
    private readonly Counter<long> _clientUdpSentBytes;

    public UdpMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);
        _udpReceiveBytes = _meter.CreateCounter<long>("kestrel.udp.receive.bytes", unit: "By", "Total number of bytes that have been received by the UDP.");
        _clientUdpReceiveBytes = _meter.CreateCounter<long>("kestrel.client.udp.receive.bytes", unit: "By", "Total number of bytes that have been received by the UDP client.");
        _clientUdpSentBytes = _meter.CreateCounter<long>("kestrel.client.udp.sent.bytes", unit: "By", "Total number of bytes that have been sented by the UDP client.");
    }

    public void RecordUdpReceiveBytes(int bytes)
    {
        _udpReceiveBytes.Add(bytes);
    }

    public void RecordClientUdpReceiveBytes(int bytes)
    {
        _clientUdpReceiveBytes.Add(bytes);
    }

    public void RecordClientUdpSentBytes(int bytes)
    {
        _clientUdpSentBytes.Add(bytes);
    }
}