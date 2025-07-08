using S7Scanner.Lib.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S7Scanner.UnitTests.Services;

/// <summary>
/// Provides unit tests for the <see cref="PlcDetailsService"/>, focusing specifically on the data parsing logic.
/// </summary>
public class PlcDetailsServiceUnitTests
{
    [Fact]
    public void ParseFirstResponse_WithValidData_ShouldExtractCorrectDetails()
    {
        // ARRANGE
        var response = new byte[125];
        response[7] = 0x32; // S7 Protocol ID

        // Embed "6ES7 315-2EH14-0AB0" (plus null terminator) at offset 43
        var moduleBytes = System.Text.Encoding.ASCII.GetBytes("6ES7 315-2EH14-0AB0\0");
        moduleBytes.CopyTo(response, 43);

        // Embed "6ES7 315-2EH14-0AB0" (plus null terminator) at offset 71
        var hardwareBytes = System.Text.Encoding.ASCII.GetBytes("6ES7 315-2EH14-0AB0\0");
        hardwareBytes.CopyTo(response, 71);

        // Embed version bytes 3, 2, 6 at offset 122
        response[122] = 3;
        response[123] = 2;
        response[124] = 6;

        // ACT
        PlcDetailsService.ParseFirstResponse(response, out var module, out var basicHardware, out var version);

        // ASSERT
        Assert.Equal("6ES7 315-2EH14-0AB0", module);
        Assert.Equal("6ES7 315-2EH14-0AB0", basicHardware);
        Assert.Equal("3.2.6", version);
    }

    [Fact]
    public void ParseSecondResponse_WithValidData_ShouldExtractCorrectDetails()
    {
        // ARRANGE
        var response = new byte[200];
        response[7] = 0x32;  // S7 Protocol ID
        response[30] = 0x1c; // SZL ID

        System.Text.Encoding.ASCII.GetBytes("SIMATIC 300(1)\0").CopyTo(response, 39);
        System.Text.Encoding.ASCII.GetBytes("CPU 315-2 PN/DP\0").CopyTo(response, 73);
        System.Text.Encoding.ASCII.GetBytes("S C-U9B12345678\0").CopyTo(response, 175);

        // ACT
        PlcDetailsService.ParseSecondResponse(response, out var systemName, out var moduleType, out var serialNumber, out _, out _);

        // ASSERT
        Assert.Equal("SIMATIC 300(1)", systemName);
        Assert.Equal("CPU 315-2 PN/DP", moduleType);
        Assert.Equal("S C-U9B12345678", serialNumber);
    }

    [Fact]
    public void ParseFirstResponse_WithNullInput_ShouldNotThrowAndReturnNulls()
    {
        // ARRANGE
        byte[]? response = null;

        // ACT
        PlcDetailsService.ParseFirstResponse(response, out var module, out var basicHardware, out var version);

        // ASSERT
        Assert.Null(module);
        Assert.Null(basicHardware);
        Assert.Null(version);
    }

    [Fact]
    public void ParseSecondResponse_WithNullInput_ShouldNotThrowAndReturnNulls()
    {
        // ARRANGE
        byte[]? response = null;

        // ACT
        PlcDetailsService.ParseSecondResponse(response, out var systemName, out var moduleType, out var serialNumber, out var plantId, out var copyright);

        // ASSERT
        Assert.Null(systemName);
        Assert.Null(moduleType);
        Assert.Null(serialNumber);
        Assert.Null(plantId);
        Assert.Null(copyright);
    }

    [Fact]
    public void ParseFirstResponse_WithInsufficientLength_ShouldReturnNulls()
    {
        // ARRANGE
        var response = new byte[124]; // One byte too short
        response[7] = 0x32;

        // ACT
        PlcDetailsService.ParseFirstResponse(response, out var module, out var basicHardware, out var version);

        // ASSERT
        Assert.Null(module);
        Assert.Null(basicHardware);
        Assert.Null(version);
    }

    [Fact]
    public void ParseFirstResponse_WithInvalidS7Id_ShouldReturnNulls()
    {
        // ARRANGE
        var response = new byte[125];
        response[7] = 0x00; // Wrong protocol ID

        // ACT
        PlcDetailsService.ParseFirstResponse(response, out var module, out var basicHardware, out var version);

        // ASSERT
        Assert.Null(module);
        Assert.Null(basicHardware);
        Assert.Null(version);
    }
}
