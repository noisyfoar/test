using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Lis.Core.Lis;
using Newtonsoft.Json;

namespace Lis.Compare.Cli
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Error.WriteLine("Usage: Lis.Compare.Cli <input.lis> [output.json]");
                return 1;
            }

            string inputPath = args[0];
            if (!File.Exists(inputPath))
            {
                Console.Error.WriteLine("Input file not found: " + inputPath);
                return 1;
            }

            string? outputPath = args.Length > 1 ? args[1] : null;

            try
            {
                Summary summary = BuildSummary(inputPath);
                string json = JsonConvert.SerializeObject(summary, Formatting.Indented);

                if (!string.IsNullOrWhiteSpace(outputPath))
                {
                    File.WriteAllText(outputPath, json);
                }
                else
                {
                    Console.WriteLine(json);
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Failed to build summary: " + ex);
                return 2;
            }
        }

        private static Summary BuildSummary(string inputPath)
        {
            using var stream = File.OpenRead(inputPath);
            var parser = new LisFileParser();
            var metrics = new LisReadMetrics();
            var options = new LisReadOptions(
                selectedCurveMnemonics: null,
                includeFrames: false,
                includeCurves: true);

            IReadOnlyList<LisLogicalFileData> files = parser.Parse(stream, options, metrics);
            var logicalFiles = new List<LogicalFileSummary>(files.Count);

            for (int i = 0; i < files.Count; i++)
            {
                LisLogicalFileData file = files[i];
                var dfsrList = new List<DfsrSummary>(file.DataFormatSpecifications.Count);
                for (int d = 0; d < file.DataFormatSpecifications.Count; d++)
                {
                    LisDataFormatSpecificationRecord dfsr = file.DataFormatSpecifications[d];
                    var channels = new List<ChannelSummary>(dfsr.SpecBlocks.Count);
                    var sampleRates = new SortedSet<int>();

                    for (int c = 0; c < dfsr.SpecBlocks.Count; c++)
                    {
                        LisDfsrSpecBlock spec = dfsr.SpecBlocks[c];
                        sampleRates.Add(spec.Samples);
                        channels.Add(new ChannelSummary
                        {
                            Mnemonic = spec.Mnemonic,
                            Units = spec.Units,
                            Samples = spec.Samples,
                            RepresentationCode = spec.RepresentationCode
                        });
                    }

                    dfsrList.Add(new DfsrSummary
                    {
                        Index = d,
                        Subtype = dfsr.Subtype,
                        SpecCount = dfsr.SpecBlocks.Count,
                        SampleRates = sampleRates.ToList(),
                        Channels = channels
                    });
                }

                var curves = file.Curves
                    .Select(kvp => new CurveSummary
                    {
                        Mnemonic = kvp.Key,
                        SampleCount = kvp.Value.Count
                    })
                    .OrderBy(x => x.Mnemonic, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                logicalFiles.Add(new LogicalFileSummary
                {
                    Index = i,
                    FileHeaderName = file.FileHeader?.FileName,
                    FileTrailerName = file.FileTrailer?.FileName,
                    TextRecordCount = file.TextRecords.Count,
                    DfsrCount = file.DataFormatSpecifications.Count,
                    FrameCount = file.Frames.Count,
                    CurveCount = file.Curves.Count,
                    Curves = curves,
                    Dfsrs = dfsrList
                });
            }

            return new Summary
            {
                SourcePath = Path.GetFullPath(inputPath),
                GeneratedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                LogicalFileCount = logicalFiles.Count,
                LogicalFiles = logicalFiles,
                Metrics = new MetricsSummary
                {
                    LogicalRecordsRead = metrics.LogicalRecordsRead,
                    FdataBytesRead = metrics.FdataBytesRead,
                    SamplesDecoded = metrics.SamplesDecoded,
                    SamplesSkipped = metrics.SamplesSkipped,
                    ParseElapsedMilliseconds = metrics.ParseElapsedMilliseconds
                }
            };
        }

        private sealed class Summary
        {
            public string SourcePath { get; set; } = string.Empty;

            public string GeneratedAtUtc { get; set; } = string.Empty;

            public int LogicalFileCount { get; set; }

            public List<LogicalFileSummary> LogicalFiles { get; set; } = new List<LogicalFileSummary>();

            public MetricsSummary Metrics { get; set; } = new MetricsSummary();
        }

        private sealed class LogicalFileSummary
        {
            public int Index { get; set; }

            public string? FileHeaderName { get; set; }

            public string? FileTrailerName { get; set; }

            public int TextRecordCount { get; set; }

            public int DfsrCount { get; set; }

            public int FrameCount { get; set; }

            public int CurveCount { get; set; }

            public List<CurveSummary> Curves { get; set; } = new List<CurveSummary>();

            public List<DfsrSummary> Dfsrs { get; set; } = new List<DfsrSummary>();
        }

        private sealed class DfsrSummary
        {
            public int Index { get; set; }

            public byte Subtype { get; set; }

            public int SpecCount { get; set; }

            public List<int> SampleRates { get; set; } = new List<int>();

            public List<ChannelSummary> Channels { get; set; } = new List<ChannelSummary>();
        }

        private sealed class ChannelSummary
        {
            public string Mnemonic { get; set; } = string.Empty;

            public string Units { get; set; } = string.Empty;

            public int Samples { get; set; }

            public byte RepresentationCode { get; set; }
        }

        private sealed class CurveSummary
        {
            public string Mnemonic { get; set; } = string.Empty;

            public int SampleCount { get; set; }
        }

        private sealed class MetricsSummary
        {
            public long LogicalRecordsRead { get; set; }

            public long FdataBytesRead { get; set; }

            public long SamplesDecoded { get; set; }

            public long SamplesSkipped { get; set; }

            public long ParseElapsedMilliseconds { get; set; }
        }
    }
}
