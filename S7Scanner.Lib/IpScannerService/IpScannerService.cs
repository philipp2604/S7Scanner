using S7Scanner.Lib.Helpers;
using S7Scanner.Lib.Models;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace S7Scanner.Lib.IpScannerService;

/// <summary>
/// Provides functionality to scan a range of IP addresses for Siemens devices and classify them as PLC or HMI.
/// </summary>
/// <remarks>
/// This service is designed to identify Siemens devices by scanning specified IP addresses and checking
/// for open ports. It uses the default Siemens S7 port (102) to detect potential devices and additional ports to
/// classify them as PLC or HMI. The scanning process supports parallel execution and can be canceled using a <see
/// cref="CancellationToken"/>.
/// </remarks>
public static class IpScannerService
{
    // The default Siemens S7 port.
    private const int _siemensS7Port = 102;

    // Ports that typically indicate an HMI device if open.
    private static readonly int[] _hmiPorts = [2308, 50523, 1033, 5001, 5002, 5800];

    /// <summary>
    /// Scans a range of IP addresses for Siemens devices and classifies them as PLC or HMI.
    /// </summary>
    /// <param name="ips">The collection of IP addresses to scan.</param>
    /// <param name="timeoutMs">The timeout in milliseconds for each connection attempt.</param>
    /// <param name="maxDegreeOfParallelism">The maximum number of concurrent scans.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A collection of <see cref="DiscoveredDevice"/> objects, sorted by IP address.</returns>
    public static async Task<IEnumerable<DiscoveredDevice>> DiscoverDevicesAsync(
        IEnumerable<IPAddress> ips,
        int timeoutMs,
        int maxDegreeOfParallelism,
        CancellationToken cancellationToken)
    {
        var discoveredDevices = new ConcurrentBag<DiscoveredDevice>();
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxDegreeOfParallelism,
            CancellationToken = cancellationToken
        };

        // Stage 1: Find all devices with the primary port (102) open.
        await Parallel.ForEachAsync(ips, parallelOptions, async (ip, token) =>
        {
            if (await IsPortOpenAsync(ip, _siemensS7Port, timeoutMs, token))
            {
                Console.WriteLine($"[CANDIDATE] Found device at {ip}. Checking device type...");

                // Stage 2: Classify the device as PLC or HMI.
                bool isHmi = false;
                foreach (var hmiPort in _hmiPorts)
                {
                    if (await IsPortOpenAsync(ip, hmiPort, timeoutMs, token))
                    {
                        isHmi = true;
                        Console.WriteLine($"[HMI DETECTED] Host {ip} has HMI port {hmiPort} open.");
                        break;
                    }
                }

                var deviceType = isHmi ? DeviceType.HMI : DeviceType.PLC;
                discoveredDevices.Add(new DiscoveredDevice(ip, deviceType));
            }
        });

        return [.. discoveredDevices.OrderBy(d => d.IpAddress, new IpAddressComparer())];
    }

    /// <summary>
    /// Checks if a specific TCP port is open on a given IP address.
    /// </summary>
    /// <param name="ip">The IP address to check.</param>
    /// <param name="port">The TCP port to check.</param>
    /// <param name="timeoutMs">The connection timeout in milliseconds.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>True if the port is open and a connection is established; otherwise, false.</returns>
    /// <exception cref="OperationCanceledException">Thrown if the operation is cancelled via the cancellation token.</exception>
    private static async Task<bool> IsPortOpenAsync(IPAddress ip, int port, int timeoutMs, CancellationToken cancellationToken)
    {
        using var client = new TcpClient();
        try
        {
            using var cts = new CancellationTokenSource(timeoutMs);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

            await client.ConnectAsync(ip, port, linkedCts.Token);
            return true;
        }
        catch (Exception)
        {
            // Any exception (SocketException, OperationCanceledException for timeouts) means the port is not accessible.
            return false;
        }
    }
}