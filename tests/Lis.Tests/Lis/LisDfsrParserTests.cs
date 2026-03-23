using System;
using System.Text;
using Lis.Core.Lis;
using Xunit;

namespace Lis.Tests.Lis
{
    public sealed class LisDfsrParserTests
    {
        [Fact]
        public void Parse_Subtype0_ParsesEntriesAndSpecBlock()
        {
            byte[] entries = Concat(
                BuildEntry((byte)LisDfsrEntryType.SpecBlockSubtype, 1, (byte)LisRepresentationCode.Byte, new byte[] { 0x00 }),
                BuildEntry((byte)LisDfsrEntryType.FrameSize, 2, (byte)LisRepresentationCode.Int16, new byte[] { 0x00, 0x2A }),
                BuildEntry((byte)LisDfsrEntryType.Terminator, 0, (byte)LisRepresentationCode.Byte, Array.Empty<byte>()));

            byte[] specBlock = BuildSpecBlockSubtype0(
                "DEPT", "SRV001", "00000001", ".1IN",
                apiLogType: 1,
                apiCurveType: 2,
                apiCurveClass: 3,
                apiModifier: 4,
                fileNumber: 7,
                reservedSize: 16,
                processLevel: 9,
                samples: 5,
                reprc: (byte)LisRepresentationCode.Byte);

            LisLogicalRecord record = BuildDfsrRecord(Concat(entries, specBlock));
            var parser = new LisDfsrParser();

            LisDataFormatSpecificationRecord parsed = parser.Parse(record);

            Assert.Equal((byte)0, parsed.Subtype);
            Assert.Equal(3, parsed.Entries.Count);
            Assert.Single(parsed.SpecBlocks);

            Assert.Equal((byte)LisDfsrEntryType.FrameSize, parsed.Entries[1].Type);
            Assert.Equal(42, parsed.Entries[1].NumericValue);

            LisDfsrSpecBlock block = parsed.SpecBlocks[0];
            Assert.Equal("DEPT", block.Mnemonic);
            Assert.Equal("SRV001", block.ServiceId);
            Assert.Equal("00000001", block.ServiceOrderNumber);
            Assert.Equal(".1IN", block.Units);
            Assert.Equal((short)7, block.FileNumber);
            Assert.Equal((short)16, block.ReservedSize);
            Assert.Equal((byte)5, block.Samples);
            Assert.Equal((byte)LisRepresentationCode.Byte, block.RepresentationCode);
            Assert.Equal((byte)1, block.ApiLogType);
            Assert.Equal((byte)2, block.ApiCurveType);
            Assert.Equal((byte)3, block.ApiCurveClass);
            Assert.Equal((byte)4, block.ApiModifier);
            Assert.Equal((byte)9, block.ProcessLevel);
            Assert.Equal(0, block.ApiCodes);
            Assert.Empty(block.ProcessIndicators);
        }

        [Fact]
        public void Parse_Subtype1_ParsesApiCodesAndProcessIndicators()
        {
            byte[] entries = Concat(
                BuildEntry((byte)LisDfsrEntryType.SpecBlockSubtype, 1, (byte)LisRepresentationCode.Byte, new byte[] { 0x01 }),
                BuildEntry((byte)LisDfsrEntryType.Terminator, 0, (byte)LisRepresentationCode.Byte, Array.Empty<byte>()));

            byte[] indicators = new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55 };
            byte[] specBlock = BuildSpecBlockSubtype1(
                "TIME", "SRV002", "00000099", "MS",
                apiCodes: 123456,
                fileNumber: 3,
                reservedSize: 8,
                samples: 2,
                reprc: (byte)LisRepresentationCode.Int16,
                processIndicators: indicators);

            LisLogicalRecord record = BuildDfsrRecord(Concat(entries, specBlock));
            var parser = new LisDfsrParser();

            LisDataFormatSpecificationRecord parsed = parser.Parse(record);

            Assert.Equal((byte)1, parsed.Subtype);
            Assert.Single(parsed.SpecBlocks);

            LisDfsrSpecBlock block = parsed.SpecBlocks[0];
            Assert.Equal(123456, block.ApiCodes);
            Assert.Equal(indicators, block.ProcessIndicators);
            Assert.Equal((short)3, block.FileNumber);
            Assert.Equal((byte)2, block.Samples);
            Assert.Equal((byte)LisRepresentationCode.Int16, block.RepresentationCode);
        }

