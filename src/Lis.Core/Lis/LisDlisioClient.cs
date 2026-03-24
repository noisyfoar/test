using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Lis.Core.Lis
{
    /// <summary>
    /// .NET Framework 4.8 совместимый клиент для чтения LIS через Python-библиотеку dlisio.
    /// </summary>
    public sealed class LisDlisioClient
    {
        private readonly ILisDlisioProcessRunner _processRunner;

        public LisDlisioClient()
            : this(new LisDlisioProcessRunner())
        {
        }

        public LisDlisioClient(ILisDlisioProcessRunner processRunner)
        {
            _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        }

        /// <summary>
        /// Читает LIS-файл через Python dlisio и возвращает структурный summary.
        /// </summary>
        public LisDlisioSummary ReadSummary(string lisPath, LisDlisioOptions? options = null)
        {
            if (lisPath == null)
            {
                throw new ArgumentNullException(nameof(lisPath));
            }

            if (lisPath.Trim().Length == 0)
            {
                throw new ArgumentException("LIS path must not be empty.", nameof(lisPath));
            }

            string fullLisPath = Path.GetFullPath(lisPath);
            if (!File.Exists(fullLisPath))
            {
                throw new FileNotFoundException("LIS file was not found.", fullLisPath);
            }

            LisDlisioOptions effectiveOptions = options ?? new LisDlisioOptions();
            string workingDirectory = ResolveWorkingDirectory(effectiveOptions, fullLisPath);
            string pythonExecutable = ResolvePythonExecutable(effectiveOptions);
            int timeout = ResolveTimeout(effectiveOptions.TimeoutMilliseconds);

            string? tempScriptPath = null;
            string scriptPath = ResolveScriptPath(effectiveOptions, out tempScriptPath);

            try
            {
                string arguments = BuildArguments(scriptPath, fullLisPath);
                LisDlisioProcessResult result;
                try
                {
                    result = _processRunner.Run(
                        pythonExecutable,
                        arguments,
                        workingDirectory,
                        timeout,
                        effectiveOptions.EnvironmentVariables);
                }
                catch (Exception ex) when (!(ex is LisDlisioBridgeException))
                {
                    throw new LisDlisioBridgeException(
                        "Python dlisio process could not be started or executed. " +
                        "Check Python installation, dlisio package and process permissions.",
                        ex);
                }

                if (result.TimedOut)
                {
                    throw new LisDlisioBridgeException(
                        "Python dlisio process timed out after " + timeout.ToString(CultureInfo.InvariantCulture) + " ms.");
                }

                if (result.ExitCode != 0)
                {
                    string message = "Python dlisio process failed with exit code " +
                        result.ExitCode.ToString(CultureInfo.InvariantCulture) +
                        ". stderr: " + NormalizeErrorText(result.StdErr);
                    if (!string.IsNullOrWhiteSpace(result.StdOut))
                    {
                        message += " stdout: " + NormalizeErrorText(result.StdOut);
                    }

                    throw new LisDlisioBridgeException(message);
                }

                if (string.IsNullOrWhiteSpace(result.StdOut))
                {
                    throw new LisDlisioBridgeException("Python dlisio process returned empty output.");
                }

                return DeserializeSummary(result.StdOut);
            }
            finally
            {
                TryDeleteTempScript(tempScriptPath);
            }
        }

        private static LisDlisioSummary DeserializeSummary(string json)
        {
            try
            {
                var serializer = new DataContractJsonSerializer(typeof(JsonSummaryDto));
                using var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
                var dto = (JsonSummaryDto?)serializer.ReadObject(ms);
                if (dto == null)
                {
                    throw new LisDlisioBridgeException("Python dlisio output is empty or invalid JSON.");
                }

                return MapSummary(dto);
            }
            catch (SerializationException ex)
            {
                throw new LisDlisioBridgeException("Python dlisio output could not be parsed as JSON.", ex);
            }
        }

        private static LisDlisioSummary MapSummary(JsonSummaryDto dto)
        {
            var files = new List<LisDlisioLogicalFileSummary>();
            if (dto.LogicalFiles != null)
            {
                for (int i = 0; i < dto.LogicalFiles.Count; i++)
                {
                    JsonLogicalFileDto sourceFile = dto.LogicalFiles[i] ?? new JsonLogicalFileDto();
                    var dfsrs = new List<LisDlisioDfsrSummary>();
                    if (sourceFile.Dfsrs != null)
                    {
                        for (int d = 0; d < sourceFile.Dfsrs.Count; d++)
                        {
                            JsonDfsrDto sourceDfsr = sourceFile.Dfsrs[d] ?? new JsonDfsrDto();
                            var sampleRates = sourceDfsr.SampleRates ?? new List<int>();
                            var channels = new List<LisDlisioChannelSummary>();
                            if (sourceDfsr.Channels != null)
                            {
                                for (int c = 0; c < sourceDfsr.Channels.Count; c++)
                                {
                                    JsonChannelDto sourceChannel = sourceDfsr.Channels[c] ?? new JsonChannelDto();
                                    channels.Add(new LisDlisioChannelSummary(
                                        sourceChannel.Mnemonic ?? string.Empty,
                                        sourceChannel.Units ?? string.Empty,
                                        sourceChannel.Samples,
                                        sourceChannel.RepresentationCode));
                                }
                            }

                            dfsrs.Add(new LisDlisioDfsrSummary(
                                sourceDfsr.Index,
                                sourceDfsr.Subtype,
                                sampleRates,
                                channels));
                        }
                    }

                    files.Add(new LisDlisioLogicalFileSummary(
                        sourceFile.Index,
                        sourceFile.FileHeaderName,
                        sourceFile.FileTrailerName,
                        sourceFile.TextRecordCount,
                        dfsrs));
                }
            }

            IReadOnlyList<string> errors = dto.DlisioErrors ?? new List<string>();
            return new LisDlisioSummary(files, errors);
        }

        private static string ResolveWorkingDirectory(LisDlisioOptions options, string lisFullPath)
        {
            string candidate = options.WorkingDirectory ?? Path.GetDirectoryName(lisFullPath) ?? Environment.CurrentDirectory;
            candidate = Path.GetFullPath(candidate);
            if (!Directory.Exists(candidate))
            {
                throw new DirectoryNotFoundException("Working directory was not found: " + candidate);
            }

            return candidate;
        }

        private static string ResolvePythonExecutable(LisDlisioOptions options)
        {
            string executable = options.PythonExecutablePath ?? string.Empty;
            if (string.IsNullOrWhiteSpace(executable))
            {
                throw new ArgumentException("Python executable path must not be empty.", nameof(options));
            }

            return executable;
        }

        private static int ResolveTimeout(int timeoutMilliseconds)
        {
            if (timeoutMilliseconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds), "Timeout must be greater than zero.");
            }

            return timeoutMilliseconds;
        }

        private static string ResolveScriptPath(LisDlisioOptions options, out string? tempScriptPath)
        {
            tempScriptPath = null;
            if (!string.IsNullOrWhiteSpace(options.ScriptPath))
            {
                string full = Path.GetFullPath(options.ScriptPath!);
                if (!File.Exists(full))
                {
                    throw new FileNotFoundException("Python bridge script was not found.", full);
                }

                return full;
            }

            string tempDir = Path.Combine(Path.GetTempPath(), "lis-core-dlisio");
            Directory.CreateDirectory(tempDir);
            string filePath = Path.Combine(tempDir, "dlisio_bridge_" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture) + ".py");
            File.WriteAllText(filePath, PythonBridgeScript, Encoding.UTF8);
            tempScriptPath = filePath;
            return filePath;
        }

        private static void TryDeleteTempScript(string? tempScriptPath)
        {
            if (string.IsNullOrWhiteSpace(tempScriptPath))
            {
                return;
            }

            try
            {
                if (File.Exists(tempScriptPath))
                {
                    File.Delete(tempScriptPath);
                }
            }
            catch
            {
                // Игнорируем cleanup-ошибку: она не должна ломать чтение LIS.
            }
        }

        private static string BuildArguments(string scriptPath, string lisPath)
        {
            return QuoteArgument(scriptPath) + " " + QuoteArgument(lisPath);
        }

        private static string NormalizeErrorText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "<empty>";
            }

            return text.Replace("\r", " ").Replace("\n", " ").Trim();
        }

        private static string QuoteArgument(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "\"\"";
            }

            bool needsQuotes = false;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (char.IsWhiteSpace(c) || c == '"')
                {
                    needsQuotes = true;
                    break;
                }
            }

            if (!needsQuotes)
            {
                return value;
            }

            var builder = new StringBuilder();
            builder.Append('"');
            int backslashes = 0;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == '\\')
                {
                    backslashes++;
                    continue;
                }

                if (c == '"')
                {
                    builder.Append('\\', backslashes * 2 + 1);
                    builder.Append('"');
                    backslashes = 0;
                    continue;
                }

                if (backslashes > 0)
                {
                    builder.Append('\\', backslashes);
                    backslashes = 0;
                }

                builder.Append(c);
            }

            if (backslashes > 0)
            {
                builder.Append('\\', backslashes * 2);
            }

            builder.Append('"');
            return builder.ToString();
        }

        [DataContract]
        private sealed class JsonSummaryDto
        {
            [DataMember(Name = "LogicalFiles")]
            public List<JsonLogicalFileDto>? LogicalFiles { get; set; }

            [DataMember(Name = "DlisioErrors")]
            public List<string>? DlisioErrors { get; set; }
        }

        [DataContract]
        private sealed class JsonLogicalFileDto
        {
            [DataMember(Name = "Index")]
            public int Index { get; set; }

            [DataMember(Name = "FileHeaderName")]
            public string? FileHeaderName { get; set; }

            [DataMember(Name = "FileTrailerName")]
            public string? FileTrailerName { get; set; }

            [DataMember(Name = "TextRecordCount")]
            public int TextRecordCount { get; set; }

            [DataMember(Name = "Dfsrs")]
            public List<JsonDfsrDto>? Dfsrs { get; set; }
        }

        [DataContract]
        private sealed class JsonDfsrDto
        {
            [DataMember(Name = "Index")]
            public int Index { get; set; }

            [DataMember(Name = "Subtype")]
            public int Subtype { get; set; }

            [DataMember(Name = "SampleRates")]
            public List<int>? SampleRates { get; set; }

            [DataMember(Name = "Channels")]
            public List<JsonChannelDto>? Channels { get; set; }
        }

        [DataContract]
        private sealed class JsonChannelDto
        {
            [DataMember(Name = "Mnemonic")]
            public string? Mnemonic { get; set; }

            [DataMember(Name = "Units")]
            public string? Units { get; set; }

            [DataMember(Name = "Samples")]
            public int Samples { get; set; }

            [DataMember(Name = "RepresentationCode")]
            public int RepresentationCode { get; set; }
        }

        private const string PythonBridgeScript = @"#!/usr/bin/env python
