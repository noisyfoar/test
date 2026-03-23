namespace Lis.Core.Lis
{
    public sealed class LisTapeHeaderRecord
    {
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
