using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace S7Scanner.Lib.IpScanner;
public class IpScannerService
{
    private const int DefaultTimeoutMs = 1000;
    private const int MaxDegreeOfParallelism = 100;

    public async Task<IEnumerable<IPAddress>> ScanAsync(IEnumerable<IPAddress> ips, int port, CancellationToken cancellationToken)
    {
        var openIps = new ConcurrentBag<IPAddress>();
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = MaxDegreeOfParallelism,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(ips, parallelOptions, async (ip, token) =>
        {
            using var client = new TcpClient();
            try
            {
                // Use a CancellationTokenSource to implement a timeout
                using var cts = new CancellationTokenSource(DefaultTimeoutMs);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, cts.Token);

                await client.ConnectAsync(ip, port, linkedCts.Token);
                openIps.Add(ip);
                Console.WriteLine($"[FOUND] Host {ip} has port {port} open.");
            }
            catch (OperationCanceledException)
            {
                // This is expected for timeouts or cancellation
                Console.WriteLine($"[TIMEOUT] Host {ip} on port {port}.");
            }
            catch (SocketException)
            {
                // This is expected for closed or unreachable ports
            }
            catch (Exception ex)
            {
                // Log other unexpected errors
                Console.WriteLine($"[ERROR] scanning {ip}: {ex.Message}");
            }
        });

        return openIps.OrderBy(ip => ip, new IpAddressComparer()).ToList();
    }
}

// Helper class to sort IP Addresses correctly
file class IpAddressComparer : IComparer<IPAddress>
{
    public int Compare(IPAddress? x, IPAddress? y)
    {
        ArgumentNullException.ThrowIfNull(x, nameof(x));
        ArgumentNullException.ThrowIfNull(y, nameof(y));

        var xBytes = x.GetAddressBytes();
        var yBytes = y.GetAddressBytes();

        // Ensure same family, though the main logic should already do this
        if (xBytes.Length != yBytes.Length)
        {
            return xBytes.Length.CompareTo(yBytes.Length);
        }

        for (int i = 0; i < xBytes.Length; i++)
        {
            if (xBytes[i] != yBytes[i])
            {
                return xBytes[i].CompareTo(yBytes[i]);
            }
        }

        return 0;
    }
}
