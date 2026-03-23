namespace Lis.Core.Lis
{
    /// <summary>
    /// Изменяемые счётчики выполнения, заполняемые в процессе разбора LIS.
    /// </summary>
    public sealed class LisReadMetrics
    {
        /// <summary>
        /// Количество логических записей, декодированных из потока.
        /// </summary>
        public long LogicalRecordsRead { get; private set; }

        /// <summary>
        /// Суммарный объём (в байтах) обработанных FData-полезных нагрузок.
        /// </summary>
        public long FdataBytesRead { get; private set; }

        /// <summary>
        /// Количество сэмплов, которые были декодированы и материализованы.
        /// </summary>
        public long SamplesDecoded { get; private set; }

        /// <summary>
        /// Количество сэмплов, пропущенных из-за фильтрации каналов.
        /// </summary>
        public long SamplesSkipped { get; private set; }

        /// <summary>
        /// Полное время разбора, измеренное на уровне <see cref="LisFileParser"/>.
        /// </summary>
        public long ParseElapsedMilliseconds { get; private set; }

        /// <summary>
        /// Подробно выполняет операцию «AddLogicalRecordsRead» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        internal void AddLogicalRecordsRead(long count)
        {
            LogicalRecordsRead += count;
        }

        /// <summary>
        /// Подробно выполняет операцию «AddFdataBytesRead» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        internal void AddFdataBytesRead(long count)
        {
            FdataBytesRead += count;
        }

        /// <summary>
        /// Подробно выполняет операцию «AddSamplesDecoded» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        internal void AddSamplesDecoded(long count)
        {
            SamplesDecoded += count;
        }

        /// <summary>
        /// Подробно выполняет операцию «AddSamplesSkipped» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        internal void AddSamplesSkipped(long count)
        {
            SamplesSkipped += count;
        }

        /// <summary>
        /// Подробно выполняет операцию «SetParseElapsedMilliseconds» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        internal void SetParseElapsedMilliseconds(long elapsedMilliseconds)
        {
            ParseElapsedMilliseconds = elapsedMilliseconds;
        }
    }
}
