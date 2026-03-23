using System;
using System.IO;
using Dlisio.Core.Parsing;
using Xunit;

namespace Dlisio.Tests.Parsing
{
    public sealed class DlisReaderTests
    {
        [Fact]
        public void ReadNextLogicalRecord_ReadsUntilLastSegment()
        {
            byte[] first = BuildSegment(0x20, 0x44, 0x10);
            byte[] second = BuildSegment(0x40, 0x44, 0x20);
            byte[] streamData = Concat(first, second);

            using var stream = new MemoryStream(streamData);
            var reader = new DlisReader();

            LogicalRecord record = reader.ReadNextLogicalRecord(stream);

            Assert.Equal((byte)0x44, record.LogicalRecordType);
            Assert.Equal(2, record.Segments.Count);
            Assert.Equal(24, record.Body.Length);
            Assert.Equal((byte)0x10, record.Body[0]);
            Assert.Equal((byte)0x2B, record.Body[23]);
        }

        [Fact]
        public void ReadNextLogicalRecord_StreamStartsFromNonFirstSegment_ThrowsDlisParseException()
        {
            using var stream = new MemoryStream(BuildSegment(0x40, 0x44, 0x20));
            var reader = new DlisReader();

            DlisParseException ex = Assert.Throws<DlisParseException>(
                () => reader.ReadNextLogicalRecord(stream));

            Assert.Contains("not marked as first", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ReadNextLogicalRecord_TruncatedChain_ThrowsDlisParseException()
        {
            using var stream = new MemoryStream(BuildSegment(0x20, 0x44, 0x10));
            var reader = new DlisReader();

            DlisParseException ex = Assert.Throws<DlisParseException>(
                () => reader.ReadNextLogicalRecord(stream));

            Assert.Contains("Unexpected end of stream", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

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

        private static byte[] BuildSegment(byte flags, byte recordType, byte bodyStart)
        {
            var segment = new byte[16];
            segment[0] = 0x00;
            segment[1] = 0x10;
            segment[2] = flags;
            segment[3] = recordType;

            for (int i = 0; i < 12; i++)
            {
                segment[4 + i] = (byte)(bodyStart + i);
            }

            return segment;
        }

        private static byte[] Concat(byte[] first, byte[] second)
        {
            var result = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, result, 0, first.Length);
            Buffer.BlockCopy(second, 0, result, first.Length, second.Length);
            return result;
        }
    }
}
