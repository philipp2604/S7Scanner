using S7Scanner.Lib.Helpers;
using S7Scanner.Lib.IpScannerService;
using S7Scanner.Lib.Models;

namespace S7Scanner.IntegrationTests.IpScanner;

/// <summary>
/// Provides integration tests for the <see cref="IpScannerService"/> to verify its ability to discover and classify
/// devices on a real network.
/// </summary>
/// <remarks>These tests are designed to run against a physical network environment with specific devices online
/// and configured. Ensure the network setup matches the expected configuration before running the tests.</remarks>
public class IpScannerService_IntegrationTests
{
    private const int _realNetworkTimeoutMs = 1500;
    private const int _realNetworkParallelism = 5;

    /// <summary>
    /// This test verifies the scanner's ability to correctly identify and classify
    /// a known set of devices on the physical network.
    /// It requires the following devices to be online and configured:
    /// - 192.168.0.2: PLC
    /// - 192.168.0.3: HMI
    /// - 192.168.0.4: HMI
    /// - 192.168.0.5: PLC
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task DiscoverDevicesAsync_OnRealNetwork_ShouldFindAndClassifyKnownDevices()
    {
        // ARRANGE
        const string ipRange = "192.168.0.1-192.168.0.5";
        var ipsToScan = IpRangeParser.Parse(ipRange);
        var expectedDevices = new Dictionary<string, DeviceType>
        {
            { "192.168.0.2", DeviceType.PLC },
            { "192.168.0.3", DeviceType.HMI },
            { "192.168.0.4", DeviceType.HMI },
            { "192.168.0.5", DeviceType.PLC }
        };

        // ACT
        Console.WriteLine($"Starting REAL network integration test for range: {ipRange}");
        Console.WriteLine("Ensure devices .2(PLC), .3(HMI), .4(HMI), .5(PLC) are online...");
        var discoveredDevices = (await IpScannerService.DiscoverDevicesAsync(
            ipsToScan, _realNetworkTimeoutMs, _realNetworkParallelism, CancellationToken.None)).ToList();
        var resultsMap = discoveredDevices.ToDictionary(d => d.IpAddress.ToString(), d => d.Type);
        Console.WriteLine($"Scan complete. Found {resultsMap.Count} devices.");

        // ASSERT
        Assert.Equal(expectedDevices.Count, resultsMap.Count);

        foreach (var expectedDevice in expectedDevices)
        {
            string expectedIp = expectedDevice.Key;
            DeviceType expectedType = expectedDevice.Value;

            Assert.True(resultsMap.ContainsKey(expectedIp), $"Expected device at {expectedIp} was not found.");

            Assert.Equal(expectedType, resultsMap[expectedIp]);
        }

        Console.WriteLine("Integration test passed successfully!");
    }
}