using System;
using System.Text;

namespace Lis.Core.Lis
{
    public sealed class LisFixedRecordParser
    {
        private const int FileRecordSize = 56;
        private const int ReelTapeRecordSize = 126;

        private static readonly Encoding Encoding = Encoding.ASCII;

        public LisFileHeaderRecord ParseFileHeader(LisLogicalRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            EnsureRecordType(record, LisRecordType.FileHeader);
            EnsureLengthAtLeast(record.Data, FileRecordSize, "file header");

            int offset = 0;
            string fileName = ReadFixedString(record.Data, ref offset, 10);
            offset += 2;
            string serviceSublevelName = ReadFixedString(record.Data, ref offset, 6);
            string versionNumber = ReadFixedString(record.Data, ref offset, 8);
            string dateOfGeneration = ReadFixedString(record.Data, ref offset, 8);
            offset += 1;
            string maxPrLength = ReadFixedString(record.Data, ref offset, 5);
            offset += 2;
            string fileType = ReadFixedString(record.Data, ref offset, 2);
            offset += 2;
            string previousFileName = ReadFixedString(record.Data, ref offset, 10);

            return new LisFileHeaderRecord(
                fileName,
                serviceSublevelName,
                versionNumber,
                dateOfGeneration,
                maxPrLength,
                fileType,
                previousFileName);
        }

        public LisFileTrailerRecord ParseFileTrailer(LisLogicalRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            EnsureRecordType(record, LisRecordType.FileTrailer);
            EnsureLengthAtLeast(record.Data, FileRecordSize, "file trailer");

            int offset = 0;
            string fileName = ReadFixedString(record.Data, ref offset, 10);
            offset += 2;
            string serviceSublevelName = ReadFixedString(record.Data, ref offset, 6);
            string versionNumber = ReadFixedString(record.Data, ref offset, 8);
            string dateOfGeneration = ReadFixedString(record.Data, ref offset, 8);
            offset += 1;
            string maxPrLength = ReadFixedString(record.Data, ref offset, 5);
            offset += 2;
            string fileType = ReadFixedString(record.Data, ref offset, 2);
            offset += 2;
            string nextFileName = ReadFixedString(record.Data, ref offset, 10);

            return new LisFileTrailerRecord(
                fileName,
                serviceSublevelName,
                versionNumber,
                dateOfGeneration,
                maxPrLength,
                fileType,
                nextFileName);
        }

        public LisReelHeaderRecord ParseReelHeader(LisLogicalRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            EnsureRecordType(record, LisRecordType.ReelHeader);
            EnsureLengthAtLeast(record.Data, ReelTapeRecordSize, "reel header");

            return ParseReelHeaderCore(record.Data);
        }

        public LisReelTrailerRecord ParseReelTrailer(LisLogicalRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            EnsureRecordType(record, LisRecordType.ReelTrailer);
            EnsureLengthAtLeast(record.Data, ReelTapeRecordSize, "reel trailer");

            return ParseReelTrailerCore(record.Data);
        }

        public LisTapeHeaderRecord ParseTapeHeader(LisLogicalRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            EnsureRecordType(record, LisRecordType.TapeHeader);
            EnsureLengthAtLeast(record.Data, ReelTapeRecordSize, "tape header");

            return ParseTapeHeaderCore(record.Data);
        }

        public LisTapeTrailerRecord ParseTapeTrailer(LisLogicalRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            EnsureRecordType(record, LisRecordType.TapeTrailer);
            EnsureLengthAtLeast(record.Data, ReelTapeRecordSize, "tape trailer");

            return ParseTapeTrailerCore(record.Data);
        }

        public LisTextRecord ParseTextRecord(LisLogicalRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            LisRecordType type = (LisRecordType)record.Header.Type;
            switch (type)
            {
                case LisRecordType.OperatorCommandInputs:
                case LisRecordType.OperatorResponseInputs:
                case LisRecordType.SystemOutputs:
                case LisRecordType.FlicComment:
                    return new LisTextRecord(type, DecodeString(record.Data, 0, record.Data.Length, trimRight: false));

                default:
                    throw new LisParseException("Invalid LIS record type for text record parser.");
            }
        }

