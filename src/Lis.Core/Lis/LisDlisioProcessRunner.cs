using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Lis.Core.Lis
{
    /// <summary>
    /// Запускает внешний Python-процесс и возвращает stdout/stderr.
    /// </summary>
    public sealed class LisDlisioProcessRunner : ILisDlisioProcessRunner
    {
        public LisDlisioProcessResult Run(
            string executablePath,
            string arguments,
            string workingDirectory,
            int timeoutMilliseconds,
            IDictionary<string, string> environmentVariables)
        {
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                throw new ArgumentException("Executable path must not be empty.", nameof(executablePath));
            }

            if (string.IsNullOrWhiteSpace(workingDirectory))
            {
                throw new ArgumentException("Working directory must not be empty.", nameof(workingDirectory));
            }

            if (timeoutMilliseconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds), "Timeout must be greater than zero.");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = arguments ?? string.Empty,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            if (environmentVariables != null)
            {
                foreach (KeyValuePair<string, string> pair in environmentVariables)
                {
                    if (string.IsNullOrWhiteSpace(pair.Key))
                    {
                        continue;
                    }

                    startInfo.EnvironmentVariables[pair.Key] = pair.Value ?? string.Empty;
                }
            }

            using var process = new Process();
            process.StartInfo = startInfo;

            var stdout = new StringBuilder();
            var stderr = new StringBuilder();
            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    stdout.AppendLine(e.Data);
                }
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    stderr.AppendLine(e.Data);
                }
            };

            if (!process.Start())
            {
                throw new LisDlisioBridgeException("Python process could not be started.");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            bool exited = process.WaitForExit(timeoutMilliseconds);
            if (!exited)
            {
                try
                {
                    process.Kill();
                }
                catch
                {
                    // Игнорируем вторичную ошибку при прерывании зависшего процесса.
                }

                return new LisDlisioProcessResult(
                    exitCode: -1,
                    stdout: stdout.ToString(),
                    stderr: stderr.ToString(),
                    timedOut: true);
            }

            process.WaitForExit();
            return new LisDlisioProcessResult(
                process.ExitCode,
                stdout.ToString(),
                stderr.ToString(),
                timedOut: false);
        }
    }
}