        [Fact]
        public void Parse_InvalidRecordType_ThrowsLisParseException()
        {
            LisLogicalRecord record = new LisLogicalRecord(
                new LisLogicalRecordHeader((byte)LisRecordType.FileHeader, 0),
                Array.Empty<byte>(),
                1);

            var parser = new LisDfsrParser();

            LisParseException ex = Assert.Throws<LisParseException>(() => parser.Parse(record));

            Assert.Contains("record type", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Parse_TruncatedEntryHeader_ThrowsLisParseException()
        {
            LisLogicalRecord record = BuildDfsrRecord(new byte[] { 0x10, 0x01 });
            var parser = new LisDfsrParser();

            LisParseException ex = Assert.Throws<LisParseException>(() => parser.Parse(record));

            Assert.Contains("header", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Parse_TruncatedEntryValue_ThrowsLisParseException()
        {
            byte[] bad = new byte[] { 0x10, 0x02, 0x42, 0xFF };
            LisLogicalRecord record = BuildDfsrRecord(bad);
            var parser = new LisDfsrParser();

            LisParseException ex = Assert.Throws<LisParseException>(() => parser.Parse(record));

            Assert.Contains("value", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Parse_MissingTerminator_ThrowsLisParseException()
        {
            byte[] bad = BuildEntry((byte)LisDfsrEntryType.FrameSize, 1, (byte)LisRepresentationCode.Byte, new byte[] { 0x01 });
            LisLogicalRecord record = BuildDfsrRecord(bad);
            var parser = new LisDfsrParser();

            LisParseException ex = Assert.Throws<LisParseException>(() => parser.Parse(record));

            Assert.Contains("terminator", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Parse_UnalignedSpecBlockTail_ThrowsLisParseException()
        {
            byte[] entries = BuildEntry((byte)LisDfsrEntryType.Terminator, 0, (byte)LisRepresentationCode.Byte, Array.Empty<byte>());
            byte[] badTail = new byte[] { 0x01, 0x02, 0x03 };
            LisLogicalRecord record = BuildDfsrRecord(Concat(entries, badTail));
            var parser = new LisDfsrParser();

            LisParseException ex = Assert.Throws<LisParseException>(() => parser.Parse(record));

            Assert.Contains("aligned", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Parse_EntryStringValue_DecodesText()
        {
            byte[] entries = Concat(
                BuildEntry((byte)LisDfsrEntryType.DepthScaleUnits, 4, (byte)LisRepresentationCode.String, Encoding.ASCII.GetBytes(".1IN")),
                BuildEntry((byte)LisDfsrEntryType.Terminator, 0, (byte)LisRepresentationCode.Byte, Array.Empty<byte>()));

            LisLogicalRecord record = BuildDfsrRecord(entries);
            var parser = new LisDfsrParser();

            LisDataFormatSpecificationRecord parsed = parser.Parse(record);

            Assert.Equal(".1IN", parsed.Entries[0].TextValue);
        }

        private static LisLogicalRecord BuildDfsrRecord(byte[] data)
        {
            return new LisLogicalRecord(
                new LisLogicalRecordHeader((byte)LisRecordType.DataFormatSpecification, 0),
                data,
                1);
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

        private static byte[] BuildSpecBlockSubtype0(
            string mnemonic,
            string serviceId,
            string serviceOrder,
            string units,
            byte apiLogType,
            byte apiCurveType,
            byte apiCurveClass,
            byte apiModifier,
            short fileNumber,
            short reservedSize,
            byte processLevel,
            byte samples,
            byte reprc)
        {
            var block = new byte[40];

            Put(block, 0, 4, mnemonic);
            Put(block, 4, 6, serviceId);
            Put(block, 10, 8, serviceOrder);
            Put(block, 18, 4, units);

            block[22] = apiLogType;
            block[23] = apiCurveType;
            block[24] = apiCurveClass;
            block[25] = apiModifier;

            WriteInt16BigEndian(block, 26, fileNumber);
            WriteInt16BigEndian(block, 28, reservedSize);

            block[32] = processLevel;
            block[33] = samples;
            block[34] = reprc;

            return block;
        }

        private static byte[] BuildSpecBlockSubtype1(
            string mnemonic,
            string serviceId,
            string serviceOrder,
            string units,
            int apiCodes,
            short fileNumber,
            short reservedSize,
            byte samples,
            byte reprc,
            byte[] processIndicators)
        {
            var block = new byte[40];

            Put(block, 0, 4, mnemonic);
            Put(block, 4, 6, serviceId);
            Put(block, 10, 8, serviceOrder);
            Put(block, 18, 4, units);

            WriteInt32BigEndian(block, 22, apiCodes);
            WriteInt16BigEndian(block, 26, fileNumber);
            WriteInt16BigEndian(block, 28, reservedSize);
            block[33] = samples;
            block[34] = reprc;

            Buffer.BlockCopy(processIndicators, 0, block, 35, Math.Min(5, processIndicators.Length));
            return block;
        }

        private static void Put(byte[] buffer, int offset, int length, string value)
        {
            for (int i = 0; i < length; i++)
            {
                buffer[offset + i] = 0x20;
            }

            byte[] bytes = Encoding.ASCII.GetBytes(value ?? string.Empty);
            int copy = Math.Min(length, bytes.Length);
            Buffer.BlockCopy(bytes, 0, buffer, offset, copy);
        }

        private static void WriteInt16BigEndian(byte[] bytes, int offset, short value)
        {
            bytes[offset] = (byte)((value >> 8) & 0xFF);
            bytes[offset + 1] = (byte)(value & 0xFF);
        }

        private static void WriteInt32BigEndian(byte[] bytes, int offset, int value)
        {
            bytes[offset] = (byte)((value >> 24) & 0xFF);
            bytes[offset + 1] = (byte)((value >> 16) & 0xFF);
            bytes[offset + 2] = (byte)((value >> 8) & 0xFF);
            bytes[offset + 3] = (byte)(value & 0xFF);
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
    }
}
