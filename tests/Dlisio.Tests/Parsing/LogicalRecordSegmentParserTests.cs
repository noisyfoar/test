using System;
using Dlisio.Core.Parsing;
using Xunit;

namespace Dlisio.Tests.Parsing
{
    public sealed class LogicalRecordSegmentParserTests
    {
        [Fact]
        public void Parse_NullInput_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => LogicalRecordSegmentParser.Parse(null!));
        }

        [Fact]
        public void Parse_NoTrailerAndNoEncryption_ReturnsBodyOnlySegment()
        {
            var segment = new byte[]
            {
                0x00, 0x10, // length = 16
                0x00,       // no trailer flags
                0x08,
                0x01, 0x02, 0x03, 0x04,
                0x05, 0x06, 0x07, 0x08,
                0x09, 0x0A, 0x0B, 0x0C
            };

            LogicalRecordSegment parsed = LogicalRecordSegmentParser.Parse(segment);

            Assert.Equal((ushort)16, parsed.Header.SegmentLength);
            Assert.Empty(parsed.EncryptionPacket);
            Assert.Equal(12, parsed.Body.Length);
            Assert.Equal(0, parsed.Trailer.TrailerLength);
            Assert.Null(parsed.Trailer.Checksum);
            Assert.Null(parsed.Trailer.TrailingLength);
        }

        [Fact]
        public void Parse_WithEncryptionPacketAndTrailer_ReturnsExpectedParts()
        {
            var segment = new byte[]
            {
                0x00, 0x18, // length = 24
                0x1F,       // encrypted + packet + padding + checksum + trailing length
                0x09,

                0x00, 0x04, // encryption packet length
                0x12, 0x34, // company code

                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, // body (9)

                0xAA, 0xBB, // pad bytes
                0x03,       // pad count
                0xCA, 0xFE, // checksum
                0x00, 0x18  // trailing length
            };

            LogicalRecordSegment parsed = LogicalRecordSegmentParser.Parse(segment);

            Assert.Equal(4, parsed.EncryptionPacket.Length);
            Assert.Equal(new byte[] { 0x00, 0x04, 0x12, 0x34 }, parsed.EncryptionPacket);
            Assert.Equal(9, parsed.Body.Length);
            Assert.Equal((byte)3, parsed.Trailer.PadCount);
            Assert.Equal(new byte[] { 0xAA, 0xBB }, parsed.Trailer.PaddingBytes);
            Assert.Equal((ushort)0xCAFE, parsed.Trailer.Checksum);
            Assert.Equal((ushort)24, parsed.Trailer.TrailingLength);
        }

        [Fact]
        public void Parse_InvalidEncryptionPacketLength_ThrowsDlisParseException()
        {
            var segment = new byte[]
            {
                0x00, 0x10, // length = 16
                0x18,       // encrypted + has encryption packet
                0x01,

                0x00, 0x03, // invalid packet length
                0x99, 0x88,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66,
                0x77, 0x88
            };

            DlisParseException ex = Assert.Throws<DlisParseException>(
                () => LogicalRecordSegmentParser.Parse(segment));

            Assert.Contains("encryption packet length", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Parse_LengthMismatchAgainstHeader_ThrowsDlisParseException()
        {
            var segment = new byte[18];
            segment[0] = 0x00;
            segment[1] = 0x10; // header says 16
            segment[2] = 0x00;
            segment[3] = 0x01;

            DlisParseException ex = Assert.Throws<DlisParseException>(
                () => LogicalRecordSegmentParser.Parse(segment));

            Assert.Contains("does not match", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Parse_EncryptionPacketExceedsBoundary_ThrowsDlisParseException()
        {
            var segment = new byte[]
            {
                0x00, 0x10, // length = 16
                0x18,       // encrypted + has encryption packet
                0x01,
                0x00, 0x0E, // packet length = 14 => exceeds (4 + 14 > 16)
                0xAA, 0xBB, 0xCC, 0xDD,
                0x01, 0x02, 0x03, 0x04,
                0x05, 0x06
            };

            DlisParseException ex = Assert.Throws<DlisParseException>(
                () => LogicalRecordSegmentParser.Parse(segment));

            Assert.Contains("exceeds", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Parse_EncryptionPacketConsumesRemainingPayload_ReturnsEmptyBody()
        {
            var segment = new byte[]
            {
                0x00, 0x10, // length = 16
                0x18,       // encrypted + packet
                0x77,
                0x00, 0x0C, // packet length = 12
                0x12, 0x34,
                0x41, 0x42, 0x43, 0x44,
                0x45, 0x46, 0x47, 0x48
            };

            LogicalRecordSegment parsed = LogicalRecordSegmentParser.Parse(segment);

            Assert.Equal(12, parsed.EncryptionPacket.Length);
            Assert.Empty(parsed.Body);
            Assert.Equal(0, parsed.Trailer.TrailerLength);
        }
    }
}