        private static LisReelHeaderRecord ParseReelHeaderCore(byte[] data)
        {
            int offset = 0;
            string serviceName = ReadFixedString(data, ref offset, 6);
            offset += 6;
            string date = ReadFixedString(data, ref offset, 8);
            offset += 2;
            string originOfData = ReadFixedString(data, ref offset, 4);
            offset += 2;
            string name = ReadFixedString(data, ref offset, 8);
            offset += 2;
            string continuation = ReadFixedString(data, ref offset, 2);
            offset += 2;
            string previousReelName = ReadFixedString(data, ref offset, 8);
            offset += 2;
            string comment = ReadFixedString(data, ref offset, 74);

            return new LisReelHeaderRecord(
                serviceName,
                date,
                originOfData,
                name,
                continuation,
                previousReelName,
                comment);
        }

        private static LisReelTrailerRecord ParseReelTrailerCore(byte[] data)
        {
            int offset = 0;
            string serviceName = ReadFixedString(data, ref offset, 6);
            offset += 6;
            string date = ReadFixedString(data, ref offset, 8);
            offset += 2;
            string originOfData = ReadFixedString(data, ref offset, 4);
            offset += 2;
            string name = ReadFixedString(data, ref offset, 8);
            offset += 2;
            string continuation = ReadFixedString(data, ref offset, 2);
            offset += 2;
            string nextReelName = ReadFixedString(data, ref offset, 8);
            offset += 2;
            string comment = ReadFixedString(data, ref offset, 74);

            return new LisReelTrailerRecord(
                serviceName,
                date,
                originOfData,
                name,
                continuation,
                nextReelName,
                comment);
        }

        private static LisTapeHeaderRecord ParseTapeHeaderCore(byte[] data)
        {
            int offset = 0;
            string serviceName = ReadFixedString(data, ref offset, 6);
            offset += 6;
            string date = ReadFixedString(data, ref offset, 8);
            offset += 2;
            string originOfData = ReadFixedString(data, ref offset, 4);
            offset += 2;
            string name = ReadFixedString(data, ref offset, 8);
            offset += 2;
            string continuation = ReadFixedString(data, ref offset, 2);
            offset += 2;
            string previousTapeName = ReadFixedString(data, ref offset, 8);
            offset += 2;
            string comment = ReadFixedString(data, ref offset, 74);

            return new LisTapeHeaderRecord(
                serviceName,
                date,
                originOfData,
                name,
                continuation,
                previousTapeName,
                comment);
        }

        private static LisTapeTrailerRecord ParseTapeTrailerCore(byte[] data)
        {
            int offset = 0;
            string serviceName = ReadFixedString(data, ref offset, 6);
            offset += 6;
            string date = ReadFixedString(data, ref offset, 8);
            offset += 2;
            string originOfData = ReadFixedString(data, ref offset, 4);
            offset += 2;
            string name = ReadFixedString(data, ref offset, 8);
            offset += 2;
            string continuation = ReadFixedString(data, ref offset, 2);
            offset += 2;
            string nextTapeName = ReadFixedString(data, ref offset, 8);
            offset += 2;
            string comment = ReadFixedString(data, ref offset, 74);

            return new LisTapeTrailerRecord(
                serviceName,
                date,
                originOfData,
                name,
                continuation,
                nextTapeName,
                comment);
        }

        private static void EnsureRecordType(LisLogicalRecord record, LisRecordType expected)
        {
            LisRecordType actual = (LisRecordType)record.Header.Type;
            if (actual != expected)
            {
                throw new LisParseException(
                    "Invalid LIS record type for fixed record parser. Expected " + expected + ", got " + actual + ".");
            }
        }

        private static void EnsureLengthAtLeast(byte[] data, int expected, string name)
        {
            if (data.Length < expected)
            {
                throw new LisParseException(
                    "Invalid LIS " + name + " length: expected at least " + expected + " bytes.");
            }
        }

        private static string ReadFixedString(byte[] data, ref int offset, int length)
        {
            string result = DecodeString(data, offset, length, trimRight: true);
            offset += length;
            return result;
        }

        private static string DecodeString(byte[] data, int offset, int length, bool trimRight)
        {
            string value = Encoding.GetString(data, offset, length);
            return trimRight ? value.TrimEnd(' ', '\0') : value;
        }
    }
}
