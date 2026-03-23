using System;
using System.IO;
using Dlisio.Core.Lis;
using Xunit;

namespace Dlisio.Tests.Lis
{
    public sealed class LisReaderTests
    {
        [Fact]
        public void ReadNextPhysicalRecordHeader_ReadsNextHeader()
        {
            byte[] record = BuildPhysicalRecord(0x0000, BuildLrhPayload((byte)LisRecordType.FileHeader, 0x00));
            using var stream = new MemoryStream(record);
            var reader = new LisReader();

            LisPhysicalRecordHeader header = reader.ReadNextPhysicalRecordHeader(stream);

            Assert.Equal((ushort)6, header.Length);
            Assert.Equal((ushort)0x0000, header.Attributes);
        }

        [Fact]
        public void ReadNextPhysicalRecordHeader_SkipsZeroPadBlock()
        {
            byte[] pad = new byte[] { 0x00, 0x00, 0x00, 0x00 };
            byte[] record = BuildPhysicalRecord(0x0000, BuildLrhPayload((byte)LisRecordType.FileHeader, 0x00));
            byte[] bytes = Concat(pad, record);

            using var stream = new MemoryStream(bytes);
            var reader = new LisReader();

            LisPhysicalRecordHeader header = reader.ReadNextPhysicalRecordHeader(stream);

            Assert.Equal((ushort)6, header.Length);
            Assert.Equal((ushort)0x0000, header.Attributes);
        }

        [Fact]
        public void ReadNextPhysicalRecordHeader_NullStream_ThrowsArgumentNullException()
        {
            var reader = new LisReader();
            Assert.Throws<ArgumentNullException>(() => reader.ReadNextPhysicalRecordHeader(null!));
        }

        [Fact]
        public void ReadNextPhysicalRecordHeader_UnreadableStream_ThrowsArgumentException()
        {
            var reader = new LisReader();
            using var stream = new NonReadableStream();

            Assert.Throws<ArgumentException>(() => reader.ReadNextPhysicalRecordHeader(stream));
        }

        [Fact]
        public void TryReadNextLogicalRecord_AtEof_ReturnsFalse()
        {
            using var stream = new MemoryStream(Array.Empty<byte>());
            var reader = new LisReader();

            bool ok = reader.TryReadNextLogicalRecord(stream, out LisLogicalRecord? record);

            Assert.False(ok);
            Assert.Null(record);
        }

        [Fact]
        public void TryReadNextLogicalRecord_WithTrailingPadding_ReturnsFalseAtEnd()
        {
            byte[] recordBytes = BuildPhysicalRecord(
                0x0000,
                BuildLrhPayload((byte)LisRecordType.FileHeader, 0x00, 0x55));
            byte[] trailingPad = new byte[] { 0x20, 0x20, 0x20, 0x20, 0x20 };

            using var stream = new MemoryStream(Concat(recordBytes, trailingPad));
            var reader = new LisReader();

            bool first = reader.TryReadNextLogicalRecord(stream, out LisLogicalRecord? firstRecord);
            bool second = reader.TryReadNextLogicalRecord(stream, out LisLogicalRecord? secondRecord);

            Assert.True(first);
            Assert.NotNull(firstRecord);
            Assert.False(second);
            Assert.Null(secondRecord);
        }

        [Fact]
        public void TryReadNextLogicalRecord_UnseekableReadableStream_ThrowsArgumentException()
        {
            byte[] recordBytes = BuildPhysicalRecord(
                0x0000,
                BuildLrhPayload((byte)LisRecordType.FileHeader, 0x00));
            using var stream = new ReadOnlyNonSeekableStream(recordBytes);
            var reader = new LisReader();

            Assert.Throws<ArgumentException>(() => reader.TryReadNextLogicalRecord(stream, out _));
        }

        [Fact]
        public void TrySkipNextLogicalRecord_ReadsRecordInfoWithoutMaterializingPayload()
        {
            byte[] first = BuildPhysicalRecord(
                0x0001,
                BuildLrhPayload((byte)LisRecordType.DataFormatSpecification, 0x01, 0x21, 0x22));
            byte[] second = BuildPhysicalRecord(
                0x0002,
                new byte[] { 0x23, 0x24, 0x25 });
            using var stream = new MemoryStream(Concat(first, second));
            var reader = new LisReader();

            bool ok = reader.TrySkipNextLogicalRecord(stream, out LisRecordInfo? info);

            Assert.True(ok);
            Assert.NotNull(info);
            Assert.Equal(0L, info!.Offset);
            Assert.Equal(LisRecordType.DataFormatSpecification, info.Type);
            Assert.Equal(2, info.PhysicalRecordCount);
            Assert.Equal(5, info.DataLength);
            Assert.Equal(stream.Length, stream.Position);
        }

        [Fact]
        public void TrySkipNextLogicalRecord_AtEof_ReturnsFalse()
        {
            using var stream = new MemoryStream(Array.Empty<byte>());
            var reader = new LisReader();

            bool ok = reader.TrySkipNextLogicalRecord(stream, out LisRecordInfo? info);

            Assert.False(ok);
            Assert.Null(info);
        }

