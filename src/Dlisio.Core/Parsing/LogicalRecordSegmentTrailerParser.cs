using System;

namespace Dlisio.Core.Parsing
{
    public static class LogicalRecordSegmentTrailerParser
    {
        public static LogicalRecordSegmentTrailer Parse(
            byte[] segmentData,
            LogicalRecordSegmentHeader header,
            int payloadOffset)
        {
            if (segmentData == null)
            {
                throw new ArgumentNullException(nameof(segmentData));
            }

            if (header == null)
            {
                throw new ArgumentNullException(nameof(header));
            }

            if (segmentData.Length != header.SegmentLength)
            {
                throw new DlisParseException(
                    "Segment data length does not match the segment length from LRSH.");
            }

            if (payloadOffset < LogicalRecordSegmentHeaderParser.HeaderLength || payloadOffset > segmentData.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(payloadOffset));
            }

            int cursor = segmentData.Length;

            ushort? trailingLength = null;
            if (header.HasTrailingLength)
            {
                EnsureAvailableBytes(cursor, payloadOffset, 2, "trailing length");
                trailingLength = ReadUInt16BigEndian(segmentData, cursor - 2);
                cursor -= 2;
            }

            ushort? checksum = null;
            if (header.HasChecksum)
            {
                EnsureAvailableBytes(cursor, payloadOffset, 2, "checksum");
                checksum = ReadUInt16BigEndian(segmentData, cursor - 2);
                cursor -= 2;
            }

            byte? padCount = null;
            byte[] paddingBytes = Array.Empty<byte>();
            if (header.HasPadding)
            {
                EnsureAvailableBytes(cursor, payloadOffset, 1, "pad count");

                byte value = segmentData[cursor - 1];
                if (value == 0)
                {
                    throw new DlisParseException(
                        "Invalid trailer: pad count must be at least 1 when padding bit is set.");
                }

                int paddingLength = value;
                int paddingStart = cursor - paddingLength;
                if (paddingStart < payloadOffset)
                {
                    throw new DlisParseException(
                        "Invalid trailer: padding overlaps segment payload.");
                }

                padCount = value;
                if (paddingLength > 1)
                {
                    paddingBytes = new byte[paddingLength - 1];
                    Buffer.BlockCopy(segmentData, paddingStart, paddingBytes, 0, paddingBytes.Length);
                }

                cursor = paddingStart;
            }

            int trailerLength = segmentData.Length - cursor;
            if (trailingLength.HasValue && trailingLength.Value != header.SegmentLength)
            {
                throw new DlisParseException(
                    "Invalid trailer: trailing length does not match LRSH segment length.");
            }

            return new LogicalRecordSegmentTrailer(
                paddingBytes,
                padCount,
                checksum,
                trailingLength,
                trailerLength);
        }

        private static void EnsureAvailableBytes(
            int cursor,
            int payloadOffset,
            int requiredLength,
            string component)
        {
            if (cursor - requiredLength < payloadOffset)
            {
                throw new DlisParseException(
                    "Invalid trailer: not enough bytes for " + component + ".");
            }
        }

        private static ushort ReadUInt16BigEndian(byte[] data, int offset)
        {
            return (ushort)((data[offset] << 8) | data[offset + 1]);
        }
    }
}
