namespace Dlisio.Core.Parsing
{
    public sealed class LogicalRecordSegment
    {
        public LogicalRecordSegment(
            LogicalRecordSegmentHeader header,
            byte[] encryptionPacket,
            byte[] body,
            LogicalRecordSegmentTrailer trailer)
        {
            Header = header;
            EncryptionPacket = encryptionPacket;
            Body = body;
            Trailer = trailer;
        }

        public LogicalRecordSegmentHeader Header { get; }

        public byte[] EncryptionPacket { get; }

        public byte[] Body { get; }

        public LogicalRecordSegmentTrailer Trailer { get; }
    }
}
