using S7Scanner.Lib.Helpers;
using System.Net;

namespace S7Scanner.UnitTests.Helpers;

public class IpRangeParserTests
{
    [Fact]
    public void Parse_ValidRange_ReturnsCorrectIpAddresses()
    {
        // Arrange
        const string range = "192.168.1.254-192.168.2.1";
        var expectedIps = new List<IPAddress>
        {
            IPAddress.Parse("192.168.1.254"),
            IPAddress.Parse("192.168.1.255"),
            IPAddress.Parse("192.168.2.0"),
            IPAddress.Parse("192.168.2.1")
        };

        // Act
        var result = IpRangeParser.Parse(range).ToList();

        // Assert
        Assert.Equal(expectedIps, result);
    }

    [Fact]
    public void Parse_SingleIp_ReturnsSingleIp()
    {
        // Arrange
        const string range = "10.0.0.1";
        var expectedIp = IPAddress.Parse("10.0.0.1");

        // Act
        var result = IpRangeParser.Parse(range).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(expectedIp, result[0]);
    }

    [Fact]
    public void Parse_IdenticalStartAndEndIp_ReturnsSingleIp()
    {
        // Arrange
        const string range = "10.10.10.10-10.10.10.10";
        var expectedIp = IPAddress.Parse("10.10.10.10");

        // Act
        var result = IpRangeParser.Parse(range).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(expectedIp, result[0]);
    }

    [Fact]
    public void Parse_StartIpGreaterThanEndIp_ThrowsArgumentException()
    {
        // Arrange
        const string range = "192.168.1.10-192.168.1.5";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => IpRangeParser.Parse(range).ToList());
    }

    [Theory]
    [InlineData("192.168.1.1-b.c.d")]
    [InlineData("192.168.1")]
    [InlineData("192.168.1.1-192.168.1.5-192.168.1.10")]
    public void Parse_InvalidFormat_ThrowsFormatException(string invalidRange)
    {
        // Act & Assert
        Assert.Throws<FormatException>(() => IpRangeParser.Parse(invalidRange).ToList());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_NullOrWhitespaceInput_ThrowsArgumentException(string? input)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => IpRangeParser.Parse(input!).ToList());
    }
}