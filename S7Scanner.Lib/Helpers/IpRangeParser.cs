using System.Net;

namespace S7Scanner.Lib.Helpers;

public static class IpRangeParser
{
    /// <summary>
    /// Parses a string representing an IP address or a range of IP addresses and returns the corresponding sequence of
    /// <see cref="IPAddress"/> objects.
    /// </summary>
    /// <remarks>This method supports both IPv4 and IPv6 addresses. When specifying a range, the start IP
    /// address must not be greater than the end IP address. The method ensures that all IP addresses in the range are
    /// of the same address family.</remarks>
    /// <param name="ipRange">A string representing either a single IP address (e.g., "192.168.1.1") or a range of IP addresses in the format
    /// "startIP-endIP" (e.g., "192.168.1.1-192.168.1.10").</param>
    /// <returns>An <see cref="IEnumerable{IPAddress}"/> containing the parsed IP addresses. If the input represents a single IP
    /// address, the sequence contains one element. If the input represents a range, the sequence contains all IP
    /// addresses within the range, inclusive.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="ipRange"/> is null, empty, or consists only of whitespace, or if the start and end IP
    /// addresses in the range are not of the same address family.</exception>
    /// <exception cref="FormatException">Thrown if <paramref name="ipRange"/> is not a valid IP address or range format, or if any IP address in the
    /// range is invalid.</exception>
    public static IEnumerable<IPAddress> Parse(string ipRange)
    {
        if (string.IsNullOrWhiteSpace(ipRange))
        {
            throw new ArgumentException("IP range cannot be empty.", nameof(ipRange));
        }

        // --- Eager Validation for Single IP ---
        return !ipRange.Contains('-')
            ? ipRange.Split('.').Length != 4
                ? throw new FormatException("Invalid IP address format. A four-part dotted-quad string is required.")
                : IPAddress.TryParse(ipRange, out var singleIp) ? (IEnumerable<IPAddress>)([singleIp]) : throw new FormatException("Invalid IP address format.")
            : ParseRangeInternal(ipRange);
    }

    private static IEnumerable<IPAddress> ParseRangeInternal(string ipRange)
    {
        var parts = ipRange.Split('-');
        if (parts.Length != 2)
        {
            throw new FormatException("Invalid IP range format. Use 'startIP-endIP'.");
        }

        string startIpString = parts[0];
        string endIpString = parts[1];

        if (startIpString.Split('.').Length != 4 || endIpString.Split('.').Length != 4)
        {
            throw new FormatException("Invalid IP address format in range. Both IPs must be four-part dotted-quad strings.");
        }

        if (!IPAddress.TryParse(startIpString, out var startIp) || !IPAddress.TryParse(endIpString, out var endIp))
        {
            throw new FormatException("Invalid IP address in range.");
        }

        if (startIp.AddressFamily != endIp.AddressFamily)
            throw new ArgumentException("Start and end IP addresses must be of the same family.");

        var startBytes = startIp.GetAddressBytes();
        var endBytes = endIp.GetAddressBytes();

        for (int i = 0; i < startBytes.Length; i++)
        {
            if (startBytes[i] > endBytes[i]) throw new ArgumentException("Start IP cannot be greater than end IP.");
            if (startBytes[i] < endBytes[i]) break;
        }

        var currentIpBytes = (byte[])startBytes.Clone();
        while (true)
        {
            yield return new IPAddress(currentIpBytes);
            if (currentIpBytes.SequenceEqual(endBytes)) break;
            for (int i = currentIpBytes.Length - 1; i >= 0; i--)
            {
                currentIpBytes[i]++;
                if (currentIpBytes[i] != 0) break;
            }
        }
    }
}