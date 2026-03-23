namespace Lis.Core.Lis
{
    public sealed class LisTapeHeaderRecord
    {
        /// <summary>
        /// Подробно выполняет операцию «LisTapeHeaderRecord» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        public LisTapeHeaderRecord(
            string serviceName,
            string date,
            string originOfData,
            string name,
            string continuationNumber,
            string previousTapeName,
            string comment)
        {
            ServiceName = serviceName;
            Date = date;
            OriginOfData = originOfData;
            Name = name;
            ContinuationNumber = continuationNumber;
            PreviousTapeName = previousTapeName;
            Comment = comment;
        }

        public string ServiceName { get; }

        public string Date { get; }

        public string OriginOfData { get; }

        public string Name { get; }

        public string ContinuationNumber { get; }

        public string PreviousTapeName { get; }

        public string Comment { get; }
    }
}
