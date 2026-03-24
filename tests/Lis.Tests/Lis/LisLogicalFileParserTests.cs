using System;
using System.IO;
using System.Text;
using Lis.Core.Lis;
using Xunit;

namespace Lis.Tests.Lis
{
    public sealed class LisLogicalFileParserTests
    {
        [Fact]
        public void Parse_ValidLogicalFile_ParsesHeaderTrailerDfsrFramesAndText()
        {
            byte[] fileHeader = BuildLogicalRecord(
                LisRecordType.FileHeader,
                BuildFileRecordData("FILE000001", "PREVFILE01"));
            byte[] text = BuildLogicalRecord(
                LisRecordType.OperatorCommandInputs,
                Encoding.ASCII.GetBytes("HELLO"));
            byte[] dfsr = BuildLogicalRecord(
                LisRecordType.DataFormatSpecification,
                BuildSimpleDfsrForByteChannel("C1"));
            byte[] fdata = BuildLogicalRecord(
                LisRecordType.NormalData,
                new byte[] { 0x2A });
            byte[] fileTrailer = BuildLogicalRecord(
                LisRecordType.FileTrailer,
                BuildFileRecordData("FILE000001", "NEXTFILE01"));

            byte[] bytes = Concat(fileHeader, text, dfsr, fdata, fileTrailer);
            using var stream = new MemoryStream(bytes);

            LisRecordIndex index = new LisIndexer().Index(stream);
            var logicalFiles = new LisLogicalFilePartitioner().Partition(index);

            Assert.Single(logicalFiles);
            LisLogicalFileData parsed = new LisLogicalFileParser().Parse(stream, logicalFiles[0]);

            Assert.NotNull(parsed.FileHeader);
            Assert.NotNull(parsed.FileTrailer);
            Assert.Equal("FILE000001", parsed.FileHeader!.FileName);
            Assert.Equal("FILE000001", parsed.FileTrailer!.FileName);
            Assert.Single(parsed.TextRecords);
            Assert.Equal("HELLO", parsed.TextRecords[0].Message);
            Assert.Single(parsed.DataFormatSpecifications);
            Assert.Single(parsed.Frames);
            Assert.Single(parsed.Frames[0].Channels);
            Assert.Equal("C1", parsed.Frames[0].Channels[0].Mnemonic);
            Assert.Single(parsed.Frames[0].Channels[0].Samples);
            Assert.Equal((byte)0x2A, parsed.Frames[0].Channels[0].Samples[0]);
        }

        [Fact]
        public void Parse_DataBeforeDfsr_ThrowsLisParseException()
        {
            byte[] fileHeader = BuildLogicalRecord(
                LisRecordType.FileHeader,
                BuildFileRecordData("FILE000002", "PREVFILE02"));
            byte[] fdata = BuildLogicalRecord(
                LisRecordType.NormalData,
                new byte[] { 0x2A });
            byte[] fileTrailer = BuildLogicalRecord(
                LisRecordType.FileTrailer,
                BuildFileRecordData("FILE000002", "NEXTFILE02"));

            byte[] bytes = Concat(fileHeader, fdata, fileTrailer);
            using var stream = new MemoryStream(bytes);

            LisRecordIndex index = new LisIndexer().Index(stream);
            var logicalFiles = new LisLogicalFilePartitioner().Partition(index);
            var parser = new LisLogicalFileParser();

            LisParseException ex = Assert.Throws<LisParseException>(() => parser.Parse(stream, logicalFiles[0]));

            Assert.Contains("before Data Format Specification", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Parse_NullArguments_ThrowArgumentNullException()
        {
            var parser = new LisLogicalFileParser();
            using var stream = new MemoryStream();
            var logicalFile = new LisLogicalFile(Array.Empty<LisRecordInfo>(), null, null, false);

            Assert.Throws<ArgumentNullException>(() => parser.Parse(null!, logicalFile));
            Assert.Throws<ArgumentNullException>(() => parser.Parse(stream, null!));
        }

        [Fact]
        public void Parse_UnseekableOrUnreadableStream_ThrowsArgumentException()
        {
            var parser = new LisLogicalFileParser();
            var logicalFile = new LisLogicalFile(Array.Empty<LisRecordInfo>(), null, null, false);

            using var nonReadable = new NonReadableStream();
            using var nonSeekable = new ReadOnlyNonSeekableStream(new byte[] { 0x00 });

            Assert.Throws<ArgumentException>(() => parser.Parse(nonReadable, logicalFile));
            Assert.Throws<ArgumentException>(() => parser.Parse(nonSeekable, logicalFile));
        }

        [Fact]
        public void Parse_WithCurveOptions_CollectsCurvesWithoutFrames()
        {
            byte[] fileHeader = BuildLogicalRecord(
                LisRecordType.FileHeader,
                BuildFileRecordData("FILE000003", "PREVFILE03"));
            byte[] dfsr = BuildLogicalRecord(
                LisRecordType.DataFormatSpecification,
                BuildSimpleDfsrForByteChannel("C1"));
            byte[] fdata = BuildLogicalRecord(
                LisRecordType.NormalData,
                new byte[] { 0x33 });
            byte[] fileTrailer = BuildLogicalRecord(
                LisRecordType.FileTrailer,
                BuildFileRecordData("FILE000003", "NEXTFILE03"));

            byte[] bytes = Concat(Concat(fileHeader, dfsr, fdata), fileTrailer);
            using var stream = new MemoryStream(bytes);

            LisRecordIndex index = new LisIndexer().Index(stream);
            var logicalFiles = new LisLogicalFilePartitioner().Partition(index);
            var parser = new LisLogicalFileParser();
            var options = new LisReadOptions(selectedCurveMnemonics: new[] { "C1" }, includeFrames: false, includeCurves: true);
            var metrics = new LisReadMetrics();

            LisLogicalFileData parsed = parser.Parse(stream, logicalFiles[0], options, metrics);

            Assert.Empty(parsed.Frames);
            Assert.Single(parsed.Curves);
            Assert.Equal((byte)0x33, parsed.Curves["C1"][0]);
            Assert.Equal(4, metrics.LogicalRecordsRead);
            Assert.Equal(1, metrics.SamplesDecoded);
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
            spec[33] = 1; // samples
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

        private static byte[] Concat(byte[] first, byte[] second, byte[] third, byte[] fourth, byte[] fifth)
        {
            return Concat(Concat(first, second, third), Concat(fourth, fifth));
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
