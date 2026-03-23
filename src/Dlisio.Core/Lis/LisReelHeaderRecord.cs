namespace Dlisio.Core.Lis
{
    public sealed class LisReelHeaderRecord
    {
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