import json
import sys
from dlisio import lis

def _safe_trim(value):
    if value is None:
        return None
    return str(value).strip()

def build_summary(lis_path):
    logical_files = []
    errors = []
    try:
        with lis.load(lis_path) as files:
            for file_index, logical_file in enumerate(files):
                try:
                    header = logical_file.header()
                except Exception as ex:
                    header = None
                    errors.append(""LF["" + str(file_index) + ""] header(): "" + str(ex))

                try:
                    trailer = logical_file.trailer()
                except Exception as ex:
                    trailer = None
                    errors.append(""LF["" + str(file_index) + ""] trailer(): "" + str(ex))

                text_count = 0
                try:
                    text_count += len(logical_file.operator_command_inputs())
                    text_count += len(logical_file.operator_response_inputs())
                    text_count += len(logical_file.system_outputs_to_operator())
                    text_count += len(logical_file.flic_comment())
                except Exception as ex:
                    errors.append(""LF["" + str(file_index) + ""] text records: "" + str(ex))

                dfsrs = []
                try:
                    formatspecs = logical_file.data_format_specs()
                except Exception as ex:
                    formatspecs = []
                    errors.append(""LF["" + str(file_index) + ""] data_format_specs(): "" + str(ex))

                for dfsr_index, dfsr in enumerate(formatspecs):
                    sample_rates = set()
                    channels = []
                    for spec in dfsr.specs:
                        mnemonic = _safe_trim(spec.mnemonic) or """"
                        units = _safe_trim(spec.units) or """"
                        samples = int(spec.samples)
                        reprc = int(spec.reprc)
                        sample_rates.add(samples)
                        channels.append({
                            ""Mnemonic"": mnemonic,
                            ""Units"": units,
                            ""Samples"": samples,
                            ""RepresentationCode"": reprc
                        })

                    dfsrs.append({
                        ""Index"": int(dfsr_index),
                        ""Subtype"": int(dfsr.spec_block_subtype),
                        ""SampleRates"": sorted(list(sample_rates)),
                        ""Channels"": channels
                    })

                logical_files.append({
                    ""Index"": int(file_index),
                    ""FileHeaderName"": None if header is None else _safe_trim(header.file_name),
                    ""FileTrailerName"": None if trailer is None else _safe_trim(trailer.file_name),
                    ""TextRecordCount"": int(text_count),
                    ""Dfsrs"": dfsrs
                })
    except Exception as ex:
        errors.append(""load(): "" + str(ex))

    return {
        ""LogicalFiles"": logical_files,
        ""DlisioErrors"": errors
    }

def main():
    if len(sys.argv) != 2:
        sys.stderr.write(""Usage: dlisio_bridge.py <lis_path>\n"")
        return 2
    lis_path = sys.argv[1]
    summary = build_summary(lis_path)
    sys.stdout.write(json.dumps(summary, ensure_ascii=True))
    return 0

if __name__ == ""__main__"":
    raise SystemExit(main())
";
    }
}
