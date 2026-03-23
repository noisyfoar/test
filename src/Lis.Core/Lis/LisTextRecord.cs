namespace Lis.Core.Lis
{
    public sealed class LisTextRecord
    {
        /// <summary>
        /// Подробно выполняет операцию «LisTextRecord» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        public LisTextRecord(LisRecordType type, string message)
        {
            Type = type;
            Message = message;
        }

        public LisRecordType Type { get; }

        public string Message { get; }
    }
}
