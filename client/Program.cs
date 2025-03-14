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
            try
            {
                while (true)
                {
                    await Task.Delay(1000);
                }
            }
            finally
            {
                // Properly close the connection
                client.Close();
                Console.WriteLine("Client disconnected.");
            }
        }
    }
}
