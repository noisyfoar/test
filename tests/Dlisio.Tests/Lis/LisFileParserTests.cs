using System;
using System.IO;
using System.Text;
using Dlisio.Core.Lis;
using Xunit;

namespace Dlisio.Tests.Lis
{
    public sealed class LisFileParserTests
    {
        [Fact]
        public void Parse_TwoLogicalFiles_ReturnsTwoParsedFiles()
        {
            byte[] file1Header = BuildLogicalRecord(LisRecordType.FileHeader, BuildFileRecordData("FILE000001", "PREV000001"));
            byte[] file1Dfsr = BuildLogicalRecord(LisRecordType.DataFormatSpecification, BuildSimpleDfsrForByteChannel("C1"));
            byte[] file1Data = BuildLogicalRecord(LisRecordType.NormalData, new byte[] { 0x2A });
            byte[] file1Trailer = BuildLogicalRecord(LisRecordType.FileTrailer, BuildFileRecordData("FILE000001", "NEXT000001"));

            byte[] file2Header = BuildLogicalRecord(LisRecordType.FileHeader, BuildFileRecordData("FILE000002", "PREV000002"));
            byte[] file2Trailer = BuildLogicalRecord(LisRecordType.FileTrailer, BuildFileRecordData("FILE000002", "NEXT000002"));

            byte[] bytes = Concat(file1Header, file1Dfsr, file1Data, file1Trailer, file2Header, file2Trailer);
            using var stream = new MemoryStream(bytes);
            var parser = new LisFileParser();

            var files = parser.Parse(stream);

            Assert.Equal(2, files.Count);
            Assert.Equal("FILE000001", files[0].FileHeader!.FileName);
            Assert.Equal("FILE000002", files[1].FileHeader!.FileName);
            Assert.Single(files[0].DataFormatSpecifications);
            Assert.Single(files[0].Frames);
            Assert.Empty(files[1].DataFormatSpecifications);
            Assert.Empty(files[1].Frames);
        }

        [Fact]
        public void Parse_PreservesOriginalStreamPosition()
        {
            byte[] one = BuildLogicalRecord(LisRecordType.FileHeader, BuildFileRecordData("FILE000010", "PREV000010"));
            byte[] two = BuildLogicalRecord(LisRecordType.FileTrailer, BuildFileRecordData("FILE000010", "NEXT000010"));
            byte[] bytes = Concat(one, two);

            using var stream = new MemoryStream(bytes);
            stream.Position = 3;
            var parser = new LisFileParser();

            parser.Parse(stream);

            Assert.Equal(3L, stream.Position);
        }

        [Fact]
        public void Parse_NullStream_ThrowsArgumentNullException()
        {
            var parser = new LisFileParser();
            Assert.Throws<ArgumentNullException>(() => parser.Parse(null!));
        }

        [Fact]
        public void Parse_UnseekableOrUnreadableStream_ThrowsArgumentException()
        {
            var parser = new LisFileParser();

            using var nonReadable = new NonReadableStream();
            using var nonSeekable = new ReadOnlyNonSeekableStream(new byte[] { 0x00 });

            Assert.Throws<ArgumentException>(() => parser.Parse(nonReadable));
            Assert.Throws<ArgumentException>(() => parser.Parse(nonSeekable));
        }

        private static byte[] BuildLogicalRecord(LisRecordType type, byte[] data)
        {
            byte[] payload = new byte[2 + data.Length];
            payload[0] = (byte)type;
            payload[1] = 0x00;
            if (data.Length > 0)
            {
                Buffer.BlockCopy(data, 0, payload, 2, data.Length);
            }

            return BuildPhysicalRecord(0x0000, payload, Array.Empty<byte>());
        }

        private static byte[] BuildPhysicalRecord(ushort attributes, byte[] payload, byte[] trailer)
        {
            ushort length = (ushort)(4 + payload.Length + trailer.Length);
            byte[] header = new byte[]
            {
                (byte)(length >> 8), (byte)(length & 0xFF),
                (byte)(attributes >> 8), (byte)(attributes & 0xFF)
            };

            return Concat(header, payload, trailer);
        }

        private static byte[] BuildSimpleDfsrForByteChannel(string mnemonic)
        {
            byte[] entries = Concat(
                BuildEntry((byte)LisDfsrEntryType.SpecBlockSubtype, 1, (byte)LisRepresentationCode.Byte, new byte[] { 0x00 }),
                BuildEntry((byte)LisDfsrEntryType.Terminator, 0, (byte)LisRepresentationCode.Byte, Array.Empty<byte>()));

            byte[] spec = new byte[40];
            Put(spec, 0, 4, mnemonic);
            Put(spec, 4, 6, "SRV001");
            Put(spec, 10, 8, "00000001");
            Put(spec, 18, 4, "UN");
            spec[33] = 1;
            spec[34] = (byte)LisRepresentationCode.Byte;

            return Concat(entries, spec);
        }

        private static byte[] BuildEntry(byte type, byte size, byte reprc, byte[] value)
        {
            var entry = new byte[3 + value.Length];
            entry[0] = type;
            entry[1] = size;
            entry[2] = reprc;
            if (value.Length > 0)
            {
                Buffer.BlockCopy(value, 0, entry, 3, value.Length);
            }

            return entry;
        }

        private static byte[] BuildFileRecordData(string fileName, string nextOrPrevName)
        {
            var data = new byte[56];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = 0x20;
            }

            int offset = 0;
            Put(data, offset, 10, fileName); offset += 12;
            Put(data, offset, 6, "SRV001"); offset += 6;
            Put(data, offset, 8, "VER1.000"); offset += 8;
            Put(data, offset, 8, "20260323"); offset += 9;
            Put(data, offset, 5, "16384"); offset += 7;
            Put(data, offset, 2, "LI"); offset += 4;
            Put(data, offset, 10, nextOrPrevName);
            return data;
        }

        private static void Put(byte[] buffer, int offset, int length, string value)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(value ?? string.Empty);
            int copy = Math.Min(length, bytes.Length);
            Buffer.BlockCopy(bytes, 0, buffer, offset, copy);
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

        private static byte[] Concat(byte[] first, byte[] second, byte[] third, byte[] fourth, byte[] fifth, byte[] sixth)
        {
            return Concat(Concat(first, second, third), Concat(fourth, fifth, sixth));
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

            public override void Flush() => throw new NotSupportedException();
            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
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

            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }
    }
}
