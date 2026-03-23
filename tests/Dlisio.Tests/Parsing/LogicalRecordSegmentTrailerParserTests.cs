using System;
using Dlisio.Core.Parsing;
using Xunit;

namespace Dlisio.Tests.Parsing
{
    public sealed class LogicalRecordSegmentTrailerParserTests
    {
        [Fact]
        public void Parse_NullSegmentData_ThrowsArgumentNullException()
        {
            LogicalRecordSegmentHeader header = LogicalRecordSegmentHeaderParser.Parse(
                new byte[] { 0x00, 0x10, 0x00, 0x01 });

            Assert.Throws<ArgumentNullException>(
                () => LogicalRecordSegmentTrailerParser.Parse(null!, header, 4));
        }

        [Fact]
        public void Parse_NullHeader_ThrowsArgumentNullException()
        {
            var segment = new byte[]
            {
                0x00, 0x10,
                0x00,
                0x01,
                0x10, 0x11, 0x12, 0x13, 0x14, 0x15,
                0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B
            };

            Assert.Throws<ArgumentNullException>(
                () => LogicalRecordSegmentTrailerParser.Parse(segment, null!, 4));
        }

        [Fact]
        public void Parse_ChecksumAndTrailingLength_ReturnsExpectedValues()
        {
            var segment = new byte[]
            {
                0x00, 0x14, // length = 20
                0x06,       // checksum + trailing length
                0x01,       // logical record type

                0x10, 0x11, 0x12, 0x13, 0x14, 0x15,
                0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, // body (12)

                0x12, 0x34, // checksum
                0x00, 0x14  // trailing length = 20
            };

            LogicalRecordSegmentHeader header = LogicalRecordSegmentHeaderParser.Parse(segment);

            LogicalRecordSegmentTrailer trailer = LogicalRecordSegmentTrailerParser.Parse(segment, header, 4);

            Assert.Equal((ushort)0x1234, trailer.Checksum);
            Assert.Equal((ushort)20, trailer.TrailingLength);
            Assert.Null(trailer.PadCount);
            Assert.Empty(trailer.PaddingBytes);
            Assert.Equal(4, trailer.TrailerLength);
        }

        [Fact]
        public void Parse_PaddingChecksumAndTrailingLength_ReturnsPadData()
        {
            var segment = new byte[]
            {
                0x00, 0x18, // length = 24
                0x07,       // padding + checksum + trailing length
                0x02,

                0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27,
                0x28, 0x29, 0x2A, 0x2B, 0x2C, 0x2D, // body (13)

                0xAA, 0xBB, // pad bytes
                0x03,       // pad count (includes itself)
                0xBE, 0xEF, // checksum
                0x00, 0x18  // trailing length = 24
            };

            LogicalRecordSegmentHeader header = LogicalRecordSegmentHeaderParser.Parse(segment);

            LogicalRecordSegmentTrailer trailer = LogicalRecordSegmentTrailerParser.Parse(segment, header, 4);

            Assert.Equal((byte)3, trailer.PadCount);
            Assert.Equal(new byte[] { 0xAA, 0xBB }, trailer.PaddingBytes);
            Assert.Equal((ushort)0xBEEF, trailer.Checksum);
            Assert.Equal((ushort)24, trailer.TrailingLength);
            Assert.Equal(7, trailer.TrailerLength);
        }

