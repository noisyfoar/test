using System.Collections.Generic;

namespace Lis.Core.Lis
{
    /// <summary>
    /// Абстракция запуска внешнего Python-процесса (нужна для unit-тестов и подмены раннера).
    /// </summary>
    public interface ILisDlisioProcessRunner
    {
        LisDlisioProcessResult Run(
            string executablePath,
            string arguments,
            string workingDirectory,
            int timeoutMilliseconds,
            IDictionary<string, string> environmentVariables);
    }

    public sealed class LisDlisioProcessResult
    {
        public LisDlisioProcessResult(int exitCode, string stdout, string stderr, bool timedOut)
        {
            ExitCode = exitCode;
            StdOut = stdout ?? string.Empty;
            StdErr = stderr ?? string.Empty;
            TimedOut = timedOut;
        }

        public int ExitCode { get; }

        public string StdOut { get; }

        public string StdErr { get; }

        public bool TimedOut { get; }
    }
}
