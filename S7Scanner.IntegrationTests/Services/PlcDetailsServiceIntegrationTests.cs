using S7Scanner.Lib.Services;
using System.Net;

namespace S7Scanner.IntegrationTests.Services;

/// <summary>
/// Provides integration tests for the <see cref="PlcDetailsService"/> to verify its ability to query
/// real devices on a network.
/// </summary>
/// <remarks>
/// These tests require a physical network environment with specific devices online.
/// - 192.168.0.2: An S7-300/400 PLC that will respond with full details.
/// - 192.168.0.5: An S7-1200/1500 PLC that will refuse the connection/query.
/// - 192.168.0.99: An IP address that is not in use.
/// </remarks>
public class PlcDetailsService_IntegrationTests
{
    private const int _realNetworkTimeoutMs = 1500;
    private const int _siemensS7Port = 102;

    // --- Test Targets ---
    private static readonly IPAddress _s7_300_Ip = IPAddress.Parse("192.168.0.2");

    private static readonly IPAddress _s7_1200_Ip = IPAddress.Parse("192.168.0.5");
    private static readonly IPAddress _unreachableIp = IPAddress.Parse("192.168.0.99");

    /// <summary>
    /// Verifies that a query to a classic S7-300/400 PLC returns a complete details object.
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPlcDetailsAsync_WithReachableS7_300_ShouldReturnFullDetails()
    {
        // ARRANGE
        // Test target is a known S7-300 PLC.

        // ACT
        var details = await PlcDetailsService.GetPlcDetailsAsync(_s7_300_Ip, _siemensS7Port, _realNetworkTimeoutMs);

        // ASSERT
        Assert.NotNull(details);
        Assert.False(string.IsNullOrEmpty(details.Module), "Module should not be empty.");
        Assert.False(string.IsNullOrEmpty(details.SerialNumber), "SerialNumber should not be empty.");
        Assert.False(string.IsNullOrEmpty(details.Version), "Version should not be empty.");
        Assert.False(string.IsNullOrEmpty(details.SystemName), "SystemName should not be empty.");
    }

    /// <summary>
    /// Verifies that a query to a modern S7-1200/1500 PLC, which typically closes the connection,
    /// results in a null return value from this service. The placeholder logic is handled by the calling service.
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPlcDetailsAsync_WithReachableS7_1200_ShouldReturnNull()
    {
        // ARRANGE
        // Test target is a known S7-1200 that is expected to fail the SZL query.
        // The service should handle the exception gracefully and return null.

        // ACT
        var details = await PlcDetailsService.GetPlcDetailsAsync(_s7_1200_Ip, _siemensS7Port, _realNetworkTimeoutMs);

        // ASSERT
        Assert.Null(details);
    }

    /// <summary>
    /// Verifies that a query to an IP address that is not in use fails gracefully and returns null.
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPlcDetailsAsync_WithUnreachableIp_ShouldReturnNull()
    {
        // ARRANGE
        // Test target is an IP address that is known to be offline.

        // ACT
        var details = await PlcDetailsService.GetPlcDetailsAsync(_unreachableIp, _siemensS7Port, _realNetworkTimeoutMs);

        // ASSERT
        Assert.Null(details);
    }
}