using S7Scanner.Lib.Helpers;
using S7Scanner.Lib.Models;
using S7Scanner.Lib.Services;

namespace S7Scanner.IntegrationTests.Services;

/// <summary>
/// Provides integration tests for the <see cref="S7ScannerService"/> to verify its ability to discover and classify
/// devices on a real network.
/// </summary>
/// <remarks>These tests are designed to run against a physical network environment with specific devices online
/// and configured. Ensure the network setup matches the expected configuration before running the tests.</remarks>
public class S7ScannerService_IntegrationTests
{
    private const int _realNetworkTimeoutMs = 1500;
    private const int _realNetworkParallelism = 5;

    /// <summary>
    /// This test verifies the scanner's ability to correctly identify, classify,
    /// and retrieve details from a known set of devices on the physical network.
    /// It requires the following devices to be online and configured:
    /// - 192.168.0.2: S7-300 PLC (should return full details)
    /// - 192.168.0.3: HMI (should have null details)
    /// - 192.168.0.4: HMI (should have null details)
    /// - 192.168.0.5: S7-1200 PLC (should return placeholder details)
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task DiscoverDevicesAsync_OnRealNetwork_ShouldFindClassifyAndDetailKnownDevices()
    {
        // ARRANGE
        const string ipRange = "192.168.0.1-192.168.0.5";
        var ipsToScan = IpRangeParser.Parse(ipRange);

        // ACT
        var discoveredDevices = (await S7ScannerService.DiscoverDevicesAsync(
            ipsToScan, _realNetworkTimeoutMs, _realNetworkParallelism, CancellationToken.None)).ToList();

        var resultsMap = discoveredDevices.ToDictionary(d => d.IpAddress.ToString());

        // ASSERT
        // We expect exactly 4 devices in this test.
        Assert.Equal(4, resultsMap.Count);

        // --- Verify Device 1: S7-300 PLC at 192.168.0.2 ---
        Assert.True(resultsMap.TryGetValue("192.168.0.2", out var s7_300), "S7-300 PLC at 192.168.0.2 was not found.");
        Assert.Equal(DeviceType.PLC, s7_300.Type);
        Assert.NotNull(s7_300.Details);
        // We expect full details, so fields should not be null or the placeholder.
        Assert.False(string.IsNullOrEmpty(s7_300.Details.Module), "S7-300 Module should not be empty.");
        Assert.False(string.IsNullOrEmpty(s7_300.Details.SerialNumber), "S7-300 SerialNumber should not be empty.");
        Assert.NotEqual("Potential S7-1200/-1500", s7_300.Details.Module);

        // --- Verify Device 2: HMI at 192.168.0.3 ---
        Assert.True(resultsMap.TryGetValue("192.168.0.3", out var hmi1), "HMI at 192.168.0.3 was not found.");
        Assert.Equal(DeviceType.HMI, hmi1.Type);
        // HMIs should have null details.
        Assert.Null(hmi1.Details);

        // --- Verify Device 3: HMI at 192.168.0.4 ---
        Assert.True(resultsMap.TryGetValue("192.168.0.4", out var hmi2), "HMI at 192.168.0.4 was not found.");
        Assert.Equal(DeviceType.HMI, hmi2.Type);
        Assert.Null(hmi2.Details);

        // --- Verify Device 4: S7-1200 PLC at 192.168.0.5 ---
        Assert.True(resultsMap.TryGetValue("192.168.0.5", out var s7_1200), "S7-1200 PLC at 192.168.0.5 was not found.");
        Assert.Equal(DeviceType.PLC, s7_1200.Type);
        Assert.NotNull(s7_1200.Details);
        // For S7-1200, we expect the specific placeholder text.
        Assert.Equal("Potential S7-1200/-1500", s7_1200.Details.Module);
        Assert.Equal("Potential S7-1200/-1500", s7_1200.Details.SystemName);
        Assert.Equal("Potential S7-1200/-1500", s7_1200.Details.SerialNumber);
    }
}