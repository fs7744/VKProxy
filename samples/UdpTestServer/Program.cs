using System.Net;

using System.Net.Sockets;
using System.Text;

static void StartListener()
{
    UdpClient listener = new UdpClient(11000);
    IPEndPoint groupEP = null;

    try
    {
        while (true)
        {
            Console.WriteLine("Waiting for broadcast");
            byte[] bytes = listener.Receive(ref groupEP);

            var data = Encoding.ASCII.GetString(bytes, 0, bytes.Length);
            Console.WriteLine($"Received broadcast from {groupEP} :");
            Console.WriteLine($" {data}");
            listener.Send(Encoding.ASCII.GetBytes(data.Reverse().ToArray()), groupEP);
        }
    }
    catch (SocketException e)
    {
        Console.WriteLine(e);
    }
    finally
    {
        listener.Close();
    }
}

static void Client(string[] args)
{
    var client = new UdpClient();

    IPAddress broadcast = IPAddress.Parse("127.0.0.1");
    byte[] sendbuf = Encoding.ASCII.GetBytes(args[1]);
    IPEndPoint ep = new IPEndPoint(broadcast, 5000);

    client.Send(sendbuf, ep);
    Console.WriteLine("Message sent to the broadcast address");

    sendbuf = client.Receive(ref ep);

    Console.WriteLine($"Receive Message : {Encoding.ASCII.GetString(sendbuf)}");
}

if (args.Length == 0)
{
    args = new string[] { "proxy", "test" };
}
if (args[0].Equals("server"))
{
    StartListener();
}
else if (args[0].Equals("client"))
{
    Client(args);
}