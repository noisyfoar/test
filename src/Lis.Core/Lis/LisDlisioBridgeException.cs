using System;

namespace Lis.Core.Lis
{
    /// <summary>
    /// Ошибка выполнения Python-bridge для чтения LIS через dlisio.
    /// </summary>
    public sealed class LisDlisioBridgeException : Exception
    {
        public LisDlisioBridgeException(string message)
            : base(message)
        {
        }

        public LisDlisioBridgeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
