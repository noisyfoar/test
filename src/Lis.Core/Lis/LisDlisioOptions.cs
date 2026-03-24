using System.Collections.Generic;

namespace Lis.Core.Lis
{
    /// <summary>
    /// Настройки запуска Python-bridge для чтения LIS через dlisio.
    /// </summary>
    public sealed class LisDlisioOptions
    {
        public LisDlisioOptions()
        {
            PythonExecutablePath = "python";
            TimeoutMilliseconds = 120000;
            EnvironmentVariables = new Dictionary<string, string>();
        }

        /// <summary>
        /// Путь или имя python-интерпретатора (например, "python" или "py").
        /// </summary>
        public string PythonExecutablePath { get; set; }

        /// <summary>
        /// Необязательный путь к python-скрипту bridge. Если не задан, используется встроенный ресурс.
        /// </summary>
        public string? ScriptPath { get; set; }

        /// <summary>
        /// Рабочая директория процесса Python. Если не задана, используется директория LIS-файла.
        /// </summary>
        public string? WorkingDirectory { get; set; }

        /// <summary>
        /// Таймаут ожидания завершения Python-процесса в миллисекундах.
        /// </summary>
        public int TimeoutMilliseconds { get; set; }

        /// <summary>
        /// Дополнительные переменные окружения для Python-процесса.
        /// </summary>
        public IDictionary<string, string> EnvironmentVariables { get; }
    }
}
