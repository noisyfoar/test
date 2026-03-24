using System;
using System.Text;
using Lis.Core.Lis;
using Xunit;

namespace Lis.Tests.Lis
{
    public sealed class LisFixedRecordParserTests
    {
        [Fact]
        public void ParseFileHeader_ReadsExpectedFields()
        {
            byte[] data = BuildFileRecordData(
                "FILE000001",
                "SRV001",
                "VER1.000",
                "20260323",
                "16384",
                "LI",
                "PREVFILE01");

            LisLogicalRecord record = BuildRecord(LisRecordType.FileHeader, data);
            var parser = new LisFixedRecordParser();

            LisFileHeaderRecord parsed = parser.ParseFileHeader(record);

            Assert.Equal("FILE000001", parsed.FileName);
            Assert.Equal("SRV001", parsed.ServiceSublevelName);
            Assert.Equal("VER1.000", parsed.VersionNumber);
            Assert.Equal("20260323", parsed.DateOfGeneration);
            Assert.Equal("16384", parsed.MaxPhysicalRecordLength);
            Assert.Equal("LI", parsed.FileType);
            Assert.Equal("PREVFILE01", parsed.PreviousFileName);
        }

        [Fact]
        public void ParseFileTrailer_ReadsExpectedFields()
        {
            byte[] data = BuildFileRecordData(
                "FILE000002",
                "SRV002",
                "VER2.000",
                "20260324",
                "32768",
                "LI",
                "NEXTFILE02");

            LisLogicalRecord record = BuildRecord(LisRecordType.FileTrailer, data);
            var parser = new LisFixedRecordParser();

            LisFileTrailerRecord parsed = parser.ParseFileTrailer(record);

            Assert.Equal("FILE000002", parsed.FileName);
            Assert.Equal("SRV002", parsed.ServiceSublevelName);
            Assert.Equal("VER2.000", parsed.VersionNumber);
            Assert.Equal("20260324", parsed.DateOfGeneration);
            Assert.Equal("32768", parsed.MaxPhysicalRecordLength);
            Assert.Equal("LI", parsed.FileType);
            Assert.Equal("NEXTFILE02", parsed.NextFileName);
        }

