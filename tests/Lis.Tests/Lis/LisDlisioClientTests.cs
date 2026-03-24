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
                TimeoutMilliseconds = 1000
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
                ScriptPath = script.Path
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
                TimeoutMilliseconds = 32100
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
                ScriptPath = script.Path
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
                ScriptPath = script.Path
            };

            LisDlisioBridgeException ex = Assert.Throws<LisDlisioBridgeException>(() => client.ReadSummary(lis.Path, options));
            Assert.NotNull(ex.InnerException);
            Assert.IsType<InvalidOperationException>(ex.InnerException);
        }

        private sealed class FakeRunner : ILisDlisioProcessRunner
        {
            public Func<string, string, string, int, IDictionary<string, string>, LisDlisioProcessResult>? Handler { get; set; }

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
