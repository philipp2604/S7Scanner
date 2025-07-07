using System;
using System.Diagnostics;
using System.Text.Json;
using System.CommandLine;
using S7Scanner.Lib.Helpers;
using S7Scanner.Lib.IpScannerService;
using S7Scanner.Lib.Models;

namespace S7Scanner.CLI
{
    static class Program
    {
        private const int SiemensPlcPort = 102;

        static async Task<int> Main(string[] args)
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

            var rootCommand = new RootCommand($"Scans an IP range for open TCP port {SiemensPlcPort} (Siemens S7).");
            rootCommand.Options.Add(ipRangeOption);
            rootCommand.Options.Add(outputFileOption);

            rootCommand.SetAction(async (parseResult, cancellationToken) => await ExecuteScan(parseResult!.GetValue(ipRangeOption)!, parseResult!.GetValue(outputFileOption)!, cancellationToken));

            CommandLineConfiguration configuration = new(rootCommand)
            {
                Output = new StringWriter(),
                Error = TextWriter.Null
            };

            return await configuration.InvokeAsync(args);
        }

        private static async Task ExecuteScan(string ipRange, FileInfo outputFile, CancellationToken cancellationToken)
        {
            Console.WriteLine("Starting IP Scanner for Siemens PLCs...");
            Console.WriteLine($"Target Port: {SiemensPlcPort}");
            Console.WriteLine($"IP Range: {ipRange}");
            if (outputFile != null)
            {
                Console.WriteLine($"Output File: {outputFile.FullName}");
            }
            Console.WriteLine("---------------------------------------------");

            var stopwatch = Stopwatch.StartNew();
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                Console.WriteLine("Cancellation requested. Finishing in-progress tasks...");
                cts.Cancel();
                e.Cancel = true;
            };

            try
            {
                var parser = new IpRangeParser();
                var scanner = new IpScannerService();

                var ipsToScan = parser.Parse(ipRange);
                var foundIps = await scanner.ScanAsync(ipsToScan, SiemensPlcPort, cts.Token);

                stopwatch.Stop();
                Console.WriteLine("---------------------------------------------");
                Console.WriteLine($"Scan complete in {stopwatch.Elapsed.TotalSeconds:F2} seconds.");

                if (!foundIps.Any())
                {
                    Console.WriteLine("No devices found with port 102 open.");
                    return;
                }

                Console.WriteLine($"Found {foundIps.Count()} device(s):");
                var foundIpStrings = foundIps.Select(ip => ip.ToString()).ToList();
                foundIpStrings.ForEach(Console.WriteLine);

                if (outputFile != null)
                {
                    await WriteResultsToFile(foundIpStrings, outputFile.FullName);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nAn error occurred: {ex.Message}");
                Console.ResetColor();
            }
        }

        private static async Task WriteResultsToFile(List<string> ips, string filePath)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var jsonData = new { FoundIps = ips };
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
}
