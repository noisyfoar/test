namespace Lis.Core.Lis
{
    public sealed class LisTapeTrailerRecord
    {
        /// <summary>
        /// Подробно выполняет операцию «LisTapeTrailerRecord» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        public LisTapeTrailerRecord(
            string serviceName,
            string date,
            string originOfData,
            string name,
            string continuationNumber,
            string nextTapeName,
            string comment)
        {
            ServiceName = serviceName;
            Date = date;
            OriginOfData = originOfData;
            Name = name;
            ContinuationNumber = continuationNumber;
            NextTapeName = nextTapeName;
            Comment = comment;
        }

        public string ServiceName { get; }

        public string Date { get; }

        public string OriginOfData { get; }

        public string Name { get; }

        public string ContinuationNumber { get; }

        public string NextTapeName { get; }

        public string Comment { get; }
    }
}
