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
            PreferBundledBridge = true;
            PreferPythonBridge = false;
            EnableCoreFallback = false;
            RequireDlisio = true;
        }

        /// <summary>
        /// Необязательный путь к self-contained bridge executable (например, собранному через PyInstaller).
        /// Такой bridge уже содержит Python+dlisio и не требует установки Python на машине пользователя.
        /// </summary>
        public string? DlisioBridgeExecutablePath { get; set; }

        /// <summary>
        /// Если true, сначала пробуем self-contained bridge executable.
        /// Если путь не задан, клиент пытается авто-обнаружить `lis-dlisio-bridge.exe` рядом с приложением.
        /// </summary>
        public bool PreferBundledBridge { get; set; }

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
        /// Если true, после попытки bundled bridge клиент пытается читать через Python `dlisio`.
        /// </summary>
        public bool PreferPythonBridge { get; set; }

        /// <summary>
        /// Разрешает fallback на встроенный C# парсер Lis.Core, если dlisio-bridge недоступен или завершился ошибкой.
        /// По умолчанию выключен, чтобы не уходить с dlisio в parser из репозитория.
        /// </summary>
        public bool EnableCoreFallback { get; set; }

        /// <summary>
        /// Если true, клиент обязан читать именно через dlisio bridge (bundled exe или Python mode).
        /// При невозможности запуска dlisio bridge будет выброшено исключение без fallback на Lis.Core parser.
        /// </summary>
        public bool RequireDlisio { get; set; }

        /// <summary>
        /// Дополнительные переменные окружения для Python-процесса.
        /// </summary>
        public IDictionary<string, string> EnvironmentVariables { get; }
    }
}
