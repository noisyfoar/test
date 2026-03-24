namespace Lis.Core.Lis
{
    public sealed class LisReelTrailerRecord
    {
        /// <summary>
        /// Подробно выполняет операцию «LisReelTrailerRecord» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        public LisReelTrailerRecord(
            string serviceName,
            string date,
            string originOfData,
            string name,
            string continuationNumber,
            string nextReelName,
            string comment)
        {
            ServiceName = serviceName;
            Date = date;
            OriginOfData = originOfData;
            Name = name;
            ContinuationNumber = continuationNumber;
            NextReelName = nextReelName;
            Comment = comment;
        }

        public string ServiceName { get; }

        public string Date { get; }

        public string OriginOfData { get; }

        public string Name { get; }

        public string ContinuationNumber { get; }

        public string NextReelName { get; }

        public string Comment { get; }
    }
}
