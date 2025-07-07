using S7Scanner.Lib.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace S7Scanner.Lib.IpScanner;

/// <summary>
/// Provides functionality to scan a range of IP addresses for Siemens devices and classify them as PLC or HMI.
/// </summary>
/// <remarks>This service is designed to identify Siemens devices by scanning specified IP addresses and checking
/// for open ports. It uses the default Siemens S7 port (102) to detect potential devices and additional ports to
/// classify them as PLC or HMI. The scanning process supports parallel execution and can be canceled using a <see
/// cref="CancellationToken"/>.</remarks>
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
    /// <returns>A collection of DiscoveredDevice objects.</returns>
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
            // Pass the timeout to the check method
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
            return false;
        }
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