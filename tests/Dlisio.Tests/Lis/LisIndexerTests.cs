using System;
using System.IO;
using Dlisio.Core.Lis;
using Xunit;

namespace Dlisio.Tests.Lis
{
    public sealed class LisIndexerTests
    {
        [Fact]
        public void Index_EmptyStream_ReturnsEmptyIndex()
        {
            using var stream = new MemoryStream(Array.Empty<byte>());
            var indexer = new LisIndexer();

            LisRecordIndex index = indexer.Index(stream);

            Assert.Empty(index.Records);
            Assert.Empty(index.ExplicitRecords);
            Assert.Empty(index.ImplicitRecords);
            Assert.Equal(0, index.Count);
        }

        [Fact]
        public void Index_MixedRecordTypes_ReturnsOffsetsAndBuckets()
        {
            byte[] fileHeader = BuildPhysicalRecord(
                0x0000,
                BuildLrhPayload((byte)LisRecordType.FileHeader, 0x00, 0x10));

            byte[] normalData = BuildPhysicalRecord(
                0x0000,
                BuildLrhPayload((byte)LisRecordType.NormalData, 0x00, 0x20, 0x21));

            byte[] multiPartData1 = BuildPhysicalRecord(
                0x0001,
                BuildLrhPayload((byte)LisRecordType.AlternateData, 0x00, 0x30));
            byte[] multiPartData2 = BuildPhysicalRecord(
                0x0002,
                new byte[] { 0x31, 0x32 });

            byte[] bytes = Concat(fileHeader, normalData, multiPartData1, multiPartData2);

            using var stream = new MemoryStream(bytes);
            var indexer = new LisIndexer();

            LisRecordIndex index = indexer.Index(stream);

            Assert.Equal(3, index.Count);
            Assert.Single(index.ExplicitRecords);
            Assert.Equal(2, index.ImplicitRecords.Count);

            Assert.Equal(0L, index.Records[0].Offset);
            Assert.Equal(LisRecordType.FileHeader, index.Records[0].Type);
            Assert.Equal(1, index.Records[0].PhysicalRecordCount);
            Assert.Equal(1, index.Records[0].DataLength);

            Assert.Equal((long)fileHeader.Length, index.Records[1].Offset);
            Assert.Equal(LisRecordType.NormalData, index.Records[1].Type);
            Assert.Equal(1, index.Records[1].PhysicalRecordCount);
            Assert.Equal(2, index.Records[1].DataLength);

            Assert.Equal((long)(fileHeader.Length + normalData.Length), index.Records[2].Offset);
            Assert.Equal(LisRecordType.AlternateData, index.Records[2].Type);
            Assert.Equal(2, index.Records[2].PhysicalRecordCount);
            Assert.Equal(3, index.Records[2].DataLength);
        }

        [Fact]
        public void Index_OfType_FiltersCorrectly()
        {
            byte[] one = BuildPhysicalRecord(0x0000, BuildLrhPayload((byte)LisRecordType.FileHeader, 0x00));
            byte[] two = BuildPhysicalRecord(0x0000, BuildLrhPayload((byte)LisRecordType.FileTrailer, 0x00));
            byte[] three = BuildPhysicalRecord(0x0000, BuildLrhPayload((byte)LisRecordType.FileHeader, 0x00));

            using var stream = new MemoryStream(Concat(one, two, three));
            var indexer = new LisIndexer();

            LisRecordIndex index = indexer.Index(stream);
            var headers = index.OfType(LisRecordType.FileHeader);
            var trailers = index.OfType(LisRecordType.FileTrailer);

            Assert.Equal(2, headers.Count);
            Assert.Single(trailers);
        }

        [Fact]
        public void Index_WithTrailingPadding_StopsCleanly()
        {
            byte[] record = BuildPhysicalRecord(0x0000, BuildLrhPayload((byte)LisRecordType.FileHeader, 0x00, 0x10));
            byte[] trailingPad = new byte[] { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };

            using var stream = new MemoryStream(Concat(record, trailingPad));
            var indexer = new LisIndexer();

            LisRecordIndex index = indexer.Index(stream);

            Assert.Single(index.Records);
            Assert.Equal(LisRecordType.FileHeader, index.Records[0].Type);
        }

        [Fact]
        public void Index_NullStream_ThrowsArgumentNullException()
        {
            var indexer = new LisIndexer();
            Assert.Throws<ArgumentNullException>(() => indexer.Index(null!));
        }

        [Fact]
        public void Index_UnreadableStream_ThrowsArgumentException()
        {
            var indexer = new LisIndexer();
            using var stream = new NonReadableStream();

            Assert.Throws<ArgumentException>(() => indexer.Index(stream));
        }

        [Fact]
        public void Index_UnseekableStream_ThrowsArgumentException()
        {
            var indexer = new LisIndexer();
            using var stream = new ReadOnlyNonSeekableStream(new byte[] { 0x00 });

            Assert.Throws<ArgumentException>(() => indexer.Index(stream));
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

        private static byte[] Concat(byte[] first, byte[] second, byte[] third, byte[] fourth)
        {
            return Concat(Concat(first, second, third), fourth);
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
