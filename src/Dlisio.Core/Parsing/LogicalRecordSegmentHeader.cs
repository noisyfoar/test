namespace Dlisio.Core.Parsing
{
    public sealed class LogicalRecordSegmentHeader
    {
        public LogicalRecordSegmentHeader(
            ushort segmentLength,
            LogicalRecordSegmentAttributes attributes,
            byte logicalRecordType)
        {
            SegmentLength = segmentLength;
            Attributes = attributes;
            LogicalRecordType = logicalRecordType;
        }

        public ushort SegmentLength { get; }

        public LogicalRecordSegmentAttributes Attributes { get; }

        public byte LogicalRecordType { get; }

        public bool IsExplicitlyFormatted
        {
            get { return (Attributes & LogicalRecordSegmentAttributes.ExplicitlyFormatted) != 0; }
        }

        public bool IsFirstSegment
        {
            get { return (Attributes & LogicalRecordSegmentAttributes.HasPredecessor) == 0; }
        }

        public bool IsLastSegment
        {
            get { return (Attributes & LogicalRecordSegmentAttributes.HasSuccessor) == 0; }
        }

        public bool IsEncrypted
        {
            get { return (Attributes & LogicalRecordSegmentAttributes.Encrypted) != 0; }
        }

        public bool HasEncryptionPacket
        {
            get { return (Attributes & LogicalRecordSegmentAttributes.HasEncryptionPacket) != 0; }
        }

        public bool HasChecksum
        {
            get { return (Attributes & LogicalRecordSegmentAttributes.HasChecksum) != 0; }
        }

        public bool HasTrailingLength
        {
            get { return (Attributes & LogicalRecordSegmentAttributes.HasTrailingLength) != 0; }
        }

        public bool HasPadding
        {
            get { return (Attributes & LogicalRecordSegmentAttributes.HasPadding) != 0; }
        }
    }
}