        [Fact]
        public void ReadNextLogicalRecord_SinglePhysicalRecord_ParsesPayload()
        {
            byte[] payload = BuildLrhPayload((byte)LisRecordType.FileHeader, 0x01, 0x10, 0x11, 0x12);
            byte[] record = BuildPhysicalRecord(0x0000, payload);

            using var stream = new MemoryStream(record);
            var reader = new LisReader();

            LisLogicalRecord lr = reader.ReadNextLogicalRecord(stream);

            Assert.Equal((byte)LisRecordType.FileHeader, lr.Header.Type);
            Assert.Equal((byte)0x01, lr.Header.Attributes);
            Assert.Equal(1, lr.PhysicalRecordCount);
            Assert.Equal(new byte[] { 0x10, 0x11, 0x12 }, lr.Data);
        }

        [Fact]
        public void ReadNextLogicalRecord_MultiPhysicalRecord_StitchesData()
        {
            byte[] firstPayload = BuildLrhPayload((byte)LisRecordType.DataFormatSpecification, 0x00, 0x21, 0x22);
            byte[] secondPayload = new byte[] { 0x23, 0x24, 0x25 };

            byte[] first = BuildPhysicalRecord(0x0001, firstPayload);   // successor
            byte[] second = BuildPhysicalRecord(0x0002, secondPayload); // predecessor

            using var stream = new MemoryStream(Concat(first, second));
            var reader = new LisReader();

            LisLogicalRecord lr = reader.ReadNextLogicalRecord(stream);

            Assert.Equal((byte)LisRecordType.DataFormatSpecification, lr.Header.Type);
            Assert.Equal(2, lr.PhysicalRecordCount);
            Assert.Equal(new byte[] { 0x21, 0x22, 0x23, 0x24, 0x25 }, lr.Data);
        }

        [Fact]
        public void ReadNextLogicalRecord_WithTrailer_ConsumesTrailerBytes()
        {
            byte[] payload = BuildLrhPayload((byte)LisRecordType.FileTrailer, 0x00, 0x31, 0x32);
            byte[] trailer = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD };
            byte[] record = BuildPhysicalRecord(0x0600, payload, trailer); // file+record trailer

            using var stream = new MemoryStream(record);
            var reader = new LisReader();

            LisLogicalRecord lr = reader.ReadNextLogicalRecord(stream);

            Assert.Equal((byte)LisRecordType.FileTrailer, lr.Header.Type);
            Assert.Equal(new byte[] { 0x31, 0x32 }, lr.Data);
        }

