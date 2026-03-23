using System;

namespace Dlisio.Core.Parsing
{
    public static class LogicalRecordSegmentHeaderParser
    {
        public const int HeaderLength = 4;
        public const ushort MinimumSegmentLength = 16;

        public static LogicalRecordSegmentHeader Parse(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (data.Length < HeaderLength)
            {
                throw new DlisParseException(
                    "Logical Record Segment Header requires at least 4 bytes.");
            }

            return Parse(data, 0);
        }

        public static LogicalRecordSegmentHeader Parse(byte[] data, int offset)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (offset < 0 || offset > data.Length - HeaderLength)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            ushort segmentLength = ReadUInt16BigEndian(data, offset);
            var attributes = (LogicalRecordSegmentAttributes)data[offset + 2];
            byte logicalRecordType = data[offset + 3];

            ValidateSegmentLength(segmentLength);
            ValidateAttributes(attributes);

            return new LogicalRecordSegmentHeader(
                segmentLength,
                attributes,
                logicalRecordType);
        }

        private static ushort ReadUInt16BigEndian(byte[] data, int offset)
        {
            return (ushort)((data[offset] << 8) | data[offset + 1]);
        }

        private static void ValidateSegmentLength(ushort segmentLength)
        {
            if (segmentLength < MinimumSegmentLength)
            {
                throw new DlisParseException(
                    "Logical Record Segment length must be at least 16 bytes.");
            }

            if ((segmentLength & 1) != 0)
            {
                throw new DlisParseException(
                    "Logical Record Segment length must be an even number.");
            }
        }

        private static void ValidateAttributes(LogicalRecordSegmentAttributes attributes)
        {
            bool hasEncryptionPacket =
                (attributes & LogicalRecordSegmentAttributes.HasEncryptionPacket) != 0;
            bool encrypted =
                (attributes & LogicalRecordSegmentAttributes.Encrypted) != 0;

            if (hasEncryptionPacket && !encrypted)
            {
                throw new DlisParseException(
                    "Invalid attributes: encryption packet bit cannot be set when encryption bit is not set.");
            }
        }
    }
}
