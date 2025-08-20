using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SimplePackageAnalysis
{
    /// <summary>
    /// Simple package analyzer that reads the basic DBPF structure without full parsing
    /// This bypasses the TS4Tools resource enumeration bug by reading package headers directly
    /// </summary>
    class Program
    {
        private static readonly Dictionary<uint, string> KnownResourceTypes = new()
        {
            { 0x00B2D882, "DDS" },
            { 0x2E75C764, "THUM" },
            { 0x2E75C765, "IMG" },
            { 0x220557DA, "STBL" },
            { 0x015A1849, "GEOM" },
            { 0x01661233, "MODL" },
            { 0x736884F1, "MLOD" },
            { 0x319E4F1D, "CASP" },
            { 0x034AEECB, "OBJD" },
            { 0x0355E0A6, "CAS" },
            { 0x6B20C4F3, "CLIP" },
            { 0x8EAF13DE, "RIG" },
            { 0x18D878AF, "SNR" },
            { 0x6017E896, "SIMO" },
            { 0x62E94D38, "BHV" },
            { 0x0166038C, "LAYO" },
            { 0x073FAA07, "SCRIPT" },
            { 0x545AC67A, "SWB" },
            { 0x1C4A276C, "CNV" },
            { 0x025C90A6, "BUFF" }
        };

        static async Task Main(string[] args)
        {
            string gameDirectory = "/home/dez/snap/steam/common/.local/share/Steam/steamapps/common/The Sims 4";
            
            Console.WriteLine("Simple Package Analysis Tool");
            Console.WriteLine("Reading DBPF headers directly to bypass TS4Tools enumeration bug");
            Console.WriteLine($"Game Directory: {gameDirectory}");
            Console.WriteLine();

            var packageFiles = Directory.GetFiles(gameDirectory, "*.package", SearchOption.AllDirectories);
            Console.WriteLine($"Found {packageFiles.Length} package files");
            Console.WriteLine();

            var results = new AnalysisResults();
            var resourceTypeStats = new Dictionary<uint, ResourceTypeInfo>();
            var unknownTypes = new Dictionary<uint, int>();

            Console.WriteLine($"{"Package File",-50} {"Resources",-10} {"Size",-15} {"Status",-20}");
            Console.WriteLine(new string('-', 100));

            foreach (var packageFile in packageFiles)
            {
                try
                {
                    var fileInfo = new FileInfo(packageFile);
                    var fileName = Path.GetFileName(packageFile);

                    using var fileStream = File.OpenRead(packageFile);
                    var packageInfo = await AnalyzePackageStructure(fileStream);

                    Console.WriteLine($"{fileName,-50} {packageInfo.ResourceCount,-10} {FormatSize(fileInfo.Length),-15} {"Success",-20}");

                    results.TotalPackages++;
                    results.TotalResources += packageInfo.ResourceCount;
                    results.TotalSizeBytes += fileInfo.Length;

                    // Count resource types
                    foreach (var (type, count) in packageInfo.ResourceTypes)
                    {
                        if (!resourceTypeStats.ContainsKey(type))
                        {
                            var typeName = KnownResourceTypes.TryGetValue(type, out var name) ? name : "Unknown";
                            resourceTypeStats[type] = new ResourceTypeInfo
                            {
                                ResourceType = type,
                                TypeName = typeName,
                                Count = 0,
                                IsKnown = KnownResourceTypes.ContainsKey(type)
                            };
                        }

                        resourceTypeStats[type].Count += count;

                        if (!KnownResourceTypes.ContainsKey(type))
                        {
                            unknownTypes[type] = unknownTypes.GetValueOrDefault(type, 0) + count;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{Path.GetFileName(packageFile),-50} {"ERROR",-10} {"N/A",-15} {ex.Message,-20}");
                    results.FailedPackages++;
                }
            }

            Console.WriteLine(new string('-', 100));
            Console.WriteLine();

            // Display summary
            Console.WriteLine("=== Summary Statistics ===");
            Console.WriteLine($"Total package files found: {packageFiles.Length}");
            Console.WriteLine($"Successfully analyzed packages: {results.TotalPackages}");
            Console.WriteLine($"Failed to analyze packages: {results.FailedPackages}");
            Console.WriteLine($"Total resources found: {results.TotalResources:N0}");
            Console.WriteLine($"Total data size: {FormatSize(results.TotalSizeBytes)}");
            Console.WriteLine();

            // Display resource type analysis
            Console.WriteLine("=== Resource Type Analysis ===");
            Console.WriteLine($"{"Type",-12} {"Name",-20} {"Count",-15} {"Known",-10}");
            Console.WriteLine(new string('-', 70));

            foreach (var (type, info) in resourceTypeStats.OrderByDescending(x => x.Value.Count))
            {
                Console.WriteLine($"0x{type:X8} {info.TypeName,-20} {info.Count,-15:N0} {(info.IsKnown ? "Yes" : "No"),-10}");
            }

            Console.WriteLine();

            // Success rate calculation
            var knownResourceCount = resourceTypeStats.Where(x => x.Value.IsKnown).Sum(x => x.Value.Count);
            var unknownResourceCount = results.TotalResources - knownResourceCount;
            var successRate = results.TotalResources > 0 ? (double)knownResourceCount / results.TotalResources * 100 : 0;

            Console.WriteLine($"=== Identification Success Rate ===");
            Console.WriteLine($"Known resource types: {knownResourceCount:N0} ({successRate:F1}%)");
            Console.WriteLine($"Unknown resource types: {unknownResourceCount:N0} ({100 - successRate:F1}%)");
            Console.WriteLine();

            // Save detailed report
            var report = new
            {
                Summary = new
                {
                    TotalPackages = packageFiles.Length,
                    SuccessfullyAnalyzed = results.TotalPackages,
                    Failed = results.FailedPackages,
                    TotalResources = results.TotalResources,
                    TotalSizeBytes = results.TotalSizeBytes,
                    KnownResources = knownResourceCount,
                    UnknownResources = unknownResourceCount,
                    SuccessRate = successRate
                },
                ResourceTypes = resourceTypeStats.Values.OrderByDescending(x => x.Count).ToArray(),
                UnknownTypes = unknownTypes.OrderByDescending(x => x.Value).ToDictionary(x => $"0x{x.Key:X8}", x => x.Value)
            };

            var reportJson = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync("SimplePackageAnalysisReport.json", reportJson);
            Console.WriteLine("Detailed report saved to: SimplePackageAnalysisReport.json");
        }

        private static async Task<PackageInfo> AnalyzePackageStructure(Stream stream)
        {
            var reader = new BinaryReader(stream);
            
            // Read DBPF header
            var magic = reader.ReadBytes(4);
            if (Encoding.ASCII.GetString(magic) != "DBPF")
            {
                throw new InvalidDataException("Not a valid DBPF package file");
            }

            var majorVersion = reader.ReadInt32();
            var minorVersion = reader.ReadInt32();
            reader.ReadInt32(); // user version major
            reader.ReadInt32(); // user version minor
            reader.ReadInt32(); // flags
            reader.ReadInt32(); // created date
            reader.ReadInt32(); // modified date
            var indexMajorVersion = reader.ReadInt32();
            var indexRecordEntryCount = reader.ReadInt32();
            var indexRecordPositionLow = reader.ReadInt32();
            var indexRecordSizeLow = reader.ReadInt32();

            reader.ReadInt32(); // trash entry count
            reader.ReadInt32(); // trash index position
            reader.ReadInt32(); // trash index size
            var indexMinorVersion = reader.ReadInt32();

            // Read position and size 
            long indexPosition = indexRecordPositionLow;
            long indexSize = indexRecordSizeLow;

            if (indexMajorVersion >= 3)
            {
                // 64-bit positions for newer versions
                var indexRecordPositionHigh = reader.ReadInt32();
                var indexRecordSizeHigh = reader.ReadInt32();
                reader.ReadInt32(); // trash position high
                reader.ReadInt32(); // trash size high

                indexPosition = ((long)indexRecordPositionHigh << 32) | (uint)indexRecordPositionLow;
                indexSize = ((long)indexRecordSizeHigh << 32) | (uint)indexRecordSizeLow;
            }

            // Navigate to index
            stream.Seek(indexPosition, SeekOrigin.Begin);

            // Read index entries
            var resourceTypes = new Dictionary<uint, int>();
            
            for (int i = 0; i < indexRecordEntryCount; i++)
            {
                var resourceType = reader.ReadUInt32();
                var resourceGroup = reader.ReadUInt32();
                var instanceLow = reader.ReadUInt32();
                var instanceHigh = reader.ReadUInt32();
                var position = reader.ReadUInt32();
                var fileSize = reader.ReadUInt32() & 0x7FFFFFFF; // Remove compressed flag
                var memSize = reader.ReadUInt32();
                var compressed = reader.ReadUInt16();
                var unknown = reader.ReadUInt16();

                resourceTypes[resourceType] = resourceTypes.GetValueOrDefault(resourceType, 0) + 1;
            }

            return new PackageInfo
            {
                ResourceCount = indexRecordEntryCount,
                ResourceTypes = resourceTypes
            };
        }

        private static string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:F1} {sizes[order]}";
        }
    }

    public class PackageInfo
    {
        public int ResourceCount { get; set; }
        public Dictionary<uint, int> ResourceTypes { get; set; } = new();
    }

    public class AnalysisResults
    {
        public int TotalPackages { get; set; }
        public int FailedPackages { get; set; }
        public long TotalResources { get; set; }
        public long TotalSizeBytes { get; set; }
    }

    public class ResourceTypeInfo
    {
        public uint ResourceType { get; set; }
        public string TypeName { get; set; } = "";
        public long Count { get; set; }
        public bool IsKnown { get; set; }
    }
}
