using System;
using System.Text;

namespace Lis.Core.Lis
{
    public sealed class LisFixedRecordParser
    {
        private const int FileRecordSize = 56;
        private const int ReelTapeRecordSize = 126;

        private static readonly Encoding Encoding = Encoding.ASCII;

        /// <summary>
        /// Подробно выполняет операцию «ParseFileHeader» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
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

        /// <summary>
        /// Подробно выполняет операцию «ParseFileTrailer» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
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

        /// <summary>
        /// Подробно выполняет операцию «ParseReelHeader» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
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

        /// <summary>
        /// Подробно выполняет операцию «ParseReelTrailer» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
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

        /// <summary>
        /// Подробно выполняет операцию «ParseTapeHeader» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
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

        /// <summary>
        /// Подробно выполняет операцию «ParseTapeTrailer» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
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

        /// <summary>
        /// Подробно выполняет операцию «ParseTextRecord» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
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

        /// <summary>
        /// Подробно выполняет операцию «ParseReelHeaderCore» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
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

        /// <summary>
        /// Подробно выполняет операцию «ParseReelTrailerCore» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
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

        /// <summary>
        /// Подробно выполняет операцию «ParseTapeHeaderCore» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
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

        /// <summary>
        /// Подробно выполняет операцию «ParseTapeTrailerCore» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
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

        /// <summary>
        /// Подробно выполняет операцию «EnsureRecordType» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        private static void EnsureRecordType(LisLogicalRecord record, LisRecordType expected)
        {
            LisRecordType actual = (LisRecordType)record.Header.Type;
            if (actual != expected)
            {
                throw new LisParseException(
                    "Invalid LIS record type for fixed record parser. Expected " + expected + ", got " + actual + ".");
            }
        }

        /// <summary>
        /// Подробно выполняет операцию «EnsureLengthAtLeast» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        private static void EnsureLengthAtLeast(byte[] data, int expected, string name)
        {
            if (data.Length < expected)
            {
                throw new LisParseException(
                    "Invalid LIS " + name + " length: expected at least " + expected + " bytes.");
            }
        }

        /// <summary>
        /// Подробно выполняет операцию «ReadFixedString» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        private static string ReadFixedString(byte[] data, ref int offset, int length)
        {
            string result = DecodeString(data, offset, length, trimRight: true);
            offset += length;
            return result;
        }

        /// <summary>
        /// Подробно выполняет операцию «DecodeString» для обработки данных формата LIS.
        /// Метод проверяет входные значения, соблюдает инварианты формата и формирует результат согласно контракту.
        /// </summary>
        private static string DecodeString(byte[] data, int offset, int length, bool trimRight)
        {
            string value = Encoding.GetString(data, offset, length);
            return trimRight ? value.TrimEnd(' ', '\0') : value;
        }
    }
}
