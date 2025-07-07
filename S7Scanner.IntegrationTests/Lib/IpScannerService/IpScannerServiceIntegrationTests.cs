using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace S7Scanner.IntegrationTests.Lib.IpScannerService;

public class IpScannerService_IntegrationTests
{
	// These tests can be slow, so we allow a longer timeout.
	private const int RealNetworkTimeoutMs = 1500;
	private const int RealNetworkParallelism = 5; // Scan the small range concurrently

	/// <summary>
	/// This test verifies the scanner's ability to correctly identify and classify
	/// a known set of devices on the physical network.
	/// It requires the following devices to be online and configured:
	/// - 192.168.0.2: PLC (port 102 open, HMI ports closed)
	/// - 192.168.0.3: HMI (port 102 open, one or more HMI ports open)
	/// - 192.168.0.4: HMI (port 102 open, one or more HMI ports open)
	/// - 192.168.0.5: PLC (port 102 open, HMI ports closed)
	/// </summary>
	[Fact]
	[Trait("Category", "Integration")]
	public async Task DiscoverDevicesAsync_OnRealNetwork_ShouldFindAndClassifyKnownDevices()
	{
		// --- ARRANGE ---
		// 1. Define the IP range to scan.
		const string ipRange = "192.168.0.1-192.168.0.5";
		var ipsToScan = IpRangeParser.Parse(ipRange);

		// 2. Define the exact set of devices we expect to find.
		var expectedDevices = new Dictionary<string, DeviceType>
		{
			{ "192.168.0.2", DeviceType.PLC },
			{ "192.168.0.3", DeviceType.HMI },
			{ "192.168.0.4", DeviceType.HMI },
			{ "192.168.0.5", DeviceType.PLC }
		};

		// --- ACT ---
		// 3. Run the scan against the live network.

		var discoveredDevices = (await IpScannerService.DiscoverDevicesAsync(
			ipsToScan,
			RealNetworkTimeoutMs,
			RealNetworkParallelism,
			CancellationToken.None)).ToList();

		// Convert the results to a dictionary for easy lookup and assertion.
		var resultsMap = discoveredDevices.ToDictionary(d => d.IpAddress.ToString(), d => d.Type);

		// --- ASSERT ---
		// 4. Verify that the results match our expectations exactly.

		// A. The number of found devices must be exactly what we expect.
		// This catches cases where extra devices were found (e.g., .1 was unexpectedly a PLC).
		Assert.Equal(expectedDevices.Count, resultsMap.Count);

		// B. Every expected device must be present in the results with the correct type.
		foreach (var expectedDevice in expectedDevices)
		{
			string expectedIp = expectedDevice.Key;
			DeviceType expectedType = expectedDevice.Value;

			// Check that the IP was found. The Assert.Equal above makes this slightly redundant,
			// but it gives a much better error message if one is missing.
			Assert.True(resultsMap.ContainsKey(expectedIp), $"Expected device at {expectedIp} was not found.");

			// Check that the device type is correct.
			Assert.Equal(expectedType, resultsMap[expectedIp], $"Device at {expectedIp} was misclassified.");
		}
	}
}