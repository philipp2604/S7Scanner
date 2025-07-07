using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace S7Scanner.Lib.Models;

/// <summary>
/// Represents a discovered device on the network, including its IP address and type.
/// </summary>
/// <param name="IpAddress">The IP address of the device.</param>
/// <param name="Type">The determined type of the device (PLC or HMI).</param>
public record DiscoveredDevice(IPAddress IpAddress, DeviceType Type);