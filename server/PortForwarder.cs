using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

public class PortForwarder
{
    private readonly int _sourcePort;
    private readonly string _destinationHost;
    private readonly int _destinationPort;
    private TcpListener _listener;
    private bool _isRunning;

    public PortForwarder(int sourcePort, string destinationHost, int destinationPort)
    {
        _sourcePort = sourcePort;
        _destinationHost = destinationHost;
        _destinationPort = destinationPort;
    }

    public async Task StartAsync()
    {
        _listener = new TcpListener(IPAddress.Any, _destinationPort);
        _listener.Start();
        _isRunning = true;
        Console.WriteLine($"Listening on port {_destinationPort}...");

        while (_isRunning)
        {
            var client = await _listener.AcceptTcpClientAsync();
            _ = HandleClientAsync(client);
        }
    }

    public void Stop()
    {
        _isRunning = false;
        _listener.Stop();
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
