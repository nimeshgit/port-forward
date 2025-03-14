### Explanation

![Port Forwarding](/server/doc/port_forwarding.png)

There are many inspirations from linux kernel components / architecture and its ssh forwarding features from my clildhood :)


I was doing one consulting project last year and I wanted such feature with oauth, 
Here is the prototype I had developped with two console applications i.e. server and client that can do port forwarding.
However this does not include oauth but this stack was proto-type and very useful to jump-start port-forwarding feature over tcp ip 
for .net core #cross-platform application(s). 

The server listens for control messages from the client on port 9999. When a control message is received, it starts a new `PortForwarder` instance to handle the port forwarding. The `PortForwarder` listens on the specified destination port and forwards traffic to the client's source port.

The client connects to the server on port 9999 and sends a control message containing the source port, destination host, and destination port. The client keeps the connection alive to ensure the server continues forwarding traffic.

[Reference](https://ops-ml-architect.blogspot.com/)
[Image SSH Tunneling Credits](https://www.ssh.com/academy/ssh/tunneling)


### [Program.cs](file:///home/vadgama/Drive/GitHub/rnd/port-forwarding/server/Program.cs)

Update the server to handle port forwarding requests and start a new local TCP listener for each request.

````csharp
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Program
{
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
        using (client)
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
                _ = forwarder.StartAsync(); // Start forwarding without awaiting to keep the control connection alive
            }
            else
            {
                Console.WriteLine("Invalid control message received.");
            }
        }
    }
}
````

### [PortForwarder.cs](file:///home/vadgama/Drive/GitHub/rnd/port-forwarding/server/PortForwarder.cs)

Ensure the port forwarding service handles multiple clients and keeps the connection alive.

````csharp


Made changes.

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

public class PortForwarder
{
    private readonly int _sourcePort;
    private readonly string _destinationHost;
    private readonly int _destinationPort;

    public PortForwarder(int sourcePort, string destinationHost, int destinationPort)
    {
        _sourcePort = sourcePort;
        _destinationHost = destinationHost;
        _destinationPort = destinationPort;
    }

    public async Task StartAsync()
    {
        var listener = new TcpListener(IPAddress.Any, _destinationPort);
        listener.Start();
        Console.WriteLine($"Listening on port {_destinationPort}...");

        while (true)
        {
            var client = await listener.AcceptTcpClientAsync();
            _ = HandleClientAsync(client);
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        using (client)
        using (var destinationClient = new TcpClient())
        {
            await destinationClient.ConnectAsync(_destinationHost, _sourcePort);
            var clientStream = client.GetStream();
            var destinationStream = destinationClient.GetStream();

            var clientToDestination = clientStream.CopyToAsync(destinationStream);
            var destinationToClient = destinationStream.CopyToAsync(clientStream);

            await Task.WhenAny(clientToDestination, destinationToClient);
        }
    }
}
````

### [Program.cs](file:///home/vadgama/Drive/GitHub/rnd/port-forwarding/client/Program.cs)

Ensure the client keeps the connection alive and allows the server to handle port forwarding.

````csharp


Made changes.

using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length != 3)
        {
            Console.WriteLine("Usage: Client <sourcePort> <destinationHost> <destinationPort>");
            return;
        }

        int sourcePort = int.Parse(args[0]);
        string destinationHost = args[1];
        int destinationPort = int.Parse(args[2]);

        string host = destinationHost;
        int controlPort = 9999;

        using (var client = new TcpClient())
        {
            await client.ConnectAsync(host, controlPort);
            Console.WriteLine($"Connected to {host}:{controlPort}");

            var stream = client.GetStream();
            var message = $"{sourcePort} {destinationHost} {destinationPort}";
            var data = Encoding.UTF8.GetBytes(message);

            await stream.WriteAsync(data, 0, data.Length);
            Console.WriteLine($"Sent: {message}");

            // Keep the connection alive
            while (true)
            {
                await Task.Delay(1000);
            }
        }
    }
}
````
