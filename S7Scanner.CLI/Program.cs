using S7Scanner.Lib.Helpers;
using S7Scanner.Lib.Models;
using S7Scanner.Lib.Services;
using System.CommandLine;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace S7Scanner.CLI;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        var ipRangeOption = new Option<string>(
            name: "--ip-range")
        {
            Description = "The IP range to scan (e.g., '192.168.1.1-192.168.1.254').",
            Required = true,
            AllowMultipleArgumentsPerToken = false
        };

        var outputFileOption = new Option<FileInfo>(
            name: "--output-file")
        {
            Description = "Optional. Path to save the results as a JSON file."
        };

        var timeoutOption = new Option<int>(
            name: "--timeout")
        {
            Description = "Connection timeout in milliseconds for each IP.",
            DefaultValueFactory = (_) => 500
        };

        var parallelismOption = new Option<int>(
            name: "--parallelism")
        {
            Description = "Number of IPs to scan concurrently.",
            DefaultValueFactory = (_) => 100
        };

        var rootCommand = new RootCommand("Scans an IP range for Siemens devices and classifies them as PLC or HMI.");

        rootCommand.Options.Add(ipRangeOption);
        rootCommand.Options.Add(outputFileOption);
        rootCommand.Options.Add(timeoutOption);
        rootCommand.Options.Add(parallelismOption);

        rootCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            var ipRange = parseResult.GetValue(ipRangeOption)!;
            var outputFile = parseResult.GetValue(outputFileOption);
            var timeout = parseResult.GetValue(timeoutOption);
            var parallelism = parseResult.GetValue(parallelismOption);

            await ExecuteScan(ipRange, outputFile, timeout, parallelism, cancellationToken);
        });

        return await rootCommand.Parse(args).InvokeAsync();
    }

    private static async Task ExecuteScan(string ipRange, FileInfo? outputFile, int timeout, int parallelism, CancellationToken cancellationToken)
    {
        Console.WriteLine("Starting Siemens Device Scanner...");
        Console.WriteLine($"IP Range: {ipRange}");
        Console.WriteLine($"Timeout: {timeout}ms | Parallelism: {parallelism}");
        if (outputFile != null)
        {
            Console.WriteLine($"Output File: {outputFile.FullName}");
        }
        Console.WriteLine("---------------------------------------------");

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var ipsToScan = IpRangeParser.Parse(ipRange);
            var foundDevices = (await S7ScannerService.DiscoverDevicesAsync(
                ipsToScan,
                timeout,
                parallelism,
                cancellationToken)).ToList();

            stopwatch.Stop();
            Console.WriteLine("---------------------------------------------");
            Console.WriteLine($"Scan complete in {stopwatch.Elapsed.TotalSeconds:F2} seconds.");

            if (foundDevices.Count == 0)
            {
                Console.WriteLine("No devices found.");
                return;
            }

            Console.WriteLine($"Found {foundDevices.Count} device(s):");
            foreach (var device in foundDevices)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  - {device.IpAddress,-15} | Type: {device.Type}");
                Console.ResetColor();

                if (device.Details != null)
                {
                    Console.WriteLine($"    {"Module:",-22} {device.Details.Module}");
                    Console.WriteLine($"    {"Basic Hardware:",-22} {device.Details.BasicHardware}");
                    Console.WriteLine($"    {"System Name:",-22} {device.Details.SystemName}");
                    Console.WriteLine($"    {"Module Type:",-22} {device.Details.ModuleType}");
                    Console.WriteLine($"    {"Serial Number:",-22} {device.Details.SerialNumber}");
                    Console.WriteLine($"    {"Version:",-22} {device.Details.Version}");
                    Console.WriteLine($"    {"Plant Identification:",-22} {device.Details.PlantIdentification}");
                    Console.WriteLine($"    {"Copyright:",-22} {device.Details.Copyright}");
                }
            }

            if (outputFile != null)
            {
                await WriteResultsToFile(foundDevices, outputFile.FullName);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\nScan was cancelled by the user.");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nAn error occurred: {ex.Message}");
            Console.ResetColor();
        }
    }

    private static async Task WriteResultsToFile(List<DiscoveredDevice> devices, string filePath)
    {
        try
        {
            JsonSerializerOptions jsonSerializerOptions = new()
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                Converters = { new JsonStringEnumConverter() }
            };
            var options = jsonSerializerOptions;
            var jsonData = new
            {
                ScanSummary = new
                {
                    Timestamp = DateTime.UtcNow,
                    DeviceCount = devices.Count,
                    PlcCount = devices.Count(d => d.Type == DeviceType.PLC),
                    HmiCount = devices.Count(d => d.Type == DeviceType.HMI),
                },
                DiscoveredDevices = devices.ConvertAll(d => new
                {
                    IpAddress = d.IpAddress.ToString(),
                    Type = d.Type.ToString(),
                    d.Details
                })
            };

            string jsonString = JsonSerializer.Serialize(jsonData, options);
            await File.WriteAllTextAsync(filePath, jsonString);
            Console.WriteLine($"\nResults successfully written to {filePath}");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nFailed to write to output file: {ex.Message}");
            Console.ResetColor();
        }
    }
}