namespace Lis.Core.Lis
{
    /// <summary>
    /// Mutable runtime counters populated during LIS parsing.
    /// </summary>
    public sealed class LisReadMetrics
    {
        /// <summary>
        /// Number of logical records decoded from stream.
        /// </summary>
        public long LogicalRecordsRead { get; private set; }

        /// <summary>
        /// Total byte size of FData payloads consumed.
        /// </summary>
        public long FdataBytesRead { get; private set; }

        /// <summary>
        /// Number of samples decoded and materialized.
        /// </summary>
        public long SamplesDecoded { get; private set; }

        /// <summary>
        /// Number of samples skipped because of channel filtering.
        /// </summary>
        public long SamplesSkipped { get; private set; }

        /// <summary>
        /// End-to-end parse time measured at <see cref="LisFileParser"/> level.
        /// </summary>
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
