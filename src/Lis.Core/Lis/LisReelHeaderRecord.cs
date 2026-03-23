namespace Lis.Core.Lis
{
    public sealed class LisReelHeaderRecord
    {
        /// <summary>
        /// Подробно выполняет операцию «LisReelHeaderRecord» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        public LisReelHeaderRecord(
            string serviceName,
            string date,
            string originOfData,
            string name,
            string continuationNumber,
            string previousReelName,
            string comment)
        {
            ServiceName = serviceName;
            Date = date;
            OriginOfData = originOfData;
            Name = name;
            ContinuationNumber = continuationNumber;
            PreviousReelName = previousReelName;
            Comment = comment;
        }

        public string ServiceName { get; }

        public string Date { get; }

        public string OriginOfData { get; }

        public string Name { get; }

        public string ContinuationNumber { get; }

        public string PreviousReelName { get; }

        public string Comment { get; }
    }
}
