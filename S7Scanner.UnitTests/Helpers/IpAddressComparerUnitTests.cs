using S7Scanner.Lib.Helpers; // Make IpAddressComparer visible to tests if needed*
using System.Net;

namespace S7Scanner.UnitTests.Helpers;

public class IpAddressComparerTests
{
    private readonly IpAddressComparer _comparer = new();

    public static IEnumerable<object[]> Ipv4TestData =>
        [
            // Smaller IP, Larger IP
            ["192.168.1.1", "192.168.1.10"],
            ["10.0.0.255", "10.0.1.0"],
            ["127.0.0.1", "128.0.0.1"]
        ];

    public static IEnumerable<object[]> Ipv6TestData =>
        [
            // Smaller IP, Larger IP
            ["::1", "::2"],
            ["2001:db8::ffff", "2001:db9::"],
            ["fe80::1", "fe80::1:1"]
        ];

    [Fact]
    public void Compare_FirstArgumentIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var validIp = IPAddress.Parse("127.0.0.1");

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => _comparer.Compare(null, validIp));
        Assert.Equal("x", exception.ParamName);
    }

    [Fact]
    public void Compare_SecondArgumentIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var validIp = IPAddress.Parse("127.0.0.1");

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => _comparer.Compare(validIp, null));
        Assert.Equal("y", exception.ParamName);
    }

    [Theory]
    [MemberData(nameof(Ipv4TestData))]
    [MemberData(nameof(Ipv6TestData))]
    public void Compare_WhenIpsAreEqual_ReturnsZero(string ipString, string _) // We only need the first IP for this test
    {
        // Arrange
        var ip1 = IPAddress.Parse(ipString);
        var ip2 = IPAddress.Parse(ipString);

        // Act
        var result = _comparer.Compare(ip1, ip2);

        // Assert
        Assert.Equal(0, result);
    }

    [Theory]
    [MemberData(nameof(Ipv4TestData))]
    [MemberData(nameof(Ipv6TestData))]
    public void Compare_WhenFirstIpIsSmaller_ReturnsNegative(string smallerIpString, string largerIpString)
    {
        // Arrange
        var smallerIp = IPAddress.Parse(smallerIpString);
        var largerIp = IPAddress.Parse(largerIpString);

        // Act
        var result = _comparer.Compare(smallerIp, largerIp);

        // Assert
        Assert.True(result < 0, "Expected result to be negative.");
    }

    [Theory]
    [MemberData(nameof(Ipv4TestData))]
    [MemberData(nameof(Ipv6TestData))]
    public void Compare_WhenFirstIpIsLarger_ReturnsPositive(string smallerIpString, string largerIpString)
    {
        // Arrange
        var smallerIp = IPAddress.Parse(smallerIpString);
        var largerIp = IPAddress.Parse(largerIpString);

        // Act
        var result = _comparer.Compare(largerIp, smallerIp); // Arguments are swapped

        // Assert
        Assert.True(result > 0, "Expected result to be positive.");
    }

    [Fact]
    public void Compare_Ipv4VersusIpv6_CorrectlySortsByAddressFamily()
    {
        // Arrange
        var ipv4 = IPAddress.Parse("255.255.255.255"); // The "largest" IPv4
        var ipv6 = IPAddress.Parse("::1");            // A "small" IPv6

        // Act
        var ipv4vsIpv6 = _comparer.Compare(ipv4, ipv6);
        var ipv6vsIpv4 = _comparer.Compare(ipv6, ipv4);

        // Assert
        // Based on the logic (xBytes.Length.CompareTo(yBytes.Length)), IPv4 (4 bytes) should be less than IPv6 (16 bytes)
        Assert.True(ipv4vsIpv6 < 0, "IPv4 should be considered less than IPv6.");
        Assert.True(ipv6vsIpv4 > 0, "IPv6 should be considered greater than IPv4.");
    }
}