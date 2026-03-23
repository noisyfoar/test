using System;
using System.IO;
using Dlisio.Core.Parsing;
using Xunit;

namespace Dlisio.Tests.Parsing
{
    public sealed class DlisReaderTests
    {
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
