using System.Net;

namespace S7Scanner.Lib.Helpers;

/// <summary>
/// Helper class to sort IP addresses
/// </summary>
internal class IpAddressComparer : IComparer<IPAddress>
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