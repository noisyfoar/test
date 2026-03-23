using System;

namespace Lis.Core.Lis
{
    public sealed class LisParseException : Exception
    {
        /// <summary>
        /// Подробно выполняет операцию «LisParseException» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        public LisParseException(string message)
            : base(message)
        {
        }
    }
}
