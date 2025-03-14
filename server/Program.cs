using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Program
{
    private static ConcurrentDictionary<int, PortForwarder> forwarders = new ConcurrentDictionary<int, PortForwarder>();

    static async Task Main(string[] args)
    {
        int controlPort = 9999; // Port to listen for control messages from the client

        var listener = new TcpListener(IPAddress.Any, controlPort);
        listener.Start();
        Console.WriteLine($"Listening for control messages on port {controlPort}...");

        while (true)
        {
            var client = await listener.AcceptTcpClientAsync();
            _ = HandleClientAsync(client);
        }
    }

    private static async Task HandleClientAsync(TcpClient client)
    {
        var stream = client.GetStream();
        var buffer = new byte[1024];
        var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
        var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        var parts = message.Split(' ');

        if (parts.Length == 3 && int.TryParse(parts[0], out int sourcePort) && int.TryParse(parts[2], out int destinationPort))
        {
            string destinationHost = parts[1];
            var forwarder = new PortForwarder(sourcePort, destinationHost, destinationPort);
            if (forwarders.TryAdd(destinationPort, forwarder))
            {
                _ = forwarder.StartAsync(); // Start forwarding without awaiting to keep the control connection alive
            }

            // Wait for the client to disconnect
            try
            {
                while (true)
                {
                    bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                }
            }
            finally
            {
                forwarders.TryRemove(destinationPort, out _);
                forwarder.Stop();
                Console.WriteLine($"Stopped forwarding for port {destinationPort}");
            }
        }
        else
        {
            Console.WriteLine("Invalid control message received.");
        }
    }
}
