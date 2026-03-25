using System;
using System.Collections.Generic;
using System.IO;
using Lis.Core.Lis;
using Xunit;

namespace Lis.Tests.Lis
{
    public sealed class LisDlisioClientTests
    {
        [Fact]
        public void ReadSummary_NullPath_ThrowsArgumentNullException()
        {
            var client = new LisDlisioClient(new FakeRunner());

            Assert.Throws<ArgumentNullException>(() => client.ReadSummary(null!));
        }

        [Fact]
        public void ReadSummary_EmptyPath_ThrowsArgumentException()
        {
            var client = new LisDlisioClient(new FakeRunner());

            Assert.Throws<ArgumentException>(() => client.ReadSummary(" "));
        }

        [Fact]
        public void ReadSummary_MissingLisFile_ThrowsFileNotFoundException()
        {
            var client = new LisDlisioClient(new FakeRunner());
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".lis");

            Assert.Throws<FileNotFoundException>(() => client.ReadSummary(path));
        }

        [Fact]
        public void ReadSummary_ProcessTimeout_ThrowsLisDlisioBridgeException()
        {
            using var lis = TempFile.Create(".lis", "stub");
            using var script = TempFile.Create(".py", "# stub");
            var runner = new FakeRunner
            {
                Handler = (_, _, _, _, _) => new LisDlisioProcessResult(
                    exitCode: -1,
                    stdout: string.Empty,
                    stderr: "timeout",
                    timedOut: true)
            };

            var client = new LisDlisioClient(runner);
            var options = new LisDlisioOptions
            {
                ScriptPath = script.Path,
                TimeoutMilliseconds = 1000,
                PreferBundledBridge = false,
                PreferPythonBridge = true,
                EnableCoreFallback = false
            };

            LisDlisioBridgeException ex = Assert.Throws<LisDlisioBridgeException>(() => client.ReadSummary(lis.Path, options));
            Assert.Contains("timed out", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ReadSummary_NonZeroExitCode_ThrowsLisDlisioBridgeException()
        {
            using var lis = TempFile.Create(".lis", "stub");
            using var script = TempFile.Create(".py", "# stub");
            var runner = new FakeRunner
            {
                Handler = (_, _, _, _, _) => new LisDlisioProcessResult(
                    exitCode: 3,
                    stdout: "stdout-data",
                    stderr: "stderr-data",
                    timedOut: false)
            };

            var client = new LisDlisioClient(runner);
            var options = new LisDlisioOptions
            {
                ScriptPath = script.Path,
                PreferBundledBridge = false,
                PreferPythonBridge = true,
                EnableCoreFallback = false
            };

            LisDlisioBridgeException ex = Assert.Throws<LisDlisioBridgeException>(() => client.ReadSummary(lis.Path, options));
            Assert.Contains("exit code 3", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("stderr-data", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ReadSummary_ValidJson_MapsSummaryAndPassesRunnerArguments()
        {
            string tempRoot = Path.Combine(Path.GetTempPath(), "lis-dlisio-tests");
            Directory.CreateDirectory(tempRoot);
            using var lis = TempFile.Create(".lis", "stub", tempRoot, "input file.lis");
            using var script = TempFile.Create(".py", "# stub", tempRoot, "bridge script.py");

            const string json = "{"
                + "\"LogicalFiles\":[{"
                + "\"Index\":0,"
                + "\"FileHeaderName\":\"FILE000001\","
                + "\"FileTrailerName\":\"FILE000001\","
                + "\"TextRecordCount\":4,"
                + "\"Dfsrs\":[{"
                + "\"Index\":0,"
                + "\"Subtype\":1,"
                + "\"SampleRates\":[1],"
                + "\"Channels\":[{"
                + "\"Mnemonic\":\"GR\","
                + "\"Units\":\"API\","
                + "\"Samples\":1,"
                + "\"RepresentationCode\":73"
                + "}]"
                + "}]"
                + "}],"
                + "\"DlisioErrors\":[\"non-fatal warning\"]"
                + "}";

            var runner = new FakeRunner
            {
                Handler = (_, _, _, _, _) => new LisDlisioProcessResult(
                    exitCode: 0,
                    stdout: json,
                    stderr: string.Empty,
                    timedOut: false)
            };

            var client = new LisDlisioClient(runner);
            var options = new LisDlisioOptions
            {
                PythonExecutablePath = "python3",
                ScriptPath = script.Path,
                WorkingDirectory = tempRoot,
                TimeoutMilliseconds = 32100,
                PreferBundledBridge = false,
                PreferPythonBridge = true
            };
            options.EnvironmentVariables["PYTHONPATH"] = "x";

            LisDlisioSummary summary = client.ReadSummary(lis.Path, options);

            Assert.Equal("python3", runner.LastExecutablePath);
            Assert.Equal(tempRoot, runner.LastWorkingDirectory);
            Assert.Equal(32100, runner.LastTimeoutMilliseconds);
            Assert.Contains("PYTHONPATH", runner.LastEnvironmentVariables.Keys);
            Assert.Contains("\"" + script.Path + "\"", runner.LastArguments, StringComparison.Ordinal);
            Assert.Contains("\"" + lis.Path + "\"", runner.LastArguments, StringComparison.Ordinal);

            Assert.Single(summary.LogicalFiles);
            Assert.Single(summary.Errors);
            Assert.Equal("non-fatal warning", summary.Errors[0]);
            Assert.Single(summary.LogicalFiles[0].Dfsrs);
            Assert.Single(summary.LogicalFiles[0].Dfsrs[0].Channels);
            Assert.Equal("GR", summary.LogicalFiles[0].Dfsrs[0].Channels[0].Mnemonic);
        }

        [Fact]
        public void ReadSummary_InvalidJson_ThrowsLisDlisioBridgeException()
        {
            using var lis = TempFile.Create(".lis", "stub");
            using var script = TempFile.Create(".py", "# stub");
            var runner = new FakeRunner
            {
                Handler = (_, _, _, _, _) => new LisDlisioProcessResult(
                    exitCode: 0,
                    stdout: "{bad json",
                    stderr: string.Empty,
                    timedOut: false)
            };

            var client = new LisDlisioClient(runner);
            var options = new LisDlisioOptions
            {
                ScriptPath = script.Path,
                PreferBundledBridge = false,
                PreferPythonBridge = true,
                EnableCoreFallback = false
            };

            LisDlisioBridgeException ex = Assert.Throws<LisDlisioBridgeException>(() => client.ReadSummary(lis.Path, options));
            Assert.Contains("json", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ReadSummary_RunnerThrows_ThrowsLisDlisioBridgeExceptionWithInnerException()
        {
            using var lis = TempFile.Create(".lis", "stub");
            using var script = TempFile.Create(".py", "# stub");
            var runner = new FakeRunner
            {
                Handler = (_, _, _, _, _) => throw new InvalidOperationException("process start failure")
            };

            var client = new LisDlisioClient(runner);
            var options = new LisDlisioOptions
            {
                ScriptPath = script.Path,
                PreferBundledBridge = false,
                PreferPythonBridge = true,
                EnableCoreFallback = false
            };

            LisDlisioBridgeException ex = Assert.Throws<LisDlisioBridgeException>(() => client.ReadSummary(lis.Path, options));
            Assert.NotNull(ex.InnerException);
            Assert.IsType<InvalidOperationException>(ex.InnerException);
        }

        [Fact]
        public void ReadSummary_RunnerThrows_WithFallbackEnabled_ReturnsCoreSummary()
        {
            using var lis = TempFile.Create(".lis", BuildSimpleLisFile());
            using var script = TempFile.Create(".py", "# stub");
            var runner = new FakeRunner
            {
                Handler = (_, _, _, _, _) => throw new InvalidOperationException("python not installed")
            };

            var client = new LisDlisioClient(runner);
            var options = new LisDlisioOptions
            {
                ScriptPath = script.Path,
                PreferBundledBridge = false,
                PreferPythonBridge = true,
                EnableCoreFallback = true
            };

            LisDlisioSummary summary = client.ReadSummary(lis.Path, options);

            Assert.Single(summary.LogicalFiles);
            Assert.Empty(summary.Errors);
            Assert.Equal("FILE000900", summary.LogicalFiles[0].FileHeaderName);
            Assert.Equal("FILE000900", summary.LogicalFiles[0].FileTrailerName);
            Assert.Equal(1, summary.LogicalFiles[0].Dfsrs.Count);
            Assert.Equal("C1", summary.LogicalFiles[0].Dfsrs[0].Channels[0].Mnemonic);
        }

        [Fact]
        public void ReadSummary_DlisioOptionalCoreMode_UsesCore()
        {
            using var lis = TempFile.Create(".lis", BuildSimpleLisFile());
            var runner = new FakeRunner
            {
                Handler = (_, _, _, _, _) =>
                {
                    throw new InvalidOperationException("Runner must not be called.");
                }
            };

            var client = new LisDlisioClient(runner);
            var options = new LisDlisioOptions
            {
                PreferPythonBridge = false,
                RequireDlisio = false,
                EnableCoreFallback = true
            };

            LisDlisioSummary summary = client.ReadSummary(lis.Path, options);

            Assert.False(runner.WasCalled);
            Assert.Single(summary.LogicalFiles);
            Assert.Equal("FILE000900", summary.LogicalFiles[0].FileHeaderName);
            Assert.Single(summary.LogicalFiles[0].Dfsrs);
        }

        [Fact]
        public void ReadSummary_BundledBridgePath_UsesBundledExecutableAndNoPythonArguments()
        {
            using var lis = TempFile.Create(".lis", "stub");
            using var bundled = TempFile.Create(".exe", "stub-exe");
            const string json = "{"
                + "\"LogicalFiles\":[{"
                + "\"Index\":0,"
                + "\"FileHeaderName\":\"FILEBND001\","
                + "\"FileTrailerName\":\"FILEBND001\","
                + "\"TextRecordCount\":0,"
                + "\"Dfsrs\":[]"
                + "}],"
                + "\"DlisioErrors\":[]"
                + "}";

            var runner = new FakeRunner
            {
                Handler = (_, _, _, _, _) => new LisDlisioProcessResult(
                    exitCode: 0,
                    stdout: json,
                    stderr: string.Empty,
                    timedOut: false)
            };

            var client = new LisDlisioClient(runner);
            var options = new LisDlisioOptions
            {
                DlisioBridgeExecutablePath = bundled.Path,
                PreferBundledBridge = true,
                PreferPythonBridge = false,
                EnableCoreFallback = false,
                RequireDlisio = true
            };

            LisDlisioSummary summary = client.ReadSummary(lis.Path, options);

            Assert.Equal(Path.GetFullPath(bundled.Path), runner.LastExecutablePath);
            Assert.Contains("\"" + lis.Path + "\"", runner.LastArguments, StringComparison.Ordinal);
            Assert.DoesNotContain(".py", runner.LastArguments, StringComparison.OrdinalIgnoreCase);
            Assert.Single(summary.LogicalFiles);
            Assert.Equal("FILEBND001", summary.LogicalFiles[0].FileHeaderName);
        }

        [Fact]
        public void ReadSummary_RequireDlisioWithoutBridge_ThrowsLisDlisioBridgeException()
        {
            using var lis = TempFile.Create(".lis", BuildSimpleLisFile());
            var runner = new FakeRunner();
            var client = new LisDlisioClient(runner);
            var options = new LisDlisioOptions
            {
                RequireDlisio = true,
                PreferBundledBridge = true,
                PreferPythonBridge = false,
                EnableCoreFallback = false
            };

            LisDlisioBridgeException ex = Assert.Throws<LisDlisioBridgeException>(() => client.ReadSummary(lis.Path, options));
            Assert.Contains("required", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ReadSummary_DefaultOptions_AreStrictDlisioOnly()
        {
            using var lis = TempFile.Create(".lis", BuildSimpleLisFile());
            var runner = new FakeRunner();
            var client = new LisDlisioClient(runner);

            LisDlisioBridgeException ex = Assert.Throws<LisDlisioBridgeException>(() => client.ReadSummary(lis.Path));
            Assert.Contains("required", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        private sealed class FakeRunner : ILisDlisioProcessRunner
        {
            public Func<string, string, string, int, IDictionary<string, string>, LisDlisioProcessResult>? Handler { get; set; }
            public bool WasCalled { get; private set; }

            public string LastExecutablePath { get; private set; } = string.Empty;

            public string LastArguments { get; private set; } = string.Empty;

            public string LastWorkingDirectory { get; private set; } = string.Empty;

            public int LastTimeoutMilliseconds { get; private set; }

            public IDictionary<string, string> LastEnvironmentVariables { get; private set; } =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            public LisDlisioProcessResult Run(
                string executablePath,
                string arguments,
                string workingDirectory,
                int timeoutMilliseconds,
                IDictionary<string, string> environmentVariables)
            {
                WasCalled = true;
                LastExecutablePath = executablePath;
                LastArguments = arguments;
                LastWorkingDirectory = workingDirectory;
                LastTimeoutMilliseconds = timeoutMilliseconds;
                LastEnvironmentVariables = new Dictionary<string, string>(
                    environmentVariables ?? new Dictionary<string, string>(),
                    StringComparer.OrdinalIgnoreCase);

                if (Handler == null)
                {
                    return new LisDlisioProcessResult(0, "{}", string.Empty, timedOut: false);
                }

                return Handler(
                    executablePath,
                    arguments,
                    workingDirectory,
                    timeoutMilliseconds,
                    environmentVariables);
            }
        }

        private static byte[] BuildSimpleLisFile()
        {
            byte[] header = BuildLogicalRecord(LisRecordType.FileHeader, BuildFileRecordData("FILE000900", "PREV000900"));
            byte[] dfsr = BuildLogicalRecord(LisRecordType.DataFormatSpecification, BuildSimpleDfsrForByteChannel("C1"));
            byte[] data = BuildLogicalRecord(LisRecordType.NormalData, new byte[] { 0x2A });
            byte[] trailer = BuildLogicalRecord(LisRecordType.FileTrailer, BuildFileRecordData("FILE000900", "NEXT000900"));
            byte[] full = Concat(Concat(header, dfsr), Concat(data, trailer));
            return full;
        }

        private static byte[] BuildLogicalRecord(LisRecordType type, byte[] data)
        {
            byte[] payload = new byte[2 + data.Length];
            payload[0] = (byte)type;
            payload[1] = 0x00;
            if (data.Length > 0)
            {
                Buffer.BlockCopy(data, 0, payload, 2, data.Length);
            }

            return BuildPhysicalRecord(0x0000, payload, Array.Empty<byte>());
        }

        private static byte[] BuildPhysicalRecord(ushort attributes, byte[] payload, byte[] trailer)
        {
            ushort length = (ushort)(4 + payload.Length + trailer.Length);
            byte[] header = new byte[]
            {
                (byte)(length >> 8), (byte)(length & 0xFF),
                (byte)(attributes >> 8), (byte)(attributes & 0xFF)
            };

            return Concat(header, payload, trailer);
        }

        private static byte[] BuildSimpleDfsrForByteChannel(string mnemonic)
        {
            byte[] entries = Concat(
                BuildEntry((byte)LisDfsrEntryType.SpecBlockSubtype, 1, (byte)LisRepresentationCode.Byte, new byte[] { 0x00 }),
                BuildEntry((byte)LisDfsrEntryType.Terminator, 0, (byte)LisRepresentationCode.Byte, Array.Empty<byte>()));

            byte[] spec = new byte[40];
            Put(spec, 0, 4, mnemonic);
            Put(spec, 4, 6, "SRV001");
            Put(spec, 10, 8, "00000001");
            Put(spec, 18, 4, "UN");
            spec[33] = 1;
            spec[34] = (byte)LisRepresentationCode.Byte;

            return Concat(entries, spec);
        }

        private static byte[] BuildEntry(byte type, byte size, byte reprc, byte[] value)
        {
            var entry = new byte[3 + value.Length];
            entry[0] = type;
            entry[1] = size;
            entry[2] = reprc;
            if (value.Length > 0)
            {
                Buffer.BlockCopy(value, 0, entry, 3, value.Length);
            }

            return entry;
        }

        private static byte[] BuildFileRecordData(string fileName, string nextOrPrevName)
        {
            var data = new byte[56];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = 0x20;
            }

            int offset = 0;
            Put(data, offset, 10, fileName); offset += 12;
            Put(data, offset, 6, "SRV001"); offset += 6;
            Put(data, offset, 8, "VER1.000"); offset += 8;
            Put(data, offset, 8, "20260324"); offset += 9;
            Put(data, offset, 5, "16384"); offset += 7;
            Put(data, offset, 2, "LI"); offset += 4;
            Put(data, offset, 10, nextOrPrevName);
            return data;
        }

        private static void Put(byte[] buffer, int offset, int length, string value)
        {
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(value ?? string.Empty);
            int copy = Math.Min(length, bytes.Length);
            Buffer.BlockCopy(bytes, 0, buffer, offset, copy);
        }

        private static byte[] Concat(byte[] first, byte[] second)
        {
            var output = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, output, 0, first.Length);
            Buffer.BlockCopy(second, 0, output, first.Length, second.Length);
            return output;
        }

        private sealed class TempFile : IDisposable
        {
            private TempFile(string path)
            {
                Path = path;
            }

            public string Path { get; }

            public static TempFile Create(string extension, string contents, string? directory = null, string? fileName = null)
            {
                string root = directory ?? System.IO.Path.GetTempPath();
                Directory.CreateDirectory(root);
                string name = fileName ?? (Guid.NewGuid().ToString("N") + extension);
                string fullPath = System.IO.Path.Combine(root, name);
                File.WriteAllText(fullPath, contents);
                return new TempFile(fullPath);
            }

            public static TempFile Create(string extension, byte[] contents, string? directory = null, string? fileName = null)
            {
                string root = directory ?? System.IO.Path.GetTempPath();
                Directory.CreateDirectory(root);
                string name = fileName ?? (Guid.NewGuid().ToString("N") + extension);
                string fullPath = System.IO.Path.Combine(root, name);
                File.WriteAllBytes(fullPath, contents);
                return new TempFile(fullPath);
            }

            public void Dispose()
            {
                try
                {
                    if (File.Exists(Path))
                    {
                        File.Delete(Path);
                    }
                }
                catch
                {
                    // Тестовый cleanup не должен ломать тестовый прогон.
                }
            }
        }
    }
}