        [Fact]
        public void Parse_TrailingLengthMismatch_ThrowsDlisParseException()
        {
            var segment = new byte[]
            {
                0x00, 0x14,
                0x06,
                0x01,

                0x10, 0x11, 0x12, 0x13, 0x14, 0x15,
                0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B,

                0x12, 0x34,
                0x00, 0x16 // mismatch
            };

            LogicalRecordSegmentHeader header = LogicalRecordSegmentHeaderParser.Parse(segment);

            DlisParseException ex = Assert.Throws<DlisParseException>(
                () => LogicalRecordSegmentTrailerParser.Parse(segment, header, 4));

            Assert.Contains("trailing length", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Parse_ZeroPadCount_ThrowsDlisParseException()
        {
            var segment = new byte[]
            {
                0x00, 0x10, // length = 16
                0x01,       // padding only
                0x01,

                0x30, 0x31, 0x32, 0x33, 0x34, 0x35,
                0x36, 0x37, 0x38, 0x39, 0x3A, // body (11)

                0x00 // invalid pad count
            };

            LogicalRecordSegmentHeader header = LogicalRecordSegmentHeaderParser.Parse(segment);

            DlisParseException ex = Assert.Throws<DlisParseException>(
                () => LogicalRecordSegmentTrailerParser.Parse(segment, header, 4));

            Assert.Contains("pad count", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Parse_PaddingOverlapsPayload_ThrowsDlisParseException()
        {
            var segment = new byte[]
            {
                0x00, 0x10, // length = 16
                0x01,       // padding only
                0x01,

                0x40, 0x41, 0x42, 0x43, 0x44, 0x45,
                0x46, 0x47, 0x48, 0x49, 0x4A, // body (11)

                0x07 // invalid pad count
            };

            LogicalRecordSegmentHeader header = LogicalRecordSegmentHeaderParser.Parse(segment);

            DlisParseException ex = Assert.Throws<DlisParseException>(
                () => LogicalRecordSegmentTrailerParser.Parse(segment, header, 12));

            Assert.Contains("overlaps", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Parse_SegmentLengthMismatch_ThrowsDlisParseException()
        {
            LogicalRecordSegmentHeader header = LogicalRecordSegmentHeaderParser.Parse(
                new byte[] { 0x00, 0x10, 0x00, 0x01 });

            var segment = new byte[18];
            segment[0] = 0x00;
            segment[1] = 0x10;
            segment[2] = 0x00;
            segment[3] = 0x01;

            DlisParseException ex = Assert.Throws<DlisParseException>(
                () => LogicalRecordSegmentTrailerParser.Parse(segment, header, 4));

            Assert.Contains("does not match", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Parse_PayloadOffsetOutOfRange_ThrowsArgumentOutOfRangeException()
        {
            var segment = new byte[]
            {
                0x00, 0x10,
                0x00,
                0x01,
                0x10, 0x11, 0x12, 0x13, 0x14, 0x15,
                0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B
            };

            LogicalRecordSegmentHeader header = LogicalRecordSegmentHeaderParser.Parse(segment);

            Assert.Throws<ArgumentOutOfRangeException>(
                () => LogicalRecordSegmentTrailerParser.Parse(segment, header, 3));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => LogicalRecordSegmentTrailerParser.Parse(segment, header, 17));
        }

        [Fact]
        public void Parse_NotEnoughBytesForTrailingLength_ThrowsDlisParseException()
        {
            var segment = new byte[]
            {
                0x00, 0x10, // length = 16
                0x02,       // trailing length only
                0x01,
                0x50, 0x51, 0x52, 0x53, 0x54, 0x55,
                0x56, 0x57, 0x58, 0x59, 0x5A, 0x5B
            };

            LogicalRecordSegmentHeader header = LogicalRecordSegmentHeaderParser.Parse(segment);

            DlisParseException ex = Assert.Throws<DlisParseException>(
                () => LogicalRecordSegmentTrailerParser.Parse(segment, header, 15));

            Assert.Contains("not enough bytes", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Parse_PadCountOne_ReturnsNoPaddingBytes()
        {
            var segment = new byte[]
            {
                0x00, 0x10, // length = 16
                0x01,       // padding only
                0x01,
                0x60, 0x61, 0x62, 0x63, 0x64, 0x65,
                0x66, 0x67, 0x68, 0x69, 0x6A,
                0x01        // pad count = 1 (only the counter byte)
            };

            LogicalRecordSegmentHeader header = LogicalRecordSegmentHeaderParser.Parse(segment);

            LogicalRecordSegmentTrailer trailer = LogicalRecordSegmentTrailerParser.Parse(segment, header, 4);

            Assert.Equal((byte)1, trailer.PadCount);
            Assert.Empty(trailer.PaddingBytes);
            Assert.Equal(1, trailer.TrailerLength);
        }
    }
}
