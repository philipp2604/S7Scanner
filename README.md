# S7Scanner 📡
A modern, high-performance .NET library for discovering Siemens S7 devices (PLCs and HMIs) across a network. It provides a simple, asynchronous API to scan IP ranges, identify potential Siemens devices, and classify them. The project also includes a ready-to-use command-line tool.

[![.NET 8 (LTS) Build & Test](https://github.com/philipp2604/S7Scanner/actions/workflows/dotnet-8-build-and-test.yml/badge.svg)](https://github.com/philipp2604/S7Scanner/actions/workflows/dotnet-8-build-and-test.yml)
[![.NET 9 (Latest) Build & Test](https://github.com/philipp2604/S7Scanner/actions/workflows/dotnet-9-build-and-test.yml/badge.svg)](https://github.com/philipp2604/S7Scanner/actions/workflows/dotnet-9-build-and-test.yml)
[![Language](https://img.shields.io/badge/language-C%23-blue.svg)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![NuGet Version](https://img.shields.io/nuget/v/philipp2604.S7Scanner.Lib.svg?style=flat-square&logo=nuget)](https://www.nuget.org/packages/philipp2604.S7Scanner.Lib/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![GitHub issues](https://img.shields.io/github/issues/philipp2604/S7Scanner)](https://github.com/philipp2604/S7Scanner/issues)

## ✨ Key Features

- **⚡️ High-Speed Parallel Scanning**: Utilizes modern `async`/`await` and `Parallel.ForEachAsync` to scan hundreds of IP addresses concurrently, delivering results quickly.
- **🔬 Accurate Device Classification**: Intelligently distinguishes between Siemens PLCs and HMIs by checking for the standard S7 communication port (102) and a set of known HMI-specific ports.
- **〰️ Flexible IP Range Parsing**: Easily parse various input formats, including single IP addresses (`192.168.0.1`) and complex ranges (`192.168.0.1-192.168.1.254`).
- **🖥️ Ready-to-Use CLI**: Includes a powerful and easy-to-use command-line interface for immediate scanning without writing any code.
- **🏗️ Modern & Asynchronous API**: A fully `async` and thread-safe library built with modern C# features, including records for immutable data transfer objects.
- **✅ Well-Tested**: Comes with a comprehensive suite of unit and integration tests to ensure reliability and correctness.
- **💾 JSON Output**: The CLI can export scan results to a structured JSON file for easy integration with other tools and scripts.

## 🚀 Getting Started

### Installation

S7Scanner.Lib is available on NuGet. You can install it using the .NET CLI:

```bash
dotnet add package philipp2604.S7Scanner.Lib
```
Or via the NuGet Package Manager in Visual Studio.

### Quick Start (Library Usage)

Here's a simple example of how to use the `S7Scanner.Lib` in your own application.

```csharp
using S7Scanner.Lib.Helpers;
using S7Scanner.Lib.IpScannerService;
using System.Net;

// 1. Define the scan parameters
const string ipRange = "192.168.0.1-192.168.0.254";
const int timeoutMs = 500;
const int parallelism = 100;

Console.WriteLine($"Scanning IP range: {ipRange}...");

try
{
    // 2. Parse the IP range string into a collection of IP addresses
    IEnumerable<IPAddress> ipsToScan = IpRangeParser.Parse(ipRange);

    // 3. Run the discovery process asynchronously
    var discoveredDevices = await IpScannerService.DiscoverDevicesAsync(
        ipsToScan,
        timeoutMs,
        parallelism,
        CancellationToken.None
    );

    // 4. Process the results
    if (!discoveredDevices.Any())
    {
        Console.WriteLine("No devices found.");
    }
    else
    {
        Console.WriteLine($"Found {discoveredDevices.Count()} device(s):");
        foreach (var device in discoveredDevices)
        {
            Console.WriteLine($"  - IP: {device.IpAddress,-15} | Type: {device.Type}");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"An error occurred: {ex.Message}");
}
```

## 🖥️ Command-Line Interface (CLI)

The project includes a pre-built command-line tool for immediate use.

### Usage

```bash
S7Scanner.CLI.exe --ip-range <RANGE> [--output-file <PATH>] [--timeout <MS>] [--parallelism <COUNT>]
```

### Options

| Option                 | Description                                                  | Required | Default |
| ---------------------- | ------------------------------------------------------------ | :------: | :-----: |
| `--ip-range`           | The IP range to scan (e.g., '192.168.1.1-192.168.1.254').     |   Yes    |   N/A   |
| `--output-file`        | Optional. Path to save the results as a JSON file.           |    No    |   N/A   |
| `--timeout`            | Connection timeout in milliseconds for each IP.              |    No    |   500   |
| `--parallelism`        | Number of IPs to scan concurrently.                          |    No    |   100   |

### Example

```bash
# Scan a C-class network and print results to the console
./S7Scanner.CLI.exe --ip-range "192.168.0.1-192.168.0.254"

# Scan with higher timeout and save results to a file
./S7Scanner.CLI.exe --ip-range "10.0.0.1-10.0.255.254" --timeout 1000 --parallelism 200 --output-file "scan_results.json"
```

## 📖 Documentation
- **[IpScannerService](./S7Scanner.Lib/IpScannerService/IpScannerService.cs)**: The `IpScannerService` is the primary entry point for all scanning operations.
- **[CLI Example](./S7Scanner.CLI/Program.cs)**: A runnable console application demonstrating library usage in detail.
- **[Integration Tests](./S7Scanner.IntegrationTests/IpScanner/IpScannerServiceIntegrationTests.cs)**: These tests showcase real-world usage patterns against a live network and serve as excellent, practical examples.

## 🤝 Contributing

Contributions are welcome! Whether it's bug reports, feature requests, or pull requests, your help is appreciated.

1.  **Fork** the repository.
2.  Create a new **branch** for your feature or bug fix.
3.  Make your changes.
4.  Add or update **unit/integration tests** to cover your changes.
5.  Submit a **Pull Request** with a clear description of your changes.

Please open an issue first to discuss any major changes.

## ⚖️ License

This project is licensed under the **MIT License**. See the [LICENSE](LICENSE.txt) file for details. You are free to use, modify, and distribute this software in commercial and private applications.