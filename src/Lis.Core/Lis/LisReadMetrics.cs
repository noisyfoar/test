namespace Lis.Core.Lis
{
    public sealed class LisReadMetrics
    {
        public long LogicalRecordsRead { get; private set; }

        public long FdataBytesRead { get; private set; }

        public long SamplesDecoded { get; private set; }

        public long SamplesSkipped { get; private set; }

        public long ParseElapsedMilliseconds { get; private set; }

        internal void AddLogicalRecordsRead(long count)
        {
            LogicalRecordsRead += count;
        }

        internal void AddFdataBytesRead(long count)
        {
            FdataBytesRead += count;
        }

        internal void AddSamplesDecoded(long count)
        {
            SamplesDecoded += count;
        }

        internal void AddSamplesSkipped(long count)
        {
            SamplesSkipped += count;
        }

        internal void SetParseElapsedMilliseconds(long elapsedMilliseconds)
        {
            ParseElapsedMilliseconds = elapsedMilliseconds;
        }
    }
}
