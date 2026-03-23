using System;
using System.IO;
using Dlisio.Core.Parsing;
using Xunit;

namespace Dlisio.Tests.Parsing
{
    public sealed class DlisReaderTests
    {
        [Fact]
        public void ReadNextSegment_ReadsFullSegment()
        {
            var segment = new byte[]
            {
                0x00, 0x14, // length = 20
                0x06,       // checksum + trailing length
                0x22,
                0x01, 0x02, 0x03, 0x04,
                0x05, 0x06, 0x07, 0x08,
                0x09, 0x0A, 0x0B, 0x0C,
                0x12, 0x34,
                0x00, 0x14
            };

            using var stream = new MemoryStream(segment);
            var reader = new DlisReader();

            LogicalRecordSegment parsed = reader.ReadNextSegment(stream);

            Assert.Equal((ushort)20, parsed.Header.SegmentLength);
            Assert.Equal((byte)0x22, parsed.Header.LogicalRecordType);
            Assert.Equal(12, parsed.Body.Length);
            Assert.Equal((ushort)0x1234, parsed.Trailer.Checksum);
            Assert.Equal((ushort)20, parsed.Trailer.TrailingLength);
        }

        [Fact]
        public void ReadNextSegment_UnexpectedEnd_ThrowsDlisParseException()
        {
            var segment = new byte[]
            {
                0x00, 0x10,
                0x00,
                0x22,
                0x01, 0x02, 0x03
            };

            using var stream = new MemoryStream(segment);
            var reader = new DlisReader();

            DlisParseException ex = Assert.Throws<DlisParseException>(
                () => reader.ReadNextSegment(stream));

            Assert.Contains("Unexpected end of stream", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ReadFirstSegmentHeader_ReadsFromStream()
        {
            var bytes = new byte[]
            {
                0x00, 0x10,
                0x00,
                0x22
            };

            using var stream = new MemoryStream(bytes);
            var reader = new DlisReader();

            LogicalRecordSegmentHeader header = reader.ReadFirstSegmentHeader(stream);

            Assert.Equal((ushort)16, header.SegmentLength);
            Assert.Equal((byte)0x22, header.LogicalRecordType);
            Assert.True(header.IsFirstSegment);
            Assert.True(header.IsLastSegment);
        }

        [Fact]
        public void ReadFirstSegmentHeader_UnexpectedEnd_ThrowsDlisParseException()
        {
            using var stream = new MemoryStream(new byte[] { 0x00, 0x10, 0x00 });
            var reader = new DlisReader();

            DlisParseException ex = Assert.Throws<DlisParseException>(
                () => reader.ReadFirstSegmentHeader(stream));

            Assert.Contains("Unexpected end of stream", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }
}
