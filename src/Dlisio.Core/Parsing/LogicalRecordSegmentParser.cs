using System;

namespace Dlisio.Core.Parsing
{
    public static class LogicalRecordSegmentParser
    {
        public static LogicalRecordSegment Parse(byte[] segmentData)
        {
            if (segmentData == null)
            {
                throw new ArgumentNullException(nameof(segmentData));
            }

            LogicalRecordSegmentHeader header = LogicalRecordSegmentHeaderParser.Parse(segmentData);
            if (segmentData.Length != header.SegmentLength)
            {
                throw new DlisParseException(
                    "Segment data length does not match LRSH segment length.");
            }

            int payloadOffset = LogicalRecordSegmentHeaderParser.HeaderLength;
            byte[] encryptionPacket = Array.Empty<byte>();

            if (header.HasEncryptionPacket)
            {
                if (payloadOffset + 2 > segmentData.Length)
                {
                    throw new DlisParseException(
                        "Invalid segment: not enough bytes to read encryption packet size.");
                }

                ushort encryptionPacketLength = ReadUInt16BigEndian(segmentData, payloadOffset);
                if (encryptionPacketLength < 4 || (encryptionPacketLength & 1) != 0)
                {
                    throw new DlisParseException(
                        "Invalid encryption packet length: value must be even and at least 4.");
                }

                int packetEnd = payloadOffset + encryptionPacketLength;
                if (packetEnd > segmentData.Length)
                {
                    throw new DlisParseException(
                        "Invalid segment: encryption packet exceeds segment boundary.");
                }

                encryptionPacket = new byte[encryptionPacketLength];
                Buffer.BlockCopy(segmentData, payloadOffset, encryptionPacket, 0, encryptionPacket.Length);
                payloadOffset = packetEnd;
            }

            LogicalRecordSegmentTrailer trailer =
                LogicalRecordSegmentTrailerParser.Parse(segmentData, header, payloadOffset);

            int bodyLength = segmentData.Length - payloadOffset - trailer.TrailerLength;
            if (bodyLength < 0)
            {
                throw new DlisParseException(
                    "Invalid segment: trailer overlaps the segment payload.");
            }

            byte[] body = new byte[bodyLength];
            if (bodyLength > 0)
            {
                Buffer.BlockCopy(segmentData, payloadOffset, body, 0, bodyLength);
            }

            return new LogicalRecordSegment(header, encryptionPacket, body, trailer);
        }

        private static ushort ReadUInt16BigEndian(byte[] data, int offset)
        {
            return (ushort)((data[offset] << 8) | data[offset + 1]);
        }
    }
}
