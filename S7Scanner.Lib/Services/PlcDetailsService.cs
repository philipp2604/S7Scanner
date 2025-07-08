using S7Scanner.Lib.Models;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace S7Scanner.Lib.Services;

/// <summary>
/// Provides functionality to retrieve detailed information from a Siemens S7-300 PLC.
/// This service implements the S7 Communication protocol to query System-List-Data (SZL).
/// </summary>
internal static class PlcDetailsService
{
    // Pre-defined S7COMM/COTP packets
    private static readonly byte[] _ctop_cr_packet = [0x03, 0x00, 0x00, 0x16, 0x11, 0xe0, 0x00, 0x00, 0x00, 0x14, 0x00, 0xc1, 0x02, 0x01, 0x00, 0xc2, 0x02, 0x01, 0x02, 0xc0, 0x01, 0x0a];

    private static readonly byte[] _s7_setup_packet = [0x03, 0x00, 0x00, 0x19, 0x02, 0xf0, 0x80, 0x32, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0xf0, 0x00, 0x00, 0x01, 0x00, 0x01, 0x01, 0xe0];
    private static readonly byte[] _szl_first_request = [0x03, 0x00, 0x00, 0x21, 0x02, 0xf0, 0x80, 0x32, 0x07, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x08, 0x00, 0x01, 0x12, 0x04, 0x11, 0x44, 0x01, 0x00, 0xff, 0x09, 0x00, 0x04, 0x00, 0x11, 0x00, 0x01];
    private static readonly byte[] _szl_second_request = [0x03, 0x00, 0x00, 0x21, 0x02, 0xf0, 0x80, 0x32, 0x07, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x08, 0x00, 0x01, 0x12, 0x04, 0x11, 0x44, 0x01, 0x00, 0xff, 0x09, 0x00, 0x04, 0x00, 0x1c, 0x00, 0x01];

    /// <summary>
    /// Attempts to query detailed information from a PLC at the given IP address.
    /// </summary>
    /// <param name="ip">The IP address of the PLC.</param>
    /// <param name="port">The port to connect to (typically 102).</param>
    /// <param name="timeoutMs">The timeout for network operations.</param>
    /// <returns>A <see cref="PlcDetails"/> object with the information, or null if the query fails for a generic reason.
    /// A specific object is returned for newer PLCs that close the connection.</returns>
    public static async Task<PlcDetails?> GetPlcDetailsAsync(IPAddress ip, int port, int timeoutMs)
    {
        using var client = new TcpClient { SendTimeout = timeoutMs, ReceiveTimeout = timeoutMs };
        try
        {
            await client.ConnectAsync(ip, port);
            await using var stream = client.GetStream();

            // 1. COTP Connection
            var response = await SendAndReceiveAsync(stream, _ctop_cr_packet, 22, timeoutMs);
            if (response == null || response.Length < 6 || response[5] != 0xd0) // Check for CC (Connection Confirm)
            {
                return null;
            }

            // 2. S7 Communication Setup
            response = await SendAndReceiveAsync(stream, _s7_setup_packet, 25, timeoutMs);
            if (response == null || response.Length < 8 || response[7] != 0x32)
            {
                return null;
            }

            // 3. Send first SZL request and parse
            response = await SendAndReceiveAsync(stream, _szl_first_request, 125, timeoutMs);
            ParseFirstResponse(response, out var module, out var basicHardware, out var version);

            // 4. Send second SZL request and parse
            response = await SendAndReceiveAsync(stream, _szl_second_request, 180, timeoutMs);
            ParseSecondResponse(response, out var systemName, out var moduleType, out var serialNumber, out var plantId, out var copyright);

            // Check if any data was retrieved
            return string.IsNullOrEmpty(module) && string.IsNullOrEmpty(serialNumber) && string.IsNullOrEmpty(systemName)
                ? null
                : new PlcDetails
                {
                    Module = module,
                    BasicHardware = basicHardware,
                    Version = version,
                    SystemName = systemName,
                    ModuleType = moduleType,
                    SerialNumber = serialNumber,
                    PlantIdentification = plantId,
                    Copyright = copyright
                };
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static async Task<byte[]?> SendAndReceiveAsync(NetworkStream stream, byte[] request, int minBytesToRead, int timeoutMs)
    {
        await stream.WriteAsync(request);
        var buffer = new byte[4096];

        using var cts = new CancellationTokenSource(timeoutMs);
        var readTask = stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);

        var completedTask = await Task.WhenAny(readTask, Task.Delay(timeoutMs, cts.Token));
        if (completedTask != readTask || !readTask.IsCompletedSuccessfully)
        {
            return null; // Timeout or other read error
        }

        int bytesRead = readTask.Result;
        return bytesRead < minBytesToRead ? null : [.. buffer.Take(bytesRead)];
    }

    internal static void ParseFirstResponse(byte[]? response, out string? module, out string? basicHardware, out string? version)
    {
        module = null;
        basicHardware = null;
        version = null;

        if (response == null || response.Length < 125 || response[7] != 0x32) return;

        module = ParseNullTerminatedString(response, 43);
        basicHardware = ParseNullTerminatedString(response, 71);

        byte v1 = response[122];
        byte v2 = response[123];
        byte v3 = response[124];
        version = $"{v1}.{v2}.{v3}";
    }

    internal static void ParseSecondResponse(byte[]? response, out string? systemName, out string? moduleType, out string? serialNumber, out string? plantId, out string? copyright)
    {
        systemName = null;
        moduleType = null;
        serialNumber = null;
        plantId = null;
        copyright = null;

        if (response == null || response.Length < 40 || response[7] != 0x32) return;

        int offset = (response[30] == 0x1c) ? 0 : 4;

        systemName = ParseNullTerminatedString(response, 39 + offset);
        moduleType = ParseNullTerminatedString(response, 73 + offset);
        serialNumber = ParseNullTerminatedString(response, 175 + offset);
        plantId = ParseNullTerminatedString(response, 107 + offset);
        copyright = ParseNullTerminatedString(response, 141 + offset);
    }

    private static string ParseNullTerminatedString(byte[] data, int offset)
    {
        if (offset >= data.Length) return string.Empty;

        int end = Array.IndexOf(data, (byte)0, offset);
        if (end == -1) end = data.Length;

        int length = end - offset;
        return length <= 0 ? string.Empty : Encoding.ASCII.GetString(data, offset, length).Trim();
    }
}