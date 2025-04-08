using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VKProxy.Core.Sockets.Udp;

public class UdpSocketTransportOptions
{
    public int UdpMaxSize { get; set; } = 4096;

    public int UdpPoolSize { get; set; } = 1024;
}