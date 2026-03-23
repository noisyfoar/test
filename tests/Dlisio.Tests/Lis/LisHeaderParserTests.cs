using System;
using Dlisio.Core.Lis;
using Xunit;

namespace Dlisio.Tests.Lis
{
    public sealed class LisHeaderParserTests
    {
        [Fact]
        public void ParsePhysicalRecordHeader_ValidHeader_ParsesBigEndianValues()
        {
            var bytes = new byte[]
            {
                0x00, 0x10, // length 16
                0x00, 0x03  // predecessor + successor
            };

            LisPhysicalRecordHeader header = LisHeaderParser.ParsePhysicalRecordHeader(bytes);

            Assert.Equal((ushort)16, header.Length);
            Assert.Equal((ushort)0x0003, header.Attributes);
            Assert.True(header.HasPredecessor);
            Assert.True(header.HasSuccessor);
            Assert.False(header.HasChecksumTrailer);
        }

        [Fact]
        public void ParsePhysicalRecordHeader_NullInput_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => LisHeaderParser.ParsePhysicalRecordHeader(null!));
        }

        [Fact]
        public void ParsePhysicalRecordHeader_OffsetOutOfRange_ThrowsArgumentOutOfRangeException()
        {
            var bytes = new byte[] { 0x00, 0x10, 0x00, 0x00 };

            Assert.Throws<ArgumentOutOfRangeException>(
                () => LisHeaderParser.ParsePhysicalRecordHeader(bytes, -1));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => LisHeaderParser.ParsePhysicalRecordHeader(bytes, 1));
        }

        [Fact]
        public void ParsePhysicalRecordHeader_LengthBelowMinimum_ThrowsLisParseException()
        {
            var bytes = new byte[]
            {
                0x00, 0x05, // less than minimum for first PR (needs at least 6)
                0x00, 0x00
            };

            LisParseException ex = Assert.Throws<LisParseException>(
                () => LisHeaderParser.ParsePhysicalRecordHeader(bytes));

            Assert.Contains("length", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ParsePhysicalRecordHeader_TrailerBitsAffectMinimumLength()
        {
            var bytes = new byte[]
            {
                0x00, 0x0C, // 12
                0x36, 0x00  // checksum + file number + record number (6 bytes trailer)
            };

            LisPhysicalRecordHeader header = LisHeaderParser.ParsePhysicalRecordHeader(bytes);

            Assert.Equal(6, header.TrailerLength);
            Assert.True(header.HasChecksumTrailer);
            Assert.True(header.HasFileNumberTrailer);
            Assert.True(header.HasRecordNumberTrailer);
            Assert.Equal(12, header.MinimumValidLength);
        }

        [Fact]
        public void ParseLogicalRecordHeader_ValidType_ParsesSuccessfully()
        {
            var bytes = new byte[] { 0x80, 0x11 };

            LisLogicalRecordHeader header = LisHeaderParser.ParseLogicalRecordHeader(bytes);

            Assert.Equal((byte)0x80, header.Type);
            Assert.Equal((byte)0x11, header.Attributes);
            Assert.True(header.IsKnownRecordType);
        }

        [Fact]
        public void ParseLogicalRecordHeader_InvalidType_ThrowsLisParseException()
        {
            var bytes = new byte[] { 0xAA, 0x00 };

            LisParseException ex = Assert.Throws<LisParseException>(
                () => LisHeaderParser.ParseLogicalRecordHeader(bytes));

            Assert.Contains("record type", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ParseLogicalRecordHeader_OffsetVersion_ParsesFromOffset()
        {
            var bytes = new byte[] { 0xFF, 0xFF, 0x20, 0x01 };

            LisLogicalRecordHeader header = LisHeaderParser.ParseLogicalRecordHeader(bytes, 2);

            Assert.Equal((byte)0x20, header.Type);
            Assert.Equal((byte)0x01, header.Attributes);
        }

        [Fact]
        public void IsPadBytes_ReturnsTrueForNullPadBuffer()
        {
            var bytes = new byte[] { 0x00, 0x00, 0x00, 0x00 };
            Assert.True(LisHeaderParser.IsPadBytes(bytes, 0, 4));
        }

        [Fact]
        public void IsPadBytes_ReturnsTrueForSpacePadBuffer()
        {
            var bytes = new byte[] { 0x20, 0x20, 0x20, 0x20 };
            Assert.True(LisHeaderParser.IsPadBytes(bytes, 0, 4));
        }

        [Fact]
        public void IsPadBytes_ReturnsFalseForMixedValues()
        {
            var bytes = new byte[] { 0x20, 0x20, 0x00, 0x20 };
            Assert.False(LisHeaderParser.IsPadBytes(bytes, 0, 4));
        }

        [Fact]
        public void IsPadBytes_ReturnsFalseWhenCountIsZero()
        {
            var bytes = new byte[] { 0x00, 0x00 };
            Assert.False(LisHeaderParser.IsPadBytes(bytes, 0, 0));
        }
    }
}