        [Fact]
        public void ReadNextLogicalRecord_FirstPhysicalRecordMarkedAsContinuation_ThrowsLisParseException()
        {
            byte[] payload = BuildLrhPayload((byte)LisRecordType.FileHeader, 0x00);
            byte[] record = BuildPhysicalRecord(0x0002, payload); // predecessor bit set

            using var stream = new MemoryStream(record);
            var reader = new LisReader();

            LisParseException ex = Assert.Throws<LisParseException>(() => reader.ReadNextLogicalRecord(stream));

            Assert.Contains("continuation", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ReadNextLogicalRecord_BrokenSuccessorChain_ThrowsLisParseException()
        {
            byte[] first = BuildPhysicalRecord(
                0x0001,
                BuildLrhPayload((byte)LisRecordType.FileHeader, 0x00, 0x10)); // successor
            byte[] second = BuildPhysicalRecord(0x0000, new byte[] { 0x11, 0x12 }); // should have predecessor

            using var stream = new MemoryStream(Concat(first, second));
            var reader = new LisReader();

            LisParseException ex = Assert.Throws<LisParseException>(() => reader.ReadNextLogicalRecord(stream));

            Assert.Contains("successor chain", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ReadNextLogicalRecord_UnexpectedEofInPayload_ThrowsLisParseException()
        {
            byte[] header = new byte[] { 0x00, 0x0A, 0x00, 0x00 }; // expects 6 bytes payload
            byte[] truncatedPayload = new byte[] { 0x80, 0x00, 0x11 }; // only 3 bytes available
            byte[] record = Concat(header, truncatedPayload);

            using var stream = new MemoryStream(record);
            var reader = new LisReader();

            LisParseException ex = Assert.Throws<LisParseException>(() => reader.ReadNextLogicalRecord(stream));

            Assert.Contains("Unexpected end of stream", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ReadNextLogicalRecord_UnexpectedEofInTrailer_ThrowsLisParseException()
        {
            byte[] payload = BuildLrhPayload((byte)LisRecordType.FileHeader, 0x00, 0x77);
            byte[] header = BuildPrh((ushort)(4 + payload.Length + 2), 0x0200); // expects 2 trailer bytes
            byte[] record = Concat(header, payload); // trailer missing

            using var stream = new MemoryStream(record);
            var reader = new LisReader();

            LisParseException ex = Assert.Throws<LisParseException>(() => reader.ReadNextLogicalRecord(stream));

            Assert.Contains("trailer", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ReadNextLogicalRecord_InvalidLrhType_ThrowsLisParseException()
        {
            byte[] record = BuildPhysicalRecord(0x0000, BuildLrhPayload(0xAA, 0x00, 0x01));

            using var stream = new MemoryStream(record);
            var reader = new LisReader();

            LisParseException ex = Assert.Throws<LisParseException>(() => reader.ReadNextLogicalRecord(stream));

            Assert.Contains("record type", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ReadNextLogicalRecord_CanReadSequentialRecords()
        {
            byte[] first = BuildPhysicalRecord(
                0x0000,
                BuildLrhPayload((byte)LisRecordType.FileHeader, 0x00, 0x10, 0x11));
            byte[] second = BuildPhysicalRecord(
                0x0000,
                BuildLrhPayload((byte)LisRecordType.FileTrailer, 0x00, 0x20, 0x21, 0x22));

            using var stream = new MemoryStream(Concat(first, second));
            var reader = new LisReader();

            LisLogicalRecord record1 = reader.ReadNextLogicalRecord(stream);
            LisLogicalRecord record2 = reader.ReadNextLogicalRecord(stream);

            Assert.Equal((byte)LisRecordType.FileHeader, record1.Header.Type);
            Assert.Equal((byte)LisRecordType.FileTrailer, record2.Header.Type);
            Assert.Equal(new byte[] { 0x20, 0x21, 0x22 }, record2.Data);
        }

        [Fact]
        public void ReadNextLogicalRecord_SkipsSpacePadBlock()
        {
            byte[] pad = new byte[] { 0x20, 0x20, 0x20, 0x20 };
            byte[] record = BuildPhysicalRecord(
                0x0000,
                BuildLrhPayload((byte)LisRecordType.FileHeader, 0x00, 0x41));

            using var stream = new MemoryStream(Concat(pad, record));
            var reader = new LisReader();

            LisLogicalRecord lr = reader.ReadNextLogicalRecord(stream);

            Assert.Equal((byte)LisRecordType.FileHeader, lr.Header.Type);
            Assert.Equal(new byte[] { 0x41 }, lr.Data);
        }

        [Fact]
        public void ReadNextLogicalRecord_NullStream_ThrowsArgumentNullException()
        {
            var reader = new LisReader();
            Assert.Throws<ArgumentNullException>(() => reader.ReadNextLogicalRecord(null!));
        }

        [Fact]
        public void ReadNextLogicalRecord_UnreadableStream_ThrowsArgumentException()
        {
            var reader = new LisReader();
            using var stream = new NonReadableStream();

            Assert.Throws<ArgumentException>(() => reader.ReadNextLogicalRecord(stream));
        }

        private static byte[] BuildPhysicalRecord(ushort attributes, byte[] payload, byte[]? trailer = null)
        {
            trailer = trailer ?? Array.Empty<byte>();
            ushort length = (ushort)(LisPhysicalRecordHeader.HeaderLength + payload.Length + trailer.Length);
            byte[] header = BuildPrh(length, attributes);
            return Concat(header, payload, trailer);
        }

        private static byte[] BuildPrh(ushort length, ushort attributes)
        {
            return new byte[]
            {
                (byte)(length >> 8), (byte)(length & 0xFF),
                (byte)(attributes >> 8), (byte)(attributes & 0xFF)
            };
        }

        private static byte[] BuildLrhPayload(byte type, byte attributes, params byte[] data)
        {
            var payload = new byte[2 + data.Length];
            payload[0] = type;
            payload[1] = attributes;
            if (data.Length > 0)
            {
                Buffer.BlockCopy(data, 0, payload, 2, data.Length);
            }

            return payload;
        }

        private static byte[] Concat(byte[] first, byte[] second)
        {
            var output = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, output, 0, first.Length);
            Buffer.BlockCopy(second, 0, output, first.Length, second.Length);
            return output;
        }

        private static byte[] Concat(byte[] first, byte[] second, byte[] third)
        {
            var output = new byte[first.Length + second.Length + third.Length];
            Buffer.BlockCopy(first, 0, output, 0, first.Length);
            Buffer.BlockCopy(second, 0, output, first.Length, second.Length);
            Buffer.BlockCopy(third, 0, output, first.Length + second.Length, third.Length);
            return output;
        }

        private sealed class NonReadableStream : Stream
        {
            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => 0;

            public override long Position
            {
                get => 0;
                set => throw new NotSupportedException();
            }

            public override void Flush()
            {
                throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }
        }

        private sealed class ReadOnlyNonSeekableStream : Stream
        {
            private readonly byte[] _data;
            private int _position;

            public ReadOnlyNonSeekableStream(byte[] data)
            {
                _data = data;
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => _data.Length;

            public override long Position
            {
                get => _position;
                set => throw new NotSupportedException();
            }

            public override void Flush()
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int remaining = _data.Length - _position;
                if (remaining <= 0)
                {
                    return 0;
                }

                int toRead = Math.Min(remaining, count);
                Buffer.BlockCopy(_data, _position, buffer, offset, toRead);
                _position += toRead;
                return toRead;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }
        }
    }
}