        [Fact]
        public void ParseFileHeader_InvalidRecordType_ThrowsLisParseException()
        {
            LisLogicalRecord record = BuildRecord(LisRecordType.FileTrailer, new byte[56]);
            var parser = new LisFixedRecordParser();

            LisParseException ex = Assert.Throws<LisParseException>(() => parser.ParseFileHeader(record));

            Assert.Contains("Invalid LIS record type", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ParseFileHeader_TooShort_ThrowsLisParseException()
        {
            LisLogicalRecord record = BuildRecord(LisRecordType.FileHeader, new byte[12]);
            var parser = new LisFixedRecordParser();

            LisParseException ex = Assert.Throws<LisParseException>(() => parser.ParseFileHeader(record));

            Assert.Contains("length", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ParseReelHeader_ReadsExpectedFields()
        {
            byte[] data = BuildReelTapeRecordData(
                "SVC001",
                "20260323",
                "ORIG",
                "REEL0001",
                "01",
                "REELPREV",
                "REEL COMMENT");

            LisLogicalRecord record = BuildRecord(LisRecordType.ReelHeader, data);
            var parser = new LisFixedRecordParser();

            LisReelHeaderRecord parsed = parser.ParseReelHeader(record);

            Assert.Equal("SVC001", parsed.ServiceName);
            Assert.Equal("20260323", parsed.Date);
            Assert.Equal("ORIG", parsed.OriginOfData);
            Assert.Equal("REEL0001", parsed.Name);
            Assert.Equal("01", parsed.ContinuationNumber);
            Assert.Equal("REELPREV", parsed.PreviousReelName);
            Assert.StartsWith("REEL COMMENT", parsed.Comment);
        }

        [Fact]
        public void ParseReelTrailer_ReadsExpectedFields()
        {
            byte[] data = BuildReelTapeRecordData(
                "SVC002",
                "20260324",
                "ORIG",
                "REEL0002",
                "02",
                "REELNEXT",
                "END COMMENT");

            LisLogicalRecord record = BuildRecord(LisRecordType.ReelTrailer, data);
            var parser = new LisFixedRecordParser();

            LisReelTrailerRecord parsed = parser.ParseReelTrailer(record);

            Assert.Equal("REELNEXT", parsed.NextReelName);
            Assert.StartsWith("END COMMENT", parsed.Comment);
        }

        [Fact]
        public void ParseTapeHeader_AndTrailer_ReadExpectedFields()
        {
            byte[] headerData = BuildReelTapeRecordData(
                "SVC003",
                "20260325",
                "ORIG",
                "TAPE0001",
                "11",
                "TAPEPREV",
                "HEADER COMMENT");
            byte[] trailerData = BuildReelTapeRecordData(
                "SVC004",
                "20260326",
                "ORIG",
                "TAPE0002",
                "12",
                "TAPENEXT",
                "TRAILER COMMENT");

            LisLogicalRecord headerRecord = BuildRecord(LisRecordType.TapeHeader, headerData);
            LisLogicalRecord trailerRecord = BuildRecord(LisRecordType.TapeTrailer, trailerData);
            var parser = new LisFixedRecordParser();

            LisTapeHeaderRecord parsedHeader = parser.ParseTapeHeader(headerRecord);
            LisTapeTrailerRecord parsedTrailer = parser.ParseTapeTrailer(trailerRecord);

            Assert.Equal("TAPEPREV", parsedHeader.PreviousTapeName);
            Assert.Equal("TAPENEXT", parsedTrailer.NextTapeName);
        }

        [Fact]
        public void ParseTextRecord_ParsesSupportedTypes()
        {
            byte[] data = Encoding.ASCII.GetBytes("HELLO LIS TEXT");
            var parser = new LisFixedRecordParser();

            LisTextRecord opCmd = parser.ParseTextRecord(BuildRecord(LisRecordType.OperatorCommandInputs, data));
            LisTextRecord opResp = parser.ParseTextRecord(BuildRecord(LisRecordType.OperatorResponseInputs, data));
            LisTextRecord sysOut = parser.ParseTextRecord(BuildRecord(LisRecordType.SystemOutputs, data));
            LisTextRecord flic = parser.ParseTextRecord(BuildRecord(LisRecordType.FlicComment, data));

            Assert.Equal("HELLO LIS TEXT", opCmd.Message);
            Assert.Equal("HELLO LIS TEXT", opResp.Message);
            Assert.Equal("HELLO LIS TEXT", sysOut.Message);
            Assert.Equal("HELLO LIS TEXT", flic.Message);
        }

        [Fact]
        public void ParseTextRecord_InvalidType_ThrowsLisParseException()
        {
            LisLogicalRecord record = BuildRecord(LisRecordType.FileHeader, Encoding.ASCII.GetBytes("X"));
            var parser = new LisFixedRecordParser();

            LisParseException ex = Assert.Throws<LisParseException>(() => parser.ParseTextRecord(record));

            Assert.Contains("text record", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        private static LisLogicalRecord BuildRecord(LisRecordType type, byte[] data)
        {
            var header = new LisLogicalRecordHeader((byte)type, 0x00);
            return new LisLogicalRecord(header, data, 1);
        }

        private static byte[] BuildFileRecordData(
            string fileName,
            string serviceSublevel,
            string version,
            string date,
            string maxPrLength,
            string fileType,
            string nextOrPrevFileName)
        {
            var data = CreateSpaceFilled(56);
            int offset = 0;

            Put(data, ref offset, fileName, 10);
            offset += 2;
            Put(data, ref offset, serviceSublevel, 6);
            Put(data, ref offset, version, 8);
            Put(data, ref offset, date, 8);
            offset += 1;
            Put(data, ref offset, maxPrLength, 5);
            offset += 2;
            Put(data, ref offset, fileType, 2);
            offset += 2;
            Put(data, ref offset, nextOrPrevFileName, 10);

            return data;
        }

        private static byte[] BuildReelTapeRecordData(
            string serviceName,
            string date,
            string origin,
            string name,
            string continuation,
            string reelOrTapeName,
            string comment)
        {
            var data = CreateSpaceFilled(126);
            int offset = 0;

            Put(data, ref offset, serviceName, 6);
            offset += 6;
            Put(data, ref offset, date, 8);
            offset += 2;
            Put(data, ref offset, origin, 4);
            offset += 2;
            Put(data, ref offset, name, 8);
            offset += 2;
            Put(data, ref offset, continuation, 2);
            offset += 2;
            Put(data, ref offset, reelOrTapeName, 8);
            offset += 2;
            Put(data, ref offset, comment, 74);

            return data;
        }

        private static void Put(byte[] buffer, ref int offset, string value, int length)
        {
            string text = value ?? string.Empty;
            byte[] bytes = Encoding.ASCII.GetBytes(text);
            int count = Math.Min(length, bytes.Length);
            Buffer.BlockCopy(bytes, 0, buffer, offset, count);
            offset += length;
        }

        private static byte[] CreateSpaceFilled(int length)
        {
            var bytes = new byte[length];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = 0x20;
            }

            return bytes;
        }
    }
}
