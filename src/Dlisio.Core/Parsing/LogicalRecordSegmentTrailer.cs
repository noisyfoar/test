namespace Dlisio.Core.Parsing
{
    public sealed class LogicalRecordSegmentTrailer
    {
        public LogicalRecordSegmentTrailer(
            byte[] paddingBytes,
            byte? padCount,
            ushort? checksum,
            ushort? trailingLength,
            int trailerLength)
        {
            PaddingBytes = paddingBytes;
            PadCount = padCount;
            Checksum = checksum;
            TrailingLength = trailingLength;
            TrailerLength = trailerLength;
        }

        public byte[] PaddingBytes { get; }

        public byte? PadCount { get; }

        public ushort? Checksum { get; }

        public ushort? TrailingLength { get; }

        public int TrailerLength { get; }
    }
}
