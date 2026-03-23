using System;
using Dlisio.Core.Parsing;
using Xunit;

namespace Dlisio.Tests.Parsing
{
    public sealed class LogicalRecordSegmentHeaderParserTests
    {
        [Fact]
        public void Parse_ValidHeader_ReturnsExpectedValues()
        {
            var bytes = new byte[]
            {
                0x00, 0x10, // length = 16
                0xD6,       // 11010110 (explicit, predecessor, encrypted, checksum, trailing length)
                0x7F        // logical record type
            };

            LogicalRecordSegmentHeader header = LogicalRecordSegmentHeaderParser.Parse(bytes);

            Assert.Equal((ushort)16, header.SegmentLength);
            Assert.Equal((byte)0x7F, header.LogicalRecordType);
            Assert.True(header.IsExplicitlyFormatted);
            Assert.False(header.IsFirstSegment);
            Assert.True(header.IsLastSegment);
            Assert.True((header.Attributes & LogicalRecordSegmentAttributes.Encrypted) != 0);
            Assert.True((header.Attributes & LogicalRecordSegmentAttributes.HasChecksum) != 0);
            Assert.True((header.Attributes & LogicalRecordSegmentAttributes.HasTrailingLength) != 0);
        }

        [Fact]
        public void Parse_LengthBelowMinimum_ThrowsDlisParseException()
        {
            var bytes = new byte[]
            {
                0x00, 0x0E, // length = 14
                0x00,
                0x01
            };

            DlisParseException ex = Assert.Throws<DlisParseException>(
                () => LogicalRecordSegmentHeaderParser.Parse(bytes));

            Assert.Contains("at least 16 bytes", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Parse_OddLength_ThrowsDlisParseException()
        {
            var bytes = new byte[]
            {
                0x00, 0x11, // length = 17
                0x00,
                0x01
            };

            DlisParseException ex = Assert.Throws<DlisParseException>(
                () => LogicalRecordSegmentHeaderParser.Parse(bytes));

            Assert.Contains("even", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Parse_EncryptionPacketWithoutEncryption_ThrowsDlisParseException()
        {
            var bytes = new byte[]
            {
                0x00, 0x10, // length = 16
                0x08,       // has encryption packet only
                0x01
            };

            DlisParseException ex = Assert.Throws<DlisParseException>(
                () => LogicalRecordSegmentHeaderParser.Parse(bytes));

            Assert.Contains("encryption packet", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Parse_TooFewBytes_ThrowsDlisParseException()
        {
            var bytes = new byte[] { 0x00, 0x10, 0x00 };

            Assert.Throws<DlisParseException>(() => LogicalRecordSegmentHeaderParser.Parse(bytes));
        }
    }
}
